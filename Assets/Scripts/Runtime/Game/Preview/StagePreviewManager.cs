using UnityEngine;
using System.Collections;
using Game.Level;
using Game.Player;
using Game.Projection;

namespace Game.Preview
{
    /// <summary>
    /// Manages 3D stage preview functionality, including camera transitions,
    /// terrain restoration, and player physics control.
    /// </summary>
    public class StagePreviewManager : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float previewCameraSize = 10f;
        [SerializeField] private float transitionDuration = 0.5f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Components")]
        [SerializeField] private Transform player;
        [SerializeField] private GeometryProjector geometryProjector;
        [SerializeField] private PlayerMotor playerMotor;
        [SerializeField] private Rigidbody playerRigidbody;
        [SerializeField] private PerspectiveProjectionManager perspectiveProjectionManager;

        [Header("Preview Overlays")]
        [SerializeField] private Material previewMaterial;
        [SerializeField] private Material playerPreviewMaterial;

        // Camera state storage
        private Vector3 originalCameraPosition;
        private Quaternion originalCameraRotation;
        private float originalCameraSize;
        private bool originalOrthographic;
        
        // Player state storage
        private Vector3 originalPlayerVelocity;
        private Vector3 originalPlayerAngularVelocity;
        private bool wasPlayerMotorEnabled;

        // Preview state
        private bool isPreviewActive = false;
        private bool isTransitioning = false;
        private Coroutine transitionCoroutine;

        // Preview overlay objects
        private GameObject xyPlanePreview;
        private GameObject zyPlanePreview;
        private GameObject playerXYPreview;
        private GameObject playerZYPreview;

        private void Awake()
        {
            // Initialize components if not assigned
            if (!mainCamera) mainCamera = Camera.main;
            if (!cameraTransform && mainCamera) cameraTransform = mainCamera.transform;
            if (!player) player = GameObject.FindWithTag("Player")?.transform;
            if (!geometryProjector) geometryProjector = FindFirstObjectByType<GeometryProjector>();
            if (!playerMotor && player) playerMotor = player.GetComponent<PlayerMotor>();
            if (!playerRigidbody && player) playerRigidbody = player.GetComponent<Rigidbody>();
            if (!perspectiveProjectionManager) perspectiveProjectionManager = FindFirstObjectByType<PerspectiveProjectionManager>();
        }

        private void OnDestroy()
        {
            // Clean up any remaining preview objects
            DestroyPreviewOverlays();
            
            // Stop any running coroutines
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }
        }

        public void StartPreview()
        {
            if (isPreviewActive || isTransitioning) return;
            if (!ValidateComponents()) return;

            SaveCurrentState();
            
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }
            
            transitionCoroutine = StartCoroutine(TransitionToPreview());
        }

        public void EndPreview()
        {
            if (!isPreviewActive || isTransitioning) return;
            if (!ValidateComponents()) return;

            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }
            
            transitionCoroutine = StartCoroutine(TransitionFromPreview());
        }

        private bool ValidateComponents()
        {
            if (!mainCamera || !cameraTransform)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[StagePreview] Main camera or camera transform not found.");
#endif
                return false;
            }

            if (!geometryProjector)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[StagePreview] GeometryProjector not found.");
