# Brainrot Planet.io — 0.5.6 encounter and victory balance

2026-10-12. Unity 6 / Android ARM64, Realme RMX3151.

## Implemented

1. **Equal movement physics.** Player and all AI actors now use the same
   ground movement speed of 6.5 world units/second, without special hidden
   speed advantages. Existing time-limited hero dash and slow abilities work
   unchanged for all teams.
2. **Difficulty-specific encounter spacing.** Bots spawn at approximately
   53° from player on Easy, 42° on Medium and 33° on Hard. All painted
   starting cores remain separated by a safe margin. There is no teleport
   during the match; these offsets apply only to the opening positions.
3. **Fair recovery after an exposed trail is destroyed.** Player movement
   stops after respawn and waits for a fresh swipe, avoiding a repeated
   automatic run into the same opponent. A small temporary initial and
   post-respawn grace period depends on difficulty:
   - Easy: 19s from match start, 7s on respawn.
   - Medium: 12s from match start, 5s on respawn.
   - Hard: 8s from match start, 3s on respawn.
   Players still have three lives and use the existing territory rules.
   This compensates for strong opponents without slowing down or changing
   the physics of the actual characters. AI teams have no comparable grace.
4. **Balance instrumentation (development builds only).** Every 15 seconds
   the game emits a small telemetry summary. Full-match results include:
   winner, team percentages, first/repeated encounters, number of seconds
   opponents were within 48°/28°/15°, shortest separation, player movement
   time, number of captures and losses of exposed trails. No network traffic
   or analytics provider is used.
5. A one-command Unity Editor smoke verifies initial core separation and
   monotonic difficulty opening/recovery parameters.

## Regression tests

- Editor: `AI_BALANCE_SMOKE_PASS encounterSpawns=3 equalSpeed=6.5`.
- Android build: BrainrotPlanet-v0.5.6.apk (versionCode 33).
- Real device: Realme RMX3151, ADB.
- Two AI-vs-player steering harnesses:
  - `qa_balance_056.py`: intentionally long straight segments; demonstrated
    that failing to close a trail can cause rapid defeat (Easy ended in 26s).
    This prompted recovery adjustment. The script is a stress case and
    should NOT be used as a human win-rate estimate.
  - `qa_balance_closed_056.py`: same closed-loop player action script across
    all 3 difficulties and full 145-second matches. Results saved in
    `Characters/QA_BalanceClosed_056.json`.

## Interpretation

Single scripted matches compare difficulty behavior under a controlled input
pattern. They do not establish human success probabilities. More user testing
and randomized runs are needed before quoting win-rate percentages.

## Distribution

APK: https://loadfiles.testmkn.ru/downloads/brainrot-planet/BrainrotPlanet-v0.5.6.apk
Results: https://loadfiles.testmkn.ru/downloads/brainrot-planet/balance/
