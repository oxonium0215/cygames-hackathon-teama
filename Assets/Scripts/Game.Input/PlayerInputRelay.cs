using UnityEngine;
using UnityEngine.InputSystem;
using Game.Player;
using Game.Projection;

namespace Game.Input
{
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
            playerInput?.ClearTransient();
            
            bool currentlySuppress = perspective != null && perspective.IsSwitching && perspective.JumpOnlyDuringSwitch;
            if (wasInputSuppressed && !currentlySuppress && playerInput != null)
            {
                motor?.SetMove(playerInput.Move);
            }
            wasInputSuppressed = currentlySuppress;
        }

        public void OnMove(InputAction.CallbackContext ctx)
        {
            Vector2 moveValue = ctx.ReadValue<Vector2>();
            
            playerInput?.SetMove(moveValue);
            
            if (perspective != null && perspective.IsSwitching && perspective.JumpOnlyDuringSwitch)
            {
                return;
            }
            
            motor?.SetMove(moveValue);
        }


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