using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class ScenesGameManager : MonoBehaviour
{
	// Singleton instance
	private static ScenesGameManager _instance;
	public static ScenesGameManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = FindObjectOfType<ScenesGameManager>();
				if (_instance == null)
				{
					Debug.LogError("ScenesGameManager instance not found in scene!");
				}
			}
			return _instance;
		}
	}

	//this script is used to move the scenes
	[FormerlySerializedAs("inputToggleButton")] public UIDescription uiDescription;

	private void Awake()
	{
		// Ensure singleton pattern
		if (_instance != null && _instance != this)
		{
			Destroy(gameObject);
			return;
		}
		_instance = this;
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
		// Don't freeze the game completely - let UI remain interactive
		// Time.timeScale = 0;
	}
}
