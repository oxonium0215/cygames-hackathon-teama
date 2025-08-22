using UnityEngine;
using Game.Camera;

namespace Game.Projection
{
    /// <summary>
    /// Handles camera pivot adjustments during projection switches.
    /// Now coordinates with VerticalCameraFollow to prevent conflicts.
    /// </summary>
    public class CameraProjectionAdapter
    {
        private readonly Transform cameraPivot;
        private VerticalCameraFollow verticalFollow;
        
        public CameraProjectionAdapter(Transform cameraPivot)
        {
            this.cameraPivot = cameraPivot;
            // Cache the VerticalCameraFollow component for coordination
            if (cameraPivot)
            {
                verticalFollow = cameraPivot.GetComponent<VerticalCameraFollow>();
            }
        }
        
        public void RepositionPivotToCenter(Transform rotationCenter, Vector3 pivotOffset)
        {
            if (!cameraPivot) return;
            
            Vector3 center = rotationCenter ? rotationCenter.position : cameraPivot.position - pivotOffset;
            Vector3 target = center + pivotOffset;
            
            // Check if VerticalCameraFollow is configured to prevent downward scrolling
            bool shouldPreventDownward = verticalFollow != null && verticalFollow.GetNeverScrollDown();
            
            // Preserve current (higher) Y if VerticalCameraFollow prevents downward scrolling
            if (shouldPreventDownward)
            {
                target.y = Mathf.Max(target.y, cameraPivot.position.y);
            }
            else if (verticalFollow != null)
            {
                // When VerticalCameraFollow allows downward movement, be more conservative
                // Only make small adjustments to avoid conflicts with the follow system
                float currentY = cameraPivot.position.y;
                float deltaY = target.y - currentY;
                
                // Limit large sudden Y changes to prevent jerking
                const float maxSuddenChange = 2.0f; // Max units to change Y in one frame
                if (Mathf.Abs(deltaY) > maxSuddenChange)
                {
                    target.y = currentY + Mathf.Sign(deltaY) * maxSuddenChange;
                }
            }
            
            cameraPivot.position = target;
        }
        
        public void UpdateRotation(float startYaw, float targetYaw, float progress)
        {
            if (!cameraPivot) return;
            
            float deltaYaw = Mathf.DeltaAngle(startYaw, targetYaw);
            float currentYaw = Mathf.LerpAngle(startYaw, startYaw + deltaYaw, progress);
            
            var eul = cameraPivot.eulerAngles;
            eul.y = currentYaw;
            cameraPivot.eulerAngles = eul;
        }
        
        public void SetCameraDistance(float distance)
        {
            if (!cameraPivot || cameraPivot.childCount == 0) return;
            
            var cam = cameraPivot.GetChild(0);
            cam.localPosition = new Vector3(0f, 0f, -Mathf.Abs(distance));
            cam.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Suspends VerticalCameraFollow during projection switches to prevent conflicts.
        /// </summary>
        public void SuspendVerticalFollow()
        {
            if (verticalFollow != null)
            {
                verticalFollow.SuspendFollowing();
            }
        }

        /// <summary>
        /// Resumes VerticalCameraFollow after projection switches are complete.
        /// </summary>
        public void ResumeVerticalFollow()
        {
            if (verticalFollow != null)
            {
                verticalFollow.ResumeFollowing();
            }
        }
    }
}