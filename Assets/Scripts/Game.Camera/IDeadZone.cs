namespace Game.Camera
{
    /// <summary>
    /// Helper to compute thresholds and desired Y based on dead-zone settings.
    /// </summary>
    public interface IDeadZone
    {
        /// <summary>
        /// Compute the threshold Y position above which camera should start following.
        /// </summary>
        /// <param name="pivotY">Current camera pivot Y position</param>
        /// <returns>Threshold Y position</returns>
        float ComputeThreshold(float pivotY);

        /// <summary>
        /// Compute the desired camera Y position to maintain the dead zone.
        /// </summary>
        /// <param name="playerY">Player Y position</param>
        /// <returns>Desired camera Y position</returns>
        float ComputeDesiredY(float playerY);
    }
}