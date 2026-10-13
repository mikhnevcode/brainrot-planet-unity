"""Real device QA for the exact release APK. No game state injection.
Uses touch input, screenshot capture and aggregate runtime logs only.
"""
import argparse,hashlib,json,re,statistics,subprocess,time
from pathlib import Path

APK=Path(__file__).resolve().parent/'Builds/Android/BrainrotPlanet-v0.6.4.apk'
APK_HASH=hashlib.sha256(APK.read_bytes()).hexdigest()
ROOT=Path(__file__).resolve().parent/'qa_release_064'/APK_HASH[:12]
ROOT.mkdir(exist_ok=True)
ADB=[r'C:\adb\adb.exe','-s','E6ZTMFVCEA895HAA']
PACKAGE='com.mikhnevcode.brainrotplanet'
ACTIVITY=PACKAGE+'/com.unity3d.player.UnityPlayerGameActivity'
NAMES=['Tralalero','Bombardiro','Frigo','Bombombini','Ballerina','Assassino','Patapim','TungSahur','Lirili','Frulli','Boneca','Trippi','Troppa']
def call(*args):
    if len(args)>=3 and args[:2]==('shell','input') and args[2] in ('tap','swipe'):
        focused=call('shell','dumpsys','window')
        line=next((row for row in focused.splitlines() if 'mCurrentFocus=' in row),'')
        if PACKAGE not in line:raise RuntimeError('External window has focus; touch injection stopped: '+line.strip())
    p=subprocess.run(ADB+list(args),capture_output=True,timeout=40,check=True)
    return p.stdout.decode('utf-8','replace')
def tap(x,y):call('shell','input','tap',str(x),str(y))
def shot(name):
    focused=call('shell','dumpsys','window')
    line=next((row for row in focused.splitlines() if 'mCurrentFocus=' in row),'')
    if PACKAGE not in line:raise RuntimeError('External window has focus; screenshot stopped: '+line.strip())
    remote='/sdcard/qa064_'+name+'.png'
    call('shell','screencap','-p',remote);call('pull',remote,str(ROOT/(name+'.png')))
    call('shell','rm',remote)
def logs():return call('logcat','-d','-s','Unity:I','AndroidRuntime:E','*:S')

def memory_pss():
    raw=call('shell','dumpsys','meminfo',PACKAGE)
    found=re.search(r'TOTAL PSS:\s*(\d+)',raw)
    return int(found.group(1)) if found else None
