using UnityEngine;

namespace Game.Camera
{
    // - Only moves along Y
    // - Moves up when the player exceeds a top dead zone window
    // - Can move down when the player falls below a bottom dead zone window
    // - Optional smoothing
    public partial class VerticalCameraFollow : MonoBehaviour
    {
        [Header("Follow")]
        [Tooltip("Transform to follow (usually the Player).")]
        [SerializeField] private Transform followTarget;

        [Header("Dead Zone (World Units)")]
        [Tooltip("Distance above the camera pivot Y before following starts.")]
        [SerializeField, Min(0f)] private float topDeadZone = 3.0f;
        [Tooltip("Distance below the camera pivot Y before following starts downward.")]
        [SerializeField, Min(0f)] private float bottomDeadZone = 3.0f;

        [Header("Motion")]
        [Tooltip("Move speed upwards (units/second) when following.")]
        [SerializeField, Min(0.01f)] private float upSpeed = 10f;
        [Tooltip("Move speed downwards (units/second) when following.")]
        [SerializeField, Min(0.01f)] private float downSpeed = 8f;
        [Tooltip("Use SmoothDamp instead of constant speed.")]
        [SerializeField] private bool useSmoothDamp = true;
        [Tooltip("Smooth time for SmoothDamp (only if enabled).")]
        [SerializeField, Min(0.01f)] private float smoothTime = 0.15f;
        [Tooltip("Hard cap on how fast we can move up (SmoothDamp only).")]
        [SerializeField, Min(0.01f)] private float smoothMaxSpeed = 30f;

        [Header("Behavior")]
        [Tooltip("Prevents the camera from ever moving down once it has moved up.")]
        [SerializeField] private bool neverScrollDown = false;

        float _maxPivotY;
        float _velY; // for SmoothDamp

        // Pure utilities (façade pattern)
        private TopDeadZonePolicy _deadZonePolicy;
        private BottomDeadZonePolicy _bottomDeadZonePolicy;
        private ConstantSpeedSmoothing _constantSpeedSmoothing;
        private ConstantSpeedSmoothing _downConstantSpeedSmoothing;

        // Parameter snapshots for change detection (performance optimization)
        private float _lastTopDeadZone = float.NaN;
        private float _lastBottomDeadZone = float.NaN;
        private float _lastUpSpeed = float.NaN;
        private float _lastDownSpeed = float.NaN;
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
            bottomDeadZone = Mathf.Max(0f, bottomDeadZone);
            upSpeed = Mathf.Max(0.01f, upSpeed);
            downSpeed = Mathf.Max(0.01f, downSpeed);
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

            // Check if bottom dead zone policy needs updating
            if (_bottomDeadZonePolicy == null || _lastBottomDeadZone != bottomDeadZone)
            {
                _bottomDeadZonePolicy = new BottomDeadZonePolicy(bottomDeadZone);
                _lastBottomDeadZone = bottomDeadZone;
            }

            // Check if constant speed smoothing needs updating
            if (_constantSpeedSmoothing == null || _lastUpSpeed != upSpeed)
            {
                _constantSpeedSmoothing = new ConstantSpeedSmoothing(upSpeed);
                _lastUpSpeed = upSpeed;
            }

            // Check if downward constant speed smoothing needs updating
            if (_downConstantSpeedSmoothing == null || _lastDownSpeed != downSpeed)
            {
                _downConstantSpeedSmoothing = new ConstantSpeedSmoothing(downSpeed);
                _lastDownSpeed = downSpeed;
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

            // Downward scrolling off (legacy behavior)
            var pos = transform.position;
            if (neverScrollDown && pos.y < _maxPivotY)
            {
                pos.y = _maxPivotY;
                transform.position = pos;
                return;
            }

            // Get current positions
            float pivotY = transform.position.y;
            float playerY = followTarget.position.y;
            
            // Check if player is above the top dead zone
            float topThresholdY = _deadZonePolicy.ComputeThreshold(pivotY);
            
            // Check if player is below the bottom dead zone (only if downward movement is allowed)
            float bottomThresholdY = neverScrollDown ? float.NegativeInfinity : _bottomDeadZonePolicy.ComputeThreshold(pivotY);

            if (playerY > topThresholdY)
            {
                // Player is above top dead zone - follow upward
                float desiredY = _deadZonePolicy.ComputeDesiredY(playerY);
                float newY = ComputeNewY(pivotY, desiredY, true);
                
                // Enforce upward-only rule if enabled
                if (neverScrollDown) newY = Mathf.Max(newY, pivotY);
                
                pos.y = newY;
                transform.position = pos;
                
                // Update the max pivot Y reached
                if (neverScrollDown && newY > _maxPivotY)
                    _maxPivotY = newY;
            }
            else if (playerY < bottomThresholdY)
            {
                // Player is below bottom dead zone - follow downward
                float desiredY = _bottomDeadZonePolicy.ComputeDesiredY(playerY);
                float newY = ComputeNewY(pivotY, desiredY, false);
                
                pos.y = newY;
                transform.position = pos;
            }
            else
            {
                // Player is inside dead zone - no movement needed
                if (neverScrollDown)
                {
                    // If something external lowered camera, bounce back up to max
                    if (transform.position.y < _maxPivotY)
                    {
                        pos.y = _maxPivotY;
                        transform.position = pos;
                    }
                }
            }
        }

        /// <summary>
        /// Computes the new Y position using the appropriate smoothing method.
        /// </summary>
        /// <param name="currentY">Current camera Y position</param>
        /// <param name="desiredY">Target Y position</param>
        /// <param name="isUpward">True if moving upward, false if moving downward</param>
        /// <returns>New Y position</returns>
        private float ComputeNewY(float currentY, float desiredY, bool isUpward)
        {
            if (useSmoothDamp)
            {
                float newY = Mathf.SmoothDamp(currentY, desiredY, ref _velY, smoothTime, smoothMaxSpeed, Time.deltaTime);
                // SmoothDamp may overshoot, so clamp if needed
                if (isUpward && neverScrollDown)
                    newY = Mathf.Max(newY, currentY);
                return newY;
            }
            else
            {
                // Use appropriate speed based on direction
                var smoothing = isUpward ? _constantSpeedSmoothing : _downConstantSpeedSmoothing;
                return smoothing.ComputeNewY(currentY, desiredY, Time.deltaTime);
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

        /// <summary>
        /// Returns true if the camera is configured to never scroll down.
        /// Used by other systems like CameraProjectionAdapter.
        /// </summary>
        public bool GetNeverScrollDown()
        {
            return neverScrollDown;
        }
    }
}