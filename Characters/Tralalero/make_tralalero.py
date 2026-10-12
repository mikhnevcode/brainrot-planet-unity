import bpy, math, os, json
from mathutils import Vector
from math import sin, cos, pi
OUT = r"C:\Rabota\projects\BrainrotPlanetUnity\Characters\Tralalero"
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
for group in (bpy.data.armatures,):
    pass

def material(name,color,metallic=0.0,rough=.5,noise=.0):
    m=bpy.data.materials.new(name)
    m.use_nodes=True
    node=m.node_tree.nodes.get("Principled BSDF")
    node.inputs["Base Color"].default_value=(*color,1)
    node.inputs["Metallic"].default_value=metallic
    node.inputs["Roughness"].default_value=rough
    if noise:
        n=m.node_tree.nodes.new("ShaderNodeTexNoise")
        n.inputs["Scale"].default_value=18
        n.inputs["Detail"].default_value=2
        bump=m.node_tree.nodes.new("ShaderNodeBump")
        bump.inputs["Strength"].default_value=noise
        bump.inputs["Distance"].default_value=.028
        m.node_tree.links.new(n.outputs["Fac"],bump.inputs["Height"])
        m.node_tree.links.new(bump.outputs["Normal"],node.inputs["Normal"])
    return m
skin=material("01 Blue grey shark skin",(.040,.17,.28),.03,.48,.13)
skin_top=material("02 Deep dorsal blue",(.027,.083,.15),.06,.44,.12)
skin_light=material("03 Pale belly silver",(.69,.76,.76),.0,.64,.04)
gill=material("04 Gill shadow blue",(.025,.083,.115),0,.75)
eye_iris=material("05 Iris amber",(.36,.21,.10),.13,.23)
eye_black=material("06 Pupil",(.009,.014,.023),.06,.08)
eye_white=material("07 Eye wet whites",(.85,.90,.86),0,.17)
sneaker=material("08 Blue sneaker textile",(.005,.40,.78),.04,.66,.12)
sneaker_dark=material("09 Blue mesh panels",(.012,.20,.46),0,.65,.15)
shoe_sole=material("10 White sneaker soles",(.85,.90,.90),0,.59)
shoelace=material("11 Light cyan laces",(.56,.89,.96),0,.48)
shoe_rubber=material("12 Dark outsole",(.042,.088,.15),0,.77)
mouth=material("13 Dark mouth",(.075,.035,.062),0,.65)
teeth=material("14 Teeth ivory",(.88,.87,.75),0,.38)
highlight=material("15 White detail",(.9,.96,.98),0,.24)
meshes=[]
def linkmesh(ob,name,material):
    ob.name=name
    if material:ob.data.materials.append(material)
    meshes.append(ob)
    for p in ob.data.polygons:p.use_smooth=True
    return ob
def uv(name,loc,sc,mat,segments=22,rings=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=loc)
    ob=bpy.context.object;ob.scale=sc
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return linkmesh(ob,name,mat)
def cube(name,loc,sc,mat,bev=.06):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    ob=bpy.context.object;ob.scale=sc
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bev:
        mod=ob.modifiers.new("Molded rounded corners","BEVEL");mod.width=bev;mod.segments=3
        mod2=ob.modifiers.new("Face normals","WEIGHTED_NORMAL")
    return linkmesh(ob,name,mat)
def tube(name,coords,radius,mat,poly=False):
    curve=bpy.data.curves.new(name,"CURVE");curve.dimensions="3D";curve.resolution_u=16;curve.bevel_depth=radius;curve.bevel_resolution=3
    spline=curve.splines.new("POLY" if poly else "BEZIER")
    if poly:
        spline.points.add(len(coords)-1)
        for p,c in zip(spline.points,coords):p.co=(*c,1)
    else:
        spline.bezier_points.add(len(coords)-1)
        for p,c in zip(spline.bezier_points,coords):p.co=c;p.handle_left_type='AUTO';p.handle_right_type='AUTO'
    ob=bpy.data.objects.new(name,curve);bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mat)
    # convert to mesh so FBX export contains geometry
    bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob
    bpy.ops.object.convert(target='MESH')
    ob=bpy.context.object
    return linkmesh(ob,name, None)
def finmesh(name,verts,faces,mat):
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata(verts,[],faces);mesh.update()
    ob=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(ob)
    bevel=ob.modifiers.new("Soft fin edge","BEVEL");bevel.width=.018;bevel.segments=2
    norm=ob.modifiers.new("Smooth fins","WEIGHTED_NORMAL")
    return linkmesh(ob,name,mat)
