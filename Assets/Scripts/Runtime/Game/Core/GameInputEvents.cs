using UnityEngine;
using System;

namespace Game.Core
{
    /// <summary>
    /// Static input event system to decouple input from gameplay systems.
    /// This allows the input assembly to be independent of gameplay assemblies.
    /// </summary>
    public static class GameInputEvents
    {
        // Input events that gameplay systems can subscribe to
        public static event Action<Vector2> OnMoveInput;
        public static event Action OnJumpPressed;
        public static event Action OnJumpReleased;
        public static event Action OnSwitchViewPressed;
        public static event Action OnPreview3DStarted;
        public static event Action OnPreview3DEnded;

        // Methods for input system to call
        public static void InvokeMoveInput(Vector2 move)
        {
            OnMoveInput?.Invoke(move);
        }

        public static void InvokeJumpPressed()
        {
            OnJumpPressed?.Invoke();
        }

        public static void InvokeJumpReleased()
        {
            OnJumpReleased?.Invoke();
        }

        public static void InvokeSwitchViewPressed()
        {
            OnSwitchViewPressed?.Invoke();
        }

        public static void InvokePreview3DStarted()
        {
            OnPreview3DStarted?.Invoke();
        }

        public static void InvokePreview3DEnded()
        {
            OnPreview3DEnded?.Invoke();
        }
    }
}