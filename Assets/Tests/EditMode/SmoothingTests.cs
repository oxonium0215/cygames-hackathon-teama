using NUnit.Framework;
using UnityEngine;
using Game.Camera;

namespace EditModeTests.Camera
{
    public class SmoothingTests
    {
        private const float DeltaTime = 0.016f; // ~60fps
        private const float Tolerance = 0.01f;

        [Test]
        public void SmoothDampSmoothing_MovesTowardsTarget()
        {
            var smoothing = new SmoothDampSmoothing(0.15f, 30f);
            
            float currentY = 0f;
            float desiredY = 10f;
            
            float newY = smoothing.ComputeNewY(currentY, desiredY, DeltaTime);
            
            // Should move towards the target
            Assert.That(newY, Is.GreaterThan(currentY));
            Assert.That(newY, Is.LessThan(desiredY));
        }

        [Test]
        public void SmoothDampSmoothing_RespectsMaxSpeed()
        {
            var smoothing = new SmoothDampSmoothing(0.01f, 5f); // Very responsive, but capped speed
            
            float currentY = 0f;
            float desiredY = 100f; // Far target
            
            float newY = smoothing.ComputeNewY(currentY, desiredY, DeltaTime);
            float speed = (newY - currentY) / DeltaTime;
            
            // Speed should not exceed maxSpeed (with tolerance)
            Assert.That(speed, Is.LessThanOrEqualTo(5f + Tolerance));
        }

        [Test]
        public void ConstantSpeedSmoothing_MovesTowardsTarget()
        {
            var smoothing = new ConstantSpeedSmoothing(10f);
            
            float currentY = 0f;
            float desiredY = 5f;
            
            float newY = smoothing.ComputeNewY(currentY, desiredY, DeltaTime);
            
            // Should move towards the target
            Assert.That(newY, Is.GreaterThan(currentY));
            Assert.That(newY, Is.LessThan(desiredY));
        }

        [Test]
        public void ConstantSpeedSmoothing_CorrectSpeed()
        {
            var smoothing = new ConstantSpeedSmoothing(10f);
            
            float currentY = 0f;
            float desiredY = 100f; // Far target
            
            float newY = smoothing.ComputeNewY(currentY, desiredY, DeltaTime);
            float expectedDelta = 10f * DeltaTime;
            
            // Should move exactly by speed * deltaTime
            Assert.That(newY - currentY, Is.EqualTo(expectedDelta).Within(Tolerance));
        }

        [Test]
        public void ConstantSpeedSmoothing_ReachesTarget()
        {
            var smoothing = new ConstantSpeedSmoothing(10f);
            
            float currentY = 0f;
            float desiredY = 0.1f; // Close target
            
            float newY = smoothing.ComputeNewY(currentY, desiredY, DeltaTime);
            
            // Should reach exactly the target when close enough
            Assert.That(newY, Is.EqualTo(desiredY).Within(Tolerance));
        }

        [Test]
        public void SmoothDampSmoothing_NoOvershooting()
        {
            var smoothing = new SmoothDampSmoothing(0.15f, 30f);
            
            float currentY = 10f;
            float desiredY = 8f; // Decreasing target (potential for overshoot)
            
            float newY = smoothing.ComputeNewY(currentY, desiredY, DeltaTime);
            
            // With neverScrollDown logic, caller would enforce Mathf.Max(newY, currentY)
            // This test just verifies the smoothing itself doesn't go too far beyond desired
            Assert.That(newY, Is.LessThanOrEqualTo(currentY));
            Assert.That(newY, Is.GreaterThanOrEqualTo(desiredY - 1f)); // Allow some reasonable range
        }
    }
}