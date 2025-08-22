using System.Collections;
using UnityEngine;
using Game.Core;
using Game.Level;
using Game.Player;

namespace Game.Projection
{
    public class PerspectiveProjectionManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform rotationCenter;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private GeometryProjector projectionBuilder;
        [SerializeField] private PlayerMotor player;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Collider playerCollider; // used to resolve overlaps

        [Header("Camera")]
        [SerializeField] private float cameraDistance = 15f;
        [SerializeField] private Vector3 pivotOffset = Vector3.zero;

        [Header("Views")]
        [SerializeField] private float viewAYaw = 0f;
        [SerializeField] private float viewBYaw = -90f;
        [SerializeField] private Game.Level.ProjectionAxis viewAProjection = Game.Level.ProjectionAxis.FlattenZ; // XY
        [SerializeField] private Game.Level.ProjectionAxis viewBProjection = Game.Level.ProjectionAxis.FlattenX; // ZY

        [Header("Rotation")]
        [SerializeField] private float rotateDuration = 0.5f;
        [SerializeField] private AnimationCurve rotateEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Ground / Projection")]
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private float snapDownDistance = 5f;
        [SerializeField] private float snapUpAllowance = 0.5f;
        [SerializeField] private float groundSkin = 0.05f;

        [Header("Behavior")]
        [SerializeField] private bool rotatePlayerDuringSwitch = true;
        [SerializeField] private bool makePlayerKinematicDuringSwitch = true;
        [Tooltip("When true, disable lateral movement input during perspective switches (jump remains enabled).")]
        [SerializeField] private bool jumpOnlyDuringSwitch = true;
        [Tooltip("Keep the player's Y fixed to the pre-rotation height while rotating.")]
        [SerializeField] private bool fixYDuringRotation = true;
        [Tooltip("If overlapping during rotation, try to lift upward until clear (uses vertical-only depenetration).")]
        [SerializeField] private bool resolveVerticalOverlapDuringRotation = true;

        [Header("Penetration Resolve (advanced)")]
        [SerializeField, Min(1)] private int penetrationResolveIterations = 3;
        [SerializeField, Range(0.0001f, 0.02f)] private float penetrationSkin = 0.0015f;
        [SerializeField, Range(0.8f, 1.2f)] private float overlapBoxInflation = 0.98f;
        [SerializeField, Range(0.05f, 5f)] private float maxResolveStep = 2.0f;
        [SerializeField, Range(0.1f, 20f)] private float maxResolveTotal = 8.0f;

        [SerializeField] private int viewIndex = 0; // 0=A, 1=B
        private bool rotating;
        private Rigidbody playerRb;
        
        // Services
        private ProjectionController projectionController;
        private PlayerProjectionAdapter playerAdapter;
        private CameraProjectionAdapter cameraAdapter;
        private DepenetrationSolver depenetrationSolver;

        // Read-only accessors for external components (e.g., input suppression)
        /// <summary>Returns true if a perspective switch is currently in progress.</summary>
        public bool IsSwitching => projectionController?.IsRotating ?? false;
        
        /// <summary>Returns the configured jump-only-during-switch setting.</summary>
        public bool JumpOnlyDuringSwitch => jumpOnlyDuringSwitch;

        private void Start()
        {
            if (!projectionBuilder || !cameraPivot)
            {
                Debug.LogError($"{GameConstants.LOG_PREFIX_PROJECTION} Missing GeometryProjector or CameraPivot.");
                enabled = false;
                return;
            }

            if (!player && playerTransform)
                player = playerTransform.GetComponent<PlayerMotor>();
            if (!playerTransform && player)
                playerTransform = player.transform;
            if (!playerCollider && playerTransform)
                playerCollider = playerTransform.GetComponent<Collider>();

            if (rotationCenter) projectionBuilder.SetRotationCenter(rotationCenter);

            if (playerTransform) playerRb = playerTransform.GetComponent<Rigidbody>();

            // Initialize services
            projectionController = new ProjectionController();
            playerAdapter = new PlayerProjectionAdapter(player, playerRb);
            cameraAdapter = new CameraProjectionAdapter(cameraPivot);
            depenetrationSolver = new DepenetrationSolver(penetrationSkin, overlapBoxInflation, 
                maxResolveStep, maxResolveTotal, groundSkin);

            cameraAdapter.RepositionPivotToCenter(rotationCenter, pivotOffset);
            cameraAdapter.SetCameraDistance(cameraDistance);
            ApplyViewImmediate(viewIndex);
        }

