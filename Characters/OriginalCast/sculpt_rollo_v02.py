"""Reference-led Rollo art review. Does NOT replace the shipped prototype.

Continuous facial sculpt fields, anatomical flippers, painted belly/spotting,
small inset seal eyes and a proportionate foam roller. No shared cartoon face.
Run Blender --background --python-exit-code 1 --python this_file.py
"""
import sys,math,json,random
from pathlib import Path
import bpy
from mathutils import Vector
from math import sin,cos,pi,exp,sqrt
sys.path.insert(0,str(Path(__file__).resolve().parent))
import geometry as G

OUT=Path(__file__).resolve().parent/'ArtReview'/'RolloBulk_v02'
BODY=None
PROFILE=[(0,-.24,.12,.12,.16),(0,-.23,.38,.72,.60),(0,-.16,.78,1.10,.78),
         (0,-.07,1.32,1.16,.81),(0,.00,1.90,1.02,.70),(0,.13,2.47,.82,.58),
         (0,.15,2.93,.67,.46),(0,.14,3.18,.48,.33),(0,.14,3.32,.23,.16),(0,.14,3.36,.005,.005)]


def gaussian(x,z,cx,cz,rx,rz):return exp(-((x-cx)/rx)**2-((z-cz)/rz)**2)
def sculpt(x,z):
    return (.31*(gaussian(x,z,-.23,2.43,.255,.205)+gaussian(x,z,.23,2.43,.255,.205))
            +.085*gaussian(x,z,0,2.57,.20,.22)+.065*gaussian(x,z,0,2.08,.40,.14)
            -.105*(gaussian(x,z,-.35,2.80,.21,.20)+gaussian(x,z,.35,2.80,.21,.20))
            +.06*(gaussian(x,z,-.35,2.98,.25,.10)+gaussian(x,z,.35,2.98,.25,.10))
            -.085*gaussian(x,z,0,2.22,.23,.09))


def surface(x,z):
    if BODY is not None:
        hit,location,normal,index=BODY.ray_cast(Vector((x,5,z)),Vector((0,-1,0)),distance=10)
        if hit:return location.y
    lo,hi=0.,1.
    for _ in range(22):
        t=(lo+hi)/2;p=G.D.interpolate(PROFILE,t)
        if p[2]<z:lo=t
        else:hi=t
    _,cy,zz,rx,ry=G.D.interpolate(PROFILE,(lo+hi)/2)
    f=sqrt(max(0,1-(x/max(rx,.001))**2))
    return cy+ry*f+f**3*sculpt(x,z)


def node(mat,kind,**values):
    n=mat.node_tree.nodes.new(kind)
    for name,value in values.items():n.inputs[name].default_value=value
    return n
def link(mat,a,b):mat.node_tree.links.new(a,b)
def mathnode(mat,op,a,b=None):
    n=node(mat,'ShaderNodeMath');n.operation=op
    if isinstance(a,(int,float)):n.inputs[0].default_value=a
    else:link(mat,a,n.inputs[0])
    if b is not None:
        if isinstance(b,(int,float)):n.inputs[1].default_value=b
        else:link(mat,b,n.inputs[1])
    return n.outputs[0]
def rgb(hex):
    values=G.rgb(hex)
    return tuple(c/12.92 if c<.04045 else ((c+.055)/1.055)**2.4 for c in values)
def ramp(mat,socket,stops):
    n=node(mat,'ShaderNodeValToRGB');r=n.color_ramp
    for i,(position,color) in enumerate(stops):
        e=r.elements[i] if i<2 else r.elements.new(position);e.position=position
        e.color=(*rgb(color),1) if isinstance(color,str) else (color,color,color,1)
    link(mat,socket,n.inputs['Fac']);return n.outputs['Color']


