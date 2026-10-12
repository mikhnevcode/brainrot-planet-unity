from pathlib import Path
import subprocess,time,json,re,statistics
from PIL import Image,ImageDraw

root=Path(r"C:\Rabota\projects\BrainrotPlanetUnity\qa_full_match_053")
root.mkdir(parents=True,exist_ok=True)
adb=[r"C:\adb\adb.exe","-s","E6ZTMFVCEA895HAA"]
def call(*args,timeout=75):
    p=subprocess.run(adb+list(args),capture_output=True,timeout=timeout)
    out=(p.stdout+p.stderr).decode("utf-8","replace")
    print("ADB",*args,"rc",p.returncode,out[-170:],flush=True)
    if p.returncode:raise RuntimeError(" ".join(args)+": "+out[-300:])
    return p.stdout
def snap(name):
    remote="/sdcard/brainrot_053_"+name+".png"
    call("shell","screencap","-p",remote)
    dest=root/(name+".png")
    call("pull",remote,str(dest))
    return dest
call("shell","svc","power","stayon","usb")
call("shell","input","keyevent","KEYCODE_WAKEUP")
call("shell","am","force-stop","com.mikhnevcode.brainrotplanet")
call("logcat","-c")
call("shell","am","start","-n","com.mikhnevcode.brainrotplanet/com.unity3d.player.UnityPlayerGameActivity")
time.sleep(7.2)
startshot=snap("menu")
call("shell","input","tap","520","2050")
begin=time.monotonic()
shots=[startshot]
# No artificial shortcuts or developer time acceleration: let a full 145s match
# complete on the real phone while three AI teams play.
for label,at in [("landing",1.4),("playing_8s",8),("playing_40s",40),
                 ("playing_80s",80),("playing_120s",120),
                 ("end_154s",154),("end_160s",160)]:
    remaining=at-(time.monotonic()-begin)
    if remaining>0:time.sleep(remaining)
    photos=snap(label)
    shots.append(photos)
    print("QA_STAGE",label,"elapsed",round(time.monotonic()-begin,2),flush=True)
    if label.startswith("end"):
        log=call("logcat","-d","-s","Unity:I","AndroidRuntime:E","*:S").decode("utf-8","replace")
        (root/(label+"_log.txt")).write_text(log,encoding="utf-8")
# Test the "play again" button without reinstalling: new descent should start.
call("shell","input","tap","530","2000")
time.sleep(1.2)
shots.append(snap("play_again_drop"))
time.sleep(3.1)
shots.append(snap("play_again_game"))
log=call("logcat","-d","-s","Unity:I","AndroidRuntime:E","*:S").decode("utf-8","replace")
(root/"unity_log.txt").write_text(log,encoding="utf-8")
winners=re.findall(r"HERO_MATCH_VICTORY team=(\d+) cameraFocus=(\w+)",log)
replays=re.findall(r"HERO_MATCH_VICTORY_REPLAY team=(\d+)",log)
camera=re.findall(r"CAM_VICTORY_FOCUS_STARTED teamActor=([^\r\n]+)",log)
landings=re.findall(r"PLAYER_DROP_LANDED altitude=0.00 character=(\d+)",log)
begins=re.findall(r"PLAYER_DROP_BEGIN altitude=9.00 duration=2.90 character=(\d+)",log)
fps=list(map(float,re.findall(r"PERF_SAMPLE stage=game fps=([0-9.]+)",log)))
endfps=list(map(float,re.findall(r"PERF_SAMPLE stage=end fps=([0-9.]+)",log)))
report={
 "build":"0.5.3",
 "device":"Realme RMX3151",
 "full_match_without_time_skips":True,
 "match_victory_events":winners,
 "camera_focus_events":camera,
 "victory_replays":replays,
 "drop_begin_count":len(begins),
 "drop_landing_count":len(landings),
 "start_game_again":len(begins)>=2,
 "playing_fps_samples":len(fps),
 "playing_fps_mean":round(statistics.mean(fps),2) if fps else None,
 "playing_fps_min":min(fps) if fps else None,
 "end_fps_samples":len(endfps),
 "end_fps_mean":round(statistics.mean(endfps),2) if endfps else None,
 "android_fatal":"FATAL EXCEPTION" in log
}
(root/"report.json").write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding="utf-8")
sheet=Image.new("RGB",(4*310,3*655),"#131928")
for i,f in enumerate(shots):
    im=Image.open(f).convert("RGB")
    im.thumbnail((286,583))
    x=(i%4)*310+(310-im.width)//2
    y=(i//4)*655
    sheet.paste(im,(x,y))
    ImageDraw.Draw(sheet).text((i%4*310+12,y+591),f.stem,fill="white")
sheet.save(root/"FullMatch_Realme_053.jpg",quality=87,optimize=True)
print("REALME_FULL_MATCH_QA",json.dumps(report,ensure_ascii=False),flush=True)
