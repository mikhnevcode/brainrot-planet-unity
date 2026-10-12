import bpy, math, os, json
from math import pi,sin,cos
from mathutils import Vector
OUT=r"C:\Rabota\projects\BrainrotPlanetUnity\Characters\Bombardiro"
os.makedirs(OUT,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
def mat(name,rgb,met=0,rough=.60,noise=0):
 m=bpy.data.materials.new(name);m.use_nodes=True
 node=m.node_tree.nodes.get("Principled BSDF")
 node.inputs["Base Color"].default_value=(*rgb,1)
 node.inputs["Metallic"].default_value=met
 node.inputs["Roughness"].default_value=rough
 if noise:
  n=m.node_tree.nodes.new("ShaderNodeTexNoise");n.inputs["Scale"].default_value=22
  b=m.node_tree.nodes.new("ShaderNodeBump");b.inputs["Strength"].default_value=noise;b.inputs["Distance"].default_value=.015
  m.node_tree.links.new(n.outputs['Fac'],b.inputs['Height']);m.node_tree.links.new(b.outputs['Normal'],node.inputs["Normal"])
 return m
green=mat("Crocodile dark olive scales",(.095,.25,.145),0,.73,.28)
snout=mat("Crocodile nose medium green",(.22,.40,.22),0,.72,.22)
jaw=mat("Underjaw olive cream",(.44,.50,.31),0,.84,.13)
mouth=mat("Inner mouth dark red",(.13,.045,.045),0,.83)
teeth=mat("Ivory teeth",(.83,.80,.68),0,.56)
iris=mat("Yellow reptile eyes",(.77,.61,.14),0,.43)
pupil=mat("Black slit pupil",(.015,.02,.014),0,.25)
shell=mat("Brushed silver aircraft body",(.42,.48,.50),.45,.60,.035)
metal=mat("Metal dark panels",(.26,.30,.32),.25,.71,.025)
trim=mat("Silver edge trim",(.59,.64,.65),.48,.46)
black=mat("Propeller nearly black",(.045,.068,.082),.17,.55)
prop_tip=mat("Propeller yellow safety tips",(.90,.67,.15),.02,.64)
glass=mat("Cockpit blue grey glass",(.14,.28,.40),.19,.29)
outline=mat("Scale crevice dark",(.039,.10,.065),0,.82)
paint=mat("Paint tank turquoise",(.02,.65,.73),.02,.47)
mesh_objects=[];assigned={}
def register(o,n,m,b="Body"):
 o.name=n
 if m:o.data.materials.append(m)
 mesh_objects.append(o);assigned[n]=b
 for p in o.data.polygons:p.use_smooth=True
 return o
def sphere(n,at,size,m,b="Body",segments=24,rings=14):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=at)
 o=bpy.context.object;o.scale=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 return register(o,n,m,b)
def block(n,at,size,m,b="Body",bevel=.055):
 bpy.ops.mesh.primitive_cube_add(size=1,location=at)
 o=bpy.context.object;o.scale=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new("Smoothed solid edges","BEVEL");mod.width=bevel;mod.segments=3
  mod=o.modifiers.new("Weighted face normals","WEIGHTED_NORMAL")
 return register(o,n,m,b)
def cylinder(n,at,r,depth,m,b="Body",axis="Z",vertices=20):
 bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=depth,location=at)
 o=bpy.context.object
 if axis=="Y":o.rotation_euler=(pi/2,0,0)
 elif axis=="X":o.rotation_euler=(0,pi/2,0)
 return register(o,n,m,b)
def curve(n,pts,radius,m,b="Body"):
 cu=bpy.data.curves.new(n,'CURVE');cu.dimensions='3D';cu.bevel_depth=radius;cu.bevel_resolution=2
 spline=cu.splines.new('BEZIER');spline.bezier_points.add(len(pts)-1)
 for p,c in zip(spline.bezier_points,pts):
  p.co=c;p.handle_left_type='AUTO';p.handle_right_type='AUTO'
 ob=bpy.data.objects.new(n,cu);bpy.context.collection.objects.link(ob);cu.materials.append(m)
 bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob
 bpy.ops.object.convert(target='MESH')
 return register(bpy.context.object,n,None,b)
