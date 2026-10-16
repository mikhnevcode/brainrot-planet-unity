"""007: separate the upper muzzle anatomically from the lower eyelids.

006 is rejected: sampling the fused face hit the cheek above the eye rim and
folded the eye cap. Restore the convex cap and lower/flatten the actual pads.
"""
import sys,json,math
from pathlib import Path
import bpy
BASE=Path(__file__).resolve().parent
sys.path.insert(0,str(BASE))
import sculpt_rollo_reference_step005 as F
A=F.A;G=F.G;S=F.S;OUT=F.OUT


def sculpt(x,z):
    g=S.gaussian
    return F.sculpt(x,z)-.16*(g(x,z,-.24,2.79,.30,.23)+g(x,z,.24,2.79,.30,.23))+.14*(g(x,z,-.24,2.74,.30,.19)+g(x,z,.24,2.74,.30,.19))


def ell(name,loc,size,mat=None,bone='Body',segments=24,rings=14):
    if name=='Whisker-pad sculpt volume':
        return F.original_ell(name,(math.copysign(.24,loc[0]),1.17,2.74),(.32,.25,.195),mat,bone,segments,rings)
    return F.ell(name,loc,size,mat,bone,segments,rings)


def loft(name,points,mat=None,bone='Body',**kw):
    if name=='Rounded projecting tongue':
        return F.original_ell(name,(0,1.37,2.535),(.105,.045,.09),mat,bone,48,32)
    return F.loft(name,points,mat,bone,**kw)


def patch(name,outline,mat,bone='Head',offset=.010,surface=None):
    if name=='Recessed open mouth':
        outline=[(-.17,2.65),(-.15,2.55),(0,2.44),(.15,2.55),(.17,2.65),(.08,2.64),(0,2.62),(-.08,2.64)]
        surface=lambda x,z:1.26+.42*(z-2.48)-.10*(x/.24)**2
        return F.original_patch(name,outline,mat,bone,offset,surface)
    return F.projected_patch(name,outline,mat,bone,offset,surface)


def make():
    A.sculpt=sculpt;A.flipper=F.P.reference_flipper;A.eye_cap=F.eye_cap;A.seal_nose=F.seal_nose;A.projected_patch=patch
    G.ell=ell;G.tube=F.tube;G.loft=loft
    return A.make()


if __name__=='__main__':
    make();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'source-007.blend'))
    import review_rollo_reference as R
    config=json.loads((OUT/'iteration-005.json').read_text())
    config['master']='ArtReview/RolloReferenceMatch/source-007.blend'
    config['changed']=['Flatten/lower actual upper muzzle to avoid eye intersection; restore convex eyes; smaller open mouth and rounded tongue. Other regions unchanged.']
    config['sourceLandmarks'].update(R.landmarks())
    (OUT/'iteration-007.json').write_text(json.dumps(config,indent=2)+'\n')
    print('ROLLO_REFERENCE_SOURCE_PASS 007',flush=True)
