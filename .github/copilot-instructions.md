# Unity 3D Perspective-Switching Platformer Game
Unity 6000.2.0f1 (Unity 2024.3) 3D platformer game featuring dynamic perspective switching between 2D side-view and top-down gameplay modes. The game includes advanced player physics, input management, and procedural geometry projection systems.

**Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.**

## Working Effectively

### Unity Installation and Setup
- **CRITICAL**: Unity 6000.2.0f1 is required for this project
- Install Unity Hub from: https://unity3d.com/get-unity/download
- Use Unity Hub to install Unity 6000.2.0f1 with Linux Build Support (if on Linux)
- Alternative: Direct download Unity Editor from Unity Archive for version 6000.2.0f1
- **Windows users**: Install Visual Studio 2022 with Game Development with Unity workload
- **Linux users**: Install .NET 8.0 SDK (`sudo apt install dotnet-sdk-8.0`)

### Project Opening and Building
- **NEVER CANCEL**: Project opening takes 15-30 minutes on first open due to package resolution and library compilation. NEVER CANCEL this process. Set timeout to 45+ minutes.
- Open the project by selecting the root directory in Unity Hub
- Unity will automatically resolve packages from `Packages/manifest.json`
- **Build Platform**: Standalone (Windows/Mac/Linux) - configured in Build Settings
- Build command: File > Build Settings > Build (or Build and Run)
- **NEVER CANCEL**: Build process takes 10-15 minutes for first build, 2-5 minutes for incremental builds. Set timeout to 30+ minutes for safety.

### Running and Testing
- **Play Mode Testing**: Always use Play Mode in Unity Editor for testing changes
- **CRITICAL VALIDATION**: After making changes, always test both perspective modes:
  1. Start Play Mode (Ctrl+P or Cmd+P)
  2. Test basic movement with WASD/Arrow keys or gamepad left stick
  3. Test jumping with Space or gamepad East button (A on Xbox, X on PlayStation)
  4. **ESSENTIAL**: Test perspective switching with Tab or gamepad Y button
  5. Verify smooth transitions and input responsiveness
  6. Test in both 2D side-view (default) and top-down view modes
- **Performance Check**: Monitor Console for errors and frame rate in Game view
- **Input Debugging**: Enable the InputSuppressionTest component to debug input issues

### Assembly Compilation
- Project uses Assembly Definition files (.asmdef) for modular compilation
- **NEVER CANCEL**: Initial compilation takes 5-10 minutes. NEVER CANCEL. Set timeout to 20+ minutes.
- Incremental compilation is typically 30 seconds to 2 minutes
- Assembly references are managed automatically by Unity
- Core assemblies: Game.Core, Game.Player, Game.Input, Game.Level, Game.Projection, Game.Camera, Game.Debugging

### Package Management
- Unity Package Manager handles all dependencies
- Key packages: Input System (1.14.1), Universal Render Pipeline (17.2.0), Test Framework (1.5.1)
- **DO NOT** manually edit `Packages/manifest.json` unless specifically required
- Use Window > Package Manager in Unity to add/update packages

## Validation and Testing

### Manual Testing Scenarios
After making any changes, ALWAYS execute these validation steps:

#### Basic Gameplay Loop
1. Enter Play Mode and verify player spawns correctly
2. Test movement in all directions (WASD or left stick)
3. Test jump mechanics including:
   - Single jump
   - Jump buffering (press jump just before landing)
   - Coyote time (jump briefly after leaving platform)
   - Variable jump height (tap vs hold jump)
4. **Critical**: Test perspective switching (Q/E keys or left/right shoulder buttons):
   - Should smoothly transition between side-view and top-down
   - Input should remain responsive after transition
   - Camera should reposition correctly
   - Geometry should project properly

#### Input System Validation
1. Verify input responsiveness in both perspective modes
2. Test with both keyboard/mouse and gamepad if available
3. Check Console for InputSuppressionTest debug messages during perspective transitions
4. Ensure no input lag or dropped inputs after perspective changes

#### Physics and Collision Testing
1. Test ground detection on various surfaces
2. Verify collision with walls and obstacles in both views
3. Test jumping on sloped surfaces
4. Verify player doesn't fall through ground after perspective switches

### Code Quality Validation
- **ALWAYS** check Unity Console for compilation errors before committing
- No red errors should be present in Console
- Yellow warnings are acceptable but should be minimized
- Use Unity's built-in code analysis (Window > Analysis > Code Coverage if needed)

### Performance Validation
- Monitor frame rate in Game view statistics
- Target: 60 FPS on modern hardware
- Watch for GC allocation spikes during perspective transitions
- Profile memory usage if making significant changes

## Common Tasks and Commands

### Unity Editor Operations
- **Open Project**: Use Unity Hub or `Unity.exe -projectPath [path]` (command line not recommended for development)
- **Play Mode**: Ctrl+P (Windows) / Cmd+P (Mac) - ESSENTIAL for testing
- **Build**: Ctrl+Shift+B (Windows) / Cmd+Shift+B (Mac)
- **Console**: Ctrl+Shift+C (Windows) / Cmd+Shift+C (Mac)
- **Inspector**: Ctrl+3 (Windows) / Cmd+3 (Mac)

