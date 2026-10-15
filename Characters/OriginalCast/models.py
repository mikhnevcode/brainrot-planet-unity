"""Twenty-six independent meshes matching the approved original concepts."""
from math import sin, cos, pi
import random
import bpy
from mathutils import Vector
from mathutils.geometry import tessellate_polygon
import geometry as G

def p(role):return G.PALETTE[role]
def m(role,color,kind='skin'):return G.material(role,color,kind)

def rollo():
    G.loft('Seal sculpt',[(0,-.12,.23,.38,.36),(0,-.1,.65,.82,.63),(0,-.1,1.45,.85,.65),(0,.02,2.1,.58,.48),(0,.09,2.55,.12,.15)],rows=44)
    G.ell('Rounded seal cranium',(0,.01,2.21),(.59,.46,.48),bone='Head')
    G.ell('Cream seal belly',(0,.46,1.15),(.59,.15,.83),p('cream'))
    G.ell('Broad seal muzzle',(0,.53,2.0),(.43,.21,.27),p('cream'),'Head')
    G.face(.245,.46,2.27,smile=False)
    G.ell('Seal nose',(0,.75,2.12),(.11,.055,.075),p('dark'),'Head')
    G.tube('Seal smile',[(-.18,.72,1.99),(0,.76,1.95),(.18,.72,1.99)],.017,p('dark'),'Head')
    for s,label in [(-1,'L'),(1,'R')]:
        G.loft('Sweeping seal flipper',[(s*.55,0,1.45,.17,.18),(s*.92,.13,.94,.14,.20),(s*1.05,.24,.79,.035,.07)],bone='Arm_'+label)
        for j in range(3):G.tube('Whisker',[(s*.27,.68,2.0-j*.06),(s*.50,.77,2.1-j*.075),(s*.75,.76,2.1-j*.08)],.005,p('cream'),'Head')
    G.BONES['Tail']=(0,.12,.33)
    G.loft('Foam paint roller',[(-.68,.12,.33,.3,.3),(.68,.12,.33,.3,.3)],p('accent'),'Tail',rows=14,around=32)
    for s in (-1,1):
        G.ring('Roller cheek bracket',(s*.70,.12,.33),.30,.042,p('cream'),axis='X')
        G.tube('Roller fork',[(s*.70,.1,.35),(s*.72,0,.69)],.045,p('metal'))
    G.fan('Seal rear paddle',[(-.16,-.54,.65),(-.46,-.95,.8),(0,-.82,.61),(.45,-.95,.8),(.16,-.54,.65)],bone='Appendage_4')

def pelmolyot():
    dough=m('main','#F2CF98','dough')
    G.ell('Pleated dumpling hull',(0,0,1.45),(.93,.47,.73),dough)
    for i in range(13):
        a=pi*(.08+.84*i/12);x=.94*cos(a);z=1.45+.70*sin(a)
        G.tube('Pinched dough pleat',[(x,.08,z-.10),(x*.95,.12,z+.06),(x*.91,.05,z+.13)],.042,p('cream'))
    G.face(.28,.46,1.6,1.16,bone='Body')
    G.ring('Pot-lid cockpit rim',(0,-.03,2.12),.34,.045,p('metal'))
    G.ell('Glazed pot lid',(0,-.03,2.16),(.34,.29,.24),m('glass','#9FDEE5','glass'))
    G.ell('Pot lid knob',(0,-.03,2.4),(.09,.09,.06),p('gold'))
    for s,label in [(-1,'L'),(1,'R')]:
        G.tube('Fork wing handle',[(s*.74,0,1.64),(s*1.1,0,1.7),(s*1.47,0,1.75)],.065,p('metal'),'Arm_'+label)
        for j in range(4):G.tube('Fork wing tine',[(s*1.43,(j-1.5)*.10,1.75),(s*1.85,(j-1.5)*.12,1.79)],.025,p('metal'),'Arm_'+label)
    for i,col in enumerate(['#E655AB','#36C8DF','#F9A53B']):
        x=(i-1)*.42
        G.ell('Soft paint dumpling',(x,.10,.55),(.19,.18,.23),dough,f'Appendage_{i}')
        G.ell('Paint-dipped pod',(x,.12,.43),(.17,.16,.11),m('pod'+str(i),col),f'Appendage_{i}')

