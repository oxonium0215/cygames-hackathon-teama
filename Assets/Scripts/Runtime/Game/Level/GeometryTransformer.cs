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
    /// Handles in-place transformation of geometry.
    /// Maintains original object identity for checkpoints, warps, and other interactive elements.
    /// </summary>
    public class GeometryTransformer
    {
        private readonly Dictionary<Transform, Vector3> originalPositions = new();
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
        /// Restore all geometry to their original positions.
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

            isTransformed = false;
        }

        /// <summary>
        /// Clear stored original positions (call when objects are destroyed).
        /// </summary>
        public void Clear()
        {
            originalPositions.Clear();
            isTransformed = false;
        }

        /// <summary>
        /// Store original transform positions before transformation.
        /// </summary>
        private void StoreOriginalTransforms(Transform root)
        {
            originalPositions.Clear();

            var meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in meshFilters)
            {
                var transform = mf.transform;
                originalPositions[transform] = transform.position;
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