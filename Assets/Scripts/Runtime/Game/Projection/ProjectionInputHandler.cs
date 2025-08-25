using UnityEngine;
using Game.Core;

namespace Game.Projection
{
    /// <summary>
    /// Handles input events for projection switching functionality.
    /// This component subscribes to the global input event system to maintain decoupling.
    /// </summary>
    public class ProjectionInputHandler : MonoBehaviour
    {
        [Tooltip("PerspectiveProjectionManager used for view switching.")]
        [SerializeField] private PerspectiveProjectionManager perspectiveManager;

        private void Awake()
        {
            // Subscribe to input events
            GameInputEvents.OnSwitchViewPressed += HandleSwitchViewPressed;
        }

        private void OnDestroy()
        {
            // Unsubscribe from input events
            GameInputEvents.OnSwitchViewPressed -= HandleSwitchViewPressed;
        }

        private void HandleSwitchViewPressed()
        {
            perspectiveManager?.TogglePerspective();
        }
    }
}