def seal_material():
    mat=G.material('main','#3BA6B4','skin');bs=mat.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Roughness'].default_value=.31;bs.inputs['Coat Weight'].default_value=.24
    bs.inputs['Coat Roughness'].default_value=.24;bs.inputs['Subsurface Weight'].default_value=.045
    coord=node(mat,'ShaderNodeTexCoord');globalcoord=node(mat,'ShaderNodeVectorMath');globalcoord.operation='MULTIPLY_ADD'
    globalcoord.inputs[1].default_value=(1/2.32,1/1.95,1/3.36);globalcoord.inputs[2].default_value=(.5,.54,0)
    link(mat,coord.outputs['Object'],globalcoord.inputs[0]);position=globalcoord.outputs[0]
    xyz=node(mat,'ShaderNodeSeparateXYZ');link(mat,position,xyz.inputs[0])
    x=mathnode(mat,'DIVIDE',mathnode(mat,'SUBTRACT',xyz.outputs['X'],.5),.43)
    z=mathnode(mat,'DIVIDE',mathnode(mat,'SUBTRACT',xyz.outputs['Z'],.415),.365)
    inside=mathnode(mat,'SUBTRACT',1,mathnode(mat,'ADD',mathnode(mat,'MULTIPLY',x,x),mathnode(mat,'MULTIPLY',z,z)))
    soft=ramp(mat,inside,[(0,0),(.22,1)])
    front=ramp(mat,xyz.outputs['Y'],[(.60,0),(.83,1)])
    mask=mathnode(mat,'MULTIPLY',soft,front)
    broad=node(mat,'ShaderNodeTexNoise',Scale=5.5,Detail=3.2,Roughness=.65);link(mat,position,broad.inputs['Vector'])
    base=ramp(mat,broad.outputs['Fac'],[(.16,'#176F85'),(.78,'#39AFC2')])
    spots=node(mat,'ShaderNodeTexVoronoi',Scale=11.5);link(mat,position,spots.inputs['Vector'])
    stain=ramp(mat,spots.outputs['Distance'],[(.15,1),(.25,0)])
    mix=node(mat,'ShaderNodeMixRGB');mix.blend_type='MIX';link(mat,mathnode(mat,'MULTIPLY',stain,.68),mix.inputs[0]);link(mat,base,mix.inputs[1]);mix.inputs[2].default_value=(*rgb('#215E70'),1)
    belly=node(mat,'ShaderNodeMixRGB');link(mat,mask,belly.inputs[0]);link(mat,mix.outputs[0],belly.inputs[1]);belly.inputs[2].default_value=(*rgb('#F2E3C9'),1);link(mat,belly.outputs[0],bs.inputs['Base Color'])
    micro=node(mat,'ShaderNodeTexNoise',Scale=172.,Detail=2.,Roughness=.65);link(mat,position,micro.inputs['Vector'])
    bump=node(mat,'ShaderNodeBump',Strength=.18,Distance=.012);link(mat,micro.outputs['Fac'],bump.inputs['Height']);link(mat,bump.outputs[0],bs.inputs['Normal'])
    return mat


def foam_material():
    mat=G.material('accent','#F88A1A','dough');bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.61
    tex=node(mat,'ShaderNodeTexNoise',Scale=125.,Detail=3.,Roughness=.75)
    color=ramp(mat,tex.outputs['Fac'],[(.18,'#A32E02'),(.55,'#E75403'),(.80,'#FF970A')]);link(mat,color,bs.inputs['Base Color'])
    bs.inputs['Coat Weight'].default_value=.12
    bump=node(mat,'ShaderNodeBump',Strength=.50,Distance=.025);link(mat,tex.outputs['Fac'],bump.inputs['Height']);link(mat,bump.outputs[0],bs.inputs['Normal'])
    return mat


