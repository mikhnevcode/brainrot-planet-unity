# Brainrot internet match — guest-play test

The menu offers **Quick match**, **Create room**, and **Join by six-character
code**. No player has to enter a server URL or access key. Offline bot matches
remain available. This is a test deployment, not a production matchmaking or
account service.

## Architecture

Android/Windows WSS client → isolated TLS tunnel → `room_gateway.py` on
`127.0.0.1:28180` → one authoritative Unity Linux dedicated-server process per
room, on a separate loopback port starting at `28100`. Never expose the Python
or Unity listeners directly. The gateway caps each room at four peers, starts
up to eight rooms by default, and reaps empty rooms after 20 seconds. Quick
players fill an available public room; creating a private room yields a random
code, which friends enter before that room's match starts. Free slots in a
started match are filled by bots. A private room waits for its first friend
before opening the ten-second lobby. Disconnects hand the character to a bot.

The authoritative server owns movement, jumps, hero powers, collision, capture,
match result, and replay. Clients send input at 20 Hz and receive snapshots at
10 Hz. No account, purchase, or advertising data goes through this gateway.

The v0.7.17 detailed-biome test uses match protocol 7 and the same 6.2-second
deployment flight. Each map now derives its own collision relief from the shared
planet ID and seed. Do not mix protocol-6 workers/clients with this terrain build.
Earlier test rooms stay on their separate gateways.

## Build and isolated hosting

Use Unity 6000.2.7f2 with Linux Dedicated Server Mono and Android IL2CPP.
`BrainrotPlanet.Editor.MatchNetworkBuild.Server` builds the Linux worker in
`Builds/ServerMatchmaker`; `.Android` builds the development APK. Both run
in-editor smoke checks before packaging.

Keep this test deployment separate from TechFounder, shared Caddy, and other
game services. Install `requirements.txt` in a project virtual environment.
Run `python -m unittest -v test_room_gateway` before deploying. Set:

- `BRAINROT_ROOM_SERVER_EXE` to the built Linux executable's absolute path;
- `BRAINROT_ROOM_RUNTIME` to a writable private log directory;
- optionally `BRAINROT_MAX_ROOMS` (1–32), `BRAINROT_ROOM_BASE_PORT`, and
  `BRAINROT_GATEWAY_PORT`.

Start `python room_gateway.py`, then an independent TLS tunnel to
`http://127.0.0.1:28180`. Set `Assets/Resources/MatchEndpoint.txt` to the
tunnel's `wss://.../match` URL before the client build. The tunnel URL is a
temporary test endpoint; it changes when the quick tunnel restarts. The legacy
single-room `gateway.py` and its test key are separate and must not be mixed
with this client build.

## QA

`room_match_smoke.py <wss://host/match>` connects two Quick players plus a
private-room creator and friend. It checks room codes, distinct team slots,
match starts, and matching authoritative snapshots within each room. It
creates real test rooms; run only against the isolated test gateway.

Development clients support opt-in automated QA: create a temporary
`.network-qa` marker in the app's private persistent directory, then remove it
after testing. For old device scripts, `.network-qa-key` and
`BRAINROT_QA_KEY` still trigger the test but their value is not used to join.
QA exercises input, jump, ability, result, replay, and offline return.

## Remaining release gates

- The quick tunnel, worker processes, and logs need durable service supervision
  and a permanent domain for a public release. Capacity and abuse limits need
  load testing, per-client throttling, monitoring, and incident handling.
- A room admits friends only until its match starts. There is no invite link,
  reconnect to an existing team, friend list, or account identity yet.
- WSS/TCP and snapshots need tests on poor cellular connections. Client
  prediction and latency compensation are not implemented.
- iOS/TestFlight, background/resume, accessibility, localization, and long
  two-device soak tests remain outstanding.
- There are no real ads, purchases, or payment integrations in this gateway.
