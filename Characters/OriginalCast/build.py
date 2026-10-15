"""Blender authoring/export: original rigs, eight clips, previews and 130 skins.
Run: blender --background --python Characters/OriginalCast/build.py -- [index ...]
Nothing from the previous character meshes is imported or overwritten.
"""
import sys, json, math, hashlib, random
from pathlib import Path
import bpy
from mathutils import Vector, Matrix
from math import sin, cos, pi
sys.path.insert(0,str(Path(__file__).resolve().parent))
import geometry as G
from models import BUILDERS

ROOT=Path(__file__).resolve().parents[2]
SOURCE=Path(__file__).resolve().parent
OUT=SOURCE/'Models_v01'
UNITY=ROOT/'Assets'/'Resources'/'OriginalCast'
SKINS=ROOT/'Content'/'OriginalCast'/'Skins'
ROSTER=json.loads((SOURCE/'roster.json').read_text(encoding='utf-8'))['heroes']
ACTIONS={'Idle':(60,True),'Run':(24,True),'Jump':(24,False),'Land':(15,False),'Victory':(84,False),'Drop':(60,True),'Special':(30,False),'VictoryShow':(84,True)}
THEMES=[('stellar','ЗВЁЗДНЫЙ', '#536CBF','#6CDEE7','#EBE9F1'),('candy','КОНФЕТНЫЙ','#F5A8BF','#8EE1D2','#FFF0CE'),('neon','НЕОНОВЫЙ','#36425A','#64F0CD','#B7D6F4'),('tropical','ТРОПИЧЕСКИЙ','#49B697','#F5B45D','#E2F1B5'),('polar','ПОЛЯРНЫЙ','#B8DCE9','#B8A7E4','#FFF5DD')]

def prepare_mesh():
    parts=[]
    for bone,objects in G.D.PARTS.items():
        for obj in objects:
            group=obj.vertex_groups.new(name=bone);group.add(list(range(len(obj.data.vertices))),1,'REPLACE');parts.append(obj)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in parts:obj.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();obj=bpy.context.object
    obj.name='OriginalCast_SkinnedSurface'
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    lo=min(v.co.z for v in obj.data.vertices);hi=max(v.co.z for v in obj.data.vertices)
    scale=3/max(.1,hi-lo)
    for v in obj.data.vertices:v.co=Vector((v.co.x*scale,v.co.y*scale,(v.co.z-lo)*scale))
    pivots={key:(value[0]*scale,value[1]*scale,(value[2]-lo)*scale) for key,value in G.BONES.items()}
    triangles=sum(len(p.vertices)-2 for p in obj.data.polygons)
    if triangles>38000:
        mod=obj.modifiers.new('Mobile topology budget','DECIMATE');mod.ratio=38000/triangles
        bpy.ops.object.modifier_apply(modifier=mod.name)
    for face in obj.data.polygons:face.use_smooth=True
    return obj,pivots

def rig_mesh(mesh,pivots):
    data=bpy.data.armatures.new('Original26Skeleton');rig=bpy.data.objects.new('Original26Rig',data)
    bpy.context.collection.objects.link(rig);bpy.context.view_layer.objects.active=rig;rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    root=data.edit_bones.new('Root');root.head=(0,0,0);root.tail=(0,0,.18)
    for name,point in pivots.items():
        b=data.edit_bones.new(name);b.head=point;b.tail=Vector(point)+Vector((0,0,.17))
        b.parent=root if name=='Body' else data.edit_bones.get('Body')
    bpy.ops.object.mode_set(mode='OBJECT')
    mesh.parent=rig;mod=mesh.modifiers.new('Original26 authored rig','ARMATURE');mod.object=rig
    for name,loc in [('ReferenceFacingOrigin',(0,0,0)),('ReferenceFacingFront',(0,1,0))]:
        marker=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(marker);marker.parent=rig;marker.location=loc
    return rig

