using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Pure C# implementation of lateral movement and landing slide logic.
    /// </summary>
    public class PlaneMotion
    {
        private readonly bool enableLandingSlide;
        private readonly float landingSlideDuration;
        private readonly float landingAccelMultiplier;
        private readonly float landingDecelMultiplier;
        
        private float landingSlideTimer;
        
        public PlaneMotion(bool enableLandingSlide, float landingSlideDuration, 
            float landingAccelMultiplier, float landingDecelMultiplier)
        {
            this.enableLandingSlide = enableLandingSlide;
            this.landingSlideDuration = landingSlideDuration;
            this.landingAccelMultiplier = landingAccelMultiplier;
            this.landingDecelMultiplier = landingDecelMultiplier;
        }
        
        public Vector3 ApplyLateralMovement(Vector2 moveInput, MovePlane activePlane, float maxSpeed, 
            Vector3 currentVelocity, bool isGrounded, float groundAccel, float airAccel, 
            float groundDecel, float airDecel, float deltaTime, bool lateralEnabled)
        {
            float input = lateralEnabled ? Mathf.Clamp(moveInput.x, -1f, 1f) : 0f;
            float desired = input * maxSpeed;
            
            bool hasInput = Mathf.Abs(input) > 0.01f;
            float response = hasInput
                ? (isGrounded ? groundAccel : airAccel)
                : (isGrounded ? groundDecel : airDecel);
                
            // Apply landing slide multipliers
            if (enableLandingSlide && isGrounded && landingSlideTimer > 0f)
            {
                response *= hasInput ? landingAccelMultiplier : landingDecelMultiplier;
            }
            
            var velocity = currentVelocity;
            if (activePlane == MovePlane.X)
                velocity.x = Mathf.MoveTowards(velocity.x, desired, response * deltaTime);
            else
                velocity.z = Mathf.MoveTowards(velocity.z, desired, response * deltaTime);
                
            return velocity;
        }
        
        public void UpdateLandingSlide(bool isGrounded, float lastVelY, float landingMinFallSpeed, float deltaTime)
        {
            // Landing slide trigger
            if (enableLandingSlide && isGrounded && lastVelY < 0f && Mathf.Abs(lastVelY) >= landingMinFallSpeed)
                landingSlideTimer = landingSlideDuration;
                
            if (landingSlideTimer > 0f)
                landingSlideTimer -= deltaTime;
        }
        
        public (float accelMult, float decelMult) GetLandingSlideMultipliers(bool hasInput)
        {
            if (!enableLandingSlide || landingSlideTimer <= 0f)
                return (1f, 1f);
                
            return hasInput ? (landingAccelMultiplier, 1f) : (1f, landingDecelMultiplier);
        }
        
        public void ResetLandingSlide()
        {
            landingSlideTimer = 0f;
        }
    }
}