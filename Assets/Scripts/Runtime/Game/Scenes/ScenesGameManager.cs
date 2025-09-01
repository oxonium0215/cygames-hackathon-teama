using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class ScenesGameManager : MonoBehaviour
{
	private static ScenesGameManager _instance;
	
	public static ScenesGameManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = FindFirstObjectByType<ScenesGameManager>();
				if (_instance == null)
				{
					GameObject go = new GameObject("ScenesGameManager");
					_instance = go.AddComponent<ScenesGameManager>();
				}
			}
			return _instance;
		}
	}

	private void Awake()
	{
		if (_instance != null && _instance != this)
		{
			Destroy(gameObject);
			return;
		}
		
		_instance = this;
		DontDestroyOnLoad(gameObject);
	}

	//this script is used to move the scenes
	[FormerlySerializedAs("inputToggleButton")] public UIDescription uiDescription;
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
