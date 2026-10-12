from pathlib import Path
import subprocess,time,json,re,sys
from PIL import Image,ImageDraw

root=Path(r"C:\Rabota\projects\BrainrotPlanetUnity\qa_hero_motion_050")
root.mkdir(exist_ok=True)
adb=[r"C:\adb\adb.exe","-s","E6ZTMFVCEA895HAA"]
heroes=["Tralalero","Bombardiro","Frigo","Bombombini","Ballerina","Assassino",
        "Patapim","TungSahur","Lirili","Frulli","Boneca","Trippi"]
def call(*args,timeout=50):
    p=subprocess.run(adb+list(args),capture_output=True,timeout=timeout)
    message=(p.stdout+p.stderr).decode("utf-8","replace")
    if p.returncode:raise RuntimeError(" ".join(args)+": "+message[-350:])
    return p.stdout
def snap(name):
    remote="/sdcard/qa050_"+name+".png"
    call("shell","screencap","-p",remote)
    path=root/(name+".png")
    call("pull",remote,str(path))
    return path
def test(hero):
    name=heroes[hero]
    call("shell","am","force-stop","com.mikhnevcode.brainrotplanet")
    call("logcat","-c")
    call("shell","am","start","-n","com.mikhnevcode.brainrotplanet/com.unity3d.player.UnityPlayerGameActivity")
    time.sleep(8)
    for _ in range(hero//5):
        call("shell","input","tap","939","500")
        time.sleep(.30)
    y=round((222+(hero%5)*84+36)*2.5714)
    call("shell","input","tap","530",str(y))
    time.sleep(.25)
    call("shell","input","tap","530","2050")
    time.sleep(4.3)
    first=snap("%02d_%s_idle"%(hero,name))
    call("shell","input","swipe","410","960","740","960","580")
    time.sleep(2.3)
    second=snap("%02d_%s_run"%(hero,name))
    call("shell","input","tap","915","2045")
    time.sleep(1.8)
    third=snap("%02d_%s_special"%(hero,name))
    logs=call("logcat","-d","-s","Unity:I","AndroidRuntime:E","*:S").decode("utf-8","replace")
    filtered=[line for line in logs.splitlines() if any(k in line for k in
        ("HERO_MOTION","HERO_SPECIAL","HERO_FX","BONECA_SPRING",
         "PLAYER_DROP","TRALALERO_ANIMATION","Error","Exception","ANR","FATAL"))]
    (root/(name+".txt")).write_text("\n".join(filtered),encoding="utf-8")
    result={
       "hero":hero,"name":name,
       "rig":any("HERO_MOTION_RIG hero="+str(hero) in l for l in filtered),
       "dropping":any("HERO_MOTION_DROP hero="+str(hero) in l for l in filtered),
       "landed":any("PLAYER_DROP_LANDED" in l for l in filtered),
       "running":any("HERO_MOTION_ACTIVE hero="+str(hero)+" state=run" in l for l in filtered),
       "ability":any("HERO_SPECIAL_ACTIVATED hero="+str(hero) in l for l in filtered),
       "animated_ability":any("HERO_MOTION_SKILL hero="+str(hero) in l for l in filtered),
       "vfx":any("HERO_FX_ACTIVATED hero="+str(hero) in l for l in filtered),
       "spring_land":any("BONECA_SPRING_LANDED" in l for l in filtered) if hero==10 else None,
       "fatal":any("FATAL EXCEPTION" in l for l in filtered)
    }
    print("HERO_QA",json.dumps(result,ensure_ascii=False),flush=True)
    return result,[first,second,third]
call("shell","svc","power","stayon","usb")
call("shell","input","keyevent","KEYCODE_WAKEUP")
results=[];screens=[]
for hero in range(12):
    try:
        outcome,pics=test(hero)
        screens.append(pics)
    except Exception as e:
        outcome={"hero":hero,"name":heroes[hero],"error":str(e)}
        print("HERO_TEST_ERROR",heroes[hero],str(e),flush=True)
    results.append(outcome)
    (root/"report.json").write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding="utf-8")
cols=4;rows=9;w=252;h=490
canvas=Image.new("RGB",(cols*w,rows*h),"#141b2b")
draw=ImageDraw.Draw(canvas)
for idx,pics in enumerate(screens):
    # Hero index in filename is retained if an earlier case failed.
    hero_id=int(pics[0].stem[:2]);col=hero_id%cols;row=hero_id//cols
    for sub,p in enumerate(pics):
        im=Image.open(p).convert("RGB")
        im.thumbnail((230,435))
        x=col*w+(w-im.width)//2
        y=(row*3+sub)*h
        canvas.paste(im,(x,y))
        draw.text((col*w+8,y+440),heroes[hero_id]+(" idle" if sub==0 else " run" if sub==1 else " special"),fill="white")
canvas.save(root/"All12_Realme_Motion_050.jpg",quality=85,optimize=True)
print("ALL_HERO_MOTION_QA_DONE",len(results),flush=True)