#endif
                return false;
            }

            return true;
        }

        private void SaveCurrentState()
        {
            if (!cameraTransform || !mainCamera) return;

            // Save camera state
            originalCameraPosition = cameraTransform.position;
            originalCameraRotation = cameraTransform.rotation;
            originalCameraSize = mainCamera.orthographicSize;
            originalOrthographic = mainCamera.orthographic;

            // Save player physics state
            if (playerRigidbody)
            {
                originalPlayerVelocity = playerRigidbody.linearVelocity;
                originalPlayerAngularVelocity = playerRigidbody.angularVelocity;
            }
            
            // Save PlayerMotor state
            if (playerMotor)
            {
                wasPlayerMotorEnabled = playerMotor.enabled;
            }
        }

        private IEnumerator TransitionToPreview()
        {
            isTransitioning = true;

            // Stop player physics
            StopPlayerPhysics();

            // Clear terrain projections
            if (geometryProjector)
            {
                geometryProjector.ClearProjected();
            }

            // Calculate preview camera position - use absolute coordinates (16, player.y+10, -16)
            Vector3 targetPosition = new Vector3(16f, originalCameraPosition.y + 10f, -16f);
            if (player)
            {
                targetPosition.y = player.position.y + 10f;
            }

            // Smooth camera transition
            float elapsed = 0f;
            Vector3 startPos = originalCameraPosition;
            Quaternion startRot = originalCameraRotation;
            float startSize = originalCameraSize;

            // Set camera to orthographic for better preview view
            mainCamera.orthographic = true;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / transitionDuration;
                float curveT = transitionCurve.Evaluate(t);

                // Interpolate camera position and properties
                cameraTransform.position = Vector3.Lerp(startPos, targetPosition, curveT);
                cameraTransform.rotation = Quaternion.Slerp(startRot, Quaternion.identity, curveT);
                mainCamera.orthographicSize = Mathf.Lerp(startSize, previewCameraSize, curveT);

                yield return null;
            }

            // Ensure final values are set
            cameraTransform.position = targetPosition;
            cameraTransform.rotation = Quaternion.identity;
            mainCamera.orthographicSize = previewCameraSize;

            // Create preview overlays
            CreatePreviewOverlays();

            isPreviewActive = true;
            isTransitioning = false;
        }

        private IEnumerator TransitionFromPreview()
        {
            isTransitioning = true;

            // Destroy preview overlays
            DestroyPreviewOverlays();

            // Smooth transition back to original camera state
            float elapsed = 0f;
            Vector3 startPos = cameraTransform.position;
            Quaternion startRot = cameraTransform.rotation;
            float startSize = mainCamera.orthographicSize;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / transitionDuration;
                float curveT = transitionCurve.Evaluate(t);

                // Interpolate back to original state
                cameraTransform.position = Vector3.Lerp(startPos, originalCameraPosition, curveT);
                cameraTransform.rotation = Quaternion.Slerp(startRot, originalCameraRotation, curveT);
                mainCamera.orthographicSize = Mathf.Lerp(startSize, originalCameraSize, curveT);

                yield return null;
            }

            // Ensure final values are set
            cameraTransform.position = originalCameraPosition;
            cameraTransform.rotation = originalCameraRotation;
            mainCamera.orthographicSize = originalCameraSize;
            mainCamera.orthographic = originalOrthographic;

            // Restore player physics
            RestorePlayerPhysics();
            
            // Reproject geometry back to current view
            ReprojectGeometry();

            isPreviewActive = false;
            isTransitioning = false;
        }

        private void StopPlayerPhysics()
        {
            if (playerRigidbody)
            {
                playerRigidbody.linearVelocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }

            if (playerMotor)
            {
                // Disable PlayerMotor component to prevent it from updating physics
                playerMotor.enabled = false;
            }
        }

        private void RestorePlayerPhysics()
        {
            if (playerMotor)
            {
                playerMotor.enabled = wasPlayerMotorEnabled;
            }
        }

        private void CreatePreviewOverlays()
        {
            if (!geometryProjector || !geometryProjector.SourceRoot) return;

            // Only create overlays if preview material is assigned
            if (previewMaterial)
            {
                // Create XY plane preview (flatten Z)
                CreatePlanePreview(ref xyPlanePreview, "XY_Preview", ProjectionAxis.FlattenZ);
                
                // Create ZY plane preview (flatten X)  
                CreatePlanePreview(ref zyPlanePreview, "ZY_Preview", ProjectionAxis.FlattenX);
            }

            // Create player position previews if player preview material is assigned
            if (playerPreviewMaterial)
            {
                CreatePlayerPreviews();
            }
        }

        private void CreatePlanePreview(ref GameObject previewObject, string name, ProjectionAxis axis)
        {
            if (previewObject != null)
            {
                DestroyImmediate(previewObject);
            }

            previewObject = new GameObject(name);
            previewObject.transform.SetParent(transform);

            // Create a copy of geometry for preview
            Transform sourceRoot = geometryProjector.SourceRoot;
            if (sourceRoot)
            {
                // Clone the geometry hierarchy
                foreach (Transform child in sourceRoot)
                {
                    CloneObjectForPreview(child, previewObject.transform, axis);
                }
            }
        }

        private void CloneObjectForPreview(Transform original, Transform parent, ProjectionAxis axis)
        {
            GameObject clone = new GameObject(original.name + "_Preview");
            clone.transform.SetParent(parent);

            // Copy transform
            clone.transform.localPosition = ProjectPosition(original.position, axis);
            clone.transform.localRotation = original.rotation;
            clone.transform.localScale = original.localScale;

            // Copy mesh renderers with preview material
            MeshRenderer originalRenderer = original.GetComponent<MeshRenderer>();
            MeshFilter originalFilter = original.GetComponent<MeshFilter>();
            
            if (originalRenderer && originalFilter && previewMaterial)
            {
                MeshRenderer cloneRenderer = clone.AddComponent<MeshRenderer>();
                MeshFilter cloneFilter = clone.AddComponent<MeshFilter>();
                
                cloneFilter.mesh = originalFilter.mesh;
                cloneRenderer.material = previewMaterial;
            }

            // Recursively clone children
            foreach (Transform child in original)
            {
                CloneObjectForPreview(child, clone.transform, axis);
            }
        }

        private Vector3 ProjectPosition(Vector3 position, ProjectionAxis axis)
        {
            Vector3 projected = position;
            
            if (axis == ProjectionAxis.FlattenZ)
            {
                projected.z = geometryProjector.GetPlaneZ();
            }
            else if (axis == ProjectionAxis.FlattenX)
            {
                projected.x = geometryProjector.GetPlaneX();
            }
            
            return projected;
        }

        private void CreatePlayerPreviews()
        {
            if (!player || !playerPreviewMaterial) return;

            // Create XY plane player preview
            if (playerXYPreview != null) DestroyImmediate(playerXYPreview);
            playerXYPreview = CreatePlayerPreviewObject("Player_XY_Preview", ProjectionAxis.FlattenZ);

            // Create ZY plane player preview
            if (playerZYPreview != null) DestroyImmediate(playerZYPreview);  
            playerZYPreview = CreatePlayerPreviewObject("Player_ZY_Preview", ProjectionAxis.FlattenX);
        }

        private GameObject CreatePlayerPreviewObject(string name, ProjectionAxis axis)
        {
            if (!player) return null;

            // Create a copy of the player GameObject
            GameObject previewObj = new GameObject(name);
            previewObj.transform.SetParent(transform);
            
            // Copy all visual components from the player
            CopyPlayerVisualComponents(player.gameObject, previewObj);
            
            // Position the preview
            Vector3 previewPos = ProjectPosition(player.position, axis);
            previewObj.transform.position = previewPos;
            previewObj.transform.rotation = player.rotation;
            previewObj.transform.localScale = player.localScale;

            // Apply preview material to all renderers
            ApplyPreviewMaterialRecursive(previewObj, playerPreviewMaterial);
            
            // Remove all colliders since this is just for visualization
            RemoveCollidersRecursive(previewObj);

            return previewObj;
        }

        private void CopyPlayerVisualComponents(GameObject source, GameObject target)
        {
            // Copy MeshRenderer and MeshFilter if they exist
            MeshRenderer sourceMeshRenderer = source.GetComponent<MeshRenderer>();
            MeshFilter sourceMeshFilter = source.GetComponent<MeshFilter>();
            
            if (sourceMeshRenderer && sourceMeshFilter)
            {
                MeshRenderer targetMeshRenderer = target.AddComponent<MeshRenderer>();
                MeshFilter targetMeshFilter = target.AddComponent<MeshFilter>();
                
                targetMeshFilter.mesh = sourceMeshFilter.mesh;
                targetMeshRenderer.materials = sourceMeshRenderer.materials;
            }

            // Recursively copy child objects with visual components
            foreach (Transform child in source.transform)
            {
                MeshRenderer childRenderer = child.GetComponent<MeshRenderer>();
                MeshFilter childFilter = child.GetComponent<MeshFilter>();
                
                if (childRenderer || childFilter)
                {
                    GameObject childCopy = new GameObject(child.name);
                    childCopy.transform.SetParent(target.transform);
                    childCopy.transform.localPosition = child.localPosition;
                    childCopy.transform.localRotation = child.localRotation;
                    childCopy.transform.localScale = child.localScale;
                    
                    CopyPlayerVisualComponents(child.gameObject, childCopy);
                }
            }
        }

        private void ApplyPreviewMaterialRecursive(GameObject obj, Material previewMat)
        {
            if (!previewMat) return;
            
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer)
            {
                Material[] materials = new Material[renderer.materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = previewMat;
                }
                renderer.materials = materials;
            }

            foreach (Transform child in obj.transform)
            {
                ApplyPreviewMaterialRecursive(child.gameObject, previewMat);
            }
        }

        private void RemoveCollidersRecursive(GameObject obj)
        {
            Collider[] colliders = obj.GetComponentsInChildren<Collider>();
            for (int i = colliders.Length - 1; i >= 0; i--)
            {
                DestroyImmediate(colliders[i]);
            }
        }

        private void DestroyPreviewOverlays()
        {
            if (xyPlanePreview != null)
            {
                DestroyImmediate(xyPlanePreview);
                xyPlanePreview = null;
            }

            if (zyPlanePreview != null)
            {
                DestroyImmediate(zyPlanePreview);
                zyPlanePreview = null;
            }

            if (playerXYPreview != null)
            {
                DestroyImmediate(playerXYPreview);
                playerXYPreview = null;
            }

            if (playerZYPreview != null)
            {
                DestroyImmediate(playerZYPreview);
                playerZYPreview = null;
            }
        }

        private void ReprojectGeometry()
        {
            if (!geometryProjector) return;

            // Determine current projection axis based on camera orientation
            // If camera is looking along X-axis (viewB), use FlattenX (ZY projection)  
            // If camera is looking along Z-axis (viewA), use FlattenZ (XY projection)
            ProjectionAxis currentAxis = ProjectionAxis.FlattenZ; // Default XY
            
            if (cameraTransform)
            {
                // Check camera's Y rotation to determine which view we're in
                float yaw = cameraTransform.eulerAngles.y;
                // Normalize yaw to 0-360 range
                while (yaw < 0) yaw += 360;
                while (yaw >= 360) yaw -= 360;
                
                // If yaw is close to -90 degrees (270), we're in view B (ZY)
                if (Mathf.Abs(yaw - 270f) < 45f)
                {
                    currentAxis = ProjectionAxis.FlattenX; // ZY projection
                }
            }

            geometryProjector.Rebuild(currentAxis);
        }

        public bool IsPreviewActive => isPreviewActive;
        public bool IsTransitioning => isTransitioning;
    }
}