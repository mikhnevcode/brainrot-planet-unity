"""Step 006: seat the eyes on the curved face and round the tongue tip."""
import sys,json
from pathlib import Path
import bpy
BASE=Path(__file__).resolve().parent
sys.path.insert(0,str(BASE))
import sculpt_rollo_reference_step005 as F
A=F.A;G=F.G;S=F.S;OUT=F.OUT


def eye_cap(side,skin,iris,pupil):
    obj=F.eye_cap(side,skin,iris,pupil)
    # The lower rim used to disappear behind the raised muzzle. Match the
    # actual fused skin underneath the entire rim, retaining the cornea dome.
    for v in obj.data.vertices:
        r=min(1,((v.co.x-side*.35)/.118)**2+((v.co.z-3.035)/.127)**2)**.5
        v.co.y=max(v.co.y,S.surface(v.co.x,v.co.z)+.006+.072*(max(0,1-r*r)**.5))
    obj.data.update()
    return obj


def loft(name,points,mat=None,bone='Body',**kw):
    if name=='Rounded projecting tongue':
        return F.original_ell(name,(0,1.36,2.49),(.115,.049,.113),mat,bone,48,32)
    return F.loft(name,points,mat,bone,**kw)


def make():
    A.sculpt=F.sculpt;A.flipper=F.P.reference_flipper;A.eye_cap=eye_cap;A.seal_nose=F.seal_nose;A.projected_patch=F.projected_patch
    G.ell=F.ell;G.tube=F.tube;G.loft=loft
    return A.make()


if __name__=='__main__':
    make();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'source-006.blend'))
    import review_rollo_reference as R
    config=json.loads((OUT/'iteration-005.json').read_text())
    config['master']='ArtReview/RolloReferenceMatch/source-006.blend'
    config['changed']=['Eye cornea rims follow fused skin rather than being occluded by cheeks; solid rounded tongue. All other anatomy and reference camera unchanged.']
    config['sourceLandmarks'].update(R.landmarks())
    (OUT/'iteration-006.json').write_text(json.dumps(config,indent=2)+'\n')
    print('ROLLO_REFERENCE_SOURCE_PASS 006',flush=True)
