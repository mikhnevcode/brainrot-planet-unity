"""Thirteen authored reference skins, using the actual existing Blender cast.
Run Blender --background --python Characters/build_cast_skins_v072.py [-- HeroName]
Writes only Content/Skins/Cast and a new catalog. Original characters stay intact.
Geometry is exported in Unity model coordinates (x,z,-y), grouped by bone/material.
"""
import bpy,math,json,hashlib,sys
from pathlib import Path
from mathutils import Vector,Matrix
from math import sin,cos,pi

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(Path(__file__).resolve().parent))
import cast_skin_variants_v072 as V
OUT=ROOT/'Content'/'Skins'/'Cast'
OUT.mkdir(parents=True,exist_ok=True)
NAMES=['Tralalero','Bombardiro','Frigo','Bombombini','Ballerina','Assassino','Patapim','TungSahur','Lirili','Frulli','Boneca','Trippi','Troppa']
DESIGNS=[
('tralalero_reef_racer','РИФОВЫЙ ГОНЩИК','Reef Racer','Коралловая сбруя · спортивные кроссовки','#FF7052'),
('bombardiro_orbital','ОРБИТАЛЬНЫЙ СПАСАТЕЛЬ','Orbital Rescuer','Солнечные крылья · спасательные реакторы','#43BEEB'),
('frigo_polar_express','ПОЛЯРНЫЙ ЭКСПРЕСС','Polar Express','Полярный холодильник · тёплая экипировка','#62CFE6'),
('bombombini_sky_courier','НЕБЕСНЫЙ КУРЬЕР','Sky Courier','Бирюзовая авиация · курьерские сумки','#39BEAE'),
('ballerina_pearl_prima','ЖЕМЧУЖНАЯ ПРИМА','Pearl Prima','Фиолетовый атлас · жемчужная диадема','#B98AEE'),
('assassino_jade_ronin','НЕФРИТОВЫЙ РОНИН','Jade Ronin','Нефритовые доспехи · золотые узлы','#42BD94'),
('patapim_bloom_guardian','ХРАНИТЕЛЬ ЦВЕТЕНИЯ','Bloom Guardian','Цветущие ветви · лесные амулеты','#93CB62'),
('tungsahur_copper_drummer','МЕДНЫЙ БАРАБАНЩИК','Copper Drummer','Медные обручи · оркестровая сбруя','#E5A155'),
('lirili_oasis_caravan','ОАЗИСНЫЙ КАРАВАН','Oasis Caravan','Узорное седло · бирюзовые сандалии','#53C3B8'),
('frulli_tropical_dj','ТРОПИЧЕСКИЙ ДИДЖЕЙ','Tropical DJ','Большие наушники · музыкальный жилет','#B37AEB'),
('boneca_rally_amphibian','РАЛЛИ-АМФИБИЯ','Rally Amphibian','Гоночная шина · фонари и козырёк','#F1A52F'),
('trippi_pearl_reef','ПЕРЛАМУТРОВЫЙ РИФ','Pearl Reef','Перламутровый панцирь · коралловая корона','#CB93E8'),
('troppa_coral_admiral','КОРАЛЛОВЫЙ АДМИРАЛ','Coral Admiral','Морской мундир · капитанская фуражка','#E6765E')]

COSMETICS=[]
def mat(name,hexcolor,glow=False):
    color=tuple(int(hexcolor[i:i+2],16)/255 for i in (1,3,5))
    m=bpy.data.materials.new('SKIN '+name);m.diffuse_color=(*color,1)
    m['hex']=hexcolor;m['glow']=glow
    m.use_nodes=True;bsdf=m.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Base Color'].default_value=(*color,1)
    bsdf.inputs['Roughness'].default_value=.32
    if glow:bsdf.inputs['Emission Color'].default_value=(*color,1);bsdf.inputs['Emission Strength'].default_value=.65
    return m

def finish(obj,name,material,bone='Body'):
    obj.name='SKIN '+name;obj['skin_anchor']=bone;obj.data.materials.append(material)
    if obj.type=='MESH':
        for face in obj.data.polygons:face.use_smooth=True
    COSMETICS.append(obj)
    return obj

def mesh(name,vertices,faces,material,bone='Body'):
    data=bpy.data.meshes.new(name);data.from_pydata(vertices,[],faces);data.update()
    obj=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(obj)
    return finish(obj,name,material,bone)

def tube(name,points,radius,material,bone='Body',resolution=1):
    data=bpy.data.curves.new(name,'CURVE');data.dimensions='3D';data.resolution_u=5
    data.bevel_depth=radius;data.bevel_resolution=resolution;data.use_fill_caps=True
    spline=data.splines.new('BEZIER');spline.bezier_points.add(len(points)-1)
    for b,p in zip(spline.bezier_points,points):b.co=p;b.handle_left_type='AUTO';b.handle_right_type='AUTO'
    obj=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(obj)
    return finish(obj,name,material,bone)

