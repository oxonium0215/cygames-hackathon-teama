# Terrain Movement System Implementation

## Overview
This implementation changes the projection behavior during viewpoint changes from duplicating terrain to moving the terrain in-place. This solves the problem where elements like checkpoints and warp mechanics would be difficult to associate when terrain is duplicated.

## Key Changes

### 1. New GeometryTransformer Class
- **File**: `Assets/Scripts/Game.Level/GeometryTransformer.cs`
- **Purpose**: Handles in-place transformation of geometry instead of cloning
- **Key Features**:
  - Stores original positions/states to allow restoration
  - Transforms geometry to projection planes (FlattenZ or FlattenX)
  - Maintains object identity (same GameObject references)
  - Manages renderer/collider visibility and enabled states

### 2. Updated GeometryProjector Class
- **File**: `Assets/Scripts/Game.Level/GeometryProjector.cs`
- **Changes**:
  - Uses `GeometryTransformer` instead of `ProjectorPass`
  - `ProjectedRoot` now returns `SourceRoot` (backward compatibility)
  - Updated visibility logic to keep sources active in new system
  - Marked deprecated fields with appropriate tooltips
  - Maintains same public interface for existing code

### 3. Debug/Demo Component
- **File**: `Assets/Scripts/Game.Debugging/TerrainMovementDemo.cs`
- **Purpose**: Demonstrates the new system and validates object preservation
- **Features**:
  - Runtime testing of projection switching
  - Object count validation (should never change)
  - On-screen GUI showing system status
  - Keyboard controls for manual testing

## Benefits of New System

### Object Identity Preservation
- Checkpoints, warps, and interactive elements remain the same GameObject instances
- Component references are preserved across viewpoint changes
- No need to re-establish associations after perspective switches

### Performance Improvements
- No object creation/destruction during perspective switches
- Reduced garbage collection pressure
- Faster perspective switching (just position updates)

### Simplified Architecture
- Single source of truth for terrain geometry
- No synchronization needed between source and projected versions
- Easier debugging and scene setup

## Backward Compatibility

The implementation maintains full backward compatibility:
- All public methods of `GeometryProjector` work the same way
- Existing scenes and prefabs continue to work without modification
- Configuration options are preserved (though some are now deprecated)
- `PerspectiveProjectionManager` requires no changes

## How It Works

### Old System (Duplication-based):
1. Source geometry exists in `sourceRoot`
2. During projection, clones are created in `projectedRoot`
3. Clones are flattened to the appropriate plane
4. Sources are hidden, clones are shown
5. Player interacts with clones (loses object identity)

### New System (Movement-based):
1. Source geometry exists in `sourceRoot`
2. During projection, sources are moved to the appropriate plane
3. Original positions are stored for restoration
4. Sources remain visible and interactive
5. Player interacts with same objects (preserves object identity)

## Testing the Implementation

### Manual Testing:
1. Add `TerrainMovementDemo` component to a GameObject with `GeometryProjector`
2. Press `1` for FlattenZ projection (side view)
3. Press `2` for FlattenX projection (top-down view)  
4. Press `3` to restore original positions
5. Verify object count remains constant (shown in on-screen GUI)

### Code Testing:
- Object references should remain the same before and after transforms
- Position changes should only affect the flattened axis (Z for FlattenZ, X for FlattenX)
- Calling `Restore()` should return all objects to original positions
- Multiple transforms should work correctly

## Migration Guide

For existing projects:
1. **No immediate action required** - system works with existing setups
2. **Optional**: Remove unused `projectedRoot` references in scenes
3. **Optional**: Disable `hideSourcesWhenIdle` and `disableSourceColliders` for cleaner setup
4. **Recommended**: Add checkpoint/warp components directly to source terrain objects

## Technical Details

### Core Classes:
- `GeometryTransformer`: Handles the actual position transformation and state management
- `TransformationContext`: Data structure for transformation parameters
- `ProjectionAxis`: Enum defining FlattenZ (side view) or FlattenX (top-down view)

### State Management:
- Original positions stored in `Dictionary<Transform, Vector3>`
- Original renderer/collider states preserved
- Transformation state tracked to prevent double-transforms
- Automatic restoration on component destruction

This implementation successfully addresses the original issue while maintaining full backward compatibility and improving performance.