using UnityEngine;
using Game.Player;
using System.Collections.Generic;

namespace Game.Tutorial
{
    public class TutorialTrigger : MonoBehaviour
    {
        [SerializeField] private TutorialManager tutorialManager;
        [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();

        private bool hasBeenTriggered = false;

        private void OnTriggerEnter(Collider other)
        {
            if (hasBeenTriggered || tutorialManager == null || other.GetComponent<PlayerMotor>() == null)
            {
                return;
            }

            tutorialManager.StartTutorial(steps);
            hasBeenTriggered = true;
            GetComponent<Collider>().enabled = false;
        }
    }
}