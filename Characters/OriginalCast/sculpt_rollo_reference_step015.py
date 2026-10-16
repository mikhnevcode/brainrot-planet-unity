"""015: distinguish lower head/jaw from chest with a short, continuous neck."""
import sys,json
from pathlib import Path
import bpy
BASE=Path(__file__).resolve().parent
sys.path.insert(0,str(BASE))
import sculpt_rollo_v06 as V
import sculpt_rollo_v05 as A
OUT=BASE/'ArtReview/RolloReferenceMatch'
PROFILE=[(0,-.24,.10,.05,.06),(0,-.22,.30,.82,.62),(0,-.10,.70,1.30,.94),
         (0,.12,1.13,1.43,1.04),(0,.13,1.67,1.36,1.00),(0,.20,2.16,1.19,.88),
         (0,.17,2.34,1.00,.70),(0,.20,2.48,.87,.64),
         (0,.28,2.68,.96,.72),(0,.32,2.96,.94,.67),(0,.34,3.18,.75,.51),
         (0,.34,3.36,.40,.29),(0,.34,3.42,.005,.005)]


def make():
    A.PROFILE=PROFILE
    return V.make()


if __name__=='__main__':
    make();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'source-015.blend'))
    import review_rollo_reference as R
    config=json.loads((OUT/'iteration-014.json').read_text())
    config['master']='ArtReview/RolloReferenceMatch/source-015.blend'
    config['changed']=['Neck/head transition only: narrower neck below a wider jaw, continuous fused skin. Roller/flippers/face details/material/camera unchanged.']
    config['sourceLandmarks'].update(R.landmarks())
    (OUT/'iteration-015.json').write_text(json.dumps(config,indent=2)+'\n')
    print('ROLLO_REFERENCE_SOURCE_PASS 015',flush=True)
