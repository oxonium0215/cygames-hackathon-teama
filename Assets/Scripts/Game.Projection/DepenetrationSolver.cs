using UnityEngine;

namespace Game.Projection
{
    /// <summary>
    /// Handles vertical-only depenetration with iteration caps and conservative fallback.
    /// </summary>
    public class DepenetrationSolver : IDepenetrationSolver
    {
        private readonly float penetrationSkin;
        private readonly float overlapBoxInflation;
        private readonly float maxResolveStep;
        private readonly float maxResolveTotal;
        private readonly float groundSkin;
        
        public DepenetrationSolver(float penetrationSkin, float overlapBoxInflation, 
            float maxResolveStep, float maxResolveTotal, float groundSkin)
        {
            this.penetrationSkin = penetrationSkin;
            this.overlapBoxInflation = overlapBoxInflation;
            this.maxResolveStep = maxResolveStep;
            this.maxResolveTotal = maxResolveTotal;
            this.groundSkin = groundSkin;
        }
        
        public bool ResolveVerticalOverlapUpwards(Collider collider, Transform transform, LayerMask groundMask, 
            int iterations, bool conservativeFallback)
        {
            if (!collider || !transform) return false;
            
            bool movedAny = false;
            float movedTotal = 0f;
            
            for (int iter = 0; iter < iterations; iter++)
            {
                var overlaps = OverlapGroundAtPlayer(collider, transform, groundMask);
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
                var overlaps = OverlapGroundAtPlayer(collider, transform, groundMask);
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
        
        private Collider[] OverlapGroundAtPlayer(Collider collider, Transform transform, LayerMask groundMask)
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