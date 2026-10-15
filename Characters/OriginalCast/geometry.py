"""Original-cast Blender authoring primitives; no legacy meshes are loaded."""
import sys, math, random
from pathlib import Path
import bpy
from mathutils import Vector
from math import sin, cos, pi

BASE=Path(__file__).resolve().parent
sys.path.insert(0,str(BASE.parent))
import reference_detail_063 as D

PARTS=D.PARTS
MATERIALS={}
PALETTE={}
BONES={}

def rgb(value):
    return tuple(int(value[i:i+2],16)/255 for i in (1,3,5)) if isinstance(value,str) else tuple(value)

def material(role,color,kind='skin'):
    color=rgb(color);code=''.join(f'{round(max(0,min(1,c))*255):02X}' for c in color)
    key=f'Orig26 {role} #{code} {kind}'
    if key in MATERIALS:return MATERIALS[key]
    linear=tuple(c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4 for c in color)
    m=bpy.data.materials.new(key);m.diffuse_color=(*linear,1);m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*linear,1)
    bs.inputs['Roughness'].default_value=.30 if kind in ('glass','metal') else .56
    if kind=='metal':bs.inputs['Metallic'].default_value=.55
    # Deliberately solid volume: no accidental see-through body/backface holes.
    if kind=='glass':bs.inputs['Coat Weight'].default_value=.4
    if kind not in ('glass','solid'):
        nodes,links=m.node_tree.nodes,m.node_tree.links
        noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value={'cloth':95,'paper':80,'dough':45,'leaf':65,'metal':115}.get(kind,72)
        noise.inputs['Detail'].default_value=2
        bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.17;bump.inputs['Distance'].default_value=.026
        links.new(noise.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs['Normal'],bs.inputs['Normal'])
    MATERIALS[key]=m;return m

def reset(hero):
    global MATERIALS,PALETTE,BONES
    bpy.ops.wm.read_factory_settings(use_empty=True)
    D.PARTS={};D.MATERIALS={};MATERIALS={}
    PALETTE={'main':material('main',hero['main']),'accent':material('accent',hero['accent']),
      'cream':material('cream','#F8EBD4','solid'),'dark':material('dark','#253047','solid'),
      'iris':material('iris','#755338','glass'),'white':material('white','#FFF9E9','solid'),
      'metal':material('metal','#C2CED1','metal'),'gold':material('gold','#E6AE4A','metal')}
    BONES={'Body':(0,0,1.1),'Head':(0,.12,2.05),'Arm_L':(-.55,0,1.35),'Arm_R':(.55,0,1.35),
           'Leg_L':(-.34,0,.48),'Leg_R':(.34,0,.48),'Tail':(0,-.45,1.05)}
    for i in range(8):BONES[f'Appendage_{i}']=((i%2*2-1)*.45,(-.25 if i<4 else .25),.7)
    D.palette=lambda kind,color:material('detail',color,kind if kind in ('cloth','paper','dough','metal','glass','leaf') else 'skin')

def ell(name,loc,size,mat=None,bone='Body',segments=24,rings=14):
    return D.ell(name,loc,size,mat or PALETTE['main'],bone,segments,rings)
def tube(name,points,radius,mat=None,bone='Body'):
    return D.tube(name,points,radius,mat or PALETTE['main'],bone)
def loft(name,points,mat=None,bone='Body',rows=30,around=24,**kw):
    return D.loft(name,points,mat or PALETTE['main'],bone,rows=rows,around=around,**kw)
def ring(name,loc,radius,thickness,mat=None,bone='Body',axis='Z'):
    return D.torus(name,loc,radius,thickness,mat or PALETTE['accent'],bone,axis)
def box(name,loc,size,mat=None,bone='Body',bevel=.10):
    return D.box(name,loc,size,mat or PALETTE['main'],bone,bevel)
def fan(name,points,mat=None,bone='Body',thickness=.035,axis='Z'):
    return D.fan(name,points,thickness,mat or PALETTE['accent'],bone,axis)

