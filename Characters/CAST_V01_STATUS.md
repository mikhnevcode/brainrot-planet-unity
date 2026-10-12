# Brainrot Planet.io — complete first-pass character roster

Date: 2026-10-12. Unity 6 6000.2.7f2, Android / Realme RMX3151.

## Roster, game index, asset status

| Index | Character | Blender model |
|---|---|---|
| 0 | Tralalero Tralala | Tralalero_v02 |
| 1 | Bombardiro Crocodilo | Bombardiro_v01 |
| 2 | Frigo Camelo | Frigo_v01 (replaces primitive fallback) |
| 3 | Bombombini Gusini | Bombombini_v01 |
| 4 | Ballerina Cappuccina | Ballerina_v01 |
| 5 | Cappuccino Assassino | Assassino_v01 |
| 6 | Brr Brr Patapim | Patapim_v01 |
| 7 | Tung Tung Tung Sahur | TungSahur_v01 |
| 8 | Lirili Larila | Lirili_v01 |
| 9 | Frulli Frulla | Frulli_v01 |
| 10 | Boneca Ambalabu | Boneca_v01 |
| 11 | Trippi Troppi | Trippi_v01 |

The order in the Unity selection menu keeps the previously released Frigo placeholder
in index 2 to preserve existing game identity / team logic. It has now been replaced
with the Blender-backed animated Frigo model.

## Generated source

- Characters/remaining_characters_v01.py: reusable Blender 5.2.2 Python generator.
- Characters/<Hero>/<Hero>_v01.blend: editable geometry / armature / Actions.
- Assets/Resources/Characters/<Hero>_v01.fbx: Unity model + animation clips.
- Assets/Resources/Characters/<Hero>.controller: Animator state machine.
- Assets/Editor/RemainingHeroesRigSetup.cs: import + controller setup.
- Actions: Idle, Run, Jump, Land, Victory; 7 common bones per first-pass model.
- Assets/Scripts/AvatarArt.cs: runtime loading and matte re-coloring.
- Assets/Scripts/PlanetGame.cs: 12-item Android paged menu and temporary hero skills.
- Tests: qa_allheroes_041.py (device selection, movement, ability and log capture).

## Gameplay invariants

Maintain persistent swipe direction after finger release, near-surface fixed-centered
camera, jump/super ability buttons, full opaque territory capture on loop closure,
4 teams and a 145-second match. Do not replace previously completed heroes.

## Scope

User asked to create all 12 first before revisions. These models are *first passes*;
do not describe them as final likenesses or fully polished animation. Continue quality
refinement across the whole cast only when the user requests it.

## Deployment

Build method: BrainrotPlanet.Editor.PlanetEditorSetup.BuildAndroid,
scheduled in logged-in ADMIN session on DESKTOP-5ICSG76 using
RUN_ANDROID_BUILD_V041.cmd. Final APK: 0.4.1, versionCode 23.

Public deployment target:
https://loadfiles.testmkn.ru/downloads/brainrot-planet/BrainrotPlanet-v0.4.1.apk
Public cast gallery:
https://loadfiles.testmkn.ru/downloads/brainrot-planet/cast/

Never mark Android checks passed without actual QA logs; consult
qa_allheroes_041/qa_report.json when transferred from DEV.
