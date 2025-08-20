using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Game.Camera
{
    // - Only moves along Y
    // - Moves up when the player exceeds a top dead zone window
    // - Never moves down (keeps the highest Y reached)
    // - Optional smoothing
    [MovedFrom("POC.Camera")]
    public class VerticalCameraTracker : MonoBehaviour
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

        void Awake()
        {
            _maxPivotY = transform.position.y;
        }

        void LateUpdate()
        {
            if (!followTarget) return;

            // Downward scrolling off
            var pos = transform.position;
            if (neverScrollDown && pos.y < _maxPivotY)
            {
                pos.y = _maxPivotY;
                transform.position = pos;
            }

            // Check if player is above the top dead zone
            float pivotY = transform.position.y;
            float playerY = followTarget.position.y;
            float thresholdY = pivotY + topDeadZone;

            if (playerY > thresholdY)
            {
                // Desired Y keeps the player exactly at the top edge of the dead zone
                float desiredY = playerY - topDeadZone;
                float newY;

                if (useSmoothDamp)
                {
                    newY = Mathf.SmoothDamp(pivotY, desiredY, ref _velY, smoothTime, smoothMaxSpeed, Time.deltaTime);
                    // SmoothDamp may overshoot downwards if desired decreases
                    if (neverScrollDown) newY = Mathf.Max(newY, pivotY);
                } else
                {
                    float step = upSpeed * Time.deltaTime;
                    newY = Mathf.MoveTowards(pivotY, desiredY, step);
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