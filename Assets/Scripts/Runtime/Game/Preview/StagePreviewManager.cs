using UnityEngine;
using System.Collections;
using Game.Level;
using Game.Player;
using Game.Projection;

namespace Game.Preview
{
    /// <summary>
    /// Manages stage preview functionality including camera transitions and preview object generation.
    /// This component can be attached to any GameObject as it finds its dependencies dynamically.
    /// For better organization, consider attaching it to the same GameObject as PerspectiveProjectionManager.
    /// </summary>
    public class StagePreviewManager : MonoBehaviour
    {
        #region Constants
        private const int MAX_RECURSION_DEPTH = 15;
        private const int MAX_PLAYER_COPY_DEPTH = 10;
        private const float DEFAULT_CAMERA_HEIGHT_OFFSET = 10f;
        private const float DEFAULT_CAMERA_X_POSITION = 16f;
        private const float DEFAULT_CAMERA_Z_POSITION = -16f;
        private const float DEFAULT_CAMERA_ANGLE_X = 30f;
        private const float DEFAULT_CAMERA_ANGLE_Y = -45f;
        private const float FLATTENED_AXIS_SCALE = 2f;
        private const float CAMERA_ROTATION_SPEED = 90f; // degrees per second
        private const float MIN_ROTATION_ANGLE = -90f; // left limit
        private const float MAX_ROTATION_ANGLE = 90f; // right limit
        #endregion

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
        [SerializeField] private Transform levelTransform;

        [Header("Preview Overlays")]
        [SerializeField] private Material previewMaterial;
        [SerializeField] private Material playerPreviewMaterial;

        private Vector3 originalCameraPosition;
        private Quaternion originalCameraRotation;
        private float originalCameraSize;
        private bool originalOrthographic;

        private Vector3 originalPlayerVelocity;
        private Vector3 originalPlayerAngularVelocity;
        private bool wasPlayerMotorEnabled;

        private bool isPreviewActive = false;
        private bool isTransitioning = false;
        private Coroutine transitionCoroutine;

        private GameObject flattenZPlanePreview;
        private GameObject flattenXPlanePreview;
        private GameObject playerPreview;

        // Camera rotation state during preview
        private Vector3 rotationCenter;
        private float currentRotationAngle;
        private float baseDistance;
        private float currentHorizontalInput;

        private void Awake()
        {
            if (!mainCamera) mainCamera = Camera.main;
            if (!cameraTransform && mainCamera) cameraTransform = mainCamera.transform;
            if (!player) player = GameObject.FindWithTag("Player")?.transform;
            if (!geometryProjector) geometryProjector = FindFirstObjectByType<GeometryProjector>();
            if (!playerMotor && player) playerMotor = player.GetComponent<PlayerMotor>();
            if (!playerRigidbody && player) playerRigidbody = player.GetComponent<Rigidbody>();
            if (!perspectiveProjectionManager) perspectiveProjectionManager = FindFirstObjectByType<PerspectiveProjectionManager>();
            if (!levelTransform) levelTransform = GameObject.Find("Level")?.transform;
        }

        private void Update()
        {
            // Handle continuous camera rotation during preview
            if (isPreviewActive && !isTransitioning && Mathf.Abs(currentHorizontalInput) > 0.01f)
            {
                ProcessCameraRotation();
            }
        }

        private void OnDisable()
        {
            DestroyPreviewOverlays();

            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }
        }

        public void StartPreview()
        {
            if (isPreviewActive || isTransitioning) return;
            if (!ValidateComponents()) return;

            // Block if perspective projection is switching OR if we are in preview mode
            if (perspectiveProjectionManager && (perspectiveProjectionManager.IsSwitching || isPreviewActive)) return;

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

            // Block if perspective projection is switching
            if (perspectiveProjectionManager && perspectiveProjectionManager.IsSwitching) return;

            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }

