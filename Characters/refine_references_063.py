"""Rebuild the supplied reference cast, preserving all previous source files.
blender --background --python Characters/refine_references_063.py -- HeroName
No argument builds the complete reference cast; previews use the mobile mesh.
"""
import sys,os,json,math,bpy
from math import sin,cos,pi
from mathutils import Vector
sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
import reference_detail_063 as D
from reference_detail_063 import palette as P,loft,ell,tube,box,torus,fan,eyes,hand,barefoot,shoe

def limbs(skin,feet,arms=True,boot=False):
    for s,label in [(-1,'L'),(1,'R')]:
        if arms:
            loft('Organic arm '+label,[(s*.40,0,1.80,.10,.11),(s*.58,.03,1.50,.085,.095),
                (s*.66,.10,1.27,.070,.075)],skin,'Arm_'+label,rows=30)
            hand('Hand '+label,(s*.67,.10,1.20),skin,'Arm_'+label)
        loft('Anatomical leg '+label,[(s*.23,0,.98,.11,.13),(s*.25,.015,.67,.105,.11),
            (s*.25,.07,.44,.078,.085),(s*.25,.10,.24,.065,.075)],skin,'Leg_'+label,rows=32)
        shoe('Footwear '+label,s*.25,.10,feet,'Leg_'+label,boot=boot)

def frigo():
    cabinet=P('solid',(.86,.88,.86));trim=P('solid',(.55,.59,.60));gap=P('solid',(.14,.16,.16))
    skin=P('fur',(.68,.40,.17));boot=P('leather',(.49,.245,.065));dark=P('solid',(.06,.035,.016))
    box('Enamel refrigerator cabinet',(0,-.03,1.64),(1.02,.77,1.79),cabinet,bevel=.047)
    for z,h in [(2.17,.66),(1.27,1.02)]:
        box('Door shadow gasket',(0,.363,z),(.96,.037,h+.025),gap,bevel=.022)
        box('Separate enamel door',(0,.401,z),(.95,.071,h),cabinet,bevel=.026)
        tube('Brushed steel door handle',[(.32,.48,z+h*.28),(.32,.51,z+.03),(.32,.48,z-h*.28)],
            [.021,.025,.021],trim)
        for zz in (z-h*.25,z+h*.25):box('Door hinge',(-.48,.375,zz),(.035,.055,.10),trim,bevel=.009)
    for i in range(9):box('Back ventilation slot',(0,-.423,1.10+i*.08),(.65,.012,.026),gap,bevel=.005)
    # S-curved neck exits the front door and rises to a recognisable camel face.
    loft('Continuous camel neck',[(0,.44,1.65,.23,.22),(0,.75,1.65,.205,.21),
        (0,1.02,1.79,.15,.18),(0,1.12,2.22,.12,.17),(0,1.10,2.60,.14,.19)],skin,'Head',rows=64,around=40)
    loft('Camel skull',[(0,1.10,2.38,.07,.15),(0,1.12,2.63,.23,.27),
        (0,1.10,2.83,.25,.27),(0,1.02,2.98,.16,.20)],skin,'Head',rows=38,around=40)
    loft('Sculpted split camel muzzle',[(0,1.28,2.71,.19,.12),(0,1.49,2.68,.25,.15),
        (0,1.65,2.63,.20,.135),(0,1.72,2.63,.045,.075)],P('hide',(.74,.50,.28)),'Head',rows=32)
    for s in (-1,1):
        ell('Camel eye socket',(s*.198,1.27,2.825),(.055,.035,.065),dark,'Head')
        ell('Camel amber eye',(s*.21,1.296,2.829),(.033,.022,.039),P('solid',(.22,.115,.035)),'Head')
        ell('Camel pupil',(s*.214,1.313,2.829),(.018,.010,.026),dark,'Head',16,10)
        fan('Camel ear',[(s*.19,1.0,2.9),(s*.35,.94,3.02),(s*.39,1.0,2.89),(s*.23,1.06,2.83)],.046,skin,'Head',axis='Z')
        tube('Nostril crease',[(s*.15,1.635,2.72),(s*.11,1.69,2.685),(s*.05,1.696,2.681)],.011,dark,'Head')
    tube('Camel cleft lip',[(0,1.73,2.68),(0,1.738,2.62),(.12,1.70,2.59)],.008,dark,'Head')
    tube('Camel mouth',[(-.18,1.64,2.59),(0,1.72,2.57),(.18,1.64,2.59)],.009,dark,'Head')
    limbs(skin,boot,arms=False,boot=True)

