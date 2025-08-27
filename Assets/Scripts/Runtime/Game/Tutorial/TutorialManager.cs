using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Player;
using UnityEngine.InputSystem;
using TMPro;

namespace Game.Tutorial
{
    public class TutorialManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject dialogueBox;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private RawImage screenOverlay;

        [Header("Component References")]
        [SerializeField] private PlayerMotor playerMotor;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private PlayerInput playerInput; // Assign this in the inspector

        public bool IsTutorialActive { get; private set; }

        private List<TutorialStep> activeSteps;
        private int currentStepIndex;
        private bool isFocusActive;
        private Vector3 originalCameraPosition;
        private float originalCameraOrthoSize;
        private Coroutine activeCoroutine;
        private InputAction jumpAction;

        private void Awake()
        {
            if (playerInput != null)
            {
                jumpAction = playerInput.actions["Jump"];
            }
        }

        private void Start()
        {
            dialogueBox.SetActive(false);
            screenOverlay.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (IsTutorialActive && jumpAction != null && jumpAction.WasPressedThisFrame())
            {
                AdvanceStep();
            }
        }

        public void StartTutorial(List<TutorialStep> stepsToRun)
        {
            if (IsTutorialActive || stepsToRun == null || stepsToRun.Count == 0) return;

            activeSteps = stepsToRun;
            IsTutorialActive = true;
            currentStepIndex = 0;
            playerMotor.SetLateralEnabled(false);
            originalCameraPosition = mainCamera.transform.position;
            originalCameraOrthoSize = mainCamera.orthographicSize;
            ProcessStep(currentStepIndex);
        }

        private void AdvanceStep()
        {
            if (activeCoroutine != null) StopCoroutine(activeCoroutine);

            currentStepIndex++;
            if (currentStepIndex >= activeSteps.Count)
            {
                EndTutorial();
            } else
            {
                ProcessStep(currentStepIndex);
            }
        }

        private void ProcessStep(int index)
        {
            TutorialStep currentStep = activeSteps[index];
            switch (currentStep.type)
            {
                case TutorialStep.StepType.Dialogue:
                    activeCoroutine = StartCoroutine(ShowDialogue(currentStep.dialogueText));
                    break;
                case TutorialStep.StepType.FocusOn:
                    activeCoroutine = StartCoroutine(FocusOn(currentStep));
                    break;
                case TutorialStep.StepType.FocusOff:
                    activeCoroutine = StartCoroutine(FocusOff(currentStep));
                    break;
            }
        }

        private IEnumerator ShowDialogue(string text)
        {
            dialogueBox.SetActive(true);
            dialogueText.text = "";
            foreach (char letter in text.ToCharArray())
            {
                dialogueText.text += letter;
                yield return new WaitForSeconds(0.03f);
            }
        }

        private IEnumerator FocusOn(TutorialStep step)
        {
            isFocusActive = true;
            dialogueBox.SetActive(false);
            screenOverlay.gameObject.SetActive(step.showOverlay);

            Vector3 startCamPos = mainCamera.transform.position;
            float startCamSize = mainCamera.orthographicSize;
            float targetCamSize = originalCameraOrthoSize / step.zoomFactor;
            Vector3 targetCamPos = new Vector3(step.elementToFocusOn.transform.position.x, step.elementToFocusOn.transform.position.y, startCamPos.z);
            Material overlayMat = screenOverlay.material;

            float elapsed = 0f;
            while (elapsed < step.zoomDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / step.zoomDuration);
                float easedProgress = step.zoomCurve.Evaluate(progress);

                mainCamera.transform.position = Vector3.Lerp(startCamPos, targetCamPos, easedProgress);
                mainCamera.orthographicSize = Mathf.Lerp(startCamSize, targetCamSize, easedProgress);

                if (step.showOverlay)
                {
                    overlayMat.SetVector("_FocusPosition", mainCamera.WorldToViewportPoint(step.elementToFocusOn.transform.position));
                    overlayMat.SetFloat("_FocusRadius", Mathf.Lerp(0.5f, step.focusHoleDiameter, easedProgress));
                    overlayMat.SetFloat("_Feather", step.focusHoleFeather);
                }
                yield return null;
            }
        }

        private IEnumerator FocusOff(TutorialStep step)
        {
            isFocusActive = false;
            screenOverlay.gameObject.SetActive(false);
            yield return StartCoroutine(ReturnCameraToOrigin(step.zoomDuration, step.zoomCurve));
        }

        private void EndTutorial()
        {
            if (activeCoroutine != null) StopCoroutine(activeCoroutine);

            IsTutorialActive = false;
            dialogueBox.SetActive(false);
            playerMotor.SetLateralEnabled(true);

            if (isFocusActive)
            {
                screenOverlay.gameObject.SetActive(false);
                StartCoroutine(ReturnCameraToOrigin(0.5f, AnimationCurve.EaseInOut(0, 0, 1, 1)));
            }
        }

        private IEnumerator ReturnCameraToOrigin(float duration, AnimationCurve curve)
        {
            Vector3 startCamPos = mainCamera.transform.position;
            float startCamSize = mainCamera.orthographicSize;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float easedProgress = curve.Evaluate(progress);
                mainCamera.transform.position = Vector3.Lerp(startCamPos, originalCameraPosition, easedProgress);
                mainCamera.orthographicSize = Mathf.Lerp(startCamSize, originalCameraOrthoSize, easedProgress);
                yield return null;
            }

            mainCamera.transform.position = originalCameraPosition;
            mainCamera.orthographicSize = originalCameraOrthoSize;
        }
    }
}