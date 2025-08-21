using UnityEngine;

namespace Game.Player
{
    [System.Serializable]
    public class JumpLogic
    {
        [Header("Jumping")]
        [SerializeField] private float jumpHeight = 3.5f;
        [Range(0.1f, 1f)][SerializeField] private float jumpCutMultiplier = 0.5f;
        [SerializeField] private float coyoteTime = 0.1f;
        [SerializeField] private float jumpBufferTime = 0.1f;

        [Header("Landing Slide")]
        [SerializeField] private bool enableLandingSlide = true;
        [SerializeField] private float landingSlideDuration = 0.18f;
        [SerializeField] private float landingMinFallSpeed = 2.0f;
        [Range(0.1f, 1f)][SerializeField] private float landingDecelMultiplier = 0.35f;
        [Range(0.3f, 1f)][SerializeField] private float landingAccelMultiplier = 0.7f;

        // Internal state
        private bool jumpQueued;
        private bool jumpHeld;
        private float lastGroundedTimer;
        private float jumpBufferTimer;
        private float landingSlideTimer;
        private float lastVelY;

        public float JumpVelocity(float gravity) => Mathf.Sqrt(2f * Mathf.Abs(gravity) * Mathf.Max(0.0001f, jumpHeight));

        public void QueueJump(bool rotationFrozen)
        {
            if (rotationFrozen) return;
            jumpQueued = true;
            jumpHeld = true;
            jumpBufferTimer = jumpBufferTime;
        }

        public void CancelJump(Rigidbody rb, bool rotationFrozen)
        {
            if (rotationFrozen) return;
            var v = rb.linearVelocity;
            if (v.y > 0f)
            {
                v.y *= jumpCutMultiplier;
                rb.linearVelocity = v;
            }
        }

        public void UpdateTimers(bool isGrounded, bool rotationFrozen)
        {
            if (rotationFrozen) return;

            if (isGrounded) 
                lastGroundedTimer = coyoteTime;
            else 
                lastGroundedTimer -= Time.deltaTime;

            if (!jumpQueued) 
                jumpBufferTimer -= Time.deltaTime;
        }

        public void HandleLandingSlide(bool isGrounded, float currentVelY)
        {
            // Landing slide trigger
            if (enableLandingSlide && isGrounded && lastVelY < 0f && Mathf.Abs(lastVelY) >= landingMinFallSpeed)
                landingSlideTimer = landingSlideDuration;

            lastVelY = currentVelY;
        }

        public bool TryConsumeJump(float gravity, out float jumpVelocity)
        {
            jumpVelocity = 0f;
            
            // Buffered jump + coyote
            if (jumpBufferTimer > 0f && lastGroundedTimer > 0f)
            {
                jumpVelocity = JumpVelocity(gravity);
                jumpBufferTimer = 0f;
                lastGroundedTimer = 0f;
                landingSlideTimer = 0f;
                jumpQueued = false;
                return true;
            }
            
            jumpQueued = false;
            return false;
        }

        public void ModifyLateralResponse(bool isGrounded, bool hasInput, ref float response)
        {
            if (enableLandingSlide && isGrounded && landingSlideTimer > 0f)
            {
                response *= hasInput ? landingAccelMultiplier : landingDecelMultiplier;
                landingSlideTimer -= Time.fixedDeltaTime;
            }
        }

        public void ValidateParameters()
        {
            jumpHeight = Mathf.Max(0.0001f, jumpHeight);
        }
    }
}