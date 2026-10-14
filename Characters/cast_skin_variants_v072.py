"""Four further authored designs per hero. Used by build_cast_skins_v072.py.
Each variant changes geometry as well as its material palette. The already shipped
Bombardiro lava package is reused by the generator, giving exactly five per hero.
"""
from math import sin,cos,pi

# suffix, Russian name, English name, brief, main, dark, accent, light, energy.
VARIANTS=[
[
('ice_sprinter','ЛЕДЯНОЙ СПРИНТЕР','Ice Sprinter','Ледяные накладки · снежные кроссовки','#6FB8CC','#24516A','#9AEAF5','#EEF8F3','#A9ECF3'),
('reef_pirate','ПИРАТ РИФОВ','Reef Pirate','Треуголка · морские узлы на обуви','#2D6474','#3D2C28','#CA9A5A','#E3D4A8','#D6AB60'),
('neon_surfer','НЕОНОВЫЙ СЁРФЕР','Neon Surfer','Волнистый визор · неоновые плавники','#353875','#151D3D','#EE74CB','#D4F4E1','#8CEFC1'),
('space_swimmer','КОСМИЧЕСКИЙ ПЛОВЕЦ','Space Swimmer','Боковые турбины · звёздный визор','#626794','#262D4D','#F0AD52','#E0EBEF','#85CFEF')],
[
('lava','ЛАВОВЫЙ ПЕРЕХВАТЧИК','Lava Interceptor','Обсидиановая броня · огненные швы','#263344','#101925','#FF6E29','#B7B0A0','#FFAA3A'),
('storm_pilot','ШТОРМОВОЙ ПИЛОТ','Storm Pilot','Грозовые крылья · навигационные антенны','#435484','#202B46','#DFB14D','#BBD7E8','#85CBEF'),
('clockwork_aviator','ЗАВОДНОЙ АВИАТОР','Clockwork Aviator','Медные панели · заводной ключ','#A67345','#3F413C','#D9B96C','#E7DABC','#94CEB9'),
('tropical_rescuer','ТРОПИЧЕСКИЙ СПАСАТЕЛЬ','Tropical Rescuer','Посадочные поплавки · цветные крылья','#71BFA8','#20534E','#EFA36B','#E8E2C0','#96DFCB')],
[
('retro_soda','РЕТРО-ЛИМОНАД','Retro Soda','Ретро-дверцы · арочный приборный щиток','#A94F56','#653342','#E8AF55','#F1DFB8','#DADDA1'),
('desert_caravan','ПУСТЫННЫЙ КАРАВАН','Desert Caravan','Тканый шарф · походный багаж','#C89F65','#765841','#A95840','#F3D7A8','#DDB96A'),
('galactic_chill','ГАЛАКТИЧЕСКИЙ ХОЛОД','Galactic Chill','Звёздная панель · космический ранец','#5D548C','#252745','#77BFD8','#DBE6F5','#91D9EE'),
('gingerbread','ПРЯНИЧНЫЙ ФРИГО','Gingerbread Frigo','Пряничные дверцы · сахарная отделка','#AD7444','#623D2E','#D06C59','#F4E1B5','#E4B964')],
[
('polar_post','ПОЛЯРНЫЙ ПОЧТАЛЬОН','Polar Post','Зимний шлем · снежные эмблемы','#719EB0','#31586A','#9DE1E1','#EAEBD8','#B8EFF0'),
('steam_goose','СТИМПАНК ГУСЬ','Steam Goose','Медная турбина · зубчатые кольца','#A47A50','#40494C','#DABB73','#DDCFAC','#A0CAB6'),
('thunder_courier','ГРОЗОВОЙ КУРЬЕР','Thunder Courier','Грозовые законцовки · молнии на крыльях','#666097','#292E4C','#DFB658','#DEE2F3','#A8C9F2'),
('tropical_aviator','ТРОПИЧЕСКИЙ АВИАТОР','Tropical Aviator','Цветочная сбруя · коралловые поплавки','#4DAB96','#244D47','#E79376','#EBDCB9','#B5E1B4')],
[
('cherry_ballet','ВИШНЁВЫЙ БАЛЕТ','Cherry Ballet','Вишнёвые украшения · алые банты','#B74968','#5A2747','#D69E63','#F5D9C8','#D8A266'),
('moon_prima','ЛУННАЯ ПРИМА','Moon Prima','Лунная диадема · звёздная вышивка','#4A5A88','#242A51','#D6C089','#E9EBD8','#A9CAE6'),
('matcha_fairy','МАТЧА-ФЕЯ','Matcha Fairy','Чайные листья · цветочный атлас','#77A55F','#355E45','#D1B76B','#E9E7BA','#AED995'),
('caramel_prima','КАРАМЕЛЬНАЯ ПРИМА','Caramel Prima','Карамельные ленты · золотые розетки','#BE8055','#724937','#D9AB68','#EFDDC0','#E8B575')],
[
('ice_ninja','ЛЕДЯНОЙ НИНДЗЯ','Ice Ninja','Ледяная броня · снежный знак','#72B5C1','#284F66','#AEDFE6','#E4ECE7','#92DFE5'),
('scarlet_shogun','АЛЫЙ СЁГУН','Scarlet Shogun','Алые пластины · шлем с рогами','#A94E47','#382F3D','#DBAA61','#E6CCA4','#D7AF68'),
('night_courier','НОЧНОЙ КУРЬЕР','Night Courier','Плечевая сумка · дорожная сбруя','#6B688C','#282B3F','#A6B4CF','#E0DEDB','#A4D4E3'),
('copper_ronin','МЕДНЫЙ РОНИН','Copper Ronin','Медный шлем · орнаментальные накладки','#A97E52','#344B48','#D6B474','#E4D4AF','#98C7B3')],
[
('autumn_woods','ОСЕННИЙ ЛЕС','Autumn Woods','Осенние листья · амулет-жёлудь','#A8873E','#634B2D','#CF7656','#DFC88A','#E5B667'),
('northern_guardian','ХРАНИТЕЛЬ СЕВЕРА','Northern Guardian','Снежные ветви · тёплый воротник','#5B8E8A','#3E5356','#A9CFD1','#E2E6D3','#B5E1DE'),
('jungle_shaman','ДЖУНГЛЕВЫЙ ШАМАН','Jungle Shaman','Листовой венец · деревянные подвески','#438B59','#4D4D32','#D59C59','#DADEA8','#A9D46E'),
('moon_forest','ЛУННЫЙ ЛЕС','Moon Forest','Лунные грибы · серебряные листья','#62678E','#343C54','#ABA0CE','#DBDCE3','#BDD2E6')],
[
('rock_sahur','РОК-САХУР','Rock Sahur','Рок-жилет · музыкальные знаки','#695043','#232D3B','#C48E69','#D4C7A7','#8EADB8'),
('festival_sahur','ПРАЗДНИЧНЫЙ САХУР','Festival Sahur','Праздничный пояс · праздничный колпак','#A96D42','#6A3640','#E6BA65','#EFD7AA','#E6AB5B'),
('root_keeper','ХРАНИТЕЛЬ КОРНЕЙ','Root Keeper','Живые корни · листовой венец','#805B37','#3E593F','#9AC270','#D8D8A0','#C7B568'),
('clockwork_sahur','ЧАСОВОЙ САХУР','Clockwork Sahur','Часовой механизм · латунные обручи','#956D42','#3B474B','#CDA76B','#DDCFAC','#A2C2B5')],
[
('flowering_cactus','ЦВЕТУЩИЙ КАКТУС','Flowering Cactus','Цветы на кактусе · лепестковые сандалии','#528956','#335A42','#E5A1B5','#E7DABB','#DAB167'),
('sand_wanderer','ПЕСЧАНЫЙ ПУТНИК','Sand Wanderer','Походный багаж · тканый головной убор','#B28852','#5D5A3C','#C6774E','#E3CC99','#CFB06E'),
('star_oasis','ЗВЁЗДНЫЙ ОАЗИС','Star Oasis','Звёздное седло · лунный талисман','#7477A2','#373B5C','#CFB773','#E0DECE','#ACCBE9'),
('copper_caravan','МЕДНЫЙ КАРАВАН','Copper Caravan','Медные пряжки · караванные колокольцы','#A0784C','#386059','#D3AD69','#E6D6B1','#98D1BE')],
[
('sky_postbird','НЕБЕСНЫЙ ПОЧТАЛЬОН','Sky Postbird','Почтовая сумка · лётный берет','#6388B3','#344965','#D5B277','#E7D9BC','#A9CEE1'),
('rock_beak','РОК-КЛЮВ','Rock Beak','Рок-воротник · наплечные шипы','#655483','#2D2D43','#BC92CE','#DDD9D2','#C2A0E2'),
('sun_surfer','СОЛНЕЧНЫЙ СЁРФЕР','Sun Surfer','Тропические цветы · солнечный кулон','#45A58D','#275E58','#E8B26F','#EFE0B8','#BCE7AD'),
('polar_messenger','ПОЛЯРНЫЙ ВЕСТНИК','Polar Messenger','Зимняя шапка · мягкий шарф','#6298B2','#304D68','#B0D8E0','#E5E8DA','#ADDFF0')],
[
('swamp_tracker','БОЛОТНЫЙ СЛЕДОПЫТ','Swamp Tracker','Листовой козырёк · лесной обод','#568A4B','#344336','#B0A466','#D6DBAC','#AED09B'),
('neon_rally','НЕОНОВОЕ РАЛЛИ','Neon Rally','Неоновые ободы · гоночный номер','#675D99','#252D45','#BA88D7','#DCE7ED','#7BDCD3'),
('polar_rover','ПОЛЯРНЫЙ ВЕЗДЕХОД','Polar Rover','Снежный козырёк · зимние манжеты','#78A7AA','#39566B','#ADDBE3','#E8EBDD','#BEE8E7'),
('sand_rover','ПЕСКОХОД','Sand Rover','Пустынная сбруя · песочные накладки','#A08C54','#4E503B','#D1AE6B','#E9D8B1','#C4D6A0')],
[
('lava_shrimp','ЛАВОВЫЙ РАК','Lava Shrimp','Огненный панцирь · обсидиановые накладки','#6A5263','#342A3B','#E77E55','#E5C79F','#F0AC66'),
('northern_shrimp','СЕВЕРНАЯ КРЕВЕТКА','Northern Shrimp','Снежные сегменты · жемчужная подвеска','#79B5CD','#3A6179','#B9DCE3','#E6E9DD','#A4DFE9'),
('royal_reef','КОРОЛЕВСКИЙ РИФ','Royal Reef','Золотой панцирь · королевская корона','#B79A56','#414875','#DBBE75','#E9DDC0','#D8B666'),
('space_cat','КОСМИЧЕСКИЙ КОТ','Space Cat','Космический ранец · звёздные сегменты','#8370AF','#34384E','#98BCE2','#E4DDD7','#AEC7F0')],
[
('sun_captain','СОЛНЕЧНЫЙ КАПИТАН','Sun Captain','Летняя панама · тропический мундир','#4B9B9C','#275571','#E5AF68','#EEE6CC','#DAB873'),
('polar_fisher','ПОЛЯРНЫЙ РЫБОЛОВ','Polar Fisher','Зимняя фуражка · тёплые манжеты','#60949D','#334E68','#ACD6DD','#E3E7D9','#C5DCD5'),
('jade_guard','НЕФРИТОВЫЙ СТРАЖ','Jade Guard','Нефритовые плечи · морской щиток','#3D8F68','#293F3B','#D1AE67','#E3D4AB','#A5CFB2'),
('festival_gourmet','ПРАЗДНИЧНЫЙ ГУРМАН','Festival Gourmet','Поварской колпак · вишнёвый бант','#8C515B','#543A43','#DDB374','#F0DDC2','#E1C286')]
]

