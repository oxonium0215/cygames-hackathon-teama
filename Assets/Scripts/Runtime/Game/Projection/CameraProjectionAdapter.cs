using UnityEngine;

namespace Game.Projection
{
    /// <summary>
    /// Handles camera pivot adjustments during projection switches.
    /// </summary>
    public class CameraProjectionAdapter
    {
        private readonly Transform cameraPivot;
        private Vector3 tempVector3; // Cached vector to reduce GC allocations
        
        public CameraProjectionAdapter(Transform cameraPivot)
        {
            this.cameraPivot = cameraPivot;
        }
        
        public void RepositionPivotToCenter(Transform rotationCenter, Vector3 pivotOffset)
        {
            if (!cameraPivot) return;
            
            Vector3 center = rotationCenter ? rotationCenter.position : cameraPivot.position - pivotOffset;
            Vector3 target = center + pivotOffset;
            
            // Preserve current Y position to avoid sudden camera jumps during perspective switching.
            // Y movement should be handled by VerticalCameraFollow component.
            target.y = cameraPivot.position.y;
            
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
            tempVector3.x = 0f;
            tempVector3.y = 0f;
            tempVector3.z = -Mathf.Abs(distance);
            cam.localPosition = tempVector3;
            cam.localRotation = Quaternion.identity;
        }
    }
}