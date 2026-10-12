import bpy,math,os,json
from math import pi,sin,cos
from mathutils import Vector
OUT=r"C:\Rabota\projects\BrainrotPlanetUnity\Characters\Bombombini"
os.makedirs(OUT,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
materials={}
def mat(name,c,metal=0,rough=.62):
 m=bpy.data.materials.new(name);m.diffuse_color=(*c,1);m.use_nodes=True
 bs=m.node_tree.nodes.get("Principled BSDF")
 bs.inputs["Base Color"].default_value=(*c,1);bs.inputs["Metallic"].default_value=metal;bs.inputs["Roughness"].default_value=rough
 materials[name]=m;return m
feather=mat("Goose pearl white feathers",(.81,.84,.80))
feather_shadow=mat("Goose grey wing feather",(.55,.61,.63))
throat=mat("Goose neck ivory",(.95,.92,.82))
beak=mat("Goose coral orange beak",(.9,.36,.13))
mouth=mat("Goose dark beak crease",(.17,.09,.06))
eye=mat("Goose amber eyes",(.53,.33,.14),0,.26)
pupil=mat("Goose black pupil",(.02,.027,.035),0,.20)
wet=mat("Tiny eye highlights",(.98,.98,.97),0,.20)
air=mat("Airframe satin dark silver",(.34,.40,.46),.36,.60)
airlight=mat("Airframe pale aluminium",(.67,.72,.73),.25,.47)
trim=mat("Airframe rivet seams",(.23,.26,.30),.21,.60)
jet=mat("Engine gunmetal",(.18,.23,.27),.28,.55)
glow=mat("Turbine cyan paint",(.035,.57,.85),.13,.32)
pink=mat("Paint canister magenta",(.78,.16,.40))
feet=mat("Goose webbed foot coral",(.79,.33,.16))
objects=[];sections={}
def put(obj,name,ma,bone="Body"):
 obj.name=name
 if ma:obj.data.materials.append(ma)
 objects.append(obj);sections[obj.name]=bone
 for f in obj.data.polygons:f.use_smooth=True
 return obj
def sphere(name,loc,sz,ma,b="Body",seg=20):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=seg,ring_count=12,location=loc)
 ob=bpy.context.object;ob.scale=sz
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 return put(ob,name,ma,b)
def cube(name,loc,sz,ma,b="Body",bev=.04):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
 ob=bpy.context.object;ob.scale=sz
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bev:
  mod=ob.modifiers.new("Rounded edge","BEVEL");mod.width=bev;mod.segments=2
  ob.modifiers.new("Weighted normals","WEIGHTED_NORMAL")
 return put(ob,name,ma,b)
def tube(name,path,r,ma,b="Body"):
 cv=bpy.data.curves.new(name,'CURVE');cv.dimensions='3D';cv.bevel_depth=r;cv.bevel_resolution=2
 spline=cv.splines.new('BEZIER');spline.bezier_points.add(len(path)-1)
 for o,p in zip(spline.bezier_points,path):o.co=p;o.handle_left_type='AUTO';o.handle_right_type='AUTO'
 cv.materials.append(ma);ob=bpy.data.objects.new(name,cv);bpy.context.collection.objects.link(ob)
 bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob;bpy.ops.object.convert(target="MESH")
 return put(bpy.context.object,name,None,b)
def poly(name,points,t,ma,b="Body",axis="Z"):
 if axis=="Z":verts=[(x,y,z-t/2) for x,y,z in points]+[(x,y,z+t/2) for x,y,z in points]
 elif axis=="X":verts=[(x-t/2,y,z) for x,y,z in points]+[(x+t/2,y,z) for x,y,z in points]
 elif axis=="Y":verts=[(x,y-t/2,z) for x,y,z in points]+[(x,y+t/2,z) for x,y,z in points]
 n=len(points);faces=[]
 for i in range(1,n-1):faces.extend([(0,i,i+1),(n,n+i+1,n+i)])
 for i in range(n):j=(i+1)%n;faces.append((i,j,n+j,n+i))
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
 ob=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(ob)
 bv=ob.modifiers.new("Round edges","BEVEL");bv.width=.015;bv.segments=2
 ob.modifiers.new("Weighted normals","WEIGHTED_NORMAL")
 return put(ob,name,ma,b)
