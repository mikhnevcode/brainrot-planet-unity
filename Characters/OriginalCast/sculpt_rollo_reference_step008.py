"""008: rounded diagonal foreflippers; no facial/material/camera changes."""
import sys,json
from pathlib import Path
import bpy
BASE=Path(__file__).resolve().parent
sys.path.insert(0,str(BASE))
import sculpt_rollo_reference_step007 as F
A=F.A;G=F.G;S=F.S;OUT=F.OUT


def flipper(name,points,widths,thickness,bone,material):
    if name=='Anatomical foreflipper R':
        points=[(.68,1.02,2.14),(.94,1.20,2.00),(1.29,1.19,1.77),(1.59,1.02,1.54),(1.68,.91,1.47)]
        widths=[.16,.235,.245,.13,.002];thickness=.125
    elif name=='Anatomical foreflipper L':
        points=[(-1.06,.48,1.99),(-1.29,.64,1.82),(-1.49,.75,1.59),(-1.62,.78,1.40),(-1.64,.76,1.36)]
        widths=[.14,.22,.22,.13,.002];thickness=.125
    return F.F.P.original_flipper(name,points,widths,thickness,bone,material)


def make():
    A.sculpt=F.sculpt;A.flipper=flipper;A.eye_cap=F.F.eye_cap;A.seal_nose=F.F.seal_nose;A.projected_patch=F.patch
    G.ell=F.ell;G.tube=F.F.tube;G.loft=F.loft
    return A.make()


if __name__=='__main__':
    make();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'source-008.blend'))
    import review_rollo_reference as R
    config=json.loads((OUT/'iteration-007.json').read_text())
    config['master']='ArtReview/RolloReferenceMatch/source-008.blend'
    config['changed']=['Both foreflipper paths/sections only: near flipper sits forward of chest, far flipper slopes down without a square elbow.']
    config['sourceLandmarks'].update(R.landmarks())
    config['sourceLandmarks'].update(flipperNearRoot=[.68,1.02,2.14],flipperNearTip=[1.68,.91,1.47],flipperFarRoot=[-1.06,.48,1.99],flipperFarTip=[-1.64,.76,1.36])
    (OUT/'iteration-008.json').write_text(json.dumps(config,indent=2)+'\n')
    print('ROLLO_REFERENCE_SOURCE_PASS 008',flush=True)
