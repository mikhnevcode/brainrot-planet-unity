import bpy, math, os, json, traceback
from math import sin, cos, pi
from mathutils import Vector
ROOT=r"C:\Rabota\projects\BrainrotPlanetUnity\Characters"
NAMES=["Assassino","Patapim","TungSahur","Lirili","Frigo","Frulli","Boneca","Trippi"]
objs=[]; groups={}; materials={}
def mat(n,rgb):
    m=bpy.data.materials.new(n);m.diffuse_color=(*rgb,1)
    m.use_nodes=True
    bs=m.node_tree.nodes.get("Principled BSDF")
    bs.inputs["Base Color"].default_value=(*rgb,1)
    bs.inputs["Roughness"].default_value=.74
    bs.inputs["Metallic"].default_value=0
    return m
def record(o,name,m,bone="Body"):
    o.name=name
    if m:o.data.materials.append(m)
    for p in o.data.polygons:p.use_smooth=True
    objs.append(o);groups[o.name]=bone
    return o
def sphere(n,xyz,scale,m,b="Body",seg=20):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg,ring_count=12,location=xyz)
    o=bpy.context.object;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return record(o,n,m,b)
def cube(n,xyz,scale,m,b="Body",bev=.05):
    bpy.ops.mesh.primitive_cube_add(size=1,location=xyz)
    o=bpy.context.object;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bev:
        v=o.modifiers.new("Soft edges","BEVEL");v.width=bev;v.segments=2
        o.modifiers.new("Normals","WEIGHTED_NORMAL")
    return record(o,n,m,b)
def cone(n,xyz,r1,r2,depth,m,b="Body",verts=24):
    bpy.ops.mesh.primitive_cone_add(vertices=verts,radius1=r1,radius2=r2,depth=depth,location=xyz)
    return record(bpy.context.object,n,m,b)
def cylinder(n,xyz,r,depth,m,b="Body",verts=24,axis="Z"):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=depth,location=xyz)
    o=bpy.context.object
    if axis=="Y":o.rotation_euler.x=pi/2
    if axis=="X":o.rotation_euler.y=pi/2
    return record(o,n,m,b)
def torus(n,xyz,major,minor,m,b="Body",axis="Z"):
    bpy.ops.mesh.primitive_torus_add(major_segments=32,minor_segments=8,location=xyz,
                                   major_radius=major,minor_radius=minor)
    o=bpy.context.object
    if axis=="Y":o.rotation_euler.x=pi/2
    return record(o,n,m,b)
def tube(n,points,thick,m,b="Body"):
    c=bpy.data.curves.new(n,"CURVE");c.dimensions="3D";c.bevel_depth=thick;c.bevel_resolution=2
    s=c.splines.new("BEZIER");s.bezier_points.add(len(points)-1)
    for node,xyz in zip(s.bezier_points,points):
        node.co=xyz;node.handle_left_type="AUTO";node.handle_right_type="AUTO"
    c.materials.append(m);o=bpy.data.objects.new(n,c);bpy.context.collection.objects.link(o)
    bpy.ops.object.select_all(action="DESELECT");o.select_set(True);bpy.context.view_layer.objects.active=o
    bpy.ops.object.convert(target="MESH")
    return record(bpy.context.object,n,None,b)
def eyes(y,z,x=.25,white=None,iris=None,pupil=None,b="Head",scale=1.0):
    for side in (-1,1):
        sphere("Eye sclera "+str(side),(side*x,y,z),(.13*scale,.072*scale,.17*scale),white,b,16)
        sphere("Eye iris "+str(side),(side*x,y+.060*scale,z),(.068*scale,.030*scale,.112*scale),iris,b,16)
        sphere("Eye pupil "+str(side),(side*x,y+.083*scale,z),(.036*scale,.018*scale,.077*scale),pupil,b,12)
def shoe_pair(color,dark,y=0):
    for s,b in [(-1,"Leg_L"),(1,"Leg_R")]:
        sphere("Foot "+b,(s*.24,y+.11,.17),(.20,.36,.17),color,b)
        sphere("Sole "+b,(s*.24,y+.14,.065),(.21,.37,.067),dark,b)
