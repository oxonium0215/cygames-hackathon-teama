using UnityEngine;

namespace Game.Projection
{
    /// <summary>
    /// Manages projection switching state and timing.
    /// </summary>
    public interface IProjectionController
    {
        bool IsRotating { get; }
        
        /// <summary>
        /// Initiates a perspective switch operation.
        /// </summary>
        /// <param name="targetIndex">Target view index (0 or 1)</param>
        /// <param name="duration">Duration of the rotation</param>
        /// <param name="easecurve">Animation curve for rotation timing</param>
        void BeginSwitch(int targetIndex, float duration, AnimationCurve easeCore);
        
        /// <summary>
        /// Updates rotation progress and returns interpolation value.
        /// </summary>
        /// <param name="deltaTime">Time delta</param>
        /// <returns>Rotation progress (0-1), or -1 if complete</returns>
        float UpdateRotation(float deltaTime);
        
        /// <summary>
        /// Completes the current rotation operation.
        /// </summary>
        void CompleteSwitch();
    }
}