def morzho():
    G.mammal(body_size=(.84,.65,.87),arms=True)
    G.ell('Waffle-cart belly',(0,.50,1.05),(.69,.15,.65),m('waffle','#E7B35F','dough'))
    for s in (-1,1):
        for j in range(8):
            z=.50+j*.15
            G.tube('Waffle diagonal piping',[(s*-.48,.627,z),(0,.672,z+.25),(s*.45,.635,z+.45)],.014,p('cream'))
        G.loft('Ice-pop tusk',[(s*.22,.81,2.00,.075,.055),(s*.23,.87,1.58,.070,.05),(s*.25,.87,1.48,.025,.025)],p('cream'),'Head',rows=24)
        G.ring('Plum cart wheel',(s*.76,0,.45),.28,.095,m('wheel','#665075'),axis='X')
    for i in range(4):G.ell('Strawberry soft-serve swirl',(0,-.10,2.55+i*.12),(.37-i*.07,.32-i*.055,.15),p('accent'),'Head')
    for s in (-1,1):
        for j in range(3):G.tube('Walrus whisker',[(s*.20,.80,2.02-j*.055),(s*.48,.87,2.07-j*.07),(s*.62,.84,2.0-j*.07)],.006,p('cream'),'Head')

def utyugonos():
    G.mammal(muzzle=False,body_size=(.7,.55,.8))
    G.loft('Broad iron bill',[(0,.40,1.98,.38,.12),(0,.86,1.93,.50,.10),(0,1.27,1.89,.15,.06),(0,1.38,1.88,.025,.025)],p('accent'),'Head',rows=36)
    G.loft('Brushed iron sole',[(0,.4,1.87,.38,.028),(0,.85,1.82,.48,.025),(0,1.34,1.81,.02,.02)],p('metal'),'Head',rows=28)
    for x in (-.18,0,.18):G.ell('Steam vent',(x,1.03,1.97),(.025,.035,.008),p('dark'),'Head',12,8)
    G.ell('Platypus paddle tail',(0,-.73,.63),(.45,.65,.13),bone='Tail')
    G.tube('Coiled tail cord',[(0,-.75,.65),(-.6,-.93,.56),(-.92,-.81,.35),(-1.0,-.45,.35),(-.75,-.30,.35),(-.69,-.62,.35)],.055,p('main'),'Tail')
    G.box('Unplugged rounded plug',(-.68,-.63,.35),(.18,.24,.13),p('accent'),'Tail',.045)

def makaronika():
    gold=m('main','#EABD59','dough');G.ell('Pasta mantle',(0,0,2.05),(.58,.48,.68),gold,'Head')
    G.face(.23,.46,2.22)
    for i in range(8):
        a=2*pi*i/8;end=(1.15*cos(a),.55*sin(a),.22+.16*(i%2))
        start=(.30*cos(a),.26*sin(a),1.48);G.BONES[f'Appendage_{i}']=start
        G.tube('Thick macaroni tentacle',[start,(.70*cos(a),.42*sin(a),.95),end,(end[0]*1.04,end[1],end[2]+.19)],[.17,.18,.15,.145],gold,f'Appendage_{i}')
        G.ring('Hollow macaroni tip',(end[0]*1.04,end[1],end[2]+.20),.115,.030,p('accent'),f'Appendage_{i}')
    G.ell('Colander cap',(0,0,2.67),(.59,.49,.25),m('cap','#42B6C0','metal'),'Head')
    for i in range(16):
        a=i*2.39996;rr=.15+.28*((i%4)/3)
        G.ell('Colander punched hole',(rr*cos(a),rr*sin(a),2.84-.15*rr),(.025,.025,.008),p('dark'),'Head',12,8)
    for s in (-1,1):G.ring('Colander loop handle',(s*.64,0,2.68),.12,.025,p('metal'),'Head',axis='Y')

def shurshun():
    paper=m('main','#D4A05B','paper')
    G.box('Inflated gusset paper body',(0,0,1.52),(1.27,.65,1.67),paper,bevel=.16)
    G.face(.25,.36,1.88,bone='Body')
    for s,label in [(-1,'L'),(1,'R')]:
        G.tube('Paper loop handle',[(s*.29,0,2.29),(s*.67,-.02,2.85),(s*.85,.12,2.69),(s*.69,.18,2.36)],.055,paper,'Head')
        G.tube('Rolled paper arm',[(s*.59,.04,1.47),(s*.83,.15,1.31),(s*.86,.24,1.48)],[.10,.08,.12],paper,'Arm_'+label)
        G.fan('Folded paper gusset foot',[(s*.22,-.12,.65),(s*.48,.03,.49),(s*.55,.39,.13),(s*.06,.41,.13),(s*.13,.02,.54)],paper,'Leg_'+label,axis='Y')
        for j in range(5):G.tube('Side concertina fold',[(s*.64,-.28,.9+j*.23),(s*.56,-.04,1.01+j*.23),(s*.64,.28,1.10+j*.23)],.013,p('accent'))