def limbs(skin,shoe):
    for s,b in [(-1,"L"),(1,"R")]:
        tube("Arm "+b,[(s*.43,0,1.72),(s*.61,.03,1.41),(s*.67,.10,1.22)],.095,skin,"Arm_"+b)
        sphere("Hand "+b,(s*.67,.10,1.18),(.12,.11,.14),skin,"Arm_"+b)
        tube("Leg "+b,[(s*.24,0,.78),(s*.24,.025,.46),(s*.24,.11,.24)],.105,skin,"Leg_"+b)
    shoe_pair(shoe,skin)
def color_palette():
    return dict(cream=mat("cream ceramic",(.89,.78,.62)),espresso=mat("espresso dark",(.23,.105,.055)),
    black=mat("graphite fabric",(.080,.093,.11)), charcoal=mat("soot black",(.025,.034,.049)),
    white=mat("warm white",(.96,.94,.87)),steel=mat("silver metal",(.59,.68,.73)),
    gold=mat("brass gold",(.75,.47,.18)),rose=mat("pink rose",(.91,.36,.51)),
    green=mat("forest green",(.19,.39,.16)),leaf=mat("lime leaf",(.42,.67,.22)),
    wood=mat("rich wood",(.52,.32,.16)),woodlight=mat("woodgrain caramel",(.73,.48,.24)),
    blue=mat("sky ice blue",(.46,.79,.87)),orange=mat("sunset orange",(.94,.39,.13)),
    red=mat("deep crimson",(.67,.15,.16)),tan=mat("sandy khaki",(.70,.59,.37)),
    rubber=mat("tyre rubber",(.087,.11,.13)),frog=mat("frog green",(.31,.63,.20)),
    skin=mat("soft human skin",(.89,.65,.46)),salmon=mat("shrimp salmon",(.87,.39,.30)),
    peach=mat("orange cat fur",(.84,.47,.21)),pink=mat("shell pale pink",(.98,.70,.59)))
def build_assa(M):
    # Stout tapered paper take-away cup with visible lid, focused ninja face, two curved swords.
    cone("Takeaway cup core",(0,0,1.70),.37,.55,1.43,M['cream'])
    torus("Coffee cup lid lip",(0,0,2.43),.56,.050,M['espresso'],"Head")
    cylinder("Coffee cup lid",(0,0,2.46),.54,.105,M['charcoal'],"Head")
    cylinder("Hot coffee seal",(0,0,2.52),.38,.023,M['espresso'],"Head")
    torus("Sleeve coffee logo",(0,0,1.73),.395,.035,M['gold'])
    cylinder("Ninja black face wrap",(0,.412,1.52),.34,.50,M['black'],"Body",axis="Y")
    sphere("Ninja head eyes mask",(0,.44,2.04),(.49,.12,.21),M['charcoal'],"Head")
    eyes(.535,2.04,.23,M['white'],M['wood'],M['charcoal'])
    tube("Angry eyebrows",[(-.38,.59,2.19),(-.23,.62,2.25),(-.11,.60,2.20)],.018,M['black'],"Head")
    tube("Angry eyebrows mirror",[(.11,.60,2.20),(.23,.62,2.25),(.38,.59,2.19)],.018,M['black'],"Head")
    cube("Ninja headband",(0,.49,2.27),(.94,.11,.24),M['black'],"Head")
    cube("Original spiral badge",(0,.56,2.28),(.27,.042,.20),M['steel'],"Head",.025)
    tube("Spiral insignia",[(0,.59,2.29),(.08,.60,2.28),(.045,.60,2.33),(-.05,.60,2.31)],.017,M['charcoal'],"Head")
    limbs(M['black'],M['black'])
    for side,b in [(-1,"L"),(1,"R")]:
        # Stylised wooden prop blade; no sharp physical collider.
        tube("Sword brown handle "+b,[(side*.66,.18,1.24),(side*.84,.19,1.56)],.060,M['wood'],"Arm_"+b)
        cylinder("Gold sword guard "+b,(side*.85,.19,1.54),.11,.044,M['gold'],"Arm_"+b)
        tube("Curved silver sword "+b,[(side*.85,.19,1.57),(side*1.02,.20,1.94),(side*1.11,.17,2.37)],.043,M['steel'],"Arm_"+b)
        tube("Sword bright edge "+b,[(side*.95,.22,1.75),(side*1.07,.19,2.17),(side*1.11,.17,2.39)],.014,M['white'],"Arm_"+b)
