using UnityEngine;

namespace Game.Camera
{
    // - Only moves along Y
    // - Moves up when the player exceeds a top dead zone window
    // - Never moves down (keeps the highest Y reached)
    // - Optional smoothing
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

        // Pure utilities (façade pattern)
        private TopDeadZonePolicy _deadZonePolicy;
        private ConstantSpeedSmoothing _constantSpeedSmoothing;

        // Parameter snapshots for change detection (performance optimization)
        private float _lastTopDeadZone = float.NaN;
        private float _lastUpSpeed = float.NaN;
        private bool _lastUseSmoothDamp;
        private float _lastSmoothTime = float.NaN;
        private float _lastSmoothMaxSpeed = float.NaN;

        void Awake()
        {
            _maxPivotY = transform.position.y;
            
            // Initialize pure utilities
            EnsurePoliciesUpToDate();
        }

        void OnValidate()
        {
            // Enforce sane parameter ranges
            topDeadZone = Mathf.Max(0f, topDeadZone);
            upSpeed = Mathf.Max(0.01f, upSpeed);
            smoothTime = Mathf.Max(0.01f, smoothTime);
            smoothMaxSpeed = Mathf.Max(0.01f, smoothMaxSpeed);

            // Trigger policy refresh in Edit mode
            EnsurePoliciesUpToDate();
        }

        /// <summary>
        /// Ensures policies are up to date, recreating them only when parameters have changed.
        /// This prevents per-frame allocations while maintaining correct behavior.
        /// </summary>
        private void EnsurePoliciesUpToDate()
        {
            // Check if dead zone policy needs updating
            if (_deadZonePolicy == null || _lastTopDeadZone != topDeadZone)
            {
                _deadZonePolicy = new TopDeadZonePolicy(topDeadZone);
                _lastTopDeadZone = topDeadZone;
            }

            // Check if constant speed smoothing needs updating
            if (_constantSpeedSmoothing == null || _lastUpSpeed != upSpeed)
            {
                _constantSpeedSmoothing = new ConstantSpeedSmoothing(upSpeed);
                _lastUpSpeed = upSpeed;
            }

            // Update smoothing parameter snapshots (used for SmoothDamp)
            _lastUseSmoothDamp = useSmoothDamp;
            _lastSmoothTime = smoothTime;
            _lastSmoothMaxSpeed = smoothMaxSpeed;
        }

        void LateUpdate()
        {
            if (!followTarget) return;

            // Ensure policies are up to date (only recreates if parameters changed)
            EnsurePoliciesUpToDate();

            // Downward scrolling off
            var pos = transform.position;
            if (neverScrollDown && pos.y < _maxPivotY)
            {
                pos.y = _maxPivotY;
                transform.position = pos;
            }

            // Check if player is above the top dead zone using dead zone policy
            float pivotY = transform.position.y;
            float playerY = followTarget.position.y;
            float thresholdY = _deadZonePolicy.ComputeThreshold(pivotY);

            if (playerY > thresholdY)
            {
                // Desired Y keeps the player exactly at the top edge of the dead zone
                float desiredY = _deadZonePolicy.ComputeDesiredY(playerY);
                float newY;

                if (useSmoothDamp)
                {
                    newY = Mathf.SmoothDamp(pivotY, desiredY, ref _velY, smoothTime, smoothMaxSpeed, Time.deltaTime);
                    // SmoothDamp may overshoot downwards if desired decreases
                    if (neverScrollDown) newY = Mathf.Max(newY, pivotY);
                } else
                {
                    newY = _constantSpeedSmoothing.ComputeNewY(pivotY, desiredY, Time.deltaTime);
                }

                // Enforce upward-only rule
                if (neverScrollDown) newY = Mathf.Max(newY, pivotY);

                pos.y = newY;
                transform.position = pos;

                // Update the max pivot Y reached
                if (neverScrollDown && newY > _maxPivotY)
                    _maxPivotY = newY;
            } else
            {
                // Player is inside or below the dead zone
                if (neverScrollDown)
                {
                    // If something external lowered player , bounce back up to max
                    if (transform.position.y < _maxPivotY)
                    {
                        pos.y = _maxPivotY;
                        transform.position = pos;
                    }
                }
            }
        }

        // If some system needs to override the current 'highest Y' (e.g. when loading a checkpoint),
        // call this to set a new floor for the camera Y
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