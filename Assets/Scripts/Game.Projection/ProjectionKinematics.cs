using UnityEngine;
using Game.Level;
using Game.Player;

namespace Game.Projection
{
    [System.Serializable]
    public class ProjectionKinematics
    {
        [Header("Behavior")]
        [SerializeField] private bool rotatePlayerDuringSwitch = true;
        [SerializeField] private bool makePlayerKinematicDuringSwitch = true;
        [SerializeField] private bool jumpOnlyDuringSwitch = true; // disable movement input
        [Tooltip("Keep the player's Y fixed to the pre-rotation height while rotating.")]
        [SerializeField] private bool fixYDuringRotation = true;

        public bool RotatePlayerDuringSwitch => rotatePlayerDuringSwitch;
        public bool MakePlayerKinematicDuringSwitch => makePlayerKinematicDuringSwitch;
        public bool JumpOnlyDuringSwitch => jumpOnlyDuringSwitch;
        public bool FixYDuringRotation => fixYDuringRotation;

        public void BeginRotationFreeze(PlayerMotor player)
        {
            player.BeginRotationFreeze();
            if (jumpOnlyDuringSwitch) player.SetLateralEnabled(false);
        }

        public void EndRotationFreeze(PlayerMotor player)
        {
            player.EndRotationFreeze();
            if (jumpOnlyDuringSwitch) player.SetLateralEnabled(true);
        }

        public bool MakeKinematicIfNeeded(Rigidbody playerRb, out bool originalKinematic)
        {
            originalKinematic = false;
            if (playerRb && makePlayerKinematicDuringSwitch)
            {
                originalKinematic = playerRb.isKinematic;
                if (!originalKinematic)
                {
                    playerRb.linearVelocity = Vector3.zero;
                    playerRb.angularVelocity = Vector3.zero;
                    playerRb.isKinematic = true;
                    return true;
                }
            }
            return false;
        }

        public void RestoreKinematicState(Rigidbody playerRb, bool originalKinematic)
        {
            if (playerRb && makePlayerKinematicDuringSwitch)
            {
                playerRb.isKinematic = originalKinematic;
            }
        }

        public void CalculateInverseProjectionCoordinates(Vector3 playerStartPos, ProjectionAxis startAxis, out float xInv, out float zInv)
        {
            float preX = playerStartPos.x;
            float preZ = playerStartPos.z;

            if (startAxis == ProjectionAxis.FlattenZ)
            {
                // XY -> ZY: xInv = preX, zInv = -preX
                xInv = preX;
                zInv = -preX;
            }
            else
            {
                // ZY -> XY: xInv = -preZ, zInv = preZ
                xInv = -preZ;
                zInv = preZ;
            }
        }

        public void UpdatePlayerRotationPosition(Transform playerTransform, float xInv, float zInv, float fixedY)
        {
            if (!rotatePlayerDuringSwitch) return;

            var p = playerTransform.position;
            p.x = xInv;
            p.z = zInv;
            if (fixYDuringRotation) p.y = fixedY;
            playerTransform.position = p;
        }

        public Vector3 MapInertiaToNewPlane(Vector3 preVel, ProjectionAxis startAxis, ProjectionAxis nextAxis)
        {
            // Map inertia: preserve lateral direction (no sign flip), preserve vertical velocity
            float preLateral = (startAxis == ProjectionAxis.FlattenZ) ? preVel.x : preVel.z;
            float newLateral = preLateral; // CHANGED: keep direction

            Vector3 vFinal = Vector3.zero;
            if (nextAxis == ProjectionAxis.FlattenZ) 
                vFinal.x = newLateral; 
            else 
                vFinal.z = newLateral;

            // Preserve vertical velocity for natural fall
            vFinal.y = preVel.y;
            return vFinal;
        }

        public void SetFinalPlayerPosition(Transform playerTransform, ProjectionAxis startAxis, float seamX, float seamZ, float preX, float preZ)
        {
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
        }

        public void SetupPlayerForNewPlane(PlayerMotor player, ProjectionAxis nextAxis, float seamX, float seamZ)
        {
            float planeConst = (nextAxis == ProjectionAxis.FlattenZ) ? seamZ : seamX;
            player.ActivePlane = (nextAxis == ProjectionAxis.FlattenZ) ? MovePlane.X : MovePlane.Z;
            player.SetPlaneLock(player.ActivePlane, planeConst);
        }
    }
}