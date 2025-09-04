using UnityEngine;

namespace Game.Level
{
    public enum ProjectionAxis
    {
        FlattenZ, // Force z = planeZ
        FlattenX  // Force x = planeX
    }

    /// <summary>
    /// Handles geometry projection for perspective switching using in-place transformation.
    /// Transforms original terrain objects directly, preserving object identity for 
    /// checkpoints, warps, and other interactive elements.
    /// </summary>
    [DisallowMultipleComponent]
    public class GeometryProjector : MonoBehaviour
    {
        [Header("Hierarchy")]
        [SerializeField] private Transform sourceRoot;

        [Header("Center/Planes")]
        [SerializeField] private Transform rotationCenter;
        [SerializeField] private float planeZ = 0f;
        [SerializeField] private float planeX = 0f;

        [Header("Offsets (relative to RotationCenter)")]
        [SerializeField] private float planeZOffset = -8f;
        [SerializeField] private float planeXOffset = 8f;

        // Core transformation system
        private GeometryTransformer _geometryTransformer;

        public Transform SourceRoot => sourceRoot;

        private void Awake()
        {
            _geometryTransformer = new GeometryTransformer();
        }

        private void OnDestroy()
        {
            _geometryTransformer?.Restore();
            _geometryTransformer?.Clear();
        }

        /// <summary>
        /// Clear projection state and restore original positions.
        /// </summary>
        public void ClearProjected()
        {
            _geometryTransformer.Restore();
        }

        /// <summary>
        /// Rebuild geometry projection for the specified axis using in-place transformation.
        /// </summary>
        public void Rebuild(ProjectionAxis axis)
        {
            if (sourceRoot == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[GeometryProjector] SourceRoot not set.");
#endif
                return;
            }

            // Update planes from center + offsets if provided
            if (rotationCenter != null)
            {
                planeZ = rotationCenter.position.z + planeZOffset;
                planeX = rotationCenter.position.x + planeXOffset;
            }

            var context = new TransformationContext
            {
                sourceRoot = sourceRoot,
                planeZ = planeZ,
                planeX = planeX
            };
            
            _geometryTransformer.Transform(axis, context);
        }

        // Plane accessors and center configuration
        public void SetPlaneZ(float value) => planeZ = value;
        public void SetPlaneX(float value) => planeX = value;
        public float GetPlaneZ() => planeZ;
        public float GetPlaneX() => planeX;
        public void SetRotationCenter(Transform t) => rotationCenter = t;
        public Transform GetRotationCenter() => rotationCenter;

        /// <summary>
        /// Set plane offsets relative to rotation center.
        /// </summary>
        public void SetPlaneOffsets(float zOffset, float xOffset)
        {
            planeZOffset = zOffset;
            planeXOffset = xOffset;
        }
    }
}