using UnityEngine;
using UnityEngine.InputSystem;
using Game.Input;

namespace Game.Tutorial
{
    /// <summary>
    /// Manages tutorial steps, audio/text display, and input handling for step progression.
    /// Uses the InputAction system instead of hardcoded keys.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        [Header("Tutorial Settings")]
        [Tooltip("Tutorial configuration asset containing steps and settings.")]
        [SerializeField] private TutorialConfig tutorialConfig;
        
        [Tooltip("Reference to the Jump InputAction from the input system.")]
        [SerializeField] private InputActionReference jumpInputAction;
        
        [Header("UI References")]
        [Tooltip("Text component to display tutorial instructions.")]
        [SerializeField] private TMPro.TextMeshProUGUI tutorialText;
        
        [Tooltip("Canvas Group for tutorial UI to control visibility.")]
        [SerializeField] private CanvasGroup tutorialCanvas;
        
        [Header("Audio")]
        [Tooltip("Audio source for tutorial narration.")]
        [SerializeField] private AudioSource audioSource;
        
        private int currentStep = -1;
        private bool tutorialActive = false;
        private bool waitingForNextStep = false;
        private float stepStartTime = 0f;
        
        private void OnEnable()
        {
            if (jumpInputAction?.action != null)
            {
                jumpInputAction.action.performed += OnJumpPerformed;
            }
        }
        
        private void OnDisable()
        {
            if (jumpInputAction?.action != null)
            {
                jumpInputAction.action.performed -= OnJumpPerformed;
            }
        }
        
        private void Start()
        {
            // Hide tutorial UI initially
            SetTutorialUIVisible(false);
        }
        
        /// <summary>
        /// Starts the tutorial from the beginning. Can be called multiple times for repeatable tutorial.
        /// </summary>
        public void StartTutorial()
        {
            if (tutorialConfig == null || tutorialConfig.steps == null || tutorialConfig.steps.Length == 0)
            {
                Debug.LogWarning("[TutorialManager] No tutorial configuration or steps configured");
                return;
            }
            
            currentStep = 0;
            tutorialActive = true;
            waitingForNextStep = false;
            
            ShowCurrentStep();
            Debug.Log("[TutorialManager] Tutorial started");
        }
        
        /// <summary>
        /// Stops the tutorial and hides UI.
        /// </summary>
        public void StopTutorial()
        {
            tutorialActive = false;
            waitingForNextStep = false;
            currentStep = -1;
            
            SetTutorialUIVisible(false);
            
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            
            Debug.Log("[TutorialManager] Tutorial stopped");
        }
        
        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            // Only handle input if tutorial is active and waiting for next step
            if (tutorialActive && waitingForNextStep)
            {
                // Check minimum wait time if specified
                var currentStepConfig = GetCurrentStepConfig();
                if (currentStepConfig != null && currentStepConfig.minWaitTime > 0f)
                {
                    float timeSinceStepStart = Time.time - stepStartTime;
                    if (timeSinceStepStart < currentStepConfig.minWaitTime)
                    {
                        return; // Not enough time has passed
                    }
                }
                
                NextStep();
            }
        }
        
        private void ShowCurrentStep()
        {
            if (currentStep < 0 || tutorialConfig == null || currentStep >= tutorialConfig.steps.Length)
                return;
                
            var stepConfig = tutorialConfig.steps[currentStep];
            stepStartTime = Time.time;
                
            SetTutorialUIVisible(true);
            
            // Update text
            if (tutorialText != null)
            {
                string displayText = stepConfig.instructionText;
                if (!string.IsNullOrEmpty(tutorialConfig.continuePrompt))
                {
                    displayText += "\n\n" + tutorialConfig.continuePrompt;
                }
                tutorialText.text = displayText;
            }
            
            // Play audio if available
            if (audioSource != null && stepConfig.audioClip != null)
            {
                audioSource.clip = stepConfig.audioClip;
                audioSource.Play();
            }
            
            // After showing step, wait for input to continue
            waitingForNextStep = true;
            
            Debug.Log($"[TutorialManager] Showing step {currentStep + 1}/{tutorialConfig.steps.Length}: {stepConfig.instructionText}");
        }
        
        private void NextStep()
        {
            waitingForNextStep = false;
            currentStep++;
            
            if (tutorialConfig == null || currentStep >= tutorialConfig.steps.Length)
            {
                // Tutorial completed or should loop
                if (tutorialConfig != null && tutorialConfig.loopTutorial)
                {
                    currentStep = 0;
                    ShowCurrentStep();
                    Debug.Log("[TutorialManager] Tutorial looped to beginning");
                }
                else
                {
                    StopTutorial();
                    Debug.Log("[TutorialManager] Tutorial completed");
                }
            }
            else
            {
                // Show next step
                ShowCurrentStep();
            }
        }
        
        private TutorialStep GetCurrentStepConfig()
        {
            if (tutorialConfig == null || currentStep < 0 || currentStep >= tutorialConfig.steps.Length)
                return null;
            return tutorialConfig.steps[currentStep];
        }
        
        private void SetTutorialUIVisible(bool visible)
        {
            if (tutorialCanvas != null)
            {
                tutorialCanvas.alpha = visible ? 1f : 0f;
                tutorialCanvas.interactable = visible;
                tutorialCanvas.blocksRaycasts = visible;
            }
        }
        
        public bool IsTutorialActive => tutorialActive;
        public int CurrentStep => currentStep;
    }
}