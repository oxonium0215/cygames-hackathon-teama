using UnityEngine;
using Game.Core;

namespace Game.Preview
{
    /// <summary>
    /// Handles input events for stage preview functionality.
    /// This component subscribes to the global input event system to maintain decoupling.
    /// </summary>
    public class PreviewInputHandler : MonoBehaviour
    {
        [Tooltip("StagePreviewManager for 3D preview functionality.")]
        [SerializeField] private StagePreviewManager stagePreview;
        [Tooltip("Optional input suppressor to check if preview is blocked.")]
        [SerializeField] private MonoBehaviour inputSuppressorComponent;

        private IInputSuppressor inputSuppressor;

        private void Awake()
        {
            // Subscribe to input events
            GameInputEvents.OnPreview3DStarted += HandlePreview3DStarted;
            GameInputEvents.OnPreview3DEnded += HandlePreview3DEnded;

            // Get input suppressor interface if available
            if (inputSuppressorComponent != null)
                inputSuppressor = inputSuppressorComponent as IInputSuppressor;
        }

        private void OnDestroy()
        {
            // Unsubscribe from input events
            GameInputEvents.OnPreview3DStarted -= HandlePreview3DStarted;
            GameInputEvents.OnPreview3DEnded -= HandlePreview3DEnded;
        }

        private void HandlePreview3DStarted()
        {
            if (!stagePreview) return;
            
            // Prevent preview if input is suppressed (e.g., during viewpoint changes)
            if (inputSuppressor?.IsInputSuppressed ?? false) return;

            stagePreview.StartPreview();
        }

        private void HandlePreview3DEnded()
        {
            stagePreview?.EndPreview();
        }
    }
}