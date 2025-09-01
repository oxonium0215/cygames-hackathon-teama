using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Handles ground and air movement for the player
    /// </summary>
    public class PlayerMovement
    {
        private readonly Rigidbody rb;
        private readonly PlaneMotion planeMotion;
        
        // Movement settings
        private readonly float maxRunSpeed;
        private readonly float groundAcceleration;
        private readonly float airAcceleration;
        private readonly float groundDeceleration;
        private readonly float airDeceleration;
        
        // State
        private Vector2 moveInput;
        private MovePlane activePlane = MovePlane.X;
        private bool lateralEnabled = true;
        
        // Plane locking
        private bool planeLockEnabled;
        private MovePlane planeLockAxis;
        private float planeLockValue;
        
        // Cached vectors for performance
        private Vector3 tempVector3;
        
        public PlayerMovement(Rigidbody rigidbody, float maxSpeed, float groundAccel, float airAccel, 
            float groundDecel, float airDecel, PlaneMotion planeMotionService)
        {
            rb = rigidbody;
            maxRunSpeed = maxSpeed;
            groundAcceleration = groundAccel;
            airAcceleration = airAccel;
            groundDeceleration = groundDecel;
            airDeceleration = airDecel;
            planeMotion = planeMotionService;
        }
        
        public MovePlane ActivePlane
        {
            get => activePlane;
            set => activePlane = value;
        }
        
        public void SetMove(Vector2 move) => moveInput = move;
        public void SetLateralEnabled(bool enabled) => lateralEnabled = enabled;
        
        public void SetPlaneLock(MovePlane axis, float planeConst)
        {
            planeLockEnabled = true;
            planeLockAxis = axis;
            planeLockValue = planeConst;
        }
        
        public void ApplyMovement(bool isGrounded, float deltaTime)
        {
            // Keep on plane always
            if (planeLockEnabled)
            {
                ApplyPlaneLock();
            }
            
            var velocity = rb.linearVelocity;
            
            // Apply lateral movement through PlaneMotion service
            velocity = planeMotion?.ApplyLateralMovement(moveInput, activePlane, maxRunSpeed, velocity, isGrounded,
                groundAcceleration, airAcceleration, groundDeceleration, airDeceleration,
                deltaTime, lateralEnabled) ?? velocity;
                
            rb.linearVelocity = velocity;
        }
        
        private void ApplyPlaneLock()
        {
            Vector3 pos = rb.position;
            if (planeLockAxis == MovePlane.X)
            {
                if (!Mathf.Approximately(pos.z, planeLockValue))
                {
                    tempVector3.x = pos.x;
                    tempVector3.y = pos.y;
                    tempVector3.z = planeLockValue;
                    rb.position = tempVector3;
                }
                if (Mathf.Abs(rb.linearVelocity.z) > 0f)
                {
                    var vel = rb.linearVelocity;
                    tempVector3.x = vel.x;
                    tempVector3.y = vel.y;
                    tempVector3.z = 0f;
                    rb.linearVelocity = tempVector3;
                }
            }
            else
            {
                if (!Mathf.Approximately(pos.x, planeLockValue))
                {
                    tempVector3.x = planeLockValue;
                    tempVector3.y = pos.y;
                    tempVector3.z = pos.z;
                    rb.position = tempVector3;
                }
                if (Mathf.Abs(rb.linearVelocity.x) > 0f)
                {
                    var vel = rb.linearVelocity;
                    tempVector3.x = 0f;
                    tempVector3.y = vel.y;
                    tempVector3.z = vel.z;
                    rb.linearVelocity = tempVector3;
                }
            }
        }
        
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