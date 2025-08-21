# Cygames Hackathon Team A - Architecture Documentation

## Phase 1: Namespace Migration and Assembly Definition Organization

This document describes the modernized codebase organization implemented in Phase 1, which focused on namespace cleanup and assembly definition boundaries without changing runtime behavior.

## Phase 2A: Core + Player + Projection Service Extraction

**Completed in Phase 2A:**
- Removed `snapToGroundAfterRotation` feature from PerspectiveProjectionManager
- Extracted pure C# services from PlayerMotor and PerspectiveProjectionManager  
- Introduced interfaces for clean service boundaries
- Manual composition pattern (no DI framework)
- EditMode tests for extracted services
- Normalized script defaults to match RotationPOC.unity values

### Service Architecture

#### PlayerMotor Façade → GroundProbe + PlaneMotion Services

**PlayerMotor** now acts as a façade that constructs and delegates to:

- **IGroundProbe / GroundProbe**: Handles ground detection, coyote time, and jump buffer logic
  - `UpdateGroundCheck()`: Performs Physics.CheckSphere and updates timing
  - `CanJump()`: Validates jump timing based on coyote time and buffer
  - `ConsumeJump()`: Resets timers when jump is executed
  
- **IPlaneMotion / PlaneMotion**: Handles lateral input projection and landing slide effects  
  - `ApplyLateralMovement()`: Projects 2D input to 3D movement on active plane
  - `UpdateLandingSlide()`: Manages landing slide state and timing
  - `GetLandingSlideMultipliers()`: Returns acceleration/deceleration modifiers

#### PerspectiveProjectionManager Façade → Multiple Projection Services

**PerspectiveProjectionManager** now acts as a façade that constructs and delegates to:

- **IProjectionController / ProjectionController**: State and timing for perspective switches
  - `BeginSwitch()`: Initiates rotation with duration and easing
  - `UpdateRotation()`: Returns interpolated progress (0-1) 
  - `CompleteSwitch()`: Finalizes rotation state

- **IPlayerProjectionAdapter / PlayerProjectionAdapter**: Player state during projection switching
  - `PrepareForRotation()`: Freezes motor, sets kinematic state  
  - `RestoreAfterRotation()`: Restores player state post-rotation
  - `MapVelocityBetweenAxes()`: Preserves lateral velocity direction between projections
  - `SetPlayerPlane()`: Updates active plane and plane lock

- **ICameraProjectionAdapter / CameraProjectionAdapter**: Camera pivot adjustments
  - `RepositionPivotToCenter()`: Handles pivot positioning with upward-only rule
  - `UpdateRotation()`: Interpolates camera yaw during switches
  - `SetCameraDistance()`: Positions child camera at specified distance

- **IDepenetrationSolver / DepenetrationSolver**: Vertical-only depenetration
  - `ResolveVerticalOverlapUpwards()`: Uses OverlapBox + ComputePenetration  
  - Iteration caps and total displacement limits
  - Conservative fallback using bounds positioning

### Interface Design

All services use interfaces for clean boundaries and testability:
- Placed in appropriate Game.* assemblies (Player interfaces in Game.Player, etc.)
- Pure C# implementations (no MonoBehaviour inheritance)  
- Manual composition in MonoBehaviour façades
- Stateless where possible, or explicit state management

### Testing

**EditMode Tests** validate extracted service logic:
- **GroundProbeTests**: Coyote time, jump buffer, timing edge cases
- **PlaneMotionTests**: Input projection to X/Z planes, landing slide mechanics
- **ProjectionControllerTests**: State transitions, progress calculation, timing
- **DepenetrationSolverTests**: Upward resolution, iteration caps, layer filtering

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

## Phase 2B: Camera + Level Refactor with Behavior Preservation

This phase introduced a façade pattern to extract pure utilities from MonoBehaviours while maintaining identical runtime behavior and serialized field compatibility.

### Camera Module Internal Refactor

**VerticalCameraFollow** now acts as a façade delegating to pure utilities:

- **IDeadZone** interface with **TopDeadZonePolicy** implementation
  - Computes threshold Y (pivotY + topDeadZone) and desired Y (playerY - topDeadZone)
  - Encapsulates dead-zone logic without external dependencies

