"""Rollo's detailed production candidate; author, bake and export actual 3D.

The continuous high-resolution skin is voxel fused, then a UV-unwrapped mobile
surface receives its color/normal bake. No concept image is projected onto it.
"""
import sys,math,json,random
from pathlib import Path
import bpy
from mathutils import Vector
from math import sin,cos,pi,exp,sqrt
sys.path.insert(0,str(Path(__file__).resolve().parent))
import geometry as G
import sculpt_rollo_v02 as S

OUT=Path(__file__).resolve().parent/'Models_v03'/'RolloBulk'
PROFILE=[(0,-.24,.10,.05,.06),(0,-.23,.28,.66,.53),(0,-.18,.65,1.09,.74),
         (0,-.08,1.18,1.26,.86),(0,.03,1.75,1.21,.81),(0,.12,2.22,1.04,.73),
         (0,.17,2.68,.89,.61),(0,.20,2.98,.81,.53),(0,.21,3.22,.63,.42),
         (0,.21,3.39,.32,.23),(0,.21,3.44,.005,.005)]


def sculpt(x,z):
    return (.31*(S.gaussian(x,z,-.23,2.50,.215,.195)+S.gaussian(x,z,.23,2.50,.215,.195))
            +.14*S.gaussian(x,z,0,2.71,.18,.19)+.095*S.gaussian(x,z,0,2.20,.42,.12)
            -.065*(S.gaussian(x,z,-.41,2.89,.22,.17)+S.gaussian(x,z,.41,2.89,.22,.17))
            +.035*(S.gaussian(x,z,-.41,3.06,.25,.09)+S.gaussian(x,z,.41,3.06,.25,.09))
            -.065*S.gaussian(x,z,0,2.30,.23,.08)
            -.025*S.gaussian(x,z,0,2.06,.68,.045))


def torso(material):
    vertices=[];uv=[];faces=[];rows=150;around=144
    for i in range(rows):
        t=i/(rows-1);x,y,z,rx,ry=G.D.interpolate(PROFILE,t)
        for j in range(around):
            a=2*pi*j/around;xx=max(.001,rx)*cos(a);f=max(0,sin(a))
            yy=y+max(.001,ry)*sin(a)+f**3*sculpt(xx,z)
            vertices.append((xx,yy,z));uv.append((j/around,z/3.44))
    for i in range(rows-1):
        for j in range(around):k=i*around+j;n=i*around+(j+1)%around;faces.append((k,n,n+around,k+around))
    faces.extend([tuple(reversed(range(around))),tuple((rows-1)*around+j for j in range(around))])
    return G.D.mesh('Rollo continuous facial sculpt',vertices,faces,material,'Body',uv)


def eye_cap(side,skin,iris,pupil):
    ex=side*.405;ez=2.89;ey=S.surface(ex,ez)
    # The globe is mostly inside the skull: its visible cap is naturally clipped
    # by the sculpt, rather than an outlined decal or an exposed round eyeball.
    obj=G.ell('Deep-set seal eye',(ex,ey-.028,ez),(.158,.096,.145),pupil,'Head',48,32)
    upper=[];lower=[]
    for j in range(25):
        a=pi*j/24;x=ex+.17*cos(a);z=ez+.147*sin(a)*(.78+.22*sin(a));upper.append((x,S.surface(x,z)+.006,z))
        z=ez-.112*sin(a)*(.78+.22*sin(a));lower.append((x,S.surface(x,z)+.003,z))
    G.tube('Soft embedded upper eyelid',upper,.014,skin,'Head')
    return obj


def projected_patch(name,outline,mat,bone='Head',offset=.010):
    # Dense projected cap, not a flat polygon hovering in front of the muzzle.
    cx=sum(p[0] for p in outline)/len(outline);cz=sum(p[1] for p in outline)/len(outline)
    verts=[];faces=[];around=80;rows=12
    for i in range(rows):
        r=max(.0001,i/(rows-1))
        for j in range(around):
            t=j/around*len(outline);k=int(t);s=t-k;p,a,b,n=[outline[q%len(outline)] for q in (k-1,k,k+1,k+2)]
            x,z=[.5*(2*a[c]+(-p[c]+b[c])*s+(2*p[c]-5*a[c]+4*b[c]-n[c])*s*s+(-p[c]+3*a[c]-3*b[c]+n[c])*s*s*s) for c in range(2)]
            x=cx+(x-cx)*r;z=cz+(z-cz)*r;verts.append((x,S.surface(x,z)+offset*(1-.5*r*r),z))
    for i in range(rows-1):
        for j in range(around):k=i*around+j;n=i*around+(j+1)%around;faces.append((k,n,n+around,k+around))
    faces.append(tuple(reversed(range(around))))
    return G.D.mesh(name,verts,faces,mat,bone)


