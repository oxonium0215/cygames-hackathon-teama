# Cygames Hackathon Team A - Architecture Documentation

## Phase 1: Namespace Migration and Assembly Definition Organization

This document describes the modernized codebase organization implemented in Phase 1, which focuses on namespace cleanup and assembly definition boundaries without changing runtime behavior.

## Namespace Migration

The codebase has been migrated from `POC.*` namespaces to clearer `Game.*` domains:

### Namespace Mapping

| Old Namespace | New Namespace | Changes |
|---------------|---------------|---------|
| `POC.Input` | `Game.Input` | PlayerInputRouter → PlayerInputRelay |
| `POC.Gameplay` | `Game.Player` | Class names unchanged (PlayerMotor, MovePlane) |
| `POC.GameplayProjection` | `Game.Projection` | Class names unchanged (PerspectiveProjectionManager) |
| `POC.Level` | `Game.Level` | ProjectionBuilder → GeometryProjector |
| `POC.Camera` | `Game.Camera` | VerticalCameraTracker → VerticalCameraFollow |
| (Debug classes) | `Game.Debugging` | EchoInput moved to proper namespace |

### Serialization Preservation

All moved and renamed classes use `[MovedFrom]` attributes to preserve serialized references in existing scenes and prefabs:

```csharp
[MovedFrom(true, sourceNamespace: "POC.Input", sourceClassName: "PlayerInputRouter")]
public class PlayerInputRelay : MonoBehaviour
```

## Assembly Definition Structure

The project now uses modular assemblies to clarify dependencies and speed up iteration:

```
Game.Core (base utilities, shared constants)
├── Game.Player (player movement, physics)
├── Game.Camera (camera systems)
├── Game.Level (level geometry, projection)
├── Game.Debugging (debug utilities)
├── Game.Projection (perspective switching logic)
│   ├── depends on: Game.Player, Game.Level
└── Game.Input (input handling)
    └── depends on: Game.Player, Game.Projection
```

### Assembly Dependencies

- **Game.Core**: Base assembly with no dependencies
- **Game.Player**: Player movement system (depends on Game.Core)
- **Game.Camera**: Camera tracking system (depends on Game.Core) 
- **Game.Level**: Level geometry and projection building (depends on Game.Core)
- **Game.Projection**: Perspective switching manager (depends on Game.Core, Game.Player, Game.Level)
- **Game.Input**: Input routing system (depends on Game.Core, Game.Player, Game.Projection)
- **Game.Debugging**: Debug utilities (depends on Game.Core)

## File Organization

Scripts are organized in matching directories:

```
Assets/Scripts/
├── Game.Core/           (shared utilities - minimal for now)
├── Game.Player/         (PlayerMotor.cs)
├── Game.Camera/         (VerticalCameraFollow.cs)
├── Game.Level/          (GeometryProjector.cs)
├── Game.Projection/     (PerspectiveProjectionManager.cs)
├── Game.Input/          (PlayerInputRelay.cs)
└── Game.Debugging/      (EchoInput.cs)
```

## Project Settings Updates

- **Product Name**: Changed from "cygames_POC_rotation" to "CygamesHackathon"
- **Metro Package Name**: Changed from "cygames_POC_rotation" to "CygamesHackathon"

## Phase 1 Constraints

**Explicitly preserved for behavioral consistency:**
- All serialized field names and default values remain unchanged
- No scene content modifications beyond automatic reference updates
- No functional logic changes
- Script meta GUIDs preserved where possible

## Phase 2: Internal Service Architecture

Building on Phase 1's namespace organization, Phase 2 introduces internal service classes to improve code organization and testability without requiring scene modifications.

### Player Services

The `PlayerMotor` MonoBehaviour now composes three internal services as `[SerializeField]` fields, keeping all functionality accessible in the Inspector while improving code organization:

#### Game.Player.GroundProbe
**Responsibilities:**
- Ground detection logic using Physics.CheckSphere
- Automatic ground check transform management and sizing
- Ground check positioning relative to collider bounds
- Layer mask and radius configuration

**Invariants:**
- Always maintains a valid ground check transform as child of player
- Ground check position tracks player's foot position with configurable skin offset
- Radius auto-sizing based on collider type (CapsuleCollider, CharacterController, or generic bounds)