def tralalero():
    skin=P('hide',(.095,.28,.37));dorsal=P('hide',(.035,.13,.20));belly=P('hide',(.69,.73,.68))
    blue=P('cloth',(.02,.43,.67));black=P('solid',(.009,.016,.020));ivory=P('solid',(.83,.83,.70))
    obj=loft('Continuous anatomical shark',[(0,-1.78,1.41,.07,.09),(0,-1.44,1.42,.19,.20),
        (0,-.92,1.48,.42,.36),(0,-.36,1.50,.57,.44),(0,.27,1.50,.59,.43),
        (0,.77,1.49,.51,.34),(0,1.22,1.47,.39,.24),(0,1.68,1.445,.25,.15),
        (0,1.96,1.42,.025,.035)],skin,'Body',rows=90,around=48)
    obj.data.materials.append(dorsal);obj.data.materials.append(belly)
    for p in obj.data.polygons:
        z=sum(obj.data.vertices[k].co.z for k in p.vertices)/len(p.vertices)
        p.material_index=1 if z>1.64 else (2 if z<1.26 else 0)
    fan('Curved dorsal sail',[(0,-.65,1.76),(0,-.36,2.42),(0,-.13,2.62),(0,.015,1.86)],.075,dorsal,'Body')
    for s,b in [(-1,'L'),(1,'R')]:
        fan('Shark pectoral fin '+b,[(s*.43,.34,1.43),(s*.85,.15,1.1),(s*1.06,-.40,1.07),(s*.47,-.14,1.34)],.055,skin,'Fin_'+b,axis='Z')
        ell('Shark recessed eye',(s*.465,1.01,1.63),(.048,.081,.063),dorsal,'Body')
        ell('Shark black eye',(s*.502,1.045,1.641),(.017,.043,.042),black,'Body',20,12)
        ell('Shark eye glint',(s*.516,1.06,1.656),(.007,.010,.009),ivory,'Body',12,8)
        for i in range(5):
            y=.73-i*.12
            tube('Anatomical gill slit',[(s*.54,y,1.65),(s*.59,y-.025,1.48),(s*.54,y-.06,1.31)],.008,black,'Body')
        tube('Shark smile crease',[(s*.12,1.88,1.365),(s*.31,1.53,1.30),(s*.42,1.08,1.31)],.009,black,'Body')
        ell('Shark nostril',(s*.13,1.775,1.45),(.021,.042,.010),black,'Body',16,8)
    fan('Upper crescent tail',[(0,-1.55,1.43),(0,-2.04,1.93),(0,-2.16,2.42),(0,-1.78,1.61)],.05,dorsal,'Tail')
    fan('Lower crescent tail',[(0,-1.62,1.41),(0,-2.05,.92),(0,-1.92,1.42)],.045,skin,'Tail')
    for x,y,label in [(-.45,.40,'L'),(.45,.40,'R'),(0,-1.10,'Rear')]:
        loft('Shark thigh',[(x,y,1.25,.07,.08),(x,y,.98,.10,.12),(x,y,.66,.105,.10),(x,y,.48,.07,.08)],skin,'Thigh_'+label,rows=30)
        loft('Shark shin',[(x,y,.48,.077,.08),(x,y+.03,.34,.063,.07),(x,y+.04,.22,.054,.065)],skin,'Shin_'+label,rows=18)
        shoe('Blue running sneaker '+label,x,y+.04,blue,'Foot_'+label,width=.23,length=.39)
        for s in (-1,1):tube('White sport side accent',[(x+s*.21,y+.24,.25),(x+s*.22,y+.01,.19),
            (x+s*.20,y-.19,.29)],.019,ivory,'Foot_'+label)

def tung():
    wood=P('wood',(.67,.36,.11));dark=P('solid',(.075,.034,.012));lip=P('wood',(.46,.22,.063))
    def sculpt(x,z):
        nose=.15*math.exp(-(x/.085)**2-((z-2.36)/.18)**2)
        cheeks=.065*(math.exp(-((x-.27)/.12)**2-((z-2.15)/.13)**2)+math.exp(-((x+.27)/.12)**2-((z-2.15)/.13)**2))
        return nose+cheeks
    loft('Wood trunk lower',[(0,0,.66,.34,.32),(0,0,.72,.40,.34),(0,0,1.31,.40,.34),
        (0,0,1.95,.40,.34)],wood,'Body',rows=42,around=48)
    loft('Carved wooden face',[(0,0,1.91,.395,.335),(0,0,2.18,.42,.36),(0,0,2.51,.42,.34),
        (0,0,2.78,.42,.34),(0,0,2.86,.35,.29)],wood,'Head',rows=48,around=48,face=sculpt)
    eyes(.205,.343,2.48,1.03,P('solid',(.35,.14,.031)),bone='Head')
    for s in (-1,1):
        tube('Expressive carved eyebrow',[(s*.08,.37,2.70),(s*.23,.41,2.77),(s*.34,.33,2.71)],
            [.022,.052,.020],dark,'Head')
        ell('Carved ear',(s*.43,.03,2.43),(.10,.065,.16),wood,'Head')
        tube('Nostril',[(s*.042,.462,2.325),(s*.067,.464,2.32)],.009,dark,'Head')
    tube('Wide wooden smile',[(-.23,.354,2.185),(-.12,.408,2.115),(0,.431,2.105),(.15,.398,2.17),(.23,.35,2.23)],.014,dark,'Head')
    tube('Lower smiling lip',[(-.15,.385,2.09),(0,.438,2.075),(.15,.39,2.13)],.013,lip,'Head')
    for s,label in [(-1,'L'),(1,'R')]:
        tube('Anatomical wooden arm',[(s*.40,0,1.80),(s*.59,.05,1.47),(s*.63,.12,1.22)],
            [.092,.070,.055],wood,'Arm_'+label)
        hand('Articulated wooden hand',(s*.63,.12,1.17),wood,'Arm_'+label)
        tube('Wooden leg',[(s*.23,0,.83),(s*.25,.04,.52),(s*.25,.05,.25)],[.10,.075,.058],wood,'Leg_'+label)
        barefoot('Bare wooden foot',s*.25,.08,wood,'Leg_'+label,width=.18,length=.32)
    tube('Tapered wooden bat',[(.73,.15,1.08),(.82,.21,.86),(1.02,.25,.47),(1.22,.28,.14)],
        [.045,.061,.091,.12],wood,'Arm_R')
    ell('Bat end grain',(1.22,.28,.14),(.11,.11,.065),lip,'Arm_R')

