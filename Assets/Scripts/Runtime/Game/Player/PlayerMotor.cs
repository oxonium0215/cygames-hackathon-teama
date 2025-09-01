using UnityEngine;

namespace Game.Player
{
    public enum MovePlane { X, Z }

    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public partial class PlayerMotor : MonoBehaviour
    {
        [Header("Movement (Inertia)")]
        [SerializeField] private float maxRunSpeed = 9f;
        [SerializeField] private float groundAcceleration = 80f;
        [SerializeField] private float airAcceleration = 40f;
        [SerializeField] private float groundDeceleration = 60f;
        [SerializeField] private float airDeceleration = 20f;

        [Header("Jumping")]
        [SerializeField] private float jumpHeight = 3.5f;
        [Range(0.1f, 1f)][SerializeField] private float jumpCutMultiplier = 0.5f;
        [SerializeField] private float coyoteTime = 0.1f;
        [SerializeField] private float jumpBufferTime = 0.1f;

        [Header("Spring Jumping")]

        [SerializeField] private LayerMask _springMask;
        [SerializeField] private float _springJumpHeight = 9.5f;

        [Header("Gravity")]
        [SerializeField] private float gravity = -40f;
        [SerializeField] private float groundStickForce = 5f;

        [Header("Landing Slide")]
        [SerializeField] private bool enableLandingSlide = true;
        [SerializeField] private float landingSlideDuration = 0.18f;
        [SerializeField] private float landingMinFallSpeed = 2.0f;
        [Range(0.1f, 1f)][SerializeField] private float landingDecelMultiplier = 0.35f;
        [Range(0.3f, 1f)][SerializeField] private float landingAccelMultiplier = 0.7f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.2f;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private float groundCheckSkin = 0.02f;
        [SerializeField] private bool autoSizeGroundCheck = true;

        [Header("GameOver")]
        [SerializeField] private float gameOverBorder = -10f;

        private Rigidbody rb;
        private Collider col;

        // Player components
        private PlayerMovement playerMovement;
        private PlayerJumping playerJumping;
        private PlayerCollision playerCollision;

        // Services
        private GroundProbe groundProbe;
        private PlaneMotion planeMotion;

        // Rotation freeze (no gravity/inertia/velocity changes while true)
        private bool rotationFrozen;

        public bool IsGrounded => groundProbe?.IsGrounded ?? false;
        public bool OnSpring => groundProbe?.OnSpring ?? false;
        private bool _autoJump = true;

        private Transform _lastCheckPoint;

        public MovePlane ActivePlane
        {
            get => playerMovement?.ActivePlane ?? MovePlane.X;
            set
            {
                if (playerMovement != null && playerMovement.ActivePlane != value)
                {
                    playerMovement.ActivePlane = value;
                    ApplyAxisConstraints();
                }
            }
        }

        public void SetJumpingEnabled(bool enabled) => playerJumping?.SetJumpingEnabled(enabled);

        public void SetLateralEnabled(bool enabled) => playerMovement?.SetLateralEnabled(enabled);

        public void SetPlaneLock(MovePlane axis, float planeConst)
        {
            playerMovement?.SetPlaneLock(axis, planeConst);
            ApplyAxisConstraints();
            var p = transform.position;
            if (axis == MovePlane.X) p.z = planeConst; else p.x = planeConst;
            transform.position = p;
        }

        public void BeginRotationFreeze()
        {
            rotationFrozen = true;
            playerMovement?.SetLateralEnabled(false);
        }

        public void EndRotationFreeze()
        {
            rotationFrozen = false;
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();

            rb.useGravity = false; // custom gravity
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Initialize services
            groundProbe = new GroundProbe(coyoteTime);
            planeMotion = new PlaneMotion(enableLandingSlide, landingSlideDuration,
                landingAccelMultiplier, landingDecelMultiplier);
                
            // Initialize player components
            playerCollision = new PlayerCollision(transform, col, groundProbe, groundMask, _springMask, 
                gameOverBorder, autoSizeGroundCheck, groundCheckSkin, groundCheckRadius);
            playerMovement = new PlayerMovement(rb, maxRunSpeed, groundAcceleration, airAcceleration, 
                groundDeceleration, airDeceleration, planeMotion);
            playerJumping = new PlayerJumping(rb, groundProbe, jumpHeight, jumpCutMultiplier, _springJumpHeight,
                jumpBufferTime, gravity, groundStickForce, landingMinFallSpeed);

            groundCheck = playerCollision.GroundCheck;
            ApplyAxisConstraints();
        }
        
        // Expose ground check radius for external access
        public float GroundCheckRadius => playerCollision?.GroundCheckRadius ?? groundCheckRadius;

        private void OnValidate()
        {
            maxRunSpeed = Mathf.Max(0f, maxRunSpeed);
            groundAcceleration = Mathf.Max(0f, groundAcceleration);
            airAcceleration = Mathf.Max(0f, airAcceleration);
            groundDeceleration = Mathf.Max(0f, groundDeceleration);
            airDeceleration = Mathf.Max(0f, airDeceleration);
            gravity = Mathf.Min(0f, gravity);
            groundStickForce = Mathf.Max(0f, groundStickForce);
            groundCheckRadius = Mathf.Clamp(groundCheckRadius, 0.01f, 1f);
            groundCheckSkin = Mathf.Clamp(groundCheckSkin, 0f, 0.2f);
        }

        public void SetMove(Vector2 move) => playerMovement?.SetMove(move);

        public void QueueJump()
        {
            playerJumping?.QueueJump(rotationFrozen);
        }

        public void JumpCanceled()
        {
            playerJumping?.JumpCanceled(rotationFrozen);
        }

        private void Update()
        {
            if (rotationFrozen)
            {
                return;
            }

            // Update collision and ground checking
            playerCollision?.UpdateGroundCheck(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            // Do nothing while rotating; the manager drives transform/overlaps.
            if (rotationFrozen) return;

            // Check game over boundary
            playerCollision?.CheckGameOverBoundary();

            // Update landing slide logic
            playerJumping?.UpdateLandingSlide(IsGrounded, planeMotion, Time.fixedDeltaTime);

            // Apply movement
            playerMovement?.ApplyMovement(IsGrounded, Time.fixedDeltaTime);

            // Apply gravity and jumping
            playerJumping?.ApplyGravityAndJumping(IsGrounded, OnSpring, planeMotion, Time.fixedDeltaTime);
        }

        private void ApplyAxisConstraints()
        {
            var constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            if (ActivePlane == MovePlane.X) constraints |= RigidbodyConstraints.FreezePositionZ;
            else constraints |= RigidbodyConstraints.FreezePositionX;
            rb.constraints = constraints;
        }

        // Utilities
        public float GetLateralSpeed()
        {
            return playerMovement?.GetLateralSpeed() ?? 0f;
        }

        public void SetLateralSpeed(float speed)
        {
            playerMovement?.SetLateralSpeed(speed);
        }
        
        private void OnTriggerEnter(Collider other)
        {
            playerCollision?.OnTriggerEnter(other);
        }

        public void Respawn()
        {
            playerCollision?.Respawn();
        }
    }
}