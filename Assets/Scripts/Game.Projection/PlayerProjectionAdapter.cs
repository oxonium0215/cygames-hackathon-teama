using UnityEngine;
using Game.Player;
using Game.Level;

namespace Game.Projection
{
    /// <summary>
    /// Handles player state management during projection switches.
    /// </summary>
    public class PlayerProjectionAdapter : IPlayerProjectionAdapter
    {
        private readonly PlayerMotor player;
        private readonly Rigidbody playerRb;
        
        public PlayerProjectionAdapter(PlayerMotor player, Rigidbody playerRb)
        {
            this.player = player;
            this.playerRb = playerRb;
        }
        
        public bool PrepareForRotation(bool makeKinematic, bool jumpOnlyMode)
        {
            // Freeze motor and input
            player.BeginRotationFreeze();
            if (jumpOnlyMode) player.SetLateralEnabled(false);
            
            // Make kinematic during rotation
            bool originalKinematic = false;
            if (playerRb && makeKinematic)
            {
                originalKinematic = playerRb.isKinematic;
                if (!originalKinematic)
                {
                    playerRb.linearVelocity = Vector3.zero;
                    playerRb.angularVelocity = Vector3.zero;
                    playerRb.isKinematic = true;
                }
            }
            
            return originalKinematic;
        }
        
        public void RestoreAfterRotation(bool originalKinematic, bool jumpOnlyMode)
        {
            // Restore kinematic state
            if (playerRb)
            {
                playerRb.isKinematic = originalKinematic;
            }
            
            // Re-enable input
            if (jumpOnlyMode) player.SetLateralEnabled(true);
            
            // Unfreeze motor
            player.EndRotationFreeze();
        }
        
        public Vector3 MapVelocityBetweenAxes(Vector3 preRotationVelocity, ProjectionAxis sourceAxis, ProjectionAxis targetAxis)
        {
            // Map inertia: preserve lateral direction (no sign flip), preserve vertical velocity
            float preLateral = (sourceAxis == ProjectionAxis.FlattenZ) ? preRotationVelocity.x : preRotationVelocity.z;
            float newLateral = preLateral; // Keep direction as specified in original code
            
            Vector3 vFinal = Vector3.zero;
            if (targetAxis == ProjectionAxis.FlattenZ) 
                vFinal.x = newLateral; 
            else 
                vFinal.z = newLateral;
                
            // Preserve vertical velocity for natural fall
            vFinal.y = preRotationVelocity.y;
            
            return vFinal;
        }
        
        public void SetPlayerPlane(MovePlane newPlane, float planeConstant)
        {
            player.ActivePlane = newPlane;
            player.SetPlaneLock(newPlane, planeConstant);
        }
    }
}