def ballerina():
    cream=P('solid',(.92,.79,.61));skin=P('hide',(.91,.64,.49));pink=P('cloth',(.83,.41,.47))
    pinklight=P('cloth',(.91,.57,.61));dark=P('solid',(.14,.066,.038));iris=P('solid',(.32,.14,.052))
    # Closed, rounded cup wall: the same porcelain surface curves over the rim.
    pts=[(0,0,2.17,.28,.28),(0,0,2.29,.36,.36),(0,0,2.57,.47,.47),
         (0,0,2.84,.565,.565),(0,0,2.92,.58,.58)]
    loft('Porcelain cappuccino cup',pts,cream,'Head',rows=48,around=64)
    torus('Rolled porcelain cup rim',(0,0,2.93),.556,.028,cream,'Head')
    ell('Latte surface',(0,0,2.937),(.535,.535,.012),P('solid',(.62,.29,.075)),'Head',48,12)
    for k in range(6):
        w=.26-.036*k;yy=-.25+k*.07
        tube('Latte art leaf',[(0,yy-.04,2.956),(-w,yy+.03,2.957),(0,yy+.10,2.957),
            (w,yy+.03,2.957),(0,yy-.04,2.956)],.010,P('solid',(.95,.81,.55)),'Head')
    tube('Latte art stem',[(0,-.34,2.965),(0,-.1,2.965),(0,.24,2.965)],.009,cream,'Head')
    tube('Cup porcelain handle',[(.50,-.04,2.78),(.78,-.10,2.85),(.89,-.12,2.63),(.80,-.10,2.42),
        (.45,-.04,2.40)],[.065,.065,.064,.060,.066],cream,'Head')
    eyes(.232,.415,2.635,1.0,iris,bone='Head')
    for s in (-1,1):
        tube('Long upper lashes',[(s*.08,.48,2.73),(s*.21,.48,2.79),(s*.36,.425,2.75)],
            [.008,.017,.008],dark,'Head')
        for k in range(3):tube('Individual eyelash',[(s*(.29+k*.025),.45,2.78-k*.015),
            (s*(.32+k*.03),.46,2.80-k*.015)],.006,dark,'Head')
        tube('Soft eyebrow',[(s*.13,.445,2.839),(s*.24,.466,2.86),(s*.34,.414,2.842)],.010,dark,'Head')
    ell('Small nose',(0,.468,2.50),(.042,.039,.046),skin,'Head')
    tube('Smiling lips',[(-.095,.438,2.39),(0,.478,2.366),(.095,.438,2.39)],.012,P('solid',(.55,.25,.19)),'Head')
    loft('Ballerina neck',[(0,0,2.02,.11,.105),(0,0,2.20,.10,.105)],skin,'Body',rows=18)
    loft('Sculpted fitted bodice',[(0,0,1.42,.16,.13),(0,0,1.65,.205,.17),(0,0,1.86,.28,.205),
        (0,0,1.96,.26,.18)],pink,'Body',rows=38,around=40)
    for s in (-1,1):tube('Corset curved seam',[(s*.12,.143,1.45),(s*.13,.173,1.66),(s*.16,.175,1.91)],.005,pinklight,'Body')
    # Two continuous cloth surfaces with sculpted radial pleats and scalloped hem.
    for layer in range(2):
        vs=[];fs=[];uv=[];rows=18;around=96
        for i in range(rows):
            t=i/(rows-1);r=.18+.63*t;z=1.43-.29*t-layer*.055
            for j in range(around):
                a=j/around*2*pi;radius=r+.035*t*sin(a*16)
                vs.append((radius*cos(a),radius*sin(a),z+.035*t*cos(a*16)));uv.append((j/around,t))
        for i in range(rows-1):
            for j in range(around):
                n=(j+1)%around;fs.append((i*around+j,i*around+n,(i+1)*around+n,(i+1)*around+j))
        o=D.mesh('Pleated fabric tutu',vs,fs,pinklight if layer else pink,'Skirt',uv)
        m=o.modifiers.new('Thin cloth thickness','SOLIDIFY');m.thickness=.006
        bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
    for s,label in [(-1,'L'),(1,'R')]:
        loft('Graceful upper arm',[(s*.31,0,1.98,.095,.095),(s*.48,.02,1.84,.08,.08),
            (s*.65,.06,1.68,.066,.07)],skin,'Arm_'+label,rows=28)
        loft('Graceful forearm',[(s*.65,.06,1.68,.067,.07),(s*.76,.15,1.83,.057,.06),
            (s*.72,.20,2.045,.043,.048)],skin,'Forearm_'+label,rows=28)
        hand('Delicate dancer hand',(s*.72,.20,2.09),skin,'Forearm_'+label,scale=.65)
        loft('Dancer thigh',[(s*.17,0,1.23,.125,.135),(s*.17,0,.97,.108,.115),(s*.17,0,.72,.075,.087)],skin,'Leg_'+label,rows=26)
        loft('Dancer calf',[(s*.17,0,.71,.078,.09),(s*.17,.02,.48,.080,.09),(s*.17,.06,.22,.048,.055)],skin,'Shin_'+label,rows=26)
        loft('Satin pointe shoe',[(s*.17,-.05,.19,.065,.09),(s*.17,.13,.12,.095,.12),
            (s*.17,.29,.08,.063,.055)],pink,'Shin_'+label,rows=22)
        for k in range(3):tube('Crossed ballet ribbon',[(s*.17-.058,.06,.23+k*.07),(s*.17+.06,.04,.29+k*.07)],.010,pinklight,'Shin_'+label)

