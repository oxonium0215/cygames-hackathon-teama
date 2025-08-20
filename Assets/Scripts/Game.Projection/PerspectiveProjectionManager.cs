using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Game.Perspective;

[MovedFrom("POC.GameplayProjection")]
namespace Game.Projection
{
    /// <summary>
    /// Simplified facade for perspective projection management.
    /// Delegates to the new modular perspective switching system.
    /// </summary>
    [MovedFrom("POC.GameplayProjection")]
    public class PerspectiveProjectionManager : MonoBehaviour
    {
        [Header("Legacy Support (Deprecated)")]
        [SerializeField] private ViewSwitchCoordinator viewSwitchCoordinator;

        [Header("Auto-Setup")]
        [Tooltip("Automatically create and configure ViewSwitchCoordinator if not assigned.")]
        [SerializeField] private bool autoSetup = true;

        private void Awake()
        {
            if (autoSetup && !viewSwitchCoordinator)
            {
                SetupViewSwitchCoordinator();
            }
        }

        /// <summary>
        /// Legacy method - delegates to ViewSwitchCoordinator
        /// </summary>
        public void TogglePerspective()
        {
            if (viewSwitchCoordinator)
                viewSwitchCoordinator.TogglePerspective();
            else
                Debug.LogWarning("[PerspectiveProjectionManager] ViewSwitchCoordinator not assigned. Cannot toggle perspective.");
        }

        private void SetupViewSwitchCoordinator()
        {
            // Try to find existing ViewSwitchCoordinator
            viewSwitchCoordinator = FindFirstObjectByType<ViewSwitchCoordinator>();
            
            if (!viewSwitchCoordinator)
            {
                Debug.LogWarning("[PerspectiveProjectionManager] No ViewSwitchCoordinator found. " +
                    "Please add a ViewSwitchCoordinator component to manage perspective switching. " +
                    "This component is deprecated - use ViewSwitchCoordinator directly.");
            }
        }

        private void OnValidate()
        {
            if (viewSwitchCoordinator == null)
            {
                Debug.LogWarning("[PerspectiveProjectionManager] This component is deprecated. " +
                    "Please use ViewSwitchCoordinator directly for better control and cleaner architecture.");
            }
        }
    }
}