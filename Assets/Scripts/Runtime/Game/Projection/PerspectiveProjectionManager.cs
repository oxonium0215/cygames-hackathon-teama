using System.Collections;
using System.Reflection;
using UnityEngine;
using Game.Core;
using Game.Level;
using Game.Player;

namespace Game.Projection
{
    public class PerspectiveProjectionManager : MonoBehaviour, IInputSuppressor
    {
        [Header("References")]
        [SerializeField] private Transform rotationCenter;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private GeometryProjector projectionBuilder;
        [SerializeField] private PlayerMotor player;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Collider playerCollider; // used to resolve overlaps
        [SerializeField] private MonoBehaviour stagePreviewManager;

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

        /// <summary>Returns true if input should be suppressed (IInputSuppressor interface).</summary>
        public bool IsInputSuppressed => IsSwitching && JumpOnlyDuringSwitch;

        private void Start()
        {
            if (!projectionBuilder || !cameraPivot)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError($"{GameConstants.LOG_PREFIX_PROJECTION} Missing GeometryProjector or CameraPivot.");
#endif
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
            
            // Auto-discover StagePreviewManager if not assigned
            if (!stagePreviewManager) 
            {
                var previewManager = FindFirstObjectByType(System.Type.GetType("Game.Preview.StagePreviewManager, Game.Preview"));
                if (previewManager != null) 
                    stagePreviewManager = previewManager as MonoBehaviour;
            }

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
            // Block perspective switching if preview is active or transitioning
            if (stagePreviewManager && (IsPreviewManagerActive() || IsPreviewManagerTransitioning()))
                return;
                
            if (projectionController?.IsRotating != true)
                StartCoroutine(SwitchRoutine(1 - viewIndex));
        }

        private bool IsPreviewManagerActive()
        {
            if (!stagePreviewManager) return false;
            var property = stagePreviewManager.GetType().GetProperty("IsPreviewActive");
            return property != null && (bool)property.GetValue(stagePreviewManager);
        }

        private bool IsPreviewManagerTransitioning()
        {
            if (!stagePreviewManager) return false;
            var property = stagePreviewManager.GetType().GetProperty("IsTransitioning");
            return property != null && (bool)property.GetValue(stagePreviewManager);
        }

        private IEnumerator SwitchRoutine(int nextIndex)
        {
            PrepareForSwitch(nextIndex);

            // If no player, rotate camera only
            if (!playerTransform)
            {
                yield return RotateCameraOnly(nextIndex);
                viewIndex = nextIndex;
                RebuildForCurrentView();
                projectionController.CompleteSwitch();
                yield break;
            }

            var switchData = PreparePlayerRotationData(nextIndex);
            yield return PerformRotationLoop(switchData);
            FinalizePlayerSwitch(switchData);
        }

        private void PrepareForSwitch(int nextIndex)
        {
            projectionController.BeginSwitch(nextIndex, rotateDuration, rotateEase);
            projectionBuilder.ClearProjected();
            cameraAdapter.RepositionPivotToCenter(rotationCenter, pivotOffset);
        }

        private PlayerSwitchData PreparePlayerRotationData(int nextIndex)
        {
            var data = new PlayerSwitchData();
            data.nextIndex = nextIndex;
            
            // Capture pre-rotation state
            data.preVel = playerRb ? playerRb.linearVelocity : Vector3.zero;
            data.originalKinematic = playerAdapter.PrepareForRotation(makePlayerKinematicDuringSwitch, jumpOnlyDuringSwitch);
            
            // Setup rotation parameters
            data.startYaw = cameraPivot.eulerAngles.y;
            data.targetYaw = (nextIndex == 0) ? viewAYaw : viewBYaw;
            data.startAxis = GetProjectionForCurrent();
            
            Vector3 pStart = playerTransform.position;
            data.fixedY = pStart.y;
            data.seamZ = projectionBuilder.GetPlaneZ();
            data.seamX = projectionBuilder.GetPlaneX();
            
            // Calculate inverse-projection coordinates
            CalculateInverseProjectionCoordinates(data, pStart);
            
            return data;
        }

