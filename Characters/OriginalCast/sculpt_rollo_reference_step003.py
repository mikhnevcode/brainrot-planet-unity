"""Reference iteration 003: change only the near foreflipper's control path."""
import sys,json
from pathlib import Path
import bpy

BASE=Path(__file__).resolve().parent
sys.path.insert(0,str(BASE))
import sculpt_rollo_v05 as A

OUT=BASE/'ArtReview/RolloReferenceMatch'
original_flipper=A.flipper


def reference_flipper(name,points,widths,thickness,bone,material):
    if name=='Anatomical foreflipper R':
        points=[(.72,.86,2.14),(.94,1.01,2.00),(1.32,.99,1.75),(1.62,.86,1.50),(1.70,.79,1.42)]
        widths=[.22,.30,.30,.15,.002];thickness=.16
    return original_flipper(name,points,widths,thickness,bone,material)


if __name__=='__main__':
    A.flipper=reference_flipper;A.make();OUT.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'source-003.blend'))
    report={'status':'art-review-only-not-runtime','changed':['Near foreflipper control path and section widths only'],
            'unchanged':['Head and facial sculpt','Far flipper','Roller','Materials','Reference camera'],
            'replaces':'Rejected local-grab experiment 002, which pinched the shoulder surface.'}
    (OUT/'source-003-report.json').write_text(json.dumps(report,indent=2)+'\n')
    print('ROLLO_REFERENCE_SOURCE_PASS 003',flush=True)
