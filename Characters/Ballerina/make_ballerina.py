import bpy, math, os, json
from math import pi, sin, cos
from mathutils import Vector
OUT=r"C:\Rabota\projects\BrainrotPlanetUnity\Characters\Ballerina"
os.makedirs(OUT,exist_ok=True)
bpy.ops.object.select_all(action="SELECT");bpy.ops.object.delete(use_global=False)
mats={}
def material(n,c):
 m=bpy.data.materials.new(n);m.diffuse_color=(*c,1);m.use_nodes=True
 bs=m.node_tree.nodes.get("Principled BSDF")
 bs.inputs["Base Color"].default_value=(*c,1)
 bs.inputs["Roughness"].default_value=.82
 bs.inputs["Metallic"].default_value=0
 mats[n]=m;return m
cup=material("Porcelain warm cream",(.95,.85,.73))
rim=material("Cup golden rim",(.63,.32,.13))
coffee=material("Espresso dark mocha",(.28,.10,.045))
foam=material("Latte pale foam",(.98,.86,.67))
art=material("Latte art ivory",(.99,.98,.85))
skin=material("Warm rose skin",(.94,.58,.49))
pink=material("Tutu ballet soft pink",(.95,.47,.65))
pink2=material("Tutu pale blush",(.98,.70,.78))
rose=material("Ballet ribbons dusty rose",(.72,.24,.39))
white=material("Eye white",(.96,.96,.92))
iris=material("Large espresso iris",(.35,.17,.10))
black=material("Black eyelashes",(.085,.04,.045))
shine=material("Eye catchlight",(.99,.97,.92))
objects=[];sections={}
def add(o,n,m,b="Body"):
 o.name=n
 if m:o.data.materials.append(m)
 for poly in o.data.polygons:poly.use_smooth=True
 objects.append(o);sections[o.name]=b
 return o
def ell(n,loc,scale,m,b="Body",seg=20):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=seg,ring_count=12,location=loc)
 o=bpy.context.object;o.scale=scale
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 return add(o,n,m,b)
def cone(n,loc,r1,r2,depth,m,b="Body",vertices=32):
 bpy.ops.mesh.primitive_cone_add(vertices=vertices,radius1=r1,radius2=r2,depth=depth,location=loc)
 return add(bpy.context.object,n,m,b)
def path(n,pts,r,m,b="Body"):
 c=bpy.data.curves.new(n,"CURVE");c.dimensions="3D";c.resolution_u=16;c.bevel_depth=r;c.bevel_resolution=3
 s=c.splines.new("BEZIER");s.bezier_points.add(len(pts)-1)
 for p,v in zip(s.bezier_points,pts):p.co=v;p.handle_left_type="AUTO";p.handle_right_type="AUTO"
 c.materials.append(m)
 o=bpy.data.objects.new(n,c);bpy.context.collection.objects.link(o)
 bpy.ops.object.select_all(action="DESELECT");o.select_set(True);bpy.context.view_layer.objects.active=o
 bpy.ops.object.convert(target="MESH")
 return add(bpy.context.object,n,None,b)
def lathe(n,profile,m,b="Head",steps=40):
 vs=[];fs=[]
 for z,rad in profile:
  for j in range(steps):
   a=2*pi*j/steps;vs.append((rad*cos(a),rad*sin(a),z))
 for i in range(len(profile)-1):
  for j in range(steps):
   k=i*steps+j;nk=i*steps+(j+1)%steps
   fs.append((k,nk,nk+steps,k+steps))
 mesh=bpy.data.meshes.new(n);mesh.from_pydata(vs,[],fs);mesh.update()
 o=bpy.data.objects.new(n,mesh);bpy.context.collection.objects.link(o)
 return add(o,n,m,b)
# A porcelain cappuccino cup forms the whole head, not a sphere.
lathe("Cup sculpted porcelain head",[(2.17,.29),(2.22,.35),(2.38,.40),(2.66,.49),(2.85,.57),(2.92,.58),(2.95,.565)],cup)
path("Lip continuous gold band",[(.572*cos(2*pi*i/40),.572*sin(2*pi*i/40),2.929) for i in range(41)],.022,rim,"Head")
cone("Visible cappuccino disk",(0,0,2.943),.548,.548,.014,coffee,"Head",48)
path("Cappuccino foam outer spiral",[(.475*cos(2*pi*i/48),.475*sin(2*pi*i/48),2.962) for i in range(49)],.052,foam,"Head")
# Nested latte-art leaves on horizontal top, visible from a high game camera
for k in range(5):
 z=2.966+.001*k;scale=.42-.062*k
 path("Latte art leaf "+str(k),[(0,-.28+k*.048,z),(scale*.48,-.14+k*.03,z),(0,.035+k*.024,z),(-scale*.48,-.14+k*.03,z),(0,-.28+k*.048,z)],.018,art,"Head")
