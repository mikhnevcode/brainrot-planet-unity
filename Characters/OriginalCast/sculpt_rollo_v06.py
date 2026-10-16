"""Reproducible final candidate assembled from inspected small reference steps.

The production source is unposed. The matching camera/pose lives separately
in ArtReview/RolloReferenceMatch/iteration-014.json; export applies that rest
pose to the baked low-poly surface and rig pivots together.
"""
import sys,json
from pathlib import Path
import bpy
BASE=Path(__file__).resolve().parent
sys.path.insert(0,str(BASE))
import sculpt_rollo_reference_step012 as F
from sculpt_rollo_reference_step013 import finish_face
from sculpt_rollo_reference_step014 import finish_spots
G=F.G;S=F.S
OUT=BASE/'Models_v06/RolloBulk'


def make():
    body=F.make();finish_face();finish_spots(body.data.materials[0]);return body


if __name__=='__main__':
    OUT.mkdir(parents=True,exist_ok=True);make()
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'RolloBulk_v06_master.blend'))
    count=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in bpy.context.scene.objects if o.type=='MESH')
    report={'status':'reference-led-production-candidate','triangles':count,'reference':'OriginalConcepts_20261015_v1/01-rollo-bulk.png',
            'review':'ArtReview/RolloReferenceMatch/iteration-014.json','technique':'Continuous voxel-fused anatomical mesh, scripted local sculpture; not manual brush sculpting',
            'limits':['Not a pixel-identical reconstruction of the concept','User artistic approval remains separate from technical QA']}
    (OUT/'report.json').write_text(json.dumps(report,indent=2)+'\n')
    print('ROLLO_V06_MASTER_PASS',flush=True)
