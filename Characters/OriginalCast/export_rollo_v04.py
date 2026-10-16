"""Bake the detailed master into a bounded, rigged Unity character."""
import sys,json,math,hashlib
from pathlib import Path
import bpy
from mathutils import Vector
from math import sin,pi
sys.path.insert(0,str(Path(__file__).resolve().parent))
import sculpt_rollo_v04 as A
import geometry as G
import build as B

ROOT=Path(__file__).resolve().parents[2]
OUT=A.OUT
UNITY=ROOT/'Assets/Resources/OriginalCastDetail'
TEXTURES=UNITY/'RolloBulk_v04'


def triangles(obj):return sum(len(p.vertices)-2 for p in obj.data.polygons)
def verify_face_winding(mesh,repair=False):
    for i,material in enumerate(mesh.data.materials):
        if material.name.split()[1] not in ('mouth','tongue'):continue
        faces=[p for p in mesh.data.polygons if p.material_index==i]
        if repair and sum(p.normal.y for p in faces)<0:
            for face in faces:face.flip()
            mesh.data.update()
        assert sum(p.normal.y for p in faces)>0,'Back-facing facial cap: '+material.name


def save_mobile(mesh,rig):
    verify_face_winding(mesh)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'RolloBulk_v04_mobile.blend'))
    select([mesh,rig]+[o for o in bpy.context.scene.objects if o.type=='EMPTY'],rig)
    fbx=UNITY/'RolloBulk_v04.fbx'
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'MESH','ARMATURE','EMPTY'},bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_use_all_bones=True,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
    return fbx


def select(objects,active):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:obj.select_set(True)
    bpy.context.view_layer.objects.active=active


def combined_copy(sources,name,budget):
    copies=[]
    for source in sources:
        obj=source.copy();obj.data=source.data.copy();bpy.context.collection.objects.link(obj);copies.append(obj)
    select(copies,copies[0]);bpy.ops.object.join();obj=bpy.context.object;obj.name=name
    count=triangles(obj)
    if count>budget:
        modifier=obj.modifiers.new('Mobile silhouette topology','DECIMATE');modifier.ratio=budget/count
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    select([obj],obj);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.012,area_weight=.3)
    bpy.ops.object.mode_set(mode='OBJECT')
    return obj


def image(name,size,color=True):
    result=bpy.data.images.new(name,width=size,height=size,alpha=False)
    result.colorspace_settings.name='sRGB' if color else 'Non-Color'
    return result


def bake(sources,target,material,prefix,size):
    lowmat=material.copy();original_name=material.name;lowmat.name='Bake target '+original_name
    target.data.materials.clear();target.data.materials.append(lowmat)
    for face in target.data.polygons:face.material_index=0
    tex=lowmat.node_tree.nodes.new('ShaderNodeTexImage');lowmat.node_tree.nodes.active=tex
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=8
    scene.render.bake.use_selected_to_active=True;scene.render.bake.cage_extrusion=.065;scene.render.bake.max_ray_distance=.16
    scene.render.bake.margin=12;scene.render.bake.use_pass_direct=False;scene.render.bake.use_pass_indirect=False;scene.render.bake.use_pass_color=True
    maps={}
    for suffix,kind,color in [('Albedo','DIFFUSE',True),('Normal','NORMAL',False)]:
        img=image(prefix+'_'+suffix,size,color);tex.image=img;select(sources+[target],target)
        bpy.ops.object.bake(type=kind)
        img.filepath_raw=str(TEXTURES/(prefix+'_'+suffix+'.png'));img.file_format='PNG';img.save();img.pack();maps[suffix]=img
        print('ROLLO_MAP_BAKED '+prefix+'_'+suffix,flush=True)
    if prefix=='Body':
        bs=material.node_tree.nodes.get('Principled BSDF');output=next(n for n in material.node_tree.nodes if n.type=='OUTPUT_MATERIAL')
        belly=bs.inputs['Base Color'].links[0].from_node;mask=belly.inputs[0].links[0].from_socket
        invert=material.node_tree.nodes.new('ShaderNodeMath');invert.operation='SUBTRACT';invert.inputs[0].default_value=1;material.node_tree.links.new(mask,invert.inputs[1])
        emission=material.node_tree.nodes.new('ShaderNodeEmission');material.node_tree.links.new(invert.outputs[0],emission.inputs['Color']);material.node_tree.links.new(emission.outputs[0],output.inputs['Surface'])
        img=image('Body_TintMask',size,False);tex.image=img;select([o for o in sources if o.data.materials[0]==material]+[target],target)
        bpy.ops.object.bake(type='EMIT');img.filepath_raw=str(TEXTURES/'Body_TintMask.png');img.file_format='PNG';img.save();img.pack();maps['TintMask']=img
        material.node_tree.links.new(bs.outputs[0],output.inputs['Surface'])
    # The saved mobile .blend uses the same baked maps as the Unity import.
    nodes,links=lowmat.node_tree.nodes,lowmat.node_tree.links;nodes.clear()
    output=nodes.new('ShaderNodeOutputMaterial');bs=nodes.new('ShaderNodeBsdfPrincipled');links.new(bs.outputs[0],output.inputs[0])
    color=nodes.new('ShaderNodeTexImage');color.image=maps['Albedo'];links.new(color.outputs['Color'],bs.inputs['Base Color'])
    norm=nodes.new('ShaderNodeTexImage');norm.image=maps['Normal'];normal=nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.6
    links.new(norm.outputs['Color'],normal.inputs['Color']);links.new(normal.outputs[0],bs.inputs['Normal'])
    bs.inputs['Roughness'].default_value=.38 if prefix=='Body' else .64
    material.name='Master '+original_name;lowmat.name=original_name
    return lowmat


