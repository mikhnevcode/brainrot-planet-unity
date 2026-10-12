from pathlib import Path
from PIL import Image,ImageDraw
import subprocess,time,json,re
root=Path(r"C:\Rabota\projects\BrainrotPlanetUnity\qa_drop_v043")
root.mkdir(exist_ok=True)
adb=[r"C:\adb\adb.exe","-s","E6ZTMFVCEA895HAA"]
def call(*args,timeout=60):
 p=subprocess.run(adb+list(args),capture_output=True,timeout=timeout)
 out=(p.stdout+p.stderr).decode("utf-8","replace")
 print("ADB",*args,"exit",p.returncode,out[-180:],flush=True)
 if p.returncode!=0:raise RuntimeError(out[-400:])
 return p.stdout
def snap(name):
 remote="/sdcard/drop043_"+name+".png"
 call("shell","screencap","-p",remote)
 path=root/(name+".png")
 call("pull",remote,str(path))
 return path
call("shell","svc","power","stayon","usb")
call("shell","input","keyevent","KEYCODE_WAKEUP")
call("shell","am","force-stop","com.mikhnevcode.brainrotplanet")
call("logcat","-c")
call("shell","am","start","-n","com.mikhnevcode.brainrotplanet/com.unity3d.player.UnityPlayerGameActivity")
time.sleep(7.5)
shots=[snap("menu")]
call("shell","input","tap","530","2050")
start=time.monotonic()
for name,when in [("drop_0p3",.30),("drop_0p9",.90),("drop_1p5",1.50),
                  ("drop_2p1",2.10),("drop_2p6",2.60),("landed_3p4",3.40)]:
 remain=when-(time.monotonic()-start)
 if remain>0:time.sleep(remain)
 shots.append(snap(name))
call("shell","input","swipe","400","900","700","900","500")
time.sleep(.8)
shots.append(snap("moving_after_landing"))
logs=call("logcat","-d","-s","Unity:I","AndroidRuntime:E","*:S").decode("utf-8","replace")
(root/"unity_android_log.txt").write_text(logs,encoding="utf-8")
matches=[line for line in logs.splitlines() if any(q in line for q in
  ("PLAYER_DROP","CAMQA","AUTOMOVE","TERRITORY_CAPTURE team=1","FATAL EXCEPTION"))]
(root/"qa_events.txt").write_text("\n".join(matches),encoding="utf-8")
progress=[]
for line in matches:
 m=re.search(r"PLAYER_DROP_PROGRESS t=(\d+\.\d+) height=(\d+\.\d+)",line)
 if m:progress.append([float(m.group(1)),float(m.group(2))])
sheet=Image.new("RGB",(4*250,2*555),"#101827")
for i,f in enumerate(shots):
 im=Image.open(f).convert("RGB");im.thumbnail((230,495))
 x=(i%4)*250+(250-im.width)//2
 y=(i//4)*555
 sheet.paste(im,(x,y))
 ImageDraw.Draw(sheet).text(((i%4)*250+5,y+500),f.stem,fill="white")
sheet.save(root/"Realme_DropSequence_043.jpg",quality=87,optimize=True)
report={"device":"Realme RMX3151","version":"0.4.3",
 "drop_started":any("PLAYER_DROP_BEGIN" in l for l in matches),
 "drop_landed":any("PLAYER_DROP_LANDED" in l for l in matches),
 "progress":progress,
 "auto_move_after_landing":any("AUTOMOVE" in l for l in matches),
 "fatal":any("FATAL EXCEPTION" in l for l in matches),
 "frame_count":len(shots)}
(root/"report.json").write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding="utf-8")
print("DROP_REALME_QA",json.dumps(report,ensure_ascii=False),flush=True)
