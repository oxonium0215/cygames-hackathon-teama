using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

[MovedFrom("POC.Gameplay")]
namespace Game.Player
{
    [MovedFrom("POC.Gameplay")]
    public enum MovePlane { X, Z }

    [MovedFrom("POC.Gameplay")]
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Movement (Inertia)")]
        [SerializeField] private float maxRunSpeed = 7f;
        [SerializeField] private float groundAcceleration = 80f;
        [SerializeField] private float airAcceleration = 40f;
        [SerializeField] private float groundDeceleration = 60f;
        [SerializeField] private float airDeceleration = 20f;

        [Header("Jumping")]
        [SerializeField] private float jumpHeight = 3.5f;
        [Range(0.1f, 1f)][SerializeField] private float jumpCutMultiplier = 0.5f;
        [SerializeField] private float coyoteTime = 0.1f;
        [SerializeField] private float jumpBufferTime = 0.1f;

        [Header("Gravity")]
        [SerializeField] private float gravity = -30f;
        [SerializeField] private float groundStickForce = 5f;

        [Header("Landing Slide")]
        [SerializeField] private bool enableLandingSlide = true;
        [SerializeField] private float landingSlideDuration = 0.18f;
        [SerializeField] private float landingMinFallSpeed = 2.0f;
        [Range(0.1f, 1f)][SerializeField] private float landingDecelMultiplier = 0.35f;
        [Range(0.3f, 1f)][SerializeField] private float landingAccelMultiplier = 0.7f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private float groundCheckSkin = 0.02f;
        [SerializeField] private bool autoSizeGroundCheck = true;

        private Rigidbody rb;
        private Collider col;

        // Input/state
        private Vector2 moveInput;
        private bool jumpQueued;
        private bool jumpHeld;

        // Timers
        private float lastGroundedTimer;
        private float jumpBufferTimer;

        // Landing slide
        private float landingSlideTimer;
        private float lastVelY;

        // Lateral axis logic
        private MovePlane activePlane = MovePlane.X;
        private bool lateralEnabled = true;

        // Continuous physics projection lock (keeps body on plane)
        private bool planeLockEnabled;
        private MovePlane planeLockAxis;
        private float planeLockValue;

        // Rotation freeze (no gravity/inertia/velocity changes while true)
        private bool rotationFrozen;

        public bool IsGrounded { get; private set; }
        private float JumpVelocity => Mathf.Sqrt(2f * Mathf.Abs(gravity) * Mathf.Max(0.0001f, jumpHeight));

        public MovePlane ActivePlane
        {
            get => activePlane;
            set
            {
                if (activePlane != value)
                {
                    activePlane = value;
                    ApplyAxisConstraints();
                }
            }
        }

        public void SetLateralEnabled(bool enabled) => lateralEnabled = enabled;

        public void SetPlaneLock(MovePlane axis, float planeConst)
        {
            planeLockEnabled = true;
            planeLockAxis = axis;
            planeLockValue = planeConst;
            ApplyAxisConstraints();
            var p = transform.position;
            if (axis == MovePlane.X) p.z = planeConst; else p.x = planeConst;
            transform.position = p;
        }

        public void BeginRotationFreeze()
        {
            rotationFrozen = true;
            lateralEnabled = false;
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

            EnsureGroundCheckExists();
            UpdateGroundCheckPoseAndSize();
            ApplyAxisConstraints();
        }

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

        public void SetMove(Vector2 move) => moveInput = move;

        public void QueueJump()
        {
            if (rotationFrozen) return;
            jumpQueued = true;
            jumpHeld = true;
            jumpBufferTimer = jumpBufferTime;
        }

        public void JumpCanceled()
        {
            if (rotationFrozen) return;
            var v = rb.linearVelocity;
            if (v.y > 0f)
            {
                v.y *= jumpCutMultiplier;
                rb.linearVelocity = v;
            }
        }

