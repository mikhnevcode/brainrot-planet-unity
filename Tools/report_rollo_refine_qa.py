"""Validate the v0.8.2 handoff using this iteration's actual evidence."""
import hashlib,json,subprocess,xml.etree.ElementTree as ET
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'Builds/RolloRefine'


def require(text,gate):
    if gate not in text:raise RuntimeError('Missing gate: '+gate)


def main():
    gates={
        'Builds/rollo-v04-face-winding.log':['ROLLO_FACE_WINDING_PASS mouth=true tongue=true'],
        'Builds/rollo-v04-unity-prepare.log':['ROLLO_DETAIL_UNITY_PREPARE_PASS']+['ROLLO_DETAIL_RENDER_PASS variant=pose-'+pose for pose in ('Run','Jump','Drop','VictoryShow')],
        'Builds/rollo-v04-android-build.log':['ORIGINAL26_BUILD_PASS target=Android','ORIGINAL26_REGRESSION_PASS protocol=10 heroes=26','ROLLO_DETAIL_MOTION_SMOKE_PASS','ROLLO_DETAIL_FACE_SMOKE_PASS mouthOutward=true tongueOutward=true'],
        'Builds/RolloRefine/device-menu.log':['HERO_MENU_QA_PASS heroes=26 views=156'],
        'Builds/RolloRefine/device-final.log':['CAST_WARDROBE_QA_PASS heroes=1 skins=5','ORIGINAL_AI_WINNER_QA_PASS upright=true team=2','SKIN_CATALOG_READY revision=4 count=130']}
    for filename,expected in gates.items():
        content=(ROOT/filename).read_text(encoding='utf-8-sig',errors='replace')
        for gate in expected:require(content,gate)
        if any(failure in content for failure in ('CAST_WARDROBE_QA_FAIL','HERO_MENU_QA_FAIL','NullReferenceException','MissingReferenceException')):
            raise RuntimeError('Failure in '+filename)
    for name in [f'hero00-variant{i}-wardrobe' for i in range(5)]+['hero00-game','hero00-victory','ai-winner']:
        if not (BASE/'device-final'/(name+'.png')).is_file():raise RuntimeError('Missing image '+name)
    def profile(root):
        return {e.attrib['name']:(e.tag,e.attrib.get('value'),e.text or '') for e in root if not e.attrib['name'].startswith('unity')}
    before=ET.parse(BASE/'DeviceBackup/playerprefs.xml').getroot()
    current=subprocess.check_output(['C:/adb/adb.exe','-s','NBTK5TZTVC8T9TRG','exec-out','run-as','com.mikhnevcode.brainrotplanet','cat','shared_prefs/com.mikhnevcode.brainrotplanet.v2.playerprefs.xml'])
    if profile(before)!=profile(ET.fromstring(current)):raise RuntimeError('Gameplay profile changed')
    apk=ROOT/'Builds/Android/BrainrotPlanet-v0.8.2-rollo-refine-test.apk'
    digest=hashlib.sha256(apk.read_bytes()).hexdigest()
    model=json.loads((ROOT/'Characters/OriginalCast/Models_v04/RolloBulk/mobile-report.json').read_text())
    result={'schema':1,'version':'0.8.2-rollo-refine-test','versionCode':72,
        'apkFile':'BrainrotPlanet-v0.8.2-rollo-refine-test-'+digest[:8]+'.apk',
        'apkSha256':digest,'apkBytes':apk.stat().st_size,'networkProtocol':10,'skinCatalogRevision':4,
        'scope':'Rollo reference-led anatomy refinement only; other 25 characters unchanged.',
        'anatomy':['Heavy pear-shaped belly and shoulders','Broader paired whisker pads','Smaller inset almond eye caps with brown iris rims','Thicker larger flippers','Visible offset forked tail','Monotonic roller geometry: ivory discs no longer buried by cubic overshoot'],
        'mobileTriangles':model['mobileTriangles'],'animations':8,'bakedMaps':5,'colorVariants':5,
        'editor':'PASS all 26 models, 208 clips, 130 skins; drum/tail articulation; normalized weights; gameplay regression.',
        'device':'RMX2020 NBTK5TZTVC8T9TRG','menu':'PASS 26 heroes / 156 framing views',
        'wardrobe':'PASS five skin previews, movement, jump, ability, player victory and Rollo AI victory.',
        'gameplayProfileUnchanged':True,'evidence':list(gates),
        'limits':['Visual likeness still requires user art approval','Not a measured FPS/thermal endurance test','No new full network-match test: cosmetic update, protocol unchanged','No iOS build or production billing changes']}
    output=ROOT/'QA/v082/rollo-refine-report.json';output.parent.mkdir(parents=True,exist_ok=True)
    output.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print('ROLLO_REFINE_QA_PASS sha256='+digest+' gameplayProfileUnchanged=true')


if __name__=='__main__':main()
