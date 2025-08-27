using UnityEngine;
using Game.Input;

namespace Game.Tutorial
{
    /// <summary>
    /// Trigger component for tutorial flag blocks that starts tutorial when player presses jump while overlapping.
    /// Attach this to the tutorial flag block GameObject.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TutorialTrigger : MonoBehaviour
    {
        [Tooltip("Reference to the TutorialManager that handles tutorial steps.")]
        [SerializeField] private TutorialManager tutorialManager;
        
        [Tooltip("Tag of the player GameObject to detect overlaps.")]
        [SerializeField] private string playerTag = "Player";
        
        private bool playerInTrigger = false;
        private PlayerInputRelay inputRelay;
        
        private void Start()
        {
            // Find the PlayerInputRelay to access player input
            inputRelay = FindObjectOfType<PlayerInputRelay>();
            if (inputRelay == null)
            {
                Debug.LogError("[TutorialTrigger] PlayerInputRelay not found in scene");
            }
            
            // Ensure trigger is set up properly
            var collider = GetComponent<Collider>();
            if (!collider.isTrigger)
            {
                Debug.LogWarning("[TutorialTrigger] Collider should be set as trigger for proper functionality");
            }
        }
        
        private void Update()
        {
            // Check for jump input while player is in trigger
            if (playerInTrigger && inputRelay != null && inputRelay.PlayerInput != null && inputRelay.PlayerInput.JumpPressedThisFrame)
            {
                StartTutorial();
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(playerTag))
            {
                playerInTrigger = true;
                Debug.Log("[TutorialTrigger] Player entered tutorial trigger area. Press jump to start tutorial.");
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(playerTag))
            {
                playerInTrigger = false;
                Debug.Log("[TutorialTrigger] Player left tutorial trigger area");
            }
        }
        
        private void StartTutorial()
        {
            if (tutorialManager != null)
            {
                tutorialManager.StartTutorial();
                Debug.Log("[TutorialTrigger] Tutorial started via jump input");
            }
            else
            {
                Debug.LogWarning("[TutorialTrigger] TutorialManager not assigned");
            }
        }
    }
}