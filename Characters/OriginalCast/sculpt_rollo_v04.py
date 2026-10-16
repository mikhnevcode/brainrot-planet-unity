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

OUT=Path(__file__).resolve().parent/'Models_v04'/'RolloBulk'
PROFILE=[(0,-.24,.10,.05,.06),(0,-.23,.28,.78,.59),(0,-.18,.65,1.30,.88),
         (0,-.05,1.12,1.43,1.00),(0,.03,1.68,1.37,.95),(0,.13,2.18,1.16,.84),
         (0,.20,2.64,.96,.72),(0,.23,2.96,.86,.62),(0,.24,3.18,.66,.47),
         (0,.24,3.33,.34,.25),(0,.24,3.38,.005,.005)]


def sculpt(x,z):
    return (.27*(S.gaussian(x,z,-.27,2.61,.27,.21)+S.gaussian(x,z,.27,2.61,.27,.21))
            +.16*S.gaussian(x,z,0,2.79,.23,.18)+.10*S.gaussian(x,z,0,2.30,.49,.15)
            -.10*(S.gaussian(x,z,-.45,2.96,.21,.14)+S.gaussian(x,z,.45,2.96,.21,.14))
            +.06*(S.gaussian(x,z,-.45,3.09,.25,.09)+S.gaussian(x,z,.45,3.09,.25,.09))
            -.11*S.gaussian(x,z,0,2.39,.25,.10))


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
    ex=side*.45;ez=2.96;ey=S.surface(ex,ez)
    # The globe is mostly inside the skull: its visible cap is naturally clipped
    # by the sculpt, rather than an outlined decal or an exposed round eyeball.
    # A tapered almond cap follows the eye socket; no exposed spherical button.
    verts=[];faces=[];rings=12;around=64
    for i in range(rings):
        r=max(.0001,i/(rings-1))
        for j in range(around):
            a=2*pi*j/around;dx=.137*cos(a)*r
            dz=(.132 if sin(a)>0 else .105)*sin(a)*r
            x=ex+dx;z=ez+dz+side*dx*.15
            verts.append((x,S.surface(x,z)+.006+.052*(1-r*r),z))
    for i in range(rings-1):
        for j in range(around):
            k=i*around+j;n=i*around+(j+1)%around;faces.append((k,n,n+around,k+around))
    faces.append(tuple(reversed(range(around))))
    obj=G.D.mesh('Inset almond seal eye',verts,faces,pupil,'Head')
    obj.data.materials.append(iris)
    for face in obj.data.polygons:
        if (rings-3)*around<=face.index<(rings-1)*around:face.material_index=1
    upper=[];lower=[]
    for j in range(25):
        a=pi*j/24;x=ex+.143*cos(a);z=ez+.136*sin(a)+side*(x-ex)*.15;upper.append((x,S.surface(x,z)+.002,z))
        z=ez-.108*sin(a)+side*(x-ex)*.15;lower.append((x,S.surface(x,z)+.001,z))
    G.tube('Soft embedded upper eyelid',upper,.015,skin,'Head')
    G.tube('Lower socket rim',lower,.007,skin,'Head')
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
    obj=G.D.mesh(name,verts,faces,mat,bone)
    # Open projected caps have no closed volume from which Blender can infer
    # "outside". Cycles renders their backs, Unity's opaque material does not.
    if sum(p.normal.y for p in obj.data.polygons)<0:
        for polygon in obj.data.polygons:polygon.flip()
        obj.data.update()
    return obj


