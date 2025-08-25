# Preview Feature Improvements - Usage Guide

## Overview
The preview feature has been enhanced to improve distance perception and three-dimensionality through color-coded plane previews and grid overlays.

## New Materials

### Geometry Preview Materials
- **GeometryFlattenZ.mat** - Blue-tinted material for Z-plane geometry previews
- **GeometryFlattenX.mat** - Magenta-tinted material for X-plane geometry previews

### Grid Overlay Materials  
- **GridFlattenZ.mat** - Semi-transparent blue grid for Z-plane reference
- **GridFlattenX.mat** - Semi-transparent magenta grid for X-plane reference

## StagePreviewManager Configuration

### New Inspector Fields
In the Unity Inspector, the StagePreviewManager now has these additional material slots:

```
[Header("Preview Overlays")]
- Preview Material (legacy - still supported)
- Preview Material Flatten Z (new - blue plane geometry)
- Preview Material Flatten X (new - magenta plane geometry) 
- Grid Material Flatten Z (new - blue grid overlay)
- Grid Material Flatten X (new - magenta grid overlay)
- Player Preview Material (unchanged)
```

### Setup Instructions
1. Assign the new materials to their respective slots in the Inspector
2. If new materials are not assigned, the system falls back to the legacy `Preview Material`
3. Grid materials are optional - grids only appear if materials are assigned

## Visual Improvements

### Color Coding
- **Blue elements** = FlattenZ plane (objects projected onto Z=constant plane)
- **Magenta elements** = FlattenX plane (objects projected onto X=constant plane)  
- **Orange elements** = Player preview (unchanged from original)

### Grid Overlays
- Semi-transparent grid planes help visualize the projection planes
- Grid size automatically scales with camera preview size
- Grids are positioned at the appropriate plane coordinates

## Technical Details

### Backward Compatibility
- Existing scenes with only `previewMaterial` assigned will continue to work
- New functionality is additive and doesn't break existing setups
- Materials fall back to legacy material if plane-specific ones aren't assigned

### Performance
- Grid generation uses simple procedural quad meshes
- Materials use standard Unity URP/Built-in shaders
- Cleanup properly handles all preview objects including grids

## Testing
Use the `PreviewImprovementsTest` component to validate the setup:
- Add the component to any GameObject in the scene
- Assign the StagePreviewManager reference
- Use "Run Preview Tests" from the context menu or set `runTestsOnStart = true`

The test will verify material assignments, color distinctions, and grid properties.