def blade(name,points,widths,thickness,bone,mat):
    # Continuous, asymmetric hydrofoil. Broad shoulder, tapered tip, rolled edge.
    verts=[];uv=[];faces=[];rows=44;around=24
    controls=[(*p,w) for p,w in zip(points,widths)]
    for i in range(rows):
        t=i/(rows-1);x,y,z,w=G.D.interpolate(controls,t)
        for j in range(around):
            a=j*2*pi/around;c=cos(a);s=sin(a)
            verts.append((x+w*c,y+thickness*s*(.28+.72*sin(pi*t)**.6),z+.18*w*c*c))
            uv.append((j/around,t))
    for i in range(rows-1):
        for j in range(around):k=i*around+j;n=i*around+(j+1)%around;faces.append((k,n,n+around,k+around))
    faces.extend([tuple(reversed(range(around))),tuple((rows-1)*around+j for j in range(around))])
    return G.D.mesh(name,verts,faces,mat,bone,uv)


def sculpted_nose(mat):
    # A closed convex cap whose entire rim is embedded in the facial surface.
    # This avoids an isolated bulb on a stalk in the side view.
    outline=[(-.16,2.56),(-.10,2.60),(0,2.585),(.10,2.60),(.16,2.56),(.10,2.46),(0,2.40),(-.10,2.46)]
    verts=[];faces=[];around=64;rows=15
    for i in range(rows):
        r=max(.001,i/(rows-1))
        for j in range(around):
            t=j/around*len(outline);k=int(t);f=t-k
            p,a,b,n=[outline[q%len(outline)] for q in (k-1,k,k+1,k+2)]
            x,z=[.5*((2*a[c])+(-p[c]+b[c])*f+(2*p[c]-5*a[c]+4*b[c]-n[c])*f*f+(-p[c]+3*a[c]-3*b[c]+n[c])*f*f*f) for c in range(2)]
            x*=r;z=2.515+(z-2.515)*r
            verts.append((x,surface(x,z)+.15*sqrt(max(0,1-r*r))-.015,z))
    for i in range(rows-1):
        for j in range(around):k=i*around+j;n=i*around+(j+1)%around;faces.append((k,n,n+around,k+around))
    faces.append(tuple(reversed(range(around))))
    faces.append(tuple((rows-1)*around+j for j in range(around)))
    return G.D.mesh('Flush sculpted heart-shaped seal nose',verts,faces,mat,'Head')