def animate(rig,index):
    rig.animation_data_create()
    for name,(frames,loop) in ACTIONS.items():
        action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
        for frame in sorted(set(range(1,frames+1,2))|{frames}):
            t=(frame-1)/max(1,frames-1);phase=t*2*pi;stride=sin(phase)
            for b in rig.pose.bones:
                b.rotation_mode='XYZ';b.rotation_euler=(0,0,0);b.location=(0,0,0);b.scale=(1,1,1)
            body=rig.pose.bones['Body'];head=rig.pose.bones['Head']
            if name=='Idle':
                body.scale=(1+.009*stride,1+.009*stride,1-.005*stride);head.rotation_euler.y=.018*stride
            elif name=='Run':
                body.location.z=.045*abs(stride);body.rotation_euler.y=.035*stride
                for i,bn in enumerate(['Arm_L','Arm_R','Leg_L','Leg_R']):rig.pose.bones[bn].rotation_euler.x=(.18 if i<2 else .29)*sin(phase+(i%2)*pi)
                for i in range(8):rig.pose.bones[f'Appendage_{i}'].rotation_euler.x=.24*sin(phase+(i%2)*pi+(i//2)*.6)
                if index in (0,14):rig.pose.bones['Tail'].rotation_euler.x=phase
                else:rig.pose.bones['Tail'].rotation_euler.z=.13*stride
                if index in (1,15,16):
                    for s,bn in [(-1,'Arm_L'),(1,'Arm_R')]:rig.pose.bones[bn].rotation_euler.y=s*(.1+.2*stride)
            elif name=='Drop':
                body.rotation_euler.x=.13+.06*sin(phase*2);body.rotation_euler.y=.10*stride
                for s,bn in [(-1,'Arm_L'),(1,'Arm_R')]:rig.pose.bones[bn].rotation_euler.y=s*(.52+.18*sin(phase*4))
                for i in range(8):rig.pose.bones[f'Appendage_{i}'].rotation_euler.x=.28*sin(phase*4+i)
                for i,bn in enumerate(['Leg_L','Leg_R']):rig.pose.bones[bn].rotation_euler.x=.35*sin(phase*4+i*pi)
            elif name=='Jump':
                body.scale=(1+.07*sin(t*pi),1+.07*sin(t*pi),1-.10*sin(t*pi));body.rotation_euler.x=.08*sin(t*pi)
                for s,bn in [(-1,'Arm_L'),(1,'Arm_R')]:rig.pose.bones[bn].rotation_euler.y=s*.35*sin(t*pi)
            elif name=='Land':body.scale=(1+.12*sin(t*pi),1+.12*sin(t*pi),1-.15*sin(t*pi))
            elif name in ('Victory','VictoryShow'):
                body.rotation_euler.y=.12*sin(phase*2);body.rotation_euler.z=.16*stride;body.location.z=.06*abs(sin(phase*2))
                head.rotation_euler.z=.08*sin(phase*2+.5)
                for s,bn in [(-1,'Arm_L'),(1,'Arm_R')]:rig.pose.bones[bn].rotation_euler.y=s*(.22+.30*sin(phase*2))
                for i in range(8):rig.pose.bones[f'Appendage_{i}'].rotation_euler.x=.23*sin(phase*2+i*.8)
                for i,bn in enumerate(['Leg_L','Leg_R']):rig.pose.bones[bn].rotation_euler.x=.20*sin(phase*2+i*pi)
                rig.pose.bones['Tail'].rotation_euler.z=.20*stride
            elif name=='Special':
                pulse=sin(t*pi);body.scale=(1+.11*pulse,1+.07*pulse,1-.08*pulse);body.rotation_euler.x=-.13*pulse
                for s,bn in [(-1,'Arm_L'),(1,'Arm_R')]:rig.pose.bones[bn].rotation_euler.y=s*.65*pulse
                for i in range(8):rig.pose.bones[f'Appendage_{i}'].rotation_euler.x=.4*pulse*sin(i*.9)
                head.rotation_euler.x=-.07*pulse
            for b in rig.pose.bones:
                b.keyframe_insert('rotation_euler',frame=frame);b.keyframe_insert('location',frame=frame);b.keyframe_insert('scale',frame=frame)
        action['loop']=loop
    rig.animation_data.action=bpy.data.actions['Idle'];bpy.context.scene.frame_set(1)

def badge_mesh(center,theme,color,anchor='Body'):
    count=10 if theme=='stellar' else 12
    vertices=[]
    for y in (center.y,center.y+.028):
        for i in range(count):
            a=2*pi*i/count;r=.085 if i%2 else .14
            vertices.append([round(center.x+r*cos(a),5),round(center.z+r*sin(a),5),round(-y,5)])
    # Coordinates are Unity's (x,z,-y); front points toward negative Z.
    triangles=[]
    vertices.extend([[round(center.x,5),round(center.z,5),round(-center.y,5)],[round(center.x,5),round(center.z,5),round(-center.y-.028,5)]])
    for i in range(count):j=(i+1)%count;triangles.extend([count*2,i,j,count*2+1,count+j,count+i])
    for i in range(count):j=(i+1)%count;triangles.extend([i,count+j,j,i,count+i,count+j])
    return {'name':theme+' sculpted crest','color':color,'anchor':anchor,'glow':theme=='neon','vertices':[x for v in vertices for x in v],'triangles':triangles}

def skins(hero,mesh):
    anchor='Body' if mesh.vertex_groups.get('Body') is not None else 'Head'
    body_index=mesh.vertex_groups[anchor].index
    candidates=[v for v in mesh.data.vertices if .6<v.co.z<1.8 and v.normal.y>.25 and any(g.group==body_index and g.weight>.5 for g in v.groups)]
    if not candidates:candidates=list(mesh.data.vertices)
    target=Vector((.45,max(v.co.y for v in candidates),1.15))
    mount=min(candidates,key=lambda v:(v.co-target).length_squared).co+Vector((0,.018,0))
    entries=[]
    for slug,title,main,accent,cream in THEMES:
        identifier='orig_'+hero['id'].lower()+'_'+slug
        bindings=[]
        for mat in mesh.data.materials:
            role=mat.name.split()[1]
            current='#'+mat.name.split('#')[1][:6]
            color=main if role not in ('dark','white','iris','cream','gold','metal') else current
            if role in ('accent','gold'):color=accent
            if role=='cream':color=cream
            bindings.append({'material':mat.name,'color':color,'metallic':.25 if slug=='stellar' else 0,'smoothness':.32,'glow':slug=='neon' and role=='accent'})
        pack={'schema':2,'hero':hero['index'],'id':identifier,'bindings':bindings,'meshes':[badge_mesh(mount,slug,accent,anchor)]}
        data=json.dumps(pack,ensure_ascii=False,separators=(',',':')).encode('utf-8');filename=identifier+'-'+hashlib.sha256(data).hexdigest()+'.json';(SKINS/filename).write_bytes(data)
        entries.append({'id':identifier,'title':title+' ОБРАЗ','titleEn':slug.title()+' Edition','description':hero['name']+' · новая палитра и объёмная эмблема · только внешний вид','hero':hero['index'],'file':filename,'sha256':hashlib.sha256(data).hexdigest(),'bytes':len(data),'swatch':accent})
    return entries

def studio(mesh,rig,hero,folder):
    def aim(obj,target):obj.rotation_euler=(target-obj.location).to_track_quat('-Z','Y').to_euler()
    for loc,energy in [((4,5,6),950),((-4,3,4),700),((0,-4,5),900)]:
        data=bpy.data.lights.new('Original studio light','AREA');data.energy=energy;data.shape='DISK';data.size=4
        obj=bpy.data.objects.new('Original studio light',data);bpy.context.collection.objects.link(obj);obj.location=loc;aim(obj,Vector((0,0,1.5)))
    bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.025));floor=bpy.context.object;floor.data.materials.append(G.material('floor','#D4EEF3','solid'))
    data=bpy.data.cameras.new('OriginalCast studio');cam=bpy.data.objects.new('OriginalCast studio',data);bpy.context.collection.objects.link(cam)
    scene=bpy.context.scene;scene.camera=cam;scene.render.engine='BLENDER_EEVEE';scene.render.resolution_x=640;scene.render.resolution_y=640;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.world=bpy.data.worlds.new('Pale original studio');scene.world.color=(.45,.55,.6)
    scene.view_settings.view_transform='AgX';data.type='ORTHO'
    coords=[mesh.matrix_world@Vector(v) for v in mesh.bound_box];low=Vector([min(v[k] for v in coords) for k in range(3)]);high=Vector([max(v[k] for v in coords) for k in range(3)])
    target=(low+high)*.5;data.ortho_scale=max(3.6,(high-low).x*1.3,(high-low).y*1.3)
    cam.location=target+Vector((4.0,7.0,3.0));aim(cam,target)
    scene.render.filepath=str(folder/(hero['id']+'_front.png'));bpy.ops.render.render(write_still=True)

