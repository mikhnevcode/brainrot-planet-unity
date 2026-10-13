# Brainrot internet match — private alpha

Offline remains available. This is a single invited room, not public matchmaking.

## Architecture

Android/Windows ClientWebSocket (WSS, binary) → isolated TLS tunnel → Python gateway
on **127.0.0.1:27767** → authoritative Unity Linux dedicated server on
**127.0.0.1:27766**. Do not expose either loopback listener directly.

The server runs movement, jumps, hero powers, terrain/trail collisions, ownership,
three lives for every participant, match time and winner selection. Clients send
input at 20 Hz and interpolate 10 Hz full ownership/pose snapshots. Free slots are
bots. A disconnected player's living character becomes a bot. No account or
monetization data is collected by this prototype.

## Build

Unity 6000.2.7f2 with Linux Dedicated Server Mono and Android IL2CPP modules.
Batch entrypoints (specify corresponding `-buildTarget` before executeMethod):

- `BrainrotPlanet.Editor.MatchNetworkBuild.Server` — Linux64, server subtarget.
- `BrainrotPlanet.Editor.MatchNetworkBuild.Windows` — Win64, player subtarget.
- `BrainrotPlanet.Editor.MatchNetworkBuild.Android` — Android, development APK.

All entrypoints run protocol, territory, outcome and collision smoke checks.
The temporary default WSS URL is `Assets/Resources/MatchEndpoint.txt`; it contains
no key. Update it when the quick tunnel changes, or enter a new URL in the menu.

## Isolated hosting

Use a separate directory/user environment; do not edit TechFounder, shared Caddy,
or production tunnel configurations. Install `requirements.txt` in a project venv.
Run `python -m unittest -v test_gateway`, then:

1. Start the Linux executable with `-batchmode -nographics -brainrot-server`.
2. Start `gateway.py` from the isolated hosting directory. It creates
   `runtime/test.key` with mode 0600 if absent. Never publish/commit that file.
3. Start an independent `cloudflared tunnel --url http://127.0.0.1:27767`.
4. Enter its `wss://.../match` URL and the private test key on each phone.

First participant opens a ten-second join window. Join before the round starts.
Late joins are currently rejected. After the result, every remaining human must
press replay; leaving the room also removes their readiness requirement.
Closing all clients frees the room for a fresh round. Quick tunnels are temporary
and aren't a production availability solution. Processes must be supervised for
a release; a tunnel restart changes the URL.

## QA

`MatchProtocolSmoke.Run`: bounded frames, RLE baseline, malformed owners, numbers,
quaternions, teams and trailing data. Gateway unit tests exercise key/path checks,
binary round trips, text/oversize rejection and capacity.

`wss_match_smoke.py <wss-url>` uses `BRAINROT_QA_KEY` from its environment and
compares whole snapshots from two independent external peers. It opens a real
room; only run against the dedicated test instance.

Development clients alone support opt-in QA: `BRAINROT_QA_KEY` environment on PC,
or app-persistent `.network-qa-key` on Android. A scripted hero exercises input,
jump, powers, result, replay and offline return. Remove the file after testing!
Optional PC `BRAINROT_QA_LEAVE=1` disconnects after 35 seconds to test bot takeover.
For manual testing, `.network-test-key` pre-fills only the private development
room field and never auto-joins. Neither file belongs in source control or APKs.

## Known alpha limitations / next gates

- One room/four participants; no room directory, matchmaking or account identity.
- WSS/TCP is adequate for this test; not yet tested on loss-heavy cellular links.
- No client prediction, latency compensation or reconnect-to-existing-slot.
- Crystals remain offline-only; network projectile flight visuals are not yet
  replicated (server explosions, paint and sound are replicated).
- Eliminated players wait for the result; spectator camera is still pending.
- UI is Russian-only. Room key entry and full/active-room errors need polish.
- iOS build/TestFlight, background/resume, malicious-load and long soak tests
  remain release gates. No claim of production readiness or store approval.
- No ads, purchases, rewards economy or external payment integration is included.

The original 0.6.6 release APK is not overwritten by these build entrypoints.