def patapim():
    moss=P('leaf',(.11,.27,.065));fur=P('fur',(.48,.39,.24));skin=P('elephant',(.59,.37,.20));root=P('wood',(.31,.22,.11))
    loft('Organic forest torso',[(0,0,.70,.37,.29),(0,0,1.18,.48,.32),(0,-.07,1.69,.43,.33),
        (0,-.05,2.00,.32,.28)],moss,'Body',rows=50,around=48,ribs=7)
    loft('Proboscis monkey skull',[(0,.17,1.94,.26,.25),(0,.22,2.16,.38,.33),
        (0,.15,2.46,.39,.32),(0,.06,2.66,.24,.25)],skin,'Head',rows=48,around=40)
    eyes(.19,.46,2.46,.62,P('solid',(.26,.12,.033)),bone='Head')
    for s in (-1,1):
        loft('Long furry cheek',[(s*.31,.26,2.51,.11,.15),(s*.34,.35,2.28,.18,.14),
            (s*.24,.39,2.0,.11,.10)],fur,'Head',rows=36)
        tube('Heavy brow',[(s*.06,.45,2.60),(s*.18,.49,2.62),(s*.29,.43,2.59)],
            [.03,.058,.025],skin,'Head')
    loft('Long pendulous monkey nose',[(0,.51,2.53,.095,.10),(0,.66,2.38,.14,.13),
        (0,.81,2.13,.145,.14),(0,.85,1.98,.10,.10)],skin,'Head',rows=44,around=32)
    tube('Monkey mouth',[(-.12,.57,2.1),(0,.66,2.02),(.12,.57,2.1)],.008,P('solid',(.20,.08,.036)),'Head')
    # One irregular moss crown with leaf relief, not a ring of green spheres.
    loft('Dense continuous moss crown',[(0,-.08,2.44,.30,.28),(0,-.09,2.63,.55,.41),
        (0,-.11,2.84,.59,.40),(0,-.10,2.98,.37,.28),(0,-.10,3.015,.12,.11)],moss,'Head',rows=44,around=64,ribs=11)
    for s,label in [(-1,'L'),(1,'R')]:
        loft('Moss-covered arm',[(s*.32,0,1.84,.10,.10),(s*.40,0,1.81,.14,.13),(s*.57,.03,1.46,.115,.12),
            (s*.67,.10,1.18,.08,.095)],moss,'Arm_'+label,rows=36,ribs=7)
        hand('Root fingers',(s*.67,.10,1.16),skin,'Arm_'+label,scale=1.05)
        loft('Root-covered calf',[(s*.23,0,.99,.21,.20),(s*.30,.02,.59,.17,.18),
            (s*.31,.04,.24,.22,.23)],moss,'Leg_'+label,rows=32,ribs=7)
        barefoot('Oversized anatomical forest foot',s*.31,.15,skin,'Leg_'+label,width=.31,length=.61)
        for i in range(14):
            a=i/14*2*pi
            tube('Twisted exposed roots',[(s*.23+.21*cos(a),.21*sin(a),1.05),
                (s*.28+.19*cos(a+.45),.21*sin(a+.45),.62),
                (s*.31+.25*cos(a),.13+.25*sin(a),.28),
                (s*.31+.27*cos(a+.2),.30+.27*sin(a+.2),.20)],
                [.019,.025,.018,.004],root,'Leg_'+label)

def lirili():
    cactus=P('hide',(.15,.25,.066));skin=P('elephant',(.38,.34,.25));ivory=P('solid',(.73,.65,.43))
    strap=P('leather',(.34,.18,.062));rubber=P('rubber',(.16,.14,.079));black=P('solid',(.026,.019,.012))
    loft('Cactus upper trunk',[(0,0,1.71,.43,.31),(0,0,2.15,.48,.34),(0,-.10,2.53,.40,.30)],cactus,'Body',rows=44,around=64,ribs=18)
    for s,label in [(-1,'L'),(1,'R')]:
        loft('Long fluted cactus leg',[(s*.24,0,.30,.22,.23),(s*.25,0,.84,.21,.22),
            (s*.24,0,1.38,.23,.235),(s*.22,0,1.88,.24,.24)],cactus,'Leg_'+label,rows=56,around=48,ribs=12)
        barefoot('Elephant foot',s*.25,.12,skin,'Leg_'+label,width=.22,length=.37)
        loft('Sandal outsole',[(s*.25,-.19,.055,.11,.03),(s*.25,0,.055,.26,.04),
            (s*.25,.37,.055,.26,.04),(s*.25,.54,.055,.13,.03)],rubber,'Leg_'+label,rows=26)
        for yy in (.08,.32):
            tube('Broad leather sandal strap',[(s*.25-.24,yy,.13),(s*.25-.15,yy,.26),
                (s*.25+.15,yy,.26),(s*.25+.24,yy,.13)],.048,strap,'Leg_'+label)
            box('Brass sandal buckle',(s*.25+.08,yy+.004,.315),(.115,.055,.069),ivory,'Leg_'+label,.007)
        for i in range(12):
            a=i*2.4;z=.5+i*.10
            tube('Cactus spine tuft',[(s*.25+.215*cos(a),.225*sin(a),z),
                (s*.25+.24*cos(a),.25*sin(a),z+.035)],.002,ivory,'Leg_'+label)
    loft('Sculpted elephant forehead',[(0,.14,2.05,.29,.28),(0,.21,2.28,.47,.36),
        (0,.15,2.61,.43,.38),(0,.04,2.79,.28,.27),(0,.02,2.86,.015,.015)],skin,'Head',rows=54,around=48)
    for s in (-1,1):
        fan('Large sculpted elephant ear',[(s*.30,.02,2.63),(s*.71,-.02,2.70),(s*.81,.04,2.45),
            (s*.69,.12,2.10),(s*.35,.16,2.13)],.095,skin,'Head',axis='Z')
        ell('Elephant eye socket',(s*.34,.435,2.48),(.066,.030,.051),black,'Head')
        ell('Elephant amber eye',(s*.345,.454,2.485),(.038,.015,.030),P('solid',(.22,.15,.05)),'Head')
        tube('Curved ivory tusk',[(s*.24,.49,2.20),(s*.29,.66,2.035),(s*.40,.76,1.91)],
            [.043,.030,.002],ivory,'Head')
    loft('Long articulated elephant trunk',[(0,.45,2.39,.145,.15),(0,.64,2.12,.125,.135),
        (0,.73,1.78,.095,.10),(0,.76,1.48,.077,.082),(.13,.81,1.39,.068,.07),
        (.22,.83,1.47,.036,.039)],skin,'Head',rows=70,around=32)
    for i in range(10):
        z=1.64+i*.06
        tube('Trunk skin fold',[(-.075,.805,z),(0,.832,z-.006),(.075,.805,z)],.003,black,'Head')

