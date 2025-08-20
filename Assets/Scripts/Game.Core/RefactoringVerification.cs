using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Game.Player;
using Game.Projection;
using Game.Camera;
using Game.Input;
using Game.Debugging;

namespace Game.Core
{
    /// <summary>
    /// Simple verification class to ensure all assemblies can reference each other correctly
    /// and that the refactoring is successful.
    /// </summary>
    public class RefactoringVerification : MonoBehaviour
    {
        [Header("Component References for Testing")]
        public PlayerMotor playerMotor;
        public PerspectiveProjectionManager projectionManager;
        public ProjectionBuilder projectionBuilder;
        public VerticalCameraTracker cameraTracker;
        public PlayerInputRouter inputRouter;
        public EchoInput echoInput;

        private void Start()
        {
            Debug.Log("[RefactoringVerification] All Game.* assemblies are properly referenced and compiling!");
            
            // Test enum references
            MovePlane testPlane = MovePlane.X;
            ProjectionAxis testAxis = ProjectionAxis.FlattenZ;
            
            Debug.Log($"[RefactoringVerification] MovePlane enum works: {testPlane}");
            Debug.Log($"[RefactoringVerification] ProjectionAxis enum works: {testAxis}");
        }
    }
}