def menu(hero,difficulty=0,relaunch=True):
    if relaunch:
        call('shell','am','force-stop',PACKAGE);call('logcat','-c')
        call('shell','am','start','-n',ACTIVITY)
        deadline=time.monotonic()+35
        while 'HERO_MENU_PREVIEW_READY' not in logs():
            if time.monotonic()>deadline:raise RuntimeError('Menu not ready after 35 seconds')
            time.sleep(.6)
        time.sleep(.5)
    for _ in range(3):tap(80,745);time.sleep(.12)
    for _ in range(hero//4):tap(950,745);time.sleep(.20)
    tap(*[(290,915),(790,915),(290,1140),(790,1140)][hero%4]);time.sleep(.6)
    tap([216,540,861][difficulty],1885)
def result(log):
    fps=[float(x) for x in re.findall(r'PERF_SAMPLE stage=game fps=([0-9.]+)',log)]
    return dict(errors=[line for line in log.splitlines() if any(s in line for s in ('FATAL EXCEPTION','NullReferenceException','MissingReferenceException','AndroidJavaException','Shader is not supported'))],fps_windows=len(fps),fps_mean=round(statistics.mean(fps),2) if fps else None,fps_min=min(fps) if fps else None,match_results=[line for line in log.splitlines() if 'AI_BALANCE_RESULT ' in line])
def save(name,log,data):
    data['apk_sha256']=APK_HASH
    (ROOT/(name+'.txt')).write_text(log,encoding='utf-8')
    (ROOT/(name+'.json')).write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
    print(name,json.dumps(data,ensure_ascii=False),flush=True)
def hero_test(hero):
    name='hero_'+str(hero).zfill(2)+'_'+NAMES[hero];menu(hero)
    shot(name+'_menu');tap(530,2050);time.sleep(4)
    shot(name+'_landed');parts=[logs()]
    for step in range(12):
        x,y=[(720,1100),(500,880),(280,1100),(500,1320)][step%4]
        call('shell','input','swipe','500','1100',str(x),str(y),'750');time.sleep(.25)
        if step==1:tap(910,2250);shot(name+'_jump')
        if step==4:tap(910,2045);time.sleep(.25);shot(name+'_special')
        if step%4==3:parts.append(logs())
    time.sleep(1);shot(name+'_capture');parts.append(logs())
    log='\n'.join(dict.fromkeys('\n'.join(parts).splitlines()))
    data=result(log);data.update(hero=hero,name=NAMES[hero],selected=f'HERO_MENU_SELECTED index={hero} ' in log,preview=f'HERO_MENU_PREVIEW_READY index={hero} ' in log,landed='PLAYER_DROP_LANDED' in log,rig=f'HERO_MOTION_RIG hero={hero} ' in log,ability=f'HERO_SPECIAL_ACTIVATED hero={hero}' in log,animated_ability=f'HERO_MOTION_SKILL hero={hero}' in log,vfx=f'HERO_FX_ACTIVATED hero={hero}' in log,captures=len(re.findall('TERRITORY_CAPTURE team=1 ',log)))
    data['jump']=f'PLAYER_JUMP_BEGIN character={hero}' in log
    data['interrupted']='MATCH_PAUSED' in log
    data['pass']=all(data[k] for k in ('selected','preview','landed','rig','ability','animated_ability','vfx','jump')) and not data['errors'] and not data['interrupted'] and (data['fps_mean'] or 0)>=55
    save(name,log,data);return data
def match(hero,difficulty,mode,number):
    name=f'match_{number}_{mode}_{difficulty}_{NAMES[hero]}'
    menu(hero,difficulty);tap(530,2050);time.sleep(4);start=time.monotonic();step=0;parts=[]
    while time.monotonic()-start<185:
        if mode=='loops':
            x,y=[(700,1100),(500,900),(300,1100),(500,1300)][step%4]
            call('shell','input','swipe','500','1100',str(x),str(y),'450')
            if step%8==0:tap(910,2045)
            if step%12==5:tap(910,2250)
        time.sleep(.75 if mode=='loops' else 3)
        if step%4==0:
            raw=logs();parts.append(raw)
            if 'MATCH_PAUSED' in raw:
                save(name+'_interrupted','\n'.join(parts),dict(interrupted=True,reason='External pause; not a completed match'))
                raise RuntimeError('Game was paused externally; stopping device QA')
            if 'AI_BALANCE_RESULT ' in raw:break
        if step in (4,20,40):shot(name+'_'+str(step))
        step+=1
    time.sleep(2);shot(name+'_result');parts.append(logs())
    time.sleep(1);shot(name+'_dance');parts.append(logs())
    log='\n'.join(dict.fromkeys('\n'.join(parts).splitlines()));data=result(log)
    data.update(hero=hero,difficulty=difficulty,mode=mode,wall_seconds=round(time.monotonic()-start,1),stable_camera='CAM_VICTORY_STABLE' in log)
    pose=re.search(r'CAM_VICTORY_STABLE upDot=([\d.-]+) faceDot=([\d.-]+)',log)
    data['front_facing']=bool(pose and float(pose[1])>.9 and float(pose[2])>.85)
    if not data['match_results']:
        save(name+'_incomplete',log,data);raise RuntimeError('Match did not complete; no result UI taps will be sent')
    # Confirm both result buttons rather than assuming they are clickable.
    call('logcat','-c');tap(530,2030);deadline=time.monotonic()+20
    replay=logs()
    while 'PLAYER_DROP_LANDED' not in replay and time.monotonic()<deadline:
        time.sleep(.6);replay=logs()
    data['replay']='AI_DIFFICULTY_MATCH ' in replay and 'PLAYER_DROP_LANDED' in replay
    log+='\nREPLAY_PHASE\n'+replay
    tap(965,190);time.sleep(.3);tap(530,1460);time.sleep(1)
    save(name,log,data);return data

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--heroes',action='store_true');parser.add_argument('--hero',type=int);parser.add_argument('--matches',action='store_true');parser.add_argument('--stress',type=int,default=0)
    args=parser.parse_args();call('shell','input','keyevent','KEYCODE_WAKEUP')
    if args.heroes or args.hero is not None:
        indices=range(13) if args.hero is None else [args.hero]
        all_results=[hero_test(i) for i in indices];save('heroes_report','',dict(results=all_results,passed=sum(r['pass'] for r in all_results),total=len(all_results)))
    if args.matches:
        results=[match(4,0,'loops',0),match(1,1,'idle',1),match(0,2,'idle',2)]
        save('matches_report','',dict(results=results))
    if args.stress:
        # No process restarts: use replay/menu during a continuous session.
        menu(1);tap(530,2050);time.sleep(4);start=time.monotonic();step=0;parts=[];completed=0;last_result='';memory=[dict(seconds=0,pss_kib=memory_pss())]
        while time.monotonic()-start<args.stress*60:
            raw=logs();parts.append(raw)
            if raw.rfind('MATCH_PAUSED')>raw.rfind('MATCH_RESUMED'):
                save('stress_interrupted','\n'.join(parts),dict(interrupted=True,seconds=round(time.monotonic()-start,1)))
                raise RuntimeError('Game was paused externally; stopping stress QA')
            rows=[line for line in raw.splitlines() if 'AI_BALANCE_RESULT ' in line]
            if rows and rows[-1]!=last_result:
                completed+=1;last_result=rows[-1];shot('stress_result_'+str(completed));tap(530,2030);time.sleep(4)
            x,y=[(700,1100),(500,900),(300,1100),(500,1300)][step%4]
            call('shell','input','swipe','500','1100',str(x),str(y),'650')
            if step%8==0:tap(910,2045)
            time.sleep(.7);step+=1
            if step%20==0:
                seconds=round(time.monotonic()-start);memory.append(dict(seconds=seconds,pss_kib=memory_pss()));print('STRESS_SECONDS',seconds,flush=True)
        log='\n'.join(dict.fromkeys('\n'.join(parts).splitlines()));data=result(log);data.update(seconds=round(time.monotonic()-start,1),completed_matches=completed,memory=memory);save('stress_report',log,data)
