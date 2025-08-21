using UnityEngine;

namespace Game.Projection
{
    /// <summary>
    /// Handles camera pivot adjustments during projection switches, compatible with VerticalCameraFollow.
    /// </summary>
    public interface ICameraProjectionAdapter
    {
        /// <summary>
        /// Repositions camera pivot to rotation center with offset, preserving upward movement rule.
        /// </summary>
        /// <param name="rotationCenter">Center point for rotation</param>
        /// <param name="pivotOffset">Offset from center</param>
        void RepositionPivotToCenter(Transform rotationCenter, Vector3 pivotOffset);
        
        /// <summary>
        /// Updates camera pivot rotation during switch animation.
        /// </summary>
        /// <param name="startYaw">Starting yaw angle</param>
        /// <param name="targetYaw">Target yaw angle</param>
        /// <param name="progress">Animation progress (0-1)</param>
        void UpdateRotation(float startYaw, float targetYaw, float progress);
        
        /// <summary>
        /// Positions child camera at specified distance and resets local rotation.
        /// </summary>
        /// <param name="distance">Camera distance from pivot</param>
        void SetCameraDistance(float distance);
    }
}