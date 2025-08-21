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

## Phase 2 Planning

Future improvements will include:
- Scene splitting and reorganization for better loading performance
- Deeper component responsibility separation (e.g., extracting DepenetrationSolver, GroundProbe)
- Additive scene loading system
- Enhanced core utilities in Game.Core
- Component adapters for cleaner interfaces

## Verification

To verify the migration:
1. Open `Scenes/RotationPOC.unity`
2. Confirm console is clean (no missing scripts)
3. Test player movement, jumping, and perspective switching
4. Verify URP pipeline and input systems still work correctly
5. Check Project Settings show "CygamesHackathon" for both Product Name and UWP Package Name