def fanshape(name,points,thick,mat): # points are 3D perimeter; mesh double-sided shell
    # Fins in sagittal plane must have thickness along X, not Z.
    extrude_x=max(p[0] for p in points)-min(p[0] for p in points)<.15
    if extrude_x:
        verts=[(x-thick*.5,y,z) for x,y,z in points]+[(x+thick*.5,y,z) for x,y,z in points]
    else:
        verts=[(x,y,z-thick*.5) for x,y,z in points]+[(x,y,z+thick*.5) for x,y,z in points]
    n=len(points);faces=[]
    for i in range(1,n-1):
        faces.append((0,i,i+1));faces.append((n,n+i+1,n+i))
    for i in range(n):j=(i+1)%n;faces.append((i,j,n+j,n+i))
    return finmesh(name,verts,faces,mat)

# Shark organic body: soft smoothed rings along +Y (snout), -Y (tail)
profiles=[
(-1.75,.07,.10,1.39),(-1.55,.20,.23,1.39),(-1.20,.37,.35,1.45),
(-.65,.53,.43,1.48),(-.08,.61,.49,1.49),(.44,.60,.44,1.51),
(.90,.52,.36,1.50),(1.31,.45,.28,1.48),(1.69,.31,.20,1.47),
(1.92,.16,.12,1.45),(2.03,.015,.020,1.45)]
N=28;verts=[];faces=[]
for y,w,h,cz in profiles:
    for j in range(N):
        ang=2*pi*j/N
        verts.append((w*cos(ang),y,cz+h*sin(ang)))
for i in range(len(profiles)-1):
    for j in range(N):
        a=i*N+j;b=i*N+(j+1)%N;c=(i+1)*N+(j+1)%N;d=(i+1)*N+j
        faces.append((a,b,c,d))
faces+=[tuple(reversed([j for j in range(N)])),tuple((len(profiles)-1)*N+j for j in range(N))]
body=finmesh("Shark organic smoothed torso",verts,faces,skin)
body.data.materials.append(skin_light)
body.data.materials.append(skin_top)
for face in body.data.polygons:
    z=sum(body.data.vertices[i].co.z for i in face.vertices)/len(face.vertices)
    face.material_index=1 if z<1.17 else 2 if z>1.68 else 0
sub=body.modifiers.new("Smooth organic silhouette","SUBSURF");sub.levels=2;sub.render_levels=2
# anatomical lower jaw and mouth reveal
uv("Underjaw pale underside",(0,1.38,1.265),(.38,.40,.13),skin_light)
uv("Open mouth interior slit",(0,1.62,1.315),(.29,.32,.036),mouth)
tube("Mouth line left",[(-.31,1.65,1.326),(-.40,1.31,1.30),(-.39,1.07,1.34)],.018,gill)
tube("Mouth line right",[(.31,1.65,1.326),(.40,1.31,1.30),(.39,1.07,1.34)],.018,gill)
for side in (-1,1):
    uv("Eye socket "+str(side),(side*.455,1.0,1.66),(.085,.125,.115),skin_top)
    uv("Glossy eye white "+str(side),(side*.508,1.016,1.676),(.055,.087,.075),eye_white)
    uv("Iris amber "+str(side),(side*.554,1.035,1.674),(.027,.049,.045),eye_iris)
    uv("Black pupil "+str(side),(side*.577,1.045,1.676),(.016,.029,.032),eye_black)
    uv("Eye specular dot "+str(side),(side*.590,1.060,1.693),(.008,.011,.010),highlight,12,8)
    # five shallow gill slits with gentle curvature, behind the eye
    for i in range(5):
        y=.76-i*.125
        tube("Gill "+str(side)+" "+str(i),[(side*.555,y,1.72),
           (side*.600,y-.040,1.59),(side*.578,y-.03,1.47)],.012,gill)
    # lateral smile furrow
    tube("Mouth crease "+str(side),[(side*.19,1.89,1.376),(side*.39,1.53,1.35),(side*.45,1.18,1.36)],.010,skin_top)
# dorsal fin: make thick sculpture with front-facing curved shark point
fanshape("Large triangular dorsal fin",[(0,-.90,1.68),(0,-.25,1.92),(0,.04,2.45),
    (0,-.05,2.66),(0,-.23,2.65),(0,-.29,2.43),(0,-.63,2.17)],.22,skin_top)
