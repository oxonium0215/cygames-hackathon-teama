using UnityEngine;

namespace Game.Input
{
    /// <summary>
    /// Minimal input interface that represents the inputs PlayerMotor needs.
    /// </summary>
    public interface IPlayerInput
    {
        Vector2 Move { get; }
        bool JumpHeld { get; }
        bool JumpPressedThisFrame { get; }
        
        /// <summary>
        /// Called once per frame by the owner to reset edge flags.
        /// </summary>
        void ClearTransient();
    }
}