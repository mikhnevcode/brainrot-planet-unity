"""v0.8.4 facial refinement QA, frozen at the actual device fixture boundary."""
import hashlib,json,xml.etree.ElementTree as ET
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'Builds/RolloFace'


def profile(raw):return {e.attrib['name']:(e.tag,e.attrib.get('value'),e.text or '') for e in ET.fromstring(raw) if not e.attrib['name'].startswith('unity')}


def main():
    gates={
        'Builds/rollo-v07-geometry.log':['ROLLO_FACE_GEOMETRY_PASS'],
        'Builds/rollo-v07-export.log':['ROLLO_MOBILE_EXPORT_PASS'],
        'Builds/rollo-v07-unity-prepare.log':['ORIGINAL_CAST_PREPARE_PASS heroes=26 animations=208 skins=130','ROLLO_DETAIL_UNITY_PREPARE_PASS']+['ROLLO_DETAIL_RENDER_PASS variant=pose-'+p for p in ('Run','Jump','Drop','VictoryShow')],
        'Builds/rollo-v07-android-build.log':['ORIGINAL26_REGRESSION_PASS protocol=10 heroes=26','ORIGINAL26_BUILD_PASS target=Android','ROLLO_DETAIL_MOTION_SMOKE_PASS','ROLLO_DETAIL_FACE_SMOKE_PASS'],
        'Builds/RolloFace/device-menu.log':['HERO_MENU_QA_PASS heroes=26 views=156'],
        'Builds/RolloFace/device-wardrobe.log':['CAST_WARDROBE_QA_PASS heroes=1 skins=5','ORIGINAL_AI_WINNER_QA_PASS upright=true team=2','SKIN_CATALOG_READY revision=7 count=130']}
    for name,expected in gates.items():
        text=(ROOT/name).read_text(encoding='utf-8-sig',errors='replace')
        for gate in expected:
            if gate not in text:raise RuntimeError('Missing gate: '+gate)
        if any(f in text for f in ('QA_FAIL','FATAL EXCEPTION','NullReferenceException','MissingReferenceException')):raise RuntimeError('Failure in '+name)
    state=json.loads((BASE/'device-run.json').read_text());baseline=Path(state['baseline']).read_bytes()
    for phase in ('menu','wardrobe'):
        result=state['phases'][phase]
        if not result['pass'] or profile(baseline)!=profile(Path(result['snapshot']).read_bytes()):raise RuntimeError('Profile/phase validation: '+phase)
    for name in ['hero00-variant'+str(i)+'-wardrobe' for i in range(5)]+['hero00-game','hero00-victory','ai-winner']:
        if not (BASE/'device-wardrobe'/(name+'.png')).is_file():raise RuntimeError('Missing evidence: '+name)
    model=json.loads((ROOT/'Characters/OriginalCast/Models_v07/RolloBulk/mobile-report.json').read_text())
    geometry=json.loads((ROOT/'QA/v084/face-geometry-report.json').read_text())
    if not geometry['tongueClosed'] or not geometry['rollerGeometryByteIdenticalToV06']:raise RuntimeError('Geometry regression')
    apk=ROOT/'Builds/Android/BrainrotPlanet-v0.8.4-rollo-face-test.apk';digest=hashlib.sha256(apk.read_bytes()).hexdigest()
    report={'schema':1,'version':'0.8.4-rollo-face-test','versionCode':74,'apkFile':'BrainrotPlanet-v0.8.4-rollo-face-test-'+digest[:8]+'.apk','apkSha256':digest,'apkBytes':apk.stat().st_size,
            'scope':'Rollo throat/jaw, paired muzzle, real thin tongue, smiling eye openings and curved tapered brows; other 25 models unchanged',
            'networkProtocol':10,'skinCatalogRevision':7,'mobileTriangles':model['mobileTriangles'],'masterTriangles':model['masterTriangles'],'materials':model['materials'],
            'animations':8,'bakedMaps':6,'skins':5,'device':'RMX2020 NBTK5TZTVC8T9TRG','menuViews':156,
            'editor':'PASS 26 models / 208 clips / 130 skins; normalized weights; outward mouth/tongue; four action renders',
            'deviceChecks':'PASS 26 menu previews, five Rollo skins, movement/jump/ability, player and Rollo AI victory',
            'gameplayProfileUnchanged':True,'profileSnapshots':{p:hashlib.sha256(Path(state['phases'][p]['snapshot']).read_bytes()).hexdigest() for p in ('menu','wardrobe')},
            'geometry':geometry,'evidence':list(gates),'artReview':'Characters/OriginalCast/ArtReview/RolloReferenceMatch/iteration-018.json',
            'limits':['Not pixel-identical; user artistic approval separate from technical QA','Scripted anatomical sculpture, not manual brush sculpting','Not a sustained FPS/thermal test','No new full online-match test; protocol unchanged','No iOS or production billing changes']}
    output=ROOT/'QA/v084/rollo-face-report.json';output.parent.mkdir(parents=True,exist_ok=True);output.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print('ROLLO_FACE_QA_PASS sha256='+digest+' profileUnchanged=true')


if __name__=='__main__':main()