# Smaller rear fin
fanshape("Rear small dorsal fin",[(0,-1.52,1.41),(0,-1.15,1.59),(0,-1.22,1.85),
    (0,-1.32,1.77)],.12,skin)
# 3D caudal/forked tail as tapered stalk and two sharp lobes
uv("Tail peduncle",(0,-1.72,1.40),(.16,.40,.17),skin)
fanshape("Upper caudal blade",[(0,-1.92,1.35),(0,-2.16,1.45),(0,-2.55,2.19),
    (0,-2.45,2.07),(0,-2.40,1.63)],.17,skin_top)
fanshape("Lower caudal blade",[(0,-1.91,1.35),(0,-2.16,1.20),(0,-2.53,.65),
    (0,-2.43,.76),(0,-2.39,1.07)],.17,skin)
for side in (-1,1):
    # pectoral fins have sculpted, outward swept 3D wing outline
    fanshape("Pectoral fin "+str(side),[(side*.50,.33,1.40),
       (side*.80,.15,1.38),(side*1.17,-.06,1.14),
       (side*1.48,-.40,.99),(side*1.35,-.53,1.05),
       (side*.84,-.23,1.30)],.105,skin)
    uv("Pelvic small fin "+str(side),(side*.29,-1.12,1.18),(.31,.26,.08),skin)
# nostrils
for side in (-1,1):
    uv("Nostril "+str(side),(side*.165,1.79,1.53),(.028,.027,.01),gill,14,8)

# Armature & bind each submesh rigidly to exactly one bone.
armdata=bpy.data.armatures.new("Tralalero three leg skeletal rig")
rig=bpy.data.objects.new("Tralalero_Armature",armdata)
bpy.context.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig;rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
def bone(name,head,tail,parent=None):
    b=armdata.edit_bones.new(name);b.head=head;b.tail=tail
    if parent:b.parent=armdata.edit_bones[parent];b.use_connect=False
    return b
bone("Root",(0,0,0),(0,0,.18))
bone("Body",(0,0,1.20),(0,0,1.81),"Root")
bone("Tail",(0,-1.45,1.43),(0,-2.2,1.43),"Body")
bone("Fin_L",(-.45,.12,1.39),(-1,.00,1.25),"Body")
bone("Fin_R",(.45,.12,1.39),(1,.00,1.25),"Body")
legs=[
 ("L",(-.43,.40,.92),(-.46,.36,.48),(-.47,.40,.20)),
 ("R",(.43,.40,.92),(.45,.36,.48),(.45,.40,.20)),
 ("Rear",(0,-1.03,.92),(0,-1.12,.48),(0,-1.10,.20))
]
for nm,hip,knee,ankle in legs:
    bone("Thigh_"+nm,hip,knee,"Body")
    bone("Shin_"+nm,knee,ankle,"Thigh_"+nm)
    bone("Foot_"+nm,ankle,(ankle[0],ankle[1]+.34,.18),"Shin_"+nm)
bpy.ops.object.mode_set(mode='OBJECT')
assign={}
for ob in meshes:
    if ob.name.startswith("Tail") or "caudal" in ob.name:assign[ob.name]="Tail"
    elif ob.name.startswith("Pectoral fin -1"):assign[ob.name]="Fin_L"
    elif ob.name.startswith("Pectoral fin 1"):assign[ob.name]="Fin_R"
    else:assign[ob.name]="Body"
