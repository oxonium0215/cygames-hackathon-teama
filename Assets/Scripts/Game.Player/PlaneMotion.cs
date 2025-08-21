using UnityEngine;

namespace Game.Player
{
    [System.Serializable]
    public class PlaneMotion
    {
        [Header("Movement (Inertia)")]
        [SerializeField] private float maxRunSpeed = 7f;
        [SerializeField] private float groundAcceleration = 80f;
        [SerializeField] private float airAcceleration = 40f;
        [SerializeField] private float groundDeceleration = 60f;
        [SerializeField] private float airDeceleration = 20f;

        [Header("Gravity")]
        [SerializeField] private float gravity = -30f;
        [SerializeField] private float groundStickForce = 5f;

        // Internal state
        private MovePlane activePlane = MovePlane.X;
        private bool lateralEnabled = true;
        private bool planeLockEnabled;
        private MovePlane planeLockAxis;
        private float planeLockValue;

        public float Gravity => gravity;

        public MovePlane ActivePlane
        {
            get => activePlane;
            set => activePlane = value;
        }

        public void SetLateralEnabled(bool enabled) => lateralEnabled = enabled;

        public void SetPlaneLock(MovePlane axis, float planeConst)
        {
            planeLockEnabled = true;
            planeLockAxis = axis;
            planeLockValue = planeConst;
        }

        public void ApplyPlaneConstraints(Rigidbody rb)
        {
            if (!planeLockEnabled) return;

            Vector3 pos = rb.position;
            if (planeLockAxis == MovePlane.X)
            {
                if (!Mathf.Approximately(pos.z, planeLockValue))
                    rb.position = new Vector3(pos.x, pos.y, planeLockValue);
                if (Mathf.Abs(rb.linearVelocity.z) > 0f)
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, 0f);
            }
            else
            {
                if (!Mathf.Approximately(pos.x, planeLockValue))
                    rb.position = new Vector3(planeLockValue, pos.y, pos.z);
                if (Mathf.Abs(rb.linearVelocity.x) > 0f)
                    rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, rb.linearVelocity.z);
            }
        }

        public RigidbodyConstraints GetAxisConstraints()
        {
            var constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            if (activePlane == MovePlane.X) 
                constraints |= RigidbodyConstraints.FreezePositionZ;
            else 
                constraints |= RigidbodyConstraints.FreezePositionX;
            return constraints;
        }

        public Vector3 UpdateLateralMovement(Vector3 velocity, Vector2 moveInput, bool isGrounded)
        {
            // Lateral control
            float input = lateralEnabled ? Mathf.Clamp(moveInput.x, -1f, 1f) : 0f;
            float desired = input * maxRunSpeed;

            bool hasInput = Mathf.Abs(input) > 0.01f;
            float response = hasInput
                ? (isGrounded ? groundAcceleration : airAcceleration)
                : (isGrounded ? groundDeceleration : airDeceleration);

            if (activePlane == MovePlane.X)
                velocity.x = Mathf.MoveTowards(velocity.x, desired, response * Time.fixedDeltaTime);
            else
                velocity.z = Mathf.MoveTowards(velocity.z, desired, response * Time.fixedDeltaTime);

            return velocity;
        }

        public Vector3 UpdateLateralMovementWithResponse(Vector3 velocity, Vector2 moveInput, bool isGrounded, float responseMultiplier)
        {
            // Lateral control
            float input = lateralEnabled ? Mathf.Clamp(moveInput.x, -1f, 1f) : 0f;
            float desired = input * maxRunSpeed;

            bool hasInput = Mathf.Abs(input) > 0.01f;
            float response = hasInput
                ? (isGrounded ? groundAcceleration : airAcceleration)
                : (isGrounded ? groundDeceleration : airDeceleration);

            response *= responseMultiplier;

            if (activePlane == MovePlane.X)
                velocity.x = Mathf.MoveTowards(velocity.x, desired, response * Time.fixedDeltaTime);
            else
                velocity.z = Mathf.MoveTowards(velocity.z, desired, response * Time.fixedDeltaTime);

            return velocity;
        }

        public Vector3 ApplyGravityAndGroundStick(Vector3 velocity, bool isGrounded)
        {
            // Gravity + stick
            velocity.y += gravity * Time.fixedDeltaTime;
            if (isGrounded && velocity.y < 0f)
                velocity.y -= groundStickForce * Time.fixedDeltaTime;

            return velocity;
        }

        public float GetLateralSpeed(Vector3 velocity)
        {
            return (activePlane == MovePlane.X) ? velocity.x : velocity.z;
        }

        public Vector3 SetLateralSpeed(Vector3 velocity, float speed)
        {
            if (activePlane == MovePlane.X) 
                velocity.x = speed; 
            else 
                velocity.z = speed;
            return velocity;
        }

        public void ValidateParameters()
        {
            maxRunSpeed = Mathf.Max(0f, maxRunSpeed);
            groundAcceleration = Mathf.Max(0f, groundAcceleration);
            airAcceleration = Mathf.Max(0f, airAcceleration);
            groundDeceleration = Mathf.Max(0f, groundDeceleration);
            airDeceleration = Mathf.Max(0f, airDeceleration);
            gravity = Mathf.Min(0f, gravity);
            groundStickForce = Mathf.Max(0f, groundStickForce);
        }
        
        public void MigrateFrom(float maxRunSpeed, float groundAcceleration, float airAcceleration, 
                               float groundDeceleration, float airDeceleration, float gravity, float groundStickForce)
        {
            this.maxRunSpeed = maxRunSpeed;
            this.groundAcceleration = groundAcceleration;
            this.airAcceleration = airAcceleration;
            this.groundDeceleration = groundDeceleration;
            this.airDeceleration = airDeceleration;
            this.gravity = gravity;
            this.groundStickForce = groundStickForce;
        }
    }
}