using System.Collections;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Game.Level;
using Game.Player;

namespace Game.Projection
{
    [MovedFrom(true, sourceNamespace: "POC.GameplayProjection")]
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
        [SerializeField] private float cameraDistance = 15f; // Updated to match RotationPOC.unity
        [SerializeField] private Vector3 pivotOffset = Vector3.zero;

        [Header("Views")]
        [SerializeField] private float viewAYaw = 0f;
        [SerializeField] private float viewBYaw = -90f; // Updated to match RotationPOC.unity
        [SerializeField] private Game.Level.ProjectionAxis viewAProjection = Game.Level.ProjectionAxis.FlattenZ; // XY
        [SerializeField] private Game.Level.ProjectionAxis viewBProjection = Game.Level.ProjectionAxis.FlattenX; // ZY

        [Header("Rotation")]
        [SerializeField] private float rotateDuration = 0.5f; // Updated to match RotationPOC.unity
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
        [SerializeField, Min(1)] private int penetrationResolveIterations = 3; // Updated to match RotationPOC.unity
        [SerializeField, Range(0.0001f, 0.02f)] private float penetrationSkin = 0.0015f; // Updated to match RotationPOC.unity
        [SerializeField, Range(0.8f, 1.2f)] private float overlapBoxInflation = 0.98f;
        [SerializeField, Range(0.05f, 5f)] private float maxResolveStep = 2.0f;
        [SerializeField, Range(0.1f, 20f)] private float maxResolveTotal = 8.0f;

        [SerializeField] private int viewIndex = 0; // 0=A, 1=B
        private bool rotating;
        private Rigidbody playerRb;
        
        // Services
        private ProjectionController projectionController;

        // Read-only accessors for external components (e.g., input suppression)
        /// <summary>Returns true if a perspective switch is currently in progress.</summary>
        public bool IsSwitching => projectionController?.IsRotating ?? false;
        
        /// <summary>Returns the configured jump-only-during-switch setting.</summary>
        public bool JumpOnlyDuringSwitch => jumpOnlyDuringSwitch;

