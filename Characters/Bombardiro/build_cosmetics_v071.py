"""Blender-authored cosmetic meshes. Data-only packets, no runtime Python/code.
Run: blender --background --python Characters/Bombardiro/build_cosmetics_v071.py
Assets stay outside Resources: the installed app must actually download them.
"""
import bpy
import math
import json
import hashlib
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Content'/'Skins'
OUT.mkdir(parents=True,exist_ok=True)

def material(name,color,glow=False):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1)
    m['hex']='#'+''.join(f'{round(c*255):02x}' for c in color);m['glow']=glow
    return m

def finish(obj,name,mat):
    obj.name=name;obj.data.materials.append(mat)
    for face in obj.data.polygons:face.use_smooth=True
    return obj

def curve(name,points,radius,mat):
    data=bpy.data.curves.new(name,'CURVE');data.dimensions='3D';data.bevel_depth=radius;data.bevel_resolution=2
    spline=data.splines.new('BEZIER');spline.bezier_points.add(len(points)-1)
    for p,co in zip(spline.bezier_points,points):p.co=co;p.handle_left_type='AUTO';p.handle_right_type='AUTO'
    ob=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(ob);ob.data.materials.append(mat)
    return ob

def plate(name,points,depth,mat):
    verts=points+[(x,y,z-depth) for x,y,z in points];n=len(points)
    faces=[tuple(range(n)),tuple(range(2*n-1,n-1,-1))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);data.update()
    ob=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(ob)
    finish(ob,name,mat);bevel=ob.modifiers.new('Rounded machined edge','BEVEL');bevel.width=.025;bevel.segments=2
    return ob

def ring(name,location,major,minor,mat):
    bpy.ops.mesh.primitive_torus_add(major_radius=major,minor_radius=minor,major_segments=24,minor_segments=6,location=location,rotation=(math.pi/2,0,0))
    return finish(bpy.context.object,name,mat)

def export_pack(identity,title,description,palette,lava=False):
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    metal=material('Armor',(.12,.17,.23) if lava else (.88,.94,.98))
    accent=material('Accent',(1,.27,.035) if lava else (1,.48,.07),True)
    glow=material('Energy',(1,.57,.02) if lava else (.02,.72,1),True)
    dark=material('Recess',(.03,.06,.10))
    for side in (-1,1):
        x=side*1.48
        for y in (-.45,-.58):ring('Reactor collar',(x,y,1.52),.34,.065,metal)
        ring('Reactor luminous annulus',(x,-.63,1.52),.27,.045,glow)
        # An authored curved saddle bridges into each existing wing, not a new collision body.
        plate('Wing exoshell',[(side*.72,-.56,1.83),(side*2.68,-.84,1.83),(side*2.65,-1.13,1.83),(side*.73,-.98,1.83)],.09,metal)
        if not lava:
            plate('Inset solar array',[(side*1.8,-.77,1.87),(side*2.55,-.88,1.87),(side*2.53,-1.07,1.87),(side*1.8,-.97,1.87)],.015,dark)
            for i in range(6):
                xx=side*(1.84+i*.13)
                curve('Solar cell conductor',[(xx,-.81-(i*.016),1.887),(xx,-.965-i*.016,1.887)],.007,glow)
            curve('Rescue stripe',[(side*.76,-.61,1.9),(side*1.01,-.63,1.9),(side*1.26,-.67,1.9)],.06,accent)
        else:
            for i in range(4):
                xx=side*(.86+i*.44)
                curve('Molten branching seam',[(xx,-.60-i*.055,1.895),(xx+side*.16,-.79-i*.04,1.90),(xx+side*.02,-1.02-i*.025,1.895)],.025,glow)
        curve('Power feed',[(side*.32,-.43,2.14),(side*.67,-.57,1.9),(side*1.35,-.59,1.87)],.04,accent)
    if lava:
        for i in range(4):
            y=-.36-i*.4;z=2.2-i*.08
            plate('Obsidian dorsal blade',[(-.08,y,z),(.08,y,z),(.065,y-.23,z+.39),(-.065,y-.23,z+.39)],.08,metal)
            curve('Dorsal molten edge',[(0,y,z+.04),(0,y-.22,z+.39)],.025,accent)
    else:
        curve('Rescue roll hoop',[(-.40,-.47,2.05),(-.32,-.48,2.38),(.32,-.48,2.38),(.40,-.47,2.05)],.045,metal)
        ring('Navigation beacon',(0,-.46,2.40),.10,.04,glow)
    # Bake Blender modifiers and combine per material: at most four draw calls.
    deps=bpy.context.evaluated_depsgraph_get();groups={}
    for ob in list(bpy.context.scene.objects):
        if ob.type not in {'MESH','CURVE'}:continue
        ev=ob.evaluated_get(deps);mesh=ev.to_mesh();mesh.calc_loop_triangles()
        mat=ob.data.materials[0];key=mat.name
        part=groups.setdefault(key,dict(name=key,color=mat['hex'],glow=bool(mat['glow']),vertices=[],triangles=[]))
        offset=len(part['vertices'])//3
        for v in mesh.vertices:
            p=ob.matrix_world@v.co
            part['vertices'].extend([round(p.x,5),round(p.z,5),round(-p.y,5)])
        for triangle in mesh.loop_triangles:part['triangles'].extend(offset+i for i in triangle.vertices)
        ev.to_mesh_clear()
    pack=dict(schema=1,hero=1,id=identity,**palette,meshes=list(groups.values()))
    data=json.dumps(pack,separators=(',',':')).encode();digest=hashlib.sha256(data).hexdigest()
    filename=f'{identity}-{digest}.json';(OUT/filename).write_bytes(data)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/f'{identity}.blend'))
    print('COSMETIC_EXPORT',identity,'vertices',sum(len(p['vertices'])//3 for p in groups.values()),'bytes',len(data))
    return dict(id=identity,hero=1,title=title,description=description,file=filename,sha256=digest,bytes=len(data))

orbital=export_pack('bombardiro_orbital','ОРБИТАЛЬНЫЙ СПАСАТЕЛЬ','Солнечные панели · голубые реакторы',dict(hull='#e0edf7',trim='#16304d',accent='#ff902b',skin='#bbebdb'))
lava=export_pack('bombardiro_lava','ЛАВОВЫЙ ПЕРЕХВАТЧИК','Обсидиановая броня · огненные швы',dict(hull='#263344',trim='#101925',accent='#ff6e29',skin='#a7997b'),True)
for revision,entries in [(1,[orbital]),(2,[orbital,lava])]:
    (OUT/f'catalog-v{revision}.json').write_text(json.dumps(dict(schema=1,revision=revision,skins=entries),ensure_ascii=False,indent=2),encoding='utf-8',newline='\n')
