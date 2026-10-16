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

OUT=Path(__file__).resolve().parent/'Models_v05'/'RolloBulk'
PROFILE=[(0,-.24,.10,.05,.06),(0,-.22,.30,.82,.62),(0,-.10,.70,1.30,.94),
         (0,.12,1.13,1.43,1.04),(0,.13,1.67,1.36,1.00),(0,.20,2.16,1.19,.88),
         (0,.28,2.62,1.04,.76),(0,.32,2.96,.94,.67),(0,.34,3.18,.75,.51),
         (0,.34,3.36,.40,.29),(0,.34,3.42,.005,.005)]


def sculpt(x,z):
    # Deliberate anatomy fields: cheek pads, eye sockets, brow hood, recessed
    # mouth, lower lip and the soft roll of belly resting over the drum.
    return (.19*(S.gaussian(x,z,-.28,2.57,.31,.25)+S.gaussian(x,z,.28,2.57,.31,.25))
            +.10*S.gaussian(x,z,0,2.77,.25,.18)
            -.085*(S.gaussian(x,z,-.46,2.96,.22,.18)+S.gaussian(x,z,.46,2.96,.22,.18))
            +.105*(S.gaussian(x,z,-.46,3.115,.27,.095)+S.gaussian(x,z,.46,3.115,.27,.095))
            -.24*S.gaussian(x,z,0,2.34,.235,.14)
            +.065*S.gaussian(x,z,0,2.16,.37,.085)
            +.16*S.gaussian(x,z,0,1.15,.87,.25))


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
    ex=side*.46;ez=2.96;ey=S.surface(ex,ez)
    # A genuinely convex cornea seated inside the sculpted socket. The v04
    # surface-following cap inherited the concave socket and reflected like
    # metallic sunglasses. A tangent plane plus spherical dome avoids that.
    dx=.012;dz=.012
    sx=(S.surface(ex+dx,ez)-S.surface(ex-dx,ez))/(2*dx)
    sz=(S.surface(ex,ez+dz)-S.surface(ex,ez-dz))/(2*dz)
    verts=[];faces=[];rings=16;around=64
    for i in range(rings):
        r=max(.0001,i/(rings-1))
        for j in range(around):
            a=2*pi*j/around;xoff=.142*cos(a)*r
            zoff=(.148 if sin(a)>0 else .120)*sin(a)*r+side*xoff*.10
            y=ey+sx*xoff+sz*zoff+.100*sqrt(max(0,1-r*r))-.025
            verts.append((ex+xoff,y,ez+zoff))
    for i in range(rings-1):
        for j in range(around):
            k=i*around+j;n=i*around+(j+1)%around;faces.append((k,n,n+around,k+around))
    faces.append(tuple(reversed(range(around))))
    obj=G.D.mesh('Convex cornea inside sculpted socket',verts,faces,pupil,'Head')
    obj.data.materials.append(iris)
    for face in obj.data.polygons:
        if (rings-4)*around<=face.index<(rings-1)*around:face.material_index=1
    # The upper lid is a soft hood, not a complete bright outline around an eye.
    points=[]
    for j in range(25):
        a=pi*j/24;x=ex+.153*cos(a);z=ez+.147*sin(a)+side*(x-ex)*.10
        points.append((x,S.surface(x,z)+.003,z))
    G.tube('Soft hooded upper eyelid',points,[.003+.017*sin(pi*j/24) for j in range(25)],skin,'Head')
    return obj


def projected_patch(name,outline,mat,bone='Head',offset=.010,surface=None):
    # Dense projected cap, not a flat polygon hovering in front of the muzzle.
    surface=surface or S.surface
    cx=sum(p[0] for p in outline)/len(outline);cz=sum(p[1] for p in outline)/len(outline)
    verts=[];faces=[];around=80;rows=12
    for i in range(rows):
        r=max(.0001,i/(rows-1))
        for j in range(around):
            t=j/around*len(outline);k=int(t);s=t-k;p,a,b,n=[outline[q%len(outline)] for q in (k-1,k,k+1,k+2)]
            x,z=[.5*(2*a[c]+(-p[c]+b[c])*s+(2*p[c]-5*a[c]+4*b[c]-n[c])*s*s+(-p[c]+3*a[c]-3*b[c]+n[c])*s*s*s) for c in range(2)]
            x=cx+(x-cx)*r;z=cz+(z-cz)*r;verts.append((x,surface(x,z)+offset*(1-.5*r*r),z))
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