def brokko():
    G.loft('Broccoli branching stalk',[(0,0,.43,.22,.20),(0,0,1.05,.32,.27),(0,.03,1.72,.39,.32),(0,0,2.19,.52,.42)],rows=36)
    G.face(.20,.35,1.86,.90)
    rng=random.Random(26)
    for i in range(55):
        a=i*2.39996;rr=.87*(i/55)**.5;x=rr*cos(a);y=rr*sin(a)
        z=2.28+.48*(1-(rr/.95)**2)
        G.ell('Fine broccoli floret',(x,y,z),(.18,.18,.19),m('floret'+str(i%3),['#3E9C49','#65B746','#277C43'][i%3],'leaf'),'Head',16,10)
    G.limbs(2,False)
    for s,label in [(-1,'L'),(1,'R')]:
        G.tube('Branching boxing arm',[(s*.31,0,1.65),(s*.62,.08,1.42),(s*.88,.27,1.63)],.12,bone='Arm_'+label)
        for j in range(9):
            a=j*2.39996;rr=.24*(j/9)**.5
            G.ell('Cauliflower fist floret',(s*.94+rr*cos(a),.33+rr*sin(a),1.71+.10*sin(a*2)),(.14,.14,.15),p('cream'),'Arm_'+label,16,10)

def budilo():
    G.mammal(body_size=(.88,.64,.90))
    G.ring('Belly alarm brass bezel',(0,.60,1.13),.48,.055,p('gold'),axis='Y')
    G.ell('Cream alarm face',(0,.605,1.13),(.46,.035,.46),p('cream'))
    for j in range(12):
        a=2*pi*j/12;G.ell('Unnumbered clock dot',(.36*sin(a),.66,1.13+.36*cos(a)),(.035,.017,.035),p('main'),segments=12,rings=8)
    G.tube('Alarm hour hand',[(0,.69,1.13),(-.14,.69,1.31)],.033,p('accent'))
    G.tube('Alarm minute hand',[(0,.69,1.13),(.27,.69,1.21)],.026,p('gold'))
    G.ell('Alarm axle',(0,.71,1.13),(.065,.027,.065),p('gold'))
    for s in (-1,1):
        G.ell('Coral alarm bell',(s*.57,0,2.65),(.25,.19,.17),p('accent'),'Head')
        G.tube('Bell brass stem',[(s*.45,0,2.47),(s*.55,0,2.64)],.035,p('gold'),'Head')
    G.ring('Winding key loop',(.88,0,1.1),.13,.034,p('gold'),axis='Y')

def arbuzub():
    green=m('main','#4D9C44','leaf');G.ell('Watermelon boar barrel',(0,-.17,1.20),(.85,.85,.95),green)
    G.ell('Watermelon boar skull',(0,.51,1.76),(.55,.49,.47),green,'Head')
    G.ell('Pink melon snout',(0,.91,1.62),(.29,.18,.18),p('accent'),'Head')
    G.face(.23,.89,1.91,.9)
    for s in (-1,1):
        G.ell('Snout nostril',(s*.10,1.075,1.63),(.035,.014,.054),p('dark'),'Head',16,8)
        G.fan('Boar leaf ear',[(s*.34,.32,2.0),(s*.62,.30,2.45),(s*.67,.45,2.12),(s*.45,.55,2.01)],green,'Head',axis='Y')
        G.loft('Seed tusk',[(s*.30,.88,1.43,.07,.07),(s*.48,1.0,1.58,.08,.06),(s*.50,1.0,1.84,.005,.005)],p('dark'),'Head',rows=26)
    for i in range(11):
        a=2*pi*i/11;pts=[]
        for j in range(17):
            t=pi*(.10+.80*j/16);pts.append((.86*sin(t)*cos(a),-.17+.86*sin(t)*sin(a),1.2+.96*cos(t)))
        G.tube('Curved rind stripe',pts,.023,m('stripe','#286F38','leaf'))
    G.limbs(4,False,feet=p('dark'))
    G.tube('Curled vine tail',[(0,-.85,1.5),(-.30,-1.10,1.8),(-.4,-1.03,2.05),(-.2,-1.0,2.0)],.025,green,'Tail')
    G.leaf('Vine leaf',(-.2,-1,2.0),(-.65,-1.0,2.25),.20,green,'Tail')

def fenik():
    G.loft('Curved hairdryer handle',[(0,-.1,.50,.24,.22),(0,-.12,1.1,.23,.23),(0,0,1.6,.35,.3)])
    G.loft('Hairdryer blower housing',[(0,-.60,1.98,.26,.32),(0,-.35,2.0,.46,.45),(0,.50,2.0,.50,.48),(0,.70,2.0,.47,.44)],rows=34)
    G.ring('Cream nozzle lip',(0,.715,2),.45,.045,p('cream'),'Head',axis='Y')
    G.ell('Air grille shadow',(0,.73,1.83),(.28,.035,.21),p('dark'),'Head')
    G.face(.23,.75,2.17,.75,smile=False)
    for j in range(5):G.tube('Blower grille rib',[(-.22,.77,1.72+j*.06),(.22,.77,1.72+j*.06)],.018,p('metal'),'Head')
    G.limbs(2,False,feet=p('accent'))
    G.ring('Woven scarf collar',(0,0,1.48),.30,.09,p('accent'))
    G.fan('Wind-lifted scarf',[(-.18,-.20,1.52),(-.60,-.17,1.81),(-1.06,.06,2.36),(-.73,.12,2.47),(-.38,-.04,1.85),(.1,-.18,1.48)],p('accent'),'Tail',axis='Y')
    G.tube('Power cord tail',[(0,-.30,.8),(.56,-.45,.7),(.90,-.38,1.0),(1.04,-.27,1.18)],.04,p('main'),'Tail')

