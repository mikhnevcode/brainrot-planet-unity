"""011: refine the boundaries of the cream paint, preserve teal flippers."""
import sys,json
from pathlib import Path
import bpy
BASE=Path(__file__).resolve().parent
sys.path.insert(0,str(BASE))
import sculpt_rollo_reference_step010 as F
A=F.A;G=F.G;S=F.S;OUT=F.OUT


def finish_material(mat):
    nodes=mat.node_tree.nodes;bs=nodes.get('Principled BSDF')
    for node in list(nodes):
        if node.type=='MATH' and node.operation=='DIVIDE' and not node.inputs[1].is_linked:
            value=node.inputs[1].default_value
            if abs(value-.225)<1e-5:node.inputs[1].default_value=.17
            elif abs(value-.365)<1e-5:node.inputs[1].default_value=.405
        elif node.type=='MATH' and node.operation=='SUBTRACT' and not node.inputs[1].is_linked:
            if abs(node.inputs[1].default_value-.415)<1e-5:node.inputs[1].default_value=.44
        elif node.type=='BUMP':
            node.inputs['Strength'].default_value=.23;node.inputs['Distance'].default_value=.013
        elif node.type=='VALTORGB':
            stops=node.color_ramp.elements
            if abs(stops[0].position-.19)<1e-5:stops[0].position=.20;stops[1].position=.245
    xyz=next(n for n in nodes if n.type=='SEPXYZ')
    belly=bs.inputs['Base Color'].links[0].from_node;oldmask=belly.inputs[0].links[0].from_socket
    def ellipse(cx):
        x=S.mathnode(mat,'DIVIDE',S.mathnode(mat,'SUBTRACT',xyz.outputs['X'],cx),.12)
        z=S.mathnode(mat,'DIVIDE',S.mathnode(mat,'SUBTRACT',xyz.outputs['Z'],1.85/3.42),.145)
        d=S.mathnode(mat,'ADD',S.mathnode(mat,'MULTIPLY',x,x),S.mathnode(mat,'MULTIPLY',z,z))
        return S.ramp(mat,d,[(.55,1),(1,0)])
    flippers=S.mathnode(mat,'MAXIMUM',ellipse(.5+.99/3),ellipse(.5-.99/3))
    keep=S.mathnode(mat,'SUBTRACT',1,flippers)
    S.link(mat,S.mathnode(mat,'MULTIPLY',oldmask,keep),belly.inputs[0])
    return mat


def make():
    body=F.make();finish_material(body.data.materials[0]);return body


if __name__=='__main__':
    make();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'source-011.blend'))
    config=json.loads((OUT/'iteration-010.json').read_text())
    config['master']='ArtReview/RolloReferenceMatch/source-011.blend'
    config['changed']=['Cream surface boundaries only: connected chest, narrower whisker-pad marking, teal shoulder/flipper masks, crisper irregular spotting and finer wet skin relief.']
    (OUT/'iteration-011.json').write_text(json.dumps(config,indent=2)+'\n')
    print('ROLLO_REFERENCE_SOURCE_PASS 011',flush=True)