def trippi():
    shell=P('hide',(.91,.32,.045));fur=P('fur',(.85,.42,.12));cream=P('fur',(.90,.73,.46))
    dark=P('solid',(.13,.055,.016));iris=P('solid',(.68,.36,.045))
    # Curled shrimp abdomen, not a humanoid torso. Narrow shell seams follow it.
    curve=[(0,-.95,.50,.11,.13),(0,-1.03,.82,.24,.22),(0,-.99,1.21,.33,.29),
           (0,-.78,1.55,.39,.34),(0,-.43,1.72,.42,.36),(0,-.03,1.77,.40,.36)]
    loft('Continuous curled shrimp shell',curve,shell,'Body',rows=72,around=40)
    for i in range(1,7):
        v=D.interpolate(curve,i/7);x,y,z,a,b=v
        tube('Segmented shell seam',[(x-a*.88,y+.08,z+.08),(x-a*.6,y-.13,z+.18),
            (x,y-.22,z+.24),(x+a*.6,y-.13,z+.18),(x+a*.88,y+.08,z+.08)],.013,cream)
    loft('Sculpted feline head',[(0,.06,1.62,.16,.22),(0,.08,1.86,.43,.37),
        (0,.06,2.13,.49,.40),(0,.02,2.36,.41,.32),(0,-.02,2.49,.27,.22)],fur,'Head',rows=58,around=48)
    for s in (-1,1):
        fan('Pointed cat ear',[(s*.20,.03,2.40),(s*.51,-.035,2.80),(s*.48,.11,2.38)],.06,fur,'Head',axis='Z')
        fan('Pink inner ear',[(s*.29,.14,2.43),(s*.46,.13,2.70),(s*.43,.18,2.42)],.014,P('hide',(.74,.36,.24)),'Head',axis='Z')
        loft('Feline muzzle cushion',[(s*.13,.37,1.91,.10,.06),(s*.15,.47,1.98,.17,.10),
            (s*.16,.54,2.04,.10,.06)],cream,'Head',rows=22)
        for k in range(3):
            tube('Long tapered cat whisker',[(s*.19,.54,1.99),(s*.49,.63,2.00+(k-1)*.08),
                (s*.79,.69,2.02+(k-1)*.15)],[.004,.003,.001],cream,'Head')
        tube('Shrimp antenna',[(s*.25,.18,2.42),(s*.37,.28,2.74),(s*.64,.33,3.02)],
            [.008,.004,.001],shell,'Head')
        ell('Shrimp upper black eye',(s*.31,.285,2.43),(.045,.035,.045),dark,'Head')
        # Numerous articulated swimming legs originate from the abdomen.
        for k in range(6):
            yy=-.73+k*.15;zz=1.21+k*.035
            tube('Shrimp walking leg',[(s*.23,yy,zz),(s*(.43+k*.035),yy+.10,.91),
                (s*(.49+k*.035),yy+.23,.48),(s*(.51+k*.035),yy+.34,.32)],
                [.021,.019,.012,.004],shell,'Leg_'+('L' if s<0 else 'R'))
        fan('Shrimp tail paddle',[(0,-.97,.62),(s*.36,-1.06,.26),(s*.25,-.98,.07),(0,-.93,.23)],
            .028,shell,'Body',axis='Z')
    eyes(.215,.412,2.20,.80,iris)
    fan('Triangular feline nose',[(-.073,.573,2.075),(.073,.573,2.075),(0,.597,2.005)],.022,dark,'Head',axis='Z')
    tube('Cat mouth crease',[(-.11,.559,1.98),(0,.591,1.966),(.11,.559,1.98)],.008,dark,'Head')

def troppa():
    fur=P('fur',(.24,.105,.036));fish=P('fish',(.15,.35,.20));belly=P('hide',(.78,.65,.36))
    fin=P('fish',(.49,.36,.10));dark=P('solid',(.05,.026,.006));gold=P('solid',(.51,.35,.07))
    loft('Heavy bear torso',[(0,0,.56,.22,.22),(0,-.05,.90,.46,.38),(0,-.04,1.40,.64,.49),
        (0,-.10,1.84,.52,.43),(0,-.05,2.21,.25,.24)],fur,'Body',rows=64,around=48)
    loft('Continuous prominent pale belly',[(0,.28,.78,.14,.10),(0,.36,1.00,.39,.18),
        (0,.39,1.43,.53,.29),(0,.33,1.77,.42,.20),(0,.29,1.88,.24,.12)],belly,'Body',rows=54,around=48)
    loft('Large organic fish skull',[(0,.05,1.96,.25,.28),(0,.07,2.14,.49,.38),
        (0,.03,2.42,.53,.41),(0,-.04,2.68,.34,.29),(0,-.08,2.76,.10,.09)],fish,'Head',rows=58,around=48)
    for s,label in [(-1,'L'),(1,'R')]:
        loft('Heavy furry bear arm',[(s*.44,0,1.94,.20,.22),(s*.64,.04,1.57,.18,.19),
            (s*.67,.12,1.16,.135,.16),(s*.64,.16,.98,.12,.13)],fur,'Arm_'+label,rows=40)
        hand('Bear paw',(s*.65,.17,.98),fur,'Arm_'+label,1.2)
        loft('Bear haunch and calf',[(s*.28,0,.91,.21,.24),(s*.31,.02,.55,.17,.19),
            (s*.34,.09,.22,.105,.13)],fur,'Leg_'+label,rows=36)
        barefoot('Bear clawed foot',s*.34,.12,fur,'Leg_'+label,width=.19,length=.32)
        ell('Fish bulging eye gold socket',(s*.36,.346,2.45),(.16,.085,.18),gold,'Head')
        ell('Large turquoise fish iris',(s*.36,.402,2.45),(.115,.043,.13),P('solid',(.03,.61,.47)),'Head')
        ell('Fish pupil',(s*.36,.439,2.45),(.061,.021,.083),dark,'Head')
        ell('Fish eye glint',(s*.34,.458,2.50),(.02,.009,.026),P('solid',(.92,.94,.76)),'Head')
        tube('Fish gill cover',[(s*.41,.27,2.54),(s*.48,.30,2.35),(s*.37,.39,2.15)],.019,gold,'Head')
    loft('Fish protruding upper lip',[(0,.32,2.25,.26,.07),(0,.50,2.22,.27,.07),(0,.60,2.19,.23,.055)],gold,'Head',rows=24)
    loft('Fish mouth dark cavity',[(0,.42,2.12,.22,.035),(0,.56,2.13,.22,.045)],dark,'Head',rows=12)
    tube('Rounded lower fish lip',[(-.23,.57,2.11),(0,.64,2.07),(.23,.57,2.11)],.032,belly,'Head')
    for i in range(7):
        x=(i-3)*.053
        loft('Fish tooth',[(x,.579,2.17,.013,.012),(x,.598,2.13,.002,.002)],belly,'Head',rows=12,around=12)
    fan('Tall dorsal fin',[(0,-.12,2.40),(0,-.48,3.00),(0,-.83,2.96),(0,-.71,2.08),(0,-.20,1.96)],.025,fin,'Body')
    loft('Fish tail peduncle',[(0,-.25,1.72,.30,.29),(0,-.71,1.68,.22,.20),(0,-1.09,1.64,.09,.13)],fish,'Body',rows=32)
    fan('Broad forked fish tail',[(0,-1.02,1.64),(0,-1.63,2.07),(0,-1.54,1.67),
        (0,-1.63,1.26),(0,-1.02,1.53)],.025,fin,'Body')
    for i in range(8):
        z=1.30+i*.10
        tube('Tail fin rib',[(0,-1.07,1.62),(.026,-1.50,z)],.005,gold)
    ell('Belly navel',(0,.683,1.18),(.024,.008,.032),gold)

