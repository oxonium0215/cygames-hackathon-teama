using System.Collections;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Game.Player;
using Game.Projection;
using Game.Physics;

namespace Game.Perspective
{
    /// <summary>
    /// Coordinates perspective switching between different views.
    /// Manages the interaction between camera, player, and projection systems.
    /// </summary>
    [MovedFrom("POC.GameplayProjection")]
    public class ViewSwitchCoordinator : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private PerspectiveCameraController cameraController;
        [SerializeField] private ProjectionBuilder projectionBuilder;
        [SerializeField] private PlayerMotor player;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Collider playerCollider;

        [Header("Views")]
        [SerializeField] private ProjectionAxis viewAProjection = ProjectionAxis.FlattenZ; // XY
        [SerializeField] private ProjectionAxis viewBProjection = ProjectionAxis.FlattenX; // ZY

        [Header("Ground Detection")]
        [SerializeField] private LayerMask groundMask;

        [Header("Player Behavior During Switch")]
        [SerializeField] private bool makePlayerKinematicDuringSwitch = true;
        [SerializeField] private bool jumpOnlyDuringSwitch = true;
        [SerializeField] private bool rotatePlayerDuringSwitch = true;
        [SerializeField] private bool fixYDuringRotation = true;
        [SerializeField] private bool resolveVerticalOverlapDuringRotation = true;

        [Header("Collision Resolution")]
        [SerializeField] private CollisionResolutionSettings collisionSettings = CollisionResolutionSettings.Default;

        private int currentViewIndex = 0;
        private bool switching;
        private Rigidbody playerRb;

        public bool IsSwitching => switching;
        public int CurrentViewIndex => currentViewIndex;

        private void Start()
        {
            if (!cameraController || !projectionBuilder)
            {
                Debug.LogError("[ViewSwitchCoordinator] Missing required components.");
                enabled = false;
                return;
            }

            // Auto-find player components if not assigned
            if (!player && playerTransform)
                player = playerTransform.GetComponent<PlayerMotor>();
            if (!playerTransform && player)
                playerTransform = player.transform;
            if (!playerCollider && playerTransform)
                playerCollider = playerTransform.GetComponent<Collider>();
            if (playerTransform) 
                playerRb = playerTransform.GetComponent<Rigidbody>();

            cameraController.Initialize(projectionBuilder.GetRotationCenter());
            InitializeView();
        }

        public void TogglePerspective()
        {
            if (!switching) StartCoroutine(SwitchToView(1 - currentViewIndex));
        }

        public void SwitchToViewImmediate(int viewIndex)
        {
            currentViewIndex = viewIndex;
            cameraController.SetViewImmediate(viewIndex);
            ApplyViewSettings();
        }

        private IEnumerator SwitchToView(int targetViewIndex)
        {
            switching = true;

            // Show sources and clear projections during switch
            projectionBuilder.SetSourcesVisible(true);
            projectionBuilder.ClearProjected();

            // Handle player-less camera rotation
            if (!playerTransform)
            {
                yield return cameraController.RotateToView(targetViewIndex);
                currentViewIndex = targetViewIndex;
                RebuildProjection();
                projectionBuilder.SetSourcesVisible(false);
                switching = false;
                yield break;
            }

            // Prepare player for rotation
            Vector3 preVel = playerRb ? playerRb.linearVelocity : Vector3.zero;
            yield return PreparePlayerForRotation();

            // Get rotation parameters
            ProjectionAxis startAxis = GetProjectionForView(currentViewIndex);
            Vector3 pStart = playerTransform.position;
            float fixedY = pStart.y;

            float seamZ = projectionBuilder.GetPlaneZ();
            float seamX = projectionBuilder.GetPlaneX();

            // Calculate inverse-projection coordinates
            float preX = pStart.x;
            float preZ = pStart.z;
            float xInv, zInv;

            if (startAxis == ProjectionAxis.FlattenZ)
            {
                xInv = preX;
                zInv = -preX;
            }
            else
            {
                xInv = -preZ;
                zInv = preZ;
            }

            // Perform rotation with player positioning
            yield return PerformRotationWithPlayer(targetViewIndex, xInv, zInv, fixedY);

            // Finalize the switch
            currentViewIndex = targetViewIndex;
            yield return FinalizePlayerAfterRotation(startAxis, preX, preZ, seamX, seamZ, preVel);

            switching = false;
        }

        private IEnumerator PreparePlayerForRotation()
        {
            // Freeze motor and input
            player.BeginRotationFreeze();
            if (jumpOnlyDuringSwitch) player.SetLateralEnabled(false);

            // Make kinematic during rotation
            if (playerRb && makePlayerKinematicDuringSwitch && !playerRb.isKinematic)
            {
                playerRb.linearVelocity = Vector3.zero;
                playerRb.angularVelocity = Vector3.zero;
                playerRb.isKinematic = true;
            }

            yield return null;
        }

