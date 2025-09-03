using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Game.UI;

namespace Game.Core
{
    public class ScenesGameManager : MonoBehaviour
    {
        public static ScenesGameManager Instance { get; private set; }

        //this script is used to move the scenes
        [FormerlySerializedAs("inputToggleButton")] public UIDescription uiDescription;

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
            uiDescription.goal = true;
            Time.timeScale = 0;
        }
    }
}
