from pathlib import Path
import subprocess,time,json,re,statistics
from PIL import Image,ImageDraw
root=Path(r"C:\Rabota\projects\BrainrotPlanetUnity\qa_ai_tactics_054_final")
root.mkdir(parents=True,exist_ok=True)
adb=[r"C:\adb\adb.exe","-s","E6ZTMFVCEA895HAA"]
def call(*args):
 p=subprocess.run(adb+list(args),capture_output=True,timeout=75)
 data=(p.stdout+p.stderr).decode("utf-8","replace")
 if p.returncode:raise RuntimeError(" ".join(args)+" "+data[-300:])
 return p.stdout
def snap(name):
 remote="/sdcard/brainrot054final_"+name+".png"
 call("shell","screencap","-p",remote)
 target=root/(name+".png")
 call("pull",remote,str(target))
 return target

call("shell","svc","power","stayon","usb")
call("shell","input","keyevent","KEYCODE_WAKEUP")
call("shell","am","force-stop","com.mikhnevcode.brainrotplanet")
call("logcat","-c")
call("shell","am","start","-n","com.mikhnevcode.brainrotplanet/com.unity3d.player.UnityPlayerGameActivity")
time.sleep(7.5)
photos=[snap("menu")]
call("shell","input","tap","530","2050")
start=time.monotonic()
for name,at in [("landing",1.7),("rivals_12s",12),("rivals_35s",35),
                ("rivals_55s",55),("end_68s",68)]:
 remaining=at-(time.monotonic()-start)
 if remaining>0:time.sleep(remaining)
 photos.append(snap(name))
 print("AI_QA_STAGE",name,"elapsed",round(time.monotonic()-start,2),flush=True)

raw=call("logcat","-d","-s","Unity:I","AndroidRuntime:E","*:S").decode("utf-8","replace")
(root/"unity_log.txt").write_text(raw,encoding="utf-8")
lines=raw.splitlines()
decisions=[l for l in lines if "AI_DECISION team=" in l]
changes=[l for l in lines if "AI_TACTIC_CHANGE team=" in l]
hunts=[l for l in lines if "AI_TRAIL_INTERCEPT team=" in l]
skills=[l for l in lines if "AI_SKILL_USED team=" in l]
ability=[l for l in lines if "HERO_SPECIAL_ACTIVATED hero=" in l]
fps=[float(n) for n in re.findall(r"PERF_SAMPLE stage=game fps=([0-9.]+)",raw)]
modes={mode:sum("mode="+mode in l or "-> "+mode in l for l in (changes+decisions))
    for mode in ('Expand','Retreat','Defend','HuntTrail')}
report={"version":"0.5.4","phone":"Realme RMX3151",
    "no_player_input_expected":True,
    "validation":"Final AI tactical transition adjustment",
    "team_decision_counts":{str(t):sum("AI_DECISION team="+str(t) in l for l in decisions) for t in (2,3,4)},
    "ai_mode_counts":modes,
    "tactic_change_events":len(changes),
    "intercepts":len(hunts),
    "ai_ability_events":len(skills),
    "ability_team_distribution":{str(t):sum(" team="+str(t) in l for l in ability) for t in (1,2,3,4)},
    "ai_captures":{str(t):sum("TERRITORY_CAPTURE team="+str(t)+" " in l for l in lines) for t in (2,3,4)},
    "player_move_events":sum("AUTO_MOVE_STARTED" in l for l in lines),
    "player_skill_events":sum("HERO_SPECIAL_ACTIVATED hero=0 team=1" in l for l in lines),
    "match_victory_events":re.findall(r"HERO_MATCH_VICTORY team=(\d+)",raw),
    "fps_samples":len(fps),
    "steady_fps_average":round(statistics.mean(fps[1:]),2) if len(fps)>1 else None,
    "steady_fps_min":min(fps[1:]) if len(fps)>1 else None,
    "fatal_android_error":"FATAL EXCEPTION" in raw}
(root/"report.json").write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding="utf-8")
(root/"events.txt").write_text("\n".join(changes+hunts+skills),encoding="utf-8")
sheet=Image.new("RGB",(4*270,2*580),"#151a2b")
for i,file in enumerate(photos):
    with Image.open(file) as im:
        im=im.convert("RGB");im.thumbnail((255,523))
    x=i%4*270+(270-im.width)//2
    y=i//4*580
    sheet.paste(im,(x,y))
    ImageDraw.Draw(sheet).text((i%4*270+8,y+530),file.stem,fill="white")
sheet.save(root/"AI_Tactics_Realme_054.jpg",quality=87,optimize=True)
print("AI_TACTICS_REALME_RESULT",json.dumps(report,ensure_ascii=False),flush=True)