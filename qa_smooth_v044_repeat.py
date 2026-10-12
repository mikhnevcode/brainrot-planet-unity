from pathlib import Path
import subprocess,time,json,re
from PIL import Image,ImageDraw
root=Path(r"C:\Rabota\projects\BrainrotPlanetUnity\qa_smooth_v044_repeat")
root.mkdir(exist_ok=True)
adb=[r"C:\adb\adb.exe","-s","E6ZTMFVCEA895HAA"]
def cmd(*args,timeout=40):
 p=subprocess.run(adb+list(args),capture_output=True,timeout=timeout)
 message=(p.stdout+p.stderr).decode("utf-8","replace")
 print("ADB", " ".join(args), "exit",p.returncode,message[-125:],flush=True)
 if p.returncode:raise RuntimeError(message[-400:])
 return p.stdout
def snap(name):
 onphone="/sdcard/brainrot044_"+name+".png"
 cmd("shell","screencap","-p",onphone)
 dst=root/(name+".png")
 cmd("pull",onphone,str(dst))
 return dst
cmd("shell","svc","power","stayon","usb")
cmd("shell","input","keyevent","KEYCODE_WAKEUP")
cmd("shell","am","force-stop","com.mikhnevcode.brainrotplanet")
cmd("logcat","-c")
cmd("shell","am","start","-n","com.mikhnevcode.brainrotplanet/com.unity3d.player.UnityPlayerGameActivity")
time.sleep(8)
shots=[snap("menu")]
cmd("shell","input","tap","520","2050")
time.sleep(5)
shots.append(snap("before_loop"))
directions=[(430,2040),(225,1835),(25,2040),(225,2240)]*3
for i,(x,y) in enumerate(directions):
 cmd("shell","input","swipe","225","2040",str(x),str(y),"1100")
 time.sleep(.12)
 if i in (3,7,11):shots.append(snap("loop_%02d"%(i+1)))
time.sleep(.6)
shots.append(snap("after_loop"))
raw=cmd("logcat","-d","-s","Unity:I","AndroidRuntime:E","*:S",timeout=60).decode("utf-8","replace")
(root/"unity_log.txt").write_text(raw,encoding="utf-8")
events=[l for l in raw.splitlines() if any(x in l for x in
  ("TERRITORY_CAPTURE team=1","TERRITORY_SEAM_REPAIR team=1","AUTOMOVE","TRALALERO_ANIMATION","PAINT_VISUAL_UPDATE","FATAL EXCEPTION"))]
(root/"capture_events.txt").write_text("\n".join(events),encoding="utf-8")
capture=[l for l in events if "TERRITORY_CAPTURE team=1" in l]
seams=[l for l in events if "TERRITORY_SEAM_REPAIR team=1" in l]
paint_ms=[float(m.group(1)) for l in events if (m:=re.search(r"PAINT_VISUAL_UPDATE resolution=512x256 ms=([0-9.]+)",l))]
sheet=Image.new("RGB",(840,3*625),"#101827")
for i,f in enumerate(shots):
 im=Image.open(f).convert("RGB");im.thumbnail((255,550))
 x=(i%3)*280+(280-im.width)//2;y=(i//3)*625
 sheet.paste(im,(x,y))
 ImageDraw.Draw(sheet).text(((i%3)*280+10,y+554),f.stem,fill="white")
sheet.save(root/"Realme_Smooth_Territory_044.jpg",quality=88)
report={"version":"0.4.4","player_capture_events":len(capture),"player_seam_repair_events":len(seams),
        "capture_log":capture,"seam_log":seams,"automatic_movement":any("AUTOMOVE" in e for e in events),
        "fatal":any("FATAL EXCEPTION" in e for e in events),
        "visual_texture_512x256":len(paint_ms)>0,
        "visual_texture_updates":len(paint_ms),
        "visual_update_ms":paint_ms,
        "mean_visual_update_ms":round(sum(paint_ms)/len(paint_ms),2) if paint_ms else None,
        "max_visual_update_ms":max(paint_ms) if paint_ms else None}
(root/"report.json").write_text(json.dumps(report,indent=2,ensure_ascii=False),encoding="utf-8")
print("REALME_TERRITORY_RESULT",json.dumps(report,ensure_ascii=False),flush=True)