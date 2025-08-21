using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Handles lateral input projection to the active plane and landing slide effects.
    /// </summary>
    public interface IPlaneMotion
    {
        /// <summary>
        /// Projects 2D input onto the appropriate 3D movement vector based on active plane.
        /// </summary>
        /// <param name="moveInput">2D movement input (-1 to 1)</param>
        /// <param name="activePlane">Current movement plane (X or Z)</param>
        /// <param name="maxSpeed">Maximum movement speed</param>
        /// <param name="currentVelocity">Current rigidbody velocity</param>
        /// <param name="isGrounded">Whether player is grounded</param>
        /// <param name="groundAccel">Ground acceleration rate</param>
        /// <param name="airAccel">Air acceleration rate</param>
        /// <param name="groundDecel">Ground deceleration rate</param>
        /// <param name="airDecel">Air deceleration rate</param>
        /// <param name="deltaTime">Fixed delta time</param>
        /// <param name="lateralEnabled">Whether lateral movement is enabled</param>
        /// <returns>Updated velocity with lateral movement applied</returns>
        Vector3 ApplyLateralMovement(Vector2 moveInput, MovePlane activePlane, float maxSpeed, 
            Vector3 currentVelocity, bool isGrounded, float groundAccel, float airAccel, 
            float groundDecel, float airDecel, float deltaTime, bool lateralEnabled);
        
        /// <summary>
        /// Updates landing slide state and applies effects.
        /// </summary>
        /// <param name="isGrounded">Whether player is currently grounded</param>
        /// <param name="lastVelY">Previous frame's Y velocity</param>
        /// <param name="landingMinFallSpeed">Minimum fall speed to trigger slide</param>
        /// <param name="deltaTime">Fixed delta time</param>
        void UpdateLandingSlide(bool isGrounded, float lastVelY, float landingMinFallSpeed, float deltaTime);
        
        /// <summary>
        /// Gets multipliers for acceleration/deceleration during landing slide.
        /// </summary>
        /// <param name="hasInput">Whether there's lateral input</param>
        /// <returns>Tuple of (accel multiplier, decel multiplier), or (1,1) if no slide</returns>
        (float accelMult, float decelMult) GetLandingSlideMultipliers(bool hasInput);
        
        /// <summary>
        /// Resets landing slide timer.
        /// </summary>
        void ResetLandingSlide();
    }
}