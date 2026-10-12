from pathlib import Path
import subprocess,time,re,json,statistics
from PIL import Image,ImageDraw
root=Path(r"C:\Rabota\projects\BrainrotPlanetUnity\qa_balance_closed_056")
root.mkdir(parents=True,exist_ok=True)
a=[r"C:\adb\adb.exe","-s","E6ZTMFVCEA895HAA"]
names=["Easy","Medium","Hard"]
def run(*args,timeout=80):
 p=subprocess.run(a+list(args),capture_output=True,timeout=timeout)
 text=(p.stdout+p.stderr).decode("utf-8","replace")
 if p.returncode:raise RuntimeError(" ".join(args)+" "+text[-350:])
 return text
def photo(name):
 src="/sdcard/bal056loop_"+name+".png"
 run("shell","screencap","-p",src)
 dest=root/(name+".png")
 run("pull",src,str(dest))
 return dest
frames=[];scores=[]
for idx,level in enumerate(names):
 run("shell","am","force-stop","com.mikhnevcode.brainrotplanet")
 run("logcat","-c")
 run("shell","am","start","-n","com.mikhnevcode.brainrotplanet/com.unity3d.player.UnityPlayerGameActivity")
 time.sleep(8)
 run("shell","input","tap",str([216,540,861][idx]),"1885")
 time.sleep(.4)
 frames.append(photo(level+"_menu"))
 run("shell","input","tap","530","2050")
 start=time.monotonic()
 controls=[(720,1100),(500,880),(280,1100),(500,1320)]
 step=0;ended=False
 # Human-style four-way closed loops, repeated over one real 145-second game.
 while time.monotonic()-start<138:
  x,y=controls[step%4]
  run("shell","input","swipe","500","1100",str(x),str(y),"650")
  step+=1
  if step%8==0:
   check=run("logcat","-d","-s","Unity:I","*:S")
   if "HERO_MATCH_VICTORY team=" in check:
    ended=True
    print("PLAYER_ROUTE_MATCH_END",level,
          round(time.monotonic()-start,1),flush=True)
    break
   run("shell","input","tap","910","2045")
  if step%35==0:frames.append(photo(level+"_at_"+str(step)+"_moves"))
  time.sleep(.10)
 until=152-(time.monotonic()-start)
 if until>0:time.sleep(until)
 frames.append(photo(level+"_end"))
 raw=run("logcat","-d","-s","Unity:I","AndroidRuntime:E","*:S",timeout=95)
 (root/(level+"_log.txt")).write_text(raw,encoding="utf-8")
 balance=[x for x in raw.splitlines() if "AI_BALANCE_RESULT level=" in x]
 wins=re.findall(r"HERO_MATCH_VICTORY team=(\d+)",raw)
 fps=list(map(float,re.findall(r"PERF_SAMPLE stage=game fps=([0-9.]+)",raw)))
 record={"level":level,"control_swipes":step,"early_end":ended,
  "winner":wins[0] if wins else None,"player_won":bool(wins and wins[0]=="1"),
  "balance_result":balance[0] if balance else None,
  "player_captures":len(re.findall("TERRITORY_CAPTURE team=1 ",raw)),
  "player_trail_hits":len(re.findall("PLAYER_RESPAWN_RECOVERY level=",raw)),
  "encounter_starts":len(re.findall("AI_ENCOUNTER_STARTED difficulty=",raw)),
  "avg_fps":round(statistics.mean(fps[1:]),2) if len(fps)>1 else None,
  "fatal":"FATAL EXCEPTION" in raw}
 scores.append(record)
 (root/"report.json").write_text(json.dumps(scores,ensure_ascii=False,indent=2),encoding="utf-8")
 print("CLOSED_LOOP_145S",json.dumps(record,ensure_ascii=False),flush=True)
# Restore regular mode and leave menu ready for user.
run("shell","am","force-stop","com.mikhnevcode.brainrotplanet")
run("shell","am","start","-n","com.mikhnevcode.brainrotplanet/com.unity3d.player.UnityPlayerGameActivity")
time.sleep(7)
run("shell","input","tap","540","1885")
canvas=Image.new("RGB",(3*305,3*610),"#131b2c")
for f in frames:
 k=names.index(f.stem.split("_")[0])
 row=0 if f.stem.endswith("_menu") else 2 if f.stem.endswith("_end") else 1
 with Image.open(f) as im:im=im.convert("RGB");im.thumbnail((280,548))
 x=k*305+(305-im.width)//2;y=row*610
 canvas.paste(im,(x,y))
 ImageDraw.Draw(canvas).text((k*305+6,y+555),f.stem,fill="white")
canvas.save(root/"Balance_ClosedLoops_Realme_056.jpg",quality=85,optimize=True)
print("BALANCE_CLOSED_LOOP_COMPLETE",len(scores),flush=True)
