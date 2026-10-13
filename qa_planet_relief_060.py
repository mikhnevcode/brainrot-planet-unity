from pathlib import Path
import json,re,statistics,subprocess,time
from PIL import Image,ImageDraw

PROJECT=Path(r"C:\Rabota\projects\BrainrotPlanetUnity")
OUT=PROJECT/"qa_planet_relief_060"
OUT.mkdir(exist_ok=True)
ADB=[r"C:\adb\adb.exe","-s","E6ZTMFVCEA895HAA"]
APK=PROJECT/r"Builds\Android\BrainrotPlanet-v0.6.0.apk"
PACKAGE="com.mikhnevcode.brainrotplanet"
ACTIVITY=PACKAGE+"/com.unity3d.player.UnityPlayerGameActivity"

def adb(*args,timeout=120):
    p=subprocess.run(ADB+list(args),capture_output=True,timeout=timeout)
    data=(p.stdout+p.stderr).decode("utf-8","replace")
    if p.returncode:
        raise RuntimeError("ADB %r failed: %s"%(args,data[-800:]))
    return data

def shot(name):
    remote="/sdcard/qa060_%s.png"%name
    adb("shell","screencap","-p",remote)
    local=OUT/(name+".png")
    adb("pull",remote,str(local))
    return local

def launch():
    adb("shell","am","force-stop",PACKAGE)
    adb("shell","input","keyevent","KEYCODE_HOME")
    adb("shell","input","keyevent","KEYCODE_WAKEUP")
    adb("shell","wm","dismiss-keyguard")
    time.sleep(1)
    adb("logcat","-c")
    adb("shell","am","start","-W","-n",ACTIVITY)
    time.sleep(8)

adb("install","-r",str(APK),timeout=300)
package=adb("shell","dumpsys","package",PACKAGE)

# Crater route: screen-down follows the local south tangent from the landing
# core and crosses the intentionally nearby standalone crater.
launch()
screens=[shot("menu")]
adb("shell","input","tap","530","2050")
time.sleep(1.25)
screens.append(shot("descent_relief_overview"))
time.sleep(3.7)
screens.append(shot("landed"))
initial_log=adb("logcat","-d","-s","Unity:I","AndroidRuntime:E","*:S")
adb("logcat","-c")
adb("shell","input","swipe","260","1750","260","2240","420")
time.sleep(.65)
screens.append(shot("crater_near_rim"))
time.sleep(.55)
screens.append(shot("crater_bowl"))
time.sleep(.70)
screens.append(shot("crater_far_rim"))
crater_log=adb("logcat","-d","-s","Unity:I","AndroidRuntime:E","*:S")

# Hill route: a fresh match and screen-up movement reaches the independent
# rounded hill and should cause the player-specific automatic bounce.
launch()
adb("shell","input","tap","530","2050")
time.sleep(4.8)
adb("logcat","-c")
adb("shell","input","swipe","260","2030","260","1300","450")
time.sleep(.70)
screens.append(shot("hill_approach"))
time.sleep(.55)
screens.append(shot("hill_bounce"))
time.sleep(.65)
screens.append(shot("hill_crest"))
hill_log=adb("logcat","-d","-s","Unity:I","AndroidRuntime:E","*:S")

all_log=initial_log+"\n--- CRATER ROUTE ---\n"+crater_log+"\n--- HILL ROUTE ---\n"+hill_log
(OUT/"unity_log.txt").write_text(all_log,encoding="utf-8")
version_name=re.search(r"versionName=([^\s]+)",package)
version_code=re.search(r"versionCode=(\d+)",package)
fps=[float(v) for v in re.findall(r"PERF_SAMPLE stage=game fps=([0-9.]+)",all_log)]
crater_radius=[float(v) for v in re.findall(r"actorR=([0-9.]+)",crater_log)]
result={
    "build":version_name.group(1) if version_name else None,
    "version_code":int(version_code.group(1)) if version_code else None,
    "phone":"Realme RMX3151",
    "relief_ready":"PLANET_RELIEF_READY vertices=25425 hills=7 craters=6 primitiveProps=0" in initial_log,
    "landing":"PLAYER_DROP_LANDED" in initial_log,
    "crater_route_player_bounces":crater_log.count("HILL_BOUNCE team=1"),
    "crater_route_radius_min":min(crater_radius) if crater_radius else None,
    "crater_route_radius_max":max(crater_radius) if crater_radius else None,
    "hill_player_bounces":hill_log.count("HILL_BOUNCE team=1"),
    "steady_fps_average":round(statistics.mean(fps),2) if fps else None,
    "steady_fps_min":min(fps) if fps else None,
    "fatal_android_error":"FATAL EXCEPTION" in all_log,
}
(OUT/"report.json").write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding="utf-8")

thumbs=[]
for path in screens:
    with Image.open(path) as src:
        im=src.convert("RGB")
        im.thumbnail((250,558))
        thumbs.append((path.stem,im.copy()))
cols=3
rows=(len(thumbs)+cols-1)//cols
board=Image.new("RGB",(cols*270,rows*615),"#101523")
draw=ImageDraw.Draw(board)
for i,(name,im) in enumerate(thumbs):
    x=(i%cols)*270+(270-im.width)//2
    y=(i//cols)*615
    board.paste(im,(x,y))
    draw.text(((i%cols)*270+8,y+562),name,fill="white")
board.save(OUT/"Realme_PlanetRelief_060.jpg",quality=89,optimize=True)

print("REALME_PLANET_RELIEF_QA",json.dumps(result,ensure_ascii=False),flush=True)
if result["build"]!="0.6.0" or result["version_code"]!=37:
    raise RuntimeError("Unexpected installed version")
if not result["relief_ready"] or not result["landing"]:
    raise RuntimeError("Relief or landing did not initialize")
if result["crater_route_player_bounces"]!=0:
    raise RuntimeError("The crater was treated as a hill bounce area")
if result["hill_player_bounces"]<1:
    raise RuntimeError("The player did not bounce on the rounded hill")
if result["fatal_android_error"]:
    raise RuntimeError("Android fatal error detected")
