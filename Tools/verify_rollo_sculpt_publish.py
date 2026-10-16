"""Verify immutable public bytes and the exact referenced skin packs over TLS."""
import concurrent.futures,hashlib,json,time,urllib.request,urllib.error
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
BASE='https://loadfiles.testmkn.ru/downloads/brainrot-planet/cast/'
HEADERS={'User-Agent':'BrainrotPlanet-QA/0.8.3'}


def get(name):
    for attempt in range(3):
        try:
            with urllib.request.urlopen(urllib.request.Request(BASE+name,headers=HEADERS),timeout=20) as response:
                if response.status!=200:raise RuntimeError(name)
                return response.read()
        except (urllib.error.URLError,TimeoutError):
            if attempt==2:raise
            time.sleep(.4*(attempt+1))


def main():
    report=json.loads((ROOT/'QA/v083/rollo-sculpt-report.json').read_text())
    page=get('test-v0.8.3.html').decode()
    if report['apkFile'] not in page or report['apkSha256'] not in page:raise RuntimeError('Wrong public page')
    digest=hashlib.sha256();size=0
    with urllib.request.urlopen(urllib.request.Request(BASE+report['apkFile'],headers=HEADERS),timeout=60) as response:
        while chunk:=response.read(1048576):digest.update(chunk);size+=len(chunk)
    if digest.hexdigest()!=report['apkSha256'] or size!=report['apkBytes']:raise RuntimeError('Public APK mismatch')
    print('ROLLO_SCULPT_PUBLIC_APK_SHA256_PASS',flush=True)
    files=['rollo083/'+name for name in ('review-before.png','review-reference.png','review-current.png','unity-classic.png','unity-victory.png')]
    for name in files:
        if not get(name).startswith(b'\x89PNG\r\n\x1a\n'):raise RuntimeError('Invalid public PNG '+name)
    manifest=json.loads(get('rollo083/skins/catalog-original26-v5.json'))
    if manifest['revision']!=6 or len(manifest['skins'])!=130:raise RuntimeError('Wrong public wardrobe')
    def verify(entry):
        raw=get('rollo083/skins/'+entry['file'])
        if len(raw)!=entry['bytes'] or hashlib.sha256(raw).hexdigest()!=entry['sha256']:raise RuntimeError(entry['id'])
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
        for count,future in enumerate(concurrent.futures.as_completed([pool.submit(verify,e) for e in manifest['skins']]),1):
            future.result()
            if count%25==0:print('ROLLO_SCULPT_PUBLIC_SKINS_VERIFIED '+str(count),flush=True)
    result={'schema':1,'page':BASE+'test-v0.8.3.html','apkSha256':digest.hexdigest(),'apkBytes':size,'publicPngs':len(files),'verifiedSkinPacks':130,'https':'PASS'}
    (ROOT/'QA/v083/publish-verification.json').write_text(json.dumps(result,indent=2)+'\n')
    print('ROLLO_SCULPT_HTTPS_PASS fullApkSha256=true images=5 skins=130')


if __name__=='__main__':main()