def seal_material():
    mat=G.material('main','#4096A0','skin');bs=mat.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Coat Weight'].default_value=.16;bs.inputs['Coat Roughness'].default_value=.30
    bs.inputs['Subsurface Weight'].default_value=.045
    coord=S.node(mat,'ShaderNodeTexCoord');normalized=S.node(mat,'ShaderNodeVectorMath');normalized.operation='MULTIPLY_ADD'
    normalized.inputs[1].default_value=(1/3.0,1/2.4,1/3.42);normalized.inputs[2].default_value=(.5,.5,0)
    S.link(mat,coord.outputs['Object'],normalized.inputs[0]);pos=normalized.outputs[0]
    xyz=S.node(mat,'ShaderNodeSeparateXYZ');S.link(mat,pos,xyz.inputs[0])
    def ellipse(cx,cz,rx,rz):
        x=S.mathnode(mat,'DIVIDE',S.mathnode(mat,'SUBTRACT',xyz.outputs['X'],cx),rx)
        z=S.mathnode(mat,'DIVIDE',S.mathnode(mat,'SUBTRACT',xyz.outputs['Z'],cz),rz)
        inside=S.mathnode(mat,'SUBTRACT',1,S.mathnode(mat,'ADD',S.mathnode(mat,'MULTIPLY',x,x),S.mathnode(mat,'MULTIPLY',z,z)))
        return S.ramp(mat,inside,[(0,0),(.23,1)])
    belly_mask=ellipse(.5,.415,.405,.385)
    muzzle_mask=ellipse(.5,2.565/3.42,.235,.315/3.42)
    front=S.ramp(mat,xyz.outputs['Y'],[(.67,0),(.91,1)])
    mask=S.mathnode(mat,'MULTIPLY',S.mathnode(mat,'MAXIMUM',belly_mask,muzzle_mask),front)
    broad=S.node(mat,'ShaderNodeTexNoise',Scale=5.3,Detail=3.1,Roughness=.67);S.link(mat,pos,broad.inputs['Vector'])
    base=S.ramp(mat,broad.outputs['Fac'],[(.10,'#2E7C86'),(.90,'#4DA5AB')])
    distort=S.node(mat,'ShaderNodeTexNoise',Scale=8.5,Detail=3.0,Roughness=.7);S.link(mat,pos,distort.inputs['Vector'])
    offset=S.node(mat,'ShaderNodeVectorMath');offset.operation='MULTIPLY_ADD';S.link(mat,distort.outputs['Color'],offset.inputs[0]);offset.inputs[1].default_value=(.12,.12,.12);offset.inputs[2].default_value=(-.06,-.06,-.06)
    warped=S.node(mat,'ShaderNodeVectorMath');warped.operation='ADD';S.link(mat,pos,warped.inputs[0]);S.link(mat,offset.outputs[0],warped.inputs[1])
    spots=S.node(mat,'ShaderNodeTexVoronoi',Scale=17.0);S.link(mat,warped.outputs[0],spots.inputs['Vector'])
    stain=S.ramp(mat,spots.outputs['Distance'],[(.135,1),(.205,0)])
    variable=S.ramp(mat,broad.outputs['Fac'],[(.2,.50),(.8,.90)])
    mix=S.node(mat,'ShaderNodeMixRGB');S.link(mat,S.mathnode(mat,'MULTIPLY',stain,variable),mix.inputs[0]);S.link(mat,base,mix.inputs[1]);mix.inputs[2].default_value=(*S.rgb('#285F6D'),1)
    cream=S.node(mat,'ShaderNodeMixRGB');cream.inputs[1].default_value=(*S.rgb('#D9CBB0'),1);cream.inputs[2].default_value=(*S.rgb('#C7D8C9'),1)
    S.link(mat,S.ramp(mat,xyz.outputs['Z'],[(.64,0),(.82,1)]),cream.inputs[0])
    # The final mix stays explicit: export uses its factor as a palette mask.
    belly=S.node(mat,'ShaderNodeMixRGB');S.link(mat,mask,belly.inputs[0]);S.link(mat,mix.outputs[0],belly.inputs[1]);S.link(mat,cream.outputs[0],belly.inputs[2]);S.link(mat,belly.outputs[0],bs.inputs['Base Color'])
    rough=S.ramp(mat,mask,[(0,.38),(1,.62)]);S.link(mat,rough,bs.inputs['Roughness'])
    stretch=S.node(mat,'ShaderNodeVectorMath');stretch.operation='MULTIPLY';S.link(mat,pos,stretch.inputs[0]);stretch.inputs[1].default_value=(1.2,1.1,.9)
    micro=S.node(mat,'ShaderNodeTexNoise',Scale=180.,Detail=2.5,Roughness=.7);S.link(mat,stretch.outputs[0],micro.inputs['Vector'])
    bump=S.node(mat,'ShaderNodeBump',Strength=.27,Distance=.014);S.link(mat,micro.outputs['Fac'],bump.inputs['Height']);S.link(mat,bump.outputs[0],bs.inputs['Normal'])
    return mat


