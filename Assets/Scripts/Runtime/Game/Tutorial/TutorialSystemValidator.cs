using UnityEngine;
using UnityEngine.InputSystem;
using Game.Input;

namespace Game.Tutorial
{
    /// <summary>
    /// Validation component to test tutorial functionality.
    /// Attach to any GameObject to test the tutorial system integration.
    /// </summary>
    public class TutorialSystemValidator : MonoBehaviour
    {
        [Header("Validation Settings")]
        [SerializeField] private bool enableDebugLogging = true;
        [SerializeField] private bool testOnStart = false;
        
        private PlayerInputRelay inputRelay;
        private TutorialManager tutorialManager;
        private TutorialTrigger tutorialTrigger;
        
        private void Start()
        {
            if (testOnStart)
            {
                ValidateSetup();
            }
        }
        
        [ContextMenu("Validate Tutorial Setup")]
        public void ValidateSetup()
        {
            bool allValid = true;
            
            // Find core components
            inputRelay = FindObjectOfType<PlayerInputRelay>();
            tutorialManager = FindObjectOfType<TutorialManager>();
            tutorialTrigger = FindObjectOfType<TutorialTrigger>();
            
            // Validate PlayerInputRelay integration
            if (inputRelay != null)
            {
                if (inputRelay.PlayerInput != null)
                {
                    LogValidation("✅ PlayerInputRelay.PlayerInput is accessible");
                }
                else
                {
                    LogValidation("❌ PlayerInputRelay.PlayerInput is null");
                    allValid = false;
                }
            }
            else
            {
                LogValidation("❌ PlayerInputRelay not found in scene");
                allValid = false;
            }
            
            // Validate TutorialManager
            if (tutorialManager != null)
            {
                LogValidation("✅ TutorialManager found");
                
                // Check if InputActionReference is assigned (this requires Inspector setup)
                var jumpAction = GetInputActionFromManager();
                if (jumpAction != null)
                {
                    LogValidation("✅ Jump InputAction is assigned and available");
                    LogValidation($"📋 Jump InputAction bindings: {jumpAction.bindings.Count}");
                    for (int i = 0; i < jumpAction.bindings.Count; i++)
                    {
                        var binding = jumpAction.bindings[i];
                        LogValidation($"  - Binding {i}: {binding.path}");
                    }
                }
                else
                {
                    LogValidation("❌ Jump InputAction not accessible (may need Inspector setup)");
                }
            }
            else
            {
                LogValidation("❌ TutorialManager not found in scene");
                allValid = false;
            }
            
            // Validate TutorialTrigger
            if (tutorialTrigger != null)
            {
                LogValidation("✅ TutorialTrigger found");
                var triggerCollider = tutorialTrigger.GetComponent<Collider>();
                if (triggerCollider != null && triggerCollider.isTrigger)
                {
                    LogValidation("✅ TutorialTrigger has proper trigger collider setup");
                }
                else
                {
                    LogValidation("❌ TutorialTrigger missing trigger collider");
                    allValid = false;
                }
            }
            else
            {
                LogValidation("❌ TutorialTrigger not found in scene");
                allValid = false;
            }
            
            // Final validation summary
            LogValidation(allValid ? "🎉 All tutorial components validated successfully!" : "⚠️ Some tutorial components need attention");
        }
        
        [ContextMenu("Test Jump Input Detection")]
        public void TestJumpInputDetection()
        {
            if (inputRelay?.PlayerInput != null)
            {
                bool jumpPressed = inputRelay.PlayerInput.JumpPressedThisFrame;
                bool jumpHeld = inputRelay.PlayerInput.JumpHeld;
                
                LogValidation($"Jump Input State - Pressed This Frame: {jumpPressed}, Held: {jumpHeld}");
            }
            else
            {
                LogValidation("❌ Cannot test jump input - PlayerInputRelay.PlayerInput not available");
            }
        }
        
        private InputAction GetInputActionFromManager()
        {
            if (tutorialManager == null) return null;
            
            // We can't directly access the private field, but we can check if the system works
            // by looking at the InputSystem's actions
            var gameplayInputActions = FindObjectOfType<PlayerInput>();
            if (gameplayInputActions != null)
            {
                var jumpAction = gameplayInputActions.actions.FindAction("Jump");
                return jumpAction;
            }
            
            return null;
        }
        
        private void LogValidation(string message)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"[TutorialValidator] {message}");
            }
        }
    }
}