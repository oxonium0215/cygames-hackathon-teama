# Camera Specification

This document describes the camera configuration and behavior as implemented in `PerspectiveProjectionManager`.

## Camera Configuration

### Distance and Positioning
- **cameraDistance**: `15f` - Distance from the camera pivot to the actual camera position
- **pivotOffset**: `Vector3.zero` - Offset applied to the camera pivot position from the rotation center

### View Perspectives

The system supports two fixed perspective views:

#### View A (Index 0)
- **viewAYaw**: `0f` - Camera yaw angle for view A
- **viewAProjection**: `ProjectionAxis.FlattenZ` - Geometry projection mode (XY plane)

#### View B (Index 1) 
- **viewBYaw**: `-90f` - Camera yaw angle for view B  
- **viewBProjection**: `ProjectionAxis.FlattenX` - Geometry projection mode (ZY plane)

### Rotation Animation

Perspective switching uses the following animation settings:

- **rotateDuration**: `0.5f` - Time in seconds for perspective transition
- **rotateEase**: `AnimationCurve.EaseInOut(0, 0, 1, 1)` - Easing curve for smooth rotation

## Behavioral Configuration

### Player Behavior During Rotation

- **rotatePlayerDuringSwitch**: `true` - Whether to move the player during camera rotation
- **makePlayerKinematicDuringSwitch**: `true` - Whether to make player rigidbody kinematic during rotation
- **jumpOnlyDuringSwitch**: `true` - When true, lateral movement input is suppressed during rotation (jump remains enabled)
- **fixYDuringRotation**: `true` - Keep player Y position fixed during rotation to prevent falling
- **resolveVerticalOverlapDuringRotation**: `true` - Attempt to resolve vertical overlaps during rotation

### Ground and Collision Settings

- **groundMask**: Layer mask for ground detection during rotation
- **snapDownDistance**: `5f` - Maximum distance to snap player down to ground after rotation
- **snapUpAllowance**: `0.5f` - Upward allowance when snapping to ground
- **groundSkin**: `0.05f` - Skin distance for ground collision detection

### Advanced Depenetration Settings

These settings control the vertical-only depenetration system:

- **penetrationResolveIterations**: `3` - Maximum iterations for overlap resolution
- **penetrationSkin**: `0.0015f` - Skin distance for overlap detection
- **overlapBoxInflation**: `0.98f` - Inflation factor for overlap detection box
- **maxResolveStep**: `2.0f` - Maximum distance per resolution step
- **maxResolveTotal**: `8.0f` - Maximum total resolution distance

## Vertical Follow (Top Dead Zone)

The `VerticalCameraFollow` component provides Y-axis camera following with configurable dead zone behavior:

### Dead Zone Configuration
- **topDeadZone**: `3.0f` - Distance above the camera pivot Y before following starts (world units)

### Motion Settings
- **upSpeed**: `10f` - Move speed upwards (units/second) when following with constant speed
- **useSmoothDamp**: `true` - Use SmoothDamp instead of constant speed for smoother motion
- **smoothTime**: `0.15f` - Smooth time for SmoothDamp (only when useSmoothDamp is enabled)
- **smoothMaxSpeed**: `30f` - Hard cap on upward movement speed (SmoothDamp only)

### Behavior
- **neverScrollDown**: `true` - Prevents camera from moving down once it has moved up

### How It Works
```
Player Y Position
     ↑
     |  [Following Active]
     |
─────┼───── Top Dead Zone Threshold (pivot Y + topDeadZone)
     |  [Dead Zone - No Following]
     |
Camera Pivot Y
```

When the player moves above the top dead zone threshold, the camera follows to keep the player at the edge of the dead zone. The camera never moves down when `neverScrollDown` is enabled, creating a ratcheting upward movement that preserves the highest reached position.

## Camera Adapter System

The camera uses a service-based architecture with `ICameraProjectionAdapter`:

- **RepositionPivotToCenter()**: Positions camera pivot at rotation center plus offset
- **UpdateRotation()**: Interpolates camera yaw during perspective switches  
- **SetCameraDistance()**: Positions child camera at specified distance from pivot

## Implementation Notes

- Camera pivot yaw is interpolated smoothly during perspective switches using the configured easing curve
- The pivot is repositioned to the rotation center with applied offset before each switch
- Child camera is positioned at the configured distance along the local forward axis
- All values shown reflect the current scene configuration