def polygon(n,contour,thickness,m,b="Body",axis='Z'):
 npoints=len(contour)
 if axis=='Z':
  verts=[(x,y,z-thickness/2) for x,y,z in contour]+[(x,y,z+thickness/2) for x,y,z in contour]
 elif axis=='X':
  verts=[(x-thickness/2,y,z) for x,y,z in contour]+[(x+thickness/2,y,z) for x,y,z in contour]
 elif axis=='Y':
  verts=[(x,y-thickness/2,z) for x,y,z in contour]+[(x,y+thickness/2,z) for x,y,z in contour]
 faces=[]
 for i in range(1,npoints-1):
  faces.append((0,i,i+1));faces.append((npoints,npoints+i+1,npoints+i))
 for i in range(npoints):
  j=(i+1)%npoints;faces.append((i,j,npoints+j,npoints+i))
 data=bpy.data.meshes.new(n);data.from_pydata(verts,[],faces);data.update()
 ob=bpy.data.objects.new(n,data);bpy.context.collection.objects.link(ob)
 be=ob.modifiers.new("Rounded flying surface","BEVEL");be.width=.02;be.segments=2
 ob.modifiers.new("Weighted normals","WEIGHTED_NORMAL")
 return register(ob,n,m,b)
def ringtube(n,loc,r,minor,m,b="Body",rotation=(0,0,0)):
 bpy.ops.mesh.primitive_torus_add(major_segments=28,minor_segments=7,location=loc,rotation=rotation,major_radius=r,minor_radius=minor)
 return register(bpy.context.object,n,m,b)
# fuselage ring loft along +Y front / -Y tail
profiles=[(-2.25,.16,.18,1.60),(-1.97,.30,.28,1.60),(-1.60,.39,.37,1.65),
          (-1.12,.46,.46,1.66),(-.62,.53,.53,1.66),(.00,.55,.53,1.68),
          (.48,.50,.47,1.71),(.90,.40,.36,1.73),(1.20,.30,.26,1.71),
          (1.45,.16,.17,1.71)]
N=28;verts=[];faces=[]
for y,w,h,zc in profiles:
 for j in range(N):
  a=2*pi*j/N;verts.append((w*cos(a),y,zc+h*sin(a)))
for i in range(len(profiles)-1):
 for j in range(N):
  p=i*N+j;q=i*N+(j+1)%N;r=(i+1)*N+(j+1)%N;t=(i+1)*N+j
  faces.append((p,q,r,t))
mesh=bpy.data.meshes.new("Fuselage smooth rings");mesh.from_pydata(verts,[],faces);mesh.update()
obj=bpy.data.objects.new("Fuselage",mesh);bpy.context.collection.objects.link(obj);register(obj,"Fuselage silver aircraft hull",shell)
obj.modifiers.new("Subdivided hull","SUBSURF").levels=2
# cockpit atop fuselage before wings
sphere("Cockpit canopy fairing",(0,.52,2.05),(.38,.53,.26),metal)
sphere("Cockpit transparent glass",(0,.59,2.18),(.31,.43,.22),glass)
for x in (-.18,.18):
 curve("Cockpit window vertical rib "+str(x),[(x,.90,2.14),(x,.76,2.31),(x,.47,2.37)],.018,trim)
curve("Cockpit center windshield seam",[(0,1.005,2.06),(0,.96,2.24),(0,.81,2.37)],.017,trim)
# wings as trapezoidal swept biplane upper wing with thick edges
for side in (-1,1):
 wing=[(side*.40,-.49,1.68),(side*1.1,-.42,1.67),(side*2.75,-.80,1.67),
       (side*2.85,-1.17,1.67),(side*1.0,-1.10,1.67),(side*.45,-.93,1.68)]
 polygon("Airfoil main wing "+str(side),wing,.12,shell)
 curve("Wing leading polished edge "+str(side),
       [(side*.55,-.49,1.77),(side*1.8,-.58,1.76),(side*2.73,-.8,1.75)],.028,trim)
 for k,x in enumerate((.80,1.15,1.55,1.95,2.35)):
  xx=side*x
  curve("Wing sheet panel seam "+str(side)+" "+str(k),
        [(xx,-.63,1.746),(xx,-.91,1.746),(xx,-1.08,1.741)],.010,metal)
 polygon("Wing aileron "+str(side),
  [(side*1.55,-1.09,1.65),(side*2.75,-1.18,1.65),
   (side*2.66,-1.40,1.62),(side*1.58,-1.32,1.62)],.09,metal,"Flap_"+("L" if side<0 else "R"))