def weight(mesh,bone):
    mesh.vertex_groups.clear()
    if bone!='Skin':
        group=mesh.vertex_groups.new(name=bone);group.add(list(range(len(mesh.data.vertices))),1,'REPLACE');return
    groups={n:mesh.vertex_groups.new(name=n) for n in ('Body','Head','Arm_L','Arm_R','Tail')}
    for vertex in mesh.data.vertices:
        x,y,z=vertex.co
        head=max(0,min(1,(z-2.25)/.45))
        tail=max(0,min(1,(-y-.83)/.43))*(1-head)
        arm=max(0,min(1,(abs(x)-1.04)/.40))*(1-head)*(1-tail)
        values={'Head':head,'Tail':tail,'Arm_L' if x<0 else 'Arm_R':arm,'Body':max(0,1-head-tail-arm)}
        total=sum(values.values())
        for name,value in values.items():
            if value>.0001:groups[name].add([vertex.index],value/total,'REPLACE')


def skin_catalog(mesh):
    current=ROOT/'Content/OriginalCast/Skins/catalog-original26-v1.json'
    manifest=json.loads(current.read_text(encoding='utf-8'))
    manifest['revision']=4;manifest['skins']=[e for e in manifest['skins'] if e['hero']!=0]
    # A clean point on the new belly in normalized authoring space.
    body_index=mesh.vertex_groups['Body'].index
    candidates=[v for v in mesh.data.vertices if .95<v.co.z<1.5 and abs(v.co.x-.42)<.16 and v.normal.y>.5 and any(g.group==body_index and g.weight>.6 for g in v.groups)]
    mount=max(candidates,key=lambda v:v.co.y).co+Vector((0,.025,0))
    for slug,title,main,accent,cream in B.THEMES:
        identifier='orig_rollobulk_'+slug;bindings=[]
        for material in mesh.data.materials:
            tokens=material.name.split();role=tokens[1];base=tokens[2]
            color=main if role=='main' else accent if role=='accent' else base
            smoothness=.70 if role in ('eye','iris') else .56 if role=='nose' else .22 if role=='accent' else .35
            bindings.append({'material':material.name,'color':color,'metallic':0,'smoothness':smoothness,'glow':slug=='neon' and role=='accent'})
        pack={'schema':2,'hero':0,'id':identifier,'bindings':bindings,'meshes':[B.badge_mesh(mount,slug,accent)]}
        raw=json.dumps(pack,ensure_ascii=False,separators=(',',':')).encode();digest=hashlib.sha256(raw).hexdigest();filename=identifier+'-'+digest+'.json'
        (current.parent/filename).write_bytes(raw)
        manifest['skins'].append({'id':identifier,'title':title+' ОБРАЗ','titleEn':slug.title()+' Edition','description':'Ролло Бульк · детальная модель, новая палитра и эмблема · без усилений','hero':0,'file':filename,'sha256':digest,'bytes':len(raw),'swatch':accent})
    manifest['skins'].sort(key=lambda e:e['hero'])
    (current.parent/'catalog-original26-v3.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    return manifest


def main():
    OUT.mkdir(parents=True,exist_ok=True);TEXTURES.mkdir(parents=True,exist_ok=True);body=A.make()
    sources={bone:list(items) for bone,items in G.D.PARTS.items()}
    drum=next(o for parts in sources.values() for o in parts if o.name.startswith('Porous orange'))
    assert max(abs(v.co.x) for v in drum.data.vertices)<1.131,'Cubic roller overshoot returned'
    assert max(v.co.x for v in body.data.vertices)>1.80,'Heavy flipper silhouette lost'
    high=[o for items in sources.values() for o in items];master_triangles=sum(triangles(o) for o in high)
    skin=body.data.materials[0];foam=next(o.data.materials[0] for o in high if o.name.startswith('Porous orange'))
    skin_sources=[o for o in high if o.data.materials[0]==skin]
    pores=[o for o in high if o.name.startswith('Painted flush whisker pore')]
    foam_sources=[o for o in high if o.data.materials[0]==foam]
    lowbody=combined_copy(skin_sources,'Rollo mobile skin',26000);bake(skin_sources+pores,lowbody,skin,'Body',2048);weight(lowbody,'Skin')
    lowfoam=combined_copy(foam_sources,'Rollo mobile foam roller',5500);bake(foam_sources,lowfoam,foam,'Roller',1024);weight(lowfoam,'Appendage_0')
    lows=[lowbody,lowfoam]
    consumed=set(skin_sources+pores+foam_sources)
    others=[(bone,obj) for bone,items in sources.items() for obj in items if obj not in consumed]
    ratio=min(1,11000/max(1,sum(triangles(o) for bone,o in others)))
    for bone,obj in others:
        low=obj.copy();low.data=obj.data.copy();bpy.context.collection.objects.link(low);select([low],low)
        if triangles(low)>100:
            modifier=low.modifiers.new('Mobile facial topology','DECIMATE');modifier.ratio=ratio;bpy.ops.object.modifier_apply(modifier=modifier.name)
        # Projected facial patches share the skin's deformation field. Rigid
        # Head weights let the tongue/philtrum sink below the blended lower jaw
        # even during Idle, despite a correct authoring-space render.
        weight(low,'Skin' if bone=='Head' else bone);lows.append(low)
    select(lows,lows[0]);bpy.ops.object.join();mesh=bpy.context.object;mesh.name='RolloBulk_DetailedMobile'
    for obj in high:bpy.data.objects.remove(obj,do_unlink=True)
    select([mesh],mesh);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    low=min(v.co.z for v in mesh.data.vertices);highz=max(v.co.z for v in mesh.data.vertices);scale=3/(highz-low)
    for vertex in mesh.data.vertices:vertex.co=Vector((vertex.co.x*scale,vertex.co.y*scale,(vertex.co.z-low)*scale))
    pivots={name:(p[0]*scale,p[1]*scale,(p[2]-low)*scale) for name,p in G.BONES.items()}
    rig=B.rig_mesh(mesh,pivots);B.animate(rig,0)
    # The drum rotates around its own axle; the tail no longer rotates as a wheel.
    action=bpy.data.actions['Run'];rig.animation_data.action=action
    for frame in sorted(set(range(1,25,2))|{24}):
        bpy.context.scene.frame_set(frame);phase=(frame-1)/23*2*pi
        rig.pose.bones['Tail'].rotation_euler=(.035*sin(phase),0,.08*sin(phase))
        rig.pose.bones['Appendage_0'].rotation_euler=(phase,0,0)
        rig.pose.bones['Tail'].keyframe_insert('rotation_euler',frame=frame);rig.pose.bones['Appendage_0'].keyframe_insert('rotation_euler',frame=frame)
    rig.animation_data.action=bpy.data.actions['Idle'];bpy.context.scene.frame_set(1)
    manifest=skin_catalog(mesh)
    fbx=save_mobile(mesh,rig)
    report={'status':'baked-export-awaiting-Unity-QA','masterTriangles':master_triangles,'mobileTriangles':triangles(mesh),'vertices':len(mesh.data.vertices),'materials':len(mesh.data.materials),'bones':list(pivots),'animations':list(B.ACTIONS),'textures':{'body':2048,'roller':1024},'fbxBytes':fbx.stat().st_size,'skinCatalogRevision':manifest['revision']}
    report['facialCapsFacingOutward']=True
    assert report['mobileTriangles']<=45000,report
    (OUT/'mobile-report.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print('ROLLO_MOBILE_EXPORT_PASS '+json.dumps(report),flush=True)


if __name__=='__main__':
    if '--repair-face-winding' in sys.argv:
        # Resume from the complete verified bake; normals/winding corrections
        # do not require baking the unchanged body and drum all over again.
        bpy.ops.wm.open_mainfile(filepath=str(OUT/'RolloBulk_v04_mobile.blend'))
        mesh=bpy.data.objects['RolloBulk_DetailedMobile'];verify_face_winding(mesh,repair=True)
        rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
        fbx=save_mobile(mesh,rig)
        path=OUT/'mobile-report.json';report=json.loads(path.read_text());report['fbxBytes']=fbx.stat().st_size
        report['facialCapsFacingOutward']=True;path.write_text(json.dumps(report,indent=2)+'\n')
        print('ROLLO_FACE_WINDING_PASS mouth=true tongue=true',flush=True)
    elif '--catalog-only' in sys.argv:
        bpy.ops.wm.open_mainfile(filepath=str(OUT/'RolloBulk_v04_mobile.blend'))
        skin_catalog(bpy.data.objects['RolloBulk_DetailedMobile']);print('ROLLO_SKIN_CATALOG_PASS',flush=True)
    else:main()
