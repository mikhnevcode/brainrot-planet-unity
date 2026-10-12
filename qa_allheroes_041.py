from pathlib import Path
import subprocess,time,re,json
from PIL import Image,ImageDraw
root=Path(r"C:\Rabota\projects\BrainrotPlanetUnity\qa_allheroes_041")
root.mkdir(exist_ok=True)
adb=[r"C:\adb\adb.exe","-s","E6ZTMFVCEA895HAA"]
names={2:"Frigo",5:"Assassino",6:"Patapim",7:"TungSahur",8:"Lirili",9:"Frulli",10:"Boneca",11:"Trippi"}
def call(*args,timeout=60):
 p=subprocess.run(adb+list(args),capture_output=True,timeout=timeout)
 msg=(p.stdout+p.stderr).decode("utf-8","replace")
 if p.returncode: raise RuntimeError(" ".join(args)+" "+msg[-300:])
 return p.stdout
def screenshot(name):
 remote="/sdcard/v040_"+name+".png"
 call("shell","screencap","-p",remote)
 f=root/(name+".png");call("pull",remote,str(f));return f
def test(index,name):
 call("shell","am","force-stop","com.mikhnevcode.brainrotplanet")
 call("logcat","-c")
 call("shell","am","start","-n","com.mikhnevcode.brainrotplanet/com.unity3d.player.UnityPlayerGameActivity")
 time.sleep(7.5)
 for page in range(index//5):
  call("shell","input","tap","954","513")
  time.sleep(.35)
 y=round(2.571*(222+(index%5)*84+36))
 call("shell","input","tap","530",str(y))
 time.sleep(.25)
 selected_image=screenshot(name+"_selected")
 call("shell","input","tap","530","2060")
 time.sleep(4.4)
 call("shell","input","swipe","470","920","700","920","500")
 time.sleep(1.5)
 call("shell","input","tap","910","2045")
 time.sleep(.7)
 play=screenshot(name+"_playing")
 logs=call("logcat","-d","-s","Unity:I","AndroidRuntime:E","*:S").decode("utf-8","replace")
 (root/(name+"_log.txt")).write_text(logs,encoding="utf-8")
 rig=("NEW_HERO_RIG_LOADED name="+name+" controller=True") in logs
 selected=("HERO_MENU_SELECTED index="+str(index)) in logs
 run=(name+"_Armature|Run") in logs
 skill=("ADDITIONAL_HERO_SKILL index="+str(index)) in logs if index>=5 else "ФРИГО:" in logs
 fatal="FATAL EXCEPTION" in logs
 item={"index":index,"name":name,"menu_selected":selected,"rig_loaded":rig,"run_clip":run,"skill_event":skill,"fatal":fatal,"tap_y":y}
 print("QA_HERO",json.dumps(item),flush=True)
 return item,selected_image,play
call("shell","svc","power","stayon","usb")
call("shell","input","keyevent","KEYCODE_WAKEUP")
results=[];pairs=[]
for index,name in names.items():
 try:
  item,a,b=test(index,name);pairs.append((a,b))
 except Exception as exc:
  item={"index":index,"name":name,"error":str(exc)}
  print("QA_ERROR",name,str(exc),flush=True)
 results.append(item)
(root/"qa_report.json").write_text(json.dumps(results,indent=2,ensure_ascii=False),encoding="utf-8")
sheet=Image.new("RGB",(8*240,2*550),"#101827")
for i,(a,b) in enumerate(pairs):
 for row,f in enumerate((a,b)):
  im=Image.open(f).convert("RGB");im.thumbnail((222,505))
  sheet.paste(im,(i*240+(240-im.width)//2,row*550))
  ImageDraw.Draw(sheet).text((i*240+5,row*550+509),f.stem,fill="white")
sheet.save(root/"all_heroes_Realme_040.jpg",quality=83)
print("QA_ALL_HEROES_DONE",len(results),flush=True)