path("Latte art center stem",[(0,-.34,2.976),(0,-.12,2.977),(0,.23,2.976)],.015,art,"Head")
# Cup handle is recognizable in silhouette and remains attached to the head
path("Porcelain handle broad silhouette",[(.49,-.08,2.78),(.82,-.14,2.83),(.96,-.17,2.60),(.87,-.17,2.39),(.47,-.10,2.42)],.086,cup,"Head")
path("Handle gold outer pinstripe",[(.49,-.10,2.81),(.88,-.16,2.88),(1.02,-.19,2.60),(.90,-.19,2.36),(.48,-.12,2.39)],.018,rim,"Head")
# Large expressive anime-like eyes facing +Y, recognizable even as tiny character
for s in (-1,1):
 ell("Face eye white "+str(s),(s*.239,.425,2.641),(.160,.063,.192),white,"Head")
 ell("Face warm iris "+str(s),(s*.252,.482,2.636),(.085,.040,.130),iris,"Head")
 ell("Face pupil "+str(s),(s*.255,.513,2.630),(.048,.024,.096),black,"Head")
 ell("Eye glint "+str(s),(s*.223,.535,2.690),(.026,.016,.034),shine,"Head")
 path("Upper expressive eyelashes "+str(s),[(s*.105,.493,2.756),(s*.235,.481,2.794),(s*.386,.435,2.754)],.026,black,"Head")
 path("Eyebrow "+str(s),[(s*.130,.464,2.836),(s*.248,.468,2.852),(s*.351,.437,2.832)],.015,rose,"Head")
 ell("Rose cheek "+str(s),(s*.375,.401,2.478),(.085,.028,.045),pink2,"Head")
ell("Tiny nose",(0,.463,2.512),(.042,.057,.061),skin,"Head")
path("Gentle cupid smile",[(-.10,.435,2.387),(0,.482,2.369),(.10,.435,2.387)],.016,rose,"Head")
# Tiny neck, visible upper body, fitted corset with refined waist
ell("Neck",(0,0,2.125),(.145,.142,.28),skin,"Body")
ell("Ballet bodice torso",(0,0,1.80),(.306,.226,.39),pink,"Body")
cone("Pinched satin waist",(0,0,1.48),.175,.263,.40,pink,"Body")
path("Bodice gold trim",[( -.24,.14,2.01),(0,.245,1.96),(.24,.14,2.01)],.025,rim,"Body")
for s in (-1,1):
 path("Bodice seam "+str(s),[(s*.13,.209,1.96),(s*.105,.208,1.79),(s*.095,.173,1.57)],.011,pink2,"Body")
# Three layered scalloped tutu rings, recognizable in top-down view
for layer,(z,r1,r2,h,ma) in enumerate([(1.34,.30,.81,.23,pink),(1.25,.25,.91,.17,pink2),(1.17,.21,.74,.12,pink)]):
 cone("Wide ballet tutu layer "+str(layer),(0,0,z),r2,r1,h,ma,"Skirt",48)
 for k in range(16):
  a=2*pi*k/16
  ell("Tutu petal %s %s"%(layer,k),(.70*cos(a)*(1+layer*.05),.70*sin(a)*(1+layer*.05),z-.09),(.17,.13,.055),pink if k%2 else pink2,"Skirt",12)
path("Tutu embroidered waist ring",[(.25*cos(2*pi*i/36),.25*sin(2*pi*i/36),1.43) for i in range(37)],.023,rim,"Skirt")
# Sculpted graceful arms / hands and ballet poses
for s,letter in [(-1,"L"),(1,"R")]:
 upper="Arm_"+letter;fore="Forearm_"+letter;leg="Leg_"+letter;shin="Shin_"+letter
 ell("Shoulder "+letter,(s*.30,0,1.993),(.13,.145,.14),skin,upper)
 path("Swept upper arm "+letter,[(s*.32,.00,1.98),(s*.51,.02,1.81),(s*.66,.08,1.69)],.092,skin,upper)
 ell("Elbow "+letter,(s*.66,.08,1.69),(.095,.091,.09),skin,upper)
 path("Graceful lower arm "+letter,[(s*.66,.08,1.69),(s*.77,.18,1.85),(s*.71,.23,2.07)],.072,skin,fore)
 ell("Soft ballerina hand "+letter,(s*.71,.23,2.07),(.074,.061,.12),skin,fore)
 ell("Upper leg "+letter,(s*.17,0,1.04),(.14,.14,.37),skin,leg)
 ell("Knee "+letter,(s*.17,.025,.72),(.122,.13,.13),skin,leg)
 ell("Lower ballet leg "+letter,(s*.17,.03,.43),(.085,.10,.32),skin,shin)
 ell("Pointe shoe "+letter,(s*.17,.13,.133),(.135,.234,.15),pink,shin)
 ell("Pointe shoe flat sole "+letter,(s*.17,.24,.06),(.14,.08,.064),rose,shin)
 for k in range(3):
  path("Pointe ribbon "+letter+str(k),[(s*(.17-.09),.12,.26+k*.11),(s*(.17+.10),-.01,.34+k*.11)],.020,rose,shin)