def assassino():
    cream=P('solid',(.84,.70,.36));red=P('cloth',(.27,.043,.039));steel=P('solid',(.47,.55,.57))
    dark=P('solid',(.018,.012,.018));gold=P('solid',(.61,.35,.08))
    loft('Tapered ninja cup',[(0,0,.74,.32,.30),(0,0,.86,.42,.37),(0,0,2.26,.55,.48),
        (0,0,2.49,.56,.49)],cream,'Body',rows=54,around=48)
    loft('Burgundy ninja lid',[(0,0,2.42,.55,.48),(0,0,2.51,.61,.54),(0,0,2.57,.61,.54),
        (0,0,2.66,.54,.48)],red,'Head',rows=22,around=48)
    torus('Rolled lid gold rim',(0,0,2.51),.565,.024,cream,'Head')
    loft('Ninja forehead wrap',[(0,0,2.21,.56,.50),(0,0,2.41,.57,.51)],red,'Head',rows=16,around=48)
    loft('Ninja face mask',[(0,0,1.12,.45,.41),(0,0,1.40,.49,.445),(0,0,1.69,.52,.465)],red,'Head',rows=20,around=48)
    eyes(.235,.468,1.955,1.12,dark)
    for s in (-1,1):
        tube('Angry ninja eyebrow',[(s*.39,.507,2.23),(s*.22,.548,2.17),(s*.11,.519,2.11)],.032,dark,'Head')
    box('Steel forehead protector',(0,.531,2.315),(.61,.054,.15),steel,'Head',.032)
    for s in (-1,1):ell('Badge rivet',(s*.25,.566,2.315),(.018,.008,.018),dark,'Head')
    # Original spiral crest, not a licensed emblem copied from the photograph.
    pts=[(.057*t*cos(2*pi*t),.563,2.315+.057*t*sin(2*pi*t)) for t in [i/30 for i in range(31)]]
    tube('Engraved spiral crest',pts,.009,dark,'Head')
    for i in (-1,1):
        fan('Flowing tied headband',[(.47,-.08,2.31),(.89,-.24,2.17+i*.05),
            (.82,-.31,1.95+i*.08),(.55,-.10,2.19)],.02,red,'Head',axis='Z')
    limbs(red,red)
    for s,label in [(-1,'L'),(1,'R')]:
        tube('Sword grip',[(s*.64,.13,1.0),(s*.77,.17,1.28)],.038,gold,'Arm_'+label)
        torus('Sword guard',(s*.76,.17,1.29),.08,.019,gold,'Arm_'+label)
        fan('Curved steel katana',[(s*.77,.17,1.31),(s*1.02,.17,1.67),(s*1.24,.17,2.10),
            (s*1.27,.17,2.40),(s*1.11,.17,2.07),(s*.93,.17,1.65),(s*.72,.17,1.34)],
            .012,steel,'Arm_'+label,axis='Z')

def frulli():
    feather=P('feather',(.061,.050,.085));cream=P('fur',(.69,.59,.34));beak=P('hide',(.64,.25,.055))
    gold=P('solid',(.68,.46,.16));lens=P('solid',(.013,.027,.038));foot=P('hide',(.22,.12,.046))
    loft('Bird torso',[(0,0,.62,.12,.17),(0,-.04,1.08,.33,.31),(0,-.08,1.59,.34,.34),
        (0,-.06,1.99,.23,.23)],feather,'Body',rows=52,around=40)
    loft('Cream feather breast',[(0,.19,.83,.08,.04),(0,.26,1.23,.27,.10),
        (0,.23,1.67,.26,.12),(0,.18,1.95,.14,.07)],cream,'Body',rows=40)
    loft('Tufted bird skull',[(0,-.04,1.82,.18,.19),(0,-.08,2.17,.35,.30),
        (0,-.15,2.48,.32,.27),(0,-.19,2.63,.19,.17)],feather,'Head',rows=44)
    loft('Long curved orange beak',[(0,.16,2.29,.21,.16),(0,.48,2.19,.16,.12),
        (0,.75,1.99,.10,.10),(0,.84,1.74,.019,.028)],beak,'Head',rows=44)
    loft('Black beak tip',[(0,.808,1.85,.060,.07),(0,.848,1.70,.004,.009)],lens,'Head',rows=18)
    for s,label in [(-1,'L'),(1,'R')]:
        ell('Opaque dark sunglass lens',(s*.246,.238,2.35),(.221,.055,.254),lens,'Head')
        t=torus('Gold round sunglass frame',(s*.246,.275,2.35),.223,.020,gold,'Head',axis='Y');t.scale.z=1.12
        tube('Sunglass temple',[(s*.44,.19,2.40),(s*.43,-.18,2.42)],.018,gold,'Head')
        for k in range(10):
            x=s*(.31+k*.07);z=1.81-k*.038
            fan('Individual wing flight feather',[(s*.24,-.04,1.87),(x,-.32,z+.10),
                (s*(.54+k*.078),-.37,z-.20),(x,-.11,z-.13)],.025,feather,'Arm_'+label,axis='Z')
        loft('Bird bare shin',[(s*.18,0,.81,.055,.055),(s*.22,.09,.43,.043,.047),
            (s*.26,.13,.19,.036,.041)],foot,'Leg_'+label,rows=26,around=20)
        for k in (-1,0,1):
            tube('Splayed bird toe',[(s*.26,.13,.15),(s*.26+k*.09,.30,.10),
                (s*.26+k*.13,.41,.085)],[.032,.024,.009],foot,'Leg_'+label)
    tube('Gold glasses bridge',[(-.07,.278,2.37),(0,.315,2.40),(.07,.278,2.37)],.018,gold,'Head')
    for k in range(9):
        x=(k-4)*.065
        fan('Swept head crest feather',[(x,-.05,2.53),(x*1.15,-.32,2.87),
            (x*1.22,-.63,2.80),(x,-.28,2.54)],.016,feather,'Head',axis='Z')
    for k in range(5):
        x=(k-2)*.10
        fan('Fan tail feather',[(0,-.20,.87),(x,-.73,.51),(x+.055,-.80,.42),
            (x+.09,-.55,.77)],.021,feather,'Body',axis='Z')

