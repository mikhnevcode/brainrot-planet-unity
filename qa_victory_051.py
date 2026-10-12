from pathlib import Path
import subprocess,time,json
from PIL import Image,ImageDraw
root=Path(r"C:\Rabota\projects\BrainrotPlanetUnity\qa_victory_051")
root.mkdir(exist_ok=True)
adb=[r"C:\adb\adb.exe","-s","E6ZTMFVCEA895HAA"]
def call(*args):
    p=subprocess.run(adb+list(args),capture_output=True,timeout=60)
    if p.returncode:
        raise RuntimeError(str(args)+" "+(p.stdout+p.stderr).decode("utf-8","replace")[-350:])
    return p.stdout
def snap(name):
    source="/sdcard/"+name+".png"
    call("shell","screencap","-p",source)
    dest=root/(name+".png")
    call("pull",source,str(dest))
    return dest
def test(hero,name):
    call("shell","am","force-stop","com.mikhnevcode.brainrotplanet")
    call("logcat","-c")
    call("shell","am","start","-n","com.mikhnevcode.brainrotplanet/com.unity3d.player.UnityPlayerGameActivity")
    time.sleep(8)
    y=round((222+hero*84+36)*2.5714)
    call("shell","input","tap","530",str(y))
    call("shell","input","tap","530","2050")
    time.sleep(4.7)
    photos=[snap(name+"_start")]
    for i,(x,y) in enumerate([(430,2040),(225,1835),(25,2040),(225,2240)]*3):
        call("shell","input","swipe","225","2040",str(x),str(y),"1100")
        time.sleep(.12)
        if i in (3,7,11):photos.append(snap(name+"_capture_"+str(i+1)))
    log=call("logcat","-d","-s","Unity:I","AndroidRuntime:E","*:S").decode("utf-8","replace")
    lines=[l for l in log.splitlines() if any(w in l for w in
        ("TERRITORY_CAPTURE team=1","HERO_MOTION_VICTORY hero="+str(hero),
         "HERO_MOTION_ACTIVE hero="+str(hero),"TRALALERO_ANIMATION","FATAL EXCEPTION"))]
    (root/(name+".txt")).write_text("\n".join(lines),encoding="utf-8")
    result={"hero":hero,"name":name,
        "capture_events":sum("TERRITORY_CAPTURE team=1" in s for s in lines),
        "victory_triggered":any("HERO_MOTION_VICTORY hero="+str(hero) in s for s in lines),
        "victory_state":any("HERO_MOTION_ACTIVE hero="+str(hero)+" state=victory" in s for s in lines),
        "fatal":any("FATAL EXCEPTION" in s for s in lines)}
    print("DANCE_QA",json.dumps(result,ensure_ascii=False),flush=True)
    return result,photos
data=[];shots=[]
for hero,name in [(0,"Tralalero"),(4,"Ballerina")]:
    result,photos=test(hero,name)
    data.append(result);shots.extend(photos)
(root/"report.json").write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding="utf-8")
canvas=Image.new("RGB",(4*260,2*590),"#151a2a")
for idx,p in enumerate(shots):
    with Image.open(p) as im:
        im=im.convert("RGB");im.thumbnail((240,535))
    x=idx%4*260+(260-im.width)//2
    y=idx//4*590
    canvas.paste(im,(x,y))
    ImageDraw.Draw(canvas).text((idx%4*260+10,y+543),p.stem,fill="white")
canvas.save(root/"Victory_Dances_Realme_051.jpg",quality=85,optimize=True)
print("VICTORY_DANCE_QA_DONE",flush=True)
