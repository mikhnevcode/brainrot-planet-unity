# Brainrot Planet.io 0.5.3 — Winner Camera and Full Match QA

2026-10-12 · Unity 6.0 / 6000.2.7f2 · Android ARM64 · Realme RMX3151

## Improvements

- After a match finishes, the camera smoothly moves to the **actual winning team**, including AI opponents, over 1.35 seconds.
- The camera zooms close enough to make the winner's Blender animation legible on a portrait phone while preserving the planetary backdrop.
- The celebration restarts approximately every 2.9 seconds for as long as the results page is visible, so the dance does not freeze after one cycle.
- The result HUD continues to leave the centre of the screen open for the winner.
- The existing "Play again" button resets the camera for orbital descent and begins the next match.
- All 12 Blender characters, Drop/Special/VictoryShow clips, paint logic, movement and skill mechanics are retained.

## Tests on Realme RMX3151

Two separate, real-time **145-second matches** were completed:

1. **Full match with touch events from outside the QA script:** player team 1 won. The camera moved to team 1, the winner dance replayed, and "Play again" started a fresh descent. Average gameplay FPS from 31 development-build samples: **59.05**; lowest five-second sample: **57.5**. No Android fatal exceptions.
2. **Fully passive full match:** no player swipes or ability inputs were recorded; player territory stayed at the initial seed. **AI team 3** won; the camera switched to the Frigo Camelo actor. Three repeats of the victory celebration were recorded. Mean FPS: **59.15**. No Android fatal exceptions.
3. **Short independent 25-second passive input test:** zero automatic movement, zero player skill events and zero player capture events, successful landing and no fatal exception.

The first match's abnormal near-full-player capture (~99.3%) was caused by additional touch/skill events delivered to the app, even though the QA script did not send them. The passive tests did **not** reproduce automatic motion; they are consistent with separate screen input during the first long run. Exact external input source was not identified.

## Evidence

- `Characters/QA_FullMatch_053.json`
- `Characters/Realme_FullMatch_053.jpg`
- `Characters/QA_NoInput_053.json`
- `Characters/QA_AI_Winner_053.json`
- `Characters/Realme_AI_Winner_053.png`
- `qa_full_match_053.py`, `qa_no_input_053.py`, `qa_ai_win_053.py`

Code changes: `Assets/Scripts/OrbitCamera.cs`, `Assets/Scripts/PlanetGame.cs`, `Assets/Editor/PlanetEditorSetup.cs`.

## Distribution

APK: https://loadfiles.testmkn.ru/downloads/brainrot-planet/BrainrotPlanet-v0.5.3.apk

Gallery: https://loadfiles.testmkn.ru/downloads/brainrot-planet/cast/

SHA-256 expected: `ffa0bb417245308d31501e5547cfe7ba6e12a959b4c70f52c66b06c4a1fb91de`.