def build_patapim(M):
    cone("Old tree-trunk torso",(0,0,1.36),.55,.42,1.50,M['wood'])
    for i in range(12):
        a=i*2*pi/12
        tube("Bark ridge "+str(i),[(.46*cos(a),.46*sin(a),.66),(.49*cos(a),.49*sin(a),1.44),(.38*cos(a),.38*sin(a),1.91)],.022,M['woodlight'])
    sphere("Monkey facial head",(0,.24,2.25),(.46,.41,.50),M['woodlight'],"Head")
    sphere("Monkey cheek left",(-.34,.52,2.12),(.24,.18,.27),M['tan'],"Head")
    sphere("Monkey cheek right",(.34,.52,2.12),(.24,.18,.27),M['tan'],"Head")
    sphere("Monkey muzzle",(0,.61,2.08),(.30,.28,.22),M['tan'],"Head")
    tube("Long monkey nose",[(0,.62,2.35),(0,.78,2.21),(0,.92,1.91)],.11,M['tan'],"Head")
    eyes(.59,2.38,.235,M['white'],M['wood'],M['black'],scale=.83)
    for s in (-1,1):
        sphere("Round monkey ear"+str(s),(s*.48,.15,2.40),(.20,.16,.23),M['wood'],"Head")
    for i in range(17):
        a=i*2*pi/17
        sphere("Green forest treetop "+str(i),(.61*cos(a),.46*sin(a),2.85+.15*sin(i*2)),(.31,.31,.31),M['leaf'] if i%2 else M['green'],"Head",14)
    limbs(M['wood'],M['wood'])
    shoe_pair(M['tan'],M['wood'])
    for s in (-1,1):
        for i in range(3):
            tube("Vine arm "+str(s)+str(i),[(s*.41,.10,1.8),(s*(.77+i*.10),.19,1.39),(s*(.78+i*.08),.43,1.18)],.031,M['green'],"Arm_L" if s<0 else "Arm_R")
def build_tung(M):
    cylinder("Tall wooden percussion trunk",(0,0,1.58),.46,2.45,M['wood'])
    sphere("Rounded wood crown",(0,0,2.79),(.46,.46,.22),M['wood'],"Head")
    sphere("Rounded base",(0,0,.39),(.46,.46,.17),M['wood'])
    for s in (-1,1):
        eyes(.444,2.12,.235,M['white'],M['wood'],M['charcoal'],scale=.8)
    tube("Smiling carved mouth",[(-.17,.479,1.72),(0,.51,1.67),(.17,.479,1.72)],.024,M['espresso'],"Head")
    for i in range(14):
        z=.56+i*.148
        tube("Woodgrain vertical line "+str(i),[(.36*sin(i),.412,z-.11),(.41*sin(i+.35),.412,z+.10)],.014,M['woodlight'])
    limbs(M['woodlight'],M['espresso'])
    tube("Baseball bat staff",[(.78,.18,1.00),(.96,.18,1.80),(1.08,.17,2.53)],.091,M['wood'],"Arm_R")
    sphere("Bat rounded end",(1.08,.17,2.53),(.11,.11,.18),M['woodlight'],"Arm_R")
def build_lirili(M):
    sphere("Upright cactus body",(0,0,1.56),(.49,.36,.91),M['green'])
    sphere("Elephant round forehead",(0,.12,2.34),(.48,.47,.49),M['green'],"Head")
    for s in (-1,1):
        sphere("Huge elephant ear "+str(s),(s*.51,.07,2.33),(.28,.12,.45),M['leaf'],"Head")
        sphere("Inner ear "+str(s),(s*.56,.18,2.33),(.19,.044,.33),M['rose'],"Head")
    tube("Long curled elephant trunk",[(0,.49,2.27),(0,.75,1.98),(.04,.88,1.59),(.19,.88,1.58)],.15,M['green'],"Head")
    eyes(.50,2.47,.25,M['white'],M['wood'],M['black'],scale=.68)
    for s in (-1,1):
        tube("Short ivory tusk "+str(s),[(s*.21,.55,1.99),(s*.28,.72,1.80)],.057,M['white'],"Head")
    for i in range(42):
        a=i*2.40;z=.80+(i%15)*.115
        x=.49*cos(a);y=.36*sin(a)
        sphere("Cactus thorn "+str(i),(x,y,z),(.036,.031,.052),M['tan'],"Body",10)
    limbs(M['green'],M['tan'])
    shoe_pair(M['tan'],M['espresso'],.12)
    # Clasped pocket-watch as subtle lore accessory.
    cylinder("Clock dial",(.48,-.38,1.44),.14,.07,M['gold'],"Arm_R",axis="Y")
    cylinder("Clock face",(.48,-.425,1.44),.105,.022,M['white'],"Arm_R",axis="Y")