def zhele():
    glass=m('main','#86E67B','glass');G.box('Rounded gelatin volume',(0,0,1.45),(1.48,.93,1.73),glass,bevel=.25)
    G.face(.28,.49,1.89,1.08,bone='Body')
    for i in range(17):
        x=.57*sin(i*2.4);z=.85+(i%6)*.19
        G.ell('Tiny gelatin bubble',(x,.485,z),(.028,.010,.028),p('cream'),segments=12,rings=8)
    for i,(x,y) in enumerate([(-.46,-.25),(.46,-.25),(-.46,.25),(.46,.25)]):
        G.tube('Continuous jelly foot',[(x,y,.68),(x*1.18,y,.34),(x*1.20,y+.08,.20)],[.15,.11,.18],glass,f'Appendage_{i}')

def chernilo():
    glass=m('main','#8847B3','glass');G.ell('Thick violet inkwell',(0,0,1.73),(.63,.54,.65),glass)
    G.loft('Inkwell neck',[(0,0,2.05,.29,.29),(0,0,2.37,.30,.30)],glass,rows=16)
    G.ring('Inkwell rolled rim',(0,0,2.38),.34,.065,glass)
    G.ell('Visible ink',(0,0,2.38),(.28,.28,.02),p('accent'))
    G.face(.25,.51,1.99,.95,bone='Body')
    for i,(x,y) in enumerate([(-1,-.32),(1,-.32),(-.70,.50),(.70,.50)]):
        bone=f'Appendage_{i}';start=(x*.4,y*.4,1.28);G.BONES[bone]=start
        G.tube('Flexible brush handle',[start,(x*.82,y*.8,.99),(x,y,.42)],[.11,.10,.10],glass,bone)
        G.ring('Brush brass ferrule',(x,y,.42),.13,.04,p('gold'),bone)
        color=m('brush'+str(i),['#F39E39','#85D94C','#48CDE2','#E752B4'][i])
        G.loft('Brush paint bristles',[(x,y,.4,.12,.12),(x,y+.04,.17,.17,.16),(x,y+.10,.12,.15,.14)],color,bone,rows=16)
        for j in range(9):G.tube('Individual bristle groove',[(x+(j-4)*.031,y+.13,.35),(x+(j-4)*.037,y+.19,.14)],.005,p('cream'),bone)

def bublik():
    dough=m('main','#E7AE5F','dough');G.ring('Open doughnut torso',(0,0,1.12),.66,.25,dough,axis='Y')
    for i in range(19):
        a=pi*(.04+.92*i/18);G.ell('Lavender icing ribbon',(.66*cos(a),.19,1.12+.66*sin(a)),(.19,.09,.17),p('accent'))
    for i in range(13):
        a=.2+2.7*i/12;x=.66*cos(a);z=1.12+.66*sin(a)
        G.tube('Candy sprinkle',[(x-.025,.285,z-.035),(x+.025,.29,z+.035)],.018,m('sprinkle'+str(i%3),['#56DCE7','#FFCF66','#F782BC'][i%3]))
    G.ell('Hippo head',(0,.04,2.19),(.60,.48,.50),m('hippo','#B99DDC'),'Head')
    G.ell('Broad hippo muzzle',(0,.43,2.01),(.47,.24,.26),p('accent'),'Head')
    G.face(.24,.47,2.34,.9)
    for s,label in [(-1,'L'),(1,'R')]:
        G.ell('Hippo ear',(s*.48,0,2.56),(.13,.10,.17),p('accent'),'Head')
        G.tube('Hippo arm',[(s*.74,0,1.45),(s*1.0,.1,1.22),(s*.97,.2,1.06)],.13,p('accent'),'Arm_'+label)
        G.tube('Hippo leg',[(s*.45,0,.55),(s*.47,.05,.22)],.16,p('accent'),'Leg_'+label)
        G.ell('Hippo foot',(s*.47,.15,.15),(.25,.30,.15),p('accent'),'Leg_'+label)

