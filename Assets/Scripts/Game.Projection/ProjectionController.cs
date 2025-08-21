using UnityEngine;

namespace Game.Projection
{
    /// <summary>
    /// Pure C# implementation of projection switching state and timing.
    /// </summary>
    public class ProjectionController
    {
        private float rotationTimer;
        private float rotationDuration;
        private AnimationCurve rotationEase;
        private int targetIndex;
        private bool rotating;
        
        public bool IsRotating => rotating;
        
        public void BeginSwitch(int targetIndex, float duration, AnimationCurve easeCurve)
        {
            this.targetIndex = targetIndex;
            this.rotationDuration = duration;
            this.rotationEase = easeCurve;
            this.rotationTimer = 0f;
            this.rotating = true;
        }
        
        public float UpdateRotation(float deltaTime)
        {
            if (!rotating) return -1f;
            
            rotationTimer += deltaTime;
            float t = rotationTimer / Mathf.Max(0.0001f, rotationDuration);
            
            if (t >= 1f)
            {
                rotating = false;
                return 1f;
            }
            
            return rotationEase != null ? rotationEase.Evaluate(Mathf.Clamp01(t)) : t;
        }
        
        public void CompleteSwitch()
        {
            rotating = false;
            rotationTimer = 0f;
        }
    }
}