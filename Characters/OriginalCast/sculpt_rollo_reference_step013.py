"""013: a shorter, wider tongue and shallower mouth opening only."""
import sys,json
from pathlib import Path
import bpy
BASE=Path(__file__).resolve().parent
sys.path.insert(0,str(BASE))
import sculpt_rollo_reference_step012 as F
OUT=F.OUT


def finish_face():
    for obj in bpy.context.scene.objects:
        if obj.type!='MESH':continue
        inverse=obj.matrix_world.inverted()
        if obj.name.startswith(('Rounded projecting tongue','Tongue crease')):
            for v in obj.data.vertices:
                p=obj.matrix_world@v.co;p.x*=1.20;p.z=2.57+(p.z-2.535)*.84;v.co=inverse@p
        elif obj.name.startswith('Recessed open mouth'):
            for v in obj.data.vertices:
                p=obj.matrix_world@v.co;p.z=2.65+(p.z-2.65)*.80;v.co=inverse@p
        obj.data.update()


if __name__=='__main__':
    bpy.ops.wm.open_mainfile(filepath=str(OUT/'source-012.blend'));finish_face()
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'source-013.blend'))
    config=json.loads((OUT/'iteration-012.json').read_text());config['master']='ArtReview/RolloReferenceMatch/source-013.blend'
    config['changed']=['Mouth/tongue only: shallower opening and shorter, wider projecting tongue. All other geometry, material and camera unchanged.']
    (OUT/'iteration-013.json').write_text(json.dumps(config,indent=2)+'\n')
    print('ROLLO_REFERENCE_SOURCE_PASS 013',flush=True)
