using System.Collections.Generic;
using UnityEngine;

namespace Game.Level
{
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
        public void Transform(ProjectionAxis axis, TransformationContext context)
        {
            if (context.sourceRoot == null)
                return;

            if (isTransformed)
                Restore();

            StoreOriginalTransforms(context.sourceRoot);
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
                    kvp.Key.position = kvp.Value;
            }

            foreach (var kvp in originalColliderStates)
            {
                if (kvp.Key != null)
                {
                    var collider = kvp.Key.GetComponent<Collider>();
                    if (collider != null)
                        collider.enabled = kvp.Value;
                }
            }

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
        /// Control visibility of source renderers. In the new system, sources are the active geometry.
        /// </summary>
        public void SetSourcesVisible(bool visible)
        {
            foreach (var kvp in originalRendererStates)
            {
                if (kvp.Key != null)
                {
                    var renderer = kvp.Key.GetComponent<Renderer>();
                    if (renderer != null)
                        renderer.enabled = visible;
                }
            }
        }

        /// <summary>
        /// Control collider state of source objects. In the new system, source colliders are the active physics geometry.
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

        /// <summary>
        /// Store original transform positions and renderer/collider states before transformation.
        /// </summary>
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

                var collider = transform.GetComponent<Collider>();
                if (collider != null)
                    originalColliderStates[transform] = collider.enabled;

                var renderer = transform.GetComponent<Renderer>();
                if (renderer != null)
                    originalRendererStates[transform] = renderer.enabled;
            }
        }

        /// <summary>
        /// Apply position transformation to flatten geometry onto specified projection plane.
        /// </summary>
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

#if UNITY_2021_2_OR_NEWER
            Physics.SyncTransforms();
#endif
        }
    }
}