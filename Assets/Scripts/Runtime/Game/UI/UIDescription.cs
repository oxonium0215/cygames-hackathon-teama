using Game.Preview;
using UnityEngine;

namespace Game.UI
{
    public class UIDescription : MonoBehaviour
    {

	GameObject Text1;    // Start is called once before the first execution of Update after the MonoBehaviour is created

	GameObject Text2;
	public GameObject KeyText1;    // Start is called once before the first execution of Update after the MonoBehaviour is created

	public GameObject KeyText2;
	public GameObject ConText1;    // Start is called once before the first execution of Update after the MonoBehaviour is created

	public GameObject ConText2;
	public bool isKeyboard = true;
	public StagePreviewManager StagePreviewManager;
	private bool Change;
	public GameObject TieObject;
	public GameObject GoalGameObject;
	public bool goal;

	void Start()
	{
		isKeyboard = InputModeManager.IsKeyboard;
		Debug.Log($"[UIDescription] 操作方式: {(isKeyboard ? "Keyboard" : "Stick")}");
		if (isKeyboard)
		{
			Text1 = KeyText1;
			Text2 = KeyText2;
		}
		else
		{
			Text1 = ConText1;
			Text2 = ConText2;
		}
		Text1.SetActive(true);
	}

	// Update is called once per frame
	void Update()
	{

		if (StagePreviewManager.isPreviewActive != Change)
		{
			Change = StagePreviewManager.isPreviewActive;
			Text1.SetActive(!Text1.activeSelf);
			Text2.SetActive(!Text1.activeSelf);
		}

		if (goal)
		{
			TieObject.SetActive(false);
			GoalGameObject.SetActive(true);
		}
	}
}