def chaylitka():
    snail=m('main','#93D4A9');ceramic=m('teapot','#ED8E94','glass')
    G.loft('Continuous snail foot and neck',[(0,-1.0,.15,.12,.10),(0,-.42,.2,.45,.17),(0,.43,.25,.43,.22),(0,.82,.74,.26,.27),(0,.91,1.30,.25,.25)],snail,rows=46)
    G.ell('Snail soft head',(0,.92,1.39),(.35,.29,.30),snail,'Head')
    for s in (-1,1):
        G.tube('Snail eyestalk',[(s*.20,.93,1.55),(s*.27,.94,1.95),(s*.32,.92,2.21)],[.045,.04,.055],snail,'Head')
        G.ell('Snail stalk eye',(s*.32,.95,2.23),(.13,.11,.15),p('white'),'Head')
        G.ell('Snail stalk pupil',(s*.32,1.04,2.23),(.07,.035,.09),p('dark'),'Head')
    G.tube('Snail smile',[(-.2,1.15,1.38),(0,1.23,1.29),(.2,1.15,1.38)],.018,p('dark'),'Head')
    G.ell('Porcelain teapot shell',(0,-.24,1.20),(.70,.66,.79),ceramic)
    G.ring('Teapot lid rim',(0,-.24,1.90),.36,.05,p('cream'))
    G.ell('Teapot lid',(0,-.24,1.92),(.36,.36,.09),ceramic)
    G.ell('Lid teal knob',(0,-.24,2.05),(.09,.09,.085),p('accent'))
    G.loft('Curved pouring spout',[(-.52,-.15,1.12,.18,.18),(-.9,-.06,1.44,.15,.15),(-1.05,.03,1.77,.12,.12)],ceramic,rows=30)
    G.tube('Ceramic loop handle',[(.50,-.25,1.65),(.89,-.28,1.92),(1.01,-.25,1.38),(.64,-.22,.79)],.065,ceramic)

def kassetych():
    G.box('Rounded cassette shell',(0,0,1.3),(1.68,.39,1.12),bevel=.12)
    G.box('Amber reel window',(0,.21,1.48),(1.34,.055,.65),p('gold'),bevel=.08)
    for s in (-1,1):
        G.ring('Ivory reel eye',(s*.40,.26,1.50),.25,.046,p('cream'),axis='Y')
        G.ell('Reel eye hub',(s*.40,.28,1.50),(.17,.025,.20),p('iris'))
        G.ell('Reel pupil',(s*.40,.31,1.50),(.09,.02,.13),p('dark'))
        G.ell('Reel highlight',(s*.40-.03,.33,1.56),(.028,.009,.035),p('white'),segments=12,rings=8)
        for z in (.84,1.78):G.ell('Casing screw',(s*.70,.225,z),(.035,.012,.035),p('metal'),segments=12,rings=8)
    G.tube('Cassette grin',[(-.27,.23,1.10),(0,.29,1.03),(.27,.23,1.10)],.027,p('dark'))
    for s,label in [(-1,'L'),(1,'R')]:G.tube('Rubber cable arm',[(s*.79,0,1.26),(s*1.06,.1,1.14),(s*1.13,.12,1.50)],.07,p('accent'),'Arm_'+label)
    for i,(x,y) in enumerate([(-.55,-.14),(.55,-.14),(-.55,.14),(.55,.14)]):G.ring('Rubber roller foot',(x,y,.34),.20,.075,p('dark'),f'Appendage_{i}',axis='Y')
    G.fan('Loose magnetic tape',[(-.69,-.12,1.64),(-1.12,-.08,2.26),(-.75,-.05,2.55),(-.45,-.02,2.30),(-.83,-.02,2.03),(-.55,-.08,1.65)],m('tape','#78513D','paper'),'Tail',axis='Y')

def lampochmel():
    glass=m('main','#F4D452','glass');dark=m('bee','#493C36')
    G.ell('Luminous bulb abdomen',(0,-.40,1.28),(.65,.65,.70),glass)
    G.ring('Brass bulb collar',(0,-.10,1.75),.42,.06,p('gold'))
    G.ell('Fuzzy bee thorax',(0,.13,1.88),(.49,.39,.48),dark)
    G.ell('Bee head',(0,.39,2.22),(.49,.36,.46),p('accent'),'Head')
    G.face(.20,.73,2.32,.85)
    for s,label in [(-1,'L'),(1,'R')]:
        G.tube('Curled bee antenna',[(s*.21,.4,2.52),(s*.40,.4,2.88),(s*.52,.45,2.76)],.028,dark,'Head')
        G.fan('Translucent-looking bee wing',[(s*.20,-.10,1.94),(s*.48,-.36,2.71),(s*.99,-.39,2.73),(s*.91,-.16,2.21)],m('wing','#E5E8CB','glass'),'Arm_'+label,axis='Y')
        for j in range(3):
            b=f'Appendage_{j*2+(s>0)}';x=s*.31;y=.05-j*.19
            G.tube('Six bee legs',[(x,y,1.58),(s*.60,y+.03,1.22),(s*.54,y+.16,1.08)],.044,dark,b)
    G.coil('Visible warm filament',(0,.23,1.05),.16,.39,2.5,p('cream'),thickness=.018)

