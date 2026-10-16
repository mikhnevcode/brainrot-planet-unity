"""018: soft short neck and a projecting paired, canine-like muzzle.

Retains the inspected tongue and eye/brow reconstruction from 016/017.
The over-constricted 015 neck is an experiment, not the production shape.
"""
import sys,json,math
from pathlib import Path
import bpy
BASE=Path(__file__).resolve().parent
sys.path.insert(0,str(BASE))
import sculpt_rollo_v06 as V
import sculpt_rollo_v05 as A
import sculpt_rollo_reference_step005 as C
import sculpt_rollo_reference_step007 as F
from sculpt_rollo_reference_step016 import replace_tongue
from sculpt_rollo_reference_step017 import replace_expression
G=V.G;S=V.S
OUT=BASE/'ArtReview/RolloReferenceMatch'
old_ell=F.ell;old_sculpt=F.sculpt
PROFILE=[(0,-.24,.10,.05,.06),(0,-.22,.30,.82,.62),(0,-.10,.70,1.30,.94),
         (0,.12,1.13,1.43,1.04),(0,.13,1.67,1.36,1.00),(0,.20,2.16,1.19,.88),
         (0,.17,2.34,1.13,.75),(0,.20,2.48,1.07,.68),
         (0,.28,2.68,1.02,.74),(0,.32,2.96,.94,.67),(0,.34,3.18,.75,.51),
         (0,.34,3.36,.40,.29),(0,.34,3.42,.005,.005)]


def snout_ell(name,loc,size,mat=None,bone='Body',segments=24,rings=14):
    if name=='Whisker-pad sculpt volume':
        return C.original_ell(name,(math.copysign(.25,loc[0]),1.20,2.715),(.335,.290,.210),mat,bone,segments,rings)
    return old_ell(name,loc,size,mat,bone,segments,rings)


def facial_fields(x,z):
    return old_sculpt(x,z)+.075*S.gaussian(x,z,0,2.88,.21,.14)+.095*S.gaussian(x,z,0,2.43,.52,.12)


def make():
    A.PROFILE=PROFILE;F.ell=snout_ell;F.sculpt=facial_fields
    body=V.make();replace_tongue();replace_expression(body)
    return body


if __name__=='__main__':
    make();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'source-018.blend'))
    import review_rollo_reference as R
    config=json.loads((OUT/'iteration-017.json').read_text());config['master']='ArtReview/RolloReferenceMatch/source-018.blend'
    config['changed']=['Soften the over-constricted neck; retain broad shoulder silhouette but recess throat below a forward jaw. Extend paired muzzle pads and bridge. Retain 016 tongue/017 expression.']
    config['sourceLandmarks'].update(R.landmarks())
    (OUT/'iteration-018.json').write_text(json.dumps(config,indent=2)+'\n')
    print('ROLLO_REFERENCE_SOURCE_PASS 018',flush=True)
