# Projection Debugging Workflow

This document describes how to debug projection switching issues using the visual gizmos and validation features in `PerspectiveProjectionManager`.

## Debug Gizmos Overview

The `PerspectiveProjectionManager` component provides editor-only debug visualizations when selected in the Scene view. These gizmos help visualize:

1. **Projection State** - Current switching state and view information
2. **View Axes & Planes** - Active projection planes and coordinate axes
3. **Overlap Detection** - Collision detection volumes for depenetration
4. **Ground Snapping** - Raycast visualization for ground placement

## Using the Debug Gizmos

### 1. Enabling Gizmos
- Select the GameObject with the `PerspectiveProjectionManager` component in the Scene view
- Ensure Gizmos are enabled in the Scene view toolbar
- The visualization will show different information in Edit mode vs Play mode

### 2. Projection State Visualization

**What it shows:**
- Color-coded state indicator at the rotation center:
  - **Green**: Idle state during play mode
  - **Red**: Currently switching perspectives 
  - **Yellow**: Editor mode or unknown state
- Current view index (A or B) and projection axis
- Camera yaw angle for the current view

**How to use:**
- Verify the correct view is active
- Monitor switching state during runtime testing
- Check camera orientation matches expected yaw angles

### 3. View Axes & Projection Planes

**What it shows:**
- **Cyan grid**: XY projection plane (when Z-axis is flattened)
- **Magenta grid**: ZY projection plane (when X-axis is flattened)  
- **Colored axis rays**: Red (X), Green (Y), Blue (Z)
- Plane position based on `rotationCenter` and offsets

**How to use:**
- Verify projection planes are positioned correctly
- Check that axes match expected projection directions
- Ensure plane positions align with level geometry

### 4. Overlap Detection Volumes

**What it shows:**
- **Orange wireframe box**: Player collision bounds scaled by `overlapBoxInflation`
- **Semi-transparent fill**: Actual detection volume
- **Parameter labels**: Current resolve step and total limits

**How to use:**
- Verify detection volume encompasses player properly
- Check that `overlapBoxInflation` setting is appropriate
- Monitor during play to see if overlaps are detected
- Adjust `maxResolveStep` and `maxResolveTotal` if resolution fails

### 5. Ground Snap Visualization

**What it shows:**
- **Yellow ray**: Ground detection raycast from player
- **Yellow box**: Snap up allowance zone
- **Green sphere**: Ground hit point (when found)
- **Red sphere**: No ground detected within range
- **Cyan dot**: Ground skin offset position

**How to use:**
- Verify raycast reaches expected ground surfaces
- Check `snapUpAllowance` and `snapDownDistance` are appropriate
- Ensure `groundMask` includes correct layers
- Monitor `groundSkin` offset for proper player placement

## Parameter Validation

The `OnValidate` method automatically checks parameter relationships:

### Automatic Corrections
- `maxResolveStep` clamped to `maxResolveTotal`
- `penetrationSkin` adjusted relative to `maxResolveStep`
- `groundSkin` limited by `snapUpAllowance`
- `overlapBoxInflation` kept within reasonable bounds (0.5-1.5)

### Warnings
- Small angle differences between View A and View B
- Both views using same projection axis
- High iteration counts that may impact performance

## Debugging Common Issues

### Player Getting Stuck During Switching
1. Check **Overlap Detection** gizmos for proper volume sizing
2. Increase `maxResolveTotal` if player needs more lift distance
3. Verify `penetrationSkin` isn't too large
4. Check that `overlapBoxInflation` allows detection of problematic geometry

### Incorrect Ground Placement
1. Examine **Ground Snap** visualization ray
2. Verify `groundMask` includes all ground layers
3. Adjust `snapDownDistance` if player spawns too high
4. Check `groundSkin` for proper surface offset

### Camera Not Rotating to Expected Position
1. Check **Projection State** labels for correct yaw angles
2. Verify `rotationCenter` is positioned correctly
3. Ensure `cameraPivot` is properly assigned
4. Compare `viewAYaw` and `viewBYaw` values

### Projection Plane Misalignment
1. Check **View Axes & Planes** visualization
2. Verify `rotationCenter` position matches level geometry
3. Adjust `planeXOffset` and `planeZOffset` in GeometryProjector
4. Ensure projection axes match intended flattening direction

## Performance Considerations

- Debug gizmos only render when component is selected
- Gizmos have minimal performance impact in built players (compiled out)
- Higher `penetrationResolveIterations` may cause frame drops during switching
- Large `snapDownDistance` increases raycast cost

## Integration with Other Systems

The debug gizmos work alongside:
- `PlayerMotor.Gizmos.cs` - Shows ground check radius
- `VerticalCameraFollow.Gizmos.cs` - Shows camera dead zones
- Unity's built-in collision visualization
- Physics Debug Display options

## Best Practices

1. **Start with default parameters** and adjust incrementally
2. **Test switching in both directions** (A→B and B→A)
3. **Verify behavior with different player positions** relative to projection planes
4. **Check edge cases** like corners and slopes in level geometry
5. **Monitor warnings** from OnValidate and address configuration issues
6. **Use Play mode gizmos** for real-time debugging during gameplay