def cyl(name,loc,rad,depth,ma,b="Body",axis="Y"):
 bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=rad,depth=depth,location=loc)
 ob=bpy.context.object
 if axis=="Y":ob.rotation_euler.x=pi/2
 elif axis=="X":ob.rotation_euler.y=pi/2
 return put(ob,name,ma,b)
# Airframe along +Y front. Primary fuselage in satin aluminium.
sphere("Combined ship torso",(0,-.45,1.55),(.51,1.40,.48),air)
sphere("Aluminium forward intake",(0,.61,1.52),(.41,.48,.37),airlight)
sphere("White feathered body",(0,-.55,1.91),(.49,1.05,.52),feather)
sphere("Tail feather fluff",(0,-1.65,1.84),(.40,.43,.34),feather)
for k in range(6):
 angle=(k-2.5)*.26
 sphere("Tail plume %d"%k,(angle*1.2,-1.82-.14*abs(sin(angle)),1.83+.09*cos(angle)),(.14,.43,.10),feather_shadow,"Tail",16)
# Goose long curved neck rises over front of aircraft
sphere("Long curved goose neck",(0,.58,2.22),(.25,.46,.70),throat,"Neck")
sphere("Upper lifted goose neck",(0,.86,2.67),(.24,.25,.48),feather,"Neck")
sphere("White goose head",(0,1.16,2.92),(.33,.37,.33),feather,"Neck")
sphere("Goose cheek",(0,1.25,2.78),(.31,.23,.20),throat,"Neck")
sphere("Long upper bill",(0,1.56,2.76),(.255,.48,.096),beak,"Beak")
sphere("Lower bill",(0,1.55,2.64),(.235,.40,.045),beak,"Beak")
tube("Beak lip dark crease",[(-.17,1.80,2.68),(0,1.91,2.67),(.17,1.8,2.68)],.013,mouth,"Beak")
for s in (-1,1):
 sphere("Goose dark expressive eye "+str(s),(s*.281,1.24,3.01),(.052,.092,.071),eye,"Neck")
 sphere("Goose black round pupil "+str(s),(s*.329,1.269,3.016),(.022,.045,.040),pupil,"Neck")
 sphere("Goose tiny eye highlight "+str(s),(s*.346,1.282,3.039),(.008,.016,.015),wet,"Neck",12)
 sphere("Beak nostril "+str(s),(s*.16,1.72,2.84),(.024,.027,.012),mouth,"Beak",12)
