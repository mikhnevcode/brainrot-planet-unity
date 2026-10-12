# Brainrot Planet.io — Release 0.5.2

Date: 2026-10-12. Unity 6000.2.7f2, Android IL2CPP, Realme RMX3151.

## Artistic update: native Blender animations

All 12 selectable heroes now have three *keyframed, exported FBX action clips*
from Blender 5.2.2 LTS, integrated into the existing Animator state machines:

- **Drop** (73 frames): unique mid-air struggle / kicking / arm or wing flailing during the 2.9-second orbital approach.
- **Special** (34 frames): character-specific wind-up and signature gesture for each active power.
- **VictoryShow** (73 frames): unique dance with ballet pirouettes, airplane rolls, ninja salute, tire spin, elephant trunk gestures, and so on.

These are no longer solely procedural HeroMotion gestures. All original
Idle, Run, Jump, Land and Victory actions remain. The HeroMotion layer still
adds subtle breathing and secondary movement above the authored clips.
Gameplay physics, collision masks and the camera are unaffected.

Original Blender .blend sources and FBXs are preserved in Characters folders.
New source versions are .blend v02, except Tralalero v03, with matching
versioned Unity FBXs. Obsolete FBXs and their .meta files were moved from
Assets/Resources/Characters to Characters/LegacyUnityFBX (not deleted), so the
APK packages only the 12 current character models.

Eight of the first-generation source Blender scenes contained accumulated,
unrelated older actions, such as Idle.007 and Run.007. The new Blender
generation script identifies the rig's own five actions and discards only
the stale copies *in the new versions*, preserving source archives and
preventing incorrect animation mappings.

## Unity integration

- `HeroAnimationUpgrade.cs`: checks for all 36 imported actions and creates
  Animator Drop/Special/VictoryShow states for all 12.
- PlanetActor sets the `Dropping` bool for actual orbital descent, triggers
  `Special` for abilities and `Victory` for celebrations.
- GamePerformanceProbe collects actual frame pacing metrics every five
  seconds in development builds without scene search or frame logging spam.
- Character render prefabs and rig importers now refer to the new FBX names.

## Realme RMX3151 device testing

**12/12 pass (Drop & Special):**
- Source rig and character loaded correctly.
- The new Blender Drop animation played in flight.
- The player landed and the Run cycle operated.
- Special ability and its authored animation clip activated successfully.
- No Android fatal errors.
- Boneca spring still paints on touchdown.

**12/12 pass (VictoryShow):**
- Each hero closed 3–4 actual territory loops and triggered the Victory
  celebration.
- Its corresponding new Blender VictoryShow clip was observed playing.
- No Android fatal errors.

**Steady-state FPS benchmark (~40 seconds):**
- 8 consecutive five-second active gameplay samples, excluding initial
  application startup and the first partly-loaded interval.
- Average FPS: **58.80**; median 58.80; minimum of sample FPS: 58.3.
- Every five-second sample had estimated p95 frame time around 17 ms.
- Rare individual longer frames were observed (up to 83.8 ms during steady
  gameplay). Startup/transition frames were slower, not included in the
  steady-state performance figure.
- FPS measured using unscaled Unity frame delta; these are not GPU profiler
  measurements.

## Files and test evidence

- Blender generator: `Characters/make_motions_v02.py`
- Preview renderer: `Characters/render_motions_v02.py`
- All 12 new .blend sources, .fbx assets and motion metadata retained in Git.
- QA: `Characters/QA_Authored_052.json`,
  `Characters/QA_VictoryAll_052.json`,
  `Characters/QA_Performance_052.json`.
- Android screenshot sheets in `Characters/Realme_Authored_052.jpg`,
  `Characters/Realme_VictoryAll_052.jpg`.
- QA scripts: `qa_authored_052.py`, `qa_victory_all_052.py`,
  `qa_fps_v052.py`.

Public downloads:

- APK: https://loadfiles.testmkn.ru/downloads/brainrot-planet/BrainrotPlanet-v0.5.2.apk
- Motion gallery: https://loadfiles.testmkn.ru/downloads/brainrot-planet/motions/
- Full cast: https://loadfiles.testmkn.ru/downloads/brainrot-planet/cast/

Expected APK SHA-256:
`7987d48e61e0db7e4320ca531dbc801e0b3d1034372a009ed2d613ff4b40d314`

## Next opportunities

At the current camera height heroes are intentionally small; artistic
refinement can improve silhouettes and larger readable limb gestures.
Tune the more energetic procedural animation overlays to avoid exaggerated
movement when the Blender clip already performs a spin. Add a GPU profile
and full 145-second match soak test before distribution to a broader audience.
