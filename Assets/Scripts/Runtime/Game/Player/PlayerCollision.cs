using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Handles collision detection and response for the player
    /// </summary>
    public class PlayerCollision
    {
        private readonly Transform transform;
        private readonly Collider col;
        private readonly GroundProbe groundProbe;
        
        // Ground check settings
        private readonly LayerMask groundMask;
        private readonly LayerMask springMask;
        private readonly float gameOverBorder;
        private readonly bool autoSizeGroundCheck;
        
        // Ground check state
        private Transform groundCheck;
        private float groundCheckRadius;
        private readonly float groundCheckSkin;
        
        // Collision state
        private Transform lastCheckPoint;
        
        // Cached objects to reduce GC allocations
        private Vector3 tempVector3;
        private Bounds cachedBounds;
        
        public PlayerCollision(Transform playerTransform, Collider playerCollider, GroundProbe groundProbeService,
            LayerMask groundMask, LayerMask springMask, float gameOverBorder, 
            bool autoSizeGroundCheck, float groundCheckSkin, float initialGroundCheckRadius)
        {
            transform = playerTransform;
            col = playerCollider;
            groundProbe = groundProbeService;
            this.groundMask = groundMask;
            this.springMask = springMask;
            this.gameOverBorder = gameOverBorder;
            this.autoSizeGroundCheck = autoSizeGroundCheck;
            this.groundCheckSkin = groundCheckSkin;
            this.groundCheckRadius = initialGroundCheckRadius;
            
            EnsureGroundCheckExists();
        }
        
        public Transform GroundCheck => groundCheck;
        public float GroundCheckRadius => groundCheckRadius;
        
        public void UpdateGroundCheck(float deltaTime)
        {
            UpdateGroundCheckPoseAndSize();
            
            // Update ground probe (handles coyote time and jump buffer timing)
            groundProbe?.UpdateGroundCheck(groundCheck.position, groundCheckRadius, groundMask, springMask, deltaTime);
        }
        
        public void CheckGameOverBoundary()
        {
            if (transform.position.y < gameOverBorder)
            {
                Respawn();
            }
        }
        
        public void OnTriggerEnter(Collider other)
        {
            if (other.tag == "CheckPoint")
            {
                lastCheckPoint = other.transform;
            }
            else if (other.tag == "Goal")
            {
                Debug.Log("Goal!");
            }
        }
        
        public void Respawn()
        {
            if (lastCheckPoint != null)
            {
                transform.position = lastCheckPoint.position;
            }
        }
        
        private void EnsureGroundCheckExists()
        {
            if (groundCheck != null) return;
            var gc = new GameObject("GroundCheck");
            groundCheck = gc.transform;
            groundCheck.SetParent(transform, worldPositionStays: true);
        }
        
        private void UpdateGroundCheckPoseAndSize()
        {
            if (!col || !groundCheck) return;

            cachedBounds = col.bounds; // world AABB - cache to avoid repeated property access
            tempVector3.x = cachedBounds.center.x;
            tempVector3.y = cachedBounds.min.y + groundCheckSkin;
            tempVector3.z = cachedBounds.center.z;
            groundCheck.position = tempVector3;

            if (autoSizeGroundCheck)
            {
                float suggested;
                if (col is CapsuleCollider cap)
                {
                    float scaleXZ = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
                    suggested = Mathf.Clamp(cap.radius * scaleXZ * 0.6f, 0.04f, 0.5f);
                }
                else if (col is CharacterController cc)
                {
                    suggested = Mathf.Clamp(cc.radius * 0.6f, 0.04f, 0.5f);
                }
                else
                {
                    float footprint = Mathf.Min(cachedBounds.extents.x, cachedBounds.extents.z);
                    suggested = Mathf.Clamp(footprint * 0.5f, 0.04f, 0.5f);
                }
                groundCheckRadius = suggested;
            }
        }
    }
}