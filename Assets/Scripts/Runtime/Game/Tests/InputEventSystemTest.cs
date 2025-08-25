using UnityEngine;
using Game.Core;

namespace Game.Tests
{
    /// <summary>
    /// Simple runtime test to validate the refactored input event system is working.
    /// This can be added to a test scene or run via the console.
    /// </summary>
    public class InputEventSystemTest : MonoBehaviour
    {
        private bool moveEventReceived;
        private bool jumpEventReceived;
        private bool switchViewEventReceived;

        private void Awake()
        {
            // Subscribe to input events to test they work
            GameInputEvents.OnMoveInput += OnMoveInputReceived;
            GameInputEvents.OnJumpPressed += OnJumpPressedReceived;
            GameInputEvents.OnSwitchViewPressed += OnSwitchViewPressedReceived;
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            GameInputEvents.OnMoveInput -= OnMoveInputReceived;
            GameInputEvents.OnJumpPressed -= OnJumpPressedReceived;
            GameInputEvents.OnSwitchViewPressed -= OnSwitchViewPressedReceived;
        }

        private void Start()
        {
            // Test that events can be invoked programmatically
            TestInputEventSystem();
        }

        [ContextMenu("Test Input Event System")]
        public void TestInputEventSystem()
        {
            Debug.Log("Testing input event system...");

            // Reset test flags
            moveEventReceived = false;
            jumpEventReceived = false;
            switchViewEventReceived = false;

            // Simulate input events
            GameInputEvents.InvokeMoveInput(Vector2.one);
            GameInputEvents.InvokeJumpPressed();
            GameInputEvents.InvokeSwitchViewPressed();

            // Verify events were received
            if (moveEventReceived && jumpEventReceived && switchViewEventReceived)
            {
                Debug.Log("✓ Input event system test PASSED - All events working correctly");
            }
            else
            {
                Debug.LogError($"✗ Input event system test FAILED - Move: {moveEventReceived}, Jump: {jumpEventReceived}, Switch: {switchViewEventReceived}");
            }
        }

        private void OnMoveInputReceived(Vector2 move)
        {
            moveEventReceived = true;
            Debug.Log($"Move event received: {move}");
        }

        private void OnJumpPressedReceived()
        {
            jumpEventReceived = true;
            Debug.Log("Jump event received");
        }

        private void OnSwitchViewPressedReceived()
        {
            switchViewEventReceived = true;
            Debug.Log("Switch view event received");
        }
    }
}