# Rig: every merged rigid section is controlled by an animation bone
arm=bpy.data.armatures.new("Cappuccina skeleton");rig=bpy.data.objects.new("BallerinaCappuccina_Armature",arm);bpy.context.collection.objects.link(rig)
bpy.ops.object.select_all(action="DESELECT");rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode="EDIT")
def bone(n,a,b,p=None):
 eb=arm.edit_bones.new(n);eb.head=a;eb.tail=b
 if p:eb.parent=arm.edit_bones[p]
bone("Root",(0,0,.04),(0,0,.25))
bone("Body",(0,0,1.48),(0,0,1.94),"Root")
bone("Head",(0,0,2.18),(0,0,2.82),"Body")
bone("Skirt",(0,0,1.35),(0,0,1.45),"Body")
for s,label in [(-1,"L"),(1,"R")]:
 bone("Arm_"+label,(s*.32,0,1.98),(s*.65,.06,1.68),"Body")
 bone("Forearm_"+label,(s*.65,.06,1.68),(s*.72,.19,2.06),"Arm_"+label)
 bone("Leg_"+label,(s*.17,0,1.25),(s*.17,0,.71),"Body")
 bone("Shin_"+label,(s*.17,0,.71),(s*.17,.11,.12),"Leg_"+label)
bpy.ops.object.mode_set(mode="OBJECT")
prior=len(objects);joined=[]
groups={section:[o for o in objects if sections[o.name]==section] for section in sorted(set(sections.values()))}
for section,members in groups.items():
 if not members:continue
 for ob in members:
  bpy.ops.object.select_all(action="DESELECT");ob.select_set(True);bpy.context.view_layer.objects.active=ob
  for mod in list(ob.modifiers):
   try:bpy.ops.object.modifier_apply(modifier=mod.name)
   except Exception as e:print("MOD",str(e))
 bpy.ops.object.select_all(action="DESELECT")
 for ob in members:ob.select_set(True)
 bpy.context.view_layer.objects.active=members[0]
 if len(members)>1:bpy.ops.object.join()
 o=bpy.context.view_layer.objects.active;o.name="Combined_"+section;joined.append(o)
 vg=o.vertex_groups.new(name=section);vg.add(list(range(len(o.data.vertices))),1.0,"REPLACE")
 mod=o.modifiers.new("Rigid skin","ARMATURE");mod.object=rig;o.parent=rig
objects=joined
print("BALLERINA_MESHES",prior,len(joined))
rig.animation_data_create();pb={b.name:b for b in rig.pose.bones}
for b in pb.values():b.rotation_mode="XYZ"
acts={}
def make_action(name,n,pose):
 act=bpy.data.actions.new(name);rig.animation_data.action=act
 for frame in range(1,n+1,2):
  bpy.context.scene.frame_set(frame);t=2*pi*(frame-1)/(n-1)
  rr,lift=pose(t)
  for b in pb.values():
   b.rotation_euler=rr.get(b.name,(0,0,0))
   b.keyframe_insert(data_path="rotation_euler",group=b.name)
  pb["Root"].location.z=lift
  pb["Root"].keyframe_insert(data_path="location",group="Root")
 act.use_fake_user=True;acts[name]=act
def idle(t):
 return {"Head":(.035*sin(t),0,.055*sin(t)),"Body":(.025*sin(t),0,0),
 "Arm_L":(0,-.06,.05*sin(t)),"Arm_R":(0,.06,-.05*sin(t)),"Skirt":(0,0,.03*sin(t))},.022*sin(t)
def run(t):
 p,z=idle(t)
 p.update({"Leg_L":(.50*sin(t),0,-.08),"Leg_R":(-.50*sin(t),0,.08),
 "Shin_L":(-.18+.21*sin(t+1),0,0),"Shin_R":(-.18-.21*sin(t+1),0,0),
 "Arm_L":(.22*sin(t),-.20,.25),"Arm_R":(-.22*sin(t),.20,-.25),
 "Forearm_L":(-.15,0,0),"Forearm_R":(-.15,0,0),"Skirt":(0,0,.13*sin(t)),
 "Body":(.045*sin(t),0,.08*sin(t))})
 return p,.045*abs(sin(t))
