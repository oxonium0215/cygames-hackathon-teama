using NUnit.Framework;
using Game.Camera;

namespace EditModeTests.Camera
{
    public class DeadZoneTests
    {
        [Test]
        public void TopDeadZonePolicy_ComputeThreshold_CorrectCalculation()
        {
            var policy = new TopDeadZonePolicy(3.0f);
            
            float pivotY = 10f;
            float thresholdY = policy.ComputeThreshold(pivotY);
            
            Assert.That(thresholdY, Is.EqualTo(13f));
        }

        [Test]
        public void TopDeadZonePolicy_ComputeDesiredY_CorrectCalculation()
        {
            var policy = new TopDeadZonePolicy(3.0f);
            
            float playerY = 15f;
            float desiredY = policy.ComputeDesiredY(playerY);
            
            // Desired Y should keep player exactly at top edge of dead zone
            Assert.That(desiredY, Is.EqualTo(12f));
        }

        [Test]
        public void TopDeadZonePolicy_ZeroDeadZone_WorksCorrectly()
        {
            var policy = new TopDeadZonePolicy(0f);
            
            float pivotY = 5f;
            float playerY = 5f;
            
            Assert.That(policy.ComputeThreshold(pivotY), Is.EqualTo(5f));
            Assert.That(policy.ComputeDesiredY(playerY), Is.EqualTo(5f));
        }

        [Test]
        public void TopDeadZonePolicy_LargeDeadZone_WorksCorrectly()
        {
            var policy = new TopDeadZonePolicy(50f);
            
            float pivotY = 100f;
            float playerY = 200f;
            
            Assert.That(policy.ComputeThreshold(pivotY), Is.EqualTo(150f));
            Assert.That(policy.ComputeDesiredY(playerY), Is.EqualTo(150f));
        }

        [Test]
        public void TopDeadZonePolicy_NegativeValues_WorksCorrectly()
        {
            var policy = new TopDeadZonePolicy(2f);
            
            float pivotY = -10f;
            float playerY = -5f;
            
            Assert.That(policy.ComputeThreshold(pivotY), Is.EqualTo(-8f));
            Assert.That(policy.ComputeDesiredY(playerY), Is.EqualTo(-7f));
        }
    }
}