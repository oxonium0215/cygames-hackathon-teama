using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// Context for a projection pass operation.
    /// </summary>
    public struct ProjectorPassContext
    {
        public Transform sourceRoot;
        public Transform projectedRoot;
        public float planeZ;
        public float planeX;
        public bool copyMaterials;
        public int projectedLayer;
    }

    /// <summary>
    /// Pure class responsible for the idempotent cloning process performed by GeometryProjector.
    /// Handles mesh/collider cloning, material assignment, layer setting, and plane flattening.
    /// </summary>
    public class ProjectorPass
    {
        /// <summary>
        /// Perform the projection pass for the given axis and context.
        /// </summary>
        /// <param name="axis">Projection axis (FlattenZ or FlattenX)</param>
        /// <param name="context">Projection context with source data</param>
        public void Run(ProjectionAxis axis, ProjectorPassContext context)
        {
            if (context.sourceRoot == null || context.projectedRoot == null)
                return;

            // Clone from MeshFilters so disabled MeshRenderers do not block geometry creation
            var meshFilters = context.sourceRoot.GetComponentsInChildren<MeshFilter>(true);
            foreach (var srcMF in meshFilters)
            {
                var srcGO = srcMF.gameObject;
                var srcMR = srcGO.GetComponent<MeshRenderer>();

                // Create clone
                var cloneGO = new GameObject($"Clone_{srcGO.name}");
                cloneGO.transform.SetParent(context.projectedRoot, worldPositionStays: false);

                // Copy world transform then flatten
                cloneGO.transform.position = srcGO.transform.position;
                cloneGO.transform.rotation = srcGO.transform.rotation;
                cloneGO.transform.localScale = srcGO.transform.lossyScale;

                var p = cloneGO.transform.position;
                if (axis == ProjectionAxis.FlattenZ) 
                    p.z = context.planeZ; 
                else 
                    p.x = context.planeX;
                cloneGO.transform.position = p;

                // Mesh
                var mf = cloneGO.AddComponent<MeshFilter>();
                mf.sharedMesh = srcMF.sharedMesh;

                var mr = cloneGO.AddComponent<MeshRenderer>();
                if (srcMR && context.copyMaterials)
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
                if (context.projectedLayer >= 0 && context.projectedLayer <= 31)
                    SetLayerRecursively(cloneGO, context.projectedLayer);
                else
                    cloneGO.layer = srcGO.layer;
            }
        }

        /// <summary>
        /// Clear all children from the projected root.
        /// </summary>
        /// <param name="projectedRoot">Root transform to clear</param>
        public void Clear(Transform projectedRoot)
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
    }
}