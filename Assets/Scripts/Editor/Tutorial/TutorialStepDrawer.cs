using UnityEngine;
using UnityEditor;
using Game.Tutorial;

[CustomPropertyDrawer(typeof(TutorialStep))]
public class TutorialStepDrawer : PropertyDrawer
{
    private const float TextAreaHeight = 60f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var nameProperty = property.FindPropertyRelative("name");
        var typeProperty = property.FindPropertyRelative("type");
        var dialogueTextProperty = property.FindPropertyRelative("dialogueText");
        var elementToFocusOnProperty = property.FindPropertyRelative("elementToFocusOn");
        var zoomFactorProperty = property.FindPropertyRelative("zoomFactor");
        var zoomDurationProperty = property.FindPropertyRelative("zoomDuration");
        var zoomCurveProperty = property.FindPropertyRelative("zoomCurve");
        var showOverlayProperty = property.FindPropertyRelative("showOverlay");
        var focusHoleDiameterProperty = property.FindPropertyRelative("focusHoleDiameter");
        var focusHoleFeatherProperty = property.FindPropertyRelative("focusHoleFeather");

        var rect = new Rect(position.x, position.y + 2, position.width, EditorGUIUtility.singleLineHeight);

        EditorGUI.PropertyField(rect, nameProperty);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        EditorGUI.indentLevel++;
        rect = EditorGUI.IndentedRect(rect);

        EditorGUI.PropertyField(rect, typeProperty);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        TutorialStep.StepType stepType = (TutorialStep.StepType)typeProperty.enumValueIndex;

        switch (stepType)
        {
            case TutorialStep.StepType.Dialogue:
                EditorGUI.LabelField(rect, "Text");
                rect.y += EditorGUIUtility.singleLineHeight;
                rect.height = TextAreaHeight;
                dialogueTextProperty.stringValue = EditorGUI.TextArea(rect, dialogueTextProperty.stringValue);
                break;

            case TutorialStep.StepType.FocusOn:
                EditorGUI.PropertyField(rect, elementToFocusOnProperty);
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(rect, showOverlayProperty);
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                if (showOverlayProperty.boolValue)
                {
                    EditorGUI.PropertyField(rect, focusHoleDiameterProperty);
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(rect, focusHoleFeatherProperty);
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                }

                EditorGUI.PropertyField(rect, zoomFactorProperty);
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(rect, zoomDurationProperty);
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(rect, zoomCurveProperty);
                break;

            case TutorialStep.StepType.FocusOff:
                EditorGUI.PropertyField(rect, zoomDurationProperty);
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(rect, zoomCurveProperty);
                break;
        }

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var typeProperty = property.FindPropertyRelative("type");
        TutorialStep.StepType stepType = (TutorialStep.StepType)typeProperty.enumValueIndex;

        float totalHeight = (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2;

        switch (stepType)
        {
            case TutorialStep.StepType.Dialogue:
                totalHeight += EditorGUIUtility.singleLineHeight + TextAreaHeight;
                break;
            case TutorialStep.StepType.FocusOn:
                var showOverlayProperty = property.FindPropertyRelative("showOverlay");
                int focusOnFieldCount = showOverlayProperty.boolValue ? 7 : 5;
                totalHeight += (EditorGUIUtility.singleLineHeight * focusOnFieldCount) + (EditorGUIUtility.standardVerticalSpacing * (focusOnFieldCount - 1));
                break;
            case TutorialStep.StepType.FocusOff:
                totalHeight += (EditorGUIUtility.singleLineHeight * 2) + EditorGUIUtility.standardVerticalSpacing;
                break;
        }

        totalHeight += EditorGUIUtility.standardVerticalSpacing * 2;
        return totalHeight;
    }
}