# Tail and stabilizers
polygon("Vertical tail aerodynamic rudder",
 [(0,-2.30,1.67),(0,-2.05,1.85),(0,-2.17,2.43),(0,-2.42,2.52),(0,-2.65,1.70)],
 .12,shell,"Tail")
for side in (-1,1):
 polygon("Horizontal tail wing "+str(side),
  [(side*.13,-2.17,1.68),(side*.84,-2.36,1.69),(side*.90,-2.66,1.70),
    (side*.18,-2.53,1.70)],.09,shell,"Tail")
# Aircraft metal details: rivet rows on wings
for side in (-1,1):
 for i in range(10):
  t=i/9; xx=side*(.56+2.0*t)
  sphere("Wing rivet "+str(side)+" "+str(i),(xx,-.80-.18*t,1.75),(.023,.023,.016),trim,segments=12,rings=8)
# Crocodile skull/head emerges at front (+Y)
sphere("Croc neck scales",(0,1.09,1.58),(.48,.56,.43),green)
sphere("Croc broad skull",(0,1.48,1.66),(.51,.56,.34),green)
sphere("Croc long wedge muzzle",(0,2.12,1.56),(.44,.79,.205),snout)
sphere("Croc nose bulb",(0,2.70,1.59),(.39,.27,.19),green)
sphere("Croc lower jaw",(0,2.08,1.29),(.40,.84,.105),jaw,"Jaw")
sphere("Croc mouth inner",(0,2.20,1.40),(.33,.63,.050),mouth,"Jaw")
for side in (-1,1):
 for i in range(14):
  yy=1.49+i*.085
  z=1.39+(.024 if i%3==0 else 0)
  # teeth top and bottom, staggered, visible at the jaw edges
  cylinder("Tooth upper %s %02d"%(side,i),
    (side*(.36-.10*(i/14)), yy,z+.095),.020,.15,teeth,vertices=8)
  cylinder("Tooth bottom %s %02d"%(side,i),
    (side*(.35-.10*(i/14)), yy,z-.06),.018,.12,teeth,"Jaw",vertices=8)
 for j in range(3):
  yy=2.43+j*.11
  sphere("Croc snout nostril "+str(side)+" "+str(j),
         (side*(.22+.04*j),yy,1.728),(.04,.05,.014),outline,segments=12,rings=8)
 # eyes integrated into brow ridges, NOT oversized white balls
 sphere("Raised reptile eye ridge "+str(side),(side*.37,1.40,1.93),(.19,.21,.16),green)
 sphere("Reptile iris gold "+str(side),(side*.476,1.48,1.963),(.047,.072,.062),iris)
 sphere("Slit pupil "+str(side),(side*.515,1.499,1.969),(.017,.055,.025),pupil)
 for i in range(6):
  y=1.03-i*.130
  curve("Green gill crevice %s %s"%(side,i),
        [(side*.42,y,1.84),(side*.48,y-.03,1.66),(side*.42,y-.04,1.5)],.012,outline)
 for row in range(3):
  for col in range(5):
   y=.81-row*.155
   x=side*(.28+.04*col)
   sphere("Neck reptile scale %s_%s_%s"%(side,row,col),(x,y,1.80-(col%2)*.07),(.075,.07,.045),green,segments=12,rings=8)
