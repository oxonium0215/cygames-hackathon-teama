using UnityEngine;
using Game.Camera;

namespace Game.Camera
{
    /// <summary>
    /// Simple test script to validate camera downward movement functionality.
    /// Attach this to any GameObject in the scene to run basic validation tests.
    /// </summary>
    public class CameraDownwardMovementTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool runTestsOnStart = true;
        [SerializeField] private bool logResults = true;

        private void Start()
        {
            if (runTestsOnStart)
            {
                RunTests();
            }
        }

        [ContextMenu("Run Camera Tests")]
        public void RunTests()
        {
            LogResult("Starting Camera Downward Movement Tests...");
            
            TestBottomDeadZonePolicy();
            TestTopDeadZonePolicy();
            TestPolicyConsistency();
            
            LogResult("Camera tests completed!");
        }

        private void TestBottomDeadZonePolicy()
        {
            LogResult("Testing BottomDeadZonePolicy...");
            
            var policy = new BottomDeadZonePolicy(3.0f);
            
            // Test threshold computation
            float pivotY = 10.0f;
            float threshold = policy.ComputeThreshold(pivotY);
            float expected = 7.0f; // pivotY - bottomDeadZone
            
            if (Mathf.Approximately(threshold, expected))
            {
                LogResult("✓ BottomDeadZonePolicy.ComputeThreshold passed");
            }
            else
            {
                LogResult($"✗ BottomDeadZonePolicy.ComputeThreshold failed. Expected: {expected}, Got: {threshold}");
            }
            
            // Test desired Y computation
            float playerY = 5.0f;
            float desiredY = policy.ComputeDesiredY(playerY);
            float expectedDesired = 8.0f; // playerY + bottomDeadZone
            
            if (Mathf.Approximately(desiredY, expectedDesired))
            {
                LogResult("✓ BottomDeadZonePolicy.ComputeDesiredY passed");
            }
            else
            {
                LogResult($"✗ BottomDeadZonePolicy.ComputeDesiredY failed. Expected: {expectedDesired}, Got: {desiredY}");
            }
        }

        private void TestTopDeadZonePolicy()
        {
            LogResult("Testing TopDeadZonePolicy (existing functionality)...");
            
            var policy = new TopDeadZonePolicy(3.0f);
            
            // Test threshold computation
            float pivotY = 10.0f;
            float threshold = policy.ComputeThreshold(pivotY);
            float expected = 13.0f; // pivotY + topDeadZone
            
            if (Mathf.Approximately(threshold, expected))
            {
                LogResult("✓ TopDeadZonePolicy.ComputeThreshold passed");
            }
            else
            {
                LogResult($"✗ TopDeadZonePolicy.ComputeThreshold failed. Expected: {expected}, Got: {threshold}");
            }
        }

        private void TestPolicyConsistency()
        {
            LogResult("Testing policy consistency...");
            
            float deadZoneSize = 3.0f;
            var topPolicy = new TopDeadZonePolicy(deadZoneSize);
            var bottomPolicy = new BottomDeadZonePolicy(deadZoneSize);
            
            float pivotY = 10.0f;
            float topThreshold = topPolicy.ComputeThreshold(pivotY);
            float bottomThreshold = bottomPolicy.ComputeThreshold(pivotY);
            
            // The distance between thresholds should be 2 * deadZoneSize
            float distance = topThreshold - bottomThreshold;
            float expectedDistance = 2.0f * deadZoneSize;
            
            if (Mathf.Approximately(distance, expectedDistance))
            {
                LogResult("✓ Dead zone policy consistency passed");
            }
            else
            {
                LogResult($"✗ Dead zone policy consistency failed. Expected distance: {expectedDistance}, Got: {distance}");
            }
        }

        private void LogResult(string message)
        {
            if (logResults)
            {
                Debug.Log($"[CameraTest] {message}");
            }
        }
    }
}