def noskoraket():
    cloth=m('main','#E99086','cloth');cream=m('cream','#F5E7D0','cloth')
    G.loft('Curved knitted sock body',[(0,-.64,1.54,.25,.31),(0,-.36,1.50,.52,.52),(0,.23,1.36,.65,.52),(0,.79,1.30,.43,.31),(0,.96,1.31,.06,.06)],cloth,rows=45)
    for j in range(7):
        y=-.49+j*.19;r=.44+.17*sin((j/7)*pi)
        G.ring('Cream sock knit stripe',(0,y,1.46),r,.035,cream,axis='Y')
    G.ring('Thick rolled sock cuff',(0,-.67,1.58),.30,.095,cloth,axis='Y')
    G.face(.23,.67,1.63,.83,bone='Body')
    G.limbs(2,False,feet=cloth)
    for s,label in [(-1,'L'),(1,'R')]:G.fan('Folded cloth side fin',[(s*.48,-.2,1.55),(s*.91,-.22,1.15),(s*.53,.25,1.13)],cream,'Arm_'+label,axis='Z')
    G.box('Wooden tail clothespin',(0,-.73,2.0),(.16,.17,.55),p('gold'),'Tail',.025)

def khrusto():
    dough=m('main','#DCAE67','dough');animal=m('animal','#AC91BE')
    G.ell('Croissant armored back',(0,-.14,1.12),(.77,.79,.79),dough)
    for i in range(9):
        a=pi*(.08+.84*i/8)
        points=[(.8*cos(a),-.14+.72*sin(t),1.12+.82*sin(a)*cos(t)) for t in [(-.85+j*.17) for j in range(11)]]
        G.tube('Laminated croissant ridge',points,.036,p('cream'))
    G.ell('Armadillo sculpted head',(0,.61,1.30),(.42,.49,.40),animal,'Head')
    G.loft('Tapered armadillo muzzle',[(0,.83,1.2,.26,.22),(0,1.18,1.1,.13,.12),(0,1.31,1.1,.04,.05)],animal,'Head',rows=26)
    G.face(.19,.94,1.51,.7)
    for s in (-1,1):G.ell('Armadillo ear',(s*.31,.48,1.64),(.13,.07,.21),animal,'Head')
    G.limbs(4,False,feet=animal)
    G.tube('Croissant plated tail',[(0,-.75,.95),(.39,-1.06,.81),(.62,-1.2,1.02),(.56,-1.30,1.23)],[.16,.12,.08,.015],dough,'Tail')

def magnitofyr():
    G.ell('Round furry rhino torso',(0,-.18,1.15),(.79,.72,.86))
    G.ell('Furry rhino head',(0,.50,1.73),(.56,.48,.52),bone='Head')
    G.ell('Satin rhino muzzle',(0,.86,1.50),(.43,.27,.26),p('accent'),'Head')
    G.face(.24,.87,1.92,.87)
    for s in (-1,1):G.ell('Rhino ear',(s*.48,.38,2.10),(.14,.10,.22),bone='Head')
    G.tube('Horseshoe magnet horn',[(-.25,1.08,2.25),(-.25,1.08,1.83),(0,1.13,1.73),(.25,1.08,1.83),(.25,1.08,2.25)],.10,p('metal'),'Head')
    for s,col in [(-1,'#EC8777'),(1,'#48BED6')]:G.box('Rounded magnet pole',(s*.25,1.08,2.25),(.20,.20,.21),m('pole'+str(s),col),'Head',.035)
    G.limbs(4,False,feet=p('metal'))
    G.tube('Rhino tail',[(0,-.81,1.28),(.29,-1.02,1.6),(.35,-1.05,1.76)],.043,bone='Tail')

