using UnityEngine;

namespace Game.Projection
{
    /// <summary>
    /// Handles vertical-only depenetration using OverlapBox and ComputePenetration with iteration caps.
    /// </summary>
    public interface IDepenetrationSolver
    {
        /// <summary>
        /// Resolves overlaps by moving upward only, with iteration and displacement limits.
        /// </summary>
        /// <param name="collider">Player collider to resolve</param>
        /// <param name="transform">Player transform</param>
        /// <param name="groundMask">Ground layer mask</param>
        /// <param name="iterations">Maximum iterations</param>
        /// <param name="conservativeFallback">Whether to use conservative fallback method</param>
        /// <returns>True if any movement occurred</returns>
        bool ResolveVerticalOverlapUpwards(Collider collider, Transform transform, LayerMask groundMask, 
            int iterations, bool conservativeFallback);
    }
}