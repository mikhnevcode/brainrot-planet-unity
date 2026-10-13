from pathlib import Path
import json
import re
import statistics
import subprocess
import time


ROOT = Path(r"C:\Rabota\projects\BrainrotPlanetUnity\qa_victory_stage_060")
ROOT.mkdir(exist_ok=True)
ADB = [r"C:\adb\adb.exe", "-s", "E6ZTMFVCEA895HAA"]
PACKAGE = "com.mikhnevcode.brainrotplanet"
ACTIVITY = PACKAGE + "/com.unity3d.player.UnityPlayerGameActivity"


def call(*args, timeout=75):
    process = subprocess.run(ADB + list(args), capture_output=True, timeout=timeout)
    output = (process.stdout + process.stderr).decode("utf-8", "replace")
    if process.returncode:
        raise RuntimeError(" ".join(args) + ": " + output[-500:])
    return process.stdout


def launch(difficulty_x):
    call("shell", "am", "force-stop", PACKAGE)
    call("logcat", "-c")
    call("shell", "am", "start", "-n", ACTIVITY)
    time.sleep(7.0)
    call("shell", "input", "tap", str(difficulty_x), "1885")
    time.sleep(0.4)
    call("shell", "input", "tap", "540", "2060")
    time.sleep(5.0)


def logcat():
    return call("logcat", "-d", "-s", "Unity:I", "AndroidRuntime:E", "*:S").decode(
        "utf-8", "replace"
    )


def screenshot(name):
    remote = "/sdcard/" + name + ".png"
    call("shell", "screencap", "-p", remote)
    local = ROOT / (name + ".png")
    call("pull", remote, str(local))
    call("shell", "rm", remote)
    return str(local)


def metrics(raw):
    match = re.findall(
        r"CAM_VICTORY_STABLE upDot=([-0-9.]+) faceDot=([-0-9.]+) "
        r"screen=\(([-0-9.]+),([-0-9.]+),([-0-9.]+)\)", raw
    )
    if not match:
        return None
    up, face, x, y, depth = map(float, match[-1])
    return {"up_dot": up, "face_dot": face, "screen_x": x, "screen_y": y, "depth": depth}


def validate(label, raw, player_should_win):
    winners = re.findall(r"HERO_MATCH_VICTORY team=(\d+)", raw)
    winner = int(winners[-1]) if winners else None
    camera = metrics(raw)
    fps = [float(value) for value in re.findall(r"PERF_SAMPLE stage=game fps=([0-9.]+)", raw)]
    return {
        "scenario": label,
        "winner": winner,
        "winner_ok": winner == 1 if player_should_win else winner in (2, 3, 4),
        "stage_world_up": bool(re.search(r"VICTORY_STAGE_READY team=\d+ worldUp=true", raw)),
        "hero_world_up": bool(re.search(r"HERO_VICTORY_STAGE team=\d+ .* upDot=1\.000", raw)),
        "camera_stable": bool(
            camera and camera["up_dot"] > 0.85 and camera["face_dot"] > 0.95
            and 0.38 < camera["screen_x"] < 0.62 and 0.34 < camera["screen_y"] < 0.70
            and camera["depth"] > 0
        ),
        "camera_metrics": camera,
        "steady_fps": round(statistics.mean(fps[1:]), 2) if len(fps) > 1 else None,
        "fatal": "FATAL EXCEPTION" in raw,
    }


def test_play_again(raw_before):
    drops_before = raw_before.count("PLAYER_DROP_BEGIN")
    call("shell", "input", "tap", "540", "2010")
    time.sleep(3.0)
    return logcat().count("PLAYER_DROP_BEGIN") > drops_before


call("shell", "svc", "power", "stayon", "usb")
results = []

# Easy controlled route: repeat a closed loop until the real match declares the player winner.
launch(216)
route = [(720, 1100), (500, 880), (280, 1100), (500, 1320)]
started = time.monotonic()
swipes = 0
while time.monotonic() - started < 150:
    x, y = route[swipes % len(route)]
    call("shell", "input", "swipe", "500", "1100", str(x), str(y), "650")
    swipes += 1
    if swipes % 8 == 0:
        current = logcat()
        if "HERO_MATCH_VICTORY team=" in current:
            break
        call("shell", "input", "tap", "915", "2045")
    time.sleep(0.10)
time.sleep(1.0)
player_log = logcat()
(ROOT / "player_victory_log.txt").write_text(player_log, encoding="utf-8")
player = validate("player_win_controlled", player_log, True)
player["control_swipes"] = swipes
player["screenshot"] = screenshot("PlayerVictory_Stable_060")
player["play_again"] = test_play_again(player_log)
results.append(player)
print("PLAYER_VICTORY_060", json.dumps(player, ensure_ascii=False), flush=True)

# Hard passive route: AI must win without player input in the regular 145-second match.
launch(861)
print("AI_PASSIVE_MATCH_060_STARTED", flush=True)
time.sleep(151.0)
ai_log = logcat()
(ROOT / "ai_victory_log.txt").write_text(ai_log, encoding="utf-8")
ai = validate("ai_win_no_input_hard", ai_log, False)
ai["screenshot"] = screenshot("AI_Victory_Stable_060")
ai["play_again"] = test_play_again(ai_log)
results.append(ai)
print("AI_VICTORY_060", json.dumps(ai, ensure_ascii=False), flush=True)

report = {"build": "0.6.0", "phone": "Realme RMX3151", "results": results}
(ROOT / "report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
failed = [
    item for item in results
    if not item["winner_ok"] or not item["stage_world_up"] or not item["hero_world_up"]
    or not item["camera_stable"] or not item["play_again"] or item["fatal"]
]
print(f"VICTORY_STAGE_060_DONE passed={len(results)-len(failed)}/2", flush=True)
raise SystemExit(1 if failed else 0)