def flipper(name,points,widths,thickness,bone,mat):
    # Cross-sections follow the curved XZ centreline, not the global X axis.
    # This gives a continuous shoulder and a flattened leaf-shaped distal web.
    controls=[(*p,w) for p,w in zip(points,widths)];verts=[];faces=[];uv=[];rows=64;around=40
    for i in range(rows):
        t=i/(rows-1);x,y,z,w=G.D.interpolate(controls,t)
        a=Vector(G.D.interpolate(controls,max(0,t-.002))[:3]);b=Vector(G.D.interpolate(controls,min(1,t+.002))[:3]);d=b-a
        cross=Vector((-d.z,0,d.x)).normalized()
        depth=thickness*(.32+.68*sin(pi*t)**.65)
        for j in range(around):
            angle=2*pi*j/around;v=Vector((x,y,z))+cross*(w*cos(angle))
            v.y+=depth*sin(angle);verts.append(tuple(v));uv.append((j/around,t))
    for i in range(rows-1):
        for j in range(around):
            k=i*around+j;n=i*around+(j+1)%around;faces.append((k,n,n+around,k+around))
    faces.extend([tuple(reversed(range(around))),tuple((rows-1)*around+j for j in range(around))])
    return G.D.mesh(name,verts,faces,mat,bone,uv)


def sculpt_surface_pass(body):
    # Local mesh deformation after fusion: shoulder creases, soft rolls and
    # very low-amplitude skin tension. This is not a displacement of spheres.
    # Cache before editing: asking Blender for another normal after each vertex
    # move would invalidate and recompute the whole dense surface each time.
    normals=[v.normal.copy() for v in body.data.vertices]
    for vertex,normal in zip(body.data.vertices,normals):
        x,y,z=vertex.co;ax=abs(x)
        front=max(0,min(1,(y-.20)/.60))
        crease=exp(-((ax-1.12)/.30)**4)*exp(-((z-(2.16-.92*(ax-1.03)))/.038)**2)
        lower=exp(-(x/.98)**4)*exp(-((z-(1.12+.035*cos(x*2.5)))/.055)**2)
        displacement=front*(-.026*crease)
        displacement+=.0013*sin(z*38+x*13)*sin(y*29-x*8)
        vertex.co+=normal*displacement
    body.data.update()


