#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Game.Camera
{
    public partial class VerticalCameraFollow
    {
        /// <summary>
        /// Draws gizmos when this component is selected in the editor.
        /// Shows the top and bottom dead zone thresholds with labels.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Only draw in editor and when we have valid policies
            if (_deadZonePolicy == null) return;

            // Get current pivot position
            Vector3 pivotPosition = transform.position;
            float crossSize = 2.0f; // Keep modest size to avoid clutter
            
            // Draw top dead zone
            {
                float topThresholdY = _deadZonePolicy.ComputeThreshold(pivotPosition.y);
                
                // Set gizmo color to cyan for top dead zone visualization
                Gizmos.color = Color.cyan;
                Handles.color = Color.cyan;
                
                // Draw a modest horizontal cross centered at pivot XZ at threshold Y
                Vector3 thresholdCenter = new Vector3(pivotPosition.x, topThresholdY, pivotPosition.z);
                
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
            
            // Draw bottom dead zone (only if downward movement is allowed)
            if (!neverScrollDown && _bottomDeadZonePolicy != null)
            {
                float bottomThresholdY = _bottomDeadZonePolicy.ComputeThreshold(pivotPosition.y);
                
                // Set gizmo color to yellow for bottom dead zone visualization
                Gizmos.color = Color.yellow;
                Handles.color = Color.yellow;
                
                // Draw a modest horizontal cross centered at pivot XZ at threshold Y
                Vector3 thresholdCenter = new Vector3(pivotPosition.x, bottomThresholdY, pivotPosition.z);
                
                // Draw horizontal line
                Vector3 leftPoint = thresholdCenter + Vector3.left * crossSize;
                Vector3 rightPoint = thresholdCenter + Vector3.right * crossSize;
                Vector3 frontPoint = thresholdCenter + Vector3.forward * crossSize;
                Vector3 backPoint = thresholdCenter + Vector3.back * crossSize;
                
                Gizmos.DrawLine(leftPoint, rightPoint);
                Gizmos.DrawLine(frontPoint, backPoint);
                
                // Add label using Handles
                Handles.Label(thresholdCenter + Vector3.down * 0.5f, "Bottom Dead Zone");
            }
        }
    }
}
#endif