# Propeller engines and aerodynamic spinner
for side in (-1,1):
 x=side*1.48
 cylinder("Engine round housing "+str(side),(x,-.42,1.52),.33,.72,metal,axis="Y")
 cylinder("Engine silver front ring "+str(side),(x,-.035,1.52),.33,.065,trim,axis="Y")
 sphere("Engine dark cooling core "+str(side),(x,.020,1.52),(.275,.08,.27),black)
 for k in range(7):
  a=2*pi*k/7
  sphere("Engine cylinder rim "+str(side)+"_"+str(k),(x+.25*cos(a),.035,1.52+.25*sin(a)),
         (.060,.040,.06),metal,segments=14,rings=8)
 cylinder("Prop spinner shaft "+str(side),(x,.16,1.52),.094,.32,metal,axis="Y")
 bname="Prop_L" if side<0 else "Prop_R"
 sphere("Propeller pivot rotating "+str(side),(x,.37,1.52),(.11,.10,.11),trim,bname)
 for k in range(3):
  a=2*pi*k/3
  ca=cos(a);sa=sin(a)
  r0=.07;r1=.68
  # prop blade polygon in XZ plane, rotation around Y along engine
  points=[(x+r0*ca-.055*sa,.38,1.52+r0*sa+.055*ca),
          (x+r1*ca-.09*sa,.39,1.52+r1*sa+.09*ca),
          (x+(r1+.12)*ca+.06*sa,.40,1.52+(r1+.12)*sa-.06*ca),
          (x+r0*ca+.06*sa,.38,1.52+r0*sa-.06*ca)]
  polygon("Rotor blade %s_%d"%(side,k),points,.026,black,bname,axis='Y')
  tip=[(x+.67*ca-.076*sa,.40,1.52+.67*sa+.076*ca),
       (x+.80*ca-.04*sa,.40,1.52+.80*sa+.04*ca),
       (x+.80*ca+.04*sa,.40,1.52+.80*sa-.04*ca),
       (x+.67*ca+.08*sa,.40,1.52+.67*sa-.08*ca)]
  polygon("Yellow propeller tip %s_%d"%(side,k),tip,.034,prop_tip,bname,axis='Y')
 # wheel housing under wing
 cylinder("Landing wheel "+str(side),(x,-.75,.84),.19,.18,black,axis="X")
 curve("Landing strut "+str(side),[(x,-.70,1.43),(x,-.75,1.10),(x,-.75,.86)],.055,metal)
# Tailwheel
cylinder("Tail landing wheel",(0,-2.10,.95),.13,.17,black,axis="X")
curve("Tail gear strut",[(0,-1.98,1.47),(0,-2.09,1.15),(0,-2.10,1.0)],.045,metal)
# underbody turquoise paint pods = player power theme
for side in (-1,1):
 sphere("Paint fuel capsule "+str(side),(side*.45,-.66,1.10),(.14,.37,.13),paint)
 curve("Paint pipe "+str(side),[(side*.40,-.50,1.20),(side*.35,.0,1.10),(side*.18,.55,1.30)],.025,trim)
# Skeletal rig: z-up native Blender; each section rigidly skinned to a bone
armdata=bpy.data.armatures.new("Bombardiro_Skeleton")
rig=bpy.data.objects.new("Bombardiro_Armature",armdata)
bpy.context.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig;bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
def bone(n,h,t,parent=None):
 b=armdata.edit_bones.new(n);b.head=h;b.tail=t
 if parent:b.parent=armdata.edit_bones[parent];b.use_connect=False
