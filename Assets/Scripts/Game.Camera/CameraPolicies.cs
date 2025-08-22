using UnityEngine;

namespace Game.Camera
{
    /// <summary>
    /// Top dead zone policy for vertical camera following.
    /// </summary>
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

    /// <summary>
    /// Constant speed smoothing strategy using MoveTowards.
    /// </summary>
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

    /// <summary>
    /// SmoothDamp-based smoothing strategy.
    /// </summary>
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
}