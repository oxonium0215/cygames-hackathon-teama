using UnityEngine;
using UnityEngine.InputSystem;
using Game.Player;
using Game.Projection;
using Game.Preview;

namespace Game.Input
{
    // Routes UnityEvents from PlayerInput to gameplay components
    public class PlayerInputRelay : MonoBehaviour
    {
        [Tooltip("PlayerMotor component that receives movement and jump commands.")]
        [SerializeField] private PlayerMotor motor;
        [Tooltip("PerspectiveProjectionManager used to check if perspective switching is active.")]
        [SerializeField] private PerspectiveProjectionManager perspective;
        [Tooltip("StagePreviewManager for 3D preview functionality.")]
        [SerializeField] private StagePreviewManager stagePreview;

        private UnityPlayerInput playerInput;
        private bool wasInputSuppressed;

        private void Awake()
        {
            playerInput = new UnityPlayerInput();
        }

        private void Update()
        {
            // Clear transient flags once per frame
            playerInput?.ClearTransient();
            
            // Check if input suppression has ended and forward any held input
            bool currentlySuppress = perspective != null && perspective.IsSwitching && perspective.JumpOnlyDuringSwitch;
            if (wasInputSuppressed && !currentlySuppress && playerInput != null)
            {
                // Suppression just ended, forward any currently held input
                motor?.SetMove(playerInput.Move);
            }
            wasInputSuppressed = currentlySuppress;
        }

        public void OnMove(InputAction.CallbackContext ctx)
        {
            Vector2 moveValue = ctx.ReadValue<Vector2>();
            
            // Update adapter state regardless of suppression
            playerInput?.SetMove(moveValue);
            
            // Suppress lateral input during perspective switching if jump-only mode is enabled
            if (perspective != null && perspective.IsSwitching && perspective.JumpOnlyDuringSwitch)
            {
                return; // Skip forwarding to motor, but jump input remains unaffected
            }
            
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
            if (ctx.performed)
            {
                // Prevent viewpoint changes during preview
                if (stagePreview != null && stagePreview.IsPreviewActive) return;
                
                perspective?.TogglePerspective();
            }
        }

        public void OnPreview3D(InputAction.CallbackContext ctx)
        {
            if (!stagePreview) return;
            
            // Prevent preview during viewpoint changes
            if (perspective != null && perspective.IsSwitching) return;

            if (ctx.performed)
            {
                stagePreview.StartPreview();
            }
            else if (ctx.canceled)
            {
                stagePreview.EndPreview();
            }
        }
    }
}
