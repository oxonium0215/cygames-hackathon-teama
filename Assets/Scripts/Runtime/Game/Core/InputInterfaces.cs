using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Interface for components that can receive player movement input.
    /// </summary>
    public interface IMovementInputReceiver
    {
        void SetMove(Vector2 move);
        void QueueJump();
        void JumpCanceled();
    }

    /// <summary>
    /// Interface for components that manage perspective switching.
    /// </summary>
    public interface IPerspectiveSwitcher
    {
        bool IsSwitching { get; }
        bool JumpOnlyDuringSwitch { get; }
        void TogglePerspective();
    }

    /// <summary>
    /// Interface for components that handle 3D preview functionality.
    /// </summary>
    public interface IPreviewController
    {
        void StartPreview();
        void EndPreview();
    }
}