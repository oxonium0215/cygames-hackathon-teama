using UnityEngine;

#if UNITY_EDITOR

namespace Game.Player
{
    public partial class PlayerMotor : MonoBehaviour
    {
        /// <summary>
        /// Editor-only gizmo drawing for ground check visualization.
        /// Shows a wire sphere at the ground check position with the configured radius.
        /// Green when grounded at runtime, yellow in edit-time/unknown state.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null) return;

            // Determine color based on runtime grounding state
            Color gizmoColor = Color.yellow; // Default edit-time color
            if (Application.isPlaying && groundProbe != null)
            {
                gizmoColor = groundProbe.IsGrounded ? Color.green : Color.yellow;
            }

            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}

#endif