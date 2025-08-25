using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// Editor utility to organize scene hierarchy by grouping manager GameObjects.
    /// This addresses the issue of having several manager GameObjects at the root level.
    /// </summary>
    public static class SceneHierarchyOrganizer
    {
        [MenuItem("Tools/Organize Scene Hierarchy")]
        public static void OrganizeSceneHierarchy()
        {
            // Find or create _Managers parent object
            GameObject managersParent = GameObject.Find("_Managers");
            if (managersParent == null)
            {
                managersParent = new GameObject("_Managers");
                Undo.RegisterCreatedObjectUndo(managersParent, "Create _Managers object");
            }

            // List of manager object name patterns to look for
            string[] managerPatterns = new string[]
            {
                "PerspectiveProjectionManager",
                "GeometryProjector", 
                "StagePreviewManager",
                "InputManager",
                "GameManager",
                "AudioManager",
                "UIManager",
                // Add more patterns as needed
            };

            int movedCount = 0;
            
            // Find and parent manager objects
            foreach (string pattern in managerPatterns)
            {
                GameObject[] foundObjects = GameObject.FindObjectsOfType<GameObject>();
                
                foreach (GameObject obj in foundObjects)
                {
                    // Only check root-level objects
                    if (obj.transform.parent == null && obj.name.Contains(pattern))
                    {
                        Undo.SetTransformParent(obj.transform, managersParent.transform, "Parent to _Managers");
                        movedCount++;
                        Debug.Log($"Moved {obj.name} under _Managers");
                    }
                }
            }

            // Mark scene as dirty if changes were made
            if (movedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log($"Scene hierarchy organized. Moved {movedCount} manager objects under _Managers.");
            }
            else
            {
                Debug.Log("No manager objects found to organize.");
            }
        }

        [MenuItem("Tools/Organize Scene Hierarchy", true)]
        public static bool OrganizeSceneHierarchy_Validate()
        {
            // Only enable if there's an active scene
            return EditorSceneManager.GetActiveScene().IsValid();
        }
    }
}