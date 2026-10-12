# Brainrot Planet.io 0.4.2 — character scale and territory seams

Build date: 2026-10-12. Unity 6000.2.7f2, Android / Realme RMX3151.

## Changes

1. All character visuals are scaled to exactly 50% via the shared `AvatarArt.Make` hierarchy; the placement offset is also halved so characters remain close to the ground. The paint aura under the character is half as wide. Actor speed, controller input, and collision radius are unchanged.
2. `TerritorySystem.Capture` records a snapshot of the owner's existing painted cells. When the old land plus the new trail divide the sphere into components, the enclosed component is used as the fill mask; the direct trail polygon remains a fallback.
3. After normal filling, `RepairCaptureSeams` paints only short neutral gaps within three grid cells of *both* old territory and newly captured cells. It wraps across the longitude UV seam and does not overwrite another team's land. The old/new snapshot is reused, and candidates are marked in a separate array to avoid iterative spreading.
4. Editor regression `TerritoryJoinSmoke.Run()` is executed before the Android build.

## Verified results

- Unity editor smoke: `TERRITORY_JOIN_SMOKE_PASS seam=41 oldPercent=1.11 capturedPercent=7.07 events=1`.
- Checked seam continuity, longitude wrap, enemy cell preservation, and avoidance of distant unrelated territory growth.
- Android build: 0.4.2 (versionCode 24), successful.
- ADB install: Realme RMX3151, serial E6ZTMFVCEA895HAA, successful.
- On-device 12-swipe capture test: 4 player territory captures, 4 seam-repair events (28, 25, 29, 39 cells). Automatic motion remained active. No Android fatal exceptions were reported.
- Artifacts: `Characters/Realme_Territory_042.jpg`, `Characters/Territory_042_QA.json`, `Characters/Territory_042_Events.txt`.
- Public APK: https://loadfiles.testmkn.ru/downloads/brainrot-planet/BrainrotPlanet-v0.4.2.apk
- Public image / QA: https://loadfiles.testmkn.ru/downloads/brainrot-planet/cast/
- Downloaded APK over HTTPS returned HTTP 200, size 52,441,924 bytes, SHA-256 `cc462e829491c06952915092eca44a136a0fdebb11ab42b4365b3f828c5451d2`.

## Limitations

These tests prove typical connected loops and narrow seam repair. Broad visual QA across all terrain, all longitudes, large intersecting player/enemy loops, and longer game sessions is a separate step. The current repair intentionally avoids joining widely separated areas.
