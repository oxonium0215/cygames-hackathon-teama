namespace Game.Camera
{
    /// <summary>
    /// Abstraction for smoothing strategies used by VerticalCameraFollow.
    /// </summary>
    public interface ISmoothing
    {
        /// <summary>
        /// Compute the new Y position based on current position, desired position, and frame timing.
        /// </summary>
        /// <param name="currentY">Current Y position</param>
        /// <param name="desiredY">Target Y position</param>
        /// <param name="deltaTime">Frame delta time</param>
        /// <returns>New Y position</returns>
        float ComputeNewY(float currentY, float desiredY, float deltaTime);
    }
}