def build_frigo(M):
    cube("Refrigerator shell",(0,0,1.50),(1.00,.80,1.65),M['white'],"Body",.11)
    cube("Freezer top drawer",(0,.408,2.01),(.85,.038,.55),M['blue'])
    cube("Main fridge door",(0,.416,1.18),(.85,.050,1.01),M['cream'])
    cube("Door handle",( .33,.480,1.59),(.075,.087,.38),M['steel'])
    tube("Fridge metal rim",[(-.47,.46,.67),(-.48,.46,1.48),(-.47,.46,2.32)],.025,M['steel'])
    tube("Camel long neck",[(-.30,-.15,2.28),(-.40,-.09,2.56),(-.50,.25,2.78)],.19,M['tan'],"Head")
    sphere("Camel face",(-.55,.34,2.90),(.30,.35,.30),M['tan'],"Head")
    sphere("Camel broad snout",(-.55,.60,2.78),(.28,.29,.13),M['cream'],"Head")
    for s in (-1,1):
        sphere("Camel ear "+str(s),(-.55+s*.24,.14,3.11),(.09,.10,.19),M['wood'],"Head")
        sphere("Camel eye "+str(s),(-.55+s*.19,.55,2.99),(.07,.052,.08),M['black'],"Head")
    sphere("Camel furry hump",(-.04,-.26,2.35),(.39,.32,.30),M['tan'])
    limbs(M['tan'],M['red'])
    shoe_pair(M['red'],M['rubber'])
def build_frulli(M):
    sphere("Penguin dark body",(0,0,1.52),(.54,.43,.88),M['black'])
    sphere("Penguin white breast",(0,.33,1.48),(.41,.13,.72),M['white'])
    sphere("Bird skull",(0,.12,2.40),(.44,.41,.50),M['black'],"Head")
    cone("Massive orange toucan beak",(0,.71,2.25),.28,.10,.75,M['orange'],"Head",24).rotation_euler.x=pi/2
    for s in (-1,1):
        sphere("Orange goggles "+str(s),(s*.24,.49,2.53),(.20,.09,.19),M['gold'],"Head")
        sphere("Dark lens "+str(s),(s*.24,.566,2.53),(.13,.031,.13),M['rubber'],"Head")
        sphere("Goggle glint "+str(s),(s*.195,.591,2.59),(.04,.014,.04),M['blue'],"Head")
    tube("Goggle bridge",[(-.1,.54,2.55),(.0,.565,2.55),(.1,.54,2.55)],.045,M['gold'],"Head")
    for i in range(5):
        sphere("Spiky crest "+str(i),((i-2)*.14,-.07,2.91),(.12,.16,.26),M['black'],"Head")
    for s,label in [(-1,"L"),(1,"R")]:
        sphere("Flight feather wing "+label,(s*.67,-.04,1.68),(.43,.18,.50),M['black'],"Arm_"+label)
        tube("Paddle feather tips "+label,[(s*.73,-.06,1.37),(s*.95,.06,1.07)],.10,M['white'],"Arm_"+label)
        tube("Long bird leg "+label,[(s*.23,.0,.78),(s*.22,.06,.37)],.10,M['orange'],"Leg_"+label)
    shoe_pair(M['orange'],M['espresso'],.17)
