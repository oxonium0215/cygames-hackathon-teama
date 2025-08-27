using UnityEngine;

namespace Game.Tutorial
{
    /// <summary>
    /// Helper component to easily configure tutorial steps with text and audio.
    /// Add this to a GameObject with TutorialManager to define tutorial content.
    /// </summary>
    [System.Serializable]
    public class TutorialStep
    {
        [TextArea(2, 4)]
        public string instructionText;
        
        [Tooltip("Optional audio clip for this step")]
        public AudioClip audioClip;
        
        [Tooltip("Optional duration to wait before allowing next step (in seconds)")]
        public float minWaitTime = 0f;
    }
    
    /// <summary>
    /// Scriptable object or component to define tutorial content
    /// </summary>
    [CreateAssetMenu(fileName = "TutorialConfig", menuName = "Game/Tutorial Configuration")]
    public class TutorialConfig : ScriptableObject
    {
        [Tooltip("Title of this tutorial")]
        public string tutorialTitle = "Tutorial";
        
        [Tooltip("Array of tutorial steps")]
        public TutorialStep[] steps;
        
        [Tooltip("Should this tutorial loop when reaching the end?")]
        public bool loopTutorial = false;
        
        [Tooltip("Key bindings text to show (e.g., 'Press [Jump] to continue')")]
        public string continuePrompt = "Press [Jump] to continue...";
    }
}