        private void CalculateInverseProjectionCoordinates(PlayerSwitchData data, Vector3 pStart)
        {
            float preX = pStart.x;
            float preZ = pStart.z;
            
            if (data.startAxis == Game.Level.ProjectionAxis.FlattenZ)
            {
                // XY -> ZY: xInv = preX, zInv = -preX
                data.xInv = preX;
                data.zInv = -preX;
                data.preX = preX;
                data.preZ = preZ;
            } else
            {
                // ZY -> XY: xInv = -preZ, zInv = preZ
                data.xInv = -preZ;
                data.zInv = preZ;
                data.preX = preX;
                data.preZ = preZ;
            }
        }

        private IEnumerator PerformRotationLoop(PlayerSwitchData data)
        {
            float progress = 0f;
            while (progress >= 0f && progress < 1f)
            {
                progress = projectionController.UpdateRotation(Time.deltaTime);
                if (progress < 0f) break; // Complete
                
                cameraAdapter.UpdateRotation(data.startYaw, data.targetYaw, progress);
                HandlePlayerDuringRotation(data);
                
                yield return null;
            }
        }

        private void HandlePlayerDuringRotation(PlayerSwitchData data)
        {
            if (rotatePlayerDuringSwitch)
            {
                var p = playerTransform.position;
                p.x = data.xInv;
                p.z = data.zInv;
                if (fixYDuringRotation) p.y = data.fixedY;
                playerTransform.position = p;

                // Resolve vertical overlaps while rotating
                if (resolveVerticalOverlapDuringRotation && playerCollider)
                {
                    if (depenetrationSolver.ResolveVerticalOverlapUpwards(playerCollider, playerTransform, 
                        groundMask, penetrationResolveIterations, false))
                    {
                        data.fixedY = Mathf.Max(data.fixedY, playerTransform.position.y);
                    }
                }
            } else if (fixYDuringRotation)
            {
                var p = playerTransform.position;
                p.y = data.fixedY;
                playerTransform.position = p;
            }
        }

        private void FinalizePlayerSwitch(PlayerSwitchData data)
        {
            // Switch view and rebuild geometry
            viewIndex = data.nextIndex;
            RebuildForCurrentView();

            // Set final mapped position on target plane
            SetFinalPlayerPosition(data);
            
            // Lock to new plane and handle post-rotation cleanup
            HandlePostRotation(data);
            
            // Restore player state and apply final velocity
            RestorePlayerState(data);
            
            projectionController.CompleteSwitch();
        }

        private void SetFinalPlayerPosition(PlayerSwitchData data)
        {
            var p = playerTransform.position;
            if (data.startAxis == Game.Level.ProjectionAxis.FlattenZ)
            {
                p.x = data.seamX;
                p.z = -data.preX;
            } else
            {
                p.x = -data.preZ;
                p.z = data.seamZ;
            }
            playerTransform.position = p;
        }

        private void HandlePostRotation(PlayerSwitchData data)
        {
            var nextAxis = GetProjectionForCurrent();
            float planeConst = (nextAxis == Game.Level.ProjectionAxis.FlattenZ) ? data.seamZ : data.seamX;
            Game.Player.MovePlane newPlane = (nextAxis == Game.Level.ProjectionAxis.FlattenZ) ? Game.Player.MovePlane.X : Game.Player.MovePlane.Z;
            playerAdapter.SetPlayerPlane(newPlane, planeConst);

            // Post-rotation overlap resolution
            depenetrationSolver.ResolveVerticalOverlapUpwards(playerCollider, playerTransform, 
                groundMask, penetrationResolveIterations, true);
        }

        private void RestorePlayerState(PlayerSwitchData data)
        {
            playerAdapter.RestoreAfterRotation(data.originalKinematic, jumpOnlyDuringSwitch);
            
            var nextAxis = GetProjectionForCurrent();
            Vector3 vFinal = playerAdapter.MapVelocityBetweenAxes(data.preVel, data.startAxis, nextAxis);

            if (playerRb && !playerRb.isKinematic)
            {
                playerRb.linearVelocity = vFinal;
            }
        }

        // Data structure to hold switch state
        private class PlayerSwitchData
        {
            public int nextIndex;
            public Vector3 preVel;
            public bool originalKinematic;
            public float startYaw;
            public float targetYaw;
            public Game.Level.ProjectionAxis startAxis;
            public float fixedY;
            public float seamZ;
            public float seamX;
            public float xInv;
            public float zInv;
            public float preX;
            public float preZ;
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
                Physics.SyncTransforms();
                return true;
            }
            return false;
        }
    }
}