# Game Architecture - Role Divisions

This document outlines the clarified and streamlined role divisions in the Unity project architecture.

## Assembly Structure

### 🎮 **Game.Player**
**Responsibility**: Pure player movement and physics
- `PlayerMotor.cs` - Handles player movement, jumping, ground detection
- `MovePlane` enum - Defines movement planes (X/Z axis)
- **Dependencies**: None (core assembly)

### 📷 **Game.Camera** 
**Responsibility**: Camera positioning and tracking
- `VerticalCameraTracker.cs` - Manages camera following behavior
- **Dependencies**: None (core assembly)

### 🔧 **Game.Input**
**Responsibility**: Input routing and delegation
- `PlayerInputRouter.cs` - Routes Unity Input System events to appropriate systems
- **Dependencies**: `Game.Player`, `Game.Perspective`, `Unity.InputSystem`

### 🏗️ **Game.Projection**
**Responsibility**: Geometry projection building
- `ProjectionBuilder.cs` - Creates flattened geometry clones
- `ProjectionAxis` enum - Defines projection axes (FlattenX/FlattenZ)
- `PerspectiveProjectionManager.cs` - **DEPRECATED** Legacy facade component
- **Dependencies**: None (core assembly)

### 🔄 **Game.Perspective** 
**Responsibility**: View switching coordination
- `ViewSwitchCoordinator.cs` - Orchestrates perspective changes between views
- `PerspectiveCameraController.cs` - Handles camera rotation during view switches
- **Dependencies**: `Game.Player`, `Game.Camera`, `Game.Projection`, `Game.Physics`

### ⚡ **Game.Physics**
**Responsibility**: Collision resolution utilities
- `CollisionResolver.cs` - Provides reusable collision resolution methods
- `CollisionResolutionSettings` - Configuration for collision resolution
- **Dependencies**: None (utility assembly)

### 🐛 **Game.Debugging**
**Responsibility**: Debug utilities
- `EchoInput.cs` - Debug input logging
- **Dependencies**: None (core assembly)

### 🏁 **Game.Level**
**Responsibility**: Level management (future expansion)
- Currently empty, ready for future level systems
- **Dependencies**: None (core assembly)

### 🌐 **Game.Core**
**Responsibility**: Cross-system verification and testing
- `RefactoringVerification.cs` - Validates architecture integrity
- **Dependencies**: All other Game assemblies

## Key Improvements

### ✅ **Cleaner Separation of Concerns**
- **Camera logic** consolidated in `Game.Camera` and `Game.Perspective`
- **Player logic** isolated in `Game.Player`
- **Input routing** simplified in `Game.Input`
- **Collision resolution** extracted to reusable `Game.Physics` utilities

### ✅ **Reduced Coupling**
- Input system no longer directly depends on projection management
- Camera control separated from projection building
- Collision resolution utilities can be reused across systems

### ✅ **Modular Architecture**
- Each assembly has a single, clear responsibility
- Dependencies flow in one direction (no circular references)
- Components can be easily tested and maintained independently

### ✅ **Legacy Support**
- `PerspectiveProjectionManager` remains as deprecated facade
- Existing scenes continue to work without modification
- Gradual migration path to new architecture

## Migration Guide

### For New Projects
Use `ViewSwitchCoordinator` + `PerspectiveCameraController` instead of `PerspectiveProjectionManager`.

### For Existing Projects
1. Keep existing `PerspectiveProjectionManager` for compatibility
2. Optionally add `ViewSwitchCoordinator` for new features
3. Gradually migrate to new components as needed

## Dependencies Flow

```
Game.Core
    ├── Game.Player (independent)
    ├── Game.Camera (independent) 
    ├── Game.Physics (independent)
    ├── Game.Projection (independent)
    ├── Game.Debugging (independent)
    ├── Game.Level (independent)
    ├── Game.Perspective
    │   ├── Game.Player
    │   ├── Game.Camera
    │   ├── Game.Projection
    │   └── Game.Physics
    └── Game.Input
        ├── Game.Player
        ├── Game.Perspective
        └── Unity.InputSystem
```

This architecture ensures clear boundaries, reduced coupling, and improved maintainability while preserving all existing functionality.