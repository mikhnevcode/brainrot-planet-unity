# Brainrot Planet.io — Android 0.4.3

12 October 2026. Unity 6 6000.2.7f2, Android 0.4.3 (versionCode 25).

## Desant (drop-in) gameplay

- The player begins each match 9 game units above the landing surface.
- The first 0.65 seconds frame the approach from space; then the player eases down to the surface over the rest of the existing 2.9-second camera fly-in.
- Physics walking is paused for the scripted descent; no territory trail can be drawn while airborne.
- Camera targets the actual landing location during the descent so the actor visibly approaches the ground instead of remaining artificially centred throughout the fall.
- On touchdown, normal collision and player physics resume; a paint-particle burst announces landing; after that, the existing centred camera and permanent-swipe motion continue unchanged.
- Generic Animator uses Jump during descent and Land on touchdown, then Idle/Run.

## Actual QA on Realme RMX3151

The Android APK was installed via ADB and an actual launch was recorded on the phone.
Log report shows PLAYER_DROP_BEGIN altitude=9.00 duration=2.90 followed by falling heights
9.00, 9.00, 8.64, 6.88, 4.23, 1.77, 0.16 and PLAYER_DROP_LANDED. An
AUTOMOVE log entry after touchdown confirms normal movement resumed. There were
no AndroidRuntime fatal exceptions in the captured test log.
A separate animation log confirms Jump -> Land -> Idle -> Run in the real device.

- Device screenshots: Characters/Realme_DropSequence_043.jpg
- Device report: Characters/Drop_043_QA.json
- Device filtered log: Characters/Drop_043_Events.txt
- Reproducible ADB QA: qa_drop_v043.py

Only the deployment sequence, not the game mechanics or character roster, changed.

## Release

APK:
https://loadfiles.testmkn.ru/downloads/brainrot-planet/BrainrotPlanet-v0.4.3.apk

Gallery:
https://loadfiles.testmkn.ru/downloads/brainrot-planet/cast/

The public APK is verified by HTTPS and downloaded SHA-256 before release.
