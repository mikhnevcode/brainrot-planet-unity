"""014: increase the density of irregular seal markings, material only."""
import sys,json
from pathlib import Path
import bpy
BASE=Path(__file__).resolve().parent
sys.path.insert(0,str(BASE))
import sculpt_rollo_reference_step013 as F
OUT=F.OUT


def finish_spots(mat):
    for node in mat.node_tree.nodes:
        if node.type=='TEX_VORONOI':node.inputs['Scale'].default_value=15.5
        elif node.type=='VALTORGB':
            stops=node.color_ramp.elements
            if abs(stops[0].position-.20)<1e-5 and abs(stops[1].position-.245)<1e-5:
                stops[0].position=.29;stops[1].position=.35


if __name__=='__main__':
    bpy.ops.wm.open_mainfile(filepath=str(OUT/'source-013.blend'))
    body=next(o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith('Rollo reference anatomical'))
    finish_spots(body.data.materials[0]);bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'source-014.blend'))
    config=json.loads((OUT/'iteration-013.json').read_text());config['master']='ArtReview/RolloReferenceMatch/source-014.blend'
    config['changed']=['Markings only: denser broad irregular spots on teal skin. Shape/pose/camera unchanged.']
    (OUT/'iteration-014.json').write_text(json.dumps(config,indent=2)+'\n')
    print('ROLLO_REFERENCE_SOURCE_PASS 014',flush=True)
