# Tutorial System Implementation

This document describes how to set up and use the tutorial system in the Tutorial1 scene.

## Components Created

### 1. TutorialTrigger
- **Purpose**: Detects when player overlaps with tutorial flag block and starts tutorial when jump is pressed
- **Usage**: Attach to any GameObject with a Collider (set as Trigger) that should act as a tutorial flag
- **Configuration**: 
  - `tutorialManager`: Reference to the TutorialManager component
  - `playerTag`: Tag to identify player GameObject (default: "Player")

### 2. TutorialManager  
- **Purpose**: Manages tutorial steps and progression using InputAction system
- **Usage**: Attach to a GameObject in the scene to handle tutorial flow
- **Configuration**:
  - `tutorialConfig`: Reference to TutorialConfig ScriptableObject with tutorial steps
  - `jumpInputAction`: Reference to Jump InputAction from input system
  - `tutorialText`: UI Text component to display instructions
  - `tutorialCanvas`: CanvasGroup for showing/hiding tutorial UI
  - `audioSource`: AudioSource for playing tutorial narration

### 3. TutorialConfig (ScriptableObject)
- **Purpose**: Defines tutorial content and settings
- **Usage**: Create via Assets menu: Create > Game > Tutorial Configuration
- **Configuration**:
  - `tutorialTitle`: Name of the tutorial
  - `steps`: Array of tutorial steps (text + optional audio)
  - `loopTutorial`: Whether to restart from beginning after completion
  - `continuePrompt`: Text shown for input prompt (e.g. "Press [Jump] to continue...")

## Setup Instructions for Tutorial1 Scene

1. **Create Tutorial Flag Block**:
   - Create a GameObject (e.g., Cube) positioned where players should trigger tutorial
   - Add a Collider component and set `isTrigger = true`
   - Tag the GameObject appropriately or configure TutorialTrigger playerTag
   - Add `TutorialTrigger` component
   
2. **Create Tutorial Manager**:
   - Create an empty GameObject (e.g., "TutorialManager")
   - Add `TutorialManager` component
   - Create tutorial UI (Canvas with Text component for instructions)
   - Configure all references in TutorialManager

3. **Create Tutorial Configuration**:
   - Right-click in Project > Create > Game > Tutorial Configuration
   - Add tutorial steps with instruction text
   - Optionally add audio clips for each step
   - Assign to TutorialManager's `tutorialConfig` field

4. **Wire Input System**:
   - In TutorialManager, assign `jumpInputAction` to reference the Jump action from Gameplay.inputactions
   - This ensures all keys/buttons mapped to Jump work for tutorial progression

## Key Features

- ✅ **Repeatable Tutorial**: Player can re-trigger by pressing jump while on flag block
- ✅ **Dynamic Input**: Uses InputAction system, supports any key/button mapped to Jump action
- ✅ **No Hardcoded Keys**: Replaced hardcoded space key with InputAction reference
- ✅ **Flexible Configuration**: Easy to set up different tutorials via ScriptableObjects
- ✅ **Audio Support**: Optional audio narration for each step

## Input Action Integration

The system properly uses the existing InputAction setup:
- Jump action includes: Space key, Gamepad East/South buttons
- Tutorial respects all mapped inputs automatically
- No hardcoded key references in tutorial code
- Future key remapping is supported without code changes