bone("Root",(0,0,0),(0,0,.20))
bone("Body",(0,0,1.50),(0,.30,1.55),"Root")
bone("Jaw",(0,1.35,1.29),(0,1.65,1.29),"Body")
bone("Prop_L",(-1.48,.37,1.52),(-1.48,.65,1.52),"Body")
bone("Prop_R",(1.48,.37,1.52),(1.48,.65,1.52),"Body")
bone("Tail",(0,-1.90,1.62),(0,-2.34,1.67),"Body")
bone("Flap_L",(-1.52,-1.20,1.65),(-2.05,-1.28,1.65),"Body")
bone("Flap_R",(1.52,-1.20,1.65),(2.05,-1.28,1.65),"Body")
bpy.ops.object.mode_set(mode='OBJECT')
# Mobile optimisation: collapse 200+ individually modelled details into one skinned mesh
# per moving rigid section. This greatly reduces per-frame renderer overhead.
original_count=len(mesh_objects)
sections={n:[o for o in mesh_objects if assigned.get(o.name,"Body")==n] for n in ("Body","Jaw","Prop_L","Prop_R","Tail","Flap_L","Flap_R")}
joined=[]
for section in ("Body","Jaw","Prop_L","Prop_R","Tail","Flap_L","Flap_R"):
 subset=sections[section]
 if not subset:continue
 for o in subset:
  bpy.ops.object.select_all(action='DESELECT')
  o.select_set(True)
  bpy.context.view_layer.objects.active=o
  for mod in list(o.modifiers):
   try:bpy.ops.object.modifier_apply(modifier=mod.name)
   except Exception as exc:print("MODIFIER_SKIPPED",o.name,mod.name,str(exc))
 bpy.ops.object.select_all(action='DESELECT')
 for o in subset:o.select_set(True)
 bpy.context.view_layer.objects.active=subset[0]
 if len(subset)>1:bpy.ops.object.join()
 final=bpy.context.view_layer.objects.active
 final.name="Combined_"+section
 joined.append(final)
 assigned[final.name]=section
mesh_objects=joined
print("BOMBARDIRO_MESH_OPTIMIZED",original_count,"=>",len(mesh_objects))
for o in mesh_objects:
 key=assigned.get(o.name,"Body")
 grp=o.vertex_groups.new(name=key);grp.add(list(range(len(o.data.vertices))),1.,'REPLACE')
 mod=o.modifiers.new("Bombardiro skeletal motion","ARMATURE");mod.object=rig
 o.parent=rig
rig.animation_data_create()
bones={b.name:b for b in rig.pose.bones}
for b in bones.values():b.rotation_mode='XYZ'
actions={}
def anim(name,frames,func):
 a=bpy.data.actions.new(name);rig.animation_data.action=a
 for f in frames:
  bpy.context.scene.frame_set(f)
  pose=func(f)
  for b in bones.values():
   rot=pose.get(b.name,(0.,0.,0.))
   b.rotation_euler=rot
   b.keyframe_insert(data_path="rotation_euler",group=b.name)
  bones["Root"].location.z=pose.get("_lift",0.)
  bones["Root"].keyframe_insert(data_path="location",group="Root")
 a.use_fake_user=True;actions[name]=a
def engine(ph,speed=1):
 return {"Prop_L":(0,ph*speed,0),"Prop_R":(0,-ph*speed,0),
    "Tail":(0,.03*sin(ph*.3),0),"Jaw":(.035*abs(sin(ph*.4)),0,0),
    "Flap_L":(.02*sin(ph*.4),0,0),"Flap_R":(-.02*sin(ph*.4),0,0),
    "Body":(.023*sin(ph*.45),0,.027*sin(ph*.25)), "_lift":.045*sin(ph*.35)}
def idle(f):
 t=(f-1)/49;return engine(t*2*pi,1.)
def run(f):
 t=(f-1)/31;p=engine(t*4*pi,2.0)
 p["Body"]=(.055*sin(2*pi*t),.035*sin(4*pi*t),.09*sin(2*pi*t))
 p["_lift"]=.085*sin(4*pi*t)
 return p
def jump(f):
 t=(f-1)/30;p=engine(4*pi*t,1.25)
 p["Body"]=(-.12*sin(pi*t),0,.11*sin(pi*t))
 p["_lift"]=.35*sin(pi*t)
 return p
def land(f):
 t=(f-1)/22;p=engine(4*pi*t,1.)
 p["Body"]=(.16*math.exp(-7*t)*sin(10*t),0,0)
 p["_lift"]=-.11*math.exp(-9*t)
 return p
def victory(f):
 t=(f-1)/56;p=engine(6*pi*t,2.5)
 p["Body"]=(.13*sin(2*pi*t),.21*sin(2*pi*t),.30*sin(2*pi*t))
 p["Jaw"]=(.18*abs(sin(4*pi*t)),0,0)
 p["_lift"]=.19*abs(sin(6*pi*t))
 return p