        private void Update()
        {
            UpdateGroundCheckPoseAndSize();

            if (rotationFrozen)
            {
                return;
            }

            if (IsGrounded) lastGroundedTimer = coyoteTime;
            else lastGroundedTimer -= Time.deltaTime;

            if (!jumpQueued) jumpBufferTimer -= Time.deltaTime;
        }

        private void FixedUpdate()
        {
            // IMPORTANT: do nothing while rotating; the manager drives transform/overlaps.
            if (rotationFrozen) return;

            // Keep on plane always
            if (planeLockEnabled)
            {
                Vector3 pos = rb.position;
                if (planeLockAxis == MovePlane.X)
                {
                    if (!Mathf.Approximately(pos.z, planeLockValue))
                        rb.position = new Vector3(pos.x, pos.y, planeLockValue);
                    if (Mathf.Abs(rb.linearVelocity.z) > 0f)
                        rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, 0f);
                } else
                {
                    if (!Mathf.Approximately(pos.x, planeLockValue))
                        rb.position = new Vector3(planeLockValue, pos.y, pos.z);
                    if (Mathf.Abs(rb.linearVelocity.x) > 0f)
                        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, rb.linearVelocity.z);
                }
            }

            // Ground check each step
            IsGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);

            var v = rb.linearVelocity;

            // Landing slide trigger
            if (enableLandingSlide && IsGrounded && lastVelY < 0f && Mathf.Abs(lastVelY) >= landingMinFallSpeed)
                landingSlideTimer = landingSlideDuration;

            // Lateral control
            float input = lateralEnabled ? Mathf.Clamp(moveInput.x, -1f, 1f) : 0f;
            float desired = input * maxRunSpeed;

            bool hasInput = Mathf.Abs(input) > 0.01f;
            float response = hasInput
                ? (IsGrounded ? groundAcceleration : airAcceleration)
                : (IsGrounded ? groundDeceleration : airDeceleration);

            if (enableLandingSlide && IsGrounded && landingSlideTimer > 0f)
            {
                response *= hasInput ? landingAccelMultiplier : landingDecelMultiplier;
                landingSlideTimer -= Time.fixedDeltaTime;
            }

            if (activePlane == MovePlane.X)
                v.x = Mathf.MoveTowards(v.x, desired, response * Time.fixedDeltaTime);
            else
                v.z = Mathf.MoveTowards(v.z, desired, response * Time.fixedDeltaTime);

            // Gravity + stick
            v.y += gravity * Time.fixedDeltaTime;
            if (IsGrounded && v.y < 0f)
                v.y -= groundStickForce * Time.fixedDeltaTime;

            // Buffered jump + coyote
            if (jumpBufferTimer > 0f && lastGroundedTimer > 0f)
            {
                v.y = JumpVelocity;
                jumpBufferTimer = 0f;
                lastGroundedTimer = 0f;
                landingSlideTimer = 0f;
            }
            jumpQueued = false;

            rb.linearVelocity = v;
            lastVelY = v.y;
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

            var b = col.bounds; // world AABB
            Vector3 footWorld = new Vector3(b.center.x, b.min.y + groundCheckSkin, b.center.z);
            groundCheck.position = footWorld;

            if (autoSizeGroundCheck)
            {
                float suggested;
                if (col is CapsuleCollider cap)
                {
                    float scaleXZ = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
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

        private void ApplyAxisConstraints()
        {
            var constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            if (activePlane == MovePlane.X) constraints |= RigidbodyConstraints.FreezePositionZ;
            else constraints |= RigidbodyConstraints.FreezePositionX;
            rb.constraints = constraints;
        }

        // Utilities
        public float GetLateralSpeed()
        {
            var v = rb.linearVelocity;
            return (activePlane == MovePlane.X) ? v.x : v.z;
        }

        public void SetLateralSpeed(float speed)
        {
            var v = rb.linearVelocity;
            if (activePlane == MovePlane.X) v.x = speed; else v.z = speed;
            rb.linearVelocity = v;
        }
    }
}