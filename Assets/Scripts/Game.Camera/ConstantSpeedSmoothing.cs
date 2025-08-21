using UnityEngine;

namespace Game.Camera
{
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
}