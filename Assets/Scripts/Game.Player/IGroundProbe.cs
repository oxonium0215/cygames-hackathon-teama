using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Handles ground detection, coyote time, and jump buffer logic.
    /// </summary>
    public interface IGroundProbe
    {
        bool IsGrounded { get; }
        
        /// <summary>
        /// Updates ground detection and timing logic.
        /// </summary>
        /// <param name="groundCheckPosition">World position for ground check</param>
        /// <param name="groundCheckRadius">Radius for ground check sphere</param>
        /// <param name="groundMask">Layer mask for ground detection</param>
        /// <param name="deltaTime">Time delta for this frame</param>
        void UpdateGroundCheck(Vector3 groundCheckPosition, float groundCheckRadius, LayerMask groundMask, float deltaTime);
        
        /// <summary>
        /// Checks if a jump can be performed based on coyote time.
        /// </summary>
        bool CanJump();
        
        /// <summary>
        /// Sets the jump buffer timer when jump is queued.
        /// </summary>
        /// <param name="bufferTime">Jump buffer duration</param>
        void SetJumpBuffer(float bufferTime);
        
        /// <summary>
        /// Consumes the jump buffer and coyote time.
        /// </summary>
        void ConsumeJump();
        
        /// <summary>
        /// Gets the remaining jump buffer time.
        /// </summary>
        float GetJumpBufferTime();
    }
}