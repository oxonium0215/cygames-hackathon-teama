using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class ScenesGameManager : MonoBehaviour
	{
		//this script is used to move the scenes
		public static ScenesGameManager Instance { get; private set; }
		
		[FormerlySerializedAs("inputToggleButton")] public UIDescription uiDescription;
		
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
