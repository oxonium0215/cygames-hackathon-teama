using UnityEngine;

namespace Game.Preview
{
    /// <summary>
    /// Simple validation script to verify preview behavior
    /// Attach to a GameObject in the scene to validate preview functionality
    /// </summary>
    public class PreviewValidation : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private StagePreviewManager previewManager;
        [SerializeField] private Transform player;
        
        [Header("Validation Results")]
        [SerializeField] private bool hasValidated;
        [SerializeField] private int previewCount;
        [SerializeField] private Vector3 expectedPosition;
        [SerializeField] private Vector3 actualPosition;
        [SerializeField] private bool positionCorrect;

        private void Start()
        {
            if (!previewManager) previewManager = FindFirstObjectByType<StagePreviewManager>();
            if (!player) player = GameObject.FindWithTag("Player")?.transform;
        }

        [ContextMenu("Validate Preview Behavior")]
        public void ValidatePreview()
        {
            if (!previewManager || !player)
            {
                Debug.LogError("Missing references for validation");
                return;
            }

            // Record initial position
            Vector3 initialPlayerPos = player.position;
            expectedPosition = new Vector3(-initialPlayerPos.z, initialPlayerPos.y, -initialPlayerPos.x);

            // Start preview
            previewManager.StartPreview();

            // Wait a frame and then validate
            Invoke(nameof(DoValidation), 1f);
        }
        
        [ContextMenu("Test Race Condition")]
        public void TestRaceCondition()
        {
            if (!previewManager || !player)
            {
                Debug.LogError("Missing references for validation");
                return;
            }

            Debug.Log("Testing race condition by calling StartPreview multiple times rapidly...");
            
            // Try to trigger the race condition by calling StartPreview multiple times in rapid succession
            previewManager.StartPreview();
            previewManager.StartPreview(); // This should be ignored
            previewManager.StartPreview(); // This should be ignored
            previewManager.StartPreview(); // This should be ignored

            // Wait and validate
            Invoke(nameof(DoValidation), 1f);
        }

        private void DoValidation()
        {
            if (!previewManager) return;

            // Count preview objects using different methods
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            int globalPreviewCount = 0;
            GameObject foundPreview = null;

            foreach (var obj in allObjects)
            {
                if (obj.name.Contains("Player_Preview"))
                {
                    globalPreviewCount++;
                    foundPreview = obj;
                    Debug.Log($"Found preview: {obj.name} at {obj.transform.position}");
                }
            }
            
            // Also check within the preview manager's children
            Transform[] children = previewManager.GetComponentsInChildren<Transform>(true);
            int childPreviewCount = 0;
            foreach (var child in children)
            {
                if (child.name.Contains("Player_Preview"))
                {
                    childPreviewCount++;
                    Debug.Log($"Found preview in manager children: {child.name} at {child.position}");
                }
            }

            previewCount = globalPreviewCount;

            // Validate position if we found exactly one preview
            if (previewCount == 1 && foundPreview != null)
            {
                actualPosition = foundPreview.transform.position;
                positionCorrect = Vector3.Distance(expectedPosition, actualPosition) < 0.1f;
            }

            hasValidated = true;

            // Log detailed results
            string result = $"Preview Validation Results:\n" +
                           $"- Global Preview Count: {globalPreviewCount} (expected: 1)\n" +
                           $"- Child Preview Count: {childPreviewCount} (expected: 1)\n" +
                           $"- Position Correct: {positionCorrect}\n" +
                           $"- Expected Position: {expectedPosition}\n" +
                           $"- Actual Position: {actualPosition}";

            if (globalPreviewCount == 1 && childPreviewCount == 1 && positionCorrect)
            {
                Debug.Log($"✓ VALIDATION PASSED\n{result}");
            }
            else
            {
                Debug.LogError($"✗ VALIDATION FAILED\n{result}");
                
                // Additional diagnostics
                if (globalPreviewCount > 1)
                {
                    Debug.LogError($"CRITICAL: Found {globalPreviewCount} player previews! This indicates the multiple preview bug still exists.");
                }
            }

            // Clean up
            previewManager.EndPreview();
        }
    }
}