# Main hybrid feather wings: high silhouette and engines.
for s in (-1,1):
 bone="Wing_L" if s<0 else "Wing_R"
 points=[(s*.38,-.25,1.84),(s*.88,-.31,1.88),(s*2.50,-.90,1.84),
        (s*2.78,-1.27,1.82),(s*1.18,-1.18,1.81),(s*.51,-.85,1.86)]
 poly("Blended bird aircraft main wing "+str(s),points,.16,airlight,bone)
 for k in range(7):
  x=s*(.75+k*.26)
  sphere("Goose wing small feather %s_%s"%(s,k),
    (x,-1.00-k*.035,1.77),(.12,.26,.052),feather,bone,14)
 poly("Wingtip feather "+str(s),
     [(s*2.32,-.85,1.82),(s*2.99,-1.14,1.81),(s*2.94,-1.30,1.81),
      (s*2.35,-1.23,1.83)],.10,feather_shadow,bone)
 tube("Airframe wing trim "+str(s),[(s*.52,-.27,1.93),(s*1.35,-.48,1.91),(s*2.53,-.91,1.91)],.021,trim,bone)
 # Turbine engine pod under each wing, realistic circular intake
 for idx,xmag in enumerate((1.13,1.95)):
  x=s*xmag
  engine="Engine_L" if s<0 else "Engine_R"
  y=-.64-.20*idx
  sphere("Gusini engine nacelle %s %s"%(s,idx),(x,y,1.51),(.25,.37,.27),jet,engine)
  cyl("Jet intake rim %s %s"%(s,idx),(x,y+.31,1.51),.225,.050,airlight,engine)
  sphere("Jet intake recessed cone %s %s"%(s,idx),(x,y+.35,1.51),(.155,.02,.155),trim,engine)
  sphere("Jet dark inner rotor %s %s"%(s,idx),(x,y+.37,1.51),(.115,.026,.115),jet,engine)
  sphere("Cyan turbine exhaust %s %s"%(s,idx),(x,y-.30,1.51),(.15,.04,.15),glow,engine)
  for k in range(5):
   a=2*pi*k/5
   poly("Rotating tiny engine blade %s_%s_%s"%(s,idx,k),
    [(x,y+.394,1.51),(x+.11*cos(a),y+.397,1.51+.11*sin(a)),
     (x+.095*cos(a+.4),y+.4,1.51+.095*sin(a+.4))],.01,airlight,engine,axis="Y")
# Tail stabilizers are feathered jet airplane fins
poly("Tail wing left",[(0,-1.73,1.76),(-.9,-2.10,1.75),(-.88,-2.40,1.74),(0,-2.19,1.77)],.09,airlight,"Tail")
poly("Tail wing right",[(0,-1.73,1.76),(.9,-2.10,1.75),(.88,-2.40,1.74),(0,-2.19,1.77)],.09,airlight,"Tail")
poly("Goose upright tailfin",[(0,-1.68,1.90),(0,-1.82,2.20),(0,-2.10,2.53),(0,-2.29,2.37),(0,-2.36,1.81)],.10,feather,"Tail",axis="X")
# Goose small webbed feet under body — tucked for flying, swing when jumping.
for s in (-1,1):
 label="L" if s<0 else "R";bone="Foot_"+label
 tube("Tucked goose leg "+label,[(s*.24,-.85,1.28),(s*.32,-.86,.98),(s*.31,-.64,.78)],.075,feet,bone)
 poly("Goose orange webbed foot "+label,
  [(s*.31,-.71,.73),(s*.53,-.38,.73),(s*.41,-.38,.72),(s*.31,-.41,.71),
   (s*.20,-.36,.71),(s*.16,-.60,.72)],.075,feet,bone)
# stylized harmless paint cylinder instead of weapons
sphere("Gusini magenta paint reservoir",(0,-1.02,1.06),(.21,.43,.18),pink)
for k in range(7):
 tube("Aircraft side feather contour %d"%k,[(-.40,-.65-k*.105,1.85),(-.48,-.79-k*.105,1.72),(-.42,-.97-k*.105,1.66)],.012,feather_shadow)
# 8-bone rigid-section rig, then join all small parts into ~8 renderer objects
arm=bpy.data.armatures.new("GusiniSkeleton");rig=bpy.data.objects.new("BombombiniGusini_Armature",arm);bpy.context.collection.objects.link(rig)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
def bone(n,a,b,parent=None):
 eb=arm.edit_bones.new(n);eb.head=a;eb.tail=b
 if parent:eb.parent=arm.edit_bones[parent]
