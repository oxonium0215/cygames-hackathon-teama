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

        private Rigidbody rb;
        private Collider col;

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
    }
}