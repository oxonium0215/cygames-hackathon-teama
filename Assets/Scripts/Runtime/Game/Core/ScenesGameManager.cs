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
            SceneManager.LoadScene(sceneName);
            Time.timeScale = 1;
        }

        public void Goal()
        {
            // Find the UIDescription component in the active scene
            UIDescription goalUI = FindObjectOfType<UIDescription>();
            if (goalUI != null)
            {
                goalUI.goal = true;
                Time.timeScale = 0;
            }
        }
    }
}
