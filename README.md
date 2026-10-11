# Brainrot Planet.io — Unity 6 playable game scene

**Version**: 0.1.0 / first one-level gameplay implementation
**Unity Editor**: 6000.2.7f2 (a different Unity 6000.2 patch should normally open the project)
**Platform**: Android (ARM64 / IL2CPP), Editor testing on Windows.

## Play in Unity

1. Open the folder **BrainrotPlanetUnity** using Unity Hub (Unity 6).
2. Let the editor import packages and scripts. The editor script creates `Assets/Scenes/PlanetArena.unity` if it is missing. If the scene is not open, choose **Brainrot Planet → 1. Create and open game scene**.
3. Press **Play**, select a character and press **ДЕСАНТИРОВАТЬСЯ**.

## Android APK

Use **Brainrot Planet → 2. Build Android APK**. Unity Android Build Support, SDK, NDK and OpenJDK must be installed in Unity Hub. The result is `Builds/Android/BrainrotPlanet-v0.1.apk`.

Alternatively, close the Editor and run:

```
"C:\Program Files\Unity\Hub\Editor\6000.2.7f2\Editor\Unity.exe" -batchmode -nographics -quit -projectPath "C:\Rabota\projects\BrainrotPlanetUnity" -executeMethod BrainrotPlanet.Editor.PlanetEditorSetup.BuildAndroid -logFile "C:\Rabota\projects\BrainrotPlanetUnity\unity-build.log"
```

> Android compilation is **not confirmed** until Unity runs on a machine with Android Build Support. Scripts have not been through a Unity compilation in this environment; errors, if any, will be in Editor.log.

## Game rules

- Four characters land at distant points on one real sphere.
- The player chooses one of three brainrot-inspired characters: **Tralalero Tralala**, **Bombardiro Crocodilo**, **Frigo Camelo**.
- You must **leave your territory and return to it**, closing a colorful path. The smaller enclosed region of the sphere becomes yours. Enemies can run across your unfinished path.
- The real spherical geometry wraps: movement and captured paths can go over the backside of the planet.
- Score uses latitude-weighted spherical surface area. A round is 145 seconds or ends if the player loses all 3 lives.
- Bumps cause small physical hops; steep mountains with collision prohibit shortcuts; manual jumps and radial gravitational pull are enabled.
- Rare gold crystals and common cyan crystals activate only if painted under your control. Crystals release waves and shorten skill cooldown. A gold crystal spawns around 37 seconds.
- **Tralalero**: boost; **Bombardiro**: paint bomb traveling on an arc under spherical gravity; **Frigo**: icy AoE slowing opponents.

## Controls

**Android**: thumb movement pad bottom-left, jump bottom-right, ability above jump. Menu and hero selection by tapping.

**Editor**: WASD / arrow keys, Space to jump, E for ability. Mouse to select menu.

## Assets & rights

All runtime character models are constructed **procedurally from Unity primitive shapes**. They are initial recognizable silhouettes, **not finished, licensed 3D character assets**. No third-party audio, music or downloaded models are bundled. Any commercial release involving recognizable brainrot-character identities or depictions should review usage rights and licensing separately.

## Scope and next improvements

This is a **Unity game implementation**, not an HTML game. Art is intentionally procedural and effects are particle-based for the first playable build; final meshes, animation, character sounds and mobile performance profiling still require work. No online multiplayer; the three enemies use local AI. One arena only, no monetization.

## Code structure

- `PlanetSurface` procedural spherical ground, bumps, mountains and texture painting
- `PlanetActor` radial Rigidbody gravity, jump, speed, grounded slope handling
- `TerritorySystem` latitude-longitude wrapped flood-fill for enclosed area; enemy-trail crossing
- `CrystalSystem` rare and common objective crystals
- `PlanetAI` enemy route control
- `AvatarArt` procedural 3D hero silhouettes
- `PlanetEffects` particles, wave, physics paint bomb
- `PlanetGame` game loop, touch controls, UI, scores, characters
- `PlanetEditorSetup` automatic Unity scene creation + Android APK builder
