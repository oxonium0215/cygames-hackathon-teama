using UnityEngine;

namespace Game.Projection
{
    [System.Serializable]
    public class CameraPivotAdjuster
    {
        [Header("Camera")]
        [SerializeField] private float cameraDistance = 15f;  // Scene has 15, not 10
        [SerializeField] private Vector3 pivotOffset = Vector3.zero;

        [Header("Ground / Projection")]
        [SerializeField] private float snapDownDistance = 5f;
        [SerializeField] private float snapUpAllowance = 0.5f;
        [SerializeField] private float groundSkin = 0.05f;

        public float GroundSkin => groundSkin;
        public float GetCameraDistance() => cameraDistance;

        public void RepositionPivotToCenter(Transform cameraPivot, Transform rotationCenter)
        {
            if (!cameraPivot) return;

            Vector3 center = rotationCenter ? rotationCenter.position : cameraPivot.position - pivotOffset;
            Vector3 target = center + pivotOffset;

            // Do not scroll down: preserve current (higher) Y if applicable.
            target.y = Mathf.Max(target.y, cameraPivot.position.y);

            cameraPivot.position = target;

            if (cameraPivot.childCount > 0)
            {
                var cam = cameraPivot.GetChild(0);
                cam.localPosition = new Vector3(0f, 0f, -Mathf.Abs(cameraDistance));
                cam.localRotation = Quaternion.identity;
            }
        }

        public bool SnapPlayerToGround(Transform playerTransform, LayerMask groundMask)
        {
            if (!playerTransform) return false;

            Vector3 origin = playerTransform.position + Vector3.up * snapUpAllowance;
            float maxDist = snapUpAllowance + snapDownDistance;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDist, groundMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 p = playerTransform.position;
                p.y = hit.point.y + groundSkin;
                playerTransform.position = p;
#if UNITY_2021_2_OR_NEWER
                Physics.SyncTransforms();
#endif
                return true;
            }
            return false;
        }
        
        public void MigrateFrom(float cameraDistance, Vector3 pivotOffset, float snapDownDistance, 
                               float snapUpAllowance, float groundSkin)
        {
            this.cameraDistance = cameraDistance;
            this.pivotOffset = pivotOffset;
            this.snapDownDistance = snapDownDistance;
            this.snapUpAllowance = snapUpAllowance;
            this.groundSkin = groundSkin;
        }
    }
}