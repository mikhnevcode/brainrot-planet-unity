from pathlib import Path
import subprocess,time,json,re
from PIL import Image,ImageDraw
root=Path(r"C:\Rabota\projects\BrainrotPlanetUnity\qa_victory_all_052")
root.mkdir(exist_ok=True)
adb=[r"C:\adb\adb.exe","-s","E6ZTMFVCEA895HAA"]
heroes=["Tralalero","Bombardiro","Frigo","Bombombini","Ballerina","Assassino",
        "Patapim","TungSahur","Lirili","Frulli","Boneca","Trippi"]
def call(*args,timeout=65):
    r=subprocess.run(adb+list(args),capture_output=True,timeout=timeout)
    t=(r.stdout+r.stderr).decode("utf-8","replace")
    if r.returncode:raise RuntimeError(" ".join(args)+" "+t[-400:])
    return r.stdout
def snap(n):
    src="/sdcard/victory052_"+n+".png"
    call("shell","screencap","-p",src)
    out=root/(n+".png")
    call("pull",src,str(out))
    return out
def test(index):
    name=heroes[index]
    call("shell","am","force-stop","com.mikhnevcode.brainrotplanet")
    call("logcat","-c")
    call("shell","am","start","-n","com.mikhnevcode.brainrotplanet/com.unity3d.player.UnityPlayerGameActivity")
    time.sleep(7.5)
    for p in range(index//5):
        call("shell","input","tap","940","500")
        time.sleep(.25)
    y=round((222+(index%5)*84+36)*2.5714)
    call("shell","input","tap","530",str(y))
    time.sleep(.25)
    call("shell","input","tap","530","2050")
    time.sleep(4.4)
    photos=[]
    for k,(x,y) in enumerate([(430,2040),(225,1835),(25,2040),(225,2240)]*3):
        call("shell","input","swipe","225","2040",str(x),str(y),"1100")
        time.sleep(.13)
        if k in (3,11):photos.append(snap(name+"_capture_"+str(k+1)))
    log=call("logcat","-d","-s","Unity:I","AndroidRuntime:E","*:S").decode("utf-8","replace")
    lines=[l for l in log.splitlines() if any(x in l for x in
      ("HERO_MOTION_VICTORY hero=","TRALALERO_ANIMATION clip=",
       "TERRITORY_CAPTURE team=1","PERF_SAMPLE stage=game","FATAL EXCEPTION"))]
    (root/(name+".txt")).write_text("\n".join(lines),encoding="utf-8")
    state=[l for l in lines if "TRALALERO_ANIMATION clip=" in l]
    found=any("|VictoryShow" in s for s in state)
    capture=sum("TERRITORY_CAPTURE team=1" in s for s in lines)
    result={"hero":index,"name":name,"captures":capture,
      "victory_trigger":any("HERO_MOTION_VICTORY hero="+str(index) in l for l in lines),
      "blender_victory_clip":found,
      "fatal":any("FATAL EXCEPTION" in l for l in lines)}
    print("VICTORY_CLIP_QA",json.dumps(result,ensure_ascii=False),flush=True)
    return result,photos
call("shell","svc","power","stayon","usb")
results=[];allpics=[]
for hero in range(12):
 try:
    result,photos=test(hero)
    allpics.extend(photos)
 except Exception as e:
    result={"hero":hero,"name":heroes[hero],"error":str(e)}
    print("VICTORY_QA_ERROR",hero,str(e),flush=True)
 results.append(result)
 (root/"report.json").write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding="utf-8")
sheet=Image.new("RGB",(6*240,4*555),"#0e1627")
for idx,p in enumerate(allpics):
 with Image.open(p) as image:
    image=image.convert("RGB");image.thumbnail((216,501))
 x=(idx%6)*240+(240-image.width)//2
 y=(idx//6)*555
 sheet.paste(image,(x,y))
 ImageDraw.Draw(sheet).text(((idx%6)*240+7,y+508),p.stem,fill="white")
sheet.save(root/"Victory12_Realme_052.jpg",quality=85,optimize=True)
print("VICTORY_12_QA_COMPLETE",len(results),flush=True)
