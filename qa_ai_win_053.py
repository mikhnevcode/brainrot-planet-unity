from pathlib import Path
import subprocess,time,json,re,statistics
from PIL import Image
root=Path(r"C:\Rabota\projects\BrainrotPlanetUnity\qa_ai_win_053")
root.mkdir(exist_ok=True)
adb=[r"C:\adb\adb.exe","-s","E6ZTMFVCEA895HAA"]
def call(*args):
 p=subprocess.run(adb+list(args),capture_output=True,timeout=65)
 msg=(p.stdout+p.stderr).decode('utf-8','replace')
 if p.returncode:raise RuntimeError(" ".join(args)+" "+msg[-250:])
 return p.stdout
call("shell","am","force-stop","com.mikhnevcode.brainrotplanet")
call("logcat","-c")
call("shell","am","start","-n","com.mikhnevcode.brainrotplanet/com.unity3d.player.UnityPlayerGameActivity")
time.sleep(8)
call("shell","input","tap","530","2050")
print("AI_WIN_MATCH_STARTED",flush=True)
time.sleep(158)
remote="/sdcard/brainrot_053_ai_win.png"
call("shell","screencap","-p",remote)
call("pull",remote,str(root/"AI_Winner_Realme_053.png"))
raw=call("logcat","-d","-s","Unity:I","AndroidRuntime:E","*:S").decode("utf-8","replace")
(root/"unity_log.txt").write_text(raw,encoding="utf-8")
teams=re.findall(r'HERO_MATCH_VICTORY team=(\d+) cameraFocus=(\w+)',raw)
focus=re.findall(r'CAM_VICTORY_FOCUS_STARTED teamActor=([^\r\n]+)',raw)
fps=[float(n) for n in re.findall(r'PERF_SAMPLE stage=game fps=([0-9.]+)',raw)]
result={
 "match_seconds":145,
 "total_device_wait_seconds":158,
 "winner_events":teams,
 "camera_focus":focus,
 "celebration_replays":len(re.findall('HERO_MATCH_VICTORY_REPLAY',raw)),
 "user_auto_motion_events":len(re.findall('AUTO_MOVE_STARTED',raw)),
 "user_ability_events":len(re.findall('HERO_SPECIAL_ACTIVATED hero=',raw)),
 "player_territory_capture_events":len(re.findall('TERRITORY_CAPTURE team=1',raw)),
 "end_frame_samples":len(re.findall('PERF_SAMPLE stage=end',raw)),
 "steady_fps":round(statistics.mean(fps[1:]),2) if len(fps)>1 else None,
 "android_fatal":"FATAL EXCEPTION" in raw
}
(root/"report.json").write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding="utf-8")
print("AI_WIN_FULL_MATCH",json.dumps(result,ensure_ascii=False),flush=True)
