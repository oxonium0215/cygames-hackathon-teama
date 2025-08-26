using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Pure C# implementation of ground detection and jump timing logic.
    /// </summary>
    public class GroundProbe
    {
        private readonly float coyoteTime;
        private float lastGroundedTimer;
        private float jumpBufferTimer;

        public bool IsGrounded { get; private set; }
        public bool OnSpring { get; set; }

        public GroundProbe(float coyoteTime)
        {
            this.coyoteTime = coyoteTime;
        }

        public void UpdateGroundCheck(Vector3 groundCheckPosition, float groundCheckRadius, LayerMask groundMask, LayerMask springMask, float deltaTime)
        {
            OnSpring = Physics.CheckSphere(groundCheckPosition, groundCheckRadius, springMask, QueryTriggerInteraction.Ignore);
            if (OnSpring)
            {
                IsGrounded = true;
            }
            else
            {
                IsGrounded = Physics.CheckSphere(groundCheckPosition, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
            }

            if (IsGrounded)
                lastGroundedTimer = coyoteTime;
            else
                lastGroundedTimer -= deltaTime;

            jumpBufferTimer -= deltaTime;
        }

        public bool CanJump()
        {
            return jumpBufferTimer > 0f && lastGroundedTimer > 0f;
        }

        public void SetJumpBuffer(float bufferTime)
        {
            jumpBufferTimer = bufferTime;
        }

        public void ConsumeJump()
        {
            jumpBufferTimer = 0f;
            lastGroundedTimer = 0f;
        }

        public float GetJumpBufferTime()
        {
            return jumpBufferTimer;
        }
    }
}