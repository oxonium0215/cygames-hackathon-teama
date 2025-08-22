namespace Game.Camera
{
    /// <summary>
    /// Bottom dead zone policy for vertical camera following.
    /// Handles when the camera should follow the player downward.
    /// </summary>
    public class BottomDeadZonePolicy
    {
        private readonly float bottomDeadZone;

        public BottomDeadZonePolicy(float bottomDeadZone)
        {
            this.bottomDeadZone = bottomDeadZone;
        }

        public float ComputeThreshold(float pivotY)
        {
            return pivotY - bottomDeadZone;
        }

        public float ComputeDesiredY(float playerY)
        {
            return playerY + bottomDeadZone;
        }
    }
}