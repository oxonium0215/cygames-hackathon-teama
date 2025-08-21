#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Game.Projection
{
    public partial class PerspectiveProjectionManager
    {
        /// <summary>
        /// Editor-only gizmo drawing for projection state and overlap check visualization.
        /// Shows current view state, projection axes, overlap detection volumes, and ground snap rays.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!enabled) return;

            DrawProjectionStateInfo();
            DrawViewAxesAndPlanes();
            DrawOverlapDetectionVolumes();
            DrawGroundSnapVisualization();
        }

        /// <summary>
        /// Draw current projection state information with labels and color coding.
        /// </summary>
        private void DrawProjectionStateInfo()
        {
            if (rotationCenter == null) return;

            // State-based color coding
            Color stateColor = Color.white;
            string stateLabel = "Projection State: ";

            if (Application.isPlaying)
            {
                if (IsSwitching)
                {
                    stateColor = Color.red;
                    stateLabel += "SWITCHING";
                }
                else
                {
                    stateColor = Color.green;
                    stateLabel += "IDLE";
                }
            }
            else
            {
                stateColor = Color.yellow;
                stateLabel += "EDITOR";
            }

            // Current view info
            string viewLabel = $"View: {(viewIndex == 0 ? "A" : "B")} ({GetProjectionForCurrent()})";
            string yawLabel = $"Camera Yaw: {(viewIndex == 0 ? viewAYaw : viewBYaw):F1}°";

            Vector3 labelPos = rotationCenter.position + Vector3.up * 3f;
            
            Handles.color = stateColor;
            Handles.Label(labelPos, stateLabel);
            Handles.Label(labelPos + Vector3.up * 0.3f, viewLabel);
            Handles.Label(labelPos + Vector3.up * 0.6f, yawLabel);

            // Draw a small indicator sphere at rotation center
            Gizmos.color = stateColor;
            Gizmos.DrawWireSphere(rotationCenter.position, 0.2f);
        }

        /// <summary>
        /// Draw view axes and projection planes to visualize the current projection setup.
        /// </summary>
        private void DrawViewAxesAndPlanes()
        {
            if (rotationCenter == null || !projectionBuilder) return;

            Vector3 center = rotationCenter.position;
            var currentAxis = GetProjectionForCurrent();
            
            // Draw projection planes as colored rectangles
            float planeSize = 5f;
            
            if (currentAxis == Game.Level.ProjectionAxis.FlattenZ)
            {
                // XY plane (Z flattened)
                float planeZ = projectionBuilder.GetPlaneZ();
                Vector3 planeCenter = new Vector3(center.x, center.y, planeZ);
                
                Gizmos.color = new Color(0f, 1f, 1f, 0.3f); // Cyan with transparency
                DrawPlaneGizmo(planeCenter, Vector3.forward, planeSize);
                
                Handles.color = Color.cyan;
                Handles.Label(planeCenter + Vector3.up * 0.5f, "XY Projection Plane");
                
                // Draw X axis indicator
                Gizmos.color = Color.red;
                Gizmos.DrawRay(planeCenter, Vector3.right * 2f);
                Gizmos.color = Color.green;
                Gizmos.DrawRay(planeCenter, Vector3.up * 2f);
            }
            else
            {
                // ZY plane (X flattened) 
                float planeX = projectionBuilder.GetPlaneX();
                Vector3 planeCenter = new Vector3(planeX, center.y, center.z);
                
                Gizmos.color = new Color(1f, 0f, 1f, 0.3f); // Magenta with transparency
                DrawPlaneGizmo(planeCenter, Vector3.right, planeSize);
                
                Handles.color = Color.magenta;
                Handles.Label(planeCenter + Vector3.up * 0.5f, "ZY Projection Plane");
                
                // Draw Z axis indicator
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(planeCenter, Vector3.forward * 2f);
                Gizmos.color = Color.green;
                Gizmos.DrawRay(planeCenter, Vector3.up * 2f);
            }
        }

        /// <summary>
        /// Draw overlap detection volumes used by the DepenetrationSolver.
        /// </summary>
        private void DrawOverlapDetectionVolumes()
        {
            if (!playerTransform || !playerCollider) return;

            // Only show during runtime when we can get actual bounds
            if (!Application.isPlaying) return;

            // Draw the overlap detection box
            Bounds bounds = playerCollider.bounds;
            Vector3 center = bounds.center;
            Vector3 halfExtents = bounds.extents * overlapBoxInflation;

            // Color based on whether overlap is detected
            bool hasOverlap = false;
            if (depenetrationSolver != null)
            {
                // We can't directly check for overlaps without calling the private method,
                // but we can indicate the potential detection volume
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Orange with transparency
            }
            else
            {
                Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f); // Gray when not available
            }

            Gizmos.DrawCube(center, halfExtents * 2f);
            Gizmos.color = Color.orange;
            Gizmos.DrawWireCube(center, halfExtents * 2f);

            // Label the detection volume
            Handles.color = Color.orange;
            Handles.Label(center + Vector3.up * (halfExtents.y + 0.5f), 
                $"Overlap Detection\nInflation: {overlapBoxInflation:F2}");

            // Draw resolve limits
            Vector3 labelPos = center + Vector3.up * (halfExtents.y + 1.2f);
            Handles.Label(labelPos, $"Max Step: {maxResolveStep:F1}m");
            Handles.Label(labelPos + Vector3.up * 0.3f, $"Max Total: {maxResolveTotal:F1}m");
        }

        /// <summary>
        /// Draw ground snap raycast visualization.
        /// </summary>
        private void DrawGroundSnapVisualization()
        {
            if (!playerTransform) return;

            Vector3 playerPos = playerTransform.position;
            Vector3 rayOrigin = playerPos + Vector3.up * snapUpAllowance;
            float maxDist = snapUpAllowance + snapDownDistance;

            // Draw the snap ray
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(rayOrigin, Vector3.down * maxDist);

            // Draw snap allowance zone
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f); // Yellow with transparency
            Gizmos.DrawCube(rayOrigin, new Vector3(0.1f, snapUpAllowance * 2f, 0.1f));

            // Check if we're in play mode and can do actual raycasts
            if (Application.isPlaying)
            {
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, maxDist, groundMask, QueryTriggerInteraction.Ignore))
                {
                    // Hit found - draw hit point
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(hit.point, 0.1f);
                    
                    Handles.color = Color.green;
                    Handles.Label(hit.point + Vector3.up * 0.5f, $"Ground Hit\nDist: {hit.distance:F2}m");
                    
                    // Draw ground skin offset
                    Vector3 skinPos = hit.point + Vector3.up * groundSkin;
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(skinPos, 0.05f);
                }
                else
                {
                    // No hit - draw max distance point
                    Vector3 maxPoint = rayOrigin + Vector3.down * maxDist;
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(maxPoint, 0.1f);
                    
                    Handles.color = Color.red;
                    Handles.Label(maxPoint + Vector3.up * 0.5f, "No Ground Hit");
                }
            }

            // Labels for snap parameters
            Handles.color = Color.yellow;
            Vector3 labelPos = rayOrigin + Vector3.right * 1f;
            Handles.Label(labelPos, $"Snap Up: {snapUpAllowance:F2}m");
            Handles.Label(labelPos + Vector3.up * 0.3f, $"Snap Down: {snapDownDistance:F2}m");
            Handles.Label(labelPos + Vector3.up * 0.6f, $"Ground Skin: {groundSkin:F3}m");
        }

        /// <summary>
        /// Helper method to draw a plane gizmo as a grid.
        /// </summary>
        private void DrawPlaneGizmo(Vector3 center, Vector3 normal, float size)
        {
            // Create a simple grid pattern to represent the plane
            Vector3 right, up;
            if (Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.9f)
            {
                right = Vector3.right;
                up = Vector3.forward;
            }
            else
            {
                right = Vector3.Cross(normal, Vector3.up).normalized;
                up = Vector3.Cross(right, normal).normalized;
            }

            float halfSize = size * 0.5f;
            int gridLines = 5;
            float step = size / gridLines;

            // Draw grid lines
            for (int i = 0; i <= gridLines; i++)
            {
                float offset = -halfSize + i * step;
                
                // Horizontal lines
                Vector3 start1 = center + right * (-halfSize) + up * offset;
                Vector3 end1 = center + right * halfSize + up * offset;
                Gizmos.DrawLine(start1, end1);
                
                // Vertical lines  
                Vector3 start2 = center + right * offset + up * (-halfSize);
                Vector3 end2 = center + right * offset + up * halfSize;
                Gizmos.DrawLine(start2, end2);
            }
        }
    }
}
#endif