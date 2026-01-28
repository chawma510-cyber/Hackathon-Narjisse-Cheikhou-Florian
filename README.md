# Hackathon VR - Unity 6000.0.36f1

Project for the VR Hackathon.

## Team
- Narjisse
- Cheikhou
- Florian

## VR Setup

### Requirements
- Unity 6000.0.36f1
- A compatible VR headset (Oculus Quest, HTC Vive, Valve Index, Windows Mixed Reality)
- OpenXR runtime configured (SteamVR or Oculus App)

### Project Configuration

The project is configured with:
- **XR Plug-in Management** (4.4.1)
- **OpenXR Plugin** (1.12.0) - Works with all major VR headsets
- **XR Interaction Toolkit** (3.0.4) - For VR interactions

### Getting Started

1. Open Unity Hub
2. Add this project folder
3. Ensure Unity Version: **6000.0.36f1** is selected
4. Open the project

### First Launch

1. **Connect your VR headset** and ensure it's recognized by your system
2. Open `Assets/Scenes/VRScene.unity`
3. Press **Play** to test in VR

### VR Controls

| Controller | Action | Description |
|------------|--------|-------------|
| Left Joystick | Move | Smooth locomotion |
| Left Grip | Sprint | Hold to move faster |
| Right Joystick | Turn | Snap turn left/right |
| Triggers | Interact | Grab objects |

### Troubleshooting

**VR not starting:**
- Make sure OpenXR runtime is set (SteamVR or Oculus)
- Check that your headset is connected and recognized
- Verify XR is enabled: Edit → Project Settings → XR Plug-in Management → PC Settings → OpenXR

**Performance issues:**
- Target 72-90 FPS for comfortable VR
- Reduce Quality settings if needed
- Consider using Single Pass Instanced rendering

### Scripts

| Script | Description |
|--------|-------------|
| `VRInitializer.cs` | Manages XR system startup |
| `HandPresence.cs` | Controller tracking & haptics |
| `VRLocomotion.cs` | Movement & snap turn |

### XR Settings Location
- XR Configuration: `Assets/XR/`
- VR Scene: `Assets/Scenes/VRScene.unity`
- VR Scripts: `Assets/Scripts/VR/`
