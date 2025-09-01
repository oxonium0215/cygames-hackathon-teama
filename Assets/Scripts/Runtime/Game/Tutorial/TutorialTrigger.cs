using UnityEngine;
using Game.Player;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Game.Tutorial
{
    public class TutorialTrigger : MonoBehaviour
    {
        [Header("Behavior")]
        [Tooltip("If true, the tutorial will only ever run once.")]
        [SerializeField] private bool triggerOnce = false;
        [Tooltip("If true, the tutorial starts as soon as the player enters the trigger.")]
        [SerializeField] private bool triggerOnTouch = false;

        [Header("References")]
        [SerializeField] private TutorialManager tutorialManager;
        [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();

        private PlayerInput playerInput;
        private PlayerMotor playerMotor;
        private bool isPlayerInTrigger = false;
        private bool canTriggerTutorial = true;
        private bool hasBeenTriggered = false; // Used for the 'triggerOnce' logic

        private void Update()
        {
            // Only listen for jump input if triggerOnTouch is false
            if (!triggerOnTouch && isPlayerInTrigger && canTriggerTutorial && playerInput != null && playerInput.actions["Jump"].WasPressedThisFrame())
            {
                // Respect the triggerOnce flag
                if (triggerOnce && hasBeenTriggered) return;

                if (tutorialManager != null && !tutorialManager.IsTutorialActive)
                {
                    StartTutorial();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var motor = other.GetComponent<PlayerMotor>();
            if (motor == null) return;

            isPlayerInTrigger = true;
            playerMotor = motor;
            playerInput = other.GetComponent<PlayerInput>();

            // Disable jumping unless the tutorial is touch-activated (since it starts instantly)
            if (!triggerOnTouch)
            {
                playerMotor.SetJumpingEnabled(false);
            }

            // If triggerOnTouch is enabled, start the tutorial immediately
            if (triggerOnTouch && canTriggerTutorial)
            {
                // Respect the triggerOnce flag
                if (triggerOnce && hasBeenTriggered) return;

                if (tutorialManager != null && !tutorialManager.IsTutorialActive)
                {
                    StartTutorial();
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var motor = other.GetComponent<PlayerMotor>();
            if (motor != null && motor == playerMotor)
            {
                isPlayerInTrigger = false;

                // Only re-enable the trigger if it's not a one-time event
                if (!triggerOnce)
                {
                    canTriggerTutorial = true;
                }

                if (playerMotor != null && (tutorialManager == null || !tutorialManager.IsTutorialActive))
                {
                    playerMotor.SetJumpingEnabled(true);
                }

                playerMotor = null;
                playerInput = null;
            }
        }

        private void StartTutorial()
        {
            canTriggerTutorial = false;
            if (triggerOnce)
            {
                hasBeenTriggered = true;
            }
            tutorialManager.StartTutorial(steps);
        }
    }
}