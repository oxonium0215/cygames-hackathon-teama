// InputModeManager.cs
using UnityEngine;

public static class InputModeManager
{
    // 起動ごとのデフォルト（必要なら後述のPlayerPrefsで永続化も可）
    public static bool IsKeyboard = true;

    // 任意：起動間も含めて永続化したい場合はこれを使う
    public static void Save() => PlayerPrefs.SetInt("Input_IsKeyboard", IsKeyboard ? 1 : 0);
    public static void Load() => IsKeyboard = PlayerPrefs.GetInt("Input_IsKeyboard", 1) == 1;

    // 任意：ゲーム開始時に一度読む
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitOnStart()
    {
        // 起動間の永続化が不要ならコメントアウトでOK
        Load();
    }
}