def boneca():
    rubber=P('rubber',(.037,.042,.041));hide=P('hide',(.18,.47,.21));pale=P('hide',(.55,.48,.29))
    skin=P('hide',(.57,.35,.18));dark=P('solid',(.016,.024,.014));iris=P('solid',(.37,.41,.24))
    tire=torus('Vertical hollow tire carcass',(0,0,1.47),.59,.22,rubber,axis='Y');tire.scale.z=1.16
    for y in (-.20,.20):torus('Sidewall bead',(0,y,1.47),.57,.025,rubber,axis='Y').scale.z=1.16
    # Physical diagonal grooves supplement the normal map without hiding the hole.
    for i in range(40):
        a=i*2*pi/40
        for s in (-1,1):
            pts=[]
            for k in range(5):
                y=s*(k/4)*.19;angle=a+s*(k/4)*.055
                radius=.59+math.sqrt(.22**2-y**2)+.004
                pts.append((radius*sin(angle),y,1.47+radius*cos(angle)*1.16))
            tube('Chevron raised tire tread',pts,.016,rubber)
    loft('Broad sculpted frog jaw',[(0,.0,2.13,.28,.23),(0,.08,2.29,.60,.34),
        (0,.06,2.49,.62,.37),(0,-.04,2.65,.41,.28)],hide,'Head',rows=50,around=48)
    loft('Pale continuous frog throat',[(0,.18,2.14,.26,.15),(0,.25,2.25,.51,.23),
        (0,.26,2.35,.55,.20)],pale,'Head',rows=28,around=40)
    for s in (-1,1):
        loft('Integrated raised frog eye brow',[(s*.38,.08,2.41,.16,.20),(s*.39,.02,2.68,.20,.20),
            (s*.39,-.04,2.80,.16,.15),(s*.39,-.06,2.89,.015,.015)],hide,'Head',rows=32)
        ell('Frog mottled eye',(s*.39,.184,2.69),(.143,.073,.145),iris,'Head')
        ell('Horizontal frog pupil',(s*.39,.249,2.69),(.102,.020,.042),dark,'Head')
        ell('Frog eye highlight',(s*.36,.267,2.72),(.023,.007,.020),P('solid',(.87,.90,.65)),'Head')
        ell('Frog nostril',(s*.17,.381,2.51),(.025,.010,.017),dark,'Head')
    tube('Wide frog mouth crease',[(-.53,.30,2.39),(-.28,.414,2.405),(0,.442,2.41),
        (.28,.414,2.405),(.53,.30,2.39)],.012,dark,'Head')
    for s,label in [(-1,'L'),(1,'R')]:
        loft('Human anatomical frog-tire leg',[(s*.39,0,.85,.11,.13),(s*.39,.04,.62,.105,.115),
            (s*.43,.08,.38,.07,.08),(s*.44,.12,.17,.048,.062)],skin,'Leg_'+label,rows=36)
        barefoot('Human frog-tire foot',s*.44,.18,skin,'Leg_'+label,width=.15,length=.36)

