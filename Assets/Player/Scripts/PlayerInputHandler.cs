using UnityEngine;
using Game.Core;

namespace Game.Player
{
    /// <summary>
    /// Handles input events for player gameplay functionality.
    /// This component subscribes to the global input event system to maintain decoupling.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        [Tooltip("PlayerMotor component that receives movement and jump commands.")]
        [SerializeField] private PlayerMotor motor;
        [Tooltip("Optional input suppressor for conditional input handling.")]
        [SerializeField] private MonoBehaviour inputSuppressorComponent;

        private bool wasInputSuppressed;
        private Vector2 heldMoveInput;
        private IInputSuppressor inputSuppressor;

        private void Awake()
        {
            // Subscribe to input events
            GameInputEvents.OnMoveInput += HandleMoveInput;
            GameInputEvents.OnJumpPressed += HandleJumpPressed;
            GameInputEvents.OnJumpReleased += HandleJumpReleased;

            // Get input suppressor interface if available
            if (inputSuppressorComponent != null)
                inputSuppressor = inputSuppressorComponent as IInputSuppressor;
        }

        private void OnDestroy()
        {
            // Unsubscribe from input events
            GameInputEvents.OnMoveInput -= HandleMoveInput;
            GameInputEvents.OnJumpPressed -= HandleJumpPressed;
            GameInputEvents.OnJumpReleased -= HandleJumpReleased;
        }

        private void Update()
        {
            // Check if input suppression has ended and forward any held input
            bool currentlySuppress = inputSuppressor?.IsInputSuppressed ?? false;
            if (wasInputSuppressed && !currentlySuppress)
            {
                // Suppression just ended, forward any currently held input
                motor?.SetMove(heldMoveInput);
            }
            wasInputSuppressed = currentlySuppress;
        }

        private void HandleMoveInput(Vector2 moveValue)
        {
            heldMoveInput = moveValue;
            
            // Suppress lateral input if input suppressor is active
            if (inputSuppressor?.IsInputSuppressed ?? false)
            {
                return; // Skip forwarding to motor, but jump input remains unaffected
            }
            
            motor?.SetMove(moveValue);
        }

        private void HandleJumpPressed()
        {
            motor?.QueueJump();
        }

        private void HandleJumpReleased()
        {
            motor?.JumpCanceled();
        }
    }
}