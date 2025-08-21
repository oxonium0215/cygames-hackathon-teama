using UnityEngine;
using Game.Player;

namespace Game.Projection
{
    /// <summary>
    /// Handles player state during projection switching (freezing, kinematic changes, velocity mapping).
    /// </summary>
    public interface IPlayerProjectionAdapter
    {
        /// <summary>
        /// Prepares player for rotation by freezing motor and optionally making kinematic.
        /// </summary>
        /// <param name="makeKinematic">Whether to make player kinematic during rotation</param>
        /// <param name="jumpOnlyMode">Whether to disable lateral input</param>
        /// <returns>Original kinematic state</returns>
        bool PrepareForRotation(bool makeKinematic, bool jumpOnlyMode);
        
        /// <summary>
        /// Restores player state after rotation completion.
        /// </summary>
        /// <param name="originalKinematic">Original kinematic state to restore</param>
        /// <param name="jumpOnlyMode">Whether lateral input was disabled</param>
        void RestoreAfterRotation(bool originalKinematic, bool jumpOnlyMode);
        
        /// <summary>
        /// Maps velocity from source projection axis to target axis preserving direction.
        /// </summary>
        /// <param name="preRotationVelocity">Velocity before rotation</param>
        /// <param name="sourceAxis">Source projection axis</param>
        /// <param name="targetAxis">Target projection axis</param>
        /// <returns>Mapped velocity for target axis</returns>
        Vector3 MapVelocityBetweenAxes(Vector3 preRotationVelocity, ProjectionAxis sourceAxis, ProjectionAxis targetAxis);
        
        /// <summary>
        /// Updates player's active plane and plane lock settings.
        /// </summary>
        /// <param name="newPlane">New movement plane</param>
        /// <param name="planeConstant">Constant value for the plane</param>
        void SetPlayerPlane(MovePlane newPlane, float planeConstant);
    }
}