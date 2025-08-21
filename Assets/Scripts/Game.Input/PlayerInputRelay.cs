using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Scripting.APIUpdating;
using Game.Player;
using Game.Projection;

namespace Game.Input
{
    // Routes UnityEvents from PlayerInput to gameplay components
    [MovedFrom(true, sourceNamespace: "POC.Input", sourceClassName: "PlayerInputRouter")]
    public class PlayerInputRelay : MonoBehaviour
    {
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PerspectiveProjectionManager perspective;

        private UnityPlayerInput playerInput;

        private void Awake()
        {
            playerInput = new UnityPlayerInput();
        }

        private void Update()
        {
            // Clear transient flags once per frame
            playerInput?.ClearTransient();
        }

        public void OnMove(InputAction.CallbackContext ctx)
        {
            Vector2 moveValue = ctx.ReadValue<Vector2>();
            playerInput?.SetMove(moveValue);
            motor?.SetMove(moveValue);
        }

        // Handles both performed and canceled so jump-cut works
        public void OnJump(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
            {
                playerInput?.OnJumpPerformed();
                motor?.QueueJump();
            }
            else if (ctx.canceled)
            {
                playerInput?.OnJumpCanceled();
                motor?.JumpCanceled();
            }
        }

        public void OnSwitchView(InputAction.CallbackContext ctx)
        {
            if (ctx.performed) perspective?.TogglePerspective();
        }
    }
}