def bombombini():
    # Existing Engine bones animate a single old propeller centre. New paired
    # nacelles must be rigidly mounted to Wings, with their own rotor pivots.
    bpy.context.view_layer.objects.active=D.RIG;D.RIG.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    rotor_names=[]
    for s,label in [(-1,'L'),(1,'R')]:
        for k in range(2):
            x=s*(1.08+k*.69);name='Rotor_'+label+str(k)
            bone=D.RIG.data.edit_bones.new(name);bone.head=(x,.014,2.11);bone.tail=(x,.20,2.11)
            bone.parent=D.RIG.data.edit_bones['Wing_'+label];rotor_names.append(name)
    bpy.ops.object.mode_set(mode='OBJECT')
    for action in list(bpy.data.actions):
        begin,end=action.frame_range
        D.RIG.animation_data.action=action
        for name in rotor_names:
            bone=D.RIG.pose.bones[name];bone.rotation_mode='XYZ'
            for frame in (begin,end):
                bone.rotation_euler=(0,(frame-begin)/24*2*pi*6,0)
                bone.keyframe_insert(data_path='rotation_euler',frame=frame,group=name)
    D.RIG.animation_data.action=bpy.data.actions['Idle'];bpy.context.scene.frame_set(1)
    white=P('feather',(.79,.78,.70));armor=P('solid',(.13,.14,.13));metal=P('solid',(.32,.35,.34))
    black=P('solid',(.012,.017,.018));orange=P('hide',(.62,.27,.055));strap=P('leather',(.15,.105,.06))
    loft('Streamlined goose torso',[(0,-1.40,1.47,.08,.12),(0,-1.04,1.45,.39,.36),
        (0,-.45,1.58,.53,.44),(0,.10,1.79,.41,.38),(0,.43,1.97,.22,.23)],white,'Body',rows=68,around=48)
    vest=[(0,-1.25,1.47,.25,.26),(0,-1.03,1.455,.405,.38),
        (0,-.45,1.58,.548,.46),(0,.06,1.77,.445,.405),(0,.18,1.83,.35,.34)]
    loft('Continuous fitted armored goose vest',vest,armor,'Body',rows=40,around=40)
    for t in (.17,.38,.60,.82):
        x,y,z,a,b=D.interpolate(vest,t)
        tube('Conforming armor cross panel seam',[(a*cos(j*2*pi/32),y,z-b*sin(j*2*pi/32))
            for j in range(33)],.008,metal,'Body')
    for angle in (.30,1.10,2.05,2.85,3.60,4.35,5.25,5.95):
        points=[]
        for i in range(12):
            x,y,z,a,b=D.interpolate(vest,.08+i/11*.86)
            points.append((a*cos(angle)*1.008,y,z-b*sin(angle)*1.008))
        tube('Longitudinal armor panel joint',points,.005,metal,'Body')
    loft('Goose long curved neck',[(0,.29,1.85,.24,.24),(0,.49,2.04,.20,.22),
        (0,.70,2.32,.16,.18),(0,.86,2.59,.17,.17),(0,1.05,2.73,.22,.19)],white,'Neck',rows=52)
    loft('Goose sculpted head',[(0,.94,2.67,.15,.15),(0,1.14,2.74,.23,.20),
        (0,1.39,2.72,.19,.16),(0,1.45,2.68,.10,.10)],white,'Neck',rows=32)
    for s in (-1,1):
        ell('Goose black eye',(s*.222,1.18,2.80),(.023,.038,.036),black,'Neck')
        ell('Goose eye catchlight',(s*.242,1.194,2.81),(.006,.009,.010),white,'Neck',12,8)
    loft('Tapered orange goose bill',[(0,1.42,2.69,.115,.055),(0,1.64,2.65,.12,.07),
        (0,1.84,2.60,.029,.022)],orange,'Beak',rows=26,around=24)
    tube('Bill separation crease',[(-.10,1.54,2.64),(0,1.82,2.593),(.10,1.54,2.64)],.006,strap,'Beak')
    # Harness plates conform to the body and do not replace the feather volume.
    for y,z,r in [(-.89,1.53,.39),(-.49,1.65,.52),(-.08,1.81,.44)]:
        tube('Leather harness cross strap',[(-r*.94,y,z),(-r*.65,y,z+.30),(0,y,z+.41),
            (r*.65,y,z+.30),(r*.94,y,z)],.039,strap)
    for s,label in [(-1,'L'),(1,'R')]:
        fan('Organic goose wing',[(s*.45,-.55,1.93),(s*1.40,-.45,2.23),(s*2.16,-.83,2.27),
            (s*2.18,-1.16,1.92),(s*1.10,-1.13,1.75),(s*.52,-.94,1.78)],.075,white,'Wing_'+label,axis='Z')
        for k in range(13):
            x=s*(.70+k*.115)
            fan('Long layered goose flight feather',[(x,-.71,2.05),(x+s*.10,-1.26,1.84),
                (x+s*.04,-1.47,1.77),(x-s*.09,-1.12,1.87)],.024,white,'Wing_'+label,axis='Z')
        for k in range(2):
            x=s*(1.08+k*.69);bone='Wing_'+label;rotor='Rotor_'+label+str(k)
            loft('Jet engine armored nacelle',[(x,-.95,1.98,.21,.20),(x,-.58,2.05,.23,.22),
                (x,-.15,2.10,.225,.21),(x,.01,2.11,.19,.18)],metal,bone,rows=32)
            # Dark recessed disk and ring, opaque double-sided cavity geometry.
            torus('Jet intake polished lip',(x,.013,2.11),.18,.025,armor,bone,axis='Y')
            ell('Jet intake dark recessed face',(x,-.006,2.11),(.163,.016,.157),black,bone)
            ell('Turbine hub',(x,.014,2.11),(.047,.030,.047),metal,rotor)
            for j in range(7):
                a=j*2*pi/7
                tube('Turbine radial vane',[(x+.047*cos(a),.014,2.11+.047*sin(a)),
                    (x+.141*cos(a+.29),.009,2.11+.141*sin(a+.29))],.010,metal,rotor)
            box('Engine mount',(x,-.48,1.95),(.12,.40,.20),armor,'Wing_'+label,.025)
        tube('Goose trailing leg',[(s*.23,-.77,1.24),(s*.26,-1.05,.99),(s*.31,-1.19,.83)],
            [.065,.054,.038],orange,'Foot_'+label)
        fan('Webbed orange goose foot',[(s*.31,-1.13,.82),(s*.50,-1.31,.76),(s*.44,-1.53,.75),
            (s*.20,-1.53,.75),(s*.14,-1.30,.76)],.025,orange,'Foot_'+label,axis='Z')
    for k in range(6):
        x=(k-2.5)*.12
        fan('Goose tail feather',[(x*.4,-1.12,1.52),(x,-1.84,1.86),(x+.07,-1.89,1.82),
            (x+.08,-1.26,1.46)],.025,white,'Tail',axis='Z')

BUILDERS={'Frigo':frigo,'Tralalero':tralalero,'TungSahur':tung,'Ballerina':ballerina,
    'Patapim':patapim,'Lirili':lirili,'Trippi':trippi,'Troppa':troppa,
    'Assassino':assassino,'Frulli':frulli,'Boneca':boneca,'Bombombini':bombombini}

if __name__=='__main__':
    requested=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
    names=requested or list(BUILDERS)
    for name in names:
        previous='v04' if name=='Tralalero' else 'v03'
        source_name='Trippi' if name=='Troppa' else name
        source=os.path.join(D.ROOT,source_name,source_name+'_'+previous+'.blend')
        rig=D.reset(source)
        if name=='Troppa':rig.name='Troppa_Armature';rig.data.name='Troppa_Armature'
        BUILDERS[name]()
        version='v01' if name=='Troppa' else ('v05' if name=='Tralalero' else 'v04')
        D.finish(name,version,source)