def build(index,render=True):
    hero=ROSTER[index];G.reset(hero);BUILDERS[index]()
    mesh,pivots=prepare_mesh();rig=rig_mesh(mesh,pivots);animate(rig,index)
    directory=OUT/hero['id'];directory.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(directory/(hero['id']+'_v01.blend')))
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);mesh.select_set(True)
    for obj in bpy.data.objects:
        if obj.type=='EMPTY':obj.select_set(True)
    bpy.context.view_layer.objects.active=rig
    fbx=UNITY/(hero['id']+'_v01.fbx')
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'MESH','ARMATURE','EMPTY'},bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_use_all_bones=True,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
    entries=skins(hero,mesh)
    report={'hero':index,'id':hero['id'],'name':hero['name'],'blender':bpy.app.version_string,'vertices':len(mesh.data.vertices),'triangles':sum(len(p.vertices)-2 for p in mesh.data.polygons),'materials':len(mesh.data.materials),'bones':list(pivots),'actions':list(ACTIONS),'skins':entries,'fbxBytes':fbx.stat().st_size,'status':'exported-not-yet-Unity-verified'}
    (directory/'report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    if render:studio(mesh,rig,hero,directory)
    print('ORIGINAL_HERO_EXPORTED '+json.dumps({k:v for k,v in report.items() if k not in ('skins','bones')},ensure_ascii=False),flush=True)

def catalog():
    reports=[json.loads(path.read_text(encoding='utf-8')) for path in OUT.glob('*/report.json')]
    reports.sort(key=lambda e:e['hero']);entries=[skin for item in reports for skin in item['skins']]
    (SKINS/'catalog-original26-v1.json').write_text(json.dumps({'schema':2,'revision':1,'skins':entries},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print('ORIGINAL_CAST_PROGRESS heroes='+str(len(reports))+' skins='+str(len(entries)),flush=True)

if __name__=='__main__':
    for directory in (OUT,UNITY,SKINS):directory.mkdir(parents=True,exist_ok=True)
    args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
    selected=[int(value) for value in args if value.isdigit()] or list(range(26))
    for index in selected:build(index,'--no-render' not in args);catalog()
