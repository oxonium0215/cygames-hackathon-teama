using UnityEngine;
using Game.Player;
using Game.Projection;

namespace Game.Debugging
{
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