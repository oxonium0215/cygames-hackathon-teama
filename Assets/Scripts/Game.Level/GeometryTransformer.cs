using System.Collections.Generic;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// Context for a geometry transformation operation.
    /// </summary>
    public struct TransformationContext
    {
        public Transform sourceRoot;
        public float planeZ;
        public float planeX;
    }

    /// <summary>
    /// Handles in-place transformation of geometry instead of cloning.
    /// Maintains original object identity for checkpoints, warps, and other interactive elements.
    /// </summary>
    public class GeometryTransformer
    {
        private readonly Dictionary<Transform, Vector3> originalPositions = new();
        private readonly Dictionary<Transform, bool> originalColliderStates = new();
        private readonly Dictionary<Transform, bool> originalRendererStates = new();
        private ProjectionAxis currentAxis;
        private bool isTransformed = false;

        /// <summary>
        /// Transform geometry to the specified projection plane.
        /// </summary>
        /// <param name="axis">Projection axis (FlattenZ or FlattenX)</param>
        /// <param name="context">Transformation context with source data</param>
        public void Transform(ProjectionAxis axis, TransformationContext context)
        {
            if (context.sourceRoot == null)
                return;

            // If we're already transformed, restore first
            if (isTransformed)
                Restore();

            // Store original positions
            StoreOriginalTransforms(context.sourceRoot);

            // Apply transformations
            ApplyProjectionTransform(axis, context);

            currentAxis = axis;
            isTransformed = true;
        }

        /// <summary>
        /// Restore all geometry to their original positions and states.
        /// </summary>
        public void Restore()
        {
            if (!isTransformed)
                return;

            foreach (var kvp in originalPositions)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.position = kvp.Value;
                }
            }

            // Restore collider states
            foreach (var kvp in originalColliderStates)
            {
                if (kvp.Key != null)
                {
                    var collider = kvp.Key.GetComponent<Collider>();
                    if (collider != null)
                        collider.enabled = kvp.Value;
                }
            }

            // Restore renderer states
            foreach (var kvp in originalRendererStates)
            {
                if (kvp.Key != null)
                {
                    var renderer = kvp.Key.GetComponent<Renderer>();
                    if (renderer != null)
                        renderer.enabled = kvp.Value;
                }
            }

            isTransformed = false;
        }

        /// <summary>
        /// Clear stored original positions (call when objects are destroyed).
        /// </summary>
        public void Clear()
        {
            originalPositions.Clear();
            originalColliderStates.Clear();
            originalRendererStates.Clear();
            isTransformed = false;
        }

        /// <summary>
        /// Set visibility of source renderers.
        /// In the new in-place transformation system, sources are generally kept visible.
        /// </summary>
        public void SetSourcesVisible(bool visible)
        {
            foreach (var kvp in originalRendererStates)
            {
                if (kvp.Key != null)
                {
                    var renderer = kvp.Key.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        // In the new system, we generally keep sources visible since they're the active geometry
                        // Only hide them if explicitly requested (e.g., during destruction or special debug cases)
                        renderer.enabled = visible;
                    }
                }
            }
        }

        /// <summary>
        /// Set collision state of source colliders.
        /// In the new system, colliders should generally remain enabled since sources are the active geometry.
        /// </summary>
        public void SetSourceCollidersEnabled(bool enabled)
        {
            foreach (var kvp in originalColliderStates)
            {
                if (kvp.Key != null)
                {
                    var collider = kvp.Key.GetComponent<Collider>();
                    if (collider != null)
                        collider.enabled = enabled;
                }
            }
        }

        private void StoreOriginalTransforms(Transform root)
        {
            originalPositions.Clear();
            originalColliderStates.Clear();
            originalRendererStates.Clear();

            var meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in meshFilters)
            {
                var transform = mf.transform;
                originalPositions[transform] = transform.position;

                // Store collider states
                var collider = transform.GetComponent<Collider>();
                if (collider != null)
                    originalColliderStates[transform] = collider.enabled;

                // Store renderer states
                var renderer = transform.GetComponent<Renderer>();
                if (renderer != null)
                    originalRendererStates[transform] = renderer.enabled;
            }
        }

        private void ApplyProjectionTransform(ProjectionAxis axis, TransformationContext context)
        {
            foreach (var kvp in originalPositions)
            {
                if (kvp.Key != null)
                {
                    var pos = kvp.Value;
                    if (axis == ProjectionAxis.FlattenZ)
                        pos.z = context.planeZ;
                    else
                        pos.x = context.planeX;
                    
                    kvp.Key.position = pos;
                }
            }

            // Sync physics after transformation
#if UNITY_2021_2_OR_NEWER
            Physics.SyncTransforms();
#endif
        }
    }
}