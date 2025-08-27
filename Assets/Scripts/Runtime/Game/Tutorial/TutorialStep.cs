using UnityEngine;

[System.Serializable]
public class TutorialStep
{
    public enum StepType
    {
        Dialogue,
        FocusOn,
        FocusOff
    }

    public string name;
    public StepType type;

    [TextArea(3, 10)]
    public string dialogueText;

    public GameObject elementToFocusOn;
    public bool showOverlay = true;
    public float zoomFactor = 2f;
    public float zoomDuration = 1.0f;
    public AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float focusHoleDiameter = 0.15f;
    public float focusHoleFeather = 0.1f;
}