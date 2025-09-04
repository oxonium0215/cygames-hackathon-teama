using UnityEngine;
using UnityEngine.SceneManagement;
using Game.UI;

namespace Game.Core
{
    public class ScenesGameManager : MonoBehaviour
    {
        public static ScenesGameManager Instance { get; private set; }

        public bool IsGameOver { get; set; }
        public bool IsGameClear { get; set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void MoveToScene(string sceneName)
        {
            // Reset goal UI state and ensure time scale is restored
            goalUIActive = false;
            Time.timeScale = 1;
            SceneManager.LoadScene(sceneName);
        }

        private bool goalUIActive = false;
        
        private void Update()
        {
            // Handle UI interactions when game is paused (Time.timeScale = 0)
            if (goalUIActive && Time.timeScale == 0)
            {
                // Temporarily allow UI to process input by setting a very small timeScale
                if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
                {
                    Time.timeScale = 0.001f; // Small value to allow UI processing
                    // Reset back to 0 after a brief moment to maintain pause state
                    Invoke("ResetTimeScale", 0.1f);
                }
            }
        }
        
        private void ResetTimeScale()
        {
            if (goalUIActive)
                Time.timeScale = 0;
        }

        public void Goal()
        {
            // Find the UIDescription component in the active scene
            UIDescription goalUI = FindObjectOfType<UIDescription>();
            if (goalUI != null)
            {
                goalUI.goal = true;
                goalUIActive = true;
                Time.timeScale = 0;
            }
        }
    }
}