# Add distinctly scaled and detailed 3rd-leg skin + sneakers with individual bones
for nm,hip,knee,ankle in legs:
    footX=ankle[0];footY=ankle[1]
    th=uv("Leg "+nm+" thigh",((hip[0]+knee[0])/2,(hip[1]+knee[1])/2,.69),(.16,.19,.30),skin)
    assign[th.name]="Thigh_"+nm
    sh=uv("Leg "+nm+" shin",((knee[0]+ankle[0])/2,(knee[1]+ankle[1])/2,.33),(.12,.145,.22),skin_light)
    assign[sh.name]="Shin_"+nm
    # sneaker runs forward (+Y). Shoe sole and upper/heel/tab all assigned Foot_x
    pieces=[
     cube("White outsole "+nm,(footX,footY+.22,.085),(.43,.77,.17),shoe_sole,.075),
     cube("Dark tread "+nm,(footX,footY+.22,.017),(.43,.76,.067),shoe_rubber,.04),
     uv("Rounded blue shoe upper "+nm,(footX,footY+.21,.22),(.216,.365,.21),sneaker),
     uv("Toe blue mesh "+nm,(footX,footY+.48,.198),(.20,.18,.144),sneaker_dark),
     uv("Heel counter blue "+nm,(footX,footY-.09,.245),(.19,.14,.19),sneaker_dark),
     cube("Shoe tongue "+nm,(footX,footY+.065,.385),(.215,.25,.05),sneaker_dark,.025),
     cube("Heel loop "+nm,(footX,footY-.12,.43),(.07,.09,.11),shoelace,.025)]
    for ob in pieces:assign[ob.name]="Foot_"+nm
    for side in (-1,1):
        # curved generic athletic side-stripe (no logo)
        ob=tube("Sport side stripe "+nm+" "+str(side),[
          (footX+side*.212,footY-.04,.30),
          (footX+side*.225,footY+.16,.26),
          (footX+side*.18,footY+.42,.20)],.019,highlight)
        assign[ob.name]="Foot_"+nm
    for i in range(4):
        y=footY+.015+i*.075;z=.404-.023*i
        ob=tube("Laces "+nm+" "+str(i),[
          (footX-.10,y,z),(footX+.0,y+.018,z+.008),(footX+.10,y,z)],.011,shoelace)
        assign[ob.name]="Foot_"+nm
# add eyebrow over eyes assigned Body
for ob in meshes:
    b=assign.get(ob.name,"Body")
    grp=ob.vertex_groups.new(name=b)
    grp.add(list(range(len(ob.data.vertices))),1.0,'REPLACE')
    mod=ob.modifiers.new("Skinned to "+b,"ARMATURE");mod.object=rig
    ob.parent=rig
# Action animations
rig.animation_data_create()
for pb in rig.pose.bones:pb.rotation_mode='XYZ'
bones={p.name:p for p in rig.pose.bones}
action_ids={}
def set_pose(pose):
    for bone_name,xyz in pose.items():
        pb=bones[bone_name]
        pb.rotation_euler=xyz
        pb.keyframe_insert(data_path="rotation_euler",group=bone_name)
def anim(name,frames,func):
    a=bpy.data.actions.new(name)
    rig.animation_data.action=a
    for f in frames:
        bpy.context.scene.frame_set(f)
        data=func(f)
        bounce=data.pop("_bounce",0)
        set_pose(data)
        root=bones["Root"]
        root.location.z=bounce
        root.keyframe_insert(data_path="location",group="Root")
    a.use_fake_user=True
    action_ids[name]=a
    return a
def common(phase=0,mult=1):
    result={"Body":(.025*sin(phase*2),0,.017*sin(phase)),
            "Tail":(.07*sin(phase),.02*sin(phase),.14*sin(phase)),
            "Fin_L":(.05*sin(phase),0,.12*sin(phase)),
            "Fin_R":(-.05*sin(phase),0,-.12*sin(phase))}
    for i,nm in enumerate(("L","R","Rear")):
        ang=phase+2*pi*i/3
        result["Thigh_"+nm]=(.48*sin(ang)*mult,0,0)
        result["Shin_"+nm]=(-.24*max(0,sin(ang))*mult,0,0)
        result["Foot_"+nm]=(.16*sin(ang+pi/2)*mult,0,0)
    return result
def idle(f):
    ph=2*pi*(f-1)/48
    p=common(ph,.025)
    p["Body"]=(.014*sin(ph),.010*sin(ph),.015*sin(ph))
    p["_bounce"]=.018*sin(ph)
    return p
def run(f):
    ph=2*pi*(f-1)/32
    p=common(ph,1)
    p["Body"]=(.08*sin(ph*3),0,.048*sin(ph))
    p["_bounce"]=.06*abs(sin(ph*3))
    return p
def jump(f):
    t=(f-1)/31
    p=common(pi*t,.03)
    fold=sin(pi*t)
    p["Body"]=(-.13*fold,0,0)
    for nm in ("L","R","Rear"):
        p["Thigh_"+nm]=(-.25*fold,0,0)
        p["Shin_"+nm]=(.45*fold,0,0)
        p["Foot_"+nm]=(-.15*fold,0,0)
    p["_bounce"]=.30*sin(pi*t)
    return p
def land(f):
    t=(f-1)/23
    p=common(t*2*pi,.06)
    bump=math.exp(-8*t)*sin(11*t)
    p["Body"]=(.10*bump,0,0)
    for nm in ("L","R","Rear"):p["Shin_"+nm]=(.20*math.exp(-6*t),0,0)
    p["_bounce"]=-.13*math.exp(-7*t)
    return p
