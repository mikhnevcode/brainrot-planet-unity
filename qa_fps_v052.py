from pathlib import Path
import subprocess,time,re,json,statistics
root=Path(r"C:\Rabota\projects\BrainrotPlanetUnity\qa_fps_v052")
root.mkdir(exist_ok=True)
adb=[r"C:\adb\adb.exe","-s","E6ZTMFVCEA895HAA"]
def call(*args,timeout=80):
 r=subprocess.run(adb+list(args),capture_output=True,timeout=timeout)
 msg=(r.stdout+r.stderr).decode("utf-8","replace")
 if r.returncode:raise RuntimeError(" ".join(args)+" "+msg[-300:])
 return r.stdout
call("shell","svc","power","stayon","usb")
call("shell","am","force-stop","com.mikhnevcode.brainrotplanet")
call("logcat","-c")
call("shell","am","start","-n","com.mikhnevcode.brainrotplanet/com.unity3d.player.UnityPlayerGameActivity")
time.sleep(7.5)
call("shell","input","tap","530","2050")
time.sleep(4)
call("shell","input","swipe","410","935","745","935","500")
time.sleep(22)
call("shell","input","swipe","430","920","750","1250","490")
time.sleep(17)
call("shell","input","swipe","390","990","750","750","480")
time.sleep(14)
raw=call("logcat","-d","-s","Unity:I","AndroidRuntime:E","*:S").decode("utf-8","replace")
(root/"log.txt").write_text(raw,encoding="utf-8")
pattern=re.compile(r"PERF_SAMPLE stage=(\w+) fps=([0-9.]+) p95ms=([0-9.]+) maxms=([0-9.]+)")
samples=[{"stage":m.group(1),"fps":float(m.group(2)),
          "p95ms":float(m.group(3)),"maxms":float(m.group(4))}
         for m in pattern.finditer(raw)]
game=[s for s in samples if s["stage"]=="game"]
steady=game[1:] if len(game)>2 else game
data={"samples":samples,"steady_state_samples":steady,
      "mean_fps":round(statistics.mean([s["fps"] for s in steady]),2) if steady else None,
      "median_fps":round(statistics.median([s["fps"] for s in steady]),2) if steady else None,
      "min_fps":min([s["fps"] for s in steady]) if steady else None,
      "max_p95ms":max([s["p95ms"] for s in steady]) if steady else None,
      "captures":len(re.findall(r"TERRITORY_CAPTURE team=1",raw)),
      "android_fatal":("FATAL EXCEPTION" in raw)}
(root/"report.json").write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding="utf-8")
print("REALME_FPS_LONG_SOAK",json.dumps(data,ensure_ascii=False),flush=True)
