# Brainrot Planet.io — 0.5.8: larger planet and hero pass-through

Date: 2026-10-12. Unity 6000.2.7f2, ARM64 Android / Realme RMX3151.

## Changes

1. Radius increased from 14 to **24 world units** (~71% larger).
   Circumference increases from ~87.96 to ~150.80 units. A 4-second
   1.85× turbo boost at base speed 6.5 units/s covers ~31.9% of
   the new circumference instead of ~54.7%. The actors' absolute speed
   and the existing camera offset are unchanged; the hero does not
   become smaller on screen just because the planet grew.
2. AI angular loop legs scale by 14/24 to keep physical travel distances
   per planned expansion comparable. Starting difficulty-specific
   spawn angles remain unchanged, giving more space between encounters.
3. Hero SphereCollider radius reduced **0.66 to 0.46 world units**,
   matching the previously reduced avatar art. No trigger hack is used.
4. All six hero-to-hero contact pairs are marked with
   `Physics.IgnoreCollision(a,b,true)`. Pair state is reapplied after
   the player returns from the orbital drop, when the player's collider
   becomes enabled again.
5. Collision with the planet mesh, rocks, bomb contacts and territory
   logic is preserved. Crossing an opponent's exposed paint trail is
   detected independently by `TerritorySystem`, not by a physical
   hero collider; it remains dangerous.

## Regressions

Unity Editor:
- `ACTOR_COLLISION_SMOKE_PASS ignoredPairs=6 terrainContact=true
   radius=0.46 landingRefresh=true`.
- `PLANET_SCALE_SMOKE_PASS oldRadius=14 radius=24
   circumference=150.80 boostShare=0.319 aiRoute=4.48
   collisionRadius=0.46`.
- Existing territory, AI tactics, AI difficulty and encounter smoke
  tests passed.
- Android build 0.5.8, versionCode 35, exited with code 0.

Realme RMX3151:
- Six ignored hero pairs confirmed at spawn and after orbit landing.
- The mean ground actor radial coordinate was 25.017 units
  (radius 24 + terrain elevation + actor clearance).
- Player landed, moved with swipes, closed **3** real territory
  loops and triggered **3** seam repairs.
- 7 FPS samples, mean **57.56 FPS** in this short test.
- 0 fatal AndroidRuntime errors.
- Viewport screenshots and JSON:
  `Characters/Realme_PlanetScaleCollision_058.jpg`,
  `Characters/QA_PlanetScaleCollision_058.json`.

Note: this is the first implementation of larger-radius gameplay.
Subsequent longer AI-vs-player balance tests should measure encounter
frequency on the more spacious planet before readjusting spawns.
The logic grid remains 256x128 and smooth 512x256 texture is retained.

## Files

- `Assets/Scripts/ActorCollisionRules.cs`
- `Assets/Scripts/PlanetActor.cs`
- `Assets/Scripts/PlanetGame.cs`
- `Assets/Scripts/PlanetMath.cs`
- `Assets/Scripts/PlanetAI.cs`
- `Assets/Editor/ActorCollisionSmoke.cs`
- `Assets/Editor/PlanetScaleSmoke.cs`
- `qa_scale_collision_058.py`

APK: https://loadfiles.testmkn.ru/downloads/brainrot-planet/BrainrotPlanet-v0.5.8.apk