def make():
    global BODY
    G.reset({'main':'#3BA6B4','accent':'#F88A1A'})
    skin=seal_material();foam=foam_material();cream=G.material('ceramic','#E7E2D2','solid')
    dark=G.material('faceCreases','#252321','solid');eye=G.material('eye','#171411','glass');iris=G.material('iris','#573824','glass')
    nose=G.material('nose','#252321','glass');nose.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.43
    nose.node_tree.nodes.get('Principled BSDF').inputs['Coat Weight'].default_value=.08
    body=G.loft('Continuous sculpted seal — chest, neck, face',PROFILE,skin,rows=132,around=128,face=sculpt)
    # Subtle macro anatomy, not noisy displacement or glued-on bumps.
    for v in body.data.vertices:
        x,y,z=v.co;v.co.y+=.007*sin(z*11+x*4)*exp(-((z-1.7)/.24)**2)*max(0,y)
    body.data.update();bpy.context.view_layer.update();BODY=body
    for side,label in [(-1,'L'),(1,'R')]:
        blade('Sculpted tapered foreflipper '+label,[(side*.86,.20,1.99),(side*1.15,.38,1.73),(side*1.39,.53,1.36),(side*1.43,.55,1.24),(side*1.40,.54,1.20)], [.12,.25,.20,.09,.003],.080,'Arm_'+label,skin)
        # Eyelids are sculpted edges, not white balls surrounding an iris.
        ex=side*.35;ez=2.80;ey=surface(ex,ez)+.018
        G.ell('Inset dark seal eye '+label,(ex,ey,ez),(.149,.075,.165),iris,'Head',48,32)
        G.ell('Black cornea '+label,(ex+side*.008,ey+.057,ez+.006),(.113,.025,.133),eye,'Head',40,28)
        lid=[]
        for j in range(21):
            a=pi*j/20;xx=ex+.158*cos(a);zz=ez+.174*sin(a)
            lid.append((xx,surface(xx,zz)+.027,zz))
        G.tube('Integrated upper eyelid '+label,lid,.023,skin,'Head')
        lower=[]
        for j in range(15):
            a=pi+pi*j/14;xx=ex+.15*cos(a);zz=ez+.16*sin(a);lower.append((xx,surface(xx,zz)+.025,zz))
        G.tube('Lower tear rim '+label,lower,.014,skin,'Head')
        # Small natural catchlight; studio reflection supplies the broad sheen.
        G.ell('Seal eye catchlight '+label,(ex-.035,ey+.083,ez+.062),(.022,.006,.023),G.PALETTE['white'],'Head',16,12)
        brow=[(ex-side*.12,surface(ex-side*.12,3.01)+.012,3.01),(ex,surface(ex,3.045)+.018,3.045),(ex+side*.12,surface(ex+side*.12,3.01)+.010,3.01)]
        G.loft('Tapered charcoal eyebrow '+label,[(*brow[0],.008,.012),(*brow[1],.027,.017),(*brow[2],.006,.008)],dark,'Head',rows=22,around=10)
        rng=random.Random(41)
        for j in range(19):
            x=side*(.13+rng.random()*.28);z=2.29+rng.random()*.22;y=surface(x,z)+.0025
            G.ell('Flush whisker follicle',(x,y,z),(.006+rng.random()*.004,.002,.007),dark,'Head',12,8)
        for j in range(6):
            x=side*(.19+.023*j);z=2.42-j*.026;y=surface(x,z)+.015
            G.tube('Swept tapered ivory whisker',[(x,y,z),(side*.57,y+.12,z+.08-j*.017),(side*(.89+j*.026),y+.12,z+.15-j*.065),(side*(1.11+j*.014),y+.07,z+.13-j*.089)], [.007,.0055,.0033,.0006],cream,'Head')
    ny=surface(0,2.54)
    sculpted_nose(nose)
    for s in (-1,1):G.ell('Inset nostril',(s*.073,surface(s*.073,2.535)+.116,2.535),(.023,.007,.032),eye,'Head',20,16)
    G.tube('Philtrum crease',[(0,surface(0,z)+.004,z) for z in (2.41,2.36,2.32,2.29)],.006,dark,'Head')
    # Open smile silhouette conforms to the actual facial surface.
    outline=[(-.265,2.32),(-.20,2.245),(-.13,2.16),(0,2.12),(.13,2.16),(.20,2.245),(.265,2.32),(.12,2.285),(0,2.27),(-.12,2.285)]
    mouth=[(x,surface(x,z)+.012,z) for x,z in outline]
    G.D.mesh('Recessed smile opening',mouth,[tuple(range(len(mouth)))],G.material('mouth','#38201E','solid'),'Head')
    lip=[(x,surface(x,z)+.014,z) for x,z in outline[:7]];G.tube('Soft lower lip edge',lip,.012,cream,'Head')
    tongue=G.material('tongue','#DC7077','skin')
    G.loft('Rounded flattened tongue',[(0,surface(0,2.15)+.035,2.145,.03,.013),(0,surface(0,2.20)+.048,2.19,.105,.018),(0,surface(0,2.24)+.025,2.24,.08,.009)],tongue,'Head',rows=24,around=24)
    G.tube('Tongue central groove',[(0,surface(0,2.17)+.07,2.17),(0,surface(0,2.22)+.05,2.22)],.003,G.material('tongueShade','#BD4856','solid'),'Head')
    # Raised rear tail with two long, thin webbed lobes.
    G.loft('Continuous tapered tail root',[(0,-.75,.46,.29,.15),(0,-1.01,.88,.23,.12),(0,-1.21,1.29,.13,.09)],skin,'Tail',rows=36,around=32)
    for s in (-1,1):blade('Forked rear fluke',[(s*.05,-1.20,1.23),(s*.21,-1.32,1.52),(s*.34,-1.37,1.91),(s*.30,-1.39,2.04),(s*.24,-1.38,2.09)],[.08,.19,.15,.08,.002],.055,'Tail',skin)
    # Large, bevel-ended continuous foam drum, matching the concept proportions.
    G.BONES['Appendage_0']=(0,.61,.49)
    G.loft('Single continuous orange foam drum',[(-.93,.61,.49,.40,.40),(-.87,.61,.49,.485,.485),(.87,.61,.49,.485,.485),(.93,.61,.49,.40,.40)],foam,'Appendage_0',rows=90,around=144)
    for s in (-1,1):
        G.loft('Sculpted ceramic roller fork',[(s*.99,.61,.49,.15,.13),(s*1.00,.54,.88,.14,.11),(s*.92,.39,1.16,.08,.07),(s*.88,.31,1.25,.012,.015)],cream,rows=40,around=24)
        G.loft('Orange roller axle cap',[(s*.98,.61,.49,.145,.145),(s*1.075,.61,.49,.145,.145),(s*1.10,.61,.49,.09,.09)],foam,'Appendage_0',rows=12,around=40)
        G.loft('Roller axle center',[(s*1.10,.61,.49,.075,.075),(s*1.13,.61,.49,.055,.055)],G.PALETTE['gold'],'Appendage_0',rows=8,around=32)
    return body


