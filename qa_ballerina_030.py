from pathlib import Path
import subprocess,time
from PIL import Image,ImageDraw
root=Path(r"C:\Rabota\projects\BrainrotPlanetUnity\qa_ballerina_030")
root.mkdir(exist_ok=True)
adb=[r"C:\adb\adb.exe","-s","E6ZTMFVCEA895HAA"]
def call(*args,timeout=60):
 p=subprocess.run(adb+list(args),capture_output=True,timeout=timeout)
 t=(p.stdout+p.stderr).decode("utf-8","replace")
 print("ADB",*args,"rc",p.returncode,t[-160:],flush=True)
 if p.returncode:raise RuntimeError("ADB: "+t[-400:])
 return p.stdout
def shot(name):
 rp="/sdcard/qa030_"+name+".png"
 call("shell","screencap","-p",rp)
 dst=root/(name+".png")
 call("pull",rp,str(dst))
 return dst
call("shell","svc","power","stayon","usb")
call("shell","input","keyevent","KEYCODE_WAKEUP")
call("shell","am","force-stop","com.mikhnevcode.brainrotplanet")
call("logcat","-c")
call("shell","am","start","-n","com.mikhnevcode.brainrotplanet/com.unity3d.player.UnityPlayerGameActivity")
time.sleep(7)
shots=[shot("menu")]
call("shell","input","tap","540","1510")
time.sleep(.6)
shots.append(shot("selected_ballerina"))
call("shell","input","tap","540","2060")
time.sleep(4.5)
shots.append(shot("landing_idle"))
call("shell","input","swipe","470","850","770","850","500")
time.sleep(1)
shots.append(shot("run_swipe"))
time.sleep(1.8)
shots.append(shot("continues_after_release"))
call("shell","input","tap","910","2250")
time.sleep(.5)
shots.append(shot("jump"))
time.sleep(1.5)
call("shell","input","tap","910","2050")
time.sleep(.5)
shots.append(shot("coffee_whirl"))
time.sleep(1.8)
logs=call("logcat","-d","-s","Unity:I","AndroidRuntime:E","*:S").decode("utf-8","replace")
(root/"unity_android_log.txt").write_text(logs,encoding="utf-8")
key=[l for l in logs.splitlines() if any(x in l for x in ("BALLERINA","ANIMATION","AUTOMOVE","TERRITORY_CAPTURE","Exception","AndroidRuntime"))]
(root/"qa_summary.txt").write_text("\n".join(key),encoding="utf-8")
for l in key[-55:]:print("QA",l,flush=True)
tile=Image.new("RGB",(900,1950),"#151a2d")
for i,f in enumerate(shots):
 im=Image.open(f).convert("RGB");im.thumbnail((280,890))
 x=(i%3)*300+(300-im.width)//2
 y=(i//3)*650
 im.thumbnail((260,570));tile.paste(im,(x,y))
 ImageDraw.Draw(tile).text(((i%3)*300+10,y+580),f.stem,fill="white")
tile.save(root/"Ballerina_Realme_QA.jpg",quality=88)
print("BALLERINA_ANDROID_QA_DONE",flush=True)
