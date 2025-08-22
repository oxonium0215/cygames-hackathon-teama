using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Pure C# implementation of player input that is updated via methods.
    /// Mirrors the current event-driven pattern while keeping logic simple.
    /// </summary>
    public class UnityPlayerInput
    {
        private Vector2 move;
        private bool jumpHeld;
        private bool jumpPressedThisFrame;
        
        public Vector2 Move => move;
        public bool JumpHeld => jumpHeld;
        public bool JumpPressedThisFrame => jumpPressedThisFrame;
        
        public void SetMove(Vector2 v)
        {
            move = v;
        }
        
        public void OnJumpPerformed()
        {
            jumpHeld = true;
            jumpPressedThisFrame = true;
        }
        
        public void OnJumpCanceled()
        {
            jumpHeld = false;
        }
        
        public void ClearTransient()
        {
            jumpPressedThisFrame = false;
        }
    }
}