def victory(f):
    t=(f-1)/55
    ph=2*pi*t
    p=common(ph*3,.38)
    p["Body"]=(.12*sin(2*ph),0,.11*sin(ph))
    p["Tail"]=(.13*sin(ph),0,.38*sin(ph*2))
    p["_bounce"]=.13*abs(sin(ph*3))
    return p
anim("Idle",list(range(1,50,3))+[49],idle)
anim("Run",list(range(1,34,2))+[33],run)
anim("Jump",list(range(1,33,2))+[32],jump)
anim("Land",list(range(1,25,2))+[24],land)
anim("Victory",list(range(1,58,3))+[57],victory)
# default idle
rig.animation_data.action=action_ids["Idle"]
bpy.context.scene.frame_set(1)
# Save .blend before preview setup
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,"Tralalero_v02.blend"))
# FBX exporter; include all actions and bones with visible mesh
bpy.ops.object.select_all(action='DESELECT')
for ob in meshes:ob.select_set(True)
rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,"Tralalero_v02.fbx"),
    use_selection=True,add_leaf_bones=False,apply_scale_options="FBX_SCALE_ALL",
    axis_forward="-Z",axis_up="Y",bake_anim=True,
    bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,
    bake_anim_use_all_actions=True,bake_anim_step=1.0,
    object_types={"MESH","ARMATURE"},path_mode="AUTO",use_mesh_modifiers=True)
# camera preview 3/4 frontal, side, rear
bpy.ops.object.select_all(action='DESELECT')
def aim(ob,target):ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
def light(name,loc,power,size):
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size
    ob=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(ob)
    ob.location=loc;aim(ob,(0,0,1.2))
light("Large left key softbox",(-3,3.5,5),420,5)
light("Cool right softbox",(4,1.2,3.8),270,4)
light("Rim light",(1,-4,4),420,3)
bgmat=material("Preview stage ivory",(.66,.70,.72),0,.82)
plane=cube("PREVIEW GROUND",(0,0,-.05),(18,18,.08),bgmat,.0)
# remove floor from collection of exported meshes (already exported)
camdata=bpy.data.cameras.new("Preview Camera")
cam=bpy.data.objects.new("Preview Camera",camdata);bpy.context.collection.objects.link(cam)
bpy.context.scene.camera=cam
camdata.type='ORTHO';camdata.ortho_scale=5.6
scene=bpy.context.scene
try:scene.render.engine='BLENDER_EEVEE_NEXT'
except:scene.render.engine='BLENDER_EEVEE'
scene.render.resolution_x=900;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.view_settings.view_transform="AgX"
scene.view_settings.look="AgX - Medium High Contrast"
scene.render.image_settings.file_format='PNG'
scene.render.film_transparent=False
scene.world.color=(.5,.55,.6)
for name,loc in [
  ("front_3q",(3.6,5.6,3.1)),
  ("side",(7.1,.1,2.8)),
  ("rear_3q",(-3.6,-5.8,3.0))]:
    cam.location=loc;aim(cam,(0,0,1.35))
    scene.render.filepath=os.path.join(OUT,"Tralalero_"+name+".png")
    bpy.ops.render.render(write_still=True)
# Animated pose previews
cam.location=(3.5,5.3,3.1);aim(cam,(0,0,1.35))
rig.animation_data.action=action_ids["Run"]
for f in (5,17,29):
    scene.frame_set(f)
    scene.render.filepath=os.path.join(OUT,"Tralalero_run_frame_%02d.png"%f)
    bpy.ops.render.render(write_still=True)
rig.animation_data.action=action_ids["Idle"]
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,"Tralalero_v02.blend"))
report={"model":"Tralalero Tralala","blender":bpy.app.version_string,
    "mesh_objects":len(meshes),"triangles":sum(len(o.data.polygons) for o in meshes),
    "bones":[b.name for b in rig.data.bones],
    "actions":{k:[int(v.frame_range[0]),int(v.frame_range[1])] for k,v in action_ids.items()},
    "fbx":"Tralalero_v02.fbx",
    "source":"Tralalero_v02.blend"}
with open(os.path.join(OUT,"model_report.json"),"w",encoding="utf-8") as f:json.dump(report,f,indent=2,ensure_ascii=False)
print("TRALALERO_READY",json.dumps(report,ensure_ascii=False))
