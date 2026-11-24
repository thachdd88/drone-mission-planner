# Drone Mission Planner

A Unity project where you fly a quadcopter drone around a small test arena. The goal is to grow it into a mission planner game with waypoints, deliveries and patrols.

## Requirements

- Unity **6000.6.10f1** (Unity 6)
- Universal Render Pipeline 17.6.0 and Input System 1.20.1. Unity installs both from `Packages/manifest.json`.

## Getting started

1. Clone the repository:
   ```
   git clone git@github.com:thachdd88/drone-mission-planner.git
   ```
2. Open the folder in Unity Hub with Unity 6000.6.10f1. The first import takes a few minutes while Unity rebuilds `Library/`.
3. Open **`Assets/Scenes/DroneScene.unity`**. Unity may open `SampleScene` by default, but `DroneScene` is the main scene.
4. Press **Play**.

## Controls

| Key | Action |
| --- | --- |
| W / ↑ | Fly forward |
| S / ↓ | Fly backward |
| A / ← | Turn left |
| D / → | Turn right |
| Q / E | Strafe left / right |
| Space | Ascend |
| Left Shift / C | Descend |

## Project layout

```
Assets/
  Scenes/DroneScene.unity       Main scene: floor, light, ring of 12 pillars, player drone with chase camera
  Scripts/DroneController.cs    Arcade flight controller (gravity-free Rigidbody, new Input System)
  Editor/DroneSceneSetup.cs     Editor menu that rebuilds the scene from primitives
  Materials/Drone/              URP Lit materials for the drone, floor and obstacles
  Settings/                     URP render pipeline assets
```

## Rebuilding the scene

`DroneScene` is generated from Unity primitives. To rebuild it from scratch, use **Tools > Drone > Build Drone Scene** in the editor menu.

## Tuning the drone

Select the **PlayerDrone** object and change these fields on the `DroneController` component:

- **Movement:** `speed`, `climbSpeed`, `rotationSpeed`, `acceleration`
- **Visuals:** `maxTilt`, `rotorSpinSpeed`, `bobAmount`, `bobSpeed`

## Roadmap

- Waypoints placed in the scene, with the drone flying a planned route
- Missions: deliveries, patrols, timed objectives
- Mission planning UI
