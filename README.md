# Brainrot Planet.io — Unity 6 playable game scene

**Version**: 0.6.0 / polished Android gameplay build
**Unity Editor**: 6000.2.7f2 (a different Unity 6000.2 patch should normally open the project)
**Platform**: Android (ARM64 / IL2CPP), Editor testing on Windows.

## Play in Unity

1. Open the folder **BrainrotPlanetUnity** using Unity Hub (Unity 6).
2. Let the editor import packages and scripts. The editor script creates `Assets/Scenes/PlanetArena.unity` if it is missing. If the scene is not open, choose **Brainrot Planet → 1. Create and open game scene**.
3. Press **Play**, select a character and press **ДЕСАНТИРОВАТЬСЯ**.

## Android APK

Run **RUN_ANDROID_BUILD_V060.cmd** on Windows, or use **Brainrot Planet → 2. Build Android APK** inside Unity. Unity Android Build Support, SDK, NDK and OpenJDK must be installed in Unity Hub. The result is `Builds/Android/BrainrotPlanet-v0.6.0.apk`.

Verified APK: https://loadfiles.testmkn.ru/downloads/brainrot-planet/cast/BrainrotPlanet-v0.6.0.apk
SHA-256: `1E6D15D4B7873D34682CBFE3B94800CB30854B734E3F3B737BDA0D5BBAEB3198`

Alternatively, close the Editor and run:

```
"C:\Program Files\Unity\Hub\Editor\6000.2.7f2\Editor\Unity.exe" -batchmode -nographics -quit -projectPath "C:\Rabota\projects\BrainrotPlanetUnity" -executeMethod BrainrotPlanet.Editor.PlanetEditorSetup.BuildAndroid -logFile "C:\Rabota\projects\BrainrotPlanetUnity\unity-build.log"
```

The 0.6.0 ARM64/IL2CPP build and its smoke tests were completed with Unity 6000.2.7f2 and verified on a Realme RMX3151.

## Game rules

- Four characters land at distant points on one real sphere.
- The player chooses one of 12 animated heroes and one of three AI difficulty levels.
- You must **leave your territory and return to it**, closing a colorful path. The smaller enclosed region of the sphere becomes yours. Enemies can run across your unfinished path.
- The real spherical geometry wraps: movement and captured paths can go over the backside of the planet.
- Score uses latitude-weighted spherical surface area. A round is 145 seconds or ends if the player loses all 3 lives.
- Seven rounded hills cause physical hops; six standalone craters have readable rims and bowls. There are no artificial cylinder mountains or ridge chains.
- Rare gold crystals and common cyan crystals activate only if painted under your control. Crystals release waves and shorten skill cooldown. A gold crystal spawns around 37 seconds.
- All 12 heroes have an individual ability, authored Drop/Special/VictoryShow clips and a live 3D portrait in the selection menu.

## Controls

**Android**: swipe to set a direction; movement continues after release until another direction is selected. Jump is bottom-right and the ability is above it. Menu and hero selection are controlled by tapping.

**Editor**: WASD / arrow keys, Space to jump, E for ability. Mouse to select menu.

## Assets & rights

The 12 runtime characters use project-owned Blender/FBX sources and Unity Animator controllers; primitive geometry remains only as a defensive fallback. No third-party audio, music or downloaded models are bundled. Any commercial release involving recognizable brainrot-character identities or depictions should review usage rights and licensing separately.

## Scope and next improvements

This is a **Unity game implementation**, not an HTML game. It is a local single-player match against three AI opponents on one spherical arena, with no networking or monetization. The primary Android target holds 56.7–59.1 FPS in the documented 0.6.0 QA runs.

## Code structure

- `PlanetSurface` procedural spherical ground, mesh-native hills/craters and texture painting
- `PlanetActor` radial Rigidbody gravity, jump, speed, grounded slope handling
- `TerritorySystem` latitude-longitude wrapped flood-fill for enclosed area; enemy-trail crossing
- `CrystalSystem` rare and common objective crystals
- `PlanetAI` enemy route control
- `AvatarArt` Blender/FBX loading, material palettes and primitive fallbacks
- `PlanetEffects` particles, wave, physics paint bomb
- `PlanetGame` game loop, touch controls, UI, scores, characters
- `PlanetEditorSetup` automatic Unity scene creation + Android APK builder
