from pathlib import Path
import json
import subprocess
import time
import sys

from PIL import Image, ImageDraw


ROOT = Path(r"C:\Rabota\projects\BrainrotPlanetUnity\qa_allheroes_063")
ROOT.mkdir(exist_ok=True)
ADB = [r"C:\adb\adb.exe", "-s", "E6ZTMFVCEA895HAA"]
PACKAGE = "com.mikhnevcode.brainrotplanet"
ACTIVITY = PACKAGE + "/com.unity3d.player.UnityPlayerGameActivity"
HEROES = [
    "Tralalero", "Bombardiro", "Frigo", "Bombombini",
    "Ballerina", "Assassino", "Patapim", "TungSahur",
    "Lirili", "Frulli", "Boneca", "Trippi", "Troppa",
]
CARD_CENTRES = [(290, 915), (790, 915), (290, 1140), (790, 1140)]


def call(*args, timeout=50):
    process = subprocess.run(ADB + list(args), capture_output=True, timeout=timeout)
    message = (process.stdout + process.stderr).decode("utf-8", "replace")
    if process.returncode:
        raise RuntimeError(" ".join(args) + ": " + message[-500:])
    return process.stdout


def snap(name):
    remote = "/sdcard/qa063_" + name + ".png"
    call("shell", "screencap", "-p", remote)
    path = ROOT / (name + ".png")
    call("pull", remote, str(path))
    call("shell", "rm", remote)
    return path


