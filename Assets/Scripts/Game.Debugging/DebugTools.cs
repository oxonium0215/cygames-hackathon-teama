using UnityEngine;
using UnityEngine.InputSystem;
using Game.Player;
using Game.Projection;

namespace Game.Debugging
{
    /// <summary>
    /// Debug utility to echo input events to console.
    /// </summary>
    public class EchoInput : MonoBehaviour
    {
        // Must be public and take InputAction.CallbackContext to appear under the "Dynamic" list.
        public void OnMove(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed && !ctx.canceled) return;
            Vector2 v = ctx.ReadValue<Vector2>();
            Debug.Log($"[EchoInput] Move: {v} (phase: {ctx.phase})");
        }

        public void OnJump(InputAction.CallbackContext ctx)
        {
            if (ctx.performed) Debug.Log("[EchoInput] Jump pressed");
        }

        public void OnSwitchView(InputAction.CallbackContext ctx)
        {
            if (ctx.performed) Debug.Log("[EchoInput] SwitchView pressed");
        }
    }

    /// <summary>
    /// Test component to validate input suppression fix.
    /// Attach to a GameObject and check the console for debug logs during viewpoint transitions.
    /// </summary>
    public class InputSuppressionTest : MonoBehaviour
    {
        [Tooltip("PlayerInputRelay to monitor")]
        [SerializeField] private PlayerInputRelay inputRelay;
        
        [Tooltip("PerspectiveProjectionManager to monitor")]
        [SerializeField] private PerspectiveProjectionManager perspective;
        
        private bool wasSuppressionActive;
        
        private void Update()
        {
            if (perspective == null) return;
            
            bool currentSuppressionActive = perspective.IsSwitching && perspective.JumpOnlyDuringSwitch;
            
            // Log when suppression starts
            if (!wasSuppressionActive && currentSuppressionActive)
            {
                Debug.Log("[InputSuppressionTest] Input suppression STARTED");
            }
            
            // Log when suppression ends
            if (wasSuppressionActive && !currentSuppressionActive)
            {
                Debug.Log("[InputSuppressionTest] Input suppression ENDED - input should now be responsive");
            }
            
            wasSuppressionActive = currentSuppressionActive;
        }
    }
}