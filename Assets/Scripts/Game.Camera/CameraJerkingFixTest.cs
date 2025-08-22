using UnityEngine;
using Game.Camera;
using Game.Projection;

namespace Game.Camera
{
    /// <summary>
    /// Test script to validate the camera jerking fix.
    /// Attach this to any GameObject in the scene to test suspension coordination.
    /// </summary>
    public class CameraJerkingFixTest : MonoBehaviour
    {
        [Header("Test References")]
        [SerializeField] private VerticalCameraFollow cameraFollow;
        [SerializeField] private PerspectiveProjectionManager projectionManager;
        [SerializeField] private Transform testPlayer;
        
        [Header("Test Settings")]
        [SerializeField] private bool logTestResults = true;
        [SerializeField] private bool runAutomaticTest = false;
        [SerializeField] private float testDuration = 5f;
        
        private bool testRunning = false;
        
        private void Start()
        {
            // Auto-find components if not assigned
            if (!cameraFollow) cameraFollow = FindObjectOfType<VerticalCameraFollow>();
            if (!projectionManager) projectionManager = FindObjectOfType<PerspectiveProjectionManager>();
            
            if (runAutomaticTest)
            {
                StartCoroutine(AutomaticTest());
            }
        }
        
        [ContextMenu("Test Suspension Mechanism")]
        public void TestSuspensionMechanism()
        {
            if (!cameraFollow)
            {
                LogResult("✗ VerticalCameraFollow not found");
                return;
            }
            
            LogResult("Testing camera follow suspension mechanism...");
            
            // Test initial state
            bool initialState = cameraFollow.IsSuspended();
            LogResult($"Initial suspension state: {initialState}");
            
            // Test suspend
            cameraFollow.SuspendFollowing();
            bool suspendedState = cameraFollow.IsSuspended();
            LogResult($"After suspend: {suspendedState}");
            
            if (suspendedState)
            {
                LogResult("✓ Suspension mechanism working correctly");
            }
            else
            {
                LogResult("✗ Suspension mechanism failed");
            }
            
            // Test resume
            cameraFollow.ResumeFollowing();
            bool resumedState = cameraFollow.IsSuspended();
            LogResult($"After resume: {resumedState}");
            
            if (!resumedState)
            {
                LogResult("✓ Resume mechanism working correctly");
            }
            else
            {
                LogResult("✗ Resume mechanism failed");
            }
        }
        
        [ContextMenu("Test During Perspective Switch")]
        public void TestDuringPerspectiveSwitch()
        {
            if (!projectionManager)
            {
                LogResult("✗ PerspectiveProjectionManager not found");
                return;
            }
            
            if (!cameraFollow)
            {
                LogResult("✗ VerticalCameraFollow not found");
                return;
            }
            
            LogResult("Testing camera behavior during perspective switch...");
            
            // Monitor suspension state during perspective switch
            StartCoroutine(MonitorDuringSwitch());
            
            // Trigger perspective switch
            projectionManager.TogglePerspective();
        }
        
        private System.Collections.IEnumerator MonitorDuringSwitch()
        {
            float startTime = Time.time;
            bool wasEverSuspended = false;
            bool wasResumedAfter = false;
            
            LogResult("Monitoring camera suspension during perspective switch...");
            
            // Monitor for a few seconds
            while (Time.time - startTime < 3f)
            {
                bool isSuspended = cameraFollow.IsSuspended();
                bool isSwitching = projectionManager.IsSwitching;
                
                if (isSuspended && isSwitching)
                {
                    wasEverSuspended = true;
                    LogResult("✓ Camera follow suspended during perspective switch");
                }
                
                if (!isSwitching && !isSuspended && wasEverSuspended)
                {
                    wasResumedAfter = true;
                    LogResult("✓ Camera follow resumed after perspective switch");
                    break;
                }
                
                yield return null;
            }
            
            if (wasEverSuspended && wasResumedAfter)
            {
                LogResult("✓ Camera coordination working correctly during perspective switches");
            }
            else if (!wasEverSuspended)
            {
                LogResult("⚠ Camera was not suspended during perspective switch (may be normal if switch was very fast)");
            }
            else if (!wasResumedAfter)
            {
                LogResult("✗ Camera was not resumed after perspective switch");
            }
        }
        
        private System.Collections.IEnumerator AutomaticTest()
        {
            testRunning = true;
            LogResult("Starting automatic camera jerking fix test...");
            
            yield return new WaitForSeconds(1f);
            
            // Test 1: Basic suspension mechanism
            TestSuspensionMechanism();
            yield return new WaitForSeconds(2f);
            
            // Test 2: During perspective switch
            TestDuringPerspectiveSwitch();
            yield return new WaitForSeconds(3f);
            
            // Test 3: Multiple rapid switches (stress test)
            LogResult("Stress testing with rapid perspective switches...");
            for (int i = 0; i < 3; i++)
            {
                if (projectionManager && !projectionManager.IsSwitching)
                {
                    projectionManager.TogglePerspective();
                    yield return new WaitForSeconds(1f);
                }
            }
            
            LogResult("Automatic test completed!");
            testRunning = false;
        }
        
        private void Update()
        {
            // Manual trigger for testing
            if (Input.GetKeyDown(KeyCode.F1))
            {
                TestSuspensionMechanism();
            }
            
            if (Input.GetKeyDown(KeyCode.F2))
            {
                TestDuringPerspectiveSwitch();
            }
        }
        
        private void LogResult(string message)
        {
            if (logTestResults)
            {
                Debug.Log($"[CameraJerkingFixTest] {message}");
            }
        }
        
        private void OnGUI()
        {
            if (testRunning) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.Label("Camera Jerking Fix Test", GUI.skin.box);
            
            if (GUILayout.Button("Test Suspension (F1)"))
            {
                TestSuspensionMechanism();
            }
            
            if (GUILayout.Button("Test During Switch (F2)"))
            {
                TestDuringPerspectiveSwitch();
            }
            
            if (cameraFollow)
            {
                GUILayout.Label($"Camera Suspended: {cameraFollow.IsSuspended()}");
            }
            
            if (projectionManager)
            {
                GUILayout.Label($"Perspective Switching: {projectionManager.IsSwitching}");
            }
            
            GUILayout.EndArea();
        }
    }
}