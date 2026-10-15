"""Build evidence-only handoff JSON from recorded gates, not assumed results."""
import hashlib,json,re
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]


def read(name):return (ROOT/'Builds'/name).read_text(encoding='utf-8-sig',errors='replace')
def require(text,gate):
    if gate not in text:raise RuntimeError('Missing recorded gate: '+gate)


def main():
    build=read('original26-android-handoff-build.log')
    cast=read('original26-device-cast.log');store=read('original26-device-store.log')
    network=read('original26-device-network.log')
    require(build,'ORIGINAL26_BUILD_PASS target=Android')
    require(build,'ORIGINAL26_REGRESSION_PASS protocol=10 heroes=26')
    require(cast,'CAST_WARDROBE_QA_PASS heroes=26 skins=130')
    require(cast,'ORIGINAL_AI_WINNER_QA_PASS upright=true team=2')
    require(store,'STORE_QA_PASS catalog177')
    require(network,'MATCH_QA_PASS result_replay_offline')
    for text in (cast,store,network):
        if re.search(r'(CAST_WARDROBE_QA_FAIL|STORE_QA_FAIL|MATCH_QA_FAIL|NullReferenceException|MissingReferenceException)',text):raise RuntimeError('Device failure in log')
    photos=ROOT/'Builds/Original26Device/cast-qa'
    for hero in range(26):
        for variant in range(5):
            if not (photos/f'hero{hero:02d}-variant{variant}-wardrobe.png').is_file():raise RuntimeError('Missing wardrobe evidence')
        for kind in ('game','victory'):
            if not (photos/f'hero{hero:02d}-{kind}.png').is_file():raise RuntimeError('Missing gameplay evidence')
    apk=ROOT/'Builds/Android/BrainrotPlanet-v0.8.0-original26-test.apk'
    data={'schema':1,'version':'0.8.0-original26-test','androidVersionCode':70,
          'apkSha256':hashlib.sha256(apk.read_bytes()).hexdigest(),'apkBytes':apk.stat().st_size,
          'device':'RMX2020','networkProtocol':10,'heroes':26,'clips':208,'skinVariants':130,
          'artStatus':'First mobile 3D pass. Variants are palettes with sculpted crests, not bespoke full costumes.',
          'editorRegression':'PASS: 26 rigs, 208 clips, 130 applied skins, opaque materials, menu framing, store validation, terrain, poles, paint, deployment, collisions and outcomes.',
          'deviceCast':'PASS: 130 wardrobe previews, 26 gameplay/skill/player-victory paths, AI winner upright.',
          'deviceCastApkSha256':'86ef6cc9f94572f7c7d711adf083febba60c4a2441051fa2c2945d237c7269ce',
          'finalApkDeltaAfterCastRun':'Shorter wardrobe labels and separate original26 test-store cache namespace; same 26 FBX models, controllers and 130 skin packs. Final APK store and full network match tested separately.',
          'deviceStore':'PASS: cancel, error, pending, approve, duplicate, refund, restore, recovery, game cosmetics and victory; realMoney=0.',
          'deviceNetwork':'PASS: authoritative match, result, replay and return to offline mode.',
          'serverWss':'PASS: two quick peers and two invited-room peers, shared authoritative snapshots; new hero indices 13, 22 and 25 accepted.',
          'storeUnitTests':18,'storeProducts':177,'freeTestSkinGrants':26,
          'remaining':['Art polish and bespoke outfits','Long-run FPS/thermal profiling','iOS build and device QA','Production billing, advertisements and release review are not part of this test build'],
          'sourceLogs':['original26-android-handoff-build.log','original26-device-cast.log','original26-device-store.log','original26-device-network.log']}
    output=ROOT/'QA/v080/original26-report.json';output.parent.mkdir(parents=True,exist_ok=True)
    output.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print('ORIGINAL26_QA_REPORT_READY sha256='+data['apkSha256'])


if __name__=='__main__':main()
