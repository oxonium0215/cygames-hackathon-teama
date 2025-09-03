using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class ScenesGameManager : MonoBehaviour
	{
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
