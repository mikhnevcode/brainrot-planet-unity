"""012: lower the flipper shoulders and round/broaden the near distal blade."""
import sys,json
from pathlib import Path
import bpy
BASE=Path(__file__).resolve().parent
sys.path.insert(0,str(BASE))
import sculpt_rollo_reference_step011 as F
import sculpt_rollo_reference_step008 as W
A=F.A;G=F.G;S=F.S;OUT=F.OUT


def flipper(name,points,widths,thickness,bone,material):
    if name=='Anatomical foreflipper R':
        points=[(.68,1.10,1.98),(.94,1.20,1.89),(1.29,1.19,1.67),(1.64,1.02,1.43),(1.78,.91,1.41)]
        widths=[.18,.24,.28,.16,.002];thickness=.14
    elif name=='Anatomical foreflipper L':
        points=[(-1.06,.48,1.85),(-1.29,.64,1.72),(-1.49,.75,1.51),(-1.62,.78,1.34),(-1.64,.76,1.30)]
        widths=[.14,.22,.22,.13,.002];thickness=.125
    return W.F.F.P.original_flipper(name,points,widths,thickness,bone,material)


def make():
    W.flipper=flipper
    return F.make()


if __name__=='__main__':
    make();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'source-012.blend'))
    config=json.loads((OUT/'iteration-011.json').read_text())
    config['master']='ArtReview/RolloReferenceMatch/source-012.blend'
    config['changed']=['Flippers only: lower roots, wider near blade and rounded lower tip, lower far shoulder. Face/material/roller/camera unchanged.']
    config['sourceLandmarks'].update(flipperNearRoot=[.68,1.10,1.98],flipperNearTip=[1.78,.91,1.41],flipperFarRoot=[-1.06,.48,1.85],flipperFarTip=[-1.64,.76,1.30])
    (OUT/'iteration-012.json').write_text(json.dumps(config,indent=2)+'\n')
    print('ROLLO_REFERENCE_SOURCE_PASS 012',flush=True)
