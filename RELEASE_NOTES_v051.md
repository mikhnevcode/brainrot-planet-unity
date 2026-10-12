# Brainrot Planet.io — release 0.5.1

Built 2026-10-12 on DEV, Unity 6 6000.2.7f2 (Android IL2CPP).
APK: `BrainrotPlanet-v0.5.1.apk`, Android versionCode 28.

## New gameplay and character personality

All 12 heroes receive per-character secondary animations in `HeroMotion.cs`:
- breathing and idle gestures, individual run body movement;
- panic-like arm/wing/leg flailing on the **existing physical** orbital drop;
- reactive motion and one signature color wave on special ability;
- individual ~2.55-second victory choreography triggered by meaningful territory capture (at least 150 cells), plus match-end celebration.

All motion is visual-only and layered after imported Blender clips.
Existing .blend and FBX source models remain unchanged; these are **procedural
animations in Unity**, not newly hand-keyframed Blender actions.

All twelve active abilities have theme-matched behavior:
- Tralalero shark sprint;
- Bombardiro three paint bombs;
- Frigo ice slow;
- Bombombini jet dash;
- Ballerina coffee whirl;
- Assassino shadow sprint and small paint claim;
- Patapim root slow and paint;
- Tung Sahur drum shock and slow;
- Lirili cactus splash and slow;
- Frulli wing-rush and small splash;
- Boneca a real spring jump with paint applied **on landing**;
- Trippi three separate shrimp paint splashes.

The cooldown remains 12 seconds. Player input, collisions, actual spherical
territory capture, smooth 512x256 visual paint and 12 selectable roster persist.

The end-of-match UI no longer paints the entire screen almost opaque; the
winner's dance can be seen between the top scoreboard and bottom action buttons.

## Tested on real Android device

Device: Realme RMX3151 (ADB serial E6ZTMFVCEA895HAA).

A real on-device scripted QA pass against APK **0.5.0** completed 12/12:
- actor skeleton was loaded and animated;
- airborne drop + landing worked;
- locomotion got active running animation;
- all 12 specials and their procedural actions and themed particles triggered;
- Boneca additionally logged paint on touchdown;
- no Android fatal exceptions in the captured logs.

A second on-device QA pass on APK **0.5.1** tested two large-capture
victory celebrations: Tralalero and Ballerina both recorded three player
captures, `HERO_MOTION_VICTORY`, and active victory state, without Android
fatal errors. Codepaths for ten other dances are present, but the real-device
regression only explicitly validated these two.

Artifacts:
- `Characters/Hero_Motion_050_QA.json`
- `Characters/Realme_Hero_Motions_050.jpg`
- `Characters/Victory_051_QA.json`
- `Characters/Victory_Dances_Realme_051.jpg`
- `CHARACTER_GAMEPLAY_050.md`
- `qa_hero_motion_050.py`, `qa_victory_051.py`

## Known limitations / later work

- Motion is a first pass. Detail readability at current half-scale hero size
  requires further artistic passes / hand-keyframing.
- Exact mobile frame-time cost of twelve procedural visual animations has not
  been benchmarked with GPU/CPU profiling. No per-frame particles or new
  bone searches are performed by HeroMotion, but that is not an FPS claim.
- A full-length 145-second match-end celebration has not yet been separately
  recorded; only territory-capture victory triggers were validated.

## Distribution

APK: https://loadfiles.testmkn.ru/downloads/brainrot-planet/BrainrotPlanet-v0.5.1.apk
Cast gallery, real-device QA and screenshots:
https://loadfiles.testmkn.ru/downloads/brainrot-planet/cast/

SHA-256 expected for the published APK:
`e2fa4398dacf1bcd9054002513ebb4feeb4d8223e628fd26d0d5595e2bfdb342`