anim("Idle",range(1,51,2),idle)
anim("Run",range(1,33,2),run)
anim("Jump",range(1,32,2),jump)
anim("Land",range(1,24,2),land)
anim("Victory",range(1,59,2),victory)
rig.animation_data.action=actions["Idle"];bpy.context.scene.frame_set(1)
source=os.path.join(OUT,"Bombardiro_v01.blend")
bpy.ops.wm.save_as_mainfile(filepath=source)
bpy.ops.object.select_all(action='DESELECT')
for ob in mesh_objects:ob.select_set(True)
rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,"Bombardiro_v01.fbx"),
 use_selection=True,object_types={"MESH","ARMATURE"},
 bake_anim=True,bake_anim_use_all_actions=True,
 bake_anim_use_all_bones=True,bake_anim_step=1,
 bake_anim_use_nla_strips=False,add_leaf_bones=False,
 apply_scale_options="FBX_SCALE_ALL",axis_forward="-Z",axis_up="Y")
# Preview lighting and backdrop, not included in FBX
floor=block("PREVIEW FLOOR",(0,0,-.02),(11,11,.05),mat("Preview background",(.72,.76,.78)),bevel=0)
def aim(ob,target):ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
def area(n,loc,w,size):
 data=bpy.data.lights.new(n,'AREA');data.energy=w;data.shape='DISK';data.size=size
 ob=bpy.data.objects.new(n,data);bpy.context.collection.objects.link(ob)
 ob.location=loc;aim(ob,(0,0,1.6))
area("Main studio softbox",(0,3.0,6),900,5)
area("Warm fill",(-5,1.5,3.5),550,4)
area("Edge backlight",(2,-4,5),620,4)
cdata=bpy.data.cameras.new("Studio preview")
cam=bpy.data.objects.new("Studio preview",cdata);bpy.context.collection.objects.link(cam)
bpy.context.scene.camera=cam;cdata.type='ORTHO';cdata.ortho_scale=7.1
scene=bpy.context.scene
scene.render.engine="BLENDER_EEVEE"
scene.render.resolution_x=960;scene.render.resolution_y=960
scene.render.resolution_percentage=100;scene.render.image_settings.file_format="PNG"
scene.view_settings.view_transform="AgX";scene.view_settings.look="AgX - Medium High Contrast"
for n,loc in [("front_3q",(4.8,6.5,4.5)),("side",(7.5,.2,4.0)),("rear_3q",(-4.8,-6.5,4.5))]:
 cam.location=loc;aim(cam,(0,0,1.65))
 scene.render.filepath=os.path.join(OUT,"Bombardiro_"+n+".png")
 bpy.ops.render.render(write_still=True)
rig.animation_data.action=actions["Run"]
for f in (5,11,21):
 scene.frame_set(f)
 cam.location=(4.8,6.5,4.5);aim(cam,(0,0,1.65))
 scene.render.filepath=os.path.join(OUT,"Bombardiro_run_%02d.png"%f)
 bpy.ops.render.render(write_still=True)
# restore blend with no preview geometry as hero scene 
bpy.ops.object.select_all(action='DESELECT');floor.select_set(True);cam.select_set(True)
for o in bpy.data.objects:
 if o.type=="LIGHT":o.select_set(True)
bpy.ops.object.delete(use_global=False)
rig.animation_data.action=actions["Idle"]
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=source)
report={"model":"Bombardiro Crocodilo","blender":bpy.app.version_string,
 "mesh_objects":len(mesh_objects),"bones":list(bones.keys()),
 "actions":{n:[float(a.frame_range[0]),float(a.frame_range[1])] for n,a in actions.items()},
 "fbx":"Bombardiro_v01.fbx","source":"Bombardiro_v01.blend"}
with open(os.path.join(OUT,"model_report.json"),"w",encoding="utf-8") as f:json.dump(report,f,ensure_ascii=False,indent=2)
print("BOMBARDIRO_READY",json.dumps(report,ensure_ascii=False))