def face(x=.24,y=.60,z=2.1,scale=1,bone='Head',smile=True):
    for side in (-1,1):
        xx=side*x
        ell('Sculpted eyelid',(xx,y-.025,z),(.19*scale,.08*scale,.22*scale),PALETTE['dark'],bone)
        ell('Warm eye white',(xx,y,z),(.168*scale,.07*scale,.196*scale),PALETTE['white'],bone)
        ell('Eye iris',(xx+side*.012,y+.060*scale,z),(.112*scale,.035*scale,.137*scale),PALETTE['iris'],bone)
        ell('Deep pupil',(xx+side*.012,y+.087*scale,z),(.070*scale,.018*scale,.106*scale),PALETTE['dark'],bone,20,12)
        ell('Eye highlight',(xx-.027*scale,y+.103*scale,z+.055*scale),(.025*scale,.009*scale,.028*scale),PALETTE['white'],bone,12,8)
        tube('Expressive curved eyebrow',[(xx-side*.13*scale,y-.005,z+.24*scale),(xx,y+.02,z+.285*scale),(xx+side*.13*scale,y-.03,z+.26*scale)],.026*scale,PALETTE['dark'],bone)
    if smile:
        tube('Sculpted smile crease',[(-.24*scale,y+.005,z-.34*scale),(0,y+.055,z-.43*scale),(.24*scale,y+.005,z-.34*scale)],.020*scale,PALETTE['dark'],bone)
        ell('Soft lower lip',(0,y+.012,z-.45*scale),(.15*scale,.022*scale,.035*scale),PALETTE['accent'],bone,20,10)

def limbs(legs=2,arms=True,feet=None):
    for side,label in [(-1,'L'),(1,'R')]:
        if arms:
            tube('Curved arm '+label,[(side*.5,0,1.55),(side*.76,.05,1.18),(side*.82,.16,1.1)],[.12,.105,.14],bone='Arm_'+label)
        if legs==2:
            tube('Stocky leg '+label,[(side*.33,0,.82),(side*.35,0,.4),(side*.35,.08,.23)],[.18,.145,.15],bone='Leg_'+label)
            ell('Rounded padded foot '+label,(side*.35,.17,.17),(.25,.35,.17),feet or PALETTE['main'],'Leg_'+label)
    if legs==4:
        for i,(x,y) in enumerate([(-.48,.4),(.48,.4),(-.48,-.43),(.48,-.43)]):
            b=f'Appendage_{i}';BONES[b]=(x,y,.7)
            tube('Quadruped leg',[(x,y,.86),(x,y,.4),(x,y+.05,.2)],[.19,.14,.16],bone=b)
            ell('Broad animal paw',(x,y+.12,.16),(.23,.31,.16),feet or PALETTE['main'],b)

def mammal(head=(0,.18,2.15),body=(0,-.05,1.1),body_size=(.72,.56,.85),muzzle=True,arms=True,legs=2):
    loft('Continuous rounded torso',[(body[0],body[1],body[2]-.75,.18,.2),(body[0],body[1],body[2]-.35,body_size[0],body_size[1]),(body[0],body[1],body[2]+.33,body_size[0]*.88,body_size[1]*.9),(head[0],head[1]-.08,head[2],.25,.27)],rows=40)
    ell('Sculpted head',head,(.64,.49,.61),bone='Head')
    if muzzle:ell('Broad organic muzzle',(head[0],head[1]+.40,head[2]-.17),(.45,.29,.26),PALETTE['cream'],'Head')
    face(.245,head[1]+.43,head[2]+.08)
    for side in (-1,1):ell('Rounded ear',(side*.56,head[1]-.04,head[2]+.39),(.16,.10,.22),bone='Head')
    limbs(legs,arms)

def leaf(name,start,tip,width=.24,mat=None,bone='Body'):
    a,b=Vector(start),Vector(tip);mid=(a+b)*.5
    cross=(b-a).cross(Vector((0,1,0))).normalized()*width
    if cross.length<.01:cross=Vector((width,0,0))
    obj=fan(name,[tuple(a),tuple(mid+cross),tuple(b),tuple(mid-cross)],mat or PALETTE['accent'],bone,axis='Y')
    tube(name+' central vein',[tuple(a),tuple(mid+Vector((0,.02,0))),tuple(b)],.009,PALETTE['cream'],bone)
    return obj

def coil(name,center,radius,height,turns,mat=None,bone='Body',thickness=.035):
    points=[(center[0]+radius*cos(t*turns*2*pi),center[1]+radius*sin(t*turns*2*pi),center[2]+height*t) for t in [i/39 for i in range(40)]]
    return tube(name,points,thickness,mat,bone)