def seal_nose(material,nostril):
    outline=[(-.17,2.69),(-.105,2.73),(0,2.71),(.105,2.73),(.17,2.69),(.105,2.60),(0,2.54),(-.105,2.60)]
    verts=[];faces=[];rows=18;around=80
    for i in range(rows):
        r=max(.001,i/(rows-1))
        for j in range(around):
            t=j/around*len(outline);k=int(t);f=t-k;p,a,b,n=[outline[q%len(outline)] for q in (k-1,k,k+1,k+2)]
            x,z=[.5*(2*a[c]+(-p[c]+b[c])*f+(2*p[c]-5*a[c]+4*b[c]-n[c])*f*f+(-p[c]+3*a[c]-3*b[c]+n[c])*f*f*f) for c in range(2)]
            x*=r;z=2.65+(z-2.65)*r;verts.append((x,S.surface(x,z)+.125*sqrt(max(0,1-r*r))-.008,z))
    for i in range(rows-1):
        for j in range(around):
            k=i*around+j;n=i*around+(j+1)%around;faces.append((k,n,n+around,k+around))
    faces.extend([tuple(reversed(range(around))),tuple((rows-1)*around+j for j in range(around))])
    obj=G.D.mesh('Triangular seal nose with nostrils',verts,faces,material,'Head')
    for side in (-1,1):
        v=G.ell('Small embedded nostril',(side*.075,S.surface(side*.075,2.68)+.105,2.68),(.025,.012,.039),nostril,'Head',24,16)
        v.rotation_euler.y=side*.25
    return obj