def ell(name,center,radii,material,bone='Body',segments=16,rings=10):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=center)
    obj=bpy.context.object;obj.scale=radii
    return finish(obj,name,material,bone)

def ring(name,center,radius,thickness,material,bone='Body',rotation=(0,0,0),scale=(1,1,1)):
    bpy.ops.mesh.primitive_torus_add(major_radius=radius,minor_radius=thickness,major_segments=28,minor_segments=6,location=center,rotation=rotation)
    obj=bpy.context.object;obj.scale=scale
    return finish(obj,name,material,bone)

def box(name,center,size,material,bone='Body',bevel=.025):
    bpy.ops.mesh.primitive_cube_add(size=1,location=center)
    obj=bpy.context.object;obj.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    modifier=obj.modifiers.new('Soft manufactured edge','BEVEL');modifier.width=bevel;modifier.segments=3
    return finish(obj,name,material,bone)

def panel(name,points,depth,material,bone='Body',bevel=.018):
    n=len(points);vertices=points+[(x,y,z-depth) for x,y,z in points]
    faces=[tuple(range(n)),tuple(range(2*n-1,n-1,-1))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    obj=mesh(name,vertices,faces,material,bone)
    modifier=obj.modifiers.new('Tailored rounded edge','BEVEL');modifier.width=bevel;modifier.segments=2
    return obj

def band(name,z,rx,ry,height,material,bone='Body',center=(0,0),start=0,end=2*pi):
    # Curved strip with closed inner/outer walls, rather than a rectangular belt.
    vs=[];fs=[];steps=32
    for i in range(steps+1):
        a=start+(end-start)*i/steps
        for dr,dz in [(0,-height/2),(0,height/2),(.018,height/2),(.018,-height/2)]:
            vs.append((center[0]+(rx+dr)*cos(a),center[1]+(ry+dr)*sin(a),z+dz))
    for i in range(steps):
        for j in range(4):fs.append((i*4+j,i*4+(j+1)%4,(i+1)*4+(j+1)%4,(i+1)*4+j))
    fs.extend([(3,2,1,0),(steps*4,steps*4+1,steps*4+2,steps*4+3)])
    return mesh(name,vs,fs,material,bone)

def leaf(name,center,size,material,bone='Body',angle=0):
    vs=[];fs=[];rows=8;across=6
    for i in range(rows):
        t=i/(rows-1);half=size[0]*sin(pi*t)*.5
        for j in range(across):
            q=2*j/(across-1)-1;x=half*q;y=size[1]*(t-.5);z=size[2]*sin(pi*t)*(1-q*q)
            vs.append((center[0]+x*cos(angle)-y*sin(angle),center[1]+x*sin(angle)+y*cos(angle),center[2]+z))
    for i in range(rows-1):
        for j in range(across-1):k=i*across+j;fs.append((k,k+1,k+across+1,k+across))
    obj=mesh(name,vs,fs,material,bone);modifier=obj.modifiers.new('Leaf thickness','SOLIDIFY');modifier.thickness=.012
    return obj

def blossom(name,center,petals,heart,bone='Head',radius=.13):
    for i in range(5):
        a=i*2*pi/5
        obj=ell(name+' petal',(center[0]+radius*.55*cos(a),center[1]+radius*.55*sin(a),center[2]),(radius*.65,radius*.34,.028),petals,bone,12,6)
        obj.rotation_euler.z=a
    ell(name+' pollen',tuple(center),(.035,.035,.034),heart,bone,12,6)

def bow(name,center,color,trim,bone):
    for s in (-1,1):
        points=[(center[0],center[1],center[2]),(center[0]+s*.13,center[1]-.025,center[2]+.075),
                (center[0]+s*.16,center[1]-.04,center[2]-.05),(center[0]+s*.04,center[1]-.035,center[2]-.018)]
        panel(name+' loop',points,.022,color,bone,.008)
    ell(name+' knot',center,(.05,.035,.05),trim,bone,12,8)

def tralalero(m):
    coral,cream,navy=m['accent'],m['light'],m['dark']
    # Three-dimensional rescue-sport harness follows the continuous shark torso.
    for s in (-1,1):
        tube('Coral racing harness',[(s*.38,.45,1.81),(s*.57,.04,1.66),(s*.56,-.45,1.5),(s*.35,-.72,1.78)],.052,coral)
        tube('Pearl sewn piping',[(s*.43,.42,1.82),(s*.615,.02,1.63),(s*.60,-.42,1.49)],.018,cream)
        panel('Shark shoulder saddle',[(s*.18,-.55,1.90),(s*.44,-.34,1.92),(s*.47,-.65,1.84),(s*.23,-.83,1.83)],.035,navy)
    # v07 footwear already contains flush colour inlays in the continuous upper
    # and outsole.  Do not add free-standing bumpers/laces: they looked like
    # detached pieces while the three feet moved in the victory dance.
    # Small asymmetric shell insignia, clear of face and dorsal fin.
    ring('Reef league crest',(.48,-.05,1.71),.095,.018,cream,rotation=(pi/2,0,pi/2))

def bombardiro(m):
    white,dark,cyan,orange=m['light'],m['dark'],m['energy'],m['accent']
    for s in (-1,1):
        x=s*1.48
        for y in (-.42,-.57):ring('Rescue engine collar',(x,y,1.52),.34,.054,white,rotation=(pi/2,0,0))
        ring('Rescue reactor',(x,-.62,1.52),.275,.04,cyan,rotation=(pi/2,0,0))
        panel('Curved solar wing saddle',[(s*.72,-.55,1.82),(s*2.68,-.84,1.82),(s*2.65,-1.13,1.82),(s*.73,-.97,1.82)],.075,white)
        panel('Recessed photovoltaic array',[(s*1.76,-.77,1.855),(s*2.55,-.88,1.855),(s*2.53,-1.06,1.855),(s*1.76,-.96,1.855)],.014,dark)
        for i in range(6):
            xx=s*(1.82+i*.13);tube('Solar cell circuit',[(xx,-.81-i*.016,1.871),(xx,-.965-i*.016,1.871)],.007,cyan)
        tube('Rescue chevron',[(s*.78,-.61,1.89),(s*1.01,-.65,1.89),(s*1.25,-.68,1.89)],.042,orange)
        tube('Insulated power cable',[(s*.30,-.40,2.13),(s*.67,-.56,1.91),(s*1.3,-.60,1.87)],.029,orange)
    tube('Rounded rescue hoop',[(-.4,-.47,2.05),(-.32,-.48,2.36),(.32,-.48,2.36),(.4,-.47,2.05)],.036,white)
    ring('Navigation beacon',(0,-.46,2.36),.085,.03,cyan,rotation=(pi/2,0,0))
    # A tiny tail badge follows Tail; no body-attached decoration across the hinge.
    tube('Rescue tail stripe',[(-.22,-2.25,2.22),(0,-2.27,2.32),(.22,-2.25,2.22)],.027,orange,'Tail')

def frigo(m):
    frost,navy,cream,brass=m['light'],m['dark'],m['accent'],m['energy']
    for z,h in [(2.17,.66),(1.27,1.02)]:
        for s in (-1,1):tube('Enamel door border',[(s*.44,.452,z-h*.42),(s*.44,.452,z+h*.42),(s*.28,.455,z+h*.45)],.014,frost)
    # A recognisable raised snowflake emblem on the lower fridge door.
    for a in (0,pi/3,2*pi/3):
        dx,dz=.16*cos(a),.16*sin(a)
        tube('Snowflake spoke',[(-dx,.458,1.21-dz),(dx,.458,1.21+dz)],.015,frost)
    box('Polar lower kick panel',(0,.456,.83),(.82,.028,.095),navy,bevel=.025)
    band('Cream pilot cap band',2.955,.215,.25,.065,cream,'Head',(0,1.075))
    ell('Soft polar pilot cap',(0,1.055,3.02),(.24,.265,.10),navy,'Head',24,12)
    tube('Polar cap curved seam',[(-.16,1.06,3.055),(0,1.055,3.125),(.16,1.06,3.055)],.01,cream,'Head')
    for s,label in [(-1,'L'),(1,'R')]:
        band('Wool boot cuff',.32,.255,.25,.09,cream,'Leg_'+label,(s*.25,.10))
        for i in range(9):
            a=i*2*pi/9;ell('Knitted cuff stitch',(s*.25+.245*cos(a),.1+.24*sin(a),.325),(.026,.035,.04),cream,'Leg_'+label,8,6)
        box('Polar boot brass buckle',(s*.43,.19,.24),(.055,.13,.07),brass,'Leg_'+label,.012)

def bombombini(m):
    teal,cream,orange,dark=m['main'],m['light'],m['accent'],m['dark']
    for s,label in [(-1,'L'),(1,'R')]:
        tube('Wing courier piping',[(s*.6,-.32,2.00),(s*1.35,-.53,2.01),(s*2.02,-.87,1.94)],.028,orange,'Wing_'+label)
        for i in range(2):
            ring('Engine flight collar',(s*(1.06 if i==0 else 1.76),-.075,2.11),.152,.022,teal,'Wing_'+label,rotation=(pi/2,0,0))
        box('Tailored courier pouch',(s*.43,-.55,1.68),(.24,.34,.26),teal,'Body',.055)
        tube('Pouch stitched piping',[(s*.53,-.70,1.77),(s*.56,-.52,1.79),(s*.53,-.38,1.74)],.009,cream)
        tube('Courier harness',[(s*.25,.02,2.04),(s*.45,-.42,2.11),(s*.36,-.85,2.11)],.041,orange)
    band('Aviator cap leather band',2.92,.21,.23,.075,dark,'Neck',(0,1.28))
    for s in (-1,1):
        ring('Raised aviator goggle',(s*.105,1.50,2.925),.066,.017,orange,'Neck',rotation=(pi/2,0,0),scale=(1,1,.80))
        ell('Raised aviator glass',(s*.105,1.50,2.925),(.052,.019,.044),teal,'Neck',16,8)

def ballerina(m):
    pearl,gold,violet=m['light'],m['accent'],m['main']
    ring('Gilded porcelain rim',(0,0,2.954),.562,.017,gold,'Head')
    tube('Pearl tiara crown',[(-.36,-.13,2.974),(-.25,-.12,3.10),(0,-.09,3.22),(.25,-.12,3.10),(.36,-.13,2.974)],.018,gold,'Head')
    for x,z in [(-.24,3.08),(0,3.19),(.24,3.08)]:ell('Tiara pearl',(x,-.10,z),(.055,.055,.065),pearl,'Head',16,10)
    # Embroidered radial scallops follow the actual animated skirt bone.
    for i in range(12):
        a=i*2*pi/12
        tube('Gold embroidered skirt ray',[(.24*cos(a),.24*sin(a),1.411),(.5*cos(a),.5*sin(a),1.31),(.77*cos(a),.77*sin(a),1.17)],.008,gold,'Skirt')
        ell('Pearl hem jewel',(.78*cos(a),.78*sin(a),1.17),(.025,.025,.027),pearl,'Skirt',10,6)
    band('Gilded corset belt',1.49,.18,.15,.047,gold)
    for s,label in [(-1,'L'),(1,'R')]:bow('Satin pointe bow',(s*.17,.20,.25),violet,pearl,'Shin_'+label)
    ell('Corset pearl brooch',(0,.203,1.81),(.039,.022,.058),pearl,segments=12,rings=8)

def assassino(m):
    jade,gold,black=m['main'],m['accent'],m['dark']
    band('Ronin lower cup armor',.87,.41,.38,.18,jade)
    for s,label in [(-1,'L'),(1,'R')]:
        for i in range(3):
            panel('Layered jade shoulder',[(s*(.40+i*.03),-.03,1.88-i*.085),(s*(.65+i*.03),-.07,1.88-i*.085),
                (s*(.68+i*.03),.13,1.81-i*.085),(s*(.42+i*.03),.18,1.81-i*.085)],.03,jade,'Arm_'+label)
            tube('Shoulder brass edging',[(s*(.41+i*.03),.17,1.82-i*.085),(s*(.64+i*.03),.14,1.82-i*.085)],.009,gold,'Arm_'+label)
        tube('Crossed armor lacing',[(s*.33,.46,1.17),(s*.23,.49,1.38),(s*.38,.45,1.53)],.018,gold,'Head')
    band('Ronin forehead gold trim',2.435,.585,.56,.022,gold,'Head')
    # Original crescent badge avoids dependence on another franchise's insignia.
    tube('Crescent jade crest',[(-.10,.614,2.30),(-.13,.618,2.40),(0,.625,2.44),(.13,.618,2.40),(.10,.614,2.30)],.018,jade,'Head')

def patapim(m):
    moss,pink,gold,wood=m['main'],m['accent'],m['energy'],m['dark']
    for s,label in [(-1,'L'),(1,'R')]:
        tube('Flowering crown branch',[(s*.25,-.14,2.86),(s*.40,-.10,3.09),(s*.58,-.13,3.18)],.026,wood,'Head')
        leaf('Crown leaf',(s*.47,-.08,3.13),(.13,.27,.035),moss,'Head',s*.7)
        blossom('Forest crown blossom',(s*.52,-.10,3.19),pink,gold,'Head',.12)
        tube('Living wrist vine',[(s*.62,.07,1.16),(s*.70,.1,1.28),(s*.62,.14,1.32)],.024,wood,'Arm_'+label)
        leaf('Wrist leaf',(s*.67,.12,1.29),(.13,.21,.03),moss,'Arm_'+label,s*.9)
        tube('Ankle root weave',[(s*.28,-.04,.63),(s*.44,.12,.47),(s*.35,.25,.34)],.021,wood,'Leg_'+label)
    tube('Forest medallion cord',[(-.22,.27,1.89),(0,.37,1.51),(.22,.27,1.89)],.016,wood)
    leaf('Leaf shaped chest amulet',(0,.373,1.50),(.19,.27,.046),gold,'Body')

def tung(m):
    copper,ivory,dark=m['accent'],m['light'],m['dark']
    for z in (.76,1.55,1.84):
        band('Copper barrel hoop',z,.407,.346,.06,copper)
        for a in (pi*.25,pi*.75,pi*1.25,pi*1.75):ell('Hoop rivet',(.425*cos(a),.363*sin(a),z),(.019,.019,.025),ivory,segments=10,rings=6)
    for s in (-1,1):tube('Orchestral chest strap',[(s*.32,.22,1.88),(s*.28,.32,1.53),(s*.13,.36,1.14)],.024,dark)
    band('Drummer crown hoop',2.815,.428,.349,.055,copper,'Head')
    for s,label in [(-1,'L'),(1,'R')]:
        band('Copper wrist guard',1.20,.085,.08,.12,copper,'Arm_'+label,(s*.66,.12))
        tube('Wrist ivory inlay',[(s*.72,.15,1.15),(s*.72,.15,1.25)],.011,ivory,'Arm_'+label)

def lirili(m):
    teal,gold,cream=m['main'],m['accent'],m['light']
    # Tailored saddle curve only over the cactus trunk; the elephant face stays open.
    for s in (-1,1):
        panel('Oasis saddle shoulder',[(s*.13,-.15,2.61),(s*.42,-.18,2.53),(s*.44,-.30,2.30),(s*.18,-.33,2.38)],.028,teal)
        tube('Saddle embroidered edging',[(s*.15,-.17,2.64),(s*.43,-.20,2.56),(s*.47,-.31,2.29)],.014,gold)
    for i in range(3):tube('Desert saddle diamond',[(0,-.414,2.31-i*.12),(.07,-.415,2.25-i*.12),(0,-.414,2.19-i*.12),(-.07,-.415,2.25-i*.12),(0,-.414,2.31-i*.12)],.01,cream)
    for s,label in [(-1,'L'),(1,'R')]:
        band('Oasis textile gaiter',1.31,.19,.18,.19,teal,'Leg_'+label,(s*.25,.03))
        band('Oasis ankle anklet',.47,.175,.175,.07,teal,'Leg_'+label,(s*.25,.03))
        for j in range(2):
            tube('Sandals turquoise strap',[(s*.25-.18,.09+j*.17,.16),(s*.25,.13+j*.17,.22),(s*.25+.18,.09+j*.17,.16)],.035,teal,'Leg_'+label)
            ell('Sandals copper stud',(s*.25+.14,.1+j*.17,.205),(.035,.028,.019),gold,'Leg_'+label,12,8)
    tube('Elephant forehead jewel chain',[(-.30,.44,2.78),(0,.51,2.67),(.30,.44,2.78)],.013,gold,'Head')
    ell('Turquoise forehead stone',(0,.521,2.66),(.05,.032,.075),teal,'Head',16,10)

def frulli(m):
    purple,lime,dark=m['main'],m['accent'],m['dark']
    # Audio headphones frame, rather than hide, the existing enormous sunglasses.
    tube('Padded headphone arch',[(-.48,-.10,2.46),(-.45,-.15,2.77),(0,-.15,2.93),(.45,-.15,2.77),(.48,-.10,2.46)],.052,dark,'Head')
    for s in (-1,1):
        ell('Over ear headphone',(s*.47,-.12,2.49),(.12,.13,.17),purple,'Head',20,12)
        ring('Headphone luminous ring',(s*.555,-.12,2.49),.105,.018,lime,'Head',rotation=(0,pi/2,0))
        tube('DJ vest lapel',[(s*.19,.19,1.91),(s*.30,.27,1.73),(s*.16,.34,1.36)],.043,purple)
        tube('DJ vest bright seam',[(s*.22,.21,1.89),(s*.335,.27,1.71),(s*.19,.34,1.37)],.012,lime)
    tube('Headphone cable',[(-.42,-.10,2.46),(-.37,-.15,1.99),(-.25,.15,1.61)],.012,lime)
    # Sculpted eighth-note pendant, large enough to read in the portrait.
    tube('Musical pendant cord',[(-.12,.3,1.8),(0,.38,1.52),(.12,.3,1.8)],.014,dark)
    tube('Music note stem',[(.05,.397,1.38),(.05,.397,1.56),(.13,.394,1.50)],.017,lime)
    ell('Music note head',(.008,.4,1.37),(.055,.024,.038),lime,segments=12,rings=8)

def boneca(m):
    orange,cream,dark=m['accent'],m['light'],m['dark']
    # Sidewall stripes preserve the open tire center and existing tread.
    for y in (-.251,.251):
        ring('Race tire ivory stripe',(0,y,1.47),.685,.017,cream,rotation=(pi/2,0,0),scale=(1,1,1.10))
        ring('Race tire amber stripe',(0,y,1.47),.725,.020,orange,rotation=(pi/2,0,0),scale=(1,1,1.10))
    tube('Rally visor brim',[(-.42,.17,2.83),(-.21,.3,2.96),(0,.34,2.99),(.21,.3,2.96),(.42,.17,2.83)],.035,orange,'Head')
    for s in (-1,1):
        box('Rally head lamp housing',(s*.21,.20,2.925),(.12,.11,.11),dark,'Head',.025)
        ell('Rally head lamp',(s*.21,.268,2.925),(.043,.024,.043),cream,'Head',16,10)
        box('Rally leg shin badge',(s*.46,.08,.49),(.085,.09,.14),orange,'Leg_'+('L' if s<0 else 'R'),.03)

def trippi(m):
    pearl,violet,coral,gold=m['light'],m['main'],m['accent'],m['energy']
    # Curved shell contour lines respect the curled shrimp body and thin legs.
    for i in range(5):
        y=-.22-i*.16;z=1.89-i*.18;radius=.36-i*.025
        tube('Pearl shell arch',[(-radius,y,z-.06),(-radius*.65,y-.02,z+.13),(0,y-.03,z+.22),(radius*.65,y-.02,z+.13),(radius,y,z-.06)],.018,pearl)
        ell('Pearl shell stud',(0,y-.035,z+.23),(.025,.025,.04),gold,segments=10,rings=6)
    for s in (-1,1):
        tube('Coral cat diadem',[(s*.13,.0,2.72),(s*.27,-.015,2.94),(s*.34,-.035,3.03)],.024,coral,'Head')
        leaf('Cat diadem pearl leaf',(s*.28,-.05,3.03),(.10,.21,.025),pearl,'Head',s*.4)
    tube('Reef diadem headband',[(-.32,.015,2.66),(-.16,0,2.74),(0,-.025,2.77),(.16,0,2.74),(.32,.015,2.66)],.018,violet,'Head')
    # A low shell pendant sits below the feline muzzle, preserving whiskers.
    tube('Reef necklace',[(-.28,.22,1.92),(0,.28,1.81),(.28,.22,1.92)],.015,violet,'Head')
    ell('Reef pendant',(0,.30,1.81),(.065,.025,.075),gold,'Head',16,10)

def troppa(m):
    navy,coral,gold,cream=m['dark'],m['accent'],m['energy'],m['light']
    # Open-front curved coat panels deliberately keep the giant belly visible.
    for s,label in [(-1,'L'),(1,'R')]:
        panel('Admiral curved lapel',[(s*.33,.38,1.95),(s*.55,.27,1.78),(s*.59,.20,1.12),(s*.46,.42,.94),(s*.42,.52,1.44)],.04,navy)
        tube('Lapel coral piping',[(s*.33,.40,1.96),(s*.48,.43,1.69),(s*.49,.48,1.35),(s*.45,.44,1.0)],.025,coral)
        panel('Admiral epaulette',[(s*.39,-.03,1.98),(s*.65,-.05,1.97),(s*.69,.13,1.91),(s*.39,.15,1.91)],.047,coral,'Arm_'+label)
        for j in range(3):tube('Epaulette gold braid',[(s*(.42+j*.075),-.04,1.995),(s*(.42+j*.075),.13,1.94)],.012,gold,'Arm_'+label)
        for z in (1.27,1.50,1.74):ell('Coat brass button',(s*.46,.446,z),(.028,.025,.028),gold,segments=12,rings=8)
    # The cap sits on the fish forehead, forward of the tall dorsal fin.
    ell('Admiral cap crown',(0,.08,2.845),(.37,.32,.095),cream,'Head',28,14)
    band('Cap navy band',2.814,.355,.305,.06,navy,'Head',(0,.08))
    panel('Curved cap visor',[(-.27,.24,2.79),(-.24,.47,2.79),(0,.53,2.80),(.24,.47,2.79),(.27,.24,2.79)],.024,navy,'Head',.02)
    ring('Captain original wheel badge',(0,.393,2.868),.052,.012,gold,'Head',rotation=(pi/2,0,0))

BUILDERS=[tralalero,bombardiro,frigo,bombombini,ballerina,assassino,patapim,tung,lirili,frulli,boneca,trippi,troppa]
PALETTES=[
dict(main='#245C80',dark='#142E49',accent='#FF7052',light='#F7E8C9',energy='#68D9C3'),
dict(main='#DCEAF0',dark='#17304A',accent='#FF963A',light='#EEF7F5',energy='#43BEEB'),
dict(main='#286887',dark='#153C59',accent='#F6DEB0',light='#B5F2F1',energy='#DDA856'),
dict(main='#2AB6A2',dark='#173C42',accent='#F88C42',light='#E6F0D9',energy='#97E6CE'),
dict(main='#7E57B3',dark='#39245E',accent='#D6AD61',light='#FFF0D2',energy='#D8B7F4'),
dict(main='#2F9270',dark='#172F2C',accent='#DBAF65',light='#DCEAD1',energy='#77DDB3'),
dict(main='#619F45',dark='#5D442C',accent='#E5A0C3',light='#D5E8AF',energy='#EDCD68'),
dict(main='#935536',dark='#294A50',accent='#D79250',light='#F1DDB6',energy='#A9DCCC'),
dict(main='#2EAD9C',dark='#1C635B',accent='#DBAE60',light='#F1D7AF',energy='#8ADBCD'),
dict(main='#7540B3',dark='#232436',accent='#BDE94C',light='#F7D791',energy='#D191EE'),
dict(main='#3B984E',dark='#252D34',accent='#F6A832',light='#FFF2CB',energy='#73DBAE'),
dict(main='#AA76CD',dark='#5D4672',accent='#F48B76',light='#F3E2C8',energy='#DBB461'),
dict(main='#328D83',dark='#243D60',accent='#E97861',light='#F0E7CC',energy='#E2B76B')]

def bindings(hero,p):
    # Explicit source material slots; texture/normal details remain on the base.
    mapping=[
    {'Ref63 hide #18475E':p['main'],'Ref63 hide #092133':p['dark'],'Ref63 cloth #056EAB':p['accent'],'Ref63 cloth #ABD1D9':p['light']},
    {'Brushed silver aircraft body':p['main'],'Metal dark panels':p['dark'],'Cockpit blue grey glass':p['energy'],'Silver edge trim':p['light'],'Paint tank turquoise':p['accent'],'Propeller yellow safety tips':p['accent'],'Crocodile v06 sculpted textured skin':'#BBEBDB'},
    {'Ref63 solid #DBE0DB':p['main'],'Ref63 solid #8C9699':p['light'],'Ref63 leather #7D3E11':p['dark'],'Ref63 cloth #915421':p['accent']},
    {'Ref63 solid #212421':p['dark'],'Ref63 solid #525957':p['main'],'Ref63 leather #261B0F':p['accent']},
    {'Ref63 cloth #D46978':p['main'],'Ref63 cloth #E8919C':'#B89CDA','Ref63 solid #EBC99C':'#E8E1EF'},
    {'Ref63 cloth #450B0A':p['dark'],'Ref63 solid #D6B25C':'#D6C6A0','Ref63 solid #9C5914':p['accent'],'Ref63 cloth #ABD1D9':p['main']},
    {'Ref63 leaf #1C4511':p['main'],'Ref63 wood #4F381C':p['dark']},
    {'Ref63 wood #AB5C1C':p['main'],'Ref63 wood #753810':'#6A3C27'},
    {'Ref63 hide #264011':p['main'],'Ref63 leather #572E10':p['main'],'Ref63 solid #BAA66E':p['accent']},
    {'Ref63 feather #100D16':p['main'],'Ref63 solid #AD7529':p['accent'],'Ref63 fur #B09657':'#C4BA96'},
    {'Ref63 hide #2E7836':p['main'],'Ref63 rubber #090B0A':p['dark']},
    {'Ref63 hide #E8520B':p['main'],'Ref63 hide #BD5C3D':p['accent'],'Ref63 fur #D96B1F':'#DCBA8F'},
    {'Ref63 fish #265933':p['main'],'Ref63 fish #7D5C1A':p['accent'],'Ref63 fur #3D1B09':'#564338'}][hero]
    return [dict(material=name,color=color,metallic=.05 if 'solid' in name or 'silver' in name.lower() else 0,smoothness=.18,glow=False) for name,color in mapping.items()]

def design_for(hero,variant):
    if variant==0:return DESIGNS[hero],PALETTES[hero]
    suffix,title,title_en,brief,*colors=V.VARIANTS[hero][variant-1]
    identity=DESIGNS[hero][0].split('_')[0]+'_'+suffix
    p=dict(zip(('main','dark','accent','light','energy'),colors))
    return (identity,title,title_en,brief,p['accent']),p

def export(hero,variant=0):
    design,p=design_for(hero,variant)
    if hero==1 and variant==1:
        # Preserve the tested lava skin and immutable old package, rather than
        # producing a second skin with the same identity and altered content.
        legacy=json.loads((ROOT/'Content'/'Skins'/'catalog-v2.json').read_text(encoding='utf-8'))['skins'][1]
        legacy['swatch']='#EF813B';legacy['titleEn']='Lava Interceptor'
        (OUT/'bombardiro_lava.entry.json').write_text(json.dumps(legacy,ensure_ascii=False,indent=2),encoding='utf-8',newline='\n')
        print('CAST_SKIN_REUSE Bombardiro bombardiro_lava')
        return
    name=NAMES[hero];version='v07' if hero==0 else 'v06' if hero==1 else 'v01' if hero==12 else 'v04'
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Characters'/name/(name+'_'+version+'.blend')))
    COSMETICS.clear()
    for arm in [o for o in bpy.context.scene.objects if o.type=='ARMATURE']:
        if arm.animation_data:arm.animation_data.action=None
        for bone in arm.pose.bones:bone.matrix_basis=Matrix.Identity(4)
    bpy.context.scene.frame_set(0);bpy.context.view_layer.update()
    materials={k:mat(k,v,k=='energy') for k,v in p.items()}
    BUILDERS[hero](materials)
    if variant:V.add_details(hero,variant,materials,sys.modules[__name__])
    entry_id,title,title_en,description,swatch=design
    # Show the actual combined skin in the editable Blender file as well.
    patches=bindings(hero,p)
    for binding in patches:
        material=bpy.data.materials.get(binding['material'])
        if material:
            c=tuple(int(binding['color'][i:i+2],16)/255 for i in (1,3,5));material.diffuse_color=(*c,1)
            if material.use_nodes:
                node=material.node_tree.nodes.get('Principled BSDF')
                if node:node.inputs['Base Color'].default_value=(*c,1)
    groups={};deps=bpy.context.evaluated_depsgraph_get()
    for obj in COSMETICS:
        evaluated=obj.evaluated_get(deps);data=evaluated.to_mesh();data.calc_loop_triangles()
        material=obj.data.materials[0];anchor=obj['skin_anchor'];key=(anchor,material.name)
        part=groups.setdefault(key,dict(name=anchor+' '+material.name[5:],anchor=anchor,color=material['hex'],glow=bool(material['glow']),vertices=[],normals=[],triangles=[]))
        offset=len(part['vertices'])//3;normal_matrix=obj.matrix_world.to_3x3().inverted().transposed()
        for v in data.vertices:
            q=obj.matrix_world@v.co;n=(normal_matrix@v.normal).normalized()
            part['vertices'].extend([round(q.x,5),round(q.z,5),round(-q.y,5)])
            part['normals'].extend([round(n.x,5),round(n.z,5),round(-n.y,5)])
        for triangle in data.loop_triangles:part['triangles'].extend(offset+i for i in triangle.vertices)
        evaluated.to_mesh_clear()
    pack=dict(schema=2,hero=hero,id=entry_id,bindings=patches,meshes=list(groups.values()))
    vertices=sum(len(g['vertices'])//3 for g in groups.values());triangles=sum(len(g['triangles'])//3 for g in groups.values())
    if vertices>12000 or triangles>24000 or len(groups)>24:raise RuntimeError('Skin budget exceeded: '+entry_id+' '+str((vertices,triangles,len(groups))))
    data=json.dumps(pack,separators=(',',':')).encode();digest=hashlib.sha256(data).hexdigest();filename=entry_id+'-'+digest+'.json'
    if len(data)>2097152:raise RuntimeError('Pack too large: '+entry_id)
    (OUT/filename).write_bytes(data)
    # Full editable base + overlays, with original skeleton present for the next artist.
    bpy.context.preferences.filepaths.save_version=0
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(entry_id+'.blend')))
    entry=dict(id=entry_id,hero=hero,title=title,titleEn=title_en,description=description,file=filename,sha256=digest,bytes=len(data),swatch=swatch)
    (OUT/(entry_id+'.entry.json')).write_text(json.dumps(entry,ensure_ascii=False,indent=2),encoding='utf-8',newline='\n')
    print('CAST_SKIN_EXPORT',name,entry_id,'vertices',vertices,'triangles',triangles,'parts',len(groups),'bytes',len(data))

chosen=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
for hero,name in enumerate(NAMES):
    if not chosen or name in chosen:
        for variant in range(5):export(hero,variant)
entries=[]
for hero in range(len(NAMES)):
    for variant in range(5):
        design,_=design_for(hero,variant);path=OUT/(design[0]+'.entry.json')
        if path.exists():entries.append(json.loads(path.read_text(encoding='utf-8')))
manifest=dict(schema=2,revision=2,skins=entries)
(ROOT/'Content'/'Skins'/'catalog-cast-v1.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8',newline='\n')
print('CAST_CATALOG_EXPORT skins',len(entries),'heroes',len(NAMES))
