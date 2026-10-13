from pathlib import Path
import json
import re
import statistics
import subprocess
import sys
import time

root = Path(r"C:\Rabota\projects\BrainrotPlanetUnity\qa_victory_stage_059")
root.mkdir(exist_ok=True)
adb = [r"C:\adb\adb.exe", "-s", "E6ZTMFVCEA895HAA"]
package = "com.mikhnevcode.brainrotplanet"
activity = package + "/com.unity3d.player.UnityPlayerGameActivity"


def call(*args, timeout=75):
    result = subprocess.run(adb + list(args), capture_output=True, timeout=timeout)
    text = (result.stdout + result.stderr).decode("utf-8", "replace")
    if result.returncode:
        raise RuntimeError(" ".join(args) + " " + text[-400:])
    return result.stdout


def screenshot(name):
    remote = "/sdcard/" + name + ".png"
    call("shell", "screencap", "-p", remote)
    local = root / (name + ".png")
    call("pull", remote, str(local))
    return str(local)


def launch(hard=False):
    call("shell", "am", "force-stop", package)
    call("logcat", "-c")
    call("shell", "am", "start", "-n", activity)
    time.sleep(8)
    call("shell", "input", "tap", "861" if hard else "216", "1885")
    time.sleep(.4)
    call("shell", "input", "tap", "530", "2050")
    time.sleep(5)


def unity_log():
    return call("logcat", "-d", "-s", "Unity:I", "AndroidRuntime:E", "*:S").decode(
        "utf-8", "replace")


def stable_metrics(log):
    match = re.findall(
        r"CAM_VICTORY_STABLE upDot=([-0-9.]+) faceDot=([-0-9.]+) "
        r"screen=\(([-0-9.]+),([-0-9.]+),([-0-9.]+)\)", log)
    if not match:
        return None
    up, face, x, y, z = map(float, match[-1])
    return {"up_dot": up, "face_dot": face, "screen_x": x, "screen_y": y, "depth": z}


def validate(label, log, expected_player):
    winners = re.findall(r"HERO_MATCH_VICTORY team=(\d+)", log)
    team = int(winners[-1]) if winners else None
    metrics = stable_metrics(log)
    stage = bool(re.search(r"VICTORY_STAGE_READY team=\d+ worldUp=true", log))
    hero = bool(re.search(r"HERO_VICTORY_STAGE team=\d+ .* upDot=1\.000", log))
    stable = bool(
        metrics
        and metrics["up_dot"] > .85
        and metrics["face_dot"] > .95
        and .38 < metrics["screen_x"] < .62
        and .34 < metrics["screen_y"] < .70
        and metrics["depth"] > 0
    )
    winner_ok = team == 1 if expected_player else team in (2, 3, 4)
    return {
        "scenario": label,
        "winner": team,
        "winner_ok": winner_ok,
        "stage_world_up": stage,
        "hero_world_up": hero,
        "camera_stable": stable,
        "camera_metrics": metrics,
        "fatal": "FATAL EXCEPTION" in log,
    }


call("shell", "svc", "power", "stayon", "usb")
results = []

# Player win: repeat the proven closed-loop route until the real match result.
launch()
arrow_frame = screenshot("EnemyArrows_NoLabels_059")
controls = [(720, 1100), (500, 880), (280, 1100), (500, 1320)]
start = time.monotonic()
step = 0
while time.monotonic() - start < 150:
    x, y = controls[step % 4]
    call("shell", "input", "swipe", "500", "1100", str(x), str(y), "650")
    step += 1
    if step % 8 == 0:
        current = unity_log()
        if "HERO_MATCH_VICTORY team=" in current:
            break
        call("shell", "input", "tap", "910", "2045")
    time.sleep(.10)
time.sleep(.5)
player_frame = screenshot("PlayerVictory_Stable_059_a")
time.sleep(.7)
screenshot("PlayerVictory_Stable_059_b")
time.sleep(1.2)
screenshot("PlayerVictory_Stable_059_c")
player_log = unity_log()
(root / "player_victory_log.txt").write_text(player_log, encoding="utf-8")
player = validate("player_win", player_log, True)
player["control_swipes"] = step
player["play_again"] = False
for button_y in (2050, 1990):
    call("shell", "input", "tap", "530", str(button_y))
    time.sleep(3)
    player_after = unity_log()
    if player_after.count("PLAYER_DROP_BEGIN") >= 2:
        player["play_again"] = True
        break
player["screenshot"] = player_frame
results.append(player)
print("PLAYER_VICTORY_STAGE_QA", json.dumps(player, ensure_ascii=False), flush=True)

if "--player-only" in sys.argv:
    report = {
        "build": "0.5.9",
        "phone": "Realme RMX3151",
        "arrow_frame": arrow_frame,
        "results": results,
    }
    (root / "report_player_final.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    if any(
        not item["winner_ok"]
        or not item["stage_world_up"]
        or not item["hero_world_up"]
        or not item["camera_stable"]
        or not item["play_again"]
        or item["fatal"]
        for item in results
    ):
        raise RuntimeError("Player victory stage regression failed")
    print("PLAYER_VICTORY_STAGE_QA_PASS", flush=True)
    raise SystemExit(0)

# AI win: no player input on Hard for the full 145-second match.
launch(hard=True)
print("AI_PASSIVE_MATCH_STARTED", flush=True)
time.sleep(151)
ai_frame = screenshot("AI_Victory_Stable_059")
ai_log = unity_log()
(root / "ai_victory_log.txt").write_text(ai_log, encoding="utf-8")
ai = validate("ai_win_no_input_hard", ai_log, False)
fps = [float(value) for value in re.findall(r"PERF_SAMPLE stage=game fps=([0-9.]+)", ai_log)]
ai["steady_fps"] = round(statistics.mean(fps[1:]), 2) if len(fps) > 1 else None
ai["play_again"] = False
for button_y in (2050, 1990):
    call("shell", "input", "tap", "530", str(button_y))
    time.sleep(3)
    ai_after = unity_log()
    if ai_after.count("PLAYER_DROP_BEGIN") >= 2:
        ai["play_again"] = True
        break
ai["screenshot"] = ai_frame
results.append(ai)
print("AI_VICTORY_STAGE_QA", json.dumps(ai, ensure_ascii=False), flush=True)

report = {
    "build": "0.5.9",
    "phone": "Realme RMX3151",
    "arrow_frame": arrow_frame,
    "results": results,
}
(root / "report.json").write_text(
    json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")

if any(
    not item["winner_ok"]
    or not item["stage_world_up"]
    or not item["hero_world_up"]
    or not item["camera_stable"]
    or not item["play_again"]
    or item["fatal"]
    for item in results
):
    raise RuntimeError("Victory stage regression failed: " + json.dumps(results))
print("VICTORY_STAGE_QA_PASS", flush=True)
