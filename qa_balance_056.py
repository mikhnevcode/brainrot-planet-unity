from pathlib import Path
from PIL import Image,ImageDraw
import subprocess,time,json,re,statistics
root=Path(r"C:\Rabota\projects\BrainrotPlanetUnity\qa_balance_056")
root.mkdir(exist_ok=True)
adb=[r"C:\adb\adb.exe","-s","E6ZTMFVCEA895HAA"]
levels=["Easy","Medium","Hard"]
def call(*args,timeout=70):
 p=subprocess.run(adb+list(args),capture_output=True,timeout=timeout)
 msg=(p.stdout+p.stderr).decode("utf-8","replace")
 if p.returncode: raise RuntimeError(str(args)+" "+msg[-350:])
 return msg
def pic(name):
 remote="/sdcard/balance056_"+name+".png"
 call("shell","screencap","-p",remote)
 file=root/(name+".png")
 call("pull",remote,str(file))
 return file
def run(level,name):
 call("shell","am","force-stop","com.mikhnevcode.brainrotplanet")
 call("logcat","-c")
 call("shell","am","start","-n","com.mikhnevcode.brainrotplanet/com.unity3d.player.UnityPlayerGameActivity")
 time.sleep(8)
 call("shell","input","tap",str([216,540,861][level]),"1885")
 time.sleep(.4)
 p0=pic(name+"_menu")
 call("shell","input","tap","530","2050")
 start=time.monotonic()
 shots=[p0]
 # Each difficulty receives the same player swipe sequence and cadence.
 # One extended 145-second match, not an acceleration or synthetic capture.
 turns=[(430,2040),(225,1835),(25,2040),(225,2240),(430,2040),
        (225,1835),(25,2040),(225,2240),(430,2040),
        (225,1835),(25,2040),(225,2240)]
 for i,(x,y) in enumerate(turns):
  due=6+i*11
  delay=due-(time.monotonic()-start)
  if delay>0:time.sleep(delay)
  # Never press the Results-screen Replay button if the player lost early.
  status=call("logcat","-d","-s","Unity:I","*:S")
  if "HERO_MATCH_VICTORY team=" in status:
   print("PLAYER_MATCH_ENDED_BEFORE_NEXT_INPUT",name,i,flush=True)
   break
  call("shell","input","swipe","225","2040",str(x),str(y),"510")
  if i%2==0:
   call("shell","input","tap","910","2045")
  if i in (2,5,8):
   shots.append(pic(name+"_t"+str(int(time.monotonic()-start))))
 # Wait until the normal match deadline + settling.
 delay=156-(time.monotonic()-start)
 if delay>0:time.sleep(delay)
 shots.append(pic(name+"_end"))
 raw=call("logcat","-d","-s","Unity:I","AndroidRuntime:E","*:S",timeout=95)
 (root/(name+"_log.txt")).write_text(raw,encoding="utf-8")
 result=[line for line in raw.splitlines() if "AI_BALANCE_RESULT level=" in line]
 encounters=[line for line in raw.splitlines() if "AI_ENCOUNTER_STARTED difficulty=" in line]
 fps=[float(x) for x in re.findall(r"PERF_SAMPLE stage=game fps=([0-9.]+)",raw)]
 captures=[sum("TERRITORY_CAPTURE team="+str(t)+" " in ln for ln in raw.splitlines())
           for t in range(1,5)]
 win=re.findall(r"HERO_MATCH_VICTORY team=(\d+)",raw)
 data={"level":name,"balance_report":result[-1] if result else None,
       "player_win":bool(win and win[-1]=="1"),"winner":win[-1] if win else None,
       "encounter_events":len(encounters),"capture_events_by_team":captures,
       "ability_by_team":{str(t):len(re.findall(r"HERO_SPECIAL_ACTIVATED hero=\d+ team="+str(t),raw)) for t in range(1,5)},
       "fps_mean":round(statistics.mean(fps[1:]),2) if len(fps)>1 else None,
       "fps_min_interval":min(fps[1:]) if len(fps)>1 else None,
       "fatal":"FATAL EXCEPTION" in raw}
 print("BALANCE_145S",json.dumps(data,ensure_ascii=False),flush=True)
 return data,shots

call("shell","svc","power","stayon","usb")
call("shell","input","keyevent","KEYCODE_WAKEUP")
reports=[];pics=[]
for i,name in enumerate(levels):
 try:
  record,images=run(i,name);pics.extend(images)
 except Exception as exc:
  record={"level":name,"error":str(exc)}
  print("BALANCE_QA_ERROR",name,str(exc),flush=True)
 reports.append(record)
 (root/"report.json").write_text(json.dumps(reports,ensure_ascii=False,indent=2),encoding="utf-8")
# Return test phone to medium as agreed in prior release.
try:
 call("shell","am","force-stop","com.mikhnevcode.brainrotplanet")
 call("shell","am","start","-n","com.mikhnevcode.brainrotplanet/com.unity3d.player.UnityPlayerGameActivity")
 time.sleep(7)
 call("shell","input","tap","540","1885")
 print("RESTORED_MEDIUM_DIFFICULTY",flush=True)
except Exception as e:
 print("RESTORE_FAILED",str(e),flush=True)
canvas=Image.new("RGB",(3*300,5*625),"#141b2b")
for idx,p in enumerate(pics):
 with Image.open(p) as im:
  im=im.convert("RGB");im.thumbnail((276,570))
 col=idx//5;row=idx%5
 canvas.paste(im,(col*300+(300-im.width)//2,row*625))
 ImageDraw.Draw(canvas).text((col*300+8,row*625+575),p.stem,fill="white")
canvas.save(root/"Balance_Realme_056.jpg",quality=86,optimize=True)
print("BALANCE_056_TEST_FINISHED",len(reports),flush=True)
