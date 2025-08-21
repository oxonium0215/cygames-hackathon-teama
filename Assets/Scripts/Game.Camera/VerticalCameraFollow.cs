using UnityEngine;

namespace Game.Camera
{
    public class TopDeadZonePolicy
    {
        private readonly float topDeadZone;

        public TopDeadZonePolicy(float topDeadZone)
        {
            this.topDeadZone = topDeadZone;
        }

        public float ComputeThreshold(float pivotY)
        {
            return pivotY + topDeadZone;
        }

        public float ComputeDesiredY(float playerY)
        {
            return playerY - topDeadZone;
        }
    }

    public class ConstantSpeedSmoothing
    {
        private readonly float speed;

        public ConstantSpeedSmoothing(float speed)
        {
            this.speed = speed;
        }

        public float ComputeNewY(float currentY, float desiredY, float deltaTime)
        {
            float step = speed * deltaTime;
            return Mathf.MoveTowards(currentY, desiredY, step);
        }
    }

    public class SmoothDampSmoothing
    {
        private readonly float smoothTime;
        private readonly float maxSpeed;
        private float velocity;

        public SmoothDampSmoothing(float smoothTime, float maxSpeed)
        {
            this.smoothTime = smoothTime;
            this.maxSpeed = maxSpeed;
            this.velocity = 0f;
        }

        public float ComputeNewY(float currentY, float desiredY, float deltaTime)
        {
            return Mathf.SmoothDamp(currentY, desiredY, ref velocity, smoothTime, maxSpeed, deltaTime);
        }
    }
    public partial class VerticalCameraFollow : MonoBehaviour
    {
        [Header("Follow")]
        [Tooltip("Transform to follow (usually the Player).")]
        [SerializeField] private Transform followTarget;

        [Header("Dead Zone (World Units)")]
        [Tooltip("Distance above the camera pivot Y before following starts.")]
        [SerializeField, Min(0f)] private float topDeadZone = 3.0f;

        [Header("Motion")]
        [Tooltip("Move speed upwards (units/second) when following.")]
        [SerializeField, Min(0.01f)] private float upSpeed = 10f;
        [Tooltip("Use SmoothDamp instead of constant speed.")]
        [SerializeField] private bool useSmoothDamp = true;
        [Tooltip("Smooth time for SmoothDamp (only if enabled).")]
        [SerializeField, Min(0.01f)] private float smoothTime = 0.15f;
        [Tooltip("Hard cap on how fast we can move up (SmoothDamp only).")]
        [SerializeField, Min(0.01f)] private float smoothMaxSpeed = 30f;

        [Header("Behavior")]
        [Tooltip("Prevents the camera from ever moving down once it has moved up.")]
        [SerializeField] private bool neverScrollDown = true;

        float _maxPivotY;
        float _velY; // for SmoothDamp

        private TopDeadZonePolicy _deadZonePolicy;
        private ConstantSpeedSmoothing _constantSpeedSmoothing;
        private float _lastTopDeadZone = float.NaN;
        private float _lastUpSpeed = float.NaN;
        private bool _lastUseSmoothDamp;
        private float _lastSmoothTime = float.NaN;
        private float _lastSmoothMaxSpeed = float.NaN;

        void Awake()
        {
            _maxPivotY = transform.position.y;
            
            EnsurePoliciesUpToDate();
        }

        void OnValidate()
        {
            topDeadZone = Mathf.Max(0f, topDeadZone);
            upSpeed = Mathf.Max(0.01f, upSpeed);
            smoothTime = Mathf.Max(0.01f, smoothTime);
            smoothMaxSpeed = Mathf.Max(0.01f, smoothMaxSpeed);

            EnsurePoliciesUpToDate();
        }

        private void EnsurePoliciesUpToDate()
        {
            if (_deadZonePolicy == null || _lastTopDeadZone != topDeadZone)
            {
                _deadZonePolicy = new TopDeadZonePolicy(topDeadZone);
                _lastTopDeadZone = topDeadZone;
            }

            if (_constantSpeedSmoothing == null || _lastUpSpeed != upSpeed)
            {
                _constantSpeedSmoothing = new ConstantSpeedSmoothing(upSpeed);
                _lastUpSpeed = upSpeed;
            }
            _lastUseSmoothDamp = useSmoothDamp;
            _lastSmoothTime = smoothTime;
            _lastSmoothMaxSpeed = smoothMaxSpeed;
        }

        void LateUpdate()
        {
            if (!followTarget) return;

            EnsurePoliciesUpToDate();

            // Downward scrolling off
            var pos = transform.position;
            if (neverScrollDown && pos.y < _maxPivotY)
            {
                pos.y = _maxPivotY;
                transform.position = pos;
            }


            float pivotY = transform.position.y;
            float playerY = followTarget.position.y;
            float thresholdY = _deadZonePolicy.ComputeThreshold(pivotY);

            if (playerY > thresholdY)
            {
                float desiredY = _deadZonePolicy.ComputeDesiredY(playerY);
                float newY;

                if (useSmoothDamp)
                {
                    newY = Mathf.SmoothDamp(pivotY, desiredY, ref _velY, smoothTime, smoothMaxSpeed, Time.deltaTime);
                    if (neverScrollDown) newY = Mathf.Max(newY, pivotY);
                } else
                {
                    newY = _constantSpeedSmoothing.ComputeNewY(pivotY, desiredY, Time.deltaTime);
                }

                if (neverScrollDown) newY = Mathf.Max(newY, pivotY);

                pos.y = newY;
                transform.position = pos;
                if (neverScrollDown && newY > _maxPivotY)
                    _maxPivotY = newY;
            } else
            {
                if (neverScrollDown)
                {
                    if (transform.position.y < _maxPivotY)
                    {
                        pos.y = _maxPivotY;
                        transform.position = pos;
                    }
                }
            }
        }

        public void SetFloorToCurrentY()
        {
            _maxPivotY = Mathf.Max(_maxPivotY, transform.position.y);
        }

        public void SetFloor(float y)
        {
            _maxPivotY = Mathf.Max(_maxPivotY, y);
        }
    }
}