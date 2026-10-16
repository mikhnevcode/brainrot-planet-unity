"""v07: short throat/jaw transition, thin tongue, smiling eyes and brows."""
import sys,json
from pathlib import Path
import bpy
BASE=Path(__file__).resolve().parent
sys.path.insert(0,str(BASE))
import sculpt_rollo_reference_step018 as F
G=F.G;S=F.S
OUT=BASE/'Models_v07/RolloBulk'


def make():return F.make()


if __name__=='__main__':
    OUT.mkdir(parents=True,exist_ok=True);make()
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'RolloBulk_v07_master.blend'))
    count=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in bpy.context.scene.objects if o.type=='MESH')
    report={'status':'facial-refinement-production-candidate','triangles':count,
            'reference':'OriginalConcepts_20261015_v1/01-rollo-bulk.png','review':'ArtReview/RolloReferenceMatch/iteration-018.json',
            'features':['Soft short throat below forward jaw','Deeper paired muzzle pads and nose bridge','Thin bent tongue with rounded tip and center groove','Almond eye opening and lifted lower lid','Shorter tapered curved brows'],
            'unchanged':['Roller','Flippers','Tail','Skin palette and spotting'],
            'technique':'Real mesh anatomy, voxel fusion and scripted local sculpture; not manual brush sculpting',
            'limits':['Not pixel-identical to concept','Art approval separate from technical QA']}
    (OUT/'report.json').write_text(json.dumps(report,indent=2)+'\n')
    print('ROLLO_V07_MASTER_PASS',flush=True)
