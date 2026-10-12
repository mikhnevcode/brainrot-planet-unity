from pathlib import Path
import subprocess,time,json,re
root=Path(r"C:\Rabota\projects\BrainrotPlanetUnity\qa_no_input_053")
root.mkdir(exist_ok=True)
a=[r"C:\adb\adb.exe","-s","E6ZTMFVCEA895HAA"]
def cmd(*args):
 p=subprocess.run(a+list(args),capture_output=True,timeout=60)
 text=(p.stdout+p.stderr).decode("utf-8","replace")
 if p.returncode:raise RuntimeError(str(args)+text[-400:])
 return p.stdout
cmd("shell","am","force-stop","com.mikhnevcode.brainrotplanet")
cmd("logcat","-c")
cmd("shell","am","start","-n","com.mikhnevcode.brainrotplanet/com.unity3d.player.UnityPlayerGameActivity")
time.sleep(7.5)
cmd("shell","input","tap","530","2050")
time.sleep(25)
data=cmd("logcat","-d","-s","Unity:I","AndroidRuntime:E","*:S").decode("utf-8","replace")
(root/"log.txt").write_text(data,encoding="utf-8")
counts={key:sum(key in line for line in data.splitlines()) for key in
 ["AUTO_MOVE_STARTED","AUTOMOVE actor=","HERO_SPECIAL_ACTIVATED hero=0","TERRITORY_CAPTURE team=1","PLAYER_DROP_LANDED"]}
counts["fatal"]="FATAL EXCEPTION" in data
(root/"report.json").write_text(json.dumps(counts,ensure_ascii=False,indent=2),encoding="utf-8")
print("PASSIVE_INPUT_REPORT",json.dumps(counts),flush=True)
