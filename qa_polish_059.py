from pathlib import Path
import json,re,subprocess,time
from PIL import Image,ImageDraw

PROJECT=Path(r"C:\Rabota\projects\BrainrotPlanetUnity")
OUT=PROJECT/"qa_polish_059"
OUT.mkdir(exist_ok=True)
ADB=[r"C:\adb\adb.exe","-s","E6ZTMFVCEA895HAA"]
APK=PROJECT/r"Builds\Android\BrainrotPlanet-v0.5.9.apk"

def adb(*args,timeout=90):
    p=subprocess.run(ADB+list(args),capture_output=True,timeout=timeout)
    output=(p.stdout+p.stderr).decode("utf-8","replace")
    if p.returncode:
        raise RuntimeError("ADB %r failed: %s"%(args,output[-600:]))
    return output

def shot(name):
    remote="/sdcard/qa059_%s.png"%name
    adb("shell","screencap","-p",remote)
    local=OUT/(name+".png")
    adb("pull",remote,str(local))
    return local

adb("install","-r",str(APK),timeout=240)
adb("shell","svc","power","stayon","usb")
adb("shell","input","keyevent","KEYCODE_WAKEUP")
adb("shell","am","force-stop","com.mikhnevcode.brainrotplanet")
adb("logcat","-c")
adb("shell","am","start","-n","com.mikhnevcode.brainrotplanet/com.unity3d.player.UnityPlayerGameActivity")
time.sleep(7)
screens=[shot("menu")]

# Start with the persisted difficulty. The launch button remains at the bottom.
adb("shell","input","tap","530","2050")
time.sleep(4.7)
screens.append(shot("landed_stopped"))

# A rightward swipe starts persistent motion; release and wait before capture.
adb("shell","input","swipe","250","2030","780","2030","420")
time.sleep(2.2)
screens.append(shot("autorun_right"))

# Turn upward, then activate the hero ability while continuing to move.
adb("shell","input","swipe","250","2030","250","1450","420")
time.sleep(1.0)
adb("shell","input","tap","925","1960")
time.sleep(1.2)
screens.append(shot("special_active"))

# Make a compact loop and capture both exposed-trail and post-capture states.
for i,(x,y) in enumerate([(780,2030),(250,1450),(30,2030),(250,2290)]*2):
    adb("shell","input","swipe","250","2030",str(x),str(y),"720")
    time.sleep(.15)
    if i==2:screens.append(shot("open_trail_risk"))
time.sleep(2.0)
screens.append(shot("after_capture"))

log=adb("logcat","-d","-s","Unity:I","AndroidRuntime:E","*:S")
(OUT/"unity_log.txt").write_text(log,encoding="utf-8")
package=adb("shell","dumpsys","package","com.mikhnevcode.brainrotplanet")
version_name=re.search(r"versionName=([^\s]+)",package)
version_code=re.search(r"versionCode=(\d+)",package)
perf=re.findall(r"PERF_SAMPLE stage=game fps=([0-9.]+).*?managedMB=([0-9.]+) gcDelta=([0-9/]+)",log)
result={
    "build":version_name.group(1) if version_name else None,
    "version_code":int(version_code.group(1)) if version_code else None,
    "landing":"PLAYER_DROP_LANDED" in log,
    "movement_started":"AUTO_MOVE_STARTED" in log,
    "special":"HERO_SPECIAL_ACTIVATED" in log,
    "captures":log.count("TERRITORY_CAPTURE team=1"),
    "ignored_pairs":bool(re.search(r"ignoredPairs=6/6",log)),
    "performance_windows":[{"fps":float(f),"managed_mb":float(m),"gc_delta":g} for f,m,g in perf],
    "fatal_android_error":"FATAL EXCEPTION" in log,
}
(OUT/"report.json").write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding="utf-8")

thumbs=[]
for path in screens:
    with Image.open(path) as src:
        im=src.convert("RGB")
        im.thumbnail((260,590))
        thumbs.append((path.stem,im.copy()))
board=Image.new("RGB",(3*280,2*650),"#111625")
draw=ImageDraw.Draw(board)
for i,(name,im) in enumerate(thumbs):
    x=(i%3)*280+(280-im.width)//2
    y=(i//3)*650
    board.paste(im,(x,y))
    draw.text(((i%3)*280+8,y+596),name,fill="white")
board.save(OUT/"Realme_GameplayPolish_059.jpg",quality=88,optimize=True)
print("REALME_POLISH_QA",json.dumps(result,ensure_ascii=False),flush=True)
if result["build"]!="0.5.9" or result["version_code"]!=36:
    raise RuntimeError("Unexpected installed version")
if not result["landing"] or not result["movement_started"] or not result["special"]:
    raise RuntimeError("Core interaction missing")
if not result["ignored_pairs"] or result["fatal_android_error"]:
    raise RuntimeError("Collision or Android fatal regression")
