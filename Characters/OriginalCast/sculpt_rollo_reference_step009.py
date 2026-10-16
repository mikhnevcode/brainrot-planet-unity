"""009: surface only. Larger irregular spotting and warmer, bounded belly."""
import sys,json
from pathlib import Path
import bpy
BASE=Path(__file__).resolve().parent
sys.path.insert(0,str(BASE))
import sculpt_rollo_reference_step008 as F
A=F.A;G=F.G;S=F.S;OUT=F.OUT
original_material=A.seal_material


def seal_material():
    mat=original_material();bs=mat.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Coat Weight'].default_value=.24;bs.inputs['Coat Roughness'].default_value=.28
    for node in mat.node_tree.nodes:
        if node.type=='MATH' and node.operation=='DIVIDE' and not node.inputs[1].is_linked:
            value=node.inputs[1].default_value
            for old,new in [(.405,.355),(.385,.365),(.235,.225),(.315/3.42,.255/3.42)]:
                if abs(value-old)<1e-5:node.inputs[1].default_value=new
        elif node.type=='MATH' and node.operation=='SUBTRACT' and not node.inputs[1].is_linked:
            if abs(node.inputs[1].default_value-2.565/3.42)<1e-5:node.inputs[1].default_value=2.74/3.42
        elif node.type=='TEX_VORONOI':node.inputs['Scale'].default_value=10.5
        elif node.type=='BUMP':
            node.inputs['Strength'].default_value=.18;node.inputs['Distance'].default_value=.009
        elif node.type=='VALTORGB':
            stops=node.color_ramp.elements
            if abs(stops[0].position-.135)<1e-5:
                stops[0].position=.19;stops[1].position=.31
            elif abs(stops[0].color[0]-.38)<1e-5 and abs(stops[1].color[0]-.62)<1e-5:
                stops[0].color=(.30,.30,.30,1);stops[1].color=(.52,.52,.52,1)
        elif node.type=='MIX_RGB' and not node.inputs[1].is_linked and not node.inputs[2].is_linked:
            # The only unlinked color pair is warm belly-to-muzzle cream.
            node.inputs[1].default_value=(*S.rgb('#E5D2AE'),1)
            node.inputs[2].default_value=(*S.rgb('#DDD7C1'),1)
    return mat


def make():
    A.seal_material=seal_material
    return F.make()


if __name__=='__main__':
    make();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'source-009.blend'))
    config=json.loads((OUT/'iteration-008.json').read_text())
    config['master']='ArtReview/RolloReferenceMatch/source-009.blend'
    config['changed']=['Material only: broader irregular dark markings, warmer cream, narrower belly mask, shifted muzzle mask and wet skin finish. Geometry/pose/camera unchanged.']
    (OUT/'iteration-009.json').write_text(json.dumps(config,indent=2)+'\n')
    print('ROLLO_REFERENCE_SOURCE_PASS 009',flush=True)
