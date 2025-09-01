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
        public bool IsPreview { get; private set; }

        private void Awake()
        {
            playerInput = new UnityPlayerInput();
        }

        private void Start()
        {
            IsPreview = false;
        }

        private void Update()
        {
            playerInput?.ClearTransient();

            // Check if input suppression has ended and forward any held input
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

            // Update adapter state regardless of suppression
            playerInput?.SetMove(moveValue);

            // If preview is active, route horizontal input to camera rotation
            if (stagePreview != null && stagePreview.IsPreviewActive)
            {
                stagePreview.HandleCameraRotationInput(moveValue.x);
                return;
            }

            // Suppress lateral input during perspective switching if jump-only mode is enabled
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
            }
            else if (ctx.canceled)
            {
                playerInput?.OnJumpCanceled();
                motor?.JumpCanceled();
            }
        }

        public void OnSwitchView(InputAction.CallbackContext ctx)
        {
            // Tutorial active? block switching
            if (tutorialManager != null && tutorialManager.IsTutorialActive) return;

            if (ctx.performed)
                perspective?.TogglePerspective();
        }

        public void OnPreview3D(InputAction.CallbackContext ctx)
        {
            if (tutorialManager != null && tutorialManager.IsTutorialActive) return;
            if (!stagePreview) return;

            // Prevent preview during viewpoint changes
            if (perspective != null && perspective.IsSwitching) return;

            if (ctx.performed)
            {
                if (stagePreview.IsPreviewActive)
                {
                    stagePreview.EndPreview();
                    IsPreview = false;
                }
                else
                {
                    stagePreview.StartPreview();
                    IsPreview = true;
                }
            }
        }

        public void Respawn(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
            {
                motor?.Respawn();
            }
        }

        /// <summary>
        /// Clears the input state to prevent stale input from causing unwanted player movement.
        /// Called when exiting preview mode to ensure no residual input remains.
        /// </summary>
        public void ClearInputState()
        {
            playerInput?.SetMove(Vector2.zero);
        }
    }
}