def make():
    G.reset({'main':'#4096A0','accent':'#F5780B'});S.BODY=None;S.PROFILE=PROFILE
    skin=seal_material()
    foam=S.foam_material();foam.name='Orig26 accent #F5780B dough'
    bs=foam.node_tree.nodes.get('Principled BSDF');bs.inputs['Coat Weight'].default_value=0;bs.inputs['Roughness'].default_value=.64
    for node in foam.node_tree.nodes:
        if node.type=='TEX_NOISE':node.inputs['Scale'].default_value=80.
        if node.type=='BUMP':node.inputs['Strength'].default_value=.6;node.inputs['Distance'].default_value=.03
    ivory=G.material('ceramic','#D9D3BE','solid');dark=G.material('crease','#1C3639','solid')
    body=torso(skin)
    for side in (-1,1):
        G.ell('Whisker-pad sculpt volume',(side*.28,1.20,2.565),(.365,.275,.275),skin,'Body',48,32)
    for side,label in [(-1,'L'),(1,'R')]:
        flipper('Anatomical foreflipper '+label,[(side*.97,.54,2.15),(side*1.25,.76,1.98),(side*1.58,.94,1.70),(side*1.74,.97,1.45),(side*1.76,.96,1.37)], [.24,.29,.25,.14,.002],.18,'Arm_'+label,skin)
    G.loft('Tail joins pelvis',[(.45,-.95,.69,.35,.23),(.86,-1.19,1.20,.25,.17),(.98,-1.29,1.62,.18,.13)],skin,'Tail',rows=34,around=32)
    for side in (-1,1):
        flipper('Broad webbed rear fluke',[(.98+side*.03,-1.27,1.56),(.98+side*.17,-1.30,1.77),(.98+side*.30,-1.29,1.99),(.98+side*.30,-1.28,2.10)],[.14,.21,.18,.002],.115,'Tail',skin)
    # Fuse the shoulder and tail roots into the skin instead of separate paddles.
    skin_parts=[o for items in G.D.PARTS.values() for o in items if o.data.materials[0]==skin]
    bpy.ops.object.select_all(action='DESELECT')
    for obj in skin_parts:obj.select_set(True)
    bpy.context.view_layer.objects.active=body;bpy.ops.object.join()
    modifier=body.modifiers.new('Continuous sculpt volume','REMESH');modifier.mode='VOXEL';modifier.voxel_size=.010;modifier.use_smooth_shade=True
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    modifier=body.modifiers.new('Relax organic joins','SMOOTH');modifier.factor=.45;modifier.iterations=3;bpy.ops.object.modifier_apply(modifier=modifier.name)
    sculpt_surface_pass(body)
    G.D.PARTS={'Body':[body]};body.name='Rollo reference anatomical sculpt';body.data.update();bpy.context.view_layer.update();S.BODY=body
    eye=G.material('eye','#100F0D','glass');eye.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.29
    eye.node_tree.nodes.get('Principled BSDF').inputs['Specular IOR Level'].default_value=.20
    eye.node_tree.nodes.get('Principled BSDF').inputs['Coat Weight'].default_value=.12
    iris=G.material('iris','#39291E','glass');nose=G.material('nose','#28201C','glass');nose.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.34
    for s in (-1,1):
        eye_cap(s,skin,iris,eye)
        # Brows follow the skull, with a tapered matte ridge.
        points=[]
        for j in range(9):
            t=j/8;x=s*(.28+.32*t);z=3.16+.065*sin(pi*t);points.append((x,S.surface(x,z)+.005,z))
        G.tube('Tapered expressive brow',points,[.002,.018,.032,.038,.039,.035,.025,.015,.001],dark,'Head')
    # A dark nose cap fully embedded in the actual remeshed face.
    nose_obj=seal_nose(nose,eye)
    mouthmat=G.material('mouth','#42262B','solid')
    projected_patch('Recessed open mouth',[(-.23,2.425),(-.19,2.30),(0,2.18),(.19,2.30),(.23,2.425),(.10,2.415),(0,2.395),(-.10,2.415)],mouthmat,surface=lambda x,z:1.24+.5*(z-2.25)-.12*(x/.25)**2)
    tongue=G.material('tongue','#CC626A','skin')
    G.loft('Rounded projecting tongue',[(0,1.255,2.205,.035,.025),(0,1.325,2.27,.125,.052),(0,1.305,2.36,.09,.018)],tongue,'Head',rows=28,around=32)
    G.tube('Tongue crease',[(0,1.374,2.255),(0,1.38,2.28),(0,1.352,2.31)],.0025,G.material('tongueShade','#9C3D47','solid'),'Head')
    G.tube('Short philtrum',[(0,S.surface(0,z)+.003,z) for z in (2.57,2.53,2.49,2.455)],.005,dark,'Head')
    rng=random.Random(306)
    for s in (-1,1):
        for j in range(22):
            x=s*(.12+rng.random()*.30);z=2.44+rng.random()*.25
            G.ell('Painted flush whisker pore',(x,S.surface(x,z)+.0015,z),(.009+rng.random()*.005,.002,.010),dark,'Head',10,8)
        for j in range(6):
            x=s*(.27+.035*j);z=2.65-j*.040;y=S.surface(x,z)
            G.tube('Fine swept ivory whisker',[(x,y+.010,z),(s*.65,y+.075,z+.11-j*.014),(s*(.98+j*.025),y+.04,z+.17-j*.060),(s*(1.14+j*.025),y-.045,z+.14-j*.095)], [.0052,.0042,.0020,.0004],ivory,'Head')
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
    G.BONES.update({'Body':(0,0,1.35),'Head':(0,.28,2.65),'Arm_L':(-1.01,.52,2.12),'Arm_R':(1.01,.52,2.12),'Tail':(0,-.78,.70)})
    return body


def studio():
    S.OUT=OUT
    S.studio(prefix='RolloBulk_v05',standard=True,samples=24 if '--quick' in sys.argv else 48,views=[('three-quarter',(4.5,8,3.0))] if '--quick' in sys.argv else None)


if __name__=='__main__':
    OUT.mkdir(parents=True,exist_ok=True);make()
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'RolloBulk_v05_master.blend'))
    report={'status':'detailed-master-before-mobile-bake','triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in bpy.context.scene.objects if o.type=='MESH'),'geometry':'Fused torso/foreflippers/tail; inset almond eyes; projected facial details','reference':'OriginalConcepts_20261015_v1/01-rollo-bulk.png'}
    (OUT/'report.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    studio();print('ROLLO_V05_MASTER_RENDER_PASS',flush=True)
