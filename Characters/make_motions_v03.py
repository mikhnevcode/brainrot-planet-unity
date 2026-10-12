import bpy, math, json, os, traceback, sys
from math import pi,sin,cos
from mathutils import Vector
BASE=r"C:\Rabota\projects\BrainrotPlanetUnity\Characters"
CONFIG=[
("Tralalero","Tralalero_v03","Tralalero_v04"),
("Bombardiro","Bombardiro_v02","Bombardiro_v03"),
("Frigo","Frigo_v02","Frigo_v03"),
("Bombombini","Bombombini_v02","Bombombini_v03"),
("Ballerina","Ballerina_v02","Ballerina_v03"),
("Assassino","Assassino_v02","Assassino_v03"),
("Patapim","Patapim_v02","Patapim_v03"),
("TungSahur","TungSahur_v02","TungSahur_v03"),
("Lirili","Lirili_v02","Lirili_v03"),
("Frulli","Frulli_v02","Frulli_v03"),
("Boneca","Boneca_v02","Boneca_v03"),
("Trippi","Trippi_v02","Trippi_v03")]
report=[]
def clamp(q):return max(0.,min(1.,q))
def entry(hero,kind,phase,u):
    r={}
    def rset(names,a,b,c):
        for n in names:
            r[n]=(a,b,c)
    S=sin
    pulse=sin(pi*min(1.,u*1.45)) if kind=="Special" else sin(pi*u)
    if kind=="Drop":
        # Three seconds of kinetic, comical surprise. Gesture changes
        # asynchronously, so a flying machine flaps while a dancer kicks.
        rset(["Body"],.21*S(phase*1.6+hero),.12*S(phase*.8),.18*S(phase*1.9+hero*.5))
        rset(["Head","Neck","Jaw","Beak"],-.17*S(phase*2),.12*S(phase*1.3),.14*S(phase*2.3))
        rset(["Arm_L","Wing_L","Fin_L","Flap_L","Prop_L"],-.53-.43*S(phase*3.1),-.17,.41*S(phase*2.7))
        rset(["Arm_R","Wing_R","Fin_R","Flap_R","Prop_R"],-.53+.43*S(phase*3.3),.17,-.41*S(phase*2.9))
        rset(["Forearm_L","Engine_L"],.39*S(phase*2.6),0,.21*S(phase*2.4))
        rset(["Forearm_R","Engine_R"],-.39*S(phase*2.8),0,-.21*S(phase*2.5))
        rset(["Leg_L","Thigh_L","Foot_L"],.54*S(phase*3.4),0,-.12*S(phase*1.7))
        rset(["Leg_R","Thigh_R","Foot_R"],-.54*S(phase*3.4+1),0,.12*S(phase*1.7))
        rset(["Thigh_Rear","Foot_Rear"],.45*S(phase*3.7+.6),0,.12*S(phase*2.0))
        rset(["Tail","Skirt"],0,.32*S(phase*2.1),.17*S(phase*2.5))
        if hero in (1,3,9):
            rset(["Body"],.11*S(phase*1.2),.12*S(phase*.9),.40*S(phase*1.8))
            rset(["Wing_L","Flap_L"],-.65*S(phase*3.5),.05,.42*S(phase*2.7))
            rset(["Wing_R","Flap_R"],.65*S(phase*3.5),-.05,-.42*S(phase*2.7))
            rset(["Prop_L","Engine_L"],.55*S(phase*5.0),0,.1)
            rset(["Prop_R","Engine_R"],-.55*S(phase*5.0),0,-.1)
        elif hero==4:
            rset(["Body"],.12*S(phase*1.6),.42*S(phase*.6),.16*S(phase*1.9))
            rset(["Arm_L"],-.78+.38*S(phase*3.3),0,-.25)
            rset(["Arm_R"],-.78+.38*S(phase*3.8),0,.25)
            rset(["Leg_L"],.5*S(phase*3.8),0,0)
            rset(["Leg_R"],-.5*S(phase*3.8),0,0)
        elif hero==10:
            rset(["Body"],.33*S(phase*1.8),0,.43*S(phase*2))
            rset(["Leg_L"],.60*S(phase*4.1),0,0)
            rset(["Leg_R"],-.60*S(phase*4.1),0,0)
        elif hero==11:
            rset(["Body"],.22*S(phase*2),.22*S(phase*1),.30*S(phase*2.2))
            rset(["Tail"],0,.5*S(phase*3.5),.25*S(phase*3.5))
        lift=.028*S(phase*3)
    elif kind=="Special":
        # Distinctive attack punctuation without actor/root-world displacement.
        emphasis=pulse
        bodyx=-.15*emphasis
        yaw=.15*S(phase)
        armx=.45*emphasis
        if hero==0:bodyx=-.38*emphasis;armx=.58*emphasis
        if hero==1:bodyx=-.10*emphasis;yaw=.14*S(phase*3)
        if hero==2:bodyx=.26*emphasis;armx=.39*emphasis
        if hero==3:bodyx=-.25*emphasis;yaw=.22*S(phase*3)
        if hero==4:yaw=2*pi*u;armx=.45*emphasis
        if hero==5:bodyx=-.30*emphasis;armx=.83*emphasis
        if hero==6:bodyx=.24*emphasis;armx=.95*emphasis
        if hero==7:bodyx=.18*emphasis;armx=1.12*S(phase*2)*emphasis
        if hero==8:bodyx=.21*emphasis;armx=.57*emphasis
        if hero==9:yaw=.42*S(phase);armx=.93*emphasis
        if hero==10:bodyx=.51*emphasis;armx=.46*emphasis
        if hero==11:bodyx=.28*emphasis;armx=.70*emphasis
        rset(["Body"],bodyx,yaw,.13*S(phase)*emphasis)
        rset(["Head","Neck","Beak"],-.18*emphasis,0,.14*S(phase))
        rset(["Arm_L","Wing_L","Fin_L","Flap_L","Prop_L"],-armx,0,-.15*emphasis)
        rset(["Arm_R","Wing_R","Fin_R","Flap_R","Prop_R"],armx,0,.15*emphasis)
        rset(["Leg_L","Thigh_L"],-.21*emphasis,0,0)
        rset(["Leg_R","Thigh_R"],.23*emphasis,0,0)
        rset(["Tail","Skirt"],0,.31*S(phase),.11*S(phase))
        lift=.06*emphasis
    else:
        # Every character's winner dance has recognizable differences.
        turn=2*pi*u
        strength=sin(pi*u)
        body=(.05*S(phase*2),turn,.1*S(phase*2))
        aL=-.40+.35*S(phase*2);aR=-.40-.35*S(phase*2)
        legL=.20*S(phase*3);legR=-legL
        if hero==0: # 3-legged shark: extravagant surf kick
            body=(.21*S(phase*2),turn,.30*S(phase*2))
            aL=.78*S(phase*3);aR=-aL;legL=.67*S(phase*3);legR=-legL
        elif hero==1: # crocodile plane barrel rolls
            body=(.13*S(phase),.12*S(phase*2),turn*1.0)
            aL=.32*S(phase*4);aR=-aL
        elif hero==2: # refrigerator camel robot dance
            body=(.19*S(phase*4),.13*S(phase*2),.40*S(phase*3))
            aL=.81*S(phase*4);aR=-aL;legL=.49*S(phase*4);legR=-legL
        elif hero==3: # goose bomber corkscrew
            body=(.17*S(phase*2),turn,.31*S(phase*2))
            aL=.78*S(phase*4);aR=-aL
        elif hero==4: # graceful double ballet pirouette
            body=(.10*S(phase*2),turn*2, .08*S(phase*2))
            aL=-1.16+.16*S(phase*2);aR=-1.16-.16*S(phase*2)
            legL=.34*S(phase*.8);legR=-.51*S(phase*.8)
        elif hero==5: # ninja sword salute into spin
            body=(-.24*S(phase*2),turn,.18*S(phase))
            aL=-1.0+.47*S(phase*4);aR=.90-.47*S(phase*4)
        elif hero==6: # broad tree-monkey arm wave
            body=(.27*S(phase),.28*S(phase*2),.29*S(phase))
            aL=1.05*S(phase*2);aR=1.05*S(phase*2+.8)
        elif hero==7: # celebratory drum beats
            body=(.22*S(phase*4),.20*S(phase),.13*S(phase*4))
            aL=.75*S(phase*4);aR=.95*S(phase*4+pi)
            legL=.43*S(phase*4);legR=-legL
        elif hero==8: # elephant cactus trunk swing
            body=(.17*S(phase*2),turn,.25*S(phase*2))
            aL=.75*S(phase*2);aR=-aL
        elif hero==9: # bird wing flourish
            body=(-.12*S(phase*2),turn,.34*S(phase*2))
            aL=-.85+.30*S(phase*5);aR=.85-.30*S(phase*5)
        elif hero==10: # tire frog full wheeling dance
            body=(.08*S(phase*2),0,turn)
            aL=.70*S(phase*2);aR=-aL;legL=.60*S(phase*3);legR=-legL
        elif hero==11: # shrimp cat zigzag shuffle
            body=(.18*S(phase*2),turn,.34*S(phase*3))
            aL=.53*S(phase*4);aR=-aL;legL=.61*S(phase*4);legR=-legL
        rset(["Body"],*body)
        rset(["Head","Neck","Beak","Jaw"],.19*S(phase*2),0,.13*S(phase*2))
        rset(["Arm_L","Wing_L","Fin_L","Flap_L","Prop_L"],aL,0,.18*S(phase*2))
        rset(["Arm_R","Wing_R","Fin_R","Flap_R","Prop_R"],aR,0,-.18*S(phase*2))
        rset(["Leg_L","Thigh_L","Foot_L"],legL,0,.05*S(phase))
        rset(["Leg_R","Thigh_R","Foot_R"],legR,0,-.05*S(phase))
        rset(["Thigh_Rear","Foot_Rear"],.38*S(phase*3+.6),0,0)
        rset(["Tail","Skirt"],0,.36*S(phase*3),.19*S(phase*3))
        rset(["Forearm_L","Engine_L"],.38*S(phase*2),0,.08*S(phase*2))
        rset(["Forearm_R","Engine_R"],-.38*S(phase*2),0,-.08*S(phase*2))
        lift=.11*abs(S(phase*2))*strength
    # Neutral first/last poses prevent a visible snap when Unity enters or leaves the clip.
    boundary=min(1.0,u/.10 if u>0 else 0.0,(1.0-u)/.12 if u<1 else 0.0)
    r={name:tuple(value*boundary for value in rotation) for name,rotation in r.items()}
    lift*=boundary
    return r,lift

