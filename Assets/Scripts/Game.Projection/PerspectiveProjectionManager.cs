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

        [Header("Views")]
        [SerializeField] private float viewAYaw = 0f;
        [SerializeField] private float viewBYaw = -90f;  // Scene has -90, not 90
        [SerializeField] private ProjectionAxis viewAProjection = ProjectionAxis.FlattenZ; // XY
        [SerializeField] private ProjectionAxis viewBProjection = ProjectionAxis.FlattenX; // ZY

        [Header("Rotation")]
        [SerializeField] private float rotateDuration = 0.5f;  // Scene has 0.5, not 0.3
        [SerializeField] private AnimationCurve rotateEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Ground / Projection")]
        [SerializeField] private LayerMask groundMask;
        [Tooltip("If overlapping during rotation, try to lift upward until clear (uses vertical-only depenetration).")]
        [SerializeField] private bool resolveVerticalOverlapDuringRotation = true;
        [Tooltip("If true, snap the player down to the ground after rotation (vertical velocity will be zeroed). Default: false.")]
        [SerializeField] private bool snapToGroundAfterRotation = false;

        [Header("Projection Services")]
        [SerializeField] private DepenetrationSolver depenetrationSolver = new DepenetrationSolver();
        [SerializeField] private ProjectionKinematics projectionKinematics = new ProjectionKinematics();
        [SerializeField] private CameraPivotAdjuster cameraPivotAdjuster = new CameraPivotAdjuster();

        // Legacy fields - marked obsolete but kept for serialization compatibility
        [System.Obsolete("Deprecated; no longer used - moved to CameraPivotAdjuster service")]
        [SerializeField] private float cameraDistance = 10f;
        [System.Obsolete("Deprecated; no longer used - moved to CameraPivotAdjuster service")]
        [SerializeField] private Vector3 pivotOffset = Vector3.zero;
        [System.Obsolete("Deprecated; no longer used - moved to CameraPivotAdjuster service")]
        [SerializeField] private float snapDownDistance = 5f;
        [System.Obsolete("Deprecated; no longer used - moved to CameraPivotAdjuster service")]
        [SerializeField] private float snapUpAllowance = 0.5f;
        [System.Obsolete("Deprecated; no longer used - moved to CameraPivotAdjuster service")]
        [SerializeField] private float groundSkin = 0.05f;
        [System.Obsolete("Deprecated; no longer used - moved to ProjectionKinematics service")]
        [SerializeField] private bool rotatePlayerDuringSwitch = true;
        [System.Obsolete("Deprecated; no longer used - moved to ProjectionKinematics service")]
        [SerializeField] private bool makePlayerKinematicDuringSwitch = true;
        [System.Obsolete("Deprecated; no longer used - moved to ProjectionKinematics service")]
        [SerializeField] private bool jumpOnlyDuringSwitch = true;
        [System.Obsolete("Deprecated; no longer used - moved to ProjectionKinematics service")]
        [SerializeField] private bool fixYDuringRotation = true;
        [System.Obsolete("Deprecated; no longer used - moved to DepenetrationSolver service")]
        [SerializeField, Min(1)] private int penetrationResolveIterations = 6;
        [System.Obsolete("Deprecated; no longer used - moved to DepenetrationSolver service")]
        [SerializeField, Range(0.0001f, 0.02f)] private float penetrationSkin = 0.003f;
        [System.Obsolete("Deprecated; no longer used - moved to DepenetrationSolver service")]
        [SerializeField, Range(0.8f, 1.2f)] private float overlapBoxInflation = 0.98f;
        [System.Obsolete("Deprecated; no longer used - moved to DepenetrationSolver service")]
        [SerializeField, Range(0.05f, 5f)] private float maxResolveStep = 2.0f;
        [System.Obsolete("Deprecated; no longer used - moved to DepenetrationSolver service")]
        [SerializeField, Range(0.1f, 20f)] private float maxResolveTotal = 8.0f;

        [SerializeField] private int viewIndex = 0; // 0=A, 1=B
        private bool rotating;
        private Rigidbody playerRb;
        
        // Migration tracking
        [SerializeField, HideInInspector] private bool hasDataMigrated = false;

        private void Start()
        {
            // Migrate legacy data to services if not done yet
            if (!hasDataMigrated)
            {
                MigrateObsoleteFieldsToServices();
                hasDataMigrated = true;
            }

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

            cameraPivotAdjuster.RepositionPivotToCenter(cameraPivot, rotationCenter);
            ApplyViewImmediate(viewIndex);
        }

        public void TogglePerspective()
        {
            if (!rotating) StartCoroutine(SwitchRoutine(1 - viewIndex));
        }

        private IEnumerator SwitchRoutine(int nextIndex)
        {
            rotating = true;

            projectionBuilder.SetSourcesVisible(true);
            projectionBuilder.ClearProjected();
            cameraPivotAdjuster.RepositionPivotToCenter(cameraPivot, rotationCenter);

            // If no player, rotate camera only
            if (!playerTransform)
            {
                yield return RotateCameraOnly(nextIndex);
                viewIndex = nextIndex;
                RebuildForCurrentView();
                projectionBuilder.SetSourcesVisible(false);
                rotating = false;
                yield break;
            }

            // Capture pre-rotation velocity (read ok even if kinematic)
            Vector3 preVel = playerRb ? playerRb.linearVelocity : Vector3.zero;

            // Freeze motor and input using service
            projectionKinematics.BeginRotationFreeze(player);

            // Make kinematic during rotation (avoid writing velocities while kinematic)
            bool originalKinematic;
            bool wasKinematic = projectionKinematics.MakeKinematicIfNeeded(playerRb, out originalKinematic);

            // Rotation setup
            float startYaw = cameraPivot.eulerAngles.y;
            float targetYaw = (nextIndex == 0) ? viewAYaw : viewBYaw;
            float deltaYaw = Mathf.DeltaAngle(startYaw, targetYaw);

            ProjectionAxis startAxis = GetProjectionForCurrent();
            Vector3 pStart = playerTransform.position;
            float fixedY = pStart.y;

            float seamZ = projectionBuilder.GetPlaneZ();
            float seamX = projectionBuilder.GetPlaneX();

            // Inverse-of-projection lateral coordinates using service
            projectionKinematics.CalculateInverseProjectionCoordinates(pStart, startAxis, out float xInv, out float zInv);

            // Rotate camera; place player at inverse-projection coordinates every frame
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.0001f, rotateDuration);
                float s = rotateEase.Evaluate(Mathf.Clamp01(t));

                // Camera yaw
                var eul = cameraPivot.eulerAngles;
                eul.y = Mathf.LerpAngle(startYaw, startYaw + deltaYaw, s);
                cameraPivot.eulerAngles = eul;

                // Update player rotation position using service
                projectionKinematics.UpdatePlayerRotationPosition(playerTransform, xInv, zInv, fixedY);

                // Resolve vertical overlaps while rotating using service
                if (resolveVerticalOverlapDuringRotation && playerCollider)
                {
                    if (depenetrationSolver.ResolveVerticalOverlapUpwards(playerTransform, playerCollider, groundMask, false, cameraPivotAdjuster.GroundSkin))
                    {
                        fixedY = Mathf.Max(fixedY, playerTransform.position.y);
                    }
                }
                else if (projectionKinematics.FixYDuringRotation)
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

            // Final mapped position on target plane using service
            var nextAxis = GetProjectionForCurrent();
            projectionKinematics.SetFinalPlayerPosition(playerTransform, startAxis, seamX, seamZ, pStart.x, pStart.z);

            // Lock to new plane using service
            projectionKinematics.SetupPlayerForNewPlane(player, nextAxis, seamX, seamZ);

            // Post-rotation handling
            bool snappedToGround = false;
            if (snapToGroundAfterRotation)
            {
                snappedToGround = cameraPivotAdjuster.SnapPlayerToGround(playerTransform, groundMask);
            }
            bool liftedPost = depenetrationSolver.ResolveVerticalOverlapUpwards(playerTransform, playerCollider, groundMask, true, cameraPivotAdjuster.GroundSkin);

            // Restore kinematic state using service
            if (wasKinematic)
            {
                projectionKinematics.RestoreKinematicState(playerRb, originalKinematic);
            }

            // Re-enable input using service
            projectionKinematics.EndRotationFreeze(player);

            // Map inertia: preserve lateral direction (no sign flip), preserve vertical velocity
            Vector3 vFinal = projectionKinematics.MapInertiaToNewPlane(preVel, startAxis, nextAxis);
            if (snapToGroundAfterRotation && snappedToGround) vFinal.y = 0f;

            if (playerRb && !playerRb.isKinematic)
            {
                playerRb.linearVelocity = vFinal;
            }

            rotating = false;
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
            cameraPivotAdjuster.RepositionPivotToCenter(cameraPivot, rotationCenter);

            var eul = cameraPivot.eulerAngles;
            eul.y = (idx == 0) ? viewAYaw : viewBYaw;
            cameraPivot.eulerAngles = eul;

            RebuildForCurrentView();
            projectionBuilder.SetSourcesVisible(false);

            if (!playerTransform) return;
            if (!player && playerTransform) player = playerTransform.GetComponent<PlayerMotor>();
            if (!playerCollider && playerTransform) playerCollider = playerTransform.GetComponent<Collider>();

            var axis = GetProjectionForCurrent();
            float planeConst = (axis == ProjectionAxis.FlattenZ) ? projectionBuilder.GetPlaneZ() : projectionBuilder.GetPlaneX();
            player.ActivePlane = (axis == ProjectionAxis.FlattenZ) ? MovePlane.X : MovePlane.Z;
            player.SetPlaneLock(player.ActivePlane, planeConst);

            var p = playerTransform.position;
            if (axis == ProjectionAxis.FlattenZ) p.z = planeConst; else p.x = planeConst;
            playerTransform.position = p;

            // For initial placement it's okay to snap to ground to avoid starting inside geometry.
            cameraPivotAdjuster.SnapPlayerToGround(playerTransform, groundMask);
            depenetrationSolver.ResolveVerticalOverlapUpwards(playerTransform, playerCollider, groundMask, true, cameraPivotAdjuster.GroundSkin);
        }

        private ProjectionAxis GetProjectionForCurrent() => (viewIndex == 0) ? viewAProjection : viewBProjection;

        private void RebuildForCurrentView()
        {
            projectionBuilder.Rebuild(GetProjectionForCurrent());
        }
        
        private void MigrateObsoleteFieldsToServices()
        {
            #pragma warning disable CS0618 // Type or member is obsolete
            // Migrate camera settings to CameraPivotAdjuster
            cameraPivotAdjuster.MigrateFrom(cameraDistance, pivotOffset, snapDownDistance, 
                                           snapUpAllowance, groundSkin);
            
            // Migrate projection kinematics settings
            projectionKinematics.MigrateFrom(rotatePlayerDuringSwitch, makePlayerKinematicDuringSwitch,
                                            jumpOnlyDuringSwitch, fixYDuringRotation);
            
            // Migrate depenetration solver settings
            depenetrationSolver.MigrateFrom(penetrationResolveIterations, penetrationSkin, overlapBoxInflation,
                                           maxResolveStep, maxResolveTotal);
            #pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}