bone("Root",(0,0,0),(0,0,.22))
bone("Body",(0,-.4,1.53),(0,-.2,1.85),"Root")
bone("Neck",(0,.75,2.2),(0,1.16,2.97),"Body")
bone("Beak",(0,1.44,2.74),(0,1.83,2.72),"Neck")
bone("Wing_L",(-.58,-.5,1.83),(-1.8,-.75,1.82),"Body")
bone("Wing_R",(.58,-.5,1.83),(1.8,-.75,1.82),"Body")
bone("Engine_L",(-1.4,-.75,1.51),(-1.4,-.35,1.51),"Wing_L")
bone("Engine_R",(1.4,-.75,1.51),(1.4,-.35,1.51),"Wing_R")
bone("Tail",(0,-1.55,1.79),(0,-2.18,1.84),"Body")
bone("Foot_L",(-.23,-.83,1.22),(-.29,-.51,.75),"Body")
bone("Foot_R",(.23,-.83,1.22),(.29,-.51,.75),"Body")
bpy.ops.object.mode_set(mode='OBJECT')
sectionsList=("Body","Neck","Beak","Wing_L","Wing_R","Engine_L","Engine_R","Tail","Foot_L","Foot_R")
cached={n:[o for o in objects if sections[o.name]==n] for n in sectionsList}
initial=len(objects);joined=[]
for section in sectionsList:
 members=cached[section]
 if not members:continue
 for obj in members:
  bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
  for mod in list(obj.modifiers):
   try:bpy.ops.object.modifier_apply(modifier=mod.name)
   except Exception as ex:print("SKIP_MODIFIER",ex)
 bpy.ops.object.select_all(action="DESELECT")
 for obj in members:obj.select_set(True)
 bpy.context.view_layer.objects.active=members[0]
 if len(members)>1:bpy.ops.object.join()
 ob=bpy.context.view_layer.objects.active;ob.name="Combined_"+section
 joined.append(ob);sections[ob.name]=section
objects=joined
for o in objects:
 vg=o.vertex_groups.new(name=sections[o.name]);vg.add(list(range(len(o.data.vertices))),1.,'REPLACE')
 mod=o.modifiers.new("Gusini bone skin","ARMATURE");mod.object=rig;o.parent=rig
print("GUSINI_COMBINED_MESHES",initial,"TO",len(objects))
# Action animations
rig.animation_data_create();bones={p.name:p for p in rig.pose.bones}
for p in bones.values():p.rotation_mode='XYZ'
acts={}
def build(name,length,fun):
 action=bpy.data.actions.new(name);rig.animation_data.action=action
 for i in range(1,length+1,2):
  scene=bpy.context.scene;scene.frame_set(i);ph=2*pi*(i-1)/(length-1)
  angles,bounce=fun(ph)
  for b in bones.values():
   b.rotation_euler=angles.get(b.name,(0,0,0))
   b.keyframe_insert(data_path="rotation_euler",group=b.name)
  bones["Root"].location.z=bounce;bones["Root"].keyframe_insert(data_path="location",group="Root")
 action.use_fake_user=True;acts[name]=action
def neutral(ph):
 return {"Body":(.028*sin(ph),0,.025*sin(ph)),
 "Neck":(.065*sin(ph+.3),.04*sin(ph),.025*sin(ph)),
 "Wing_L":(.055*sin(ph),0,.01*sin(ph)),
 "Wing_R":(-.055*sin(ph),0,-.01*sin(ph)),
 "Engine_L":(0,1.6*ph,0),
 "Engine_R":(0,-1.6*ph,0),
 "Tail":(.04*sin(ph),0,0),"Beak":(.045*sin(ph),0,0)},.035*sin(ph)
def moving(ph):
 p,b=neutral(ph)
 p["Wing_L"]=(.22*sin(ph*2),.08*sin(ph),.10*sin(ph))
 p["Wing_R"]=(-.22*sin(ph*2),-.08*sin(ph),-.10*sin(ph))
 p["Body"]=(.06*sin(ph*2),0,.09*sin(ph))
 p["Engine_L"]=(0,5.2*ph,0);p["Engine_R"]=(0,-5.2*ph,0)
 p["Foot_L"]=(.12*sin(ph),0,0);p["Foot_R"]=(.12*sin(ph+pi),0,0)
 return p,.08*sin(ph*2)
