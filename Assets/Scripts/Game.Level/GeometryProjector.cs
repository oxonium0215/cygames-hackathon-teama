using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Game.Level
{
    [MovedFrom(true, sourceNamespace: "POC.Level")]
    public enum ProjectionAxis
    {
        FlattenZ, // Force z = planeZ
        FlattenX  // Force x = planeX
    }

    [DisallowMultipleComponent]
    [MovedFrom(true, sourceNamespace: "POC.Level", sourceClassName: "ProjectionBuilder")]
    public class GeometryProjector : MonoBehaviour
    {
        [Header("Hierarchy")]
        [SerializeField] private Transform sourceRoot;     // True 3D blocks live here
        [SerializeField] private Transform projectedRoot;  // Clones generated here

        [Header("Center/Planes")]
        [Tooltip("If set, planes are derived from this center transform on each rebuild.")]
        [SerializeField] private Transform rotationCenter;
        [Tooltip("World Z plane to flatten to when axis = FlattenZ (overridden by rotationCenter if assigned).")]
        [SerializeField] private float planeZ = 0f;
        [Tooltip("World X plane to flatten to when axis = FlattenX (overridden by rotationCenter if assigned).")]
        [SerializeField] private float planeX = 0f;

        [Header("Offsets (relative to RotationCenter)")]
        [Tooltip("Additive offset to center.z used when axis = FlattenZ.")]
        [SerializeField] private float planeZOffset = 0f;
        [Tooltip("Additive offset to center.x used when axis = FlattenX.")]
        [SerializeField] private float planeXOffset = 0f;

        [Header("Rendering/Physics")]
        [Tooltip("Clone materials from sources if a MeshRenderer exists (even if disabled).")]
        [SerializeField] private bool copyMaterials = true;
        [Tooltip("Layer to assign to projected clones (e.g., Ground/Environment). -1 keeps source layer.")]
        [SerializeField] private int projectedLayer = -1;
        [Tooltip("Disable source colliders at runtime so only clones are used for physics.")]
        [SerializeField] private bool disableSourceColliders = true;
        [Tooltip("Hide source renderers during normal play (they are shown during camera rotation).")]
        [SerializeField] private bool hideSourcesWhenIdle = true;

        private readonly List<Renderer> sourceRenderers = new();
        private readonly List<Collider> sourceColliders = new();

        // Pure utility (façade pattern)
        private ProjectorPass _projectorPass;

        public Transform SourceRoot => sourceRoot;
        public Transform ProjectedRoot => projectedRoot;

        private void Awake()
        {
            CacheSourceLists();
            _projectorPass = new ProjectorPass();
        }

        private void OnValidate()
        {
            CacheSourceLists();
            if (_projectorPass == null)
                _projectorPass = new ProjectorPass();
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
            if (hideSourcesWhenIdle) SetSourcesVisible(false);
            if (disableSourceColliders) SetSourceCollidersEnabled(false);
        }

        public void SetSourcesVisible(bool visible)
        {
            foreach (var r in sourceRenderers)
                if (r) r.enabled = visible;
        }

        public void SetSourceCollidersEnabled(bool enabled)
        {
            foreach (var c in sourceColliders)
                if (c) c.enabled = enabled;
        }

        public void ClearProjected()
        {
            if (_projectorPass == null)
                _projectorPass = new ProjectorPass();
            _projectorPass.Clear(projectedRoot);
        }

        public void Rebuild(ProjectionAxis axis)
        {
            if (sourceRoot == null || projectedRoot == null)
            {
                Debug.LogWarning("[GeometryProjector] SourceRoot or ProjectedRoot not set.");
                return;
            }

            // Update planes from center + offsets if provided
            if (rotationCenter != null)
            {
                planeZ = rotationCenter.position.z + planeZOffset;
                planeX = rotationCenter.position.x + planeXOffset;
            }

            // Create context and delegate to ProjectorPass
            var context = new ProjectorPassContext
            {
                sourceRoot = sourceRoot,
                projectedRoot = projectedRoot,
                planeZ = planeZ,
                planeX = planeX,
                copyMaterials = copyMaterials,
                projectedLayer = projectedLayer
            };

            if (_projectorPass == null)
                _projectorPass = new ProjectorPass();
            
            _projectorPass.Clear(projectedRoot);
            _projectorPass.Run(axis, context);
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