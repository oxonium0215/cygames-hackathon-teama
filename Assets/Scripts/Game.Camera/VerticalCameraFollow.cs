using UnityEngine;

namespace Game.Camera
{
    // - Only moves along Y
    // - Moves up when the player exceeds a top dead zone window
    // - Can move down to follow player downward movement
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
        [Tooltip("When enabled, prevents the camera from moving down (legacy mode). When disabled, allows full bidirectional following.")]
        [SerializeField] private bool neverScrollDown = false;

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

            float pivotY = transform.position.y;
            float playerY = followTarget.position.y;
            float thresholdY = _deadZonePolicy.ComputeThreshold(pivotY);

            // Determine if we should follow the player
            bool shouldFollowUp = playerY > thresholdY;
            bool shouldFollowDown = !neverScrollDown && playerY < pivotY;
            
            if (shouldFollowUp || shouldFollowDown)
            {
                float desiredY;
                if (shouldFollowUp)
                {
                    // Follow upward with dead zone offset
                    desiredY = _deadZonePolicy.ComputeDesiredY(playerY);
                }
                else
                {
                    // Follow downward directly (no dead zone)
                    desiredY = playerY;
                }

                // Apply smoothing
                float newY;
                if (useSmoothDamp)
                {
                    newY = Mathf.SmoothDamp(pivotY, desiredY, ref _velY, smoothTime, smoothMaxSpeed, Time.deltaTime);
                } else
                {
                    newY = _constantSpeedSmoothing.ComputeNewY(pivotY, desiredY, Time.deltaTime);
                }

                // Enforce upward-only constraint if in legacy mode
                if (neverScrollDown && newY < pivotY)
                    newY = pivotY;

                // Apply the new position
                var pos = transform.position;
                pos.y = newY;
                transform.position = pos;
            }
        }
    }
}