def jump(ph):
 p,b=neutral(ph)
 p["Wing_L"]=(.55*sin(ph/2),.12,0);p["Wing_R"]=(-.55*sin(ph/2),-.12,0)
 p["Foot_L"]=(-.27*sin(ph/2),0,0);p["Foot_R"]=(-.27*sin(ph/2),0,0)
 return p,.28*sin(ph/2)
def land(ph):
 p,b=neutral(ph)
 p["Body"]=(.12*sin(ph)*math.exp(-ph),0,0)
 p["Wing_L"]=(.2*math.exp(-ph),0,0);p["Wing_R"]=(-.2*math.exp(-ph),0,0)
 return p,-.1*math.exp(-ph)
def victory(ph):
 p,b=moving(ph)
 p["Neck"]=(.22*sin(ph*2),.16*sin(ph),.18*sin(ph))
 p["Body"]=(.18*sin(ph),.20*sin(ph),.28*sin(ph))
 return p,.14*abs(sin(ph*3))
for name,f,fun in [("Idle",49,neutral),("Run",33,moving),("Jump",33,jump),("Land",25,land),("Victory",57,victory)]:build(name,f,fun)
rig.animation_data.action=acts["Idle"];bpy.context.scene.frame_set(1)
blend=OUT+"\\Bombombini_v01.blend"
bpy.ops.wm.save_as_mainfile(filepath=blend)
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=OUT+"\\Bombombini_v01.fbx",use_selection=True,object_types={"MESH","ARMATURE"},
  bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_all_bones=True,
  bake_anim_use_nla_strips=False,add_leaf_bones=False,
  axis_forward="-Z",axis_up="Y")
def aim(obj,target):obj.rotation_euler=(Vector(target)-obj.location).to_track_quat("-Z","Y").to_euler()
def light(name,pos,watts):
 data=bpy.data.lights.new(name,"AREA");data.energy=watts;data.shape="DISK";data.size=5
 o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);o.location=pos;aim(o,(0,0,1.7))
light("Key",(-3,4,6),780);light("Fill",(4,3,4),600);light("Rim",(0,-4,5),540)
camera=bpy.data.cameras.new("Character camera")
cam=bpy.data.objects.new("Character camera",camera);bpy.context.collection.objects.link(cam);bpy.context.scene.camera=cam
camera.type="ORTHO";camera.ortho_scale=7.2
sc=bpy.context.scene;sc.render.engine="BLENDER_EEVEE";sc.render.resolution_x=800;sc.render.resolution_y=800;sc.render.resolution_percentage=100
sc.render.image_settings.file_format="PNG";sc.view_settings.view_transform="AgX";sc.view_settings.look="AgX - Medium High Contrast"
for name,pos in [("front",(4,5.8,4.7)),("side",(7,.25,4.4)),("rear",(-4,-5.8,4.3))]:
 cam.location=pos;aim(cam,(0,0,1.76));sc.render.filepath=OUT+"\\Bombombini_"+name+".png";bpy.ops.render.render(write_still=True)
rig.animation_data.action=acts["Run"]
for i in (5,17,29):
 sc.frame_set(i);cam.location=(4,5.8,4.7);aim(cam,(0,0,1.76))
 sc.render.filepath=OUT+"\\Bombombini_run_%02d.png"%i;bpy.ops.render.render(write_still=True)
report={"name":"Bombombini Gusini","blender":bpy.app.version_string,"mesh_before_merge":initial,"mesh_after_merge":len(objects),
"bones":[b.name for b in rig.data.bones],"actions":{k:[float(v.frame_range[0]),float(v.frame_range[1])] for k,v in acts.items()}}
with open(OUT+"\\model_report.json","w",encoding="utf-8") as f:json.dump(report,f,indent=2,ensure_ascii=False)
print("GUSINI_READY",json.dumps(report,ensure_ascii=False))
