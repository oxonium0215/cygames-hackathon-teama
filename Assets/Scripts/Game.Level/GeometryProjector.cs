using System.Collections.Generic;
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
    /// Transforms original terrain objects directly instead of creating clones.
    /// This preserves object identity for checkpoints, warps, and other interactive elements.
    /// </summary>
    [DisallowMultipleComponent]
    public class GeometryProjector : MonoBehaviour
    {
        [Header("Hierarchy")]
        [SerializeField] private Transform terrainRoot;

        [Header("Center/Planes")]
        [SerializeField] private Transform rotationCenter;
        [SerializeField] private float planeZ = 0f;
        [SerializeField] private float planeX = 0f;

        [Header("Offsets (relative to RotationCenter)")]
        [SerializeField] private float planeZOffset = -8.5f;
        [SerializeField] private float planeXOffset = 8.5f;

        [Header("Physics")]
        [SerializeField] private bool disableTerrainColliders = true;

        // Core transformation system
        private GeometryTransformer _geometryTransformer;

        public Transform TerrainRoot => terrainRoot;
        /// <summary>Returns terrainRoot for compatibility - terrain objects are transformed in-place</summary>
        public Transform ProjectedRoot => terrainRoot;
        
        // Legacy compatibility properties
        /// <summary>Legacy compatibility - use TerrainRoot instead</summary>
        public Transform SourceRoot => terrainRoot;

        private void Awake()
        {
            _geometryTransformer = new GeometryTransformer();
        }

        private void OnValidate()
        {
            if (_geometryTransformer == null)
                _geometryTransformer = new GeometryTransformer();
        }

        private void OnDestroy()
        {
            _geometryTransformer?.Restore();
            _geometryTransformer?.Clear();
        }

        /// <summary>
        /// Initialize the geometry projector. Terrain objects are transformed in-place.
        /// </summary>
        public void InitializeOnce()
        {
            if (disableTerrainColliders)
            {
                SetTerrainCollidersEnabled(false);
            }
        }

        /// <summary>
        /// Control terrain visibility. Terrain objects are transformed in-place.
        /// </summary>
        public void SetTerrainVisible(bool visible)
        {
            _geometryTransformer?.SetTerrainVisible(visible);
        }

        /// <summary>
        /// Enable/disable terrain colliders. Terrain colliders are the active physics geometry.
        /// </summary>
        public void SetTerrainCollidersEnabled(bool enabled)
        {
            _geometryTransformer?.SetTerrainCollidersEnabled(enabled);
        }

        /// <summary>
        /// Restore terrain to original positions and states.
        /// </summary>
        public void RestoreTerrain()
        {
            if (_geometryTransformer == null)
                _geometryTransformer = new GeometryTransformer();
            _geometryTransformer.Restore();
        }

        /// <summary>Legacy compatibility - use RestoreTerrain instead</summary>
        public void ClearProjected() => RestoreTerrain();

        /// <summary>
        /// Rebuild geometry projection for the specified axis using in-place transformation.
        /// </summary>
        public void Rebuild(ProjectionAxis axis)
        {
            if (terrainRoot == null)
            {
                Debug.LogWarning("[GeometryProjector] TerrainRoot not set.");
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
                terrainRoot = terrainRoot,
                planeZ = planeZ,
                planeX = planeX
            };

            if (_geometryTransformer == null)
                _geometryTransformer = new GeometryTransformer();
            
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
        
        // Legacy compatibility methods
        /// <summary>Legacy compatibility - use SetTerrainVisible instead</summary>
        public void SetSourcesVisible(bool visible) => SetTerrainVisible(visible);
        
        /// <summary>Legacy compatibility - use SetTerrainCollidersEnabled instead</summary>
        public void SetSourceCollidersEnabled(bool enabled) => SetTerrainCollidersEnabled(enabled);
    }
}