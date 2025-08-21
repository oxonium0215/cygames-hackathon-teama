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

        public void OnMove(InputAction.CallbackContext ctx)
        {
            motor?.SetMove(ctx.ReadValue<Vector2>());
        }

        // Handles both performed and canceled so jump-cut works
        public void OnJump(InputAction.CallbackContext ctx)
        {
            if (ctx.performed) motor?.QueueJump();
            else if (ctx.canceled) motor?.JumpCanceled();
        }

        public void OnSwitchView(InputAction.CallbackContext ctx)
        {
            if (ctx.performed) perspective?.TogglePerspective();
        }
    }
}