        public void TogglePerspective()
        {
            if (projectionController?.IsRotating != true)
                StartCoroutine(SwitchRoutine(1 - viewIndex));
        }

        private IEnumerator SwitchRoutine(int nextIndex)
        {
            projectionController.BeginSwitch(nextIndex, rotateDuration, rotateEase);

            // Clear previous projection state and reposition camera
            projectionBuilder.ClearProjected();
            cameraAdapter.RepositionPivotToCenter(rotationCenter, pivotOffset);

            // If no player, rotate camera only
            if (!playerTransform)
            {
                yield return RotateCameraOnly(nextIndex);
                viewIndex = nextIndex;
                RebuildForCurrentView();
                projectionController.CompleteSwitch();
                yield break;
            }

            // Capture pre-rotation velocity (read ok even if kinematic)
            Vector3 preVel = playerRb ? playerRb.linearVelocity : Vector3.zero;

            // Use PlayerProjectionAdapter to prepare for rotation
            bool originalKinematic = playerAdapter.PrepareForRotation(makePlayerKinematicDuringSwitch, jumpOnlyDuringSwitch);

            // Rotation setup
            float startYaw = cameraPivot.eulerAngles.y;
            float targetYaw = (nextIndex == 0) ? viewAYaw : viewBYaw;

            Game.Level.ProjectionAxis startAxis = GetProjectionForCurrent();
            Vector3 pStart = playerTransform.position;
            float fixedY = pStart.y;

            float seamZ = projectionBuilder.GetPlaneZ();
            float seamX = projectionBuilder.GetPlaneX();

            // Inverse-of-projection lateral coordinates (constant during rotation)
            float preX = pStart.x;
            float preZ = pStart.z;
            float xInv, zInv;

            if (startAxis == Game.Level.ProjectionAxis.FlattenZ)
            {
                // XY -> ZY: xInv = preX, zInv = -preX
                xInv = preX;
                zInv = -preX;
            } else
            {
                // ZY -> XY: xInv = -preZ, zInv = preZ
                xInv = -preZ;
                zInv = preZ;
            }

            // Rotate camera; place player at inverse-projection coordinates every frame
            float progress = 0f;
            while (progress >= 0f && progress < 1f)
            {
                progress = projectionController.UpdateRotation(Time.deltaTime);
                if (progress < 0f) break; // Complete
                
                // Camera yaw using CameraProjectionAdapter
                cameraAdapter.UpdateRotation(startYaw, targetYaw, progress);

                if (rotatePlayerDuringSwitch)
                {
                    var p = playerTransform.position;
                    p.x = xInv;
                    p.z = zInv;
                    if (fixYDuringRotation) p.y = fixedY;
                    playerTransform.position = p;

                    // Resolve vertical overlaps while rotating using DepenetrationSolver
                    if (resolveVerticalOverlapDuringRotation && playerCollider)
                    {
                        if (depenetrationSolver.ResolveVerticalOverlapUpwards(playerCollider, playerTransform, 
                            groundMask, penetrationResolveIterations, false))
                        {
                            fixedY = Mathf.Max(fixedY, playerTransform.position.y);
                        }
                    }
                } else if (fixYDuringRotation)
                {
                    var p = playerTransform.position;
                    p.y = fixedY;
                    playerTransform.position = p;
                }

                yield return null;
            }

            // Switch view and rebuild geometry in-place
            viewIndex = nextIndex;
            RebuildForCurrentView();

            // Final mapped position on target plane
            {
                var p = playerTransform.position;
                if (startAxis == Game.Level.ProjectionAxis.FlattenZ)
                {
                    p.x = seamX;
                    p.z = -preX;
                } else
                {
                    p.x = -preZ;
                    p.z = seamZ;
                }
                playerTransform.position = p;
            }

            // Lock to new plane using PlayerProjectionAdapter
            var nextAxis = GetProjectionForCurrent();
            float planeConst = (nextAxis == Game.Level.ProjectionAxis.FlattenZ) ? seamZ : seamX;
            Game.Player.MovePlane newPlane = (nextAxis == Game.Level.ProjectionAxis.FlattenZ) ? Game.Player.MovePlane.X : Game.Player.MovePlane.Z;
            playerAdapter.SetPlayerPlane(newPlane, planeConst);

            // Post-rotation handling using DepenetrationSolver
            bool liftedPost = depenetrationSolver.ResolveVerticalOverlapUpwards(playerCollider, playerTransform, 
                groundMask, penetrationResolveIterations, true);

            // Use PlayerProjectionAdapter to restore state
            playerAdapter.RestoreAfterRotation(originalKinematic, jumpOnlyDuringSwitch);

            // Map inertia using PlayerProjectionAdapter
            Vector3 vFinal = playerAdapter.MapVelocityBetweenAxes(preVel, startAxis, nextAxis);

            if (playerRb && !playerRb.isKinematic)
            {
                playerRb.linearVelocity = vFinal;
            }

            projectionController.CompleteSwitch();
        }

