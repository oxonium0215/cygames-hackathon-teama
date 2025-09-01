using UnityEngine;

public class PlayerStateController : MonoBehaviour
{
    // A single flag to disable all input
    public bool InputEnabled { get; private set; } = true;

    // Granular flags for specific actions
    public bool CanMove { get; private set; } = true;
    public bool CanJump { get; private set; } = true;
    public bool CanRotate { get; private set; } = true;
    public bool CanUseAbilities { get; private set; } = true; // For future expansion

    // Public methods to change the player's state
    public void EnableAllInput() => SetAllInput(true);
    public void DisableAllInput() => SetAllInput(false);

    public void SetAllInput(bool enabled)
    {
        InputEnabled = enabled;
        CanMove = enabled;
        CanJump = enabled;
        CanRotate = enabled;
        CanUseAbilities = enabled;
    }

    public void SetMovement(bool enabled) => CanMove = enabled;
    public void SetJumping(bool enabled) => CanJump = enabled;
    public void SetRotation(bool enabled) => CanRotate = enabled;
}