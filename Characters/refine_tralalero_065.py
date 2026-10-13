"""Replace humanoid shark legs with continuous shoe-bearing fins.
Preserves v05 source, skeleton rest transforms, all actions and FBX units.
Run with Blender --background --python Characters/refine_tralalero_065.py.
"""
import os, sys, math, json
import bpy
from mathutils import Vector

ROOT=os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, ROOT)
import reference_detail_063 as D

bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT,'Tralalero','Tralalero_v05.blend'))
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
rig.animation_data.action=bpy.data.actions['Idle']
bpy.context.scene.frame_set(1)
rest={b.name:[list(b.head_local),list(b.tail_local)] for b in rig.data.bones}
skin=next(m for m in bpy.data.materials if m.name.startswith('Ref63 hide #18475E'))
# Remove only the old rigid leg segments and small duplicate side flippers.
removed=[]
for obj in list(bpy.data.objects):
    if obj.name in ['Combined_'+part+'_'+side for part in ('Thigh','Shin','Fin') for side in ('L','R','Rear')]:
        removed.append(obj.name);bpy.data.objects.remove(obj,do_unlink=True)

def fin(side,x,y):
    # Fore/aft chord at the body is broad; the fin tapers into the shoe collar.
    # Elliptical sections are thin laterally, unlike cylindrical human shins.
    rear=side=='Rear'
    points=[(x,y,1.42,.095,.36 if rear else .43),
            (x*1.10,y-.04,1.16,.085,.31 if rear else .37),
            (x*1.24,y-.08,.87,.064,.23 if rear else .28),
            (x*1.16,y-.015,.56,.052,.14 if rear else .18),
            (x,y+.035,.28,.050,.080),
            (x,y+.040,.23,.046,.060)]
    rows,around=44,24;vertices=[];faces=[];uv=[]
    for i in range(rows):
        t=i/(rows-1);cx,cy,z,thickness,chord=D.interpolate(points,t)
        for j in range(around):
            a=math.tau*j/around
            # Rounded leading edge, fine trailing edge, gently cambered membrane.
            yy=chord*math.cos(a)
            xx=thickness*math.sin(a)*(0.72+0.28*math.cos(a))
            sweep=0 if rear else (-.40 if x>0 else .40)
            vertices.append((cx+xx+yy*sweep,cy+yy*math.sqrt(1-sweep*sweep),z));uv.append((j/around,t))
    for i in range(rows-1):
        for j in range(around):
            a=i*around+j;b=i*around+(j+1)%around
            faces.append((a,b,b+around,a+around))
    faces.extend([tuple(reversed(range(around))),tuple((rows-1)*around+j for j in range(around))])
    obj=D.mesh('Continuous swimming fin '+side,vertices,faces,skin,'Thigh_'+side,uv)
    groups={name:obj.vertex_groups.new(name=name) for name in ('Thigh_'+side,'Shin_'+side,'Foot_'+side)}
    for i in range(rows):
        z=vertices[i*around][2]
        shin=max(0,min(1,(.76-z)/.35));shin=shin*shin*(3-2*shin)
        foot=max(0,min(1,(.34-z)/.10));foot=foot*foot*(3-2*foot)
        weights=[1-shin,shin*(1-foot),shin*foot]
        indices=list(range(i*around,(i+1)*around))
        for group,weight in zip(groups.values(),weights):
            if weight>0:group.add(indices,weight,'REPLACE')
    modifier=obj.modifiers.new('Smooth swimming fin deformation','ARMATURE');modifier.object=rig
    obj.parent=rig
    return obj

fins=[fin(label,x,y) for x,y,label in [(-.45,.40,'L'),(.45,.40,'R'),(0,-1.10,'Rear')]]
old_tail=bpy.data.objects['Combined_Tail'];bpy.data.objects.remove(old_tail,do_unlink=True)
dorsal=next(m for m in bpy.data.materials if m.name.startswith('Ref63 hide #092133'))
tail=D.fan('Broad crescent tail',[(0,-1.56,1.49),(0,-1.76,1.78),(0,-2.21,2.24),
    (0,-2.10,1.81),(0,-1.95,1.46),(0,-2.17,.94),(0,-1.85,1.14),(0,-1.57,1.37)],.055,dorsal,'Tail')