def build_boneca(M):
    # A black vertical wheel is the BODY with clear rubber grooves, frog above and human feet.
    torus("Large automobile tire body",(0,0,1.35),.69,.25,M['rubber'],"Body",axis="Y")
    cylinder("Inset tire hub",(0,0,1.35),.41,.22,M['steel'],"Body",axis="Y")
    cylinder("Hub dark core",(0,.13,1.35),.16,.10,M['rubber'],"Body",axis="Y")
    for i in range(18):
        a=i*2*pi/18
        xyz=(.73*cos(a),.04,1.35+.73*sin(a))
        cube("Tyre tread "+str(i),xyz,(.16,.38,.14),M['charcoal'],"Body",.012)
    sphere("Large frog face",(0,.1,2.48),(.62,.51,.42),M['frog'],"Head")
    sphere("Frog lower jaw",(0,.46,2.29),(.53,.23,.22),M['leaf'],"Head")
    for s in (-1,1):
        sphere("Frog eye stalk "+str(s),(s*.40,.26,2.76),(.22,.23,.21),M['leaf'],"Head")
        sphere("Gold iris "+str(s),(s*.41,.45,2.75),(.14,.078,.13),M['gold'],"Head")
        sphere("Dark pupil "+str(s),(s*.41,.517,2.75),(.071,.025,.084),M['charcoal'],"Head")
    tube("Frog smile",[(-.33,.58,2.26),(0,.65,2.19),(.33,.58,2.26)],.018,M['espresso'],"Head")
    for s,label in [(-1,"L"),(1,"R")]:
        tube("Skinny amphibian arm "+label,[(s*.55,0,1.70),(s*.84,.07,1.49),(s*.85,.10,1.28)],.087,M['frog'],"Arm_"+label)
        tube("Human leg "+label,[(s*.32,0,.93),(s*.27,.04,.45),(s*.24,.14,.25)],.125,M['skin'],"Leg_"+label)
    shoe_pair(M['skin'],M['tan'])
def build_trippi(M):
    # The body is a curved segmented shrimp shell topped by a fluffy expressive orange cat face.
    sphere("Shrimp abdomen core",(0,-.10,1.37),(.48,.81,.50),M['salmon'])
    for k in range(7):
        sphere("Shrimp shell plate "+str(k),(0,-.60+.22*k,1.40+.13*sin(k*.68)),
               (.48-.03*k,.20,.42-.025*k),M['salmon'] if k%2 else M['pink'])
    sphere("Cat face fuzzy",(0,.63,2.13),(.43,.38,.42),M['peach'],"Head")
    sphere("Cat white muzzle",(0,.94,1.99),(.30,.11,.18),M['white'],"Head")
    for s in (-1,1):
        cone("Pointy cat ear "+str(s),(s*.30,.54,2.54),.19,.025,.40,M['peach'],"Head",18)
        sphere("Inner pink ear "+str(s),(s*.30,.68,2.57),(.09,.04,.14),M['rose'],"Head")
        sphere("Cat eyeball "+str(s),(s*.18,.984,2.21),(.10,.05,.14),M['white'],"Head")
        sphere("Cat slit pupil "+str(s),(s*.18,1.027,2.21),(.034,.022,.093),M['charcoal'],"Head")
        for k in range(3):
            tube("Cat whisker %s %s"%(s,k),[(s*.20,1.022,1.97),(s*.52,1.03,1.99+(k-1)*.075)],.011,M['white'],"Head")
    sphere("Pink cat nose",(0,1.068,2.035),(.070,.042,.041),M['rose'],"Head")
    for i in range(5):
        for s in (-1,1):
            x=s*.44;y=-.66+i*.25
            tube("Shrimp walking limb "+str(s)+str(i),[(x,y,1.28),(x+s*.20,y+.08,.92)],.055,M['salmon'],"Arm_L" if s<0 else "Arm_R")
    for s in (-1,1):
        sphere("Paddle shrimp tail "+str(s),(s*.35,-1.12,1.48),(.30,.38,.09),M['salmon'],"Body")
    tube("Shrimp left player leg",[(-.24,0,.94),(-.24,.06,.36)],.12,M['salmon'],"Leg_L")
    tube("Shrimp right player leg",[(.24,0,.94),(.24,.06,.36)],.12,M['salmon'],"Leg_R")
    shoe_pair(M['peach'],M['salmon'])
builders=dict(Assassino=build_assa,Patapim=build_patapim,TungSahur=build_tung,Lirili=build_lirili,
              Frigo=build_frigo,Frulli=build_frulli,Boneca=build_boneca,Trippi=build_trippi)