        private IEnumerator PerformRotationWithPlayer(int targetViewIndex, float xInv, float zInv, float fixedY)
        {
            var cameraRotation = cameraController.RotateToView(targetViewIndex);
            
            while (cameraController.IsRotating)
            {
                if (rotatePlayerDuringSwitch)
                {
                    var p = playerTransform.position;
                    p.x = xInv;
                    p.z = zInv;
                    if (fixYDuringRotation) p.y = fixedY;
                    playerTransform.position = p;

                    // Resolve vertical overlaps while rotating
                    if (resolveVerticalOverlapDuringRotation && playerCollider)
                    {
                        if (CollisionResolver.ResolveVerticalOverlapUpwards(
                            playerTransform, playerCollider, groundMask, collisionSettings))
                        {
                            fixedY = Mathf.Max(fixedY, playerTransform.position.y);
                        }
                    }
                }
                else if (fixYDuringRotation)
                {
                    var p = playerTransform.position;
                    p.y = fixedY;
                    playerTransform.position = p;
                }

                yield return null;
            }
        }

        private IEnumerator FinalizePlayerAfterRotation(ProjectionAxis startAxis, float preX, float preZ, 
            float seamX, float seamZ, Vector3 preVel)
        {
            // Rebuild projection for new view
            RebuildProjection();
            projectionBuilder.SetSourcesVisible(false);

            // Final mapped position on target plane
            var p = playerTransform.position;
            if (startAxis == ProjectionAxis.FlattenZ)
            {
                p.x = seamX;
                p.z = -preX;
            }
            else
            {
                p.x = -preZ;
                p.z = seamZ;
            }
            playerTransform.position = p;

            // Lock to new plane
            var nextAxis = GetProjectionForView(currentViewIndex);
            float planeConst = (nextAxis == ProjectionAxis.FlattenZ) ? seamZ : seamX;
            player.ActivePlane = (nextAxis == ProjectionAxis.FlattenZ) ? MovePlane.X : MovePlane.Z;
            player.SetPlaneLock(player.ActivePlane, planeConst);

            // Post-rotation collision resolution
            CollisionResolver.ResolveVerticalOverlapUpwards(
                playerTransform, playerCollider, groundMask, collisionSettings);

            // Restore player state
            if (playerRb && makePlayerKinematicDuringSwitch)
            {
                playerRb.isKinematic = false;
            }

            if (jumpOnlyDuringSwitch) player.SetLateralEnabled(true);

            // Map inertia: preserve lateral direction, preserve vertical velocity
            float preLateral = (startAxis == ProjectionAxis.FlattenZ) ? preVel.x : preVel.z;
            Vector3 vFinal = Vector3.zero;
            if (nextAxis == ProjectionAxis.FlattenZ) vFinal.x = preLateral; 
            else vFinal.z = preLateral;
            vFinal.y = preVel.y;

            if (playerRb && !playerRb.isKinematic)
            {
                playerRb.linearVelocity = vFinal;
            }

            // Unfreeze motor
            player.EndRotationFreeze();

            yield return null;
        }

        private void InitializeView()
        {
            ApplyViewSettings();
            projectionBuilder.InitializeOnce();
            RebuildProjection();
            projectionBuilder.SetSourcesVisible(false);

            if (playerTransform)
            {
                var axis = GetProjectionForView(currentViewIndex);
                float planeConst = (axis == ProjectionAxis.FlattenZ) ? projectionBuilder.GetPlaneZ() : projectionBuilder.GetPlaneX();
                player.ActivePlane = (axis == ProjectionAxis.FlattenZ) ? MovePlane.X : MovePlane.Z;
                player.SetPlaneLock(player.ActivePlane, planeConst);

                var p = playerTransform.position;
                if (axis == ProjectionAxis.FlattenZ) p.z = planeConst; 
                else p.x = planeConst;
                playerTransform.position = p;

                CollisionResolver.ResolveVerticalOverlapUpwards(
                    playerTransform, playerCollider, groundMask, collisionSettings);
            }
        }

        private void ApplyViewSettings()
        {
            if (projectionBuilder.GetRotationCenter())
                cameraController.Initialize(projectionBuilder.GetRotationCenter());
        }

        private void RebuildProjection()
        {
            projectionBuilder.Rebuild(GetProjectionForView(currentViewIndex));
        }

        private ProjectionAxis GetProjectionForView(int viewIndex) => 
            (viewIndex == 0) ? viewAProjection : viewBProjection;
    }
}