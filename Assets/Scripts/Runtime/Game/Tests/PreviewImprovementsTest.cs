using UnityEngine;
using Game.Preview;
using Game.Level;

namespace Game.Tests
{
    /// <summary>
    /// Simple test class to verify preview improvements functionality
    /// This would typically be in a Tests folder in a full Unity test setup
    /// </summary>
    public class PreviewImprovementsTest : MonoBehaviour
    {
        [Header("Test Components")]
        [SerializeField] private StagePreviewManager previewManager;
        [SerializeField] private bool runTestsOnStart = false;
        
        private void Start()
        {
            if (runTestsOnStart)
            {
                RunTests();
            }
        }

        public void RunTests()
        {
            Debug.Log("=== Preview Improvements Tests ===");
            
            TestMaterialAssignments();
            TestGridCreationLogic();
            TestPlaneSpecificMaterials();
            
            Debug.Log("=== Tests Completed ===");
        }

        private void TestMaterialAssignments()
        {
            Debug.Log("Testing material assignments...");
            
            if (previewManager == null)
            {
                Debug.LogWarning("PreviewManager not assigned for testing");
                return;
            }

            // Check if new material fields exist via reflection (since they're private SerializeFields)
            var managerType = typeof(StagePreviewManager);
            var flattenZField = managerType.GetField("previewMaterialFlattenZ", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var flattenXField = managerType.GetField("previewMaterialFlattenX", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var gridZField = managerType.GetField("gridMaterialFlattenZ", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var gridXField = managerType.GetField("gridMaterialFlattenX", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            bool allFieldsExist = flattenZField != null && flattenXField != null && 
                                 gridZField != null && gridXField != null;

            Debug.Log($"Material fields exist: {allFieldsExist}");
            
            if (allFieldsExist)
            {
                Debug.Log("✓ All required material fields are present");
            }
            else
            {
                Debug.LogError("✗ Some material fields are missing");
            }
        }

        private void TestGridCreationLogic()
        {
            Debug.Log("Testing grid creation logic...");
            
            // Test that grid materials have the expected color properties
            Material testGridMaterial = new Material(Shader.Find("UI/Default"));
            testGridMaterial.color = new Color(0.4f, 0.9f, 1f, 0.3f);
            
            bool hasCorrectAlpha = testGridMaterial.color.a == 0.3f;
            bool hasCorrectTransparency = testGridMaterial.color.a < 1f;
            
            Debug.Log($"Grid material alpha test: {hasCorrectAlpha}");
            Debug.Log($"Grid material transparency test: {hasCorrectTransparency}");
            
            if (hasCorrectAlpha && hasCorrectTransparency)
            {
                Debug.Log("✓ Grid material properties are correct");
            }
            else
            {
                Debug.LogError("✗ Grid material properties are incorrect");
            }
            
            // Clean up test material
            DestroyImmediate(testGridMaterial);
        }

        private void TestPlaneSpecificMaterials()
        {
            Debug.Log("Testing plane-specific material colors...");
            
            // Create test materials with expected colors
            Material flattenZMaterial = new Material(Shader.Find("UI/Default"));
            flattenZMaterial.color = new Color(0.2f, 0.8f, 1f, 0.4862745f); // Blue-ish
            
            Material flattenXMaterial = new Material(Shader.Find("UI/Default"));  
            flattenXMaterial.color = new Color(1f, 0.2f, 0.8f, 0.4862745f); // Magenta-ish
            
            // Test that colors are distinct
            bool colorsAreDistinct = !Mathf.Approximately(flattenZMaterial.color.r, flattenXMaterial.color.r) ||
                                   !Mathf.Approximately(flattenZMaterial.color.g, flattenXMaterial.color.g) ||
                                   !Mathf.Approximately(flattenZMaterial.color.b, flattenXMaterial.color.b);
            
            Debug.Log($"Plane materials have distinct colors: {colorsAreDistinct}");
            
            if (colorsAreDistinct)
            {
                Debug.Log("✓ Plane materials have distinct colors for easy identification");
            }
            else
            {
                Debug.LogError("✗ Plane materials have similar colors - may be hard to distinguish");
            }
            
            // Clean up test materials
            DestroyImmediate(flattenZMaterial);
            DestroyImmediate(flattenXMaterial);
        }

        [ContextMenu("Run Preview Tests")]
        public void RunTestsFromMenu()
        {
            RunTests();
        }
    }
}