def jump(t):
 p,z=idle(t);q=sin(t/2)
 p.update({"Leg_L":(-.18*q,0,-.15*q),"Leg_R":(.32*q,0,.20*q),
 "Arm_L":(-.32*q,-.27*q,.18*q),"Arm_R":(-.32*q,.27*q,-.18*q),
 "Shin_R":(-.25*q,0,0)})
 return p,.28*q
def land(t):
 p,z=idle(t);d=math.exp(-t)
 p.update({"Body":(.23*d,0,0),"Leg_L":(-.23*d,0,0),"Leg_R":(-.23*d,0,0)})
 return p,-.07*d
def victory(t):
 p,z=idle(t)
 p.update({"Body":(0,t,0),"Head":(.05*sin(t),0,-.08*sin(t)),
 "Arm_L":(-.35,-.28,.33),"Arm_R":(-.35,.28,-.33),
 "Forearm_L":(-.55,0,0),"Forearm_R":(-.55,0,0),
 "Leg_R":(-.38,0,.18),"Shin_R":(-.38,0,0),"Skirt":(0,0,.12*sin(t))})
 return p,.08*abs(sin(t))
for name,n,fn in [("Idle",49,idle),("Run",33,run),("Jump",33,jump),("Land",25,land),("Victory",65,victory)]:
 make_action(name,n,fn)
rig.animation_data.action=acts["Idle"];bpy.context.scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,"Ballerina_v01.blend"))
bpy.ops.object.select_all(action="DESELECT")
for o in objects:o.select_set(True)
rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,"Ballerina_v01.fbx"),use_selection=True,
 object_types={"MESH","ARMATURE"},bake_anim=True,bake_anim_use_all_actions=True,
 bake_anim_use_nla_strips=False,bake_anim_use_all_bones=True,add_leaf_bones=False,
 axis_forward="-Z",axis_up="Y")
def aim(ob,target):ob.rotation_euler=(Vector(target)-ob.location).to_track_quat("-Z","Y").to_euler()
def light(n,xyz,power,size):
 data=bpy.data.lights.new(n,"AREA");data.energy=power;data.size=size
 o=bpy.data.objects.new(n,data);bpy.context.collection.objects.link(o);o.location=xyz;aim(o,(0,0,1.55))
light("Warm Key",(-3,4,6),650,4)
light("Soft Fill",(3,4,5),430,5)
light("Rear Rim",(0,-4,5),500,3)
ca=bpy.data.cameras.new("Ballerina camera");cam=bpy.data.objects.new("Ballerina camera",ca)
bpy.context.collection.objects.link(cam);bpy.context.scene.camera=cam;ca.type="ORTHO";ca.ortho_scale=4.85
sc=bpy.context.scene;sc.render.engine="BLENDER_EEVEE";sc.render.resolution_x=720;sc.render.resolution_y=720;sc.render.resolution_percentage=100
sc.render.image_settings.file_format="PNG";sc.view_settings.view_transform="AgX"
for name,xyz in [("front",(4,6,4)),("side",(7,0,3.5)),("rear",(-4,-6,4)),("game_top",(3,4,8))]:
 cam.location=xyz;aim(cam,(0,0,1.6));sc.render.filepath=os.path.join(OUT,"Ballerina_"+name+".png");bpy.ops.render.render(write_still=True)
rig.animation_data.action=acts["Run"]
for f in range(1,34,3):
 sc.frame_set(f);cam.location=(4,6,4);aim(cam,(0,0,1.6))
 sc.render.filepath=os.path.join(OUT,"Ballerina_run_%02d.png"%f);bpy.ops.render.render(write_still=True)
rig.animation_data.action=acts["Victory"]
for f in (9,33,57):
 sc.frame_set(f);cam.location=(4,6,4);aim(cam,(0,0,1.6))
 sc.render.filepath=os.path.join(OUT,"Ballerina_victory_%02d.png"%f);bpy.ops.render.render(write_still=True)
report={"character":"Ballerina Cappuccina","blender":bpy.app.version_string,
 "mesh_before_merge":prior,"mesh_after_merge":len(objects),
 "bones":[b.name for b in rig.data.bones],
 "actions":{n:list(a.frame_range) for n,a in acts.items()}}
with open(os.path.join(OUT,"model_report.json"),"w",encoding="utf-8") as f:json.dump(report,f,ensure_ascii=False,indent=2)
print("BALLERINA_READY",json.dumps(report,ensure_ascii=False))
