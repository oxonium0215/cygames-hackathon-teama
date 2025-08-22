using System.Collections.Generic;
using UnityEngine;

namespace Game.Level
{
    public enum ProjectionAxis
    {
        FlattenZ, // Force z = planeZ
        FlattenX  // Force x = planeX
    }

    [DisallowMultipleComponent]
    public class GeometryProjector : MonoBehaviour
    {
        [Header("Hierarchy")]
        [SerializeField] private Transform sourceRoot;
        [SerializeField] private Transform projectedRoot;  // DEPRECATED: kept for compatibility

        [Header("Center/Planes")]
        [SerializeField] private Transform rotationCenter;
        [SerializeField] private float planeZ = 0f;
        [SerializeField] private float planeX = 0f;

        [Header("Offsets (relative to RotationCenter)")]
        [SerializeField] private float planeZOffset = -8.5f;
        [SerializeField] private float planeXOffset = 8.5f;

        [Header("Rendering/Physics")]
#pragma warning disable 0414
        [SerializeField] private bool copyMaterials = true;        // DEPRECATED
        [SerializeField] private int projectedLayer = -1;         // DEPRECATED  
#pragma warning restore 0414
        [SerializeField] private bool disableSourceColliders = true;
        [SerializeField] private bool hideSourcesWhenIdle = true; // DEPRECATED in new system

        private readonly List<Renderer> sourceRenderers = new();
        private readonly List<Collider> sourceColliders = new();

        // Use geometry transformer instead of projector pass
        private GeometryTransformer _geometryTransformer;

        public Transform SourceRoot => sourceRoot;
        public Transform ProjectedRoot => sourceRoot; // Same as sourceRoot since we transform in-place

        private void Awake()
        {
            CacheSourceLists();
            _geometryTransformer = new GeometryTransformer();
        }

        private void OnValidate()
        {
            CacheSourceLists();
            if (_geometryTransformer == null)
                _geometryTransformer = new GeometryTransformer();
        }

        private void OnDestroy()
        {
            _geometryTransformer?.Restore();
            _geometryTransformer?.Clear();
        }

        private void CacheSourceLists()
        {
            sourceRenderers.Clear();
            sourceColliders.Clear();

            if (sourceRoot == null) return;

            sourceRenderers.AddRange(sourceRoot.GetComponentsInChildren<Renderer>(true));
            sourceColliders.AddRange(sourceRoot.GetComponentsInChildren<Collider>(true));
        }

        public void InitializeOnce()
        {
            // In the new system, sources are the active geometry and should remain visible and collidable
            // Only disable if explicitly configured to do so
            if (disableSourceColliders) SetSourceCollidersEnabled(false);
        }

        public void SetSourcesVisible(bool visible)
        {
            if (_geometryTransformer != null)
            {
                // In the new system, sources are the active geometry and should generally stay visible
                _geometryTransformer.SetSourcesVisible(visible);
            }
            else
            {
                foreach (var r in sourceRenderers)
                    if (r) r.enabled = visible;
            }
        }

        public void SetSourceCollidersEnabled(bool enabled)
        {
            if (_geometryTransformer != null)
            {
                // In the new system, source colliders are the active physics geometry
                _geometryTransformer.SetSourceCollidersEnabled(enabled);
            }
            else
            {
                foreach (var c in sourceColliders)
                    if (c) c.enabled = enabled;
            }
        }

        public void ClearProjected()
        {
            if (_geometryTransformer == null)
                _geometryTransformer = new GeometryTransformer();
            _geometryTransformer.Restore();
        }

        public void Rebuild(ProjectionAxis axis)
        {
            if (sourceRoot == null)
            {
                Debug.LogWarning("[GeometryProjector] SourceRoot not set.");
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

            if (_geometryTransformer == null)
                _geometryTransformer = new GeometryTransformer();
            
            _geometryTransformer.Transform(axis, context);
        }

        // Plane accessors and center setter
        public void SetPlaneZ(float value) => planeZ = value;
        public void SetPlaneX(float value) => planeX = value;
        public float GetPlaneZ() => planeZ;
        public float GetPlaneX() => planeX;
        public void SetRotationCenter(Transform t) => rotationCenter = t;
        public Transform GetRotationCenter() => rotationCenter;

        // Offsets
        public void SetPlaneOffsets(float zOffset, float xOffset)
        {
            planeZOffset = zOffset;
            planeXOffset = xOffset;
        }
    }
}