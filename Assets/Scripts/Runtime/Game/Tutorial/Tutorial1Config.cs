using UnityEngine;

namespace Game.Tutorial
{
    /// <summary>
    /// Example tutorial configuration for Tutorial1 scene.
    /// This demonstrates the basic setup for a tutorial system.
    /// </summary>
    [CreateAssetMenu(fileName = "Tutorial1Config", menuName = "Game/Tutorial Configuration/Tutorial1")]
    public class Tutorial1Config : TutorialConfig
    {
        private void OnEnable()
        {
            // Set default values for Tutorial1
            if (steps == null || steps.Length == 0)
            {
                tutorialTitle = "Basic Controls Tutorial";
                continuePrompt = "Press [Jump] to continue...";
                loopTutorial = false;
                
                steps = new TutorialStep[]
                {
                    new TutorialStep 
                    { 
                        instructionText = "Welcome to the tutorial! This will teach you the basic controls.",
                        minWaitTime = 1f
                    },
                    new TutorialStep 
                    { 
                        instructionText = "Use WASD or arrow keys to move around. Try moving now!",
                        minWaitTime = 2f
                    },
                    new TutorialStep 
                    { 
                        instructionText = "Press SPACE, or any gamepad jump button to jump. The same button advances these tutorial steps!",
                        minWaitTime = 1f
                    },
                    new TutorialStep 
                    { 
                        instructionText = "You can return to this tutorial flag block and press jump again to repeat this tutorial anytime.",
                        minWaitTime = 1f
                    },
                    new TutorialStep 
                    { 
                        instructionText = "Tutorial complete! You now know the basic controls. Good luck!",
                        minWaitTime = 1f
                    }
                };
            }
        }
    }
}