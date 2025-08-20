using UnityEngine;

namespace Game.Physics
{
    /// <summary>
    /// Utility system for resolving collision overlaps.
    /// Provides reusable collision resolution methods separated from game logic.
    /// </summary>
    public static class CollisionResolver
    {
        /// <summary>
        /// Resolves vertical overlaps by moving the transform upward until clear.
        /// </summary>
        /// <param name="transform">Transform to move</param>
        /// <param name="collider">Collider to check overlaps for</param>
        /// <param name="groundMask">Layer mask for ground objects</param>
        /// <param name="settings">Resolution settings</param>
        /// <returns>True if any movement occurred</returns>
        public static bool ResolveVerticalOverlapUpwards(
            Transform transform, 
            Collider collider, 
            LayerMask groundMask, 
            CollisionResolutionSettings settings)
        {
            if (!collider) return false;

            bool movedAny = false;
            float movedTotal = 0f;

            for (int iter = 0; iter < settings.iterations; iter++)
            {
                var overlaps = GetOverlapsAtPosition(transform, collider, groundMask, settings.overlapBoxInflation);
                if (overlaps == null || overlaps.Length == 0) break;

                float requiredUp = 0f;
                bool hadOverlap = false;

                for (int i = 0; i < overlaps.Length; i++)
                {
                    var other = overlaps[i];
                    if (!other || other == collider) continue;
                    hadOverlap = true;

                    if (UnityEngine.Physics.ComputePenetration(
                        collider, transform.position, transform.rotation,
                        other, other.transform.position, other.transform.rotation,
                        out Vector3 dir, out float dist))
                    {
                        if (dist <= 0f) continue;

                        float upCompAbs = Mathf.Abs(Vector3.Dot(dir.normalized, Vector3.up));
                        float step = (upCompAbs > 0.0001f) ? (dist / upCompAbs) : settings.maxResolveStep;

                        step = Mathf.Clamp(step, settings.penetrationSkin, settings.maxResolveStep);
                        requiredUp = Mathf.Max(requiredUp, step);
                    }
                }

                if (!hadOverlap) break;

                if (movedTotal + requiredUp > settings.maxResolveTotal)
                    requiredUp = Mathf.Max(0f, settings.maxResolveTotal - movedTotal);

                if (requiredUp <= 0f) break;

                var p = transform.position;
                p.y += requiredUp + settings.penetrationSkin;
                transform.position = p;
#if UNITY_2021_2_OR_NEWER
                UnityEngine.Physics.SyncTransforms();
#endif
                movedAny = true;
                movedTotal += requiredUp + settings.penetrationSkin;
            }

            // Conservative fallback
            if (settings.useConservativeFallback)
            {
                var overlaps = GetOverlapsAtPosition(transform, collider, groundMask, settings.overlapBoxInflation);
                if (overlaps != null && overlaps.Length > 0)
                {
                    float highestTop = float.NegativeInfinity;
                    Bounds myB = collider.bounds;

                    foreach (var other in overlaps)
                    {
                        if (!other || other == collider) continue;
                        highestTop = Mathf.Max(highestTop, other.bounds.max.y);
                    }

                    if (!float.IsNegativeInfinity(highestTop))
                    {
                        float targetMinY = highestTop + settings.penetrationSkin + settings.groundSkin;
                        float delta = (targetMinY - myB.min.y);
                        if (delta > 0f)
                        {
                            delta = Mathf.Min(delta, Mathf.Max(0f, settings.maxResolveTotal));
                            var p = transform.position;
                            p.y += delta;
                            transform.position = p;
#if UNITY_2021_2_OR_NEWER
                            UnityEngine.Physics.SyncTransforms();
#endif
                            movedAny = true;
                        }
                    }
                }
            }

            return movedAny;
        }

        private static Collider[] GetOverlapsAtPosition(Transform transform, Collider collider, LayerMask groundMask, float inflation)
        {
            Bounds b = collider.bounds;
            Vector3 center = b.center;
            Vector3 halfExtents = b.extents * inflation;

            return UnityEngine.Physics.OverlapBox(
                center,
                halfExtents,
                transform.rotation,
                groundMask,
                QueryTriggerInteraction.Ignore
            );
        }
    }

    /// <summary>
    /// Settings for collision resolution behavior.
    /// </summary>
    [System.Serializable]
    public struct CollisionResolutionSettings
    {
        [Header("Resolution Parameters")]
        [SerializeField, Min(1)] public int iterations;
        [SerializeField, Range(0.0001f, 0.02f)] public float penetrationSkin;
        [SerializeField, Range(0.8f, 1.2f)] public float overlapBoxInflation;
        [SerializeField, Range(0.05f, 5f)] public float maxResolveStep;
        [SerializeField, Range(0.1f, 20f)] public float maxResolveTotal;
        [SerializeField] public float groundSkin;
        [SerializeField] public bool useConservativeFallback;

        public static CollisionResolutionSettings Default => new CollisionResolutionSettings
        {
            iterations = 6,
            penetrationSkin = 0.003f,
            overlapBoxInflation = 0.98f,
            maxResolveStep = 2.0f,
            maxResolveTotal = 8.0f,
            groundSkin = 0.05f,
            useConservativeFallback = true
        };
    }
}