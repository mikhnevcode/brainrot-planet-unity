from pathlib import Path
import subprocess,time,json,re,statistics
from PIL import Image,ImageDraw
root=Path(r"C:\Rabota\projects\BrainrotPlanetUnity\qa_difficulty_055")
root.mkdir(exist_ok=True)
adb=[r"C:\adb\adb.exe","-s","E6ZTMFVCEA895HAA"]
names=["Easy","Medium","Hard"]
def call(*args):
 p=subprocess.run(adb+list(args),capture_output=True,timeout=85)
 out=(p.stdout+p.stderr).decode("utf-8","replace")
 if p.returncode:raise RuntimeError("ADB "+" ".join(args)+" "+out[-450:])
 return p.stdout
def shot(n):
 src="/sdcard/difficulty055_"+n+".png"
 call("shell","screencap","-p",src)
 target=root/(n+".png")
 call("pull",src,str(target))
 return target
results=[];screen=[]
call("shell","svc","power","stayon","usb")
call("shell","input","keyevent","KEYCODE_WAKEUP")
for i,name in enumerate(names):
 call("shell","am","force-stop","com.mikhnevcode.brainrotplanet")
 call("logcat","-c")
 call("shell","am","start","-n","com.mikhnevcode.brainrotplanet/com.unity3d.player.UnityPlayerGameActivity")
 time.sleep(8)
 # 1080 px width / 420 logical = 2.571; selector near logicalH-226
 call("shell","input","tap",str([216,540,861][i]),"1885")
 time.sleep(.75)
 screen.append(shot(name+"_selection"))
 call("shell","input","tap","530","2050")
 time.sleep(5)
 screen.append(shot(name+"_game"))
 time.sleep(39)
 screen.append(shot(name+"_after_45s"))
 raw=call("logcat","-d","-s","Unity:I","AndroidRuntime:E","*:S").decode("utf-8","replace")
 (root/(name+"_log.txt")).write_text(raw,encoding="utf-8")
 lines=raw.splitlines()
 decision=[l for l in lines if "AI_DECISION team=" in l]
 config=[l for l in lines if "AI_DIFFICULTY_CONFIG team=" in l]
 mode=[l for l in lines if "AI_TACTIC_CHANGE team=" in l]
 abilities=[l for l in lines if "AI_SKILL_USED team=" in l]
 fps=list(map(float,re.findall(r"PERF_SAMPLE stage=game fps=([0-9.]+)",raw)))
 res={"level":name,"level_selected":name in raw and ("AI_DIFFICULTY_SELECTED level="+name in raw),
     "match_level":("AI_DIFFICULTY_MATCH level="+name) in raw,
     "bot_config_count":sum(("level="+name) in l for l in config),
     "bot_decisions":len(decision),
     "tactical_changes":len(mode),
     "intercept_targets":raw.count("AI_TRAIL_INTERCEPT team="),
     "ability_uses":len(abilities),
     "bot_captures":sum("TERRITORY_CAPTURE team="+str(t)+" " in l for l in lines for t in (2,3,4)),
     "player_move_events":raw.count("AUTO_MOVE_STARTED"),
     "game_fps_average":round(statistics.mean(fps[1:]),2) if len(fps)>1 else None,
     "game_fps_samples":len(fps),
     "fatal_android_error":"FATAL EXCEPTION" in raw}
 print("DIFFICULTY_QA",json.dumps(res,ensure_ascii=False),flush=True)
 results.append(res)
 (root/"report.json").write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding="utf-8")
sheet=Image.new("RGB",(3*310,3*625),"#142035")
for idx,f in enumerate(screen):
 im=Image.open(f).convert("RGB");im.thumbnail((290,575))
 x=(idx//3)*310+(310-im.width)//2
 y=(idx%3)*625
 sheet.paste(im,(x,y))
 ImageDraw.Draw(sheet).text(((idx//3)*310+12,y+580),f.stem,fill="white")
sheet.save(root/"Realme_Difficulties_055.jpg",quality=85,optimize=True)
print("ALL_DIFFICULTIES_QA_COMPLETE",len(results),flush=True)
