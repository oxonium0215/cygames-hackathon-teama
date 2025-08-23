# 3D Stage Preview Implementation

## CRITICAL FIX - Multiple Player Preview Issue

**This document describes the fix for issue #43 where multiple player previews were appearing in a straight line.**

### Problem Analysis

The original issue was multiple player previews being created in a straight line during preview mode, accumulating over time as the player moved between preview activations.

### Root Cause: Race Condition in State Management

The fundamental problem was a **race condition** in the state management of `StagePreviewManager`:

#### Original (Broken) Logic:
1. `StartPreview()` called
2. Guard check passes: `if (isPreviewActive || isTransitioning) return;`
3. Cleanup and setup code executes
4. Coroutine `TransitionToPreview()` is started
5. **`isTransitioning = true` is set INSIDE the coroutine** (potentially next frame)

**Problem**: There was a window between steps 2-5 where `isTransitioning` was still `false`, allowing multiple rapid calls to `StartPreview()` to pass the guard check and create multiple previews.

#### Fixed Logic:
1. `StartPreview()` called
2. Guard check passes: `if (isPreviewActive || isTransitioning) return;`
3. **`isTransitioning = true` is set IMMEDIATELY**
4. Cleanup and setup code executes (now protected)
5. Coroutine `TransitionToPreview()` is started

**Solution**: By setting `isTransitioning = true` immediately in the public method rather than inside the coroutine, we eliminate the race condition window entirely.

### Key Changes Made

#### 1. Immediate State Management
- `isTransitioning = true` is now set immediately in `StartPreview()` and `EndPreview()`
- Removed redundant state setting inside coroutines
- Added safety cleanup of coroutine references

#### 2. Enhanced Cleanup Logic
- Improved `CleanupPlayerPreviews()` with targeted search using `GetComponentsInChildren<Transform>()`
- Added comprehensive logging to track cleanup operations
- Added warning when multiple previews are found (diagnostic for the bug)

#### 3. Safety Guards
- Added validation in `CreatePlayerPreviews()` to prevent creation outside preview mode
- Enhanced duplicate prevention in `CreatePlayerPreviewObject()`
- Added `ForceCleanup()` method for emergency cleanup

#### 4. Debugging and Validation
- Added comprehensive logging throughout the preview creation/cleanup cycle
- Enhanced `PreviewValidation.cs` with detailed diagnostics
- Added `TestRaceCondition()` method to validate the fix
- Multiple counting methods to ensure accurate preview tracking

### Testing the Fix

To verify the fix works:

1. Attach `PreviewValidation` script to a GameObject in the scene
2. Set the `previewManager` and `player` references
3. Use the context menu options:
   - "Validate Preview Behavior" - Basic validation
   - "Test Race Condition" - Rapid StartPreview() calls
4. Watch the console for validation results

#### Expected Results:
- ✅ Only one player preview should ever exist
- ✅ Preview should appear at `(-currentPos.z, currentPos.y, -currentPos.x)`
- ✅ No preview accumulation over multiple activations
- ✅ Clean state transitions with proper cleanup

---

## Overview
The 3D Stage Preview feature allows players to press and hold the V key to get an overview of the stage layout, helping with navigation and strategic planning.

## Components Added

### StagePreviewManager
- **Location**: `Assets/Scripts/Runtime/Game/Preview/StagePreviewManager.cs`
- **Purpose**: Manages the 3D preview functionality including camera transitions, terrain restoration, and preview overlays

### Input Action
- **Action Name**: Preview3D
- **Key Binding**: V key (Keyboard)
- **Type**: Button with performed/canceled events

### Materials
- **GeometryPreviewMaterial**: Semi-transparent cyan material for terrain previews
- **PlayerPreviewMaterial**: Semi-transparent orange material for player position previews

## How It Works

### When V Key is Pressed:
1. **Camera State**: Current camera position, rotation, size, and orthographic mode are saved
2. **Camera Transition**: Smoothly transitions to position (16, player's y+10, -16) with orthographic size 10
3. **Terrain Restoration**: Calls `GeometryProjector.ClearProjected()` to show terrain in original positions
4. **Player Physics**: Stops player movement by setting `PlayerMotor.SetLateralEnabled(false)` and making rigidbody kinematic
5. **Preview Overlays**: Creates semi-transparent previews of:
   - Geometry projected onto XY plane (flatten Z)
   - Geometry projected onto ZY plane (flatten X)  
   - Player position on both projection planes

### When V Key is Released:
1. **Cleanup**: Destroys all preview overlay objects
2. **Camera Restoration**: Smoothly returns camera to original position, rotation, size, and projection mode
3. **Player Physics**: Re-enables player movement and restores rigidbody state

## Setup Instructions

### In Unity Editor:
1. **Add StagePreviewManager**: Add the `StagePreviewManager` component to a GameObject in your scene
2. **Assign References**: In the inspector, assign:
   - Main Camera (auto-detected if not assigned)
   - Player Transform (auto-detected if Player tag exists)
   - Geometry Projector (auto-detected if in scene)
   - Player Motor (auto-detected from player)
   - Player Rigidbody (auto-detected from player)
   - Preview Material: Assign `GeometryPreviewMaterial`
   - Player Preview Material: Assign `PlayerPreviewMaterial`

3. **Wire Input**: In the `PlayerInputRelay` component, assign the `StagePreviewManager` to the "Stage Preview" field

### Input System Setup:
The input action is already configured in `Gameplay.inputactions`:
- Action: "Preview3D" 
- Binding: `<Keyboard>/v`
- Handled in `PlayerInputRelay.OnPreview3D()`

## Features

### Camera Behavior
- Smooth transitions using configurable AnimationCurve
- Orthographic view for better stage overview
- Position calculated relative to player position (player's y + 10 offset)

### Terrain Visualization
- Shows original terrain positions (before any projections)
- Semi-transparent overlays show how terrain would appear on each projection plane
- Uses existing `GeometryProjector` infrastructure

### Player State Management
- Physics stopped but position maintained
- Visual previews show where player would be on each projection plane
- No interference with existing projection switching system

## Configuration Options

### StagePreviewManager Inspector:
- **Preview Camera Offset**: (16, 10, -16) - Offset from player position
- **Preview Camera Size**: 10 - Orthographic camera size
- **Transition Duration**: 0.5s - Time for camera transitions
- **Transition Curve**: Ease curve for smooth camera movement

## Technical Notes

### Architecture
- Clean separation in `Game.Preview` namespace
- No modifications to existing core systems
- Uses existing `GeometryProjector` and `PlayerMotor` APIs
- Proper assembly references and dependencies

### Performance
- Preview overlays created dynamically only when needed
- Proper cleanup on component destruction
- Uses object pooling patterns where appropriate

### Robustness
- Component validation and auto-discovery
- Graceful degradation if materials not assigned
- Debug logging for development builds
- Proper coroutine cleanup