using NUnit.Framework;
using UnityEngine;
using Game.Projection;

namespace EditModeTests.Projection
{
    public class ProjectionControllerTests
    {
        private ProjectionController controller;
        
        [SetUp]
        public void Setup()
        {
            controller = new ProjectionController();
        }
        
        [Test]
        public void ProjectionController_Initially_NotRotating()
        {
            Assert.IsFalse(controller.IsRotating);
        }
        
        [Test]
        public void ProjectionController_BeginSwitch_StartsRotating()
        {
            AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            
            controller.BeginSwitch(1, 0.3f, easeCurve);
            
            Assert.IsTrue(controller.IsRotating);
        }
        
        [Test]
        public void ProjectionController_UpdateRotation_ReturnsProgress()
        {
            AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            float duration = 1.0f;
            
            controller.BeginSwitch(1, duration, easeCurve);
            
            // Update with half the duration
            float progress = controller.UpdateRotation(0.5f);
            
            Assert.That(progress, Is.GreaterThan(0f));
            Assert.That(progress, Is.LessThan(1f));
            Assert.IsTrue(controller.IsRotating);
        }
        
        [Test]
        public void ProjectionController_UpdateRotation_CompletesWhenTimeExceeded()
        {
            AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            float duration = 0.5f;
            
            controller.BeginSwitch(1, duration, easeCurve);
            
            // Update with more than the duration
            float progress = controller.UpdateRotation(1.0f);
            
            Assert.AreEqual(1f, progress);
            Assert.IsFalse(controller.IsRotating);
        }
        
        [Test]
        public void ProjectionController_UpdateRotation_NotRotating_ReturnsNegative()
        {
            // Don't start any switch
            float progress = controller.UpdateRotation(0.1f);
            
            Assert.AreEqual(-1f, progress);
        }
        
        [Test]
        public void ProjectionController_CompleteSwitch_StopsRotating()
        {
            AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            
            controller.BeginSwitch(1, 1.0f, easeCurve);
            Assert.IsTrue(controller.IsRotating);
            
            controller.CompleteSwitch();
            Assert.IsFalse(controller.IsRotating);
        }
        
        [Test]
        public void ProjectionController_EaseCurve_AppliedCorrectly()
        {
            // Create a linear curve for predictable testing
            AnimationCurve linearCurve = AnimationCurve.Linear(0, 0, 1, 1);
            float duration = 1.0f;
            
            controller.BeginSwitch(1, duration, linearCurve);
            
            // At 25% time, should be approximately 25% progress with linear curve
            float progress = controller.UpdateRotation(0.25f);
            Assert.That(progress, Is.InRange(0.2f, 0.3f));
            
            // At 50% time, should be approximately 50% progress
            progress = controller.UpdateRotation(0.25f); // Total 0.5f
            Assert.That(progress, Is.InRange(0.45f, 0.55f));
        }
        
        [Test]
        public void ProjectionController_MultipleBeginSwitch_ResetsState()
        {
            AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            
            // Start first switch
            controller.BeginSwitch(1, 1.0f, easeCurve);
            controller.UpdateRotation(0.5f); // Advance partway
            
            // Start new switch - should reset
            controller.BeginSwitch(0, 0.5f, easeCurve);
            float progress = controller.UpdateRotation(0.1f);
            
            // Should be early in new rotation, not continuing from old one
            Assert.That(progress, Is.LessThan(0.5f));
            Assert.IsTrue(controller.IsRotating);
        }
    }
}