def add_details(hero,variant,m,g):
    # g is the generator module: each shape is real authored mesh/curve data.
    tube,ell,ring,box,panel,band,leaf,blossom,bow=[getattr(g,k) for k in ('tube','ell','ring','box','panel','band','leaf','blossom','bow')]
    main,dark,accent,light,energy=[m[k] for k in ('main','dark','accent','light','energy')]
    if hero==4:
        for obj in list(g.COSMETICS):
            if obj.name.startswith(('SKIN Pearl tiara crown','SKIN Tiara pearl')):
                g.COSMETICS.remove(obj);g.bpy.data.objects.remove(obj,do_unlink=True)
    def motif(kind,c,size,bone='Body'):
        x,y,z=c
        if kind in ('star','snow','bolt'):
            if kind=='star':points=[(x+size*(1 if i%2==0 else .42)*cos(pi/2+i*pi/5),y,z+size*(1 if i%2==0 else .42)*sin(pi/2+i*pi/5)) for i in range(10)]
            elif kind=='bolt':points=[(x-size*.1,y,z+size),(x-size*.7,y,z-size*.04),(x-size*.05,y,z-size*.02),(x-size*.2,y,z-size),(x+size*.7,y,z+size*.1),(x+size*.08,y,z+size*.1)]
            else:
                for a in (0,pi/3,2*pi/3):tube('Snow stitched symbol',[(x-size*cos(a),y,z-size*sin(a)),(x+size*cos(a),y,z+size*sin(a))],.011,light,bone)
                return
            # Front-facing emblem has thickness along Y, unlike a horizontal plate.
            n=len(points);v=points+[(a,b+.018,c) for a,b,c in points]
            f=[tuple(range(n)),tuple(range(2*n-1,n-1,-1))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
            g.mesh(kind+' embroidered crest',v,f,energy,bone)
        elif kind=='cog':
            ring('Clockwork wheel',c,size*.78,.015,accent,bone,rotation=(pi/2,0,0))
            for i in range(10):
                a=i*pi/5;obj=box('Clockwork tooth',(x+size*cos(a),y,z+size*sin(a)),(.033,.028,.04),accent,bone,.006);obj.rotation_euler.y=-a
        elif kind=='moon':
            tube('Crescent inlay',[(x+size*.35,y,z+size),(x-size*.5,y,z+size*.6),(x-size*.65,y,z),(x-size*.5,y,z-size*.6),(x+size*.35,y,z-size)],.016,light,bone)
        elif kind=='music':
            tube('Music icon',[(x,y,z-size*.4),(x,y,z+size),(x+size*.65,y,z+size*.5)],.014,accent,bone)
            ell('Music icon bead',(x-size*.22,y,z-size*.45),(.038,.021,.028),accent,bone,12,8)
    def soft_cap(c,bone='Head',rx=.3,ry=.28):
        ell('Tailored variant cap',(c[0],c[1],c[2]+.055),(rx,ry,.10),main,bone,24,12)
        band('Variant cap brim',c[2],rx*.94,ry*.94,.045,accent,bone,(c[0],c[1]))
    def shoulder_crystals(label,x,z):
        for j in range(3):
            panel('Cut crystal accent',[(x-.055,-.02,z+j*.04),(x+.055,-.02,z+j*.04),(x+.045,.09,z+j*.04),(x,.07,z+.16+j*.04)],.025,energy,label,.006)
    if hero==0:
        if variant==1:
            for s,label in [(-1,'L'),(1,'R')]:
                panel('Ice fin inlay',[(s*.38,.37,1.79),(s*.53,.03,1.62),(s*.48,-.25,1.68),(s*.27,.01,1.91)],.023,light)
                for j in range(2):panel('Icy sneaker fin',[(s*.45-.08,.38+j*.1,.39),(s*.45+.08,.38+j*.1,.39),(s*.45,.37+j*.1,.50)],.018,energy,'Foot_'+label,.005)
            motif('snow',(0,1.08,1.78),.09)
        elif variant==2:
            panel('Low pirate tricorn',[(-.28,.86,1.98),(0,.84,2.12),(.28,.86,1.98),(.19,1.18,1.90),(-.19,1.18,1.90)],.04,dark)
            tube('Pirate hat gold stitching',[(-.24,.87,2.0),(0,.85,2.11),(.24,.87,2.0)],.015,accent)
            for x,y,b in [(-.45,.44,'Foot_L'),(.45,.44,'Foot_R'),(0,-1.10,'Foot_Rear')]:
                bow('Nautical shoe knot',(x,y+.13,.40),accent,light,b)
            motif('star',(0,1.13,1.98),.055)
        elif variant==3:
            tube('Surfer brow visor',[(-.37,1.18,1.75),(0,1.38,1.81),(.37,1.18,1.75)],.026,energy)
            for s,label in [(-1,'L'),(1,'R')]:
                tube('Surf fin neon trim',[(s*.46,.54,1.09),(s*.58,.3,.81),(s*.52,.40,.51)],.018,energy,'Thigh_'+label)
                motif('bolt',(s*.45,.73,.25),.055,'Foot_'+label)
        else:
            for s in (-1,1):
                ell('Rounded swim thruster',(s*.51,-.72,1.50),(.12,.29,.14),light)
                ring('Swim thruster outlet',(s*.51,-1.0,1.50),.085,.022,energy,rotation=(pi/2,0,0))
            tube('Diver open face frame',[(-.48,1.07,1.66),(-.33,1.27,1.84),(0,1.4,1.88),(.33,1.27,1.84),(.48,1.07,1.66)],.023,light)
            motif('star',(0,-.92,1.70),.08)
    elif hero==1:
        if variant==2:
            for s in (-1,1):
                tube('Storm lightning wing',[(s*.78,-.71,1.91),(s*1.18,-.79,1.91),(s*1.02,-.94,1.91),(s*1.62,-1.0,1.91)],.023,energy)
                tube('Navigation mast',[(s*.22,-.36,2.21),(s*.22,-.35,2.43)],.018,accent)
            ell('Radar canoe',(0,-1.47,1.92),(.12,.30,.08),dark)
        elif variant==3:
            tube('Winding key stem',[(0,-.41,2.22),(0,-.41,2.46)],.035,accent)
            for s in (-1,1):ring('Butterfly winding key',(s*.10,-.41,2.45),.08,.025,accent,rotation=(pi/2,0,0))
            for s in (-1,1):
                motif('cog',(s*1.05,-.43,1.52),.14)
                panel('Copper wing scallop',[(s*1.60,-.59,1.90),(s*2.02,-.65,1.90),(s*2.06,-.85,1.90),(s*1.62,-.86,1.90)],.018,accent)
        elif variant==4:
            for s in (-1,1):
                ell('Rescue rounded pontoon',(s*1.14,-.74,1.03),(.16,.61,.12),main)
                tube('Float outrigger',[(s*1.14,-.6,1.18),(s*1.16,-.58,1.62)],.027,accent)
                blossom('Rescue wing hibiscus',(s*2.26,-.97,1.92),accent,light,'Body',.11)
    elif hero==2:
        if variant==1:
            ell('Retro raised instrument panel',(0,.452,2.24),(.29,.035,.10),accent)
            for j in range(3):ell('Retro chrome button',(-.11+j*.11,.50,2.25),(.025,.017,.025),light,segments=12,rings=8)
            tube('Cabinet retro side sweep',[(-.515,-.15,1.0),(-.52,-.1,1.63),(-.515,-.05,2.35)],.018,accent)
        elif variant==2:
            band('Camel wool scarf',2.24,.151,.185,.13,accent,'Head',(0,1.11))
            for s in (-1,1):box('Caravan cabinet pannier',(s*.54,-.20,1.65),(.19,.44,.35),dark,bevel=.065)
            tube('Woven scarf tail',[(.12,1.24,2.23),(.15,1.19,2.0),(.12,1.13,1.84)],.034,light,'Head')
        elif variant==3:
            motif('star',(0,.46,2.18),.14)
            box('Space cabinet backpack',(0,-.51,1.84),(.52,.22,.55),dark,bevel=.065)
            for s in (-1,1):tube('Cold circuit side',[(s*.525,-.2,1.12),(s*.525,-.2,1.63),(s*.525,-.12,1.85),(s*.525,-.12,2.34)],.015,energy)
        else:
            tube('Gingerbread cream door icing',[(-.39,.454,2.42),(0,.46,2.46),(.39,.454,2.42)],.026,light)
            for s in (-1,1):blossom('Sugar cabinet rosette',(s*.28,.46,1.79),accent,light,'Body',.065)
            motif('star',(0,.46,2.22),.12)
    elif hero==3:
        if variant==1:
            soft_cap((0,1.27,2.92),'Neck',.21,.23)
            for s,label in [(-1,'L'),(1,'R')]:motif('snow',(s*1.6,-.36,2.21),.10,'Wing_'+label)
        elif variant==2:
            motif('cog',(0,.38,1.82),.19)
            for s,label in [(-1,'L'),(1,'R')]:
                ring('Copper flight turbine collar',(s*1.42,-.015,2.12),.18,.025,accent,'Wing_'+label,rotation=(pi/2,0,0))
                tube('Steam flight pipe',[(s*.30,-.14,2.13),(s*.36,-.3,2.29),(s*.42,-.54,2.23)],.029,dark)
        elif variant==3:
            for s,label in [(-1,'L'),(1,'R')]:
                tube('Wing lightning stripe',[(s*.77,-.69,2.21),(s*1.02,-.78,2.21),(s*.92,-.99,2.21),(s*1.46,-1.03,2.21)],.025,energy,'Wing_'+label)
                panel('Storm tip winglet',[(s*1.94,-1.0,1.98),(s*2.18,-1.1,1.98),(s*2.22,-1.13,2.24)],.025,main,'Wing_'+label)
        else:
            for s in (-1,1):
                blossom('Courier hibiscus',(s*.34,-.15,2.15),accent,light,'Body',.12)
                ell('Coral landing float',(s*.33,-1.02,1.09),(.12,.33,.10),accent,'Foot_'+('L' if s<0 else 'R'))
    elif hero==4:
        if variant==1:
            for s in (-1,1):
                ell('Cherry hair ornament',(s*.17,-.06,3.055),(.078,.075,.075),accent,'Head',18,12)
                tube('Cherry stem',[(s*.17,-.06,3.10),(s*.12,-.06,3.19),(0,-.06,3.16)],.012,dark,'Head')
            bow('Cherry waist bow',(0,.173,1.48),main,light,'Body')
        elif variant==2:
            motif('moon',(0,.27,3.03),.12,'Head')
            for i in range(5):a=i*pi*.4;motif('star',(.59*cos(a),.59*sin(a),1.27),.043,'Skirt')
        elif variant==3:
            for s in (-1,1):leaf('Matcha crown leaf',(s*.22,-.06,3.07),(.17,.29,.04),main,'Head',s*.6)
            for i in range(5):a=i*2*pi/5;leaf('Fairy skirt petal',(.59*cos(a),.59*sin(a),1.25),(.15,.30,.045),energy,'Skirt',a)
        else:
            for s in (-1,1):blossom('Caramel porcelain rosette',(s*.35,-.23,2.99),accent,light,'Head',.075)
            tube('Caramel cup flourish',[(-.30,.27,2.94),(-.11,.4,2.952),(.11,.4,2.952),(.30,.27,2.94)],.019,accent,'Head')
            bow('Caramel bodice bow',(0,.21,1.83),main,light,'Body')
    elif hero==5:
        if variant==1:
            for s,label in [(-1,'L'),(1,'R')]:shoulder_crystals('Arm_'+label,s*.55,1.82)
            motif('snow',(0,.616,2.33),.072,'Head')
        elif variant==2:
            for s in (-1,1):tube('Shogun curved crest',[(s*.19,.03,2.65),(s*.32,.05,2.83),(s*.39,.07,2.92)],.035,accent,'Head')
            band('Red shogun belt',1.08,.46,.41,.10,main,'Head')
        elif variant==3:
            box('Courier padded back pouch',(0,-.52,1.67),(.35,.20,.40),main,bevel=.075)
            tube('Courier diagonal chest strap',[(-.42,.27,1.91),(0,.50,1.58),(.35,.29,1.11)],.024,accent,'Head')
            motif('star',(.25,.44,1.41),.065,'Head')
        else:
            soft_cap((0,-.02,2.665),'Head',.47,.46)
            motif('cog',(0,.59,2.35),.075,'Head')
            for s,label in [(-1,'L'),(1,'R')]:band('Copper ankle armor',.38,.16,.15,.12,accent,'Leg_'+label,(s*.25,.03))
    elif hero==6:
        if variant==1:
            for s in (-1,1):
                leaf('Autumn crown leaf',(s*.45,.05,2.91),(.23,.34,.04),accent,'Head',s*.8)
                ell('Acorn shoulder',(s*.31,.25,1.79),(.055,.049,.07),accent,segments=12,rings=8)
            ell('Chest acorn amulet',(0,.405,1.55),(.079,.045,.095),accent)
        elif variant==2:
            band('Soft winter forest collar',1.94,.31,.28,.13,light)
            for s in (-1,1):tube('Snow tipped branch',[(s*.42,-.10,3.03),(s*.55,-.10,3.12)],.031,light,'Head')
        elif variant==3:
            for s in (-1,1):
                leaf('Jungle broad leaf',(s*.43,.01,2.95),(.24,.41,.045),energy,'Head',s*.65)
                box('Carved shoulder talisman',(s*.41,.16,1.76),(.085,.04,.13),accent,bevel=.025)
        else:
            motif('moon',(0,.42,1.68),.13)
            for s in (-1,1):
                ell('Moon mushroom cap',(s*.43,-.04,3.0),(.10,.10,.055),accent,'Head',16,10)
                tube('Moon mushroom stem',[(s*.43,-.04,2.90),(s*.43,-.04,2.99)],.019,light,'Head')
    elif hero==7:
        if variant==1:
            for s in (-1,1):tube('Rock vest seam',[(s*.33,.27,1.90),(s*.31,.34,1.45),(s*.35,.30,.95)],.036,dark)
            motif('music',(0,.364,1.54),.12)
        elif variant==2:
            soft_cap((0,0,2.89),'Head',.40,.34)
            bow('Festival wooden bow',(0,.375,1.72),dark,light,'Body')
            motif('star',(0,.366,1.35),.12)
        elif variant==3:
            for s in (-1,1):
                tube('Root chest vine',[(s*.24,.30,1.88),(s*.31,.35,1.48),(s*.13,.36,.96)],.024,dark)
                leaf('Root crown leaf',(s*.23,-.02,2.84),(.17,.27,.03),energy,'Head',s*.7)
        else:
            motif('cog',(0,.37,1.38),.18)
            motif('cog',(.19,.337,1.67),.08)
            tube('Clockwork side lever',[(.40,.03,1.77),(.47,.08,1.85),(.47,.08,2.00)],.022,accent)
    elif hero==8:
        if variant==1:
            for s in (-1,1):blossom('Cactus trunk blossom',(s*.37,-.13,2.33),accent,light,'Body',.11)
            blossom('Elephant cap blossom',(0,-.02,2.875),accent,light,'Head',.12)
        elif variant==2:
            soft_cap((0,-.01,2.825),'Head',.27,.25)
            for s in (-1,1):box('Desert saddle pouch',(s*.45,-.16,2.00),(.14,.22,.28),dark,bevel=.045)
        elif variant==3:
            motif('moon',(0,.529,2.69),.07,'Head')
            for i in range(3):motif('star',(-.16+i*.16,-.411,2.40),.047)
        else:
            for s,label in [(-1,'L'),(1,'R')]:
                ring('Copper anklet bell',(s*.42,.06,.46),.041,.016,accent,'Leg_'+label,rotation=(pi/2,0,0))
                motif('cog',(s*.40,.25,.19),.045,'Leg_'+label)
            band('Copper elephant brow guard',2.835,.22,.23,.05,accent,'Head',(0,.15))
    elif hero==9:
        if variant==1:
            soft_cap((0,-.12,2.86),'Head',.30,.30)
            box('Postbird canvas satchel',(.29,-.20,1.17),(.23,.19,.26),main,bevel=.055)
            tube('Postbird shoulder strap',[(-.25,.20,1.86),(0,.35,1.54),(.29,.07,1.22)],.024,accent)
        elif variant==2:
            for s,label in [(-1,'L'),(1,'R')]:
                shoulder_crystals('Arm_'+label,s*.37,1.89)
            motif('music',(0,.384,1.68),.12)
        elif variant==3:
            for s in (-1,1):blossom('Surf floral collar',(s*.17,.27,1.89),accent,light,'Body',.11)
            motif('star',(0,.388,1.52),.13)
        else:
            soft_cap((0,-.15,2.86),'Head',.31,.30)
            band('Bird warm collar',1.94,.20,.22,.10,light)
            tube('Bird winter scarf',[(.15,.20,1.94),(.21,.32,1.72),(.18,.35,1.55)],.043,accent)
    elif hero==10:
        if variant==1:
            for s in (-1,1):leaf('Swamp visor leaf',(s*.33,.05,2.90),(.19,.32,.028),main,'Head',s*.9)
            tube('Wood tire protection arc',[(-.71,.259,1.58),(-.54,.259,1.98),(0,.259,2.17),(.54,.259,1.98),(.71,.259,1.58)],.025,dark)
        elif variant==2:
            motif('bolt',(.48,.265,1.95),.13)
            for s in (-1,1):ring('Neon rally tire lens',(s*.72,.26,1.48),.06,.012,energy,rotation=(pi/2,0,0))
            tube('Rally neon visor',[(-.39,.21,2.81),(0,.34,2.96),(.39,.21,2.81)],.018,energy,'Head')
        elif variant==3:
            for s,label in [(-1,'L'),(1,'R')]:band('Rover winter gaiter',.47,.11,.11,.11,light,'Leg_'+label,(s*.44,.07))
            motif('snow',(0,.43,2.75),.09,'Head')
        else:
            band('Desert frog cap band',2.858,.43,.18,.068,main,'Head',(0,.1))
            for s in (-1,1):box('Tire desert guard',(s*.75,.0,1.62),(.11,.32,.30),accent,bevel=.04)
    elif hero==11:
        if variant==1:
            for s in (-1,1):
                tube('Lava shell fissure',[(s*.16,-.25,2.13),(s*.26,-.43,1.82),(s*.16,-.55,1.70),(s*.25,-.73,1.40)],.022,energy)
            panel('Obsidian shrimp ridge',[(-.065,-.48,1.9),(.065,-.48,1.9),(.05,-.76,1.66),(0,-.64,2.11)],.025,dark)
        elif variant==2:
            motif('snow',(0,.37,1.88),.067,'Head')
            for s in (-1,1):leaf('Frost shell leaf',(s*.20,-.52,1.90),(.17,.32,.055),light,'Body',s*.75)
        elif variant==3:
            for x in (-.18,0,.18):
                tube('Royal cat crown spire',[(x,.02,2.87),(x,.02,3.12 if x==0 else 3.04)],.015,accent,'Head')
                ell('Royal crown gem',(x,.02,3.12 if x==0 else 3.04),(.036,.027,.043),energy,'Head',12,8)
            motif('star',(0,.36,1.89),.066,'Head')
        else:
            for s in (-1,1):
                ell('Cat compact space pack',(s*.33,-.29,1.98),(.10,.18,.12),light)
                ring('Space pack nozzle',(s*.33,-.46,1.98),.055,.019,energy,rotation=(pi/2,0,0))
            motif('star',(0,.37,1.89),.07,'Head')
    elif hero==12:
        if variant==1:
            for s in (-1,1):blossom('Summer admiral shoulder flower',(s*.45,.12,1.96),accent,light,'Arm_'+('L' if s<0 else 'R'),.09)
            tube('Sun cap stitched brim',[(-.29,.3,2.80),(0,.54,2.80),(.29,.3,2.80)],.017,accent,'Head')
        elif variant==2:
            for s,label in [(-1,'L'),(1,'R')]:band('Fisher padded cuff',1.14,.15,.13,.13,light,'Arm_'+label,(s*.63,.08))
            motif('snow',(0,.40,2.875),.061,'Head')
        elif variant==3:
            for s,label in [(-1,'L'),(1,'R')]:shoulder_crystals('Arm_'+label,s*.52,1.95)
            motif('star',(0,.40,2.868),.06,'Head')
        else:
            # Forward chef toque fits beside, rather than on top of, the fish dorsal fin.
            for obj in list(g.COSMETICS):
                if obj.name.startswith(('SKIN Admiral cap crown','SKIN Curved cap visor')):
                    g.COSMETICS.remove(obj);g.bpy.data.objects.remove(obj,do_unlink=True)
            for x in (-.17,0,.17):ell('Soft chef toque fold',(x,.15,2.97),(.13,.20,.14),light,'Head',18,12)
            bow('Gourmet collar bow',(0,.36,1.91),main,accent,'Body')
