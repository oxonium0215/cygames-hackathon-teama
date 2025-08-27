using UnityEngine;
using UnityEngine.InputSystem;
using Game.Player;
using Game.Projection;
using Game.Preview;
using Game.Tutorial;

namespace Game.Input
{
    public class PlayerInputRelay : MonoBehaviour
    {
        [Header("Component References")]
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PerspectiveProjectionManager perspective;
        [SerializeField] private StagePreviewManager stagePreview;
        [SerializeField] private TutorialManager tutorialManager;

        private UnityPlayerInput playerInput;
        private bool wasInputSuppressed;

        private void Awake()
        {
            playerInput = new UnityPlayerInput();
        }

        private void Update()
        {
            playerInput?.ClearTransient();

            bool currentlySuppress = perspective != null && perspective.IsSwitching && perspective.JumpOnlyDuringSwitch;
            if (wasInputSuppressed && !currentlySuppress && playerInput != null)
            {
                motor?.SetMove(playerInput.Move);
            }
            wasInputSuppressed = currentlySuppress;
        }

        public void OnMove(InputAction.CallbackContext ctx)
        {
            if (tutorialManager != null && tutorialManager.IsTutorialActive)
            {
                motor?.SetMove(Vector2.zero);
                return;
            }

            Vector2 moveValue = ctx.ReadValue<Vector2>();
            playerInput?.SetMove(moveValue);

            if (stagePreview != null && stagePreview.IsPreviewActive)
            {
                stagePreview.HandleCameraRotationInput(moveValue.x);
                return;
            }

            if (perspective != null && perspective.IsSwitching && perspective.JumpOnlyDuringSwitch)
            {
                return;
            }

            motor?.SetMove(moveValue);
        }

        public void OnJump(InputAction.CallbackContext ctx)
        {
            if (tutorialManager != null && tutorialManager.IsTutorialActive) return;

            if (ctx.performed)
            {
                playerInput?.OnJumpPerformed();
                motor?.QueueJump();
            } else if (ctx.canceled)
            {
                playerInput?.OnJumpCanceled();
                motor?.JumpCanceled();
            }
        }

        public void OnSwitchView(InputAction.CallbackContext ctx)
        {
            if (tutorialManager != null && tutorialManager.IsTutorialActive) return;

            if (ctx.performed)
                perspective?.TogglePerspective();
        }

        public void OnPreview3D(InputAction.CallbackContext ctx)
        {
            if (tutorialManager != null && tutorialManager.IsTutorialActive) return;

            if (!stagePreview) return;
            if (perspective != null && perspective.IsSwitching) return;

            if (ctx.performed)
            {
                if (stagePreview.IsPreviewActive)
                {
                    stagePreview.EndPreview();
                } else
                {
                    stagePreview.StartPreview();
                }
            }
        }

        public void ClearInputState()
        {
            playerInput?.SetMove(Vector2.zero);
        }
    }
}