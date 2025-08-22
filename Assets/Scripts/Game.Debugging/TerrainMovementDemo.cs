using UnityEngine;
using Game.Level;

namespace Game.Debugging
{
    /// <summary>
    /// Demonstration component showing how the new terrain movement system works.
    /// Attach to a GameObject with GeometryProjector to see terrain movement vs duplication.
    /// </summary>
    public class TerrainMovementDemo : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GeometryProjector geometryProjector;
        
        [Header("Demo Controls")]
        [SerializeField] private KeyCode switchToFlattenZ = KeyCode.Alpha1;
        [SerializeField] private KeyCode switchToFlattenX = KeyCode.Alpha2;
        [SerializeField] private KeyCode restoreOriginal = KeyCode.Alpha3;
        
        [Header("Debug Info")]
        [SerializeField] private bool showDebugInfo = true;

        private int childCountBefore;
        private bool hasTransformed = false;

        void Start()
        {
            if (!geometryProjector)
                geometryProjector = GetComponent<GeometryProjector>();
            
            if (geometryProjector?.SourceRoot != null)
            {
                childCountBefore = CountAllChildren(geometryProjector.SourceRoot);
                Debug.Log($"[TerrainMovementDemo] Initial terrain objects: {childCountBefore}");
            }
        }

        void Update()
        {
            if (!geometryProjector) return;

            if (Input.GetKeyDown(switchToFlattenZ))
            {
                Debug.Log("[TerrainMovementDemo] Switching to FlattenZ projection (side view)");
                geometryProjector.Rebuild(ProjectionAxis.FlattenZ);
                hasTransformed = true;
                CheckObjectCount("after FlattenZ transform");
            }
            else if (Input.GetKeyDown(switchToFlattenX))
            {
                Debug.Log("[TerrainMovementDemo] Switching to FlattenX projection (top-down view)");
                geometryProjector.Rebuild(ProjectionAxis.FlattenX);
                hasTransformed = true;
                CheckObjectCount("after FlattenX transform");
            }
            else if (Input.GetKeyDown(restoreOriginal))
            {
                Debug.Log("[TerrainMovementDemo] Restoring original positions");
                geometryProjector.ClearProjected();
                hasTransformed = false;
                CheckObjectCount("after restore");
            }
        }

        private void CheckObjectCount(string context)
        {
            if (!showDebugInfo || !geometryProjector?.SourceRoot) return;

            int currentCount = CountAllChildren(geometryProjector.SourceRoot);
            string status = currentCount == childCountBefore ? "preserved" : "CHANGED";
            Debug.Log($"[TerrainMovementDemo] Terrain objects {context}: {currentCount} " +
                      $"(was {childCountBefore}). Objects {status}!");
            
            if (currentCount != childCountBefore)
            {
                Debug.LogWarning("[TerrainMovementDemo] Object count changed! This suggests the old duplication system might still be active.");
            }
        }

        private int CountAllChildren(Transform root)
        {
            int count = 0;
            for (int i = 0; i < root.childCount; i++)
            {
                count += 1 + CountAllChildren(root.GetChild(i));
            }
            return count;
        }

        void OnGUI()
        {
            if (!showDebugInfo) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Terrain Movement Demo");
            GUILayout.Space(10);
            
            GUILayout.Label($"Press '{switchToFlattenZ}' - Switch to side view (FlattenZ)");
            GUILayout.Label($"Press '{switchToFlattenX}' - Switch to top view (FlattenX)");
            GUILayout.Label($"Press '{restoreOriginal}' - Restore original positions");
            GUILayout.Space(10);
            
            if (geometryProjector?.SourceRoot != null)
            {
                int currentCount = CountAllChildren(geometryProjector.SourceRoot);
                GUILayout.Label($"Terrain objects: {currentCount}");
                GUILayout.Label($"Status: {(hasTransformed ? "Transformed" : "Original")}");
                
                if (currentCount == childCountBefore)
                {
                    GUILayout.Label("✓ Objects preserved");
                }
                else
                {
                    GUILayout.Label("⚠ Object count changed!");
                }
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}