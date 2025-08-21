using UnityEngine;
using UnityEngine.InputSystem;
using Game.Player;
using Game.Projection;

namespace Game.Input
{
    /// <summary>
    /// Pure C# implementation of player input that is updated via methods.
    /// Mirrors the current event-driven pattern while keeping logic simple.
    /// </summary>
    public class UnityPlayerInput
    {
        private Vector2 move;
        private bool jumpHeld;
        private bool jumpPressedThisFrame;
        
        public Vector2 Move => move;
        public bool JumpHeld => jumpHeld;
        public bool JumpPressedThisFrame => jumpPressedThisFrame;
        
        public void SetMove(Vector2 v)
        {
            move = v;
        }
        
        public void OnJumpPerformed()
        {
            jumpHeld = true;
            jumpPressedThisFrame = true;
        }
        
        public void OnJumpCanceled()
        {
            jumpHeld = false;
        }
        
        public void ClearTransient()
        {
            jumpPressedThisFrame = false;
        }
    }

namespace Game.Input
{
    // Routes UnityEvents from PlayerInput to gameplay components
    public class PlayerInputRelay : MonoBehaviour
    {
        [Tooltip("PlayerMotor component that receives movement and jump commands.")]
        [SerializeField] private PlayerMotor motor;
        [Tooltip("PerspectiveProjectionManager used to check if perspective switching is active.")]
        [SerializeField] private PerspectiveProjectionManager perspective;

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
            
            // Early-out: suppress lateral input during perspective switching if jump-only mode is enabled
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
                perspective?.TogglePerspective();
        }
    }
}