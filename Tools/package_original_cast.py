"""Package this milestone against the verified authoritative Git checkout.

Read-only against the server. Includes a hash manifest; never uploads caches,
credentials, .blend backups, old APKs, or unrelated untracked project files.
"""
import hashlib
import io
import json
from pathlib import Path
import subprocess
import tarfile

ROOT=Path(__file__).resolve().parents[1]
SSH=['ssh','-o','BatchMode=yes','-i',str(Path.home()/'.ssh/homeserver_ed25519'),'user@192.168.0.107']
REMOTE='/home/user/brainrot-planet-unity/BrainrotPlanetUnity'


def remote(command):
    return subprocess.check_output(SSH+[f'cd {REMOTE} && {command}'])


def main():
    if remote('git status --porcelain').strip():
        raise RuntimeError('Authoritative checkout is dirty; review before synchronization')
    head=remote('git rev-parse HEAD').decode().strip()
    tracked={}
    for line in remote('git ls-files -s -z').split(b'\0'):
        if line:
            metadata,path=line.split(b'\t',1)
            tracked[path.decode()]=metadata.split()[1].decode()
    # Archive the authoritative historical bytes, not locally reserialized old
    # materials/import settings. Local rollback copies are left untouched.
    historical={}
    with tarfile.open(fileobj=io.BytesIO(remote('git archive HEAD Assets/Resources/Characters Assets/Resources/Characters.meta Assets/Resources/HeroPortraits064.png Assets/Resources/HeroPortraits064.png.meta'))) as old:
        for member in old.getmembers():
            if member.isfile():historical[member.name.replace('Assets/Resources/','Assets/LegacyCharacterArchive/',1)]=old.extractfile(member).read()
    scoped=('Assets/Scripts/','Assets/Editor/','Assets/Resources/Characters/',
            'Assets/Resources/HeroPortraits064.png')
    candidates={p for p in tracked if p.startswith(scoped)}
    candidates.update(['.gitignore','ProjectSettings/ProjectSettings.asset',
                       'Assets/Resources/Characters.meta','Assets/Resources/MatchEndpoint.txt',
                       'Assets/Resources/StoreTestEndpoint.txt','Server/build_store_catalog.py',
                       'Server/test_store.py','Server/room_match_smoke.py','Server/store_wss_smoke.py'])
    for directory in ['Assets/Scripts','Assets/Editor','Assets/Resources/OriginalCast',
                      'Assets/LegacyCharacterArchive','Characters/OriginalCast','Content/OriginalCast']:
        candidates.update(p.relative_to(ROOT).as_posix() for p in (ROOT/directory).rglob('*') if p.is_file())
    candidates.update(['Assets/Resources/OriginalCast.meta','Assets/Resources/OriginalHeroSurface.shader',
                       'Assets/Resources/OriginalHeroSurface.shader.meta','Assets/LegacyCharacterArchive.meta',
                       'Tools/prepare_original_content.py','Tools/package_original_cast.py'])
    changed=[];deleted=[]
    for name in sorted(candidates):
        # Pre-existing whitespace-only metadata differences are unrelated to this
        # milestone. Keep the authoritative GUID files byte-for-byte unchanged.
        if name in {'Assets/Scripts/GameplayFeedback.cs.meta','Assets/Editor/GameplayFeedbackSmoke.cs.meta'}:continue
        if name.endswith(('.blend1','.blend2','.pyc')) or '/__pycache__/' in name:continue
        path=ROOT/name
        if not path.is_file():
            if name in tracked:deleted.append({'path':name,'oldBlob':tracked[name]})
            continue
        raw=historical.get(name,path.read_bytes())
        blob=hashlib.sha1(b'blob '+str(len(raw)).encode()+b'\0'+raw).hexdigest()
        if blob!=tracked.get(name):changed.append({'path':name,'sha256':hashlib.sha256(raw).hexdigest(),'oldBlob':tracked.get(name)})
    allowed_delete=('Assets/Resources/Characters','Assets/Resources/HeroPortraits064.png')
    if any(not item['path'].startswith(allowed_delete) for item in deleted):
        raise RuntimeError('Unexpected missing tracked source; do not delete: '+str([i['path'] for i in deleted if not i['path'].startswith(allowed_delete)]))
    manifest={'schema':1,'baseCommit':head,'changed':changed,'deleted':deleted}
    output=ROOT/'Builds/original26-source.tar.gz'
    with tarfile.open(output,'w:gz') as archive:
        for item in changed:
            if item['path'] in historical:
                raw=historical[item['path']];info=tarfile.TarInfo(item['path']);info.size=len(raw);info.mode=0o644
                archive.addfile(info,io.BytesIO(raw))
            else:archive.add(ROOT/item['path'],arcname=item['path'],recursive=False)
    (ROOT/'Builds/original26-sync.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    print(f'ORIGINAL_SOURCE_PACKAGE changed={len(changed)} archivedPaths={len(deleted)} bytes={output.stat().st_size} base={head}')


if __name__=='__main__':main()
