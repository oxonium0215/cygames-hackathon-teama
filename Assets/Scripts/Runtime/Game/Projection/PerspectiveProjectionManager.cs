using System.Collections;
using System.Reflection;
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

            CachePlayerComponents();

            if (rotationCenter) projectionBuilder.SetRotationCenter(rotationCenter);
            
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

        /// <summary>
        /// Cache all player-related components to avoid repeated GetComponent calls.
        /// </summary>
        private void CachePlayerComponents()
        {
            if (!player && playerTransform)
                player = playerTransform.GetComponent<PlayerMotor>();
            if (!playerTransform && player)
                playerTransform = player.transform;
            if (!playerCollider && playerTransform)
                playerCollider = playerTransform.GetComponent<Collider>();
            if (playerTransform && !playerRb)
                playerRb = playerTransform.GetComponent<Rigidbody>();
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

            // Prepare rotation data
            var rotationSetup = PrepareRotationData(nextIndex);
            
            // Prepare player for rotation
            bool originalKinematic = playerAdapter.PrepareForRotation(makePlayerKinematicDuringSwitch, jumpOnlyDuringSwitch);

            // Perform rotation animation
            yield return PerformRotationAnimation(rotationSetup);

            // Finalize the switch
            FinalizeViewSwitch(nextIndex, rotationSetup, originalKinematic);
        }

        /// <summary>
        /// Prepare all data needed for the rotation operation.
        /// </summary>
        private RotationSetupData PrepareRotationData(int nextIndex)
        {
            Vector3 preVel = playerRb ? playerRb.linearVelocity : Vector3.zero;
            float startYaw = cameraPivot.eulerAngles.y;
            float targetYaw = (nextIndex == 0) ? viewAYaw : viewBYaw;
            Game.Level.ProjectionAxis startAxis = GetProjectionForCurrent();
            Vector3 pStart = playerTransform.position;
            float fixedY = pStart.y;
            float seamZ = projectionBuilder.GetPlaneZ();
            float seamX = projectionBuilder.GetPlaneX();

            // Calculate inverse-of-projection lateral coordinates (constant during rotation)
            float xInv, zInv;
            if (startAxis == Game.Level.ProjectionAxis.FlattenZ)
            {
                // XY -> ZY: xInv = preX, zInv = -preX
                xInv = pStart.x;
                zInv = -pStart.x;
            }
            else
            {
                // ZY -> XY: xInv = -preZ, zInv = preZ
                xInv = -pStart.z;
                zInv = pStart.z;
            }

            return new RotationSetupData
            {
                preVelocity = preVel,
                startYaw = startYaw,
                targetYaw = targetYaw,
                startAxis = startAxis,
                fixedY = fixedY,
                seamZ = seamZ,
                seamX = seamX,
                xInv = xInv,
                zInv = zInv,
                preX = pStart.x,
                preZ = pStart.z
            };
        }

        /// <summary>
        /// Perform the rotation animation loop.
        /// </summary>
        private IEnumerator PerformRotationAnimation(RotationSetupData setup)
        {
            float progress = 0f;
            float fixedY = setup.fixedY;

            while (progress >= 0f && progress < 1f)
            {
                progress = projectionController.UpdateRotation(Time.deltaTime);
                if (progress < 0f) break; // Complete
                
                // Update camera rotation
                cameraAdapter.UpdateRotation(setup.startYaw, setup.targetYaw, progress);

                // Update player position during rotation
                UpdatePlayerPositionDuringRotation(setup, ref fixedY);

                yield return null;
            }
        }

        /// <summary>
        /// Update player position and handle collision resolution during rotation.
        /// </summary>
        private void UpdatePlayerPositionDuringRotation(RotationSetupData setup, ref float fixedY)
        {
            if (rotatePlayerDuringSwitch)
            {
                var p = playerTransform.position;
                p.x = setup.xInv;
                p.z = setup.zInv;
                if (fixYDuringRotation) p.y = fixedY;
                playerTransform.position = p;

                // Resolve vertical overlaps while rotating
                if (resolveVerticalOverlapDuringRotation && playerCollider)
                {
                    if (depenetrationSolver.ResolveVerticalOverlapUpwards(playerCollider, playerTransform, 
                        groundMask, penetrationResolveIterations, false))
                    {
                        fixedY = Mathf.Max(fixedY, playerTransform.position.y);
                    }
                }
            }
            else if (fixYDuringRotation)
            {
                var p = playerTransform.position;
                p.y = fixedY;
                playerTransform.position = p;
            }
        }

        /// <summary>
        /// Finalize the view switch by updating geometry, positioning player, and restoring physics state.
        /// </summary>
        private void FinalizeViewSwitch(int nextIndex, RotationSetupData setup, bool originalKinematic)
        {
            // Switch view and rebuild geometry in-place
            viewIndex = nextIndex;
            RebuildForCurrentView();

            // Set final mapped position on target plane
            SetFinalPlayerPosition(setup);

            // Configure player for new plane
            ConfigurePlayerForNewPlane(setup);

            // Handle post-rotation collision resolution
            depenetrationSolver.ResolveVerticalOverlapUpwards(playerCollider, playerTransform, 
                groundMask, penetrationResolveIterations, true);

            // Restore player state and apply velocity mapping
            RestorePlayerStateAndVelocity(setup, originalKinematic);

            projectionController.CompleteSwitch();
        }

        /// <summary>
        /// Set the player's final position on the target plane.
        /// </summary>
        private void SetFinalPlayerPosition(RotationSetupData setup)
        {
            var p = playerTransform.position;
            if (setup.startAxis == Game.Level.ProjectionAxis.FlattenZ)
            {
                p.x = setup.seamX;
                p.z = -setup.preX;
            }
            else
            {
                p.x = -setup.preZ;
                p.z = setup.seamZ;
            }
            playerTransform.position = p;
        }

        /// <summary>
        /// Configure the player for movement on the new projection plane.
        /// </summary>
        private void ConfigurePlayerForNewPlane(RotationSetupData setup)
        {
            var nextAxis = GetProjectionForCurrent();
            float planeConst = (nextAxis == Game.Level.ProjectionAxis.FlattenZ) ? setup.seamZ : setup.seamX;
            Game.Player.MovePlane newPlane = (nextAxis == Game.Level.ProjectionAxis.FlattenZ) ? Game.Player.MovePlane.X : Game.Player.MovePlane.Z;
            playerAdapter.SetPlayerPlane(newPlane, planeConst);
        }

        /// <summary>
        /// Restore player physics state and apply velocity mapping.
        /// </summary>
        private void RestorePlayerStateAndVelocity(RotationSetupData setup, bool originalKinematic)
        {
            // Restore player state
            playerAdapter.RestoreAfterRotation(originalKinematic, jumpOnlyDuringSwitch);

            // Map velocity between coordinate systems
            var nextAxis = GetProjectionForCurrent();
            Vector3 vFinal = playerAdapter.MapVelocityBetweenAxes(setup.preVelocity, setup.startAxis, nextAxis);

            if (playerRb && !playerRb.isKinematic)
            {
                playerRb.linearVelocity = vFinal;
            }
        }

        /// <summary>
        /// Data structure to hold all rotation setup parameters.
        /// </summary>
        private struct RotationSetupData
        {
            public Vector3 preVelocity;
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
            
            // Ensure components are cached (in case this is called before Start)
            CachePlayerComponents();

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