            transitionCoroutine = StartCoroutine(TransitionFromPreview());
        }

        private bool ValidateComponents()
        {
            bool isValid = true;

            if (!mainCamera)
            {
                Debug.LogWarning("StagePreviewManager: mainCamera is not assigned");
                isValid = false;
            }

            if (!cameraTransform)
            {
                Debug.LogWarning("StagePreviewManager: cameraTransform is not assigned");
                isValid = false;
            }

            if (!geometryProjector)
            {
                Debug.LogWarning("StagePreviewManager: geometryProjector is not assigned");
                isValid = false;
            }

            if (!player)
            {
                Debug.LogWarning("StagePreviewManager: player is not assigned");
                isValid = false;
            }

            return isValid;
        }

        private void SaveCurrentState()
        {
            if (!cameraTransform || !mainCamera) return;

            originalCameraPosition = cameraTransform.position;
            originalCameraRotation = cameraTransform.rotation;
            originalCameraSize = mainCamera.orthographicSize;
            originalOrthographic = mainCamera.orthographic;

            if (playerRigidbody)
            {
                originalPlayerVelocity = playerRigidbody.linearVelocity;
                originalPlayerAngularVelocity = playerRigidbody.angularVelocity;
            }

            if (playerMotor)
            {
                wasPlayerMotorEnabled = playerMotor.enabled;
            }
        }

        private IEnumerator TransitionToPreview()
        {
            isTransitioning = true;

            StopPlayerPhysics();

            if (geometryProjector)
            {
                geometryProjector.ClearProjected();
            }

            Vector3 targetPosition = new Vector3(DEFAULT_CAMERA_X_POSITION, originalCameraPosition.y + DEFAULT_CAMERA_HEIGHT_OFFSET, DEFAULT_CAMERA_Z_POSITION);
            if (player)
            {
                targetPosition.y = player.position.y + DEFAULT_CAMERA_HEIGHT_OFFSET;
            }

            Quaternion targetRotation = Quaternion.Euler(DEFAULT_CAMERA_ANGLE_X, DEFAULT_CAMERA_ANGLE_Y, 0f);

            float elapsed = 0f;
            Vector3 startPos = originalCameraPosition;
            Quaternion startRot = originalCameraRotation;
            float startSize = originalCameraSize;

            mainCamera.orthographic = true;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / transitionDuration;
                float curveT = transitionCurve.Evaluate(t);

                cameraTransform.position = Vector3.Lerp(startPos, targetPosition, curveT);
                cameraTransform.rotation = Quaternion.Slerp(startRot, targetRotation, curveT);
                mainCamera.orthographicSize = Mathf.Lerp(startSize, previewCameraSize, curveT);

                yield return null;
            }

            cameraTransform.position = targetPosition;
            cameraTransform.rotation = targetRotation;
            mainCamera.orthographicSize = previewCameraSize;

            CreatePreviewOverlays();

            // Initialize camera rotation state
            InitializeCameraRotation();

            isPreviewActive = true;
            isTransitioning = false;
        }

        private IEnumerator TransitionFromPreview()
        {
            isTransitioning = true;

            // Reset camera rotation input
            currentHorizontalInput = 0f;

            DestroyPreviewOverlays();

            float elapsed = 0f;
            Vector3 startPos = cameraTransform.position;
            Quaternion startRot = cameraTransform.rotation;
            float startSize = mainCamera.orthographicSize;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / transitionDuration;
                float curveT = transitionCurve.Evaluate(t);

                cameraTransform.position = Vector3.Lerp(startPos, originalCameraPosition, curveT);
                cameraTransform.rotation = Quaternion.Slerp(startRot, originalCameraRotation, curveT);
                mainCamera.orthographicSize = Mathf.Lerp(startSize, originalCameraSize, curveT);

                yield return null;
            }

            cameraTransform.position = originalCameraPosition;
            cameraTransform.rotation = originalCameraRotation;
            mainCamera.orthographicSize = originalCameraSize;
            mainCamera.orthographic = originalOrthographic;

            RestorePlayerPhysics();
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

            if (previewMaterial)
            {
                CreatePlanePreview(ref flattenZPlanePreview, "FlattenZ_Preview", ProjectionAxis.FlattenZ);
                CreatePlanePreview(ref flattenXPlanePreview, "FlattenX_Preview", ProjectionAxis.FlattenX);
            }

            if (playerPreviewMaterial && player)
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
            // Parent to Level instead of this StagePreviewManager
            Transform parentTransform = levelTransform ? levelTransform : transform;
            previewObject.transform.SetParent(parentTransform);

            Transform sourceRoot = geometryProjector.SourceRoot;
            if (sourceRoot)
            {
                foreach (Transform child in sourceRoot)
                {
                    CloneObjectForPreview(child, previewObject.transform, axis);
                }
            }
        }

        private void CloneObjectForPreview(Transform original, Transform parent, ProjectionAxis axis)
        {
            CloneObjectForPreviewRecursive(original, parent, axis, 0, MAX_RECURSION_DEPTH);
        }

        private void CloneObjectForPreviewRecursive(Transform original, Transform parent, ProjectionAxis axis, int currentDepth, int maxDepth)
        {
            if (currentDepth >= maxDepth) return;

            // Skip if original is already a preview object to prevent recursive preview generation
            if (original.name.Contains("Preview")) return;

            GameObject clone = new GameObject(original.name + "_Preview");
            clone.transform.SetParent(parent);

            clone.transform.localPosition = ProjectPosition(original.position, axis);
            clone.transform.localRotation = original.rotation;

            // Set uniform depth size
            Vector3 scale = original.localScale;
            if (axis == ProjectionAxis.FlattenZ)
            {
                scale.z = FLATTENED_AXIS_SCALE;
            } else if (axis == ProjectionAxis.FlattenX)
            {
                scale.x = FLATTENED_AXIS_SCALE;
            }
            clone.transform.localScale = scale;

            MeshRenderer originalRenderer = original.GetComponent<MeshRenderer>();
            MeshFilter originalFilter = original.GetComponent<MeshFilter>();

            if (originalRenderer && originalFilter && previewMaterial)
            {
                MeshRenderer cloneRenderer = clone.AddComponent<MeshRenderer>();
                MeshFilter cloneFilter = clone.AddComponent<MeshFilter>();

                cloneFilter.mesh = originalFilter.mesh;
                cloneRenderer.material = previewMaterial;
            }

            foreach (Transform child in original)
            {
                if (child == null) continue;
                // Skip if child is already a preview object to prevent recursive preview generation
                if (child.name.Contains("Preview")) continue;
                CloneObjectForPreviewRecursive(child, clone.transform, axis, currentDepth + 1, maxDepth);
            }
        }

        private Vector3 ProjectPosition(Vector3 position, ProjectionAxis axis)
        {
            Vector3 projected = position;

            if (axis == ProjectionAxis.FlattenZ)
            {
                projected.z = geometryProjector.GetPlaneZ();
            } else if (axis == ProjectionAxis.FlattenX)
            {
                projected.x = geometryProjector.GetPlaneX();
            }

            return projected;
        }

        private ProjectionAxis GetCurrentProjectionAxis()
        {
            ProjectionAxis currentAxis = ProjectionAxis.FlattenZ;

            if (cameraTransform)
            {
                float yaw = cameraTransform.eulerAngles.y;
                while (yaw < 0) yaw += 360;
                while (yaw >= 360) yaw -= 360;

                if (Mathf.Abs(yaw - 270f) < 45f)
                {
                    currentAxis = ProjectionAxis.FlattenX;
                }
            }

            return currentAxis;
        }

        private void CreatePlayerPreviews()
        {
            if (!player || !playerPreviewMaterial) return;

            CleanupPlayerPreviews();

            playerPreview = CreatePlayerPreviewObject("Player_Preview");
        }

        private void CleanupPlayerPreviews()
        {
            if (playerPreview != null)
            {
                DestroyImmediate(playerPreview);
                playerPreview = null;
            }

            // Check both under Level and under this transform for cleanup
            Transform[] searchTransforms = { levelTransform, transform };

            foreach (Transform searchTransform in searchTransforms)
            {
                if (searchTransform == null) continue;

                // Use GetComponentsInChildren to find all preview objects in hierarchy
                Transform[] allChildren = searchTransform.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in allChildren)
                {
                    if (child != null && child.name.Contains("Player_Preview"))
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }
        }

        private GameObject CreatePlayerPreviewObject(string name)
        {
            if (!player) return null;

            GameObject previewObj = new GameObject(name);
            // Parent to Level instead of this StagePreviewManager
            Transform parentTransform = levelTransform ? levelTransform : transform;
            previewObj.transform.SetParent(parentTransform);

            CopyPlayerVisualComponents(player.gameObject, previewObj);

            Vector3 currentPos = player.position;
            Vector3 previewPos = new Vector3(-currentPos.z, currentPos.y, -currentPos.x);
            previewObj.transform.position = previewPos;
            previewObj.transform.rotation = player.rotation;
            previewObj.transform.localScale = player.localScale;

            ApplyPreviewMaterialRecursive(previewObj, playerPreviewMaterial);
            RemoveCollidersRecursive(previewObj);

            return previewObj;
        }

        private void CopyPlayerVisualComponents(GameObject source, GameObject target)
        {
            CopyPlayerVisualComponentsRecursive(source, target, 0, MAX_PLAYER_COPY_DEPTH);
        }

        private void CopyPlayerVisualComponentsRecursive(GameObject source, GameObject target, int currentDepth, int maxDepth)
        {
            if (currentDepth >= maxDepth) return;

            // Skip if source is already a preview object to prevent recursive preview generation
            if (source.name.Contains("Preview")) return;

            MeshRenderer sourceMeshRenderer = source.GetComponent<MeshRenderer>();
            MeshFilter sourceMeshFilter = source.GetComponent<MeshFilter>();

            if (sourceMeshRenderer && sourceMeshFilter)
            {
                MeshRenderer targetMeshRenderer = target.AddComponent<MeshRenderer>();
                MeshFilter targetMeshFilter = target.AddComponent<MeshFilter>();

                targetMeshFilter.mesh = sourceMeshFilter.mesh;
                targetMeshRenderer.materials = sourceMeshRenderer.materials;
            }

            foreach (Transform child in source.transform)
            {
                if (child == null) continue;

                // Skip if child is already a preview object to prevent recursive preview generation
                if (child.name.Contains("Preview")) continue;

                MeshRenderer childRenderer = child.GetComponent<MeshRenderer>();
                MeshFilter childFilter = child.GetComponent<MeshFilter>();

                if (childRenderer || childFilter)
                {
                    GameObject childCopy = new GameObject(child.name);
                    childCopy.transform.SetParent(target.transform);
                    childCopy.transform.localPosition = child.localPosition;
                    childCopy.transform.localRotation = child.localRotation;
                    childCopy.transform.localScale = child.localScale;

                    CopyPlayerVisualComponentsRecursive(child.gameObject, childCopy, currentDepth + 1, maxDepth);
                }
            }
        }

        private void ApplyPreviewMaterialRecursive(GameObject obj, Material previewMat)
        {
            ApplyPreviewMaterialRecursive(obj, previewMat, 0, MAX_RECURSION_DEPTH);
        }

        private void ApplyPreviewMaterialRecursive(GameObject obj, Material previewMat, int currentDepth, int maxDepth)
        {
            if (!previewMat || currentDepth >= maxDepth) return;

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
                if (child == null) continue;
                ApplyPreviewMaterialRecursive(child.gameObject, previewMat, currentDepth + 1, maxDepth);
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
            if (flattenZPlanePreview != null)
            {
                DestroyImmediate(flattenZPlanePreview);
                flattenZPlanePreview = null;
            }

            if (flattenXPlanePreview != null)
            {
                DestroyImmediate(flattenXPlanePreview);
                flattenXPlanePreview = null;
            }

            // Use dedicated cleanup for player previews
            CleanupPlayerPreviews();
        }

        private void ReprojectGeometry()
        {
            if (!geometryProjector) return;

            ProjectionAxis currentAxis = GetCurrentProjectionAxis();
            geometryProjector.Rebuild(currentAxis);
        }

        private void InitializeCameraRotation()
        {
            if (!cameraTransform || !player) return;

            // Set rotation center to player position
            rotationCenter = player.position;

            // Calculate base distance from camera to center
            Vector3 cameraPos = cameraTransform.position;
            Vector3 toCameraVector = cameraPos - rotationCenter;
            baseDistance = new Vector3(toCameraVector.x, 0f, toCameraVector.z).magnitude;

            // Initialize current rotation angle from camera's Y rotation relative to center
            Vector3 directionToCamera = (cameraPos - rotationCenter).normalized;
            currentRotationAngle = Mathf.Atan2(directionToCamera.x, directionToCamera.z) * Mathf.Rad2Deg;
        }

        public void HandleCameraRotationInput(float horizontalInput)
        {
            // Store the current input value for continuous processing
            currentHorizontalInput = horizontalInput;
        }

        private void ProcessCameraRotation()
        {
            if (!cameraTransform) return;

            // Rotate camera around the center point while maintaining top-down perspective
            float rotationDelta = currentHorizontalInput * CAMERA_ROTATION_SPEED * Time.unscaledDeltaTime;
            float newRotationAngle = currentRotationAngle + rotationDelta;

            // Clamp rotation angle to limits
            newRotationAngle = Mathf.Clamp(newRotationAngle, MIN_ROTATION_ANGLE, MAX_ROTATION_ANGLE);

            // Only update if the angle actually changed (respects limits)
            if (Mathf.Abs(newRotationAngle - currentRotationAngle) > 0.001f)
            {
                currentRotationAngle = newRotationAngle;

                // Calculate new camera position around the center
                float angleRad = currentRotationAngle * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Sin(angleRad) * baseDistance, 0f, Mathf.Cos(angleRad) * baseDistance);

                Vector3 newPosition = rotationCenter + offset;
                newPosition.y = cameraTransform.position.y; // Keep original height

                cameraTransform.position = newPosition;

                // Keep the camera rotation fixed at the default preview angles to maintain top-down perspective
                cameraTransform.rotation = Quaternion.Euler(DEFAULT_CAMERA_ANGLE_X, DEFAULT_CAMERA_ANGLE_Y + currentRotationAngle, 0f);
            }
        }

        public bool IsPreviewActive => isPreviewActive;
        public bool IsTransitioning => isTransitioning;
    }
}