def construct_rig(model):
    arm=bpy.data.armatures.new(model+" Skeleton")
    rig=bpy.data.objects.new(model+"_Armature",arm);bpy.context.collection.objects.link(rig)
    bpy.ops.object.select_all(action="DESELECT");rig.select_set(True)
    bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode="EDIT")
    def bone(name,start,end,par=None):
        b=arm.edit_bones.new(name);b.head=start;b.tail=end
        if par:b.parent=arm.edit_bones[par]
    bone("Root",(0,0,.10),(0,0,.29))
    bone("Body",(0,0,1.14),(0,0,1.77),"Root")
    bone("Head",(0,.04,2.04),(0,.04,2.43),"Body")
    for s,label in [(-1,"L"),(1,"R")]:
        bone("Arm_"+label,(s*.38,0,1.81),(s*.74,.10,1.22),"Body")
        bone("Leg_"+label,(s*.23,0,.98),(s*.24,.10,.20),"Root")
    bpy.ops.object.mode_set(mode="OBJECT")
    cache={n:[ob for ob in objs if groups[ob.name]==n] for n in set(groups.values())}
    out=[]
    for sec,members in cache.items():
        if sec not in rig.pose.bones:raise RuntimeError("Unknown section "+sec)
        for o in members:
            bpy.ops.object.select_all(action="DESELECT");o.select_set(True);bpy.context.view_layer.objects.active=o
            for mod in list(o.modifiers):
                try:bpy.ops.object.modifier_apply(modifier=mod.name)
                except Exception as exc:print("MODIFIER_WARNING",model,o.name,str(exc))
        bpy.ops.object.select_all(action="DESELECT")
        for ob in members:ob.select_set(True)
        bpy.context.view_layer.objects.active=members[0]
        if len(members)>1:bpy.ops.object.join()
        o=bpy.context.view_layer.objects.active;o.name="Combined_"+sec
        group=o.vertex_groups.new(name=sec);group.add(list(range(len(o.data.vertices))),1,"REPLACE")
        modifier=o.modifiers.new("Character armature","ARMATURE");modifier.object=rig
        o.parent=rig;out.append(o)
    return rig,out
def animation(rig):
    rig.animation_data_create()
    bones={x.name:x for x in rig.pose.bones}
    for b in bones.values():b.rotation_mode="XYZ"
    actions={}
    def make(name,count):
        act=bpy.data.actions.new(name);rig.animation_data.action=act
        for i in range(1,count+1,2):
            bpy.context.scene.frame_set(i);phase=2*pi*(i-1)/(count-1)
            angle={};rise=0
            if name=="Idle":
                angle={"Body":(.015*sin(phase),0,.028*sin(phase)),
                       "Head":(.03*sin(phase),0,.04*sin(phase))}
                rise=.026*sin(phase)
            elif name=="Run":
                angle={"Body":(.07*sin(phase),0,.06*sin(phase)),
                       "Head":(.04*sin(phase+.9),0,-.04*sin(phase)),
                       "Arm_L":(-.28*sin(phase),0,.12),
                       "Arm_R":(.28*sin(phase),0,-.12),
                       "Leg_L":(.38*sin(phase),0,0),
                       "Leg_R":(-.38*sin(phase),0,0)}
                rise=.05*abs(sin(phase))
            elif name=="Jump":
                f=sin(phase/2)
                angle={"Body":(-.08*f,0,0),"Leg_L":(-.30*f,0,0),
                       "Leg_R":(-.30*f,0,0),"Arm_L":(-.32*f,0,-.20*f),
                       "Arm_R":(-.32*f,0,.20*f)}
                rise=.29*f
            elif name=="Land":
                f=math.exp(-phase)
                angle={"Body":(.25*f,0,0),"Leg_L":(-.25*f,0,0),
                       "Leg_R":(-.25*f,0,0)}
                rise=-.08*f
            elif name=="Victory":
                angle={"Body":(0,0,phase),"Head":(.1*sin(phase),0,.16*sin(phase)),
                       "Arm_L":(-.4,0,-.18),"Arm_R":(-.4,0,.18),
                       "Leg_L":(.12*sin(phase),0,0),"Leg_R":(-.12*sin(phase),0,0)}
                rise=.10*abs(sin(2*phase))
            for n,b in bones.items():
                b.rotation_euler=angle.get(n,(0,0,0))
                b.keyframe_insert(data_path="rotation_euler",group=n)
            bones["Root"].location.z=rise
            bones["Root"].keyframe_insert(data_path="location",group="Root")
        act.use_fake_user=True
        actions[name]=act
    for name,count in [("Idle",49),("Run",33),("Jump",33),("Land",25),("Victory",65)]:
        make(name,count)
    rig.animation_data.action=actions["Idle"];bpy.context.scene.frame_set(1)
    return actions
