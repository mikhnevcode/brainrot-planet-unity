"""Bake/rig/export the inspected v06 mesh; keep historical v05 outputs intact."""
import sys,json
from pathlib import Path
import bpy
BASE=Path(__file__).resolve().parent
sys.path.insert(0,str(BASE))
import export_rollo_v05 as E
import sculpt_rollo_v06 as A
import review_rollo_reference as R

E.A=A;E.OUT=A.OUT;E.MODEL='RolloBulk_v06';E.TEXTURES=E.UNITY/E.MODEL
E.CATALOG_REVISION=6;E.CATALOG_FILE='catalog-original26-v5.json'
POSE={'headRoll':-10.,'headYaw':5.,'leanForward':1.3705287221286306}
E.REST_TRANSFORM=lambda p:R.posed(p,POSE)


if __name__=='__main__':
    if '--catalog-only' in sys.argv:
        bpy.ops.wm.open_mainfile(filepath=str(E.OUT/(E.MODEL+'_mobile.blend')))
        E.skin_catalog(bpy.data.objects['RolloBulk_DetailedMobile']);print('ROLLO_SKIN_CATALOG_PASS revision=6',flush=True)
    else:E.main()