        private void Start()
        {
            if (!projectionBuilder || !cameraPivot)
            {
                Debug.LogError("[PerspectiveProjectionManager] Missing GeometryProjector or CameraPivot.");
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
            projectionBuilder.InitializeOnce();

            if (playerTransform) playerRb = playerTransform.GetComponent<Rigidbody>();

            // Initialize services
            projectionController = new ProjectionController();

            RepositionCameraPivotToCenter(rotationCenter, pivotOffset);
            SetCameraDistance(cameraDistance);
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

            projectionBuilder.SetSourcesVisible(true);
            projectionBuilder.ClearProjected();
            RepositionCameraPivotToCenter(rotationCenter, pivotOffset);

            // If no player, rotate camera only
            if (!playerTransform)
            {
                yield return RotateCameraOnly(nextIndex);
                viewIndex = nextIndex;
                RebuildForCurrentView();
                projectionBuilder.SetSourcesVisible(false);
                projectionController.CompleteSwitch();
                yield break;
            }

            // Capture pre-rotation velocity (read ok even if kinematic)
            Vector3 preVel = playerRb ? playerRb.linearVelocity : Vector3.zero;

            // Prepare player for rotation
            bool originalKinematic = PreparePlayerForRotation(makePlayerKinematicDuringSwitch, jumpOnlyDuringSwitch);

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
                
                // Camera yaw
                UpdateCameraRotation(startYaw, targetYaw, progress);

                if (rotatePlayerDuringSwitch)
                {
                    var p = playerTransform.position;
                    p.x = xInv;
                    p.z = zInv;
                    if (fixYDuringRotation) p.y = fixedY;
                    playerTransform.position = p;

                    // Resolve vertical overlaps while rotating
                    if (resolveVerticalOverlapDuringRotation && playerCollider)
                    {
                        if (ResolveVerticalOverlap(penetrationResolveIterations, false))
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

            // Switch view and rebuild
            viewIndex = nextIndex;
            RebuildForCurrentView();
            projectionBuilder.SetSourcesVisible(false);

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

            // Lock to new plane
            var nextAxis = GetProjectionForCurrent();
            float planeConst = (nextAxis == Game.Level.ProjectionAxis.FlattenZ) ? seamZ : seamX;
            Game.Player.MovePlane newPlane = (nextAxis == Game.Level.ProjectionAxis.FlattenZ) ? Game.Player.MovePlane.X : Game.Player.MovePlane.Z;
            SetPlayerPlane(newPlane, planeConst);

            // Post-rotation handling
            bool liftedPost = ResolveVerticalOverlap(penetrationResolveIterations, true);

            // Restore player state
            RestorePlayerAfterRotation(originalKinematic, jumpOnlyDuringSwitch);

            // Map inertia
            Vector3 vFinal = MapVelocityBetweenAxes(preVel, startAxis, nextAxis);

            if (playerRb && !playerRb.isKinematic)
            {
                playerRb.linearVelocity = vFinal;
            }

            projectionController.CompleteSwitch();
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
            RepositionCameraPivotToCenter(rotationCenter, pivotOffset);

            var eul = cameraPivot.eulerAngles;
            eul.y = (idx == 0) ? viewAYaw : viewBYaw;
            cameraPivot.eulerAngles = eul;

            RebuildForCurrentView();
            projectionBuilder.SetSourcesVisible(false);

            if (!playerTransform) return;
            if (!player && playerTransform) player = playerTransform.GetComponent<PlayerMotor>();
            if (!playerCollider && playerTransform) playerCollider = playerTransform.GetComponent<Collider>();

            var axis = GetProjectionForCurrent();
            float planeConst = (axis == Game.Level.ProjectionAxis.FlattenZ) ? projectionBuilder.GetPlaneZ() : projectionBuilder.GetPlaneX();
            Game.Player.MovePlane newPlane = (axis == Game.Level.ProjectionAxis.FlattenZ) ? Game.Player.MovePlane.X : Game.Player.MovePlane.Z;
            SetPlayerPlane(newPlane, planeConst);

            var p = playerTransform.position;
            if (axis == Game.Level.ProjectionAxis.FlattenZ) p.z = planeConst; else p.x = planeConst;
            playerTransform.position = p;

            // For initial placement it's okay to snap to ground to avoid starting inside geometry.
            SnapPlayerToGround();
            ResolveVerticalOverlap(penetrationResolveIterations, true);
        }

        private Game.Level.ProjectionAxis GetProjectionForCurrent() => (viewIndex == 0) ? viewAProjection : viewBProjection;

        private void RebuildForCurrentView()
        {
            projectionBuilder.Rebuild(GetProjectionForCurrent());
        }

        private void RepositionPivotToCenter()
        {
            RepositionCameraPivotToCenter(rotationCenter, pivotOffset);
            SetCameraDistance(cameraDistance);
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

        #region Private Methods (replacing adapters)
        
        // Camera operations (replacing CameraProjectionAdapter)
        private void RepositionCameraPivotToCenter(Transform rotationCenter, Vector3 pivotOffset)
        {
            if (!cameraPivot) return;
            
            Vector3 center = rotationCenter ? rotationCenter.position : cameraPivot.position - pivotOffset;
            Vector3 target = center + pivotOffset;
            
            // Do not scroll down: preserve current (higher) Y if applicable (compatible with VerticalCameraFollow).
            target.y = Mathf.Max(target.y, cameraPivot.position.y);
            
            cameraPivot.position = target;
        }
        
        private void UpdateCameraRotation(float startYaw, float targetYaw, float progress)
        {
            if (!cameraPivot) return;
            
            float deltaYaw = Mathf.DeltaAngle(startYaw, targetYaw);
            float currentYaw = Mathf.LerpAngle(startYaw, startYaw + deltaYaw, progress);
            
            var eul = cameraPivot.eulerAngles;
            eul.y = currentYaw;
            cameraPivot.eulerAngles = eul;
        }
        
        private void SetCameraDistance(float distance)
        {
            if (!cameraPivot || cameraPivot.childCount == 0) return;
            
            var cam = cameraPivot.GetChild(0);
            cam.localPosition = new Vector3(0f, 0f, -Mathf.Abs(distance));
            cam.localRotation = Quaternion.identity;
        }

        // Player operations (replacing PlayerProjectionAdapter)
        private bool PreparePlayerForRotation(bool makeKinematic, bool jumpOnlyMode)
        {
            // Freeze motor and input
            player.BeginRotationFreeze();
            if (jumpOnlyMode) player.SetLateralEnabled(false);
            
            // Make kinematic during rotation
            bool originalKinematic = false;
            if (playerRb && makeKinematic)
            {
                originalKinematic = playerRb.isKinematic;
                if (!originalKinematic)
                {
                    playerRb.linearVelocity = Vector3.zero;
                    playerRb.angularVelocity = Vector3.zero;
                    playerRb.isKinematic = true;
                }
            }
            
            return originalKinematic;
        }
        
        private void RestorePlayerAfterRotation(bool originalKinematic, bool jumpOnlyMode)
        {
            // Restore kinematic state
            if (playerRb)
            {
                playerRb.isKinematic = originalKinematic;
            }
            
            // Re-enable input
            if (jumpOnlyMode) player.SetLateralEnabled(true);
            
            // Unfreeze motor
            player.EndRotationFreeze();
        }
        
        private Vector3 MapVelocityBetweenAxes(Vector3 preRotationVelocity, Game.Level.ProjectionAxis sourceAxis, Game.Level.ProjectionAxis targetAxis)
        {
            // Map inertia: preserve lateral direction (no sign flip), preserve vertical velocity
            float preLateral = (sourceAxis == Game.Level.ProjectionAxis.FlattenZ) ? preRotationVelocity.x : preRotationVelocity.z;
            float newLateral = preLateral; // Keep direction as specified in original code
            
            Vector3 vFinal = Vector3.zero;
            if (targetAxis == Game.Level.ProjectionAxis.FlattenZ) 
                vFinal.x = newLateral; 
            else 
                vFinal.z = newLateral;
                
            // Preserve vertical velocity for natural fall
            vFinal.y = preRotationVelocity.y;
            
            return vFinal;
        }
        
        private void SetPlayerPlane(Game.Player.MovePlane newPlane, float planeConstant)
        {
            player.ActivePlane = newPlane;
            player.SetPlaneLock(newPlane, planeConstant);
        }

        // Depenetration operations (replacing DepenetrationSolver)
        private bool ResolveVerticalOverlap(int iterations, bool conservativeFallback)
        {
            return DepenetrationUtility.ResolveVerticalOverlapUpwards(playerCollider, playerTransform, 
                groundMask, iterations, conservativeFallback, penetrationSkin, overlapBoxInflation, 
                maxResolveStep, maxResolveTotal, groundSkin);
        }
        
        #endregion
        }
    }
}