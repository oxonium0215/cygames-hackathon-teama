using NUnit.Framework;
using UnityEngine;
using Game.Player;

namespace EditModeTests.Player
{
    public class PlaneMotionTests
    {
        private PlaneMotion planeMotion;
        private const bool EnableLandingSlide = true;
        private const float LandingSlideDuration = 0.18f;
        private const float LandingAccelMultiplier = 0.7f;
        private const float LandingDecelMultiplier = 0.35f;
        
        [SetUp]
        public void Setup()
        {
            planeMotion = new PlaneMotion(EnableLandingSlide, LandingSlideDuration, 
                LandingAccelMultiplier, LandingDecelMultiplier);
        }
        
        [Test]
        public void PlaneMotion_ApplyLateralMovement_XPlane_CorrectProjection()
        {
            Vector2 input = new Vector2(1f, 0f); // Full right input
            MovePlane plane = MovePlane.X;
            float maxSpeed = 7f;
            Vector3 currentVelocity = Vector3.zero;
            float groundAccel = 80f;
            float deltaTime = 0.016f;
            bool lateralEnabled = true;
            
            Vector3 result = planeMotion.ApplyLateralMovement(input, plane, maxSpeed, currentVelocity, 
                true, groundAccel, 40f, 60f, 20f, deltaTime, lateralEnabled);
            
            // Should apply movement to X axis for X plane
            Assert.That(result.x, Is.GreaterThan(0f));
            Assert.AreEqual(0f, result.z); // Z should remain unchanged
            Assert.AreEqual(0f, result.y); // Y should remain unchanged
        }
        
        [Test]
        public void PlaneMotion_ApplyLateralMovement_ZPlane_CorrectProjection()
        {
            Vector2 input = new Vector2(1f, 0f); // Full right input  
            MovePlane plane = MovePlane.Z;
            float maxSpeed = 7f;
            Vector3 currentVelocity = Vector3.zero;
            float groundAccel = 80f;
            float deltaTime = 0.016f;
            bool lateralEnabled = true;
            
            Vector3 result = planeMotion.ApplyLateralMovement(input, plane, maxSpeed, currentVelocity, 
                true, groundAccel, 40f, 60f, 20f, deltaTime, lateralEnabled);
            
            // Should apply movement to Z axis for Z plane
            Assert.That(result.z, Is.GreaterThan(0f));
            Assert.AreEqual(0f, result.x); // X should remain unchanged
            Assert.AreEqual(0f, result.y); // Y should remain unchanged
        }
        
        [Test]
        public void PlaneMotion_ApplyLateralMovement_LateralDisabled_NoMovement()
        {
            Vector2 input = new Vector2(1f, 0f); // Full right input
            MovePlane plane = MovePlane.X;
            float maxSpeed = 7f;
            Vector3 currentVelocity = Vector3.zero;
            float groundAccel = 80f;
            float deltaTime = 0.016f;
            bool lateralEnabled = false; // Disabled
            
            Vector3 result = planeMotion.ApplyLateralMovement(input, plane, maxSpeed, currentVelocity, 
                true, groundAccel, 40f, 60f, 20f, deltaTime, lateralEnabled);
            
            // Should not apply any lateral movement when disabled
            Assert.AreEqual(0f, result.x);
            Assert.AreEqual(0f, result.z);
        }
        
        [Test]
        public void PlaneMotion_GroundedVsAirborne_DifferentAcceleration()
        {
            Vector2 input = new Vector2(1f, 0f);
            MovePlane plane = MovePlane.X;
            float maxSpeed = 7f;
            Vector3 currentVelocity = Vector3.zero;
            float groundAccel = 80f;
            float airAccel = 40f;
            float deltaTime = 0.016f;
            bool lateralEnabled = true;
            
            // Test grounded movement
            Vector3 groundedResult = planeMotion.ApplyLateralMovement(input, plane, maxSpeed, currentVelocity, 
                true, groundAccel, airAccel, 60f, 20f, deltaTime, lateralEnabled);
            
            // Test airborne movement
            Vector3 airborneResult = planeMotion.ApplyLateralMovement(input, plane, maxSpeed, currentVelocity, 
                false, groundAccel, airAccel, 60f, 20f, deltaTime, lateralEnabled);
            
            // Grounded should accelerate faster than airborne
            Assert.That(groundedResult.x, Is.GreaterThan(airborneResult.x));
        }
        
        [Test]
        public void PlaneMotion_UpdateLandingSlide_TriggersOnFastLanding()
        {
            bool isGrounded = true;
            float lastVelY = -3f; // Fast downward velocity
            float landingMinFallSpeed = 2f;
            float deltaTime = 0.016f;
            
            planeMotion.UpdateLandingSlide(isGrounded, lastVelY, landingMinFallSpeed, deltaTime);
            
            // Should be in landing slide state now
            var (accelMult, decelMult) = planeMotion.GetLandingSlideMultipliers(true);
            Assert.AreEqual(LandingAccelMultiplier, accelMult);
            Assert.AreEqual(1f, decelMult); // Decel multiplier only applies when hasInput is false
        }
        
        [Test]
        public void PlaneMotion_GetLandingSlideMultipliers_NoInput_ReturnsDecelMultiplier()
        {
            // Trigger landing slide first
            planeMotion.UpdateLandingSlide(true, -3f, 2f, 0.016f);
            
            var (accelMult, decelMult) = planeMotion.GetLandingSlideMultipliers(false);
            Assert.AreEqual(1f, accelMult);
            Assert.AreEqual(LandingDecelMultiplier, decelMult);
        }
        
        [Test]
        public void PlaneMotion_ResetLandingSlide_ClearsSlideState()
        {
            // Trigger landing slide
            planeMotion.UpdateLandingSlide(true, -3f, 2f, 0.016f);
            
            // Verify slide is active
            var (accelMult, decelMult) = planeMotion.GetLandingSlideMultipliers(true);
            Assert.AreEqual(LandingAccelMultiplier, accelMult);
            
            // Reset slide
            planeMotion.ResetLandingSlide();
            
            // Verify slide is inactive
            var (accelMultAfter, decelMultAfter) = planeMotion.GetLandingSlideMultipliers(true);
            Assert.AreEqual(1f, accelMultAfter);
            Assert.AreEqual(1f, decelMultAfter);
        }
    }
}