def select_hero(hero):
    for _ in range(hero // 4):
        call("shell", "input", "tap", "950", "745")
        time.sleep(0.35)
    x, y = CARD_CENTRES[hero % 4]
    call("shell", "input", "tap", str(x), str(y))
    time.sleep(1.0)


def test(hero):
    name = HEROES[hero]
    call("shell", "am", "force-stop", PACKAGE)
    call("logcat", "-c")
    call("shell", "am", "start", "-n", ACTIVITY)
    time.sleep(7.0)
    select_hero(hero)
    menu = snap(f"{hero:02d}_{name}_menu")
    menu_logs = call("logcat", "-d", "-s", "Unity:I", "AndroidRuntime:E", "*:S").decode("utf-8", "replace")
    call("shell", "input", "tap", "216", "1885")
    call("shell", "input", "tap", "540", "2060")
    time.sleep(4.5)
    landed = snap(f"{hero:02d}_{name}_landed")
    landing_logs = call("logcat", "-d", "-s", "Unity:I", "AndroidRuntime:E", "*:S").decode("utf-8", "replace")
    for x,y in [(720,1100),(500,880),(280,1100),(500,1320)]*4:
        call("shell", "input", "swipe", "500", "1100", str(x), str(y), "1100")
        time.sleep(.10)
    snap(f"{hero:02d}_{name}_capture")
    capture_logs=call("logcat", "-d", "-s", "Unity:I", "AndroidRuntime:E", "*:S").decode("utf-8", "replace")
    call("shell", "input", "swipe", "410", "960", "740", "960", "580")
    time.sleep(2.2)
    call("shell", "input", "tap", "915", "2045")
    time.sleep(1.8)
    special = snap(f"{hero:02d}_{name}_special")
    call("shell", "input", "tap", "915", "2250")
    time.sleep(2.0)
    jump_logs=call("logcat", "-d", "-s", "Unity:I", "AndroidRuntime:E", "*:S").decode("utf-8", "replace")
    logs = call("logcat", "-d", "-s", "Unity:I", "AndroidRuntime:E", "*:S").decode(
        "utf-8", "replace"
    )
    logs = "\n".join(dict.fromkeys((menu_logs + "\n" + landing_logs + "\n" + capture_logs + "\n" + jump_logs + "\n" + logs).splitlines()))
    interesting = [
        line for line in logs.splitlines()
        if any(key in line for key in (
            "HERO_MENU_SELECTED", "HERO_MENU_PREVIEW_READY", "HERO_MOTION", "BOMBARDIRO_",
            "HERO_SPECIAL", "HERO_FX", "PLAYER_DROP", "PERF_SAMPLE",
            "BONECA_SPRING", "FATAL", "Exception", "ANR", "PLAYER_JUMP", "TRALALERO_ANIMATION", "TERRITORY_CAPTURE", "REFERENCE_SKIN",
        ))
    ]
    (ROOT / (name + ".txt")).write_text("\n".join(interesting), encoding="utf-8")
    result = {
        "hero": hero,
        "name": name,
        "selected": any(f"HERO_MENU_SELECTED index={hero}" in line for line in interesting),
        "preview": any(f"HERO_MENU_PREVIEW_READY index={hero}" in line for line in interesting),
        "rig": any(f"HERO_MOTION_RIG hero={hero}" in line for line in interesting),
        "drop": any(f"HERO_MOTION_DROP hero={hero}" in line for line in interesting),
        "landed": any("PLAYER_DROP_LANDED" in line for line in interesting),
        "run": any(f"HERO_MOTION_ACTIVE hero={hero} state=run" in line for line in interesting),
        "ability": any(f"HERO_SPECIAL_ACTIVATED hero={hero}" in line for line in interesting),
        "animated_ability": any(f"HERO_MOTION_SKILL hero={hero}" in line for line in interesting),
        "vfx": any(f"HERO_FX_ACTIVATED hero={hero}" in line for line in interesting),
        "jump": any(f"PLAYER_JUMP_BEGIN character={hero}" in line for line in interesting),
        "jump_clip": any("|Jump" in line for line in interesting),
        "land_clip": any("|Land" in line for line in interesting),
        "idle_clip": any("|Idle" in line for line in interesting),
        "victory": any(f"HERO_MOTION_VICTORY hero={hero}" in line for line in interesting),
        "victory_clip": any("|VictoryShow" in line for line in interesting),
        "captures": sum("TERRITORY_CAPTURE team=1 " in line for line in interesting),
        "fatal": any("FATAL EXCEPTION" in line for line in interesting),
        "fps_samples": [line for line in interesting if "PERF_SAMPLE stage=game" in line],
    }
    print("HERO_QA", json.dumps(result, ensure_ascii=False), flush=True)
    return result, (menu, landed, special)


call("shell", "svc", "power", "stayon", "usb")
call("shell", "input", "keyevent", "KEYCODE_WAKEUP")
required = ("selected", "preview", "rig", "drop", "landed", "run", "ability", "animated_ability", "vfx", "jump", "victory", "victory_clip")
results = []
screens = []
indices = list(range(len(HEROES)))
if "--hero" in sys.argv:
    indices=[int(sys.argv[sys.argv.index("--hero")+1])]
    if (ROOT / "report.json").exists():
        results=json.loads((ROOT / "report.json").read_text(encoding="utf-8"))
        for r in results:
            i=r["hero"]
            pictures=tuple(ROOT / f"{i:02d}_{HEROES[i]}_{phase}.png" for phase in ("menu","landed","special"))
            if all(p.exists() for p in pictures):screens.append((i,pictures))
if "--retry-failed" in sys.argv and (ROOT / "report.json").exists():
    results = json.loads((ROOT / "report.json").read_text(encoding="utf-8"))
    indices = [r["hero"] for r in results if not all(r.get(k) for k in required) or r.get("fatal")]
    for r in results:
        hero = r["hero"]
        pictures = tuple(ROOT / f"{hero:02d}_{HEROES[hero]}_{phase}.png" for phase in ("menu", "landed", "special"))
        if all(p.exists() for p in pictures): screens.append((hero, pictures))
for hero_index in indices:
    try:
        outcome, pictures = test(hero_index)
        screens = [(i, p) for i, p in screens if i != hero_index]
        screens.append((hero_index, pictures))
    except Exception as error:
        outcome = {"hero": hero_index, "name": HEROES[hero_index], "error": str(error)}
        print("HERO_TEST_ERROR", HEROES[hero_index], error, flush=True)
    results = [r for r in results if r["hero"] != hero_index]
    results.append(outcome)
    results.sort(key=lambda r: r["hero"])
    (ROOT / "report.json").write_text(
        json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8"
    )

cell_width, cell_height = 252, 490
canvas = Image.new("RGB", (4 * cell_width, 12 * cell_height), "#081020")
draw = ImageDraw.Draw(canvas)
for hero_index, pictures in screens:
    column = hero_index % 4
    for phase, picture in enumerate(pictures):
        image = Image.open(picture).convert("RGB")
        image.thumbnail((230, 435))
        row = (hero_index // 4) * 3 + phase
        x = column * cell_width + (cell_width - image.width) // 2
        y = row * cell_height
        canvas.paste(image, (x, y))
        label = (" menu", " landed", " special")[phase]
        draw.text((column * cell_width + 8, y + 440), HEROES[hero_index] + label, fill="white")
canvas.save(ROOT / "All13_Realme_063.jpg", quality=85, optimize=True)

required = ("selected", "preview", "rig", "drop", "landed", "run", "ability", "animated_ability", "vfx", "jump", "victory", "victory_clip")
passed = [result for result in results if all(result.get(key) for key in required) and not result.get("fatal")]
print(f"ALL_HERO_QA_DONE passed={len(passed)}/13", flush=True)
raise SystemExit(0 if len(passed) == len(HEROES) else 1)
