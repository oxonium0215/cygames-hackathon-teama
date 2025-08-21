using System.Collections;
using UnityEngine;
using POC.Level;
using POC.Gameplay;

namespace POC.GameplayProjection
{
    public class PerspectiveProjectionManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform rotationCenter;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private ProjectionBuilder projectionBuilder;
        [SerializeField] private PlayerMotor player;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Collider playerCollider; // used to resolve overlaps

        [Header("Camera")]
        [SerializeField] private float cameraDistance = 10f;
        [SerializeField] private Vector3 pivotOffset = Vector3.zero;

        [Header("Views")]
        [SerializeField] private float viewAYaw = 0f;
        [SerializeField] private float viewBYaw = 90f;
        [SerializeField] private ProjectionAxis viewAProjection = ProjectionAxis.FlattenZ; // XY
        [SerializeField] private ProjectionAxis viewBProjection = ProjectionAxis.FlattenX; // ZY

        [Header("Rotation")]
        [SerializeField] private float rotateDuration = 0.3f;
        [SerializeField] private AnimationCurve rotateEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Ground / Projection")]
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private float snapDownDistance = 5f;
        [SerializeField] private float snapUpAllowance = 0.5f;
        [SerializeField] private float groundSkin = 0.05f;

        [Header("Behavior")]
        [SerializeField] private bool rotatePlayerDuringSwitch = true;
        [SerializeField] private bool makePlayerKinematicDuringSwitch = true;
        [SerializeField] private bool jumpOnlyDuringSwitch = true; // disable movement input
        [Tooltip("Keep the player's Y fixed to the pre-rotation height while rotating.")]
        [SerializeField] private bool fixYDuringRotation = true;
        [Tooltip("If overlapping during rotation, try to lift upward until clear (uses vertical-only depenetration).")]
        [SerializeField] private bool resolveVerticalOverlapDuringRotation = true;
        [Tooltip("If true, snap the player down to the ground after rotation (vertical velocity will be zeroed). Default: false.")]
        [SerializeField] private bool snapToGroundAfterRotation = false;

        [Header("Penetration Resolve (advanced)")]
        [SerializeField, Min(1)] private int penetrationResolveIterations = 6;
        [SerializeField, Range(0.0001f, 0.02f)] private float penetrationSkin = 0.003f;
        [SerializeField, Range(0.8f, 1.2f)] private float overlapBoxInflation = 0.98f;
        [SerializeField, Range(0.05f, 5f)] private float maxResolveStep = 2.0f;
        [SerializeField, Range(0.1f, 20f)] private float maxResolveTotal = 8.0f;

        [SerializeField] private int viewIndex = 0; // 0=A, 1=B
        private bool rotating;
        private Rigidbody playerRb;

        private void Start()
        {
            if (!projectionBuilder || !cameraPivot)
            {
                Debug.LogError("[PerspectiveProjectionManager] Missing ProjectionBuilder or CameraPivot.");
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

            RepositionPivotToCenter();
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
            RepositionPivotToCenter();

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

            // Freeze motor and input
            player.BeginRotationFreeze();
            if (jumpOnlyDuringSwitch) player.SetLateralEnabled(false);

            // Make kinematic during rotation (avoid writing velocities while kinematic)
            bool originalKinematic = false;
            if (playerRb && makePlayerKinematicDuringSwitch)
            {
                originalKinematic = playerRb.isKinematic;
                if (!originalKinematic)
                {
                    playerRb.linearVelocity = Vector3.zero;
                    playerRb.angularVelocity = Vector3.zero;
                    playerRb.isKinematic = true;
                }
            }

            // Rotation setup
            float startYaw = cameraPivot.eulerAngles.y;
            float targetYaw = (nextIndex == 0) ? viewAYaw : viewBYaw;
            float deltaYaw = Mathf.DeltaAngle(startYaw, targetYaw);

            ProjectionAxis startAxis = GetProjectionForCurrent();
            Vector3 pStart = playerTransform.position;
            float fixedY = pStart.y;

            float seamZ = projectionBuilder.GetPlaneZ();
            float seamX = projectionBuilder.GetPlaneX();

            // Inverse-of-projection lateral coordinates (constant during rotation)
            float preX = pStart.x;
            float preZ = pStart.z;
            float xInv, zInv;

            if (startAxis == ProjectionAxis.FlattenZ)
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
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.0001f, rotateDuration);
                float s = rotateEase.Evaluate(Mathf.Clamp01(t));

                // Camera yaw
                var eul = cameraPivot.eulerAngles;
                eul.y = Mathf.LerpAngle(startYaw, startYaw + deltaYaw, s);
                cameraPivot.eulerAngles = eul;

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
                        if (ResolveVerticalOverlapUpwards(iterations: penetrationResolveIterations, conservativeFallback: false))
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
                if (startAxis == ProjectionAxis.FlattenZ)
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
            float planeConst = (nextAxis == ProjectionAxis.FlattenZ) ? seamZ : seamX;
            player.ActivePlane = (nextAxis == ProjectionAxis.FlattenZ) ? MovePlane.X : MovePlane.Z;
            player.SetPlaneLock(player.ActivePlane, planeConst);

            // Post-rotation handling
            bool snappedToGround = false;
            if (snapToGroundAfterRotation)
            {
                snappedToGround = SnapPlayerToGround();
            }
            bool liftedPost = ResolveVerticalOverlapUpwards(iterations: penetrationResolveIterations, conservativeFallback: true);

            // Restore kinematic state
            if (playerRb && makePlayerKinematicDuringSwitch)
            {
                playerRb.isKinematic = originalKinematic;
            }

            // Re-enable input
            if (jumpOnlyDuringSwitch) player.SetLateralEnabled(true);

            // Map inertia: preserve lateral direction (no sign flip), preserve vertical velocity
            float preLateral = (startAxis == ProjectionAxis.FlattenZ) ? preVel.x : preVel.z;
            float newLateral = preLateral; // CHANGED: keep direction

            Vector3 vFinal = Vector3.zero;
            if (nextAxis == ProjectionAxis.FlattenZ) vFinal.x = newLateral; else vFinal.z = newLateral;

            // Preserve vertical velocity for natural fall
            vFinal.y = preVel.y;
            if (snapToGroundAfterRotation && snappedToGround) vFinal.y = 0f;

            if (playerRb && !playerRb.isKinematic)
            {
                playerRb.linearVelocity = vFinal;
            }

            // Unfreeze motor
            player.EndRotationFreeze();

            rotating = false;
        }