#### Game.Player.PlaneMotion  
**Responsibilities:**
- Lateral movement speed and acceleration handling for both X and Z planes
- Plane locking constraints for projection system compatibility
- Rigidbody constraint management (freezing unused axes)
- Gravity and ground stick force application

**Invariants:**
- Active plane determines which lateral axis accepts input (X or Z)
- Plane lock enforces precise positioning on projection planes during perspective switches
- Axis constraints always freeze rotation on X/Z and unused position axis
- Gravity is always negative, acceleration values are always non-negative

#### Game.Player.JumpLogic
**Responsibilities:**
- Jump buffering (pressing jump before landing)
- Coyote time (jumping shortly after leaving ground)
- Variable height jumping with cut multiplier
- Landing slide mechanics (reduced control after hard landings)

**Invariants:**
- Jump buffer timer counts down from jumpBufferTime to 0
- Coyote timer resets to coyoteTime when grounded, counts down while airborne
- Landing slide only triggers on ground landing with sufficient fall speed
- Jump velocity calculated from jumpHeight and gravity using kinematic equations

### Projection Services

The `PerspectiveProjectionManager` MonoBehaviour decomposes complex rotation logic into three internal services:

#### Game.Projection.DepenetrationSolver
**Responsibilities:**  
- Vertical-only overlap resolution using Physics.ComputePenetration
- Iterative upward movement until clear of ground geometry
- Conservative fallback positioning above highest overlapping surface
- Configurable iteration limits and movement constraints

**Invariants:**
- Only moves player upward (never pushes down)
- Uses OverlapBox with configurable inflation factor to detect overlaps  
- Respects maximum resolve distance and step size limits
- Always syncs transforms after position changes

#### Game.Projection.ProjectionKinematics
**Responsibilities:**
- Player movement freezing/unfreezing during perspective transitions
- Rigidbody kinematic state management during rotation
- Inverse-projection coordinate mapping for smooth rotation paths  
- Velocity preservation and mapping between perspective planes

**Invariants:**
- Rotation freeze disables all player physics and input processing
- Kinematic state is always restored to original value after rotation
- Inverse-projection coordinates maintain spatial continuity (XY ↔ ZY mapping)
- Lateral velocity direction is preserved across plane switches (no sign flip)

#### Game.Projection.CameraPivotAdjuster
**Responsibilities:**
- Camera pivot repositioning with "never scroll down" behavior  
- Ground snapping via raycast with configurable distance and skin
- Camera distance and offset management for child camera transforms

**Invariants:**
- Camera pivot Y position never decreases (implements never-scroll-down)
- Ground snap uses raycast from above player with up/down distance limits
- Child camera position is always at -cameraDistance on local Z axis

### Service Integration Pattern

All services follow this integration pattern:
```csharp
[System.Serializable]
public class ServiceClass
{
    [SerializeField] private float configField = defaultValue;
    
    public void ValidateParameters() { /* clamp values */ }
    public ReturnType ServiceMethod(params) { /* logic */ }
}

public class HostMonoBehaviour : MonoBehaviour  
{
    [SerializeField] private ServiceClass service = new ServiceClass();
    
    private void OnValidate() => service.ValidateParameters();
    private void SomeMethod() => service.ServiceMethod(args);
}
```

This pattern maintains Inspector editability, serialization compatibility, and clear separation of concerns without requiring scene modifications.

## Phase 2 Planning

## Future Improvements (Phase 3+)

Upcoming phases will build on the internal service foundation:
- Scene splitting and reorganization for better loading performance  
- Additive scene loading system with Bootstrap/Systems/Stages separation
- Enhanced core utilities in Game.Core
- Component adapters for cleaner interfaces between systems
- External service extraction (e.g., separate MonoBehaviours for complex services)
- Physics and networking integration for multiplayer support

## Verification

To verify the migration:
1. Open `Scenes/RotationPOC.unity`
2. Confirm console is clean (no missing scripts)
3. Test player movement, jumping, and perspective switching
4. Verify URP pipeline and input systems still work correctly
5. Check Project Settings show "CygamesHackathon" for both Product Name and UWP Package Name