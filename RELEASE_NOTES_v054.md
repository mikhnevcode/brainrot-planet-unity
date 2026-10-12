# Brainrot Planet.io — Release 0.5.4: Tactical AI

Date: 2026-10-12; Unity 6000.2.7f2, Android ARM64, Realme RMX3151.

## Features

The three opponents now use lightweight, context-dependent behavior instead
of the old random three-leg triangle route. Each considers other characters,
current territory and incomplete paint trails every ~0.45–0.78 seconds.

- **Expand:** plan and close loops to add painted cells.
- **Retreat:** return toward the team's home territory to close an exposed loop;
  commit to retreat until the trail has been cleared.
- **Defend:** interrupt an opponent near the team's home core.
- **HuntTrail:** intercept the nearest reachable opponent's exposed trail.
- AI abilities: same 12 distinct game powers as the player, correct team color,
  independent 12.5–17.2 second cooldown with situation-specific activation.
- Fair movement: every team still uses the same PlanetActor/Rigidbody physics,
  trail collision and capture logic.
- Opponent visual tags in player camera view; spawn cores are separated by about
  40 degrees in one planet-facing sector rather than opposite sides of the world,
  making encounters and trail interception realistic.

Performance: AI route decisions throttled; enemy trail detection samples up to
64 points per opposing team and does not scan every frame. Three bots operate
concurrently. The tactical priority rules are unit-testable in `AiTactics.cs`.

## Unity validation

Editor smoke `AI_TACTICS_SMOKE_PASS priorities=5 skills=4 trailTarget=2 reset=OK`.

## Actual Android QA

Full 145s autonomous match, no input from QA script:
- Expand, Retreat, Defend and HuntTrail all occurred.
- 22 recorded new trail intercept targets; 18 autonomous abilities.
- Team territory captures: 23 / 21 / 38 for AI teams 2 / 3 / 4 (82 combined).
- 0 player movement or ability events; bot team 2 won.
- Steady average FPS: 59.09; minimum 5s mean FPS 58.3.
- No fatal Android exception in captured logcat.

Final compiled version after UI text tweak and reduced tactic switching:
- Separate 68s unattended Realme run, 7 intercept targets,
  10 autonomous abilities, 16/16/22 AI captures; 0 player input.
- Average steady FPS: 58.90; minimum: 58.5.
- No Android fatal exceptions.
- Note: final 68s run finishes before match-end, so winner checks reference
  the earlier complete 145s run.

## Evidence

- `Characters/QA_AITactics_054_Full.json`
- `Characters/QA_AITactics_054_FullEvents.txt`
- `Characters/Realme_AITactics_054_Full.jpg`
- `Characters/QA_AITactics_054_Final.json`
- `Characters/QA_AITactics_054_FinalEvents.txt`
- `Characters/Realme_AITactics_054_Final.jpg`
- `qa_ai_tactics_054.py`, `qa_ai_tactics_054_final.py`.

## Public build

https://loadfiles.testmkn.ru/downloads/brainrot-planet/BrainrotPlanet-v0.5.4.apk

Remaining future work:
- Tune AI difficulty with easy/medium/hard mode, rather than hardcoding a
  single skill level, and make encounter intensity adapt to player progress.
- Benchmark GPU and startup/memory separately. All FPS figures above are from
  a Unity development build.
