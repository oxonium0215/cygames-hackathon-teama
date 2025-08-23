# 3D Stage Preview Implementation

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