def studio(prefix='RolloBulk_v02',standard=False,samples=48,views=None):
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=samples;scene.cycles.use_denoising=True
    scene.render.resolution_x=1120;scene.render.resolution_y=1120;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.world=bpy.data.worlds.new('Pastel studio');scene.world.color=(.36,.42,.46)
    scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast';scene.view_settings.exposure=-.25
    if standard:
        scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.view_settings.exposure=-.7
    for location,power,size in [((4,5,7),1050,4.0),((-4,3,4),650,4.0),((2,-4,5),1250,3.0)]:
        data=bpy.data.lights.new('Softbox','AREA');data.energy=power;data.shape='DISK';data.size=size
        ob=bpy.data.objects.new('Softbox',data);bpy.context.collection.objects.link(ob);ob.location=location;ob.rotation_euler=(Vector((0,0,1.6))-ob.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.008));bpy.context.object.name='Review floor';bpy.context.object.data.materials.append(G.material('studio','#BBDDE3','solid'))
    camdata=bpy.data.cameras.new('Art review');cam=bpy.data.objects.new('Art review',camdata);bpy.context.collection.objects.link(cam);scene.camera=cam;camdata.type='ORTHO';camdata.ortho_scale=4.25
    for suffix,offset in (views or [('three-quarter',(4.5,8,3.0)),('front',(0,9,2.25)),('side',(9,.2,2.5))]):
        target=Vector((0,-.12,1.64));cam.location=target+Vector(offset);cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
        inverse=cam.rotation_euler.to_quaternion().inverted()
        corners=[inverse@(ob.matrix_world@Vector(corner)-target) for ob in scene.objects if ob.type=='MESH' and ob.name!='Review floor' for corner in ob.bound_box]
        camdata.ortho_scale=2.16*max(max(abs(p.x),abs(p.y)) for p in corners)
        scene.render.filepath=str(OUT/(prefix+'_'+suffix+'.png'));bpy.ops.render.render(write_still=True)


if __name__=='__main__':
    OUT.mkdir(parents=True,exist_ok=True);make()
    report={'status':'art-review-only-not-integrated','triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in bpy.context.scene.objects if o.type=='MESH'),'reference':'OriginalConcepts_20261015_v1/01-rollo-bulk.png','surface':'Continuous facial sculpt fields and painted material; no shared spherical cartoon face.'}
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'RolloBulk_v02_master.blend'))
    (OUT/'review.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    studio();print('ROLLO_V02_ART_REVIEW_RENDERED',flush=True)
