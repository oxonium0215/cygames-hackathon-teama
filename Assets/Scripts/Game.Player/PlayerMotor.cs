using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Game.Player
{
    [MovedFrom(true, sourceNamespace: "POC.Gameplay")]
    public enum MovePlane { X, Z }

    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    [MovedFrom(true, sourceNamespace: "POC.Gameplay")]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Player Services")]
        [SerializeField] private GroundProbe groundProbe = new GroundProbe();
        [SerializeField] private PlaneMotion planeMotion = new PlaneMotion();
        [SerializeField] private JumpLogic jumpLogic = new JumpLogic();
        
        // Legacy fields - marked obsolete but kept for serialization compatibility  
        [System.Obsolete("Deprecated; no longer used - moved to PlaneMotion service")]
        [SerializeField] private float maxRunSpeed = 7f;
        [System.Obsolete("Deprecated; no longer used - moved to PlaneMotion service")]
        [SerializeField] private float groundAcceleration = 80f;
        [System.Obsolete("Deprecated; no longer used - moved to PlaneMotion service")]
        [SerializeField] private float airAcceleration = 40f;
        [System.Obsolete("Deprecated; no longer used - moved to PlaneMotion service")]
        [SerializeField] private float groundDeceleration = 60f;
        [System.Obsolete("Deprecated; no longer used - moved to PlaneMotion service")]
        [SerializeField] private float airDeceleration = 20f;
        [System.Obsolete("Deprecated; no longer used - moved to JumpLogic service")]
        [SerializeField] private float jumpHeight = 3.5f;
        [System.Obsolete("Deprecated; no longer used - moved to JumpLogic service")]
        [Range(0.1f, 1f)][SerializeField] private float jumpCutMultiplier = 0.5f;
        [System.Obsolete("Deprecated; no longer used - moved to JumpLogic service")]
        [SerializeField] private float coyoteTime = 0.1f;
        [System.Obsolete("Deprecated; no longer used - moved to JumpLogic service")]
        [SerializeField] private float jumpBufferTime = 0.1f;
        [System.Obsolete("Deprecated; no longer used - moved to PlaneMotion service")]
        [SerializeField] private float gravity = -30f;
        [System.Obsolete("Deprecated; no longer used - moved to PlaneMotion service")]
        [SerializeField] private float groundStickForce = 5f;
        [System.Obsolete("Deprecated; no longer used - moved to JumpLogic service")]
        [SerializeField] private bool enableLandingSlide = true;
        [System.Obsolete("Deprecated; no longer used - moved to JumpLogic service")]
        [SerializeField] private float landingSlideDuration = 0.18f;
        [System.Obsolete("Deprecated; no longer used - moved to JumpLogic service")]
        [SerializeField] private float landingMinFallSpeed = 2.0f;
        [System.Obsolete("Deprecated; no longer used - moved to JumpLogic service")]
        [Range(0.1f, 1f)][SerializeField] private float landingDecelMultiplier = 0.35f;
        [System.Obsolete("Deprecated; no longer used - moved to JumpLogic service")]
        [Range(0.3f, 1f)][SerializeField] private float landingAccelMultiplier = 0.7f;
        [System.Obsolete("Deprecated; no longer used - moved to GroundProbe service")]
        [SerializeField] private Transform groundCheck;
        [System.Obsolete("Deprecated; no longer used - moved to GroundProbe service")]
        [SerializeField] private float groundCheckRadius = 0.15f;
        [System.Obsolete("Deprecated; no longer used - moved to GroundProbe service")]
        [SerializeField] private LayerMask groundMask;
        [System.Obsolete("Deprecated; no longer used - moved to GroundProbe service")]
        [SerializeField] private float groundCheckSkin = 0.02f;
        [System.Obsolete("Deprecated; no longer used - moved to GroundProbe service")]
        [SerializeField] private bool autoSizeGroundCheck = true;

        private Rigidbody rb;
        private Collider col;
        
        // Migration tracking
        [SerializeField, HideInInspector] private bool hasDataMigrated = false;

        // Input/state
        private Vector2 moveInput;

        // Rotation freeze (no gravity/inertia/velocity changes while true)
        private bool rotationFrozen;

        public bool IsGrounded => groundProbe.IsGrounded;

        public MovePlane ActivePlane
        {
            get => planeMotion.ActivePlane;
            set
            {
                if (planeMotion.ActivePlane != value)
                {
                    planeMotion.ActivePlane = value;
                    ApplyAxisConstraints();
                }
            }
        }

        public void SetLateralEnabled(bool enabled) => planeMotion.SetLateralEnabled(enabled);

        public void SetPlaneLock(MovePlane axis, float planeConst)
        {
            planeMotion.SetPlaneLock(axis, planeConst);
            ApplyAxisConstraints();
            var p = transform.position;
            if (axis == MovePlane.X) p.z = planeConst; else p.x = planeConst;
            transform.position = p;
        }

        public void BeginRotationFreeze()
        {
            rotationFrozen = true;
            planeMotion.SetLateralEnabled(false);
        }

        public void EndRotationFreeze()
        {
            rotationFrozen = false;
        }

        private void Awake()
        {
            // Migrate legacy data to services if not done yet
            if (!hasDataMigrated)
            {
                MigrateObsoleteFieldsToServices();
                hasDataMigrated = true;
            }

            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();

            rb.useGravity = false; // custom gravity
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            groundProbe.Initialize(transform);
            ApplyAxisConstraints();
        }

        private void OnValidate()
        {
            groundProbe.ValidateParameters();
            planeMotion.ValidateParameters();
            jumpLogic.ValidateParameters();
        }

        public void SetMove(Vector2 move) => moveInput = move;

        public void QueueJump()
        {
            jumpLogic.QueueJump(rotationFrozen);
        }

        public void JumpCanceled()
        {
            jumpLogic.CancelJump(rb, rotationFrozen);
        }

        private void Update()
        {
            groundProbe.UpdateGroundCheck(rb, col);
            jumpLogic.UpdateTimers(IsGrounded, rotationFrozen);
        }

        private void FixedUpdate()
        {
            // IMPORTANT: do nothing while rotating; the manager drives transform/overlaps.
            if (rotationFrozen) return;

            // Apply plane lock constraints
            planeMotion.ApplyPlaneConstraints(rb);

            var v = rb.linearVelocity;

            // Landing slide handling
            jumpLogic.HandleLandingSlide(IsGrounded, v.y);

            // Lateral control with potential landing slide modification
            float input = Mathf.Clamp(moveInput.x, -1f, 1f);
            bool hasInput = Mathf.Abs(input) > 0.01f;
            float responseMultiplier = 1f;
            jumpLogic.ModifyLateralResponse(IsGrounded, hasInput, ref responseMultiplier);

            if (responseMultiplier != 1f)
            {
                v = planeMotion.UpdateLateralMovementWithResponse(v, moveInput, IsGrounded, responseMultiplier);
            }
            else
            {
                v = planeMotion.UpdateLateralMovement(v, moveInput, IsGrounded);
            }

            // Apply gravity and ground stick
            v = planeMotion.ApplyGravityAndGroundStick(v, IsGrounded);

            // Try to consume jump
            if (jumpLogic.TryConsumeJump(planeMotion.Gravity, out float jumpVel))
            {
                v.y = jumpVel;
            }

            rb.linearVelocity = v;
        }

        private void ApplyAxisConstraints()
        {
            rb.constraints = planeMotion.GetAxisConstraints();
        }

        // Utilities
        public float GetLateralSpeed()
        {
            return planeMotion.GetLateralSpeed(rb.linearVelocity);
        }

        public void SetLateralSpeed(float speed)
        {
            rb.linearVelocity = planeMotion.SetLateralSpeed(rb.linearVelocity, speed);
        }
        
        private void MigrateObsoleteFieldsToServices()
        {
            #pragma warning disable CS0618 // Type or member is obsolete
            // Migrate to PlaneMotion service
            planeMotion.MigrateFrom(maxRunSpeed, groundAcceleration, airAcceleration, 
                                   groundDeceleration, airDeceleration, gravity, groundStickForce);
            
            // Migrate to JumpLogic service
            jumpLogic.MigrateFrom(jumpHeight, jumpCutMultiplier, coyoteTime, jumpBufferTime,
                                 enableLandingSlide, landingSlideDuration, landingMinFallSpeed,
                                 landingDecelMultiplier, landingAccelMultiplier);
            
            // Migrate to GroundProbe service  
            groundProbe.MigrateFrom(groundCheck, groundCheckRadius, groundMask, 
                                   groundCheckSkin, autoSizeGroundCheck);
            #pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}