        // Vertical-only overlap resolution (delegated to DepenetrationSolver service).
        private bool ResolveVerticalOverlapUpwards(int iterations, bool conservativeFallback)
        {
            return depenetrationSolver?.ResolveVerticalOverlapUpwards(playerCollider, playerTransform, 
                groundMask, iterations, conservativeFallback) ?? false;
        }


        private IEnumerator RotateCameraOnly(int nextIndex)
        {
            float startY = cameraPivot.eulerAngles.y;
            float targetY = (nextIndex == 0) ? viewAYaw : viewBYaw;
            float deltaYaw = Mathf.DeltaAngle(startY, targetY);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.0001f, rotateDuration);
                float eased = rotateEase.Evaluate(Mathf.Clamp01(t));
                float y = Mathf.LerpAngle(startY, startY + deltaYaw, eased);
                var eul = cameraPivot.eulerAngles; eul.y = y; cameraPivot.eulerAngles = eul;
                yield return null;
            }
        }

        private void ApplyViewImmediate(int idx)
        {
            cameraAdapter?.RepositionPivotToCenter(rotationCenter, pivotOffset);

            var eul = cameraPivot.eulerAngles;
            eul.y = (idx == 0) ? viewAYaw : viewBYaw;
            cameraPivot.eulerAngles = eul;

            RebuildForCurrentView();

            if (!playerTransform) return;
            if (!player && playerTransform) player = playerTransform.GetComponent<PlayerMotor>();
            if (!playerCollider && playerTransform) playerCollider = playerTransform.GetComponent<Collider>();

            var axis = GetProjectionForCurrent();
            float planeConst = (axis == Game.Level.ProjectionAxis.FlattenZ) ? projectionBuilder.GetPlaneZ() : projectionBuilder.GetPlaneX();
            Game.Player.MovePlane newPlane = (axis == Game.Level.ProjectionAxis.FlattenZ) ? Game.Player.MovePlane.X : Game.Player.MovePlane.Z;
            playerAdapter?.SetPlayerPlane(newPlane, planeConst);

            var p = playerTransform.position;
            if (axis == Game.Level.ProjectionAxis.FlattenZ) p.z = planeConst; else p.x = planeConst;
            playerTransform.position = p;

            // For initial placement it's okay to snap to ground to avoid starting inside geometry.
            SnapPlayerToGround();
            depenetrationSolver?.ResolveVerticalOverlapUpwards(playerCollider, playerTransform, 
                groundMask, penetrationResolveIterations, true);
        }

        private Game.Level.ProjectionAxis GetProjectionForCurrent() => (viewIndex == 0) ? viewAProjection : viewBProjection;

        private void RebuildForCurrentView()
        {
            projectionBuilder.Rebuild(GetProjectionForCurrent());
        }

        private void RepositionPivotToCenter()
        {
            cameraAdapter?.RepositionPivotToCenter(rotationCenter, pivotOffset);
            cameraAdapter?.SetCameraDistance(cameraDistance);
        }

        private bool SnapPlayerToGround()
        {
            if (!playerTransform) return false;

            Vector3 origin = playerTransform.position + Vector3.up * snapUpAllowance;
            float maxDist = snapUpAllowance + snapDownDistance;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDist, groundMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 p = playerTransform.position;
                p.y = hit.point.y + groundSkin;
                playerTransform.position = p;
#if UNITY_2021_2_OR_NEWER
                Physics.SyncTransforms();
#endif
                return true;
            }
            return false;
        }
    }
}