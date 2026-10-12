from pathlib import Path
import subprocess,time,json
root=Path(r"C:\Rabota\projects\BrainrotPlanetUnity\qa_difficulty_055")
adb=[r"C:\adb\adb.exe","-s","E6ZTMFVCEA895HAA"]
def call(*args):
 p=subprocess.run(adb+list(args),capture_output=True,timeout=70)
 msg=(p.stdout+p.stderr).decode("utf-8","replace")
 if p.returncode: raise RuntimeError(msg[-300:])
 return msg
def launch():
 call("shell","am","force-stop","com.mikhnevcode.brainrotplanet")
 call("logcat","-c")
 call("shell","am","start","-n","com.mikhnevcode.brainrotplanet/com.unity3d.player.UnityPlayerGameActivity")
 time.sleep(8)
 return call("logcat","-d","-s","Unity:I","*:S")
before=launch()
wasHard="AI_DIFFICULTY_LOADED level=Hard" in before
call("shell","input","tap","540","1885")
time.sleep(.5)
after=launch()
nowMedium="AI_DIFFICULTY_LOADED level=Medium" in after
remote="/sdcard/difficulty055_medium_restored.png"
call("shell","screencap","-p",remote)
call("pull",remote,str(root/"Medium_restored.png"))
out={"hard_persisted_after_restart":wasHard,
     "medium_persisted_after_restart":nowMedium,
     "device_restored_to_medium":wasHard and nowMedium}
(root/"persistence.json").write_text(json.dumps(out,indent=2),encoding="utf-8")
print("DIFFICULTY_PERSISTENCE_QA",json.dumps(out),flush=True)
if not out["device_restored_to_medium"]:raise RuntimeError("Difficulty persistence failed")
