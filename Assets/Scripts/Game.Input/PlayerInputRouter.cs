using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Scripting.APIUpdating;
using Game.Player;
using Game.Perspective;

namespace Game.Input
{
    /// <summary>
    /// Simplified input router that delegates to appropriate systems.
    /// Focused purely on input routing without complex logic.
    /// </summary>
    [MovedFrom("POC.Input")]
    public class PlayerInputRouter : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private ViewSwitchCoordinator viewSwitchCoordinator;

        [Header("Legacy Support")]
        [SerializeField] private PerspectiveProjectionManager legacyPerspectiveManager;

        private void Awake()
        {
            // Auto-find components if not assigned
            if (!motor) motor = FindFirstObjectByType<PlayerMotor>();
            if (!viewSwitchCoordinator) viewSwitchCoordinator = FindFirstObjectByType<ViewSwitchCoordinator>();
            if (!legacyPerspectiveManager) legacyPerspectiveManager = FindFirstObjectByType<PerspectiveProjectionManager>();
        }

        public void OnMove(InputAction.CallbackContext ctx)
        {
            motor?.SetMove(ctx.ReadValue<Vector2>());
        }

        public void OnJump(InputAction.CallbackContext ctx)
        {
            if (ctx.performed) motor?.QueueJump();
            else if (ctx.canceled) motor?.JumpCanceled();
        }

        public void OnSwitchView(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;

            // Prefer new ViewSwitchCoordinator over legacy system
            if (viewSwitchCoordinator)
            {
                viewSwitchCoordinator.TogglePerspective();
            }
            else if (legacyPerspectiveManager)
            {
                legacyPerspectiveManager.TogglePerspective();
            }
            else
            {
                Debug.LogWarning("[PlayerInputRouter] No view switching system found. Please assign ViewSwitchCoordinator or PerspectiveProjectionManager.");
            }
        }
    }
}