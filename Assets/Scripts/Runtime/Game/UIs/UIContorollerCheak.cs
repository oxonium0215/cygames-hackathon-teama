using UnityEngine;
using TMPro;  // TextMeshPro 用
using UnityEngine.UI;

public class UIContorollerCheak : MonoBehaviour
{
    [SerializeField] private Button toggleButton;
    [SerializeField] private TMP_Text displayText;

    private void Start()
    {
        // 初期表示
        UpdateText();

        // ボタンにイベント登録
        toggleButton.onClick.AddListener(ToggleInputMode);
    }

    private void ToggleInputMode()
    {
        // 値を切り替え
        InputModeManager.IsKeyboard = !InputModeManager.IsKeyboard;
        UpdateText();
    }

    private void UpdateText()
    {
        displayText.text = InputModeManager.IsKeyboard ? "Keyboard Mode" : "Stick Mode";
    }
}