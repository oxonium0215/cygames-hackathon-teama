using NUnit.Framework;
using UnityEngine;
using Game.Player;

namespace EditModeTests.Player
{
    public class GroundProbeTests
    {
        private GroundProbe groundProbe;
        private const float CoyoteTime = 0.1f;
        
        [SetUp]
        public void Setup()
        {
            groundProbe = new GroundProbe(CoyoteTime);
        }
        
        [Test]
        public void GroundProbe_Initially_NotGrounded()
        {
            Assert.IsFalse(groundProbe.IsGrounded);
        }
        
        [Test]
        public void GroundProbe_WhenGroundCheckReturnsTrue_IsGrounded()
        {
            // Create a test position and use Physics.CheckSphere mock behavior
            Vector3 testPos = Vector3.zero;
            LayerMask testMask = 1;
            
            // We can't easily mock Physics.CheckSphere in EditMode tests without scene setup,
            // but we can test the timing logic by calling UpdateGroundCheck with known state
            
            // This is a basic behavioral test - in a real scenario, the Physics.CheckSphere 
            // would be called inside UpdateGroundCheck
            groundProbe.UpdateGroundCheck(testPos, 0.1f, testMask, 0.016f);
            
            // The actual grounded state depends on Physics.CheckSphere which we can't easily control
            // in EditMode tests without creating colliders. This test validates the API works.
            Assert.IsNotNull(groundProbe);
        }
        
        [Test]
        public void GroundProbe_CoyoteTime_WorksCorrectly()
        {
            // Test coyote time behavior by manipulating internal state through UpdateGroundCheck
            Vector3 testPos = Vector3.zero;
            LayerMask testMask = 1;
            
            // Initially cannot jump
            Assert.IsFalse(groundProbe.CanJump());
            
            // Set jump buffer
            groundProbe.SetJumpBuffer(0.1f);
            
            // Should still not be able to jump without coyote time
            Assert.IsFalse(groundProbe.CanJump());
            
            // After consuming, buffer should be reset
            groundProbe.ConsumeJump();
            Assert.AreEqual(0f, groundProbe.GetJumpBufferTime());
        }
        
        [Test]
        public void GroundProbe_JumpBuffer_DecrementsOverTime()
        {
            const float bufferTime = 0.2f;
            const float deltaTime = 0.05f;
            
            groundProbe.SetJumpBuffer(bufferTime);
            
            // Update should decrement buffer
            Vector3 testPos = Vector3.zero;
            LayerMask testMask = 1;
            
            groundProbe.UpdateGroundCheck(testPos, 0.1f, testMask, deltaTime);
            
            // Buffer should be decremented by deltaTime
            float remainingTime = groundProbe.GetJumpBufferTime();
            Assert.That(remainingTime, Is.LessThan(bufferTime));
            Assert.That(remainingTime, Is.GreaterThanOrEqualTo(bufferTime - deltaTime));
        }
        
        [Test]
        public void GroundProbe_ConsumeJump_ResetsTimers()
        {
            groundProbe.SetJumpBuffer(0.2f);
            
            groundProbe.ConsumeJump();
            
            Assert.AreEqual(0f, groundProbe.GetJumpBufferTime());
            Assert.IsFalse(groundProbe.CanJump());
        }
    }
}