def garmoshmyak():
    red=m('main','#CE575D','cloth')
    for j in range(13):
        y=-.8+j*.12;r=.58+(.06 if j%2 else 0)
        G.loft('Accordion leather bellows rib',[(0,y-.025,1.16,r,.66),(0,y+.025,1.16,r,.66)],red,rows=3,around=24)
        if j%2==0:G.ring('Bellows cream piping',(0,y,1.16),r,.016,p('cream'),axis='Y')
    G.ell('Caterpillar enamel head',(0,.82,1.30),(.57,.29,.52),p('accent'),'Head')
    G.face(.23,1.09,1.47,.89)
    for s in (-1,1):G.tube('Striped caterpillar antenna',[(s*.23,.82,1.74),(s*.33,.82,2.12),(s*.43,.86,2.16)],.034,p('gold'),'Head')
    for i in range(6):
        s=-1 if i%2==0 else 1;y=-.57+(i//2)*.50
        G.ell('Six soft caterpillar feet',(s*.48,y,.36),(.18,.24,.21),p('gold'),f'Appendage_{i}')

def puzyr():
    pearl=m('main','#B9E4ED','glass')
    G.loft('Pear-shaped bubble volume',[(0,0,.40,.34,.32),(0,0,.93,.74,.51),(0,0,1.55,.66,.49),(0,0,2.16,.43,.37),(0,0,2.46,.13,.14)],pearl,rows=42)
    G.face(.23,.45,2.02,.95,bone='Body')
    for s in (-1,1):G.ell('Sponge heart lobe',(s*.13,.52,1.03),(.20,.07,.22),p('gold'))
    G.fan('Sponge heart point',[(-.25,.53,1.04),(0,.55,.72),(.25,.53,1.04)],p('gold'),axis='Y')
    G.limbs(2,True,feet=pearl)
    for i in range(14):
        a=i*2.39996;G.ell('White soap foam crown',(.21*cos(a),.16*sin(a),2.43+.09*(i%3)),(.10,.09,.10),p('white'),'Head',16,10)

def klubkomot():
    yarn=m('main','#BC63B7','cloth');lav=m('head','#CFACDD','cloth')
    G.ell('Wound yarn-ball torso',(0,-.21,1.12),(.78,.72,.85),yarn)
    for j in range(23):
        a=j*.47;pts=[]
        for k in range(33):
            t=2*pi*k/32;pts.append((.79*cos(t)*cos(a),-.21+.73*cos(t)*sin(a),1.12+.86*sin(t)))
        G.tube('Thick wound yarn strand',pts,.014,p('accent'))
    G.ell('Knitted mammoth head',(0,.47,1.83),(.48,.41,.53),lav,'Head')
    G.face(.20,.83,1.98,.83)
    for s in (-1,1):
        G.ell('Floppy yarn ear',(s*.52,.35,1.98),(.30,.11,.41),yarn,'Head')
        G.tube('Wooden crochet tusk',[(s*.25,.81,1.60),(s*.40,1.05,1.33),(s*.53,1.15,1.48)],[.07,.055,.02],p('cream'),'Head')
    for j in range(3):
        pts=[]
        for k in range(19):
            t=k/18;pts.append((.065*cos(t*6*pi+j*2*pi/3),.86+.35*t,1.79-1.0*t+.15*sin(t*pi)))
        G.tube('Braided yarn trunk strand',pts,.065,lav,'Head')
    G.limbs(4,False,feet=lav)
    G.tube('Loose yarn tail',[(0,-.84,1.2),(-.4,-1.02,1.0),(-.55,-1.02,.60)],.035,yarn,'Tail')

def luna():
    outer=[(cos(t),sin(t)+1.57) for t in [pi/3+i*(4*pi/3)/48 for i in range(49)]]
    inner=[(.46+.83*cos(t),1.57+.83*sin(t)) for t in [-pi/2-i*pi/36 for i in range(37)]]
    shape=outer+inner;n=len(shape);vs=[(x,y,z) for y in (-.20,.20) for x,z in shape]
    triangles=tessellate_polygon([[Vector((x,z,0)) for x,z in shape]])
    lookup={(round(x,7),round(z,7)):i for i,(x,z) in enumerate(shape)};fs=[]
    for tri in triangles:
        ids=[v if isinstance(v,int) else lookup[(round(v.x,7),round(v.y,7))] for v in tri];fs.extend([tuple(reversed(ids)),tuple(i+n for i in ids)])
    fs.extend((i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n))
    obj=G.D.mesh('Thick crescent lunar body',vs,fs,p('main'),'Body')
    bevel=obj.modifiers.new('Soft lunar rim','BEVEL');bevel.width=.06;bevel.segments=3
    bpy.context.view_layer.objects.active=obj;bpy.ops.object.modifier_apply(modifier=bevel.name)
    G.BONES['Head']=(-.57,0,1.8)
    G.face(.15,.24,1.8,.63)
    for obj in G.D.PARTS['Head']:obj.location.x-=.58
    for s,label in [(-1,'L'),(1,'R')]:
        x=-.45+s*.23;G.ell('Lunar padded foot',(x,.02,.21),(.21,.28,.19),p('main'),'Leg_'+label)
        G.tube('Moon stubby leg',[(x,0,.79),(x,.02,.28)],.11,p('main'),'Leg_'+label)
    for i in range(12):
        a=1.1+i*.29;x=.79*cos(a);z=1.57+.79*sin(a)
        G.ring('Smooth moon crater rim',(x,.21,z),.045,.011,p('accent'),axis='Y')

def kaktus():
    green=m('main','#69AE8B','leaf')
    G.loft('Ribbed cactus torso',[(0,0,.35,.28,.28),(0,0,.8,.65,.48),(0,0,1.4,.65,.48),(0,0,2.0,.42,.36),(0,0,2.14,.14,.16)],green,rows=40,ribs=10)
    G.face(.21,.41,1.92,.81,bone='Body')
    G.ell('Rubber squeeze-bulb belly',(0,.48,1.05),(.36,.10,.39),p('accent'))
    G.limbs(2,False,feet=green)
    for s,label in [(-1,'L'),(1,'R')]:
        G.tube('Curved cactus arm',[(s*.5,0,1.15),(s*.89,0,1.31),(s*.98,.04,1.82)],.16,green,'Arm_'+label)
        for j in range(4):G.ell('Soft blunt cactus spine',(s*(.82+.03*(j%2)),.14,1.28+j*.16),(.025,.05,.035),p('gold'),'Arm_'+label,12,8)
    G.loft('Trumpet-flower flare',[(0,0,2.09,.08,.08),(0,0,2.5,.10,.10),(0,0,2.8,.24,.24),(0,0,2.94,.48,.48)],p('gold'),'Head',rows=30)
    G.ring('Trumpet flower lip',(0,0,2.94),.48,.035,p('gold'),'Head')
    G.ell('Trumpet inner shadow',(0,0,2.937),(.43,.43,.01),m('horninner','#B68032'),'Head')

def pylekhvost():
    fur=m('animal','#DEA568');metal=m('main','#50A7B8','metal')
    G.ell('Vacuum canister torso',(0,-.15,1.12),(.70,.64,.77),metal)
    G.ell('Fuzzy anteater head',(0,.47,1.76),(.47,.38,.48),fur,'Head')
    G.face(.20,.79,1.91,.81)
    for s in (-1,1):G.ell('Small anteater ear',(s*.38,.32,2.08),(.12,.09,.17),fur,'Head')
    pts=[(0,.76,1.65),(0,1.02,1.31),(0,1.18,.82),(0,1.25,.48)]
    G.tube('Flexible suction hose snout',pts,[.13,.13,.12,.12],p('dark'),'Head')
    for j in range(11):G.ring('Corrugated hose rib',(0,.88+j*.034,1.53-j*.08),.13,.015,p('metal'),'Head')
    G.ell('Broad brush suction nozzle',(0,1.27,.35),(.31,.23,.12),metal,'Head')
    for j in range(11):G.tube('Nozzle brush bristle',[(.045*(j-5),1.46,.35),(.05*(j-5),1.50,.14)],.012,p('dark'),'Head')
    G.limbs(4,False,feet=fur)
    G.tube('Fuzzy sweeping tail',[(0,-.69,1.15),(.60,-.91,1.38),(.90,-.77,1.96),(.61,-.75,2.40)],[.13,.19,.21,.16],fur,'Tail')
    G.ell('Power button',(0,-.13,1.85),(.13,.14,.035),p('accent'))

def popkorn():
    butter=m('main','#EFD991','dough');G.ell('Pufferfish body',(0,0,1.40),(.81,.64,.86),butter)
    G.face(.27,.62,1.69,1.1,bone='Body',smile=False)
    G.ring('Surprised pink fish mouth',(0,.69,1.31),.09,.033,m('mouth','#ED9A9D'),axis='Y')
    rng=random.Random(260)
    for i in range(78):
        z=1-2*(i+.5)/78;a=i*2.39996;r=(1-z*z)**.5;x=.83*r*cos(a);y=.66*r*sin(a)
        if y>.33 and abs(x)<.60 and z>-.35:continue
        G.ell('Soft popcorn floret',(x,y,1.40+.88*z),(.13+rng.random()*.035,.13,.13),p('cream'),segments=14,rings=8)
    for s,label in [(-1,'L'),(1,'R')]:
        G.leaf('Corn-husk fin',(s*.65,0,1.35),(s*1.19,.12,1.72),.28,p('accent'),'Arm_'+label)
        G.leaf('Folded husk foot',(s*.30,0,.65),(s*.44,.20,.17),.13,p('accent'),'Leg_'+label)
    for s in (-1,1):G.leaf('Husk fan tail',(0,-.47,1.35),(s*.46,-1.08,1.56),.24,p('accent'),'Tail')

BUILDERS=[rollo,pelmolyot,morzho,utyugonos,makaronika,shurshun,brokko,budilo,arbuzub,fenik,zhele,chernilo,bublik,chaylitka,kassetych,lampochmel,noskoraket,khrusto,magnitofyr,garmoshmyak,puzyr,klubkomot,luna,kaktus,pylekhvost,popkorn]