group=tail.vertex_groups.new(name='Tail');group.add(list(range(len(tail.data.vertices))),1,'REPLACE')
modifier=tail.modifiers.new('Authored tail movement','ARMATURE');modifier.object=rig;tail.parent=rig
meshes=[o for o in bpy.data.objects if o.type=='MESH' and o.parent==rig and not o.name.startswith('SCULPT_SOURCE')]
assert rest=={b.name:[list(b.head_local),list(b.tail_local)] for b in rig.data.bones}, 'Rest rig changed'
assert len(removed)==8,removed
assert all(o.name not in removed for o in meshes)
triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
assert triangles<=65020,triangles
out=os.path.join(ROOT,'Tralalero','V06');os.makedirs(out,exist_ok=True)
blend=os.path.join(ROOT,'Tralalero','Tralalero_v06.blend')
fbx=os.path.join(ROOT,'Tralalero','Tralalero_v06.fbx')
bpy.ops.wm.save_as_mainfile(filepath=blend)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for obj in meshes:obj.select_set(True)
for obj in bpy.data.objects:
    if obj.name in ('ReferenceFacingOrigin','ReferenceFacingFront'):obj.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=fbx,use_selection=True,object_types={'MESH','ARMATURE','EMPTY'},
    bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,
    bake_anim_use_all_bones=True,add_leaf_bones=False,apply_scale_options='FBX_SCALE_NONE',
    axis_forward='-Z',axis_up='Y',path_mode='STRIP')

def aim(obj,target):obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
for loc,energy,color in [((4,5,6),850,(1,.89,.78)),((-4,2,4),650,(.63,.81,1)),((2,-4,5),750,(.8,.9,1))]:
    data=bpy.data.lights.new('Preview light','AREA');data.energy=energy;data.size=4;data.color=color
    obj=bpy.data.objects.new('Preview light',data);bpy.context.collection.objects.link(obj);obj.location=loc;aim(obj,(0,0,1.3))
bpy.ops.mesh.primitive_plane_add(size=24,location=(0,0,-.035))
bpy.context.object.data.materials.append(D.palette('solid',(.10,.13,.17)))
camdata=bpy.data.cameras.new('Preview');cam=bpy.data.objects.new('Preview',camdata);bpy.context.collection.objects.link(cam)
camdata.type='ORTHO';camdata.ortho_scale=5.0
sc=bpy.context.scene;sc.camera=cam;sc.render.engine='BLENDER_EEVEE'
sc.render.resolution_x=1100;sc.render.resolution_y=850;sc.render.resolution_percentage=100
sc.render.image_settings.file_format='PNG';sc.render.film_transparent=False
sc.world.color=(.025,.03,.045);sc.view_settings.view_transform='AgX'
for view,loc,action,frame in [('threequarter',(6,8,4),'Idle',1),('side',(8,1,3),'Idle',1),('run',(6,8,4),'Run',9)]:
    rig.animation_data.action=bpy.data.actions[action];sc.frame_set(frame)
    cam.location=Vector((0,-.15,1.25))+Vector(loc);aim(cam,(0,-.15,1.25))
    sc.render.filepath=os.path.join(out,'Tralalero_v06_'+view+'.png');bpy.ops.render.render(write_still=True)
report=dict(source='Tralalero_v05.blend',output='Tralalero_v06.blend',triangles=triangles,
            removed=removed,continuous_fins=len(fins),rest_skeleton_preserved=True,
            actions=[a.name for a in bpy.data.actions],blender=bpy.app.version_string)
with open(os.path.join(out,'Tralalero_v06_report.json'),'w',encoding='utf-8') as f:json.dump(report,f,indent=2)
print('TRALALERO_FIN_REFINEMENT_PASS',json.dumps(report),flush=True)
