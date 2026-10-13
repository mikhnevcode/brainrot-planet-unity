from pathlib import Path
import subprocess,time,re,json,statistics,math
from PIL import Image,ImageDraw
root=Path(r"C:\Rabota\projects\BrainrotPlanetUnity\qa_scale_collision_059")
root.mkdir(exist_ok=True)
a=[r"C:\adb\adb.exe","-s","E6ZTMFVCEA895HAA"]
def cmd(*args):
 p=subprocess.run(a+list(args),capture_output=True,timeout=70)
 out=(p.stdout+p.stderr).decode("utf-8","replace")
 if p.returncode:raise RuntimeError("ADB "+str(args)+" "+out[-320:])
 return p.stdout
def shot(name):
 path="/sdcard/qa059_"+name+".png"
 cmd("shell","screencap","-p",path)
 dst=root/(name+".png")
 cmd("pull",path,str(dst))
 return dst
cmd("shell","svc","power","stayon","usb")
cmd("shell","input","keyevent","KEYCODE_WAKEUP")
cmd("shell","am","force-stop","com.mikhnevcode.brainrotplanet")
cmd("logcat","-c")
cmd("shell","am","start","-n","com.mikhnevcode.brainrotplanet/com.unity3d.player.UnityPlayerGameActivity")
time.sleep(8)
shots=[shot("menu")]
cmd("shell","input","tap","530","2050")
time.sleep(4.7)
shots.append(shot("first_landing"))
# Strong repeatable closed-loop path; movement uses no privileged API.
waypoints=[(430,2040),(225,1835),(25,2040),(225,2240)]*5
for i,(x,y) in enumerate(waypoints):
 cmd("shell","input","swipe","225","2040",str(x),str(y),"1100")
 time.sleep(.10)
 if i in (3,11,19):
  shots.append(shot("loop_"+str(i+1)))
time.sleep(3)
shots.append(shot("after_movement"))
log=cmd("logcat","-d","-s","Unity:I","AndroidRuntime:E","*:S").decode("utf-8","replace")
(root/"unity_log.txt").write_text(log,encoding="utf-8")
ls=log.splitlines()
m=re.findall(r"CAMQA stage=ground actorR=([0-9.]+)",log)
radius=[float(x) for x in m]
fps=[float(x) for x in re.findall(r"PERF_SAMPLE stage=game fps=([0-9.]+)",log)]
pairs=re.findall(r"HERO_BODY_PASS_THROUGH reason=([a-z_]+) ignoredPairs=(\d+)/(\d+)",log)
caps=sum("TERRITORY_CAPTURE team=1" in x for x in ls)
seams=sum("TERRITORY_SEAM_REPAIR team=1" in x for x in ls)
result={
"build":"0.5.9","phone":"Realme RMX3151",
"hero_ignores_physical_other_heroes": bool(pairs) and all(int(p[1])==6 and int(p[2])==6 for p in pairs),
"pair_details":pairs,
"ground_actor_radius_samples":radius[:20],
"ground_actor_radius_mean":round(statistics.mean(radius),3) if radius else None,
"larger_planet_confirmed":bool(radius) and min(radius)>24,
"landing":("PLAYER_DROP_LANDED" in log),
"player_capture_count":caps,"territory_seam_repair_count":seams,
"player_movement":("AUTO_MOVE_STARTED" in log),
"fps_average":round(statistics.mean(fps),2) if fps else None,
"fps_samples":len(fps),
"fatal_android_error":"FATAL EXCEPTION" in log
}
(root/"report.json").write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding="utf-8")
board=Image.new("RGB",(3*265,2*640),"#141929")
for k,imfile in enumerate(shots):
 with Image.open(imfile) as img:
  pic=img.convert("RGB");pic.thumbnail((250,585))
 x=(k%3)*265+(265-pic.width)//2
 y=(k//3)*640
 board.paste(pic,(x,y))
 ImageDraw.Draw(board).text(((k%3)*265+8,y+590),imfile.stem,fill="white")
board.save(root/"Realme_PlanetScale_Collision_059.jpg",quality=87,optimize=True)
print("REALME_SCALE_COLLISION_QA",json.dumps(result,ensure_ascii=False),flush=True)
if not result["hero_ignores_physical_other_heroes"] or not result["larger_planet_confirmed"] or result["fatal_android_error"]:
 raise RuntimeError("The enlarged planet or actor contact rules did not pass on Android")
