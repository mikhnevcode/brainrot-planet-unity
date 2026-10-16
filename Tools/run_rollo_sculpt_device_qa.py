"""Serial-qualified v0.8.3 cosmetic QA; never clear data or award test progress."""
import argparse,datetime,json,subprocess,time,xml.etree.ElementTree as ET
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'Builds/RolloSculpt'
ADB='C:/Program Files/Unity/Hub/Editor/6000.2.7f2/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe'
SERIAL='NBTK5TZTVC8T9TRG';PACKAGE='com.mikhnevcode.brainrotplanet'
FILES='/sdcard/Android/data/'+PACKAGE+'/files'
PREF='shared_prefs/'+PACKAGE+'.v2.playerprefs.xml'
MARKERS=('.hero-menu-qa','.wardrobe-cast-qa')


def adb(*args):return subprocess.check_output([ADB,'-s',SERIAL,*args],stderr=subprocess.PIPE)
def shell(command):return adb('shell',command).decode(errors='replace').strip()
def profile(raw):
    return {e.attrib['name']:(e.tag,e.attrib.get('value'),e.text or '') for e in ET.fromstring(raw) if not e.attrib['name'].startswith('unity')}
def prefs():return adb('exec-out','run-as',PACKAGE,'cat',PREF)
def cleanup():
    for marker in MARKERS:shell('rm -f '+FILES+'/'+marker)
def stop():shell('am force-stop '+PACKAGE)
def launch():shell('monkey -p '+PACKAGE+' -c android.intent.category.LAUNCHER 1')


def main():
    p=argparse.ArgumentParser();p.add_argument('--install',action='store_true');p.add_argument('--phase',choices=['menu','wardrobe']);p.add_argument('--finish',action='store_true');args=p.parse_args()
    if shell('getprop ro.product.model').splitlines()[-1]!='RMX2020':raise RuntimeError('Wrong physical device')
    BASE.mkdir(parents=True,exist_ok=True);statepath=BASE/'device-run.json'
    if args.install:
        stop();cleanup()
        backup=BASE/'DeviceBackup'/datetime.datetime.now().strftime('%Y%m%d-%H%M%S');backup.mkdir(parents=True,exist_ok=False)
        raw=prefs();(backup/'playerprefs.xml').write_bytes(raw)
        installed=shell('pm path '+PACKAGE).splitlines()[0].removeprefix('package:').strip()
        adb('pull',installed,str(backup/'previous.apk'))
        apk=ROOT/'Builds/Android/BrainrotPlanet-v0.8.3-rollo-sculpt-test.apk'
        if not apk.is_file():raise RuntimeError('Missing APK; device left stopped with backup intact')
        result=adb('install','-r',str(apk)).decode(errors='replace')
        if 'Success' not in result:raise RuntimeError(result)
        if 'versionCode=73' not in shell('dumpsys package '+PACKAGE):raise RuntimeError('Unexpected installed version')
        if profile(raw)!=profile(prefs()):raise RuntimeError('Install changed gameplay profile')
        state={'serial':SERIAL,'model':'RMX2020','baseline':str(backup/'playerprefs.xml'),'phases':{}}
        statepath.write_text(json.dumps(state,indent=2)+'\n');print('ROLLO_SCULPT_INSTALL_PASS profileUnchanged=true',flush=True)
    if args.phase:
        state=json.loads(statepath.read_text());baseline=Path(state['baseline']).read_bytes()
        if profile(baseline)!=profile(prefs()):raise RuntimeError('Profile changed before fixture; do not overwrite it')
        stop();cleanup()
        marker=BASE/'qa-marker.txt';marker.write_text('rollo06' if args.phase=='wardrobe' else 'menu')
        adb('push',str(marker),FILES+('/.wardrobe-cast-qa' if args.phase=='wardrobe' else '/.hero-menu-qa'))
        start=shell("date '+%m-%d %H:%M:%S.000'")
        expected='HERO_MENU_QA_PASS heroes=26 views=156' if args.phase=='menu' else 'CAST_WARDROBE_QA_PASS heroes=1 skins=5'
        try:
            launch();deadline=time.monotonic()+180
            while time.monotonic()<deadline:
                time.sleep(3)
                raw=adb('logcat','-d','-T',start,'-s','Unity','AndroidRuntime');log=raw.decode(errors='replace')
                (BASE/('device-'+args.phase+'.log')).write_bytes(raw)
                if any(f in log for f in ('QA_FAIL','FATAL EXCEPTION','NullReferenceException','MissingReferenceException')):raise RuntimeError('Device fixture failure; inspect private log')
                if expected in log:break
            else:raise RuntimeError('Device fixture timeout')
            time.sleep(2)
            if profile(baseline)!=profile(prefs()):raise RuntimeError('Gameplay profile changed during fixture')
            after=Path(state['baseline']).with_name('post-'+args.phase+'-playerprefs.xml');after.write_bytes(prefs())
            folder='hero-menu-qa' if args.phase=='menu' else 'rollo-detail-qa'
            output=BASE/('device-'+args.phase);output.mkdir(exist_ok=True)
            adb('pull',FILES+'/'+folder+'/.',str(output))
            state['phases'][args.phase]={'pass':True,'log':str(BASE/('device-'+args.phase+'.log')),'snapshot':str(after),'profileUnchanged':True}
            statepath.write_text(json.dumps(state,indent=2)+'\n');print('ROLLO_SCULPT_DEVICE_PASS phase='+args.phase+' profileUnchanged=true',flush=True)
        finally:stop();cleanup()
    if args.finish:stop();cleanup();launch();print('ROLLO_SCULPT_NORMAL_LAUNCH',flush=True)


if __name__=='__main__':main()