### Git Operations
- **CRITICAL**: Unity generates many meta files - commit .meta files alongside assets
- **DO NOT** commit Library/, Temp/, Logs/, or UserSettings/ folders (already in .gitignore)
- **Safe to commit**: Assets/, ProjectSettings/, Packages/manifest.json
- Use `git status` to verify only intended files are staged

### Build Process Timing Expectations
- **Project Opening**: 15-30 minutes first time, 2-5 minutes subsequent opens
- **Assembly Compilation**: 5-10 minutes first time, 30 seconds - 2 minutes incremental
- **Full Build**: 10-15 minutes first time, 2-5 minutes incremental
- **Test Play**: Immediate in editor, 30-60 seconds for builds

## Project Architecture

### Core Systems Overview
- **Game.Player**: Player movement, physics, ground detection (`PlayerMotor`, `GroundProbe`, `PlaneMotion`)
- **Game.Input**: Input handling and relay system (`PlayerInputRelay`, `UnityPlayerInput`)
- **Game.Projection**: Perspective switching and geometry projection (`PerspectiveProjectionManager`, `ProjectorPass`)
- **Game.Level**: Geometry management and projection (`GeometryProjector`, `ProjectorPass`)
- **Game.Camera**: Camera follow and smoothing systems (`VerticalCameraFollow`)
- **Game.Debugging**: Debug tools and input validation (`InputSuppressionTest`, `EchoInput`)

### Key Classes and Their Roles
- **PlayerMotor**: Main player controller handling movement, jumping, and physics
- **PerspectiveProjectionManager**: Manages perspective switching and camera transitions
- **GeometryProjector**: Handles 3D to 2D geometry projection for different view modes
- **PlayerInputRelay**: Routes input from Unity Input System to game components
- **GroundProbe**: Pure C# ground detection with coyote time and jump buffering

### Input System Configuration
- Input Actions asset: `Assets/Input/Gameplay.inputactions`
- Supports keyboard (WASD, Space, Q/E) and gamepad (left stick, East button, left/right shoulders)
- Input suppression during perspective transitions controlled by `JumpOnlyDuringSwitch` flag

### Scene Structure
- Main scene: `Assets/Scenes/RotationPOC.unity`
- Contains player, geometry, camera system, and projection managers
- Uses URP (Universal Render Pipeline) for rendering

## Troubleshooting

### Common Issues
1. **"Unity not found"**: Install Unity Hub and Unity 6000.2.0f1
2. **Long build times**: Normal - wait for completion, increase timeouts
3. **Input not working**: Check PerspectiveProjectionManager.JumpOnlyDuringSwitch setting
4. **Perspective switching broken**: Verify GeometryProjector configuration and rotation center setup
5. **Compilation errors**: Check all .asmdef files are properly configured

### Debug Tools
- Enable InputSuppressionTest component to monitor input suppression during transitions
- Use EchoInput component to debug input values
- Unity Console shows all debug logs and errors
- Game view statistics show performance metrics

### Performance Optimization
- Perspective switching involves geometry cloning - expect brief frame drops during transition
- Use Unity Profiler for detailed performance analysis if needed
- Monitor Console for excessive GC allocations

## File Organization Quick Reference

### Key Directories
```
Assets/
├── Input/                     # Input System configuration
├── Materials/                 # Materials and textures
├── Prefabs/                   # Game object prefabs (if any)
├── Scenes/                    # Unity scenes (RotationPOC.unity)
├── Scripts/                   # All C# source code
│   ├── Game.Camera/          # Camera follow systems
│   ├── Game.Core/            # Shared utilities (placeholder)
│   ├── Game.Debugging/       # Debug and test tools
│   ├── Game.Input/           # Input handling
│   ├── Game.Level/           # Level geometry and projection
│   ├── Game.Player/          # Player controller and physics
│   └── Game.Projection/      # Perspective switching system
├── Settings/                  # URP and rendering settings
ProjectSettings/               # Unity project configuration
Packages/                     # Unity package dependencies
```

### Frequently Modified Files
- `Assets/Scripts/Game.Player/PlayerMotor.cs` - Player movement logic
- `Assets/Scripts/Game.Input/PlayerInputRelay.cs` - Input routing
- `Assets/Scripts/Game.Projection/PerspectiveProjectionManager.cs` - View switching
- `Assets/Input/Gameplay.inputactions` - Input bindings
- `Assets/Scenes/RotationPOC.unity` - Main game scene

**CRITICAL REMINDERS**:
- NEVER CANCEL builds or long-running operations
- ALWAYS test perspective switching after changes
- ALWAYS check Unity Console for errors
- ALWAYS run through complete user scenarios before considering changes complete
- Remember to set appropriate timeouts (30+ minutes for builds, 20+ minutes for compilation)