def render_preview(out,rig,actions,model):
    def aim(ob,xyz):ob.rotation_euler=(Vector(xyz)-ob.location).to_track_quat("-Z","Y").to_euler()
    for n,loc,power in [("Key",(-3,4,6),710),("Fill",(4,4,4),470),("Rim",(0,-4,5),540)]:
        d=bpy.data.lights.new(n,"AREA");d.energy=power;d.size=5
        o=bpy.data.objects.new(n,d);bpy.context.collection.objects.link(o);o.location=loc
        aim(o,(0,0,1.6))
    d=bpy.data.cameras.new("Portrait");cam=bpy.data.objects.new("Portrait",d)
    bpy.context.collection.objects.link(cam);bpy.context.scene.camera=cam
    d.type="ORTHO";d.ortho_scale=5.1
    sc=bpy.context.scene;sc.render.engine="BLENDER_EEVEE"
    sc.render.resolution_x=640;sc.render.resolution_y=640;sc.render.resolution_percentage=100
    sc.render.image_settings.file_format="PNG";sc.view_settings.view_transform="AgX"
    for name,loc in [("front",(3.6,6,3.6)),("side",(7,0,3.6)),("game_top",(3,4,8.0))]:
        cam.location=loc;aim(cam,(0,0,1.6))
        sc.render.filepath=os.path.join(out,model+"_"+name+".png")
        bpy.ops.render.render(write_still=True)
    rig.animation_data.action=actions["Run"];sc.frame_set(9)
    cam.location=(3.6,6,3.6);aim(cam,(0,0,1.6))
    sc.render.filepath=os.path.join(out,model+"_run09.png")
    bpy.ops.render.render(write_still=True)
def build(name):
    global objs,groups
    bpy.ops.object.select_all(action="SELECT");bpy.ops.object.delete(use_global=False)
    objs=[];groups={}
    directory=os.path.join(ROOT,name);os.makedirs(directory,exist_ok=True)
    M=color_palette()
    builders[name](M)
    initial=len(objs)
    rig,meshes=construct_rig(name);acts=animation(rig)
    blend=os.path.join(directory,name+"_v01.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend)
    bpy.ops.object.select_all(action="DESELECT")
    rig.select_set(True)
    for o in meshes:o.select_set(True)
    bpy.context.view_layer.objects.active=rig
    fbx=os.path.join(directory,name+"_v01.fbx")
    bpy.ops.export_scene.fbx(filepath=fbx,use_selection=True,object_types={"MESH","ARMATURE"},
        bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,
        bake_anim_use_all_bones=True,add_leaf_bones=False,axis_forward="-Z",axis_up="Y")
    render_preview(directory,rig,acts,name)
    report={"character":name,"source":blend,"fbx":fbx,"mesh_parts":initial,
            "render_meshes":len(meshes),"bones":len(rig.data.bones),
            "animations":list(acts),"blender":bpy.app.version_string}
    with open(os.path.join(directory,"model_report.json"),"w",encoding="utf-8") as f:
        json.dump(report,f,indent=2,ensure_ascii=False)
    print("CHARACTER_READY",json.dumps(report,ensure_ascii=False),flush=True)
    return report
report=[]
for name in NAMES:
    try:
        report.append(build(name))
    except Exception as exc:
        print("CHARACTER_ERROR",name,str(exc),traceback.format_exc(),flush=True)
        raise
with open(os.path.join(ROOT,"remaining_characters_report.json"),"w",encoding="utf-8") as f:
    json.dump(report,f,ensure_ascii=False,indent=2)
print("ALL_REMAINING_MODELS_READY",len(report),flush=True)
