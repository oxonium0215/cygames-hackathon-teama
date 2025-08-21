namespace Game.Core
{
    /// <summary>
    /// Defines the plane of movement for the player character.
    /// Used by both player controller and projection system.
    /// </summary>
    public enum MovePlane 
    { 
        X,  // Movement along X-axis, locked to Z plane
        Z   // Movement along Z-axis, locked to X plane
    }

    // Placeholder for future shared utilities and constants
}