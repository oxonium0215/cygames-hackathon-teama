# Input Implementation Guide

This beginner-friendly guide explains the current input system architecture and how to extend it for future input functionality.

## Current Input Path Overview

The input flows through three main components:

```
Unity Input System → PlayerInputRelay → UnityPlayerInput → PlayerMotor
                        ↓
                 PerspectiveProjectionManager
```

## Component Breakdown

### 1. PlayerInputRelay (MonoBehaviour)
**Location**: `Assets/Scripts/Game.Input/PlayerInputRelay.cs`

This is the entry point for all input from Unity's Input System. It acts as a router that:

- Receives `InputAction.CallbackContext` events from Unity Input System
- Forwards input to both the adapter (`UnityPlayerInput`) and the motor (`PlayerMotor`) 
- Implements input suppression logic for perspective switching

**Key Methods**:
- `OnMove(InputAction.CallbackContext)` - Handles movement input with suppression logic
- `OnJump(InputAction.CallbackContext)` - Handles jump input (performed and canceled)  
- `OnSwitchView(InputAction.CallbackContext)` - Triggers perspective switching

**Input Suppression Logic**:
```csharp
// Early-out: suppress lateral input during perspective switching if jump-only mode is enabled
if (perspective != null && perspective.IsSwitching && perspective.JumpOnlyDuringSwitch)
{
    return; // Skip forwarding to motor, but jump input remains unaffected
}
```

### 2. UnityPlayerInput (Pure C# Adapter)
**Location**: `Assets/Scripts/Game.Input/UnityPlayerInput.cs`

This adapter implements the `IPlayerInput` interface and provides:

- Event-driven state updates (no polling required)
- Transient flag management (edge detection for button presses)
- Clean interface between Unity Input System and game logic

**Key Methods**:
- `SetMove(Vector2)` - Updates movement vector
- `OnJumpPerformed()` - Sets jump held and pressed flags  
- `OnJumpCanceled()` - Clears jump held flag
- `ClearTransient()` - Resets edge flags (called once per frame)

### 3. PlayerMotor (MonoBehaviour)
**Location**: `Assets/Scripts/Game.Player/PlayerMotor.cs`

The final destination for input, handles:

- Movement processing through services (`IGroundProbe`, `IPlaneMotion`)
- Jump buffering and coyote time
- Physics integration

**Key Methods**:
- `SetMove(Vector2)` - Stores movement input for physics processing
- `QueueJump()` - Triggers jump with buffering
- `JumpCanceled()` - Implements jump-cut mechanics

## How to Add New Input

### Step 1: Add Unity Input Action

1. Open the Input Action asset in Unity
2. Add your new action (e.g., "Dash", "Attack", etc.)
3. Assign appropriate bindings (keyboard, gamepad, etc.)
4. Regenerate the Input Action C# class if using code generation

### Step 2: Extend PlayerInputRelay

Add a new public method to handle your input:

```csharp
public void OnDash(InputAction.CallbackContext ctx)
{
    if (ctx.performed)
    {
        // Add your logic here
        // You can access suppression state: perspective.IsSwitching
        
        // Forward to motor if needed
        motor?.TriggerDash();
    }
}
```

**Wire it up**: In the PlayerInput component inspector, add your new method to the appropriate action's events.

### Step 3: Extend IPlayerInput Interface (if needed)

If you need persistent state tracking, extend the adapter:

```csharp
public interface IPlayerInput
{
    Vector2 Move { get; }
    bool JumpHeld { get; }
    bool JumpPressedThisFrame { get; }
    bool DashPressedThisFrame { get; } // New property
    void ClearTransient();
}
```

### Step 4: Update UnityPlayerInput Implementation

```csharp
public class UnityPlayerInput : IPlayerInput
{
    private bool dashPressed;
    
    public bool DashPressedThisFrame => dashPressed;
    
    public void OnDashPerformed() => dashPressed = true;
    
    public void ClearTransient()
    {
        dashPressed = false; // Clear edge flag
    }
}
```

### Step 5: Update PlayerInputRelay to use Adapter

```csharp
public void OnDash(InputAction.CallbackContext ctx)
{
    if (ctx.performed)
    {
        playerInput?.OnDashPerformed(); // Update adapter
        
        // Apply suppression logic if needed
        if (perspective?.IsSwitching == true) return;
        
        motor?.TriggerDash();
    }
}
```

### Step 6: Extend Target Component

Add the new functionality to `PlayerMotor` or your target component:

```csharp
public void TriggerDash()
{
    if (rotationFrozen) return; // Respect rotation freeze
    
    // Implement dash logic
}
```

## Best Practices

### Edge Flag Management
- **Always** call `playerInput?.ClearTransient()` once per frame in `PlayerInputRelay.Update()`
- Use edge flags (`PressedThisFrame`) for one-shot actions like dash, attack, etc.
- Use held flags (`Held`) for continuous actions like charging

### Input Suppression
- Consider whether new input should be suppressed during perspective switching
- Jump input is intentionally **not** suppressed to allow mid-air perspective changes
- Movement input **is** suppressed to prevent disorientation during rotation

### Null Safety
- Always use null-conditional operators (`?.`) when calling methods on serialized references
- This prevents errors if components are missing or destroyed

### Architecture Consistency
- Keep `PlayerInputRelay` as a simple router - avoid complex game logic here
- Put game logic in the target components (`PlayerMotor`, etc.)
- Use the adapter pattern (`UnityPlayerInput`) to decouple Unity Input System from game logic

## Common Pitfalls

### 1. Forgetting to Clear Transient Flags
**Problem**: Edge detection flags never reset, causing repeated triggering
**Solution**: Ensure `ClearTransient()` is called once per frame

### 2. Input System Event Mapping
**Problem**: Unity Input Action events not properly connected to PlayerInputRelay methods
**Solution**: Check the PlayerInput component inspector - each action needs its callback methods assigned

### 3. Rotation Freeze Handling
**Problem**: New input continues to work during perspective switching when it shouldn't
**Solution**: Check `rotationFrozen` state in target components, or use suppression logic in PlayerInputRelay

### 4. Timing Issues with Edge Detection
**Problem**: Edge flags checked before or after they're cleared
**Solution**: Input processing should happen in `Update()` (UI/Logic) or `FixedUpdate()` (Physics), and clearing should happen in `Update()` after input processing

## Testing Your Input

1. **Play Mode Testing**: Enter play mode and test the input during normal gameplay and during perspective switching
2. **Edge Case Testing**: Test input timing around perspective switch start/end
3. **Multi-Input Testing**: Test combinations (e.g., jump + dash, move + attack)
4. **Null Reference Testing**: Test with missing component references to ensure graceful failure

## Examples of Advanced Input Patterns

### Charged Attack
```csharp
// In IPlayerInput
bool AttackHeld { get; }
float AttackHoldTime { get; }

// In PlayerInputRelay  
public void OnAttack(InputAction.CallbackContext ctx)
{
    if (ctx.performed) playerInput?.OnAttackStarted();
    else if (ctx.canceled) playerInput?.OnAttackReleased();
}

// Usage in target component
if (playerInput.AttackHeld)
{
    chargeTime += Time.deltaTime;
    // Visual charging effects
}
```

### Combo System
```csharp
// Track sequence of inputs with timing
private List<InputType> inputSequence = new List<InputType>();
private float lastInputTime;

// Clear sequence if too much time passes
if (Time.time - lastInputTime > comboWindow)
    inputSequence.Clear();
```

This guide provides a solid foundation for extending the input system while maintaining the existing clean architecture and behavior consistency.