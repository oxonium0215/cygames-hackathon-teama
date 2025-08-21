using UnityEngine;

namespace Game.Camera
{
    /// <summary>
    /// SmoothDamp-based smoothing strategy.
    /// </summary>
    public class SmoothDampSmoothing : ISmoothing
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