        // Vertical-only overlap resolution (upward depenetration).
        private bool ResolveVerticalOverlapUpwards(int iterations, bool conservativeFallback)
        {
            if (!playerCollider) return false;

            bool movedAny = false;
            float movedTotal = 0f;

            for (int iter = 0; iter < iterations; iter++)
            {
                var overlaps = OverlapGroundAtPlayer();
                if (overlaps == null || overlaps.Length == 0) break;

                float requiredUp = 0f;
                bool hadOverlap = false;

                for (int i = 0; i < overlaps.Length; i++)
                {
                    var other = overlaps[i];
                    if (!other || other == playerCollider) continue;
                    hadOverlap = true;

                    if (Physics.ComputePenetration(
                        playerCollider, playerTransform.position, playerTransform.rotation,
                        other, other.transform.position, other.transform.rotation,
                        out Vector3 dir, out float dist))
                    {
                        if (dist <= 0f) continue;

                        float upCompAbs = Mathf.Abs(Vector3.Dot(dir.normalized, Vector3.up));
                        float step = (upCompAbs > 0.0001f) ? (dist / upCompAbs) : maxResolveStep;

                        step = Mathf.Clamp(step, penetrationSkin, maxResolveStep);
                        requiredUp = Mathf.Max(requiredUp, step);
                    }
                }

                if (!hadOverlap) break;

                if (movedTotal + requiredUp > maxResolveTotal)
                    requiredUp = Mathf.Max(0f, maxResolveTotal - movedTotal);

                if (requiredUp <= 0f) break;

                var p = playerTransform.position;
                p.y += requiredUp + penetrationSkin;
                playerTransform.position = p;
#if UNITY_2021_2_OR_NEWER
                Physics.SyncTransforms();
#endif
                movedAny = true;
                movedTotal += requiredUp + penetrationSkin;
            }

            if (conservativeFallback)
            {
                var overlaps = OverlapGroundAtPlayer();
                if (overlaps != null && overlaps.Length > 0)
                {
                    float highestTop = float.NegativeInfinity;
                    Bounds myB = playerCollider.bounds;

                    foreach (var other in overlaps)
                    {
                        if (!other || other == playerCollider) continue;
                        highestTop = Mathf.Max(highestTop, other.bounds.max.y);
                    }

                    if (!float.IsNegativeInfinity(highestTop))
                    {
                        float targetMinY = highestTop + penetrationSkin + groundSkin;
                        float delta = (targetMinY - myB.min.y);
                        if (delta > 0f)
                        {
                            delta = Mathf.Min(delta, Mathf.Max(0f, maxResolveTotal));
                            var p = playerTransform.position;
                            p.y += delta;
                            playerTransform.position = p;
#if UNITY_2021_2_OR_NEWER
                            Physics.SyncTransforms();
#endif
                            movedAny = true;
                        }
                    }
                }
            }

            return movedAny;
        }

        private Collider[] OverlapGroundAtPlayer()
        {
            Bounds b = playerCollider.bounds;
            Vector3 center = b.center;
            Vector3 halfExtents = b.extents * overlapBoxInflation;

            return Physics.OverlapBox(
                center,
                halfExtents,
                playerTransform.rotation,
                groundMask,
                QueryTriggerInteraction.Ignore
            );
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
            RepositionPivotToCenter();

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
            SnapPlayerToGround();
            ResolveVerticalOverlapUpwards(iterations: penetrationResolveIterations, conservativeFallback: true);
        }

        private ProjectionAxis GetProjectionForCurrent() => (viewIndex == 0) ? viewAProjection : viewBProjection;

        private void RebuildForCurrentView()
        {
            projectionBuilder.Rebuild(GetProjectionForCurrent());
        }

        private void RepositionPivotToCenter()
        {
            if (!cameraPivot) return;

            Vector3 center = rotationCenter ? rotationCenter.position : cameraPivot.position - pivotOffset;
            Vector3 target = center + pivotOffset;

            // Do not scroll down: preserve current (higher) Y if applicable.
            target.y = Mathf.Max(target.y, cameraPivot.position.y);

            cameraPivot.position = target;

            if (cameraPivot.childCount > 0)
            {
                var cam = cameraPivot.GetChild(0);
                cam.localPosition = new Vector3(0f, 0f, -Mathf.Abs(cameraDistance));
                cam.localRotation = Quaternion.identity;
            }
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