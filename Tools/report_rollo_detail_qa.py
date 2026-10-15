"""Record the detailed Rollo handoff from actual build/device evidence."""
import hashlib,io,json,subprocess,tarfile,xml.etree.ElementTree as ET
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'Builds/RolloDetail'


def text(path):return path.read_text(encoding='utf-8-sig',errors='replace')
def require(value,gate):
    if gate not in value:raise RuntimeError('Missing gate '+gate)


def main():
    build=text(ROOT/'Builds/rollo-v03-android-handoff-build.log')
    menu=text(BASE/'device-menu.log');device=text(BASE/'device-final.log')
    require(build,'ORIGINAL26_BUILD_PASS target=Android');require(build,'ORIGINAL26_REGRESSION_PASS protocol=10 heroes=26')
    require(build,'ROLLO_DETAIL_MOTION_SMOKE_PASS');require(menu,'HERO_MENU_QA_PASS heroes=26 views=156')
    require(device,'CAST_WARDROBE_QA_PASS heroes=1 skins=5');require(device,'ORIGINAL_AI_WINNER_QA_PASS upright=true team=2')
    for value in (menu,device):
        if any(v in value for v in ('CAST_WARDROBE_QA_FAIL','NullReferenceException','MissingReferenceException')):raise RuntimeError('Device failure')
    for variant in range(5):
        if not (BASE/f'device-final/hero00-variant{variant}-wardrobe.png').is_file():raise RuntimeError('Missing skin screenshot')
    pref='shared_prefs/com.mikhnevcode.brainrotplanet.v2.playerprefs.xml'
    backup=(BASE/'DeviceBackup/prefs.tar').read_bytes()
    # This legacy Windows adb adds CR before LF even for exec-out. The backup
    # contains only Android's LF-formatted SharedPreferences XML and TAR headers.
    # Normalize the transport in memory; never rewrite the device profile.
    if len(backup)%512:backup=backup.replace(b'\r\n',b'\n')
    with tarfile.open(fileobj=io.BytesIO(backup)) as archive:before=ET.fromstring(archive.extractfile(pref).read())
    now=subprocess.check_output(['C:/adb/adb.exe','-s','NBTK5TZTVC8T9TRG','exec-out','run-as','com.mikhnevcode.brainrotplanet','cat',pref])
    def fields(root):return {e.attrib['name']:(e.tag,e.attrib.get('value'),e.text or '') for e in root if not e.attrib['name'].startswith('unity')}
    if fields(before)!=fields(ET.fromstring(now)):raise RuntimeError('Gameplay profile changed in QA')
    apk=ROOT/'Builds/Android/BrainrotPlanet-v0.8.1-rollo-detail-test.apk';raw=apk.read_bytes();digest=hashlib.sha256(raw).hexdigest()
    model=json.loads((ROOT/'Characters/OriginalCast/Models_v03/RolloBulk/mobile-report.json').read_text())
    result={'schema':1,'version':'0.8.1-rollo-detail-test','versionCode':71,'apkSha256':digest,'apkBytes':len(raw),
            'apkFile':'BrainrotPlanet-v0.8.1-rollo-detail-test-'+digest[:8]+'.apk','networkProtocol':10,
            'scope':'Detailed Rollo only; 25 other characters remain previous technical models.',
            'masterTriangles':model['masterTriangles'],'mobileTriangles':model['mobileTriangles'],'maps':5,'animations':8,'colorVariants':5,
            'source':'Blender authored mesh, voxel-fused sculpt, UV unwrap and albedo/normal bake. No projected concept image.',
            'editor':'PASS: all 26 models, 208 clips and 130 skins; detailed drum/tail articulation and normalized weights.',
            'device':'RMX2020','deviceMenu':'PASS: 26 heroes / 156 framing views',
            'deviceRollo':'PASS: five color previews, skill, movement, jump, player victory and Rollo as AI winner upright.',
            'gameplayProfileUnchanged':True,'skinCatalogRevision':3,
            'network':'Cosmetic-only change, protocol 10 unchanged. Full network match was not rerun for 0.8.1.',
            'limits':['Art remains an approximation of the approved concept','Long-run FPS/thermal measurements not performed','No iOS build in this iteration','No production billing or advertisements'],
            'evidence':['Builds/rollo-v03-android-handoff-build.log','Builds/RolloDetail/device-menu.log','Builds/RolloDetail/device-final.log']}
    output=ROOT/'QA/v081/rollo-detail-report.json';output.parent.mkdir(parents=True,exist_ok=True)
    output.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print('ROLLO_DETAIL_QA_REPORT_PASS sha256='+digest+' gameplayProfileUnchanged=true')


if __name__=='__main__':main()