- **ISmoothing** interface with **ConstantSpeedSmoothing** implementation
  - Pure speed-based movement using Mathf.MoveTowards
  - SmoothDamp remains directly in the façade to preserve exact _velY state behavior

- **Behavior preservation**: neverScrollDown semantics, serialized field names, default values, and update order remain identical

### Level Module Internal Refactor

**GeometryProjector** now acts as a façade delegating to:

- **ProjectorPass** class handling idempotent cloning process
  - Mesh/MeshRenderer cloning with material assignment (copyMaterials flag)
  - Collider cloning (BoxCollider, CapsuleCollider, MeshCollider)
  - Layer assignment (projectedLayer, -1 keeps source layer)
  - Plane flattening per axis (FlattenZ/FlattenX) with rotationCenter + offsets
  - Provides Run(axis, context) and Clear(parentTransform) methods

- **Behavior preservation**: All serialized fields, hideSourcesWhenIdle and disableSourceColliders handling, idempotency maintained exactly

### Test Coverage

**EditMode tests** validate the extracted utilities:

- **SmoothingTests**: ConstantSpeedSmoothing behavior, speed limits, target reaching
- **DeadZoneTests**: TopDeadZonePolicy threshold and desired Y computations  
- **ProjectorPassTests**: 
  - Idempotent rebuilds (no duplicate children after repeated runs)
  - Material copying on/off behavior
  - Layer assignment including -1 source layer preservation
  - Plane flattening on X vs Z axes with correct coordinate preservation
  - Collider cloning with property preservation

### Defaults Normalization

Script field initializers updated to match scene-tuned values:
- **VerticalCameraFollow**: All defaults already matched RotationPOC scene
- **GeometryProjector**: Updated planeZOffset (-8.5f) and planeXOffset (8.5f) to match scene values

## Phase 2C: Simplified Input Path with Minimal Adapter

**Completed in Phase 2C:**
- Removed all test scaffolding (Assets/Tests/**) for project simplicity
- Introduced minimal input adapter pattern with clean interfaces
- No behavior changes, no scene/prefab changes, no public API changes

### Simplified Input Architecture

**PlayerInputRelay (MonoBehaviour façade) → UnityPlayerInput (IPlayerInput) → PlayerMotor (unchanged public API)**

The input path now uses a minimal adapter pattern:

- **IPlayerInput** interface provides minimal inputs PlayerMotor needs:
  - `Vector2 Move { get; }`
  - `bool JumpHeld { get; }`  
  - `bool JumpPressedThisFrame { get; }`
  - `void ClearTransient()` // resets edge flags once per frame

- **UnityPlayerInput** class implements IPlayerInput with event-driven updates:
  - `SetMove(Vector2)` - updates move vector
  - `OnJumpPerformed()` - sets held and pressed flags
  - `OnJumpCanceled()` - clears held flag
  - Pure C# class (no MonoBehaviour)

- **PlayerInputRelay** maintains identical public API but internally:
  - Creates and owns a UnityPlayerInput instance
  - Forwards input events to both UnityPlayerInput and PlayerMotor (preserving existing behavior)
  - Calls `playerInput.ClearTransient()` once per frame to reset edge flags

### Test Removal

Phase 2C intentionally removed all test files and assemblies to keep the project simple at this time. The extracted services from Phase 2A/2B remain fully functional but are no longer covered by automated tests.

## Phase 2C+ Planning

Future improvements will include:
- Scene splitting and reorganization for better loading performance  
- Additive scene loading system
- Enhanced core utilities in Game.Core
- Additional service extractions as needed

## Verification

To verify Phase 1 + 2B + 2C:
1. Open `Scenes/RotationPOC.unity`
2. Confirm console is clean (no missing scripts)
3. Test player movement, jumping, and perspective switching
4. **Verify camera behavior identical**: dead-zone, upward-only scrolling, smooth/constant speed options
5. **Verify projection behavior identical**: geometry cloning, layer assignment, material copying
6. **Verify input behavior identical**: move, jump, jump-cut, and view switching work exactly as before
7. Verify URP pipeline and input systems still work correctly
8. Check Project Settings show "CygamesHackathon" for both Product Name and UWP Package Name
9. **Test defaults**: Create new scene, add VerticalCameraFollow + GeometryProjector, verify inspector values match RotationPOC
10. **Note**: EditMode tests were removed in Phase 2C for project simplicity