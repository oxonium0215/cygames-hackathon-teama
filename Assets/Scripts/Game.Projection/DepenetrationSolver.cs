using UnityEngine;

namespace Game.Projection
{
    [System.Serializable]
    public class DepenetrationSolver
    {
        [Header("Penetration Resolve (advanced)")]
        [SerializeField, Min(1)] private int penetrationResolveIterations = 6;
        [SerializeField, Range(0.0001f, 0.02f)] private float penetrationSkin = 0.003f;
        [SerializeField, Range(0.8f, 1.2f)] private float overlapBoxInflation = 0.98f;
        [SerializeField, Range(0.05f, 5f)] private float maxResolveStep = 2.0f;
        [SerializeField, Range(0.1f, 20f)] private float maxResolveTotal = 8.0f;

        public bool ResolveVerticalOverlapUpwards(Transform playerTransform, Collider playerCollider, LayerMask groundMask, bool conservativeFallback, float groundSkin)
        {
            return ResolveVerticalOverlapUpwards(playerTransform, playerCollider, groundMask, penetrationResolveIterations, conservativeFallback, groundSkin);
        }

        // Vertical-only overlap resolution (upward depenetration).
        public bool ResolveVerticalOverlapUpwards(Transform playerTransform, Collider playerCollider, LayerMask groundMask, int iterations, bool conservativeFallback, float groundSkin)
        {
            if (!playerCollider) return false;

            bool movedAny = false;
            float movedTotal = 0f;

            for (int iter = 0; iter < iterations; iter++)
            {
                var overlaps = OverlapGroundAtPlayer(playerTransform, playerCollider, groundMask);
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
                var overlaps = OverlapGroundAtPlayer(playerTransform, playerCollider, groundMask);
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

        private Collider[] OverlapGroundAtPlayer(Transform playerTransform, Collider playerCollider, LayerMask groundMask)
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
    }
}