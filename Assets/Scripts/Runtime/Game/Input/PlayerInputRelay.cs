using UnityEngine;
using UnityEngine.InputSystem;
using Game.Core;

namespace Game.Input
{
    // Routes UnityEvents from PlayerInput to the decoupled event system
    public class PlayerInputRelay : MonoBehaviour
    {
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
            
            // Invoke event for gameplay systems to handle
            GameInputEvents.InvokeMoveInput(moveValue);
        }

        // Handles both performed and canceled so jump-cut works
        public void OnJump(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
            {
                playerInput?.OnJumpPerformed();
                GameInputEvents.InvokeJumpPressed();
            }
            else if (ctx.canceled)
            {
                playerInput?.OnJumpCanceled();
                GameInputEvents.InvokeJumpReleased();
            }
        }

        public void OnSwitchView(InputAction.CallbackContext ctx)
        {
            if (ctx.performed) 
                GameInputEvents.InvokeSwitchViewPressed();
        }

        public void OnPreview3D(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
            {
                GameInputEvents.InvokePreview3DStarted();
            }
            else if (ctx.canceled)
            {
                GameInputEvents.InvokePreview3DEnded();
            }
        }
    }
}