def keyframe_action(rig,hero,actName,kind,numFrames):
    act=bpy.data.actions.new(actName)
    rig.animation_data_create()
    rig.animation_data.action=act
    bones={b.name:b for b in rig.pose.bones}
    for b in bones.values():
        b.rotation_mode="XYZ"
    for frame in range(1,numFrames+1,3):
        phase=2*pi*(frame-1)/(numFrames-1)
        u=(frame-1)/(numFrames-1)
        rotations,lift=entry(hero,kind,phase,u)
        bpy.context.scene.frame_set(frame)
        for bone in rig.pose.bones:
            rot=rotations.get(bone.name,(0.0,0.0,0.0))
            bone.rotation_euler=rot
            bone.keyframe_insert(data_path="rotation_euler",frame=frame,group=bone.name)
            if bone.name=="Root":
                bone.location=(0,0,lift)
                bone.keyframe_insert(data_path="location",frame=frame,group="Root")
    act.use_fake_user=True
    print("ACTION_CREATED",rig.name,act.name,list(act.frame_range),flush=True)
    return act

def run_one(i,hero,oldVer,newVer):
    folder=os.path.join(BASE,hero)
    oldBlend=os.path.join(folder,oldVer+".blend")
    newBlend=os.path.join(folder,newVer+".blend")
    newFBX=os.path.join(folder,newVer+".fbx")
    if not os.path.isfile(oldBlend):
        raise FileNotFoundError(oldBlend)
    bpy.ops.wm.open_mainfile(filepath=oldBlend)
    rigs=[ob for ob in bpy.data.objects if ob.type=="ARMATURE"]
    if len(rigs)!=1:
        raise RuntimeError("Unexpected rig count %d in %s"%(len(rigs),hero))
    rig=rigs[0]
    # Preserve only the five clips belonging to THIS active armature.
    # Early versions generated several characters in one Blender process
    # without clearing action datablocks. Later source files consequently
    # contain Idle.007, Run.007 etc alongside stale earlier characters.
    before=[a.name for a in bpy.data.actions]
    active=rig.animation_data.action.name if rig.animation_data and rig.animation_data.action else "Idle"
    suffix=active[len("Idle"):] if active.startswith("Idle") else ""
    original=["Idle","Run","Jump","Land","Victory"]
    keep={base+suffix for base in original}
    missing=[n for n in keep if bpy.data.actions.get(n) is None]
    if missing:raise RuntimeError("Missing own source clips "+hero+": "+str(missing))
    for action in list(bpy.data.actions):
        if action.name not in keep:
            bpy.data.actions.remove(action,do_unlink=True)
    for base in original:
        bpy.data.actions[base+suffix].name=base
    print("ORIGINAL_ACTIONS_RETAINED",hero,original,"removed",len(before)-5,flush=True)
    # Source-model polish: exported materials stay matte and avoid tiny white
    # phone-render highlights. Unity still applies its established color palette.
    for material in bpy.data.materials:
        if hasattr(material,"roughness"):material.roughness=max(.72,material.roughness)
        if hasattr(material,"metallic"):material.metallic=0.0
        if hasattr(material,"specular_ior_level"):material.specular_ior_level=min(.25,material.specular_ior_level)
        material["brainrot_polish_version"]="0.5.9"
    newActs={}
    for action,kind,frames in [("Drop","Drop",73),("Special","Special",34),("VictoryShow","Victory",73)]:
        if bpy.data.actions.get(action):
            bpy.data.actions.remove(bpy.data.actions[action],do_unlink=True)
        newActs[action]=keyframe_action(rig,i,action,kind,frames)
    bpy.context.scene.frame_set(1)
    rig.animation_data.action=newActs["Drop"]
    # Versioned result; previous .blend is not modified.
    bpy.ops.wm.save_as_mainfile(filepath=newBlend)
    bpy.ops.object.select_all(action="DESELECT")
    selected=[rig]+[o for o in bpy.data.objects if o.type=="MESH" and
      (o.parent==rig or any(m.type=="ARMATURE" and m.object==rig for m in o.modifiers))]
    for o in selected:o.select_set(True)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.fbx(filepath=newFBX,use_selection=True,
        object_types={"MESH","ARMATURE"},bake_anim=True,
        bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,
        bake_anim_use_all_bones=True,add_leaf_bones=False,
        axis_forward="-Z",axis_up="Y")
    after=[a.name for a in bpy.data.actions]
    report={"hero":hero,"new":newVer,"source":oldVer,
        "meshes":len(selected)-1,"bones":len(rig.data.bones),
        "old_actions":before,"all_actions":after,
        "new_actions":{n:list(a.frame_range) for n,a in newActs.items()},
        "blend_bytes":os.path.getsize(newBlend),
        "fbx_bytes":os.path.getsize(newFBX)}
    with open(os.path.join(folder,newVer+"_motion.json"),"w",encoding="utf-8") as f:
        json.dump(report,f,ensure_ascii=False,indent=2)
    print("UPDATED_HERO",json.dumps(report,ensure_ascii=False),flush=True)
    return report

limit=os.environ.get("BP_CHARACTER","")
for i,(hero,oldVer,newVer) in enumerate(CONFIG):
    if limit and limit.lower()!=hero.lower():continue
    try:report.append(run_one(i,hero,oldVer,newVer))
    except Exception as exc:
        print("BLENDER_ANIMATION_FAILED",hero,traceback.format_exc(),flush=True)
        raise
with open(os.path.join(BASE,"motions_v03_report.json"),"w",encoding="utf-8") as f:
    json.dump(report,f,ensure_ascii=False,indent=2)
print("NEW_CLIPS_COMPLETE",len(report),flush=True)