def make():
    G.reset({'main':'#349CA9','accent':'#F5780B'});S.BODY=None;S.PROFILE=PROFILE
    skin=S.seal_material();skin.name='Orig26 main #349CA9 skin'
    skin_bs=skin.node_tree.nodes.get('Principled BSDF');belly=skin_bs.inputs['Base Color'].links[0].from_node
    coord=next(n for n in skin.node_tree.nodes if n.type=='SEPXYZ')
    mx=S.mathnode(skin,'DIVIDE',S.mathnode(skin,'SUBTRACT',coord.outputs['X'],.5),.255)
    mz=S.mathnode(skin,'DIVIDE',S.mathnode(skin,'SUBTRACT',coord.outputs['Z'],2.50/3.36),.29/3.36)
    muzzle_inside=S.mathnode(skin,'SUBTRACT',1,S.mathnode(skin,'ADD',S.mathnode(skin,'MULTIPLY',mx,mx),S.mathnode(skin,'MULTIPLY',mz,mz)))
    muzzle_mask=S.mathnode(skin,'MULTIPLY',S.ramp(skin,muzzle_inside,[(0,0),(.24,1)]),S.ramp(skin,coord.outputs['Y'],[(.80,0),(.96,1)]))
    combined=S.mathnode(skin,'MAXIMUM',belly.inputs[0].links[0].from_socket,muzzle_mask);S.link(skin,combined,belly.inputs[0])
    # Cooler padded muzzle and warmer belly are painted onto the same fused skin.
    facecolor=S.node(skin,'ShaderNodeMixRGB');facecolor.inputs[1].default_value=(*S.rgb('#F0E2C7'),1);facecolor.inputs[2].default_value=(*S.rgb('#D3E5DD'),1)
    S.link(skin,S.ramp(skin,coord.outputs['Z'],[(.64,0),(.755,1)]),facecolor.inputs[0]);S.link(skin,facecolor.outputs[0],belly.inputs[2])
    foam=S.foam_material();foam.name='Orig26 accent #F5780B dough'
    bs=foam.node_tree.nodes.get('Principled BSDF');bs.inputs['Coat Weight'].default_value=0;bs.inputs['Roughness'].default_value=.64
    ivory=G.material('ceramic','#EAE3CE','solid');dark=G.material('crease','#29434A','solid')
    body=torso(skin)
    for side in (-1,1):
        G.ell('Whisker-pad sculpt volume',(side*.225,.95,2.50),(.295,.285,.23),skin,'Body',48,32)
    for side,label in [(-1,'L'),(1,'R')]:
        S.blade('Fleshy foreflipper '+label,[(side*.88,.26,2.03),(side*1.13,.43,1.88),(side*1.38,.60,1.56),(side*1.48,.64,1.38),(side*1.46,.64,1.33)], [.12,.27,.24,.10,.002],.092,'Arm_'+label,skin)
    G.loft('Tail joins pelvis',[(0,-.77,.52,.33,.18),(0,-1.11,.91,.25,.13),(0,-1.34,1.33,.13,.10)],skin,'Tail',rows=34,around=32)
    for s in (-1,1):S.blade('Rounded webbed tail lobe',[(s*.055,-1.30,1.27),(s*.19,-1.39,1.55),(s*.32,-1.43,1.91),(s*.29,-1.43,2.02),(s*.245,-1.42,2.065)],[.07,.19,.155,.09,.002],.065,'Tail',skin)
    # Fuse the shoulder and tail roots into the skin instead of separate paddles.
    skin_parts=[o for items in G.D.PARTS.values() for o in items if o.data.materials[0]==skin]
    bpy.ops.object.select_all(action='DESELECT')
    for obj in skin_parts:obj.select_set(True)
    bpy.context.view_layer.objects.active=body;bpy.ops.object.join()
    modifier=body.modifiers.new('Continuous sculpt volume','REMESH');modifier.mode='VOXEL';modifier.voxel_size=.014;modifier.use_smooth_shade=True
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    modifier=body.modifiers.new('Relax organic joins','SMOOTH');modifier.factor=.45;modifier.iterations=3;bpy.ops.object.modifier_apply(modifier=modifier.name)
    G.D.PARTS={'Body':[body]};body.name='Rollo fused skin master';body.data.update();bpy.context.view_layer.update();S.BODY=body
    eye=G.material('eye','#100F0E','glass');eye.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.21
    eye.node_tree.nodes.get('Principled BSDF').inputs['Specular IOR Level'].default_value=.20
    eye.node_tree.nodes.get('Principled BSDF').inputs['Coat Weight'].default_value=.12
    iris=G.material('iris','#543A29','glass');nose=G.material('nose','#302823','glass');nose.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.34
    for s in (-1,1):
        eye_cap(s,skin,iris,eye)
        # Brows follow the skull, with a tapered matte ridge.
        points=[]
        for j in range(9):
            t=j/8;x=s*(.27+.27*t);z=3.075+.05*sin(pi*t);points.append((x,S.surface(x,z)+.005,z))
        G.tube('Tapered expressive brow',points,[.002,.012,.023,.027,.027,.023,.016,.009,.001],dark,'Head')
    # A dark nose cap fully embedded in the actual remeshed face.
    nose_obj=S.sculpted_nose(nose)
    for v in nose_obj.data.vertices:
        z=v.co.z+.105;v.co.z=z;v.co.y=S.surface(v.co.x,z)+max(0,v.co.y-S.surface(v.co.x,z-.105))
    nose_obj.data.update()
    mouthmat=G.material('mouth','#42262B','solid')
    projected_patch('Open smiling mouth',[(-.235,2.37),(-.18,2.27),(0,2.205),(.18,2.27),(.235,2.37),(.10,2.35),(0,2.325),(-.10,2.35)],mouthmat)
    tongue=G.material('tongue','#E77D83','skin')
    projected_patch('Soft rounded tongue',[(-.095,2.28),(-.072,2.225),(0,2.205),(.072,2.225),(.095,2.28),(0,2.295)],tongue,offset=.028)
    G.tube('Tongue crease',[(0,S.surface(0,z)+.030,z) for z in (2.275,2.255,2.238)],.002,G.material('tongueShade','#BF525C','solid'),'Head')
    G.tube('Short philtrum',[(0,S.surface(0,z)+.003,z) for z in (2.51,2.46,2.40,2.36)],.005,dark,'Head')
    rng=random.Random(306)
    for s in (-1,1):
        for j in range(22):
            x=s*(.12+rng.random()*.30);z=2.37+rng.random()*.23
            G.ell('Painted flush whisker pore',(x,S.surface(x,z)+.0015,z),(.005+rng.random()*.003,.0015,.006),dark,'Head',10,8)
        for j in range(6):
            x=s*(.17+.023*j);z=2.53-j*.028;y=S.surface(x,z)
            G.tube('Fine swept ivory whisker',[(x,y+.010,z),(s*.58,y+.085,z+.10-j*.022),(s*(.92+j*.04),y+.07,z+.14-j*.075),(s*(1.13+j*.03),y+.025,z+.10-j*.105)], [.0058,.0046,.0024,.0004],ivory,'Head')
    center=(0,.73,.51);G.BONES['Appendage_0']=center
    G.loft('Porous orange paint drum',[(-1.00,.73,.51,.43,.43),(-.95,.73,.51,.505,.505),(.95,.73,.51,.505,.505),(1.00,.73,.51,.43,.43)],foam,'Appendage_0',rows=86,around=128)
    for s in (-1,1):
        G.loft('Ivory roller cheek guard',[(s*1.025,.73,.51,.27,.27),(s*1.07,.73,.51,.305,.305),(s*1.11,.73,.51,.27,.27)],ivory,rows=15,around=64)
        G.loft('Rounded guard attachment',[(s*1.07,.71,.55,.13,.10),(s*1.08,.63,.91,.125,.09),(s*.99,.48,1.16,.075,.055),(s*.95,.42,1.22,.01,.01)],ivory,rows=32,around=24)
        G.loft('Bright orange axle hub',[(s*1.105,.73,.51,.15,.15),(s*1.15,.73,.51,.15,.15),(s*1.18,.73,.51,.11,.11)],foam,'Appendage_0',rows=12,around=48)
        G.loft('Axle center',[(s*1.18,.73,.51,.065,.065),(s*1.19,.73,.51,.06,.06)],foam,'Appendage_0',rows=5,around=32)
    G.BONES.update({'Body':(0,0,1.35),'Head':(0,.18,2.55),'Arm_L':(-.93,.27,1.99),'Arm_R':(.93,.27,1.99),'Tail':(0,-.78,.70)})
    return body


def studio():
    S.OUT=OUT
    S.studio(prefix='RolloBulk_v03',standard=True)


if __name__=='__main__':
    OUT.mkdir(parents=True,exist_ok=True);make()
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'RolloBulk_v03_master.blend'))
    report={'status':'detailed-master-before-mobile-bake','triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in bpy.context.scene.objects if o.type=='MESH'),'geometry':'Fused torso/foreflippers/tail; inset almond eyes; projected facial details','reference':'OriginalConcepts_20261015_v1/01-rollo-bulk.png'}
    (OUT/'report.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    studio();print('ROLLO_V03_MASTER_RENDER_PASS',flush=True)
