using UnityEngine;

namespace Game.Projection
{
    /// <summary>
    /// Static utility for vertical-only depenetration using OverlapBox and ComputePenetration with iteration caps.
    /// </summary>
    internal static class DepenetrationUtility
    {
        /// <summary>
        /// Resolves overlaps by moving upward only, with iteration and displacement limits.
        /// </summary>
        /// <param name="collider">Player collider to resolve</param>
        /// <param name="transform">Player transform</param>
        /// <param name="groundMask">Ground layer mask</param>
        /// <param name="iterations">Maximum iterations</param>
        /// <param name="conservativeFallback">Whether to use conservative fallback method</param>
        /// <param name="penetrationSkin">Skin distance for penetration</param>
        /// <param name="overlapBoxInflation">Inflation factor for overlap box</param>
        /// <param name="maxResolveStep">Maximum resolve step distance</param>
        /// <param name="maxResolveTotal">Maximum total resolve distance</param>
        /// <param name="groundSkin">Ground skin distance</param>
        /// <returns>True if any movement occurred</returns>
        public static bool ResolveVerticalOverlapUpwards(Collider collider, Transform transform, LayerMask groundMask, 
            int iterations, bool conservativeFallback, float penetrationSkin, float overlapBoxInflation, 
            float maxResolveStep, float maxResolveTotal, float groundSkin)
        {
            if (!collider || !transform) return false;
            
            bool movedAny = false;
            float movedTotal = 0f;
            
            for (int iter = 0; iter < iterations; iter++)
            {
                var overlaps = OverlapGroundAtPlayer(collider, transform, groundMask, overlapBoxInflation);
                if (overlaps == null || overlaps.Length == 0) break;
                
                float requiredUp = 0f;
                bool hadOverlap = false;
                
                for (int i = 0; i < overlaps.Length; i++)
                {
                    var other = overlaps[i];
                    if (!other || other == collider) continue;
                    hadOverlap = true;
                    
                    if (Physics.ComputePenetration(
                        collider, transform.position, transform.rotation,
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
                
                var p = transform.position;
                p.y += requiredUp + penetrationSkin;
                transform.position = p;
#if UNITY_2021_2_OR_NEWER
                Physics.SyncTransforms();
#endif
                movedAny = true;
                movedTotal += requiredUp + penetrationSkin;
            }
            
            if (conservativeFallback)
            {
                var overlaps = OverlapGroundAtPlayer(collider, transform, groundMask, overlapBoxInflation);
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
                        float targetMinY = highestTop + penetrationSkin + groundSkin;
                        float delta = (targetMinY - myB.min.y);
                        if (delta > 0f)
                        {
                            delta = Mathf.Min(delta, Mathf.Max(0f, maxResolveTotal));
                            var p = transform.position;
                            p.y += delta;
                            transform.position = p;
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
        
        private static Collider[] OverlapGroundAtPlayer(Collider collider, Transform transform, LayerMask groundMask, float overlapBoxInflation)
        {
            Bounds b = collider.bounds;
            Vector3 center = b.center;
            Vector3 halfExtents = b.extents * overlapBoxInflation;
            
            return Physics.OverlapBox(
                center,
                halfExtents,
                transform.rotation,
                groundMask,
                QueryTriggerInteraction.Ignore
            );
        }
    }
}