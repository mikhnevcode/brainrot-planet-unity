"""Reference step 004: facial proportions only, using a shared skin/detail warp.

True mesh coordinates are changed; no concept projection or image substitution.
The previous 003 source and every earlier review remain untouched.
"""
import sys,json,math
from pathlib import Path
import bpy
from mathutils import Vector

BASE=Path(__file__).resolve().parent
sys.path.insert(0,str(BASE))
import sculpt_rollo_reference_step003 as P
import sculpt_rollo_v05 as A

OUT=BASE/'ArtReview/RolloReferenceMatch'


def facial_warp(point):
    x,y,z=point
    # Localized to the front of the upper torso, zero on the flippers/back.
    front=max(0,min(1,(y-.55)/.28))
    centre=math.exp(-(abs(x)/.90)**8)
    muzzle=math.exp(-((z-2.56)/.29)**4)
    eyes=math.exp(-((z-2.97)/.19)**4)
    f=front*centre
    return Vector((x*(1-f*(.115*muzzle+.24*eyes)),
                   y+f*(-.075*muzzle+.105*eyes),
                   z+f*(.225*muzzle+.061*eyes)))


def make():
    A.flipper=P.reference_flipper
    body=A.make()
    for obj in list(bpy.context.scene.objects):
        if obj.type!='MESH':continue
        inverse=obj.matrix_world.inverted()
        for vertex in obj.data.vertices:
            vertex.co=inverse@facial_warp(obj.matrix_world@vertex.co)
        obj.data.update()
    return body


if __name__=='__main__':
    make();OUT.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'source-004.blend'))
    config=json.loads((OUT/'iteration-003.json').read_text())
    config['master']='ArtReview/RolloReferenceMatch/source-004.blend'
    config['changed']=['Facial proportions only: raised muzzle and mouth, reduced lateral eye spacing, narrower cheeks. Same camera/pose/body/flippers/roller.']
    config['sourceLandmarks']={k:list(facial_warp(v)) for k,v in config['sourceLandmarks'].items()}
    (OUT/'iteration-004.json').write_text(json.dumps(config,indent=2)+'\n')
    print('ROLLO_REFERENCE_SOURCE_PASS 004',flush=True)
