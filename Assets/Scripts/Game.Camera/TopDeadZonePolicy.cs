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
}