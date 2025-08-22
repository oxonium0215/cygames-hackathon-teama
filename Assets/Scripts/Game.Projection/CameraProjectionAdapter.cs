using UnityEngine;
using Game.Camera;

namespace Game.Projection
{
    /// <summary>
    /// Handles camera pivot adjustments during projection switches.
    /// </summary>
    public class CameraProjectionAdapter
    {
        private readonly Transform cameraPivot;
        
        public CameraProjectionAdapter(Transform cameraPivot)
        {
            this.cameraPivot = cameraPivot;
        }
        
        public void RepositionPivotToCenter(Transform rotationCenter, Vector3 pivotOffset)
        {
            if (!cameraPivot) return;
            
            Vector3 center = rotationCenter ? rotationCenter.position : cameraPivot.position - pivotOffset;
            Vector3 target = center + pivotOffset;
            
            // Check if VerticalCameraFollow is configured to prevent downward scrolling
            var verticalFollow = cameraPivot.GetComponent<VerticalCameraFollow>();
            bool shouldPreventDownward = verticalFollow != null && verticalFollow.GetNeverScrollDown();
            
            // Preserve current (higher) Y if VerticalCameraFollow prevents downward scrolling
            if (shouldPreventDownward)
            {
                target.y = Mathf.Max(target.y, cameraPivot.position.y);
            }
            else if (verticalFollow != null)
            {
                // When VerticalCameraFollow allows downward movement, make Y changes more gradual
                // to avoid conflicts with the follow system during perspective transitions
                float currentY = cameraPivot.position.y;
                float deltaY = target.y - currentY;
                
                // Limit large sudden Y changes to prevent jerking during transitions
                const float maxYChangePerFrame = 1.5f;
                if (Mathf.Abs(deltaY) > maxYChangePerFrame)
                {
                    target.y = currentY + Mathf.Sign(deltaY) * maxYChangePerFrame;
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
    }
}