using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

[MovedFrom("POC.Level")]
namespace Game.Projection
{
    [MovedFrom("POC.Level")]
    public enum ProjectionAxis
    {
        FlattenZ, // Force z = planeZ
        FlattenX  // Force x = planeX
    }

    [MovedFrom("POC.Level")]
    [DisallowMultipleComponent]
    public class ProjectionBuilder : MonoBehaviour
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
        [SerializeField] private int projectedLayer = 3; // Default to Ground layer
        [Tooltip("Disable source colliders at runtime so only clones are used for physics.")]
        [SerializeField] private bool disableSourceColliders = true;
        [Tooltip("Hide source renderers during normal play (they are shown during camera rotation).")]
        [SerializeField] private bool hideSourcesWhenIdle = true;

        private readonly List<Renderer> sourceRenderers = new();
        private readonly List<Collider> sourceColliders = new();

        public Transform SourceRoot => sourceRoot;
        public Transform ProjectedRoot => projectedRoot;

        private void Awake()
        {
            CacheSourceLists();
        }

        private void OnValidate()
        {
            CacheSourceLists();
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
            if (projectedRoot == null) return;
            for (int i = projectedRoot.childCount - 1; i >= 0; i--)
            {
                var child = projectedRoot.GetChild(i);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEditor.Undo.DestroyObjectImmediate(child.gameObject);
                else
                    Object.Destroy(child.gameObject);
#else
                Object.Destroy(child.gameObject);
#endif
            }
        }

        public void Rebuild(ProjectionAxis axis)
        {
            if (sourceRoot == null || projectedRoot == null)
            {
                Debug.LogWarning("[ProjectionBuilder] SourceRoot or ProjectedRoot not set.");
                return;
            }

            // Update planes from center + offsets if provided
            if (rotationCenter != null)
            {
                planeZ = rotationCenter.position.z + planeZOffset;
                planeX = rotationCenter.position.x + planeXOffset;
            }

            ClearProjected();

            // Clone from MeshFilters so disabled MeshRenderers do not block geometry creation
            var meshFilters = sourceRoot.GetComponentsInChildren<MeshFilter>(true);
            foreach (var srcMF in meshFilters)
            {
                var srcGO = srcMF.gameObject;
                var srcMR = srcGO.GetComponent<MeshRenderer>();

                // Create clone
                var cloneGO = new GameObject($"Clone_{srcGO.name}");
                cloneGO.transform.SetParent(projectedRoot, worldPositionStays: false);

                // Copy world transform then flatten
                cloneGO.transform.position = srcGO.transform.position;
                cloneGO.transform.rotation = srcGO.transform.rotation;
                cloneGO.transform.localScale = srcGO.transform.lossyScale;

                var p = cloneGO.transform.position;
                if (axis == ProjectionAxis.FlattenZ) p.z = planeZ; else p.x = planeX;
                cloneGO.transform.position = p;

                // Mesh
                var mf = cloneGO.AddComponent<MeshFilter>();
                mf.sharedMesh = srcMF.sharedMesh;

                var mr = cloneGO.AddComponent<MeshRenderer>();
                if (srcMR && copyMaterials)
                {
                    mr.sharedMaterials = srcMR.sharedMaterials;
                    mr.shadowCastingMode = srcMR.shadowCastingMode;
                    mr.receiveShadows = srcMR.receiveShadows;
                    mr.lightProbeUsage = srcMR.lightProbeUsage;
                    mr.reflectionProbeUsage = srcMR.reflectionProbeUsage;
                    mr.allowOcclusionWhenDynamic = srcMR.allowOcclusionWhenDynamic;
                }

                // Colliders: copy common types
                CloneSupportedColliders(srcGO, cloneGO);

                // Layer
                if (projectedLayer >= 0 && projectedLayer <= 31)
                    SetLayerRecursively(cloneGO, projectedLayer);
                else
                    cloneGO.layer = srcGO.layer;
            }
        }

        private void CloneSupportedColliders(GameObject src, GameObject dst)
        {
            foreach (var c in src.GetComponents<Collider>())
            {
                switch (c)
                {
                    case BoxCollider b:
                        var nb = dst.AddComponent<BoxCollider>();
                        nb.center = b.center;
                        nb.size = b.size;
                        nb.isTrigger = b.isTrigger;
                        break;
                    case CapsuleCollider cap:
                        var ncap = dst.AddComponent<CapsuleCollider>();
                        ncap.center = cap.center;
                        ncap.radius = cap.radius;
                        ncap.height = cap.height;
                        ncap.direction = cap.direction;
                        ncap.isTrigger = cap.isTrigger;
                        break;
                    case MeshCollider mc:
                        var nmc = dst.AddComponent<MeshCollider>();
                        nmc.sharedMesh = mc.sharedMesh;
                        nmc.convex = mc.convex;
                        nmc.cookingOptions = mc.cookingOptions;
                        nmc.isTrigger = mc.isTrigger;
                        break;
                }
            }
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++)
                SetLayerRecursively(go.transform.GetChild(i).gameObject, layer);
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