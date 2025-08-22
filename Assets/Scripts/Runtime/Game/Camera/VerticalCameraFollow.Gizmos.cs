#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Game.Camera
{
    public partial class VerticalCameraFollow
    {
        /// <summary>
        /// Draws gizmos when this component is selected in the editor.
        /// Shows the top dead zone threshold as a cyan horizontal line with label.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Get current pivot position
            Vector3 pivotPosition = transform.position;
            
            // Compute the threshold Y: pivotPosition.y + topDeadZone
            float thresholdY = pivotPosition.y + topDeadZone;
            
            // Set gizmo color to cyan for dead zone visualization
            Gizmos.color = Color.cyan;
            Handles.color = Color.cyan;
            
            // Draw a modest horizontal cross centered at pivot XZ at threshold Y
            Vector3 thresholdCenter = new Vector3(pivotPosition.x, thresholdY, pivotPosition.z);
            float crossSize = 2.0f; // Keep modest size to avoid clutter
            
            // Draw horizontal line
            Vector3 leftPoint = thresholdCenter + Vector3.left * crossSize;
            Vector3 rightPoint = thresholdCenter + Vector3.right * crossSize;
            Vector3 frontPoint = thresholdCenter + Vector3.forward * crossSize;
            Vector3 backPoint = thresholdCenter + Vector3.back * crossSize;
            
            Gizmos.DrawLine(leftPoint, rightPoint);
            Gizmos.DrawLine(frontPoint, backPoint);
            
            // Add label using Handles
            Handles.Label(thresholdCenter + Vector3.up * 0.5f, "Top Dead Zone");
        }
    }
}
#endif
