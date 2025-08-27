using UnityEngine;
using Game.Player;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Game.Tutorial
{
    public class TutorialTrigger : MonoBehaviour
    {
        [SerializeField] private TutorialManager tutorialManager;
        [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();

        private PlayerInput playerInput;

        private void OnTriggerStay(Collider other)
        {
            if (tutorialManager == null || other.GetComponent<PlayerMotor>() == null)
            {
                return;
            }

            if (playerInput == null)
            {
                playerInput = other.GetComponent<PlayerInput>();
            }

            if (playerInput != null && playerInput.actions["Jump"].WasPressedThisFrame())
            {
                tutorialManager.StartTutorial(steps);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<PlayerMotor>() != null)
            {
                playerInput = null;
            }
        }
    }
}