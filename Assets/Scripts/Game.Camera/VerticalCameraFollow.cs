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

        void OnValidate()
        {
            // Enforce sane parameter ranges
            topDeadZone = Mathf.Max(0f, topDeadZone);
            upSpeed = Mathf.Max(0.01f, upSpeed);
            smoothTime = Mathf.Max(0.01f, smoothTime);
            smoothMaxSpeed = Mathf.Max(0.01f, smoothMaxSpeed);
        }

        void LateUpdate()
        {
            if (!followTarget) return;

            float pivotY = transform.position.y;
            float playerY = followTarget.position.y;
            
            // Compute threshold: pivotY + topDeadZone (inlined from TopDeadZonePolicy)
            float thresholdY = pivotY + topDeadZone;

            // Determine if we should follow the player
            bool shouldFollowUp = playerY > thresholdY;
            bool shouldFollowDown = !neverScrollDown && playerY < pivotY;
            
            if (shouldFollowUp || shouldFollowDown)
            {
                float desiredY;
                if (shouldFollowUp)
                {
                    // Follow upward with dead zone offset: playerY - topDeadZone (inlined from TopDeadZonePolicy)
                    desiredY = playerY - topDeadZone;
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
                } 
                else
                {
                    // Constant speed smoothing: inlined from ConstantSpeedSmoothing
                    float step = upSpeed * Time.deltaTime;
                    newY = Mathf.MoveTowards(pivotY, desiredY, step);
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