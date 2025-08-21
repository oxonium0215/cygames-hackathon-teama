using UnityEngine;

namespace Game.Player
{
    [System.Serializable]
    public class GroundProbe
    {
        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.2f;  // Scene has 0.2, not 0.15
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private float groundCheckSkin = 0.02f;
        [SerializeField] private bool autoSizeGroundCheck = true;

        public bool IsGrounded { get; private set; }

        public void Initialize(Transform hostTransform)
        {
            EnsureGroundCheckExists(hostTransform);
        }

        public void UpdateGroundCheck(Rigidbody rb, Collider col)
        {
            if (!col || !groundCheck) return;

            UpdateGroundCheckPoseAndSize(col);
            
            // Perform ground check
            IsGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
        }

        private void EnsureGroundCheckExists(Transform hostTransform)
        {
            if (groundCheck != null) return;
            var gc = new GameObject("GroundCheck");
            groundCheck = gc.transform;
            groundCheck.SetParent(hostTransform, worldPositionStays: true);
        }

        private void UpdateGroundCheckPoseAndSize(Collider col)
        {
            if (!col || !groundCheck) return;

            var b = col.bounds; // world AABB
            Vector3 footWorld = new Vector3(b.center.x, b.min.y + groundCheckSkin, b.center.z);
            groundCheck.position = footWorld;

            if (autoSizeGroundCheck)
            {
                float suggested;
                if (col is CapsuleCollider cap)
                {
                    float scaleXZ = Mathf.Max(Mathf.Abs(col.transform.lossyScale.x), Mathf.Abs(col.transform.lossyScale.z));
                    suggested = Mathf.Clamp(cap.radius * scaleXZ * 0.6f, 0.04f, 0.5f);
                }
#if UNITY_6_0_OR_NEWER
                else if (col is CharacterController cc)
                {
                    suggested = Mathf.Clamp(cc.radius * 0.6f, 0.04f, 0.5f);
                }
#endif
                else
                {
                    float footprint = Mathf.Min(b.extents.x, b.extents.z);
                    suggested = Mathf.Clamp(footprint * 0.5f, 0.04f, 0.5f);
                }
                groundCheckRadius = suggested;
            }
        }

        public void ValidateParameters()
        {
            groundCheckRadius = Mathf.Clamp(groundCheckRadius, 0.01f, 1f);
            groundCheckSkin = Mathf.Clamp(groundCheckSkin, 0f, 0.2f);
        }
        
        public void MigrateFrom(Transform groundCheck, float groundCheckRadius, LayerMask groundMask, 
                               float groundCheckSkin, bool autoSizeGroundCheck)
        {
            this.groundCheck = groundCheck;
            this.groundCheckRadius = groundCheckRadius;
            this.groundMask = groundMask;
            this.groundCheckSkin = groundCheckSkin;
            this.autoSizeGroundCheck = autoSizeGroundCheck;
        }
    }
}