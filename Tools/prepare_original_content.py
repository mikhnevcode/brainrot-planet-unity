"""Validate and bundle the original wardrobe; preserve unpublished drafts."""
import hashlib
import json
import shutil
import subprocess
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
source=ROOT/'Content/OriginalCast/Skins'
manifest=json.loads((source/'catalog-original26-v1.json').read_text(encoding='utf-8'))
roster=json.loads((ROOT/'Characters/OriginalCast/roster.json').read_text(encoding='utf-8'))
assert len(roster['heroes'])==26 and len(manifest['skins'])==130
bundle=ROOT/'Assets/Resources/OriginalCast/Skins'
bundle.mkdir(parents=True,exist_ok=True)
expected={'catalog-original26-v1.json'}
for hero in range(26):assert sum(e['hero']==hero for e in manifest['skins'])==5
for entry in manifest['skins']:
    assert entry['id'].startswith('orig_') and entry['file']==entry['id']+'-'+entry['sha256']+'.json'
    raw=(source/entry['file']).read_bytes();assert len(raw)==entry['bytes'] and hashlib.sha256(raw).hexdigest()==entry['sha256']
    pack=json.loads(raw);assert pack['hero']==entry['hero'] and pack['id']==entry['id'] and pack['schema']==2
    assert 0<len(pack['bindings'])<128 and len(pack['meshes'])>0
    for mesh in pack['meshes']:
        assert len(mesh['vertices'])%3==0 and len(mesh['triangles'])%3==0
        assert max(mesh['triangles'])<len(mesh['vertices'])//3
        assert all(abs(v)<5 for v in mesh['vertices'])
    shutil.copyfile(source/entry['file'],bundle/(entry['id']+'.json'))
    expected.add(entry['file'])
shutil.copyfile(source/'catalog-original26-v1.json',bundle/'catalog-original26-v1.json')
# These are our never-published intermediate exports, not installed/user content.
drafts=ROOT/'Builds/OriginalCast_unpublished_pack_drafts'
for file in source.glob('orig_*.json'):
    if file.name not in expected:
        drafts.mkdir(parents=True,exist_ok=True)
        target=drafts/file.name
        if not target.exists():file.rename(target)
subprocess.run([sys.executable,str(ROOT/'Server/build_store_catalog.py'),'--original26'],check=True)
print('ORIGINAL_CONTENT_PASS heroes=26 skins=130 immutable_hashes=130 bundled=true')