def make():
    G.reset({'main':'#349CA9','accent':'#F5780B'});S.BODY=None;S.PROFILE=PROFILE
    skin=S.seal_material();skin.name='Orig26 main #349CA9 skin'
    skin_bs=skin.node_tree.nodes.get('Principled BSDF');belly=skin_bs.inputs['Base Color'].links[0].from_node
    skin_bs.inputs['Roughness'].default_value=.40;skin_bs.inputs['Coat Weight'].default_value=.16
    coord=next(n for n in skin.node_tree.nodes if n.type=='SEPXYZ')
    mx=S.mathnode(skin,'DIVIDE',S.mathnode(skin,'SUBTRACT',coord.outputs['X'],.5),.30)
    mz=S.mathnode(skin,'DIVIDE',S.mathnode(skin,'SUBTRACT',coord.outputs['Z'],2.61/3.36),.32/3.36)
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
        G.ell('Whisker-pad sculpt volume',(side*.27,1.13,2.61),(.35,.30,.25),skin,'Body',48,32)
    for side,label in [(-1,'L'),(1,'R')]:
        S.blade('Fleshy foreflipper '+label,[(side*.98,.34,2.18),(side*1.22,.55,2.02),(side*1.50,.76,1.66),(side*1.67,.80,1.39),(side*1.65,.80,1.31)], [.20,.37,.35,.18,.002],.145,'Arm_'+label,skin)
    G.loft('Tail joins pelvis',[(.36,-.90,.56,.38,.23),(.76,-1.20,1.12,.28,.18),(.96,-1.36,1.60,.17,.12)],skin,'Tail',rows=34,around=32)
    for s in (-1,1):S.blade('Rounded webbed tail lobe',[(.96+s*.07,-1.33,1.53),(.96+s*.23,-1.39,1.79),(.96+s*.39,-1.43,2.13),(.96+s*.36,-1.43,2.25),(.96+s*.29,-1.42,2.29)],[.09,.23,.19,.12,.002],.085,'Tail',skin)
    # Fuse the shoulder and tail roots into the skin instead of separate paddles.
    skin_parts=[o for items in G.D.PARTS.values() for o in items if o.data.materials[0]==skin]
    bpy.ops.object.select_all(action='DESELECT')
    for obj in skin_parts:obj.select_set(True)
    bpy.context.view_layer.objects.active=body;bpy.ops.object.join()
    modifier=body.modifiers.new('Continuous sculpt volume','REMESH');modifier.mode='VOXEL';modifier.voxel_size=.014;modifier.use_smooth_shade=True
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    modifier=body.modifiers.new('Relax organic joins','SMOOTH');modifier.factor=.45;modifier.iterations=3;bpy.ops.object.modifier_apply(modifier=modifier.name)
    G.D.PARTS={'Body':[body]};body.name='Rollo fused skin master';body.data.update();bpy.context.view_layer.update();S.BODY=body
    eye=G.material('eye','#1C140F','glass');eye.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.29
    eye.node_tree.nodes.get('Principled BSDF').inputs['Specular IOR Level'].default_value=.20
    eye.node_tree.nodes.get('Principled BSDF').inputs['Coat Weight'].default_value=.12
    iris=G.material('iris','#543A29','glass');nose=G.material('nose','#302823','glass');nose.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.34
    for s in (-1,1):
        eye_cap(s,skin,iris,eye)
        # Brows follow the skull, with a tapered matte ridge.
        points=[]
        for j in range(9):
            t=j/8;x=s*(.29+.29*t);z=3.13+.06*sin(pi*t);points.append((x,S.surface(x,z)+.005,z))
        G.tube('Tapered expressive brow',points,[.002,.012,.023,.027,.027,.023,.016,.009,.001],dark,'Head')
    # A dark nose cap fully embedded in the actual remeshed face.
    nose_obj=S.sculpted_nose(nose)
    for v in nose_obj.data.vertices:
        z=v.co.z+.21;v.co.z=z;v.co.y=S.surface(v.co.x,z)+max(0,v.co.y-S.surface(v.co.x,z-.21))
    nose_obj.data.update()
    mouthmat=G.material('mouth','#42262B','solid')
    projected_patch('Open smiling mouth',[(-.255,2.48),(-.21,2.35),(0,2.265),(.21,2.35),(.255,2.48),(.11,2.45),(0,2.43),(-.11,2.45)],mouthmat)
    tongue=G.material('tongue','#E77D83','skin')
    projected_patch('Soft rounded tongue',[(-.125,2.38),(-.10,2.29),(0,2.27),(.10,2.29),(.125,2.38),(0,2.405)],tongue,offset=.028)
    G.tube('Tongue crease',[(0,S.surface(0,z)+.030,z) for z in (2.37,2.335,2.30)],.002,G.material('tongueShade','#BF525C','solid'),'Head')
    G.tube('Short philtrum',[(0,S.surface(0,z)+.003,z) for z in (2.62,2.57,2.51,2.47)],.005,dark,'Head')
    rng=random.Random(306)
    for s in (-1,1):
        for j in range(22):
            x=s*(.12+rng.random()*.30);z=2.48+rng.random()*.23
            G.ell('Painted flush whisker pore',(x,S.surface(x,z)+.0015,z),(.005+rng.random()*.003,.0015,.006),dark,'Head',10,8)
        for j in range(6):
            x=s*(.17+.023*j);z=2.65-j*.028;y=S.surface(x,z)
            G.tube('Fine swept ivory whisker',[(x,y+.010,z),(s*.58,y+.085,z+.10-j*.022),(s*(.92+j*.04),y+.07,z+.14-j*.075),(s*(1.13+j*.03),y+.025,z+.10-j*.105)], [.0058,.0046,.0024,.0004],ivory,'Head')
    center=(0,.79,.55);G.BONES['Appendage_0']=center
    # Axial coordinates must stay monotonic. The generic cubic loft overshot
    # its short bevel controls and buried both ivory discs inside the drum.
    vertices=[];faces=[];uv=[];rows=86;around=128
    for i in range(rows):
        t=i/(rows-1);x=-1.13+2.26*t;edge=min(1,(1.13-abs(x))/.065)
        radius=.48+.06*edge*edge*(3-2*edge)
        for j in range(around):
            a=2*pi*j/around;vertices.append((x,.79+radius*cos(a),.55+radius*sin(a)));uv.append((j/around,t))
    for i in range(rows-1):
        for j in range(around):
            k=i*around+j;n=i*around+(j+1)%around;faces.append((k,n,n+around,k+around))
    faces.extend([tuple(reversed(range(around))),tuple((rows-1)*around+j for j in range(around))])
    G.D.mesh('Porous orange paint drum',vertices,faces,foam,'Appendage_0',uv)
    for s in (-1,1):
        G.loft('Ivory roller cheek guard',[(s*1.15,.79,.55,.35,.35),(s*1.20,.79,.55,.38,.38),(s*1.25,.79,.55,.35,.35)],ivory,rows=15,around=64)
        G.loft('Rounded guard attachment',[(s*1.20,.77,.59,.16,.13),(s*1.20,.70,1.02,.15,.12),(s*1.12,.54,1.30,.10,.08),(s*1.07,.48,1.35,.01,.01)],ivory,rows=32,around=24)
        G.loft('Bright orange axle hub',[(s*1.245,.79,.55,.18,.18),(s*1.30,.79,.55,.18,.18),(s*1.33,.79,.55,.14,.14)],foam,'Appendage_0',rows=12,around=48)
        G.loft('Axle center',[(s*1.33,.79,.55,.08,.08),(s*1.35,.79,.55,.075,.075)],foam,'Appendage_0',rows=5,around=32)
    G.BONES.update({'Body':(0,0,1.35),'Head':(0,.20,2.63),'Arm_L':(-1.04,.38,2.13),'Arm_R':(1.04,.38,2.13),'Tail':(0,-.78,.70)})
    return body


def studio():
    S.OUT=OUT
    S.studio(prefix='RolloBulk_v04',standard=True)


if __name__=='__main__':
    OUT.mkdir(parents=True,exist_ok=True);make()
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'RolloBulk_v04_master.blend'))
    report={'status':'detailed-master-before-mobile-bake','triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in bpy.context.scene.objects if o.type=='MESH'),'geometry':'Fused torso/foreflippers/tail; inset almond eyes; projected facial details','reference':'OriginalConcepts_20261015_v1/01-rollo-bulk.png'}
    (OUT/'report.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    studio();print('ROLLO_V04_MASTER_RENDER_PASS',flush=True)
