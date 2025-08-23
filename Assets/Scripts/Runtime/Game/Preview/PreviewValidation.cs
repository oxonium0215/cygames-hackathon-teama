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

        private void DoValidation()
        {
            if (!previewManager) return;

            // Count preview objects
            GameObject[] allPreviews = GameObject.FindObjectsOfType<GameObject>();
            previewCount = 0;
            GameObject foundPreview = null;

            foreach (var obj in allPreviews)
            {
                if (obj.name.Contains("Player_Preview"))
                {
                    previewCount++;
                    foundPreview = obj;
                }
            }

            // Validate position if we found exactly one preview
            if (previewCount == 1 && foundPreview != null)
            {
                actualPosition = foundPreview.transform.position;
                positionCorrect = Vector3.Distance(expectedPosition, actualPosition) < 0.1f;
            }

            hasValidated = true;

            // Log results
            string result = $"Preview Validation Results:\n" +
                           $"- Preview Count: {previewCount} (expected: 1)\n" +
                           $"- Position Correct: {positionCorrect}\n" +
                           $"- Expected Position: {expectedPosition}\n" +
                           $"- Actual Position: {actualPosition}";

            if (previewCount == 1 && positionCorrect)
            {
                Debug.Log($"✓ VALIDATION PASSED\n{result}");
            }
            else
            {
                Debug.LogError($"✗ VALIDATION FAILED\n{result}");
            }

            // Clean up
            previewManager.EndPreview();
        }
    }
}