using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Handles jumping and gravity for the player
    /// </summary>
    public class PlayerJumping
    {
        private readonly Rigidbody rb;
        private readonly GroundProbe groundProbe;
        
        // Jump settings
        private readonly float jumpHeight;
        private readonly float jumpCutMultiplier;
        private readonly float springJumpHeight;
        private readonly float jumpBufferTime;
        
        // Gravity settings
        private readonly float gravity;
        private readonly float groundStickForce;
        private readonly float landingMinFallSpeed;
        
        // State
        private bool jumpQueued;
        private bool jumpHeld;
        private bool jumpingEnabled = true;
        private bool autoJump = true;
        private float lastVelY;
        
        private float JumpVelocity => Mathf.Sqrt(2f * Mathf.Abs(gravity) * Mathf.Max(0.0001f, jumpHeight));
        private float SpringJumpVelocity => Mathf.Sqrt(2f * Mathf.Abs(gravity) * Mathf.Max(0.0001f, springJumpHeight));
        
        public PlayerJumping(Rigidbody rigidbody, GroundProbe groundProbeService, 
            float jumpHeight, float jumpCutMultiplier, float springJumpHeight, float jumpBufferTime,
            float gravity, float groundStickForce, float landingMinFallSpeed)
        {
            rb = rigidbody;
            groundProbe = groundProbeService;
            this.jumpHeight = jumpHeight;
            this.jumpCutMultiplier = jumpCutMultiplier;
            this.springJumpHeight = springJumpHeight;
            this.jumpBufferTime = jumpBufferTime;
            this.gravity = gravity;
            this.groundStickForce = groundStickForce;
            this.landingMinFallSpeed = landingMinFallSpeed;
        }
        
        public void SetJumpingEnabled(bool enabled) => jumpingEnabled = enabled;
        
        public void QueueJump(bool rotationFrozen)
        {
            if (rotationFrozen || !jumpingEnabled) return;
            jumpQueued = true;
            jumpHeld = true;
            autoJump = false;
            groundProbe?.SetJumpBuffer(jumpBufferTime);
        }
        
        public void JumpCanceled(bool rotationFrozen)
        {
            if (rotationFrozen) return;
            var v = rb.linearVelocity;
            autoJump = true;
            if (v.y > 0f)
            {
                v.y *= jumpCutMultiplier;
                rb.linearVelocity = v;
            }
        }
        
        public void AutoJump(bool rotationFrozen)
        {
            if (rotationFrozen) return;
            jumpQueued = true;
            jumpHeld = true;
            groundProbe?.SetJumpBuffer(jumpBufferTime);
        }
        
        public void ApplyGravityAndJumping(bool isGrounded, bool onSpring, PlaneMotion planeMotion, float deltaTime)
        {
            var velocity = rb.linearVelocity;
            
            // Handle spring auto-jump
            if (autoJump && onSpring)
            {
                AutoJump(false);
            }
            else if (!autoJump && onSpring)
            {
                QueueJump(false);
            }
            
            // Apply gravity + stick
            velocity.y += gravity * deltaTime;
            if (isGrounded && velocity.y < 0f)
                velocity.y -= groundStickForce * deltaTime;
            
            // Buffered jump + coyote (using GroundProbe service)
            if (groundProbe != null && groundProbe.CanJump())
            {
                if (onSpring && !autoJump)
                {
                    velocity.y = SpringJumpVelocity;
                }
                else
                {
                    velocity.y = JumpVelocity;
                }
                groundProbe.ConsumeJump();
                planeMotion?.ResetLandingSlide();
            }
            jumpQueued = false;
            
            rb.linearVelocity = velocity;
            lastVelY = velocity.y;
        }
        
        public void UpdateLandingSlide(bool isGrounded, PlaneMotion planeMotion, float deltaTime)
        {
            // Update landing slide logic
            planeMotion?.UpdateLandingSlide(isGrounded, lastVelY, landingMinFallSpeed, deltaTime);
        }
    }
}