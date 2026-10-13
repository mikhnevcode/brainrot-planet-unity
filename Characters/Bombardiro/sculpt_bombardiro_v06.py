"""Continuous crocodile head, sculpt source, game meshes and shared skin maps.

Run with Blender 5.2 --background --python this_file.py. Previous versions stay
intact. Small hide detail is generated on the surface and in tangent-space maps;
the mobile mesh keeps the authored skeleton and eight action clips.
"""
import bpy
import bmesh
import json
import math
import os
import numpy as np
from mathutils import Vector

ROOT = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(ROOT, 'V06')
os.makedirs(OUT, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT, 'Bombardiro_v04.blend'))
rig = bpy.data.objects['Bombardiro_Armature']
rig.animation_data.action = bpy.data.actions['Idle']
bpy.context.scene.frame_set(1)

def material(fragment):
    return next(m for m in bpy.data.materials if fragment.lower() in m.name.lower())

GREEN = material('crocodile dark')
SNOUT = material('crocodile nose')
JAW = material('underjaw')
MOUTH = material('inner mouth')
TEETH = material('ivory teeth')
IRIS = material('yellow reptile')
PUPIL = material('black slit')
CREASE = material('scale crevice')
METAL = material('metal dark')
TRIM = material('silver edge')
croc_materials = {GREEN, SNOUT, JAW, MOUTH, TEETH, IRIS, PUPIL, CREASE}

# Delete the old head, every sphere scute, teeth and facial overlays by material.
# Aircraft mesh, engine/propeller sections and authored bone transforms survive.
for obj in list(bpy.data.objects):
    if obj.type != 'MESH': continue
    bm = bmesh.new(); bm.from_mesh(obj.data)
    deleted = [v for v in bm.verts if v.link_faces and
               all(obj.data.materials[f.material_index] in croc_materials for f in v.link_faces)]
    bmesh.ops.delete(bm, geom=deleted, context='VERTS')
    bm.to_mesh(obj.data); bm.free(); obj.data.update()

# Repair aircraft winding component by component. In v04 the 4,144-vertex
# fuselage had negative signed volume (-1.99), with 224 open boundary edges.
normal_repairs = []
for obj in [o for o in bpy.data.objects if o.type == 'MESH']:
    bm = bmesh.new(); bm.from_mesh(obj.data)
    unseen = set(bm.verts)
    while unseen:
        todo = [unseen.pop()]; component = set(todo)
        while todo:
            vertex = todo.pop()
            for edge in vertex.link_edges:
                other = edge.other_vert(vertex)
                if other in unseen:
                    unseen.remove(other); component.add(other); todo.append(other)
        faces = list({f for v in component for f in v.link_faces})
        if not faces: continue
        is_hull = len(component) > 3000 and all(
            'brushed silver' in obj.data.materials[f.material_index].name.lower() for f in faces)
        boundary = list({e for v in component for e in v.link_edges if e.is_boundary})
        if is_hull and boundary:
            filled = bmesh.ops.holes_fill(bm, edges=boundary, sides=0)['faces']
            for f in filled: f.material_index = faces[0].material_index
            faces += list(filled)
        bmesh.ops.recalc_face_normals(bm, faces=faces)
        volume = 0.0
        for f in faces:
            pts = [v.co for v in f.verts]
            for i in range(1, len(pts)-1):
                volume += pts[0].dot(pts[i].cross(pts[i+1])) / 6.0
        if volume < 0:
            for f in faces: f.normal_flip()
            volume = -volume
        if is_hull:
            normal_repairs.append(dict(mesh=obj.name, vertices=len(component),
                boundary_before=len(boundary), boundary_after=sum(e.is_boundary for e in boundary),
                outward_signed_volume=round(volume, 6)))
    bm.to_mesh(obj.data); bm.free(); obj.data.update()

# Irregular cellular hide: fine incised seams, flat scale centres and subdued
# mottled colour. All maps share the explicit loft UVs of upper/lower head.
SIZE = 1024
rng = np.random.default_rng(73061)
GRID_X, GRID_Y = 42, 25
jitter = rng.uniform(-.32, .32, (GRID_Y, GRID_X, 2)).astype(np.float32)
cell_tone = rng.uniform(-.035, .035, (GRID_Y, GRID_X)).astype(np.float32)

def hide(u, v):
    gx, gy = u * GRID_X, v * GRID_Y
    ix, iy = np.floor(gx).astype(int), np.floor(gy).astype(int)
    nearest = np.full_like(gx, 100.0); second = np.full_like(gx, 100.0)
    tone = np.zeros_like(gx)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            cx, cy = ix + dx, iy + dy
            jx = jitter[cy % GRID_Y, cx % GRID_X, 0]
            jy = jitter[cy % GRID_Y, cx % GRID_X, 1]
            distance = (gx-cx-.5-jx)**2 + (gy-cy-.5-jy)**2
            closer = distance < nearest
            second = np.where(closer, nearest, np.minimum(second, distance))
            nearest = np.minimum(nearest, distance)
            tone = np.where(closer, cell_tone[cy % GRID_Y, cx % GRID_X], tone)
    edge_distance = np.sqrt(second) - np.sqrt(nearest)
    seam = np.exp(-(edge_distance/.057)**2)
    grain = .035 * np.sin(u*1731 + np.sin(v*449)) * np.sin(v*1413)
    height = .62 - seam*.48 + grain
    return height, seam, tone

uv = np.arange(SIZE, dtype=np.float32) / SIZE
u, v = np.meshgrid(uv, uv)
height, seam, tone = hide(u, v)
mottle = .025*np.sin(u*37+np.sin(v*19))*np.sin(v*51)
dorsal = np.exp(-((u-.5)/.27)**4)
base = np.stack((.235 + tone*.7 + mottle,
                 .315 + tone + mottle,
                 .215 + tone*.6 + mottle*.7), axis=-1)
base *= (1 - seam[..., None]*.28)
base *= (1 - dorsal[..., None]*.13)
albedo = np.ones((SIZE, SIZE, 4), np.float32)
albedo[:, :, :3] = np.clip(base, .025, 1)
# Derivatives in the same UV coordinate system used by the low mesh.
du = (np.roll(height,-1,axis=1)-np.roll(height,1,axis=1))*.55
dv = (np.roll(height,-1,axis=0)-np.roll(height,1,axis=0))*.55
normal = np.stack((-du,-dv,np.ones_like(du)), axis=-1)
normal /= np.linalg.norm(normal,axis=-1)[...,None]
normal_pixels = np.ones((SIZE, SIZE, 4), np.float32)
normal_pixels[:,:,:3] = normal*.5+.5

def save_map(name, pixels, colorspace):
    image = bpy.data.images.new(name, width=SIZE, height=SIZE, alpha=False)
    image.colorspace_settings.name = colorspace
    image.pixels.foreach_set(pixels.ravel())
    image.filepath_raw = os.path.join(OUT, name+'.png')
    image.file_format = 'PNG'; image.save(); image.pack()
    return image

albedo_map = save_map('Bombardiro_v06_Skin_Albedo', albedo, 'sRGB')
normal_map = save_map('Bombardiro_v06_Skin_Normal', normal_pixels, 'Non-Color')
skin = bpy.data.materials.new('Crocodile v06 sculpted textured skin')
skin.use_nodes = True
nodes, links = skin.node_tree.nodes, skin.node_tree.links
bsdf = nodes.get('Principled BSDF')
bsdf.inputs['Roughness'].default_value = .72
tex = nodes.new('ShaderNodeTexImage'); tex.image = albedo_map
links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])
nt = nodes.new('ShaderNodeTexImage'); nt.image = normal_map
nm = nodes.new('ShaderNodeNormalMap'); nm.inputs['Strength'].default_value = .9
links.new(nt.outputs['Color'],nm.inputs['Color'])
links.new(nm.outputs['Normal'],bsdf.inputs['Normal'])
skin.diffuse_color = (.24,.32,.22,1)

# Long organic profile, sampled with smooth cubic interpolation. Width, top,
# bottom and exponent define rounded U-shaped cross-sections, with no boxes.
UPPER = [
    (.64,.34,1.85,1.20,2.0), (.85,.48,1.92,1.24,2.1),
    (1.08,.56,1.95,1.35,2.15), (1.34,.575,1.945,1.45,2.15),
    (1.58,.54,1.875,1.49,2.3), (1.84,.45,1.79,1.52,2.65),
    (2.12,.40,1.735,1.52,2.9), (2.45,.375,1.705,1.52,3.0),
    (2.76,.365,1.705,1.52,2.85), (3.01,.34,1.745,1.51,2.6),
    (3.20,.265,1.73,1.53,2.1), (3.28,.025,1.63,1.59,2.0),
]
LOWER = [
    (.94,.23,1.39,1.18,2.0), (1.20,.43,1.35,1.13,2.2),
    (1.48,.46,1.32,1.12,2.4), (1.85,.41,1.30,1.115,2.7),
    (2.25,.36,1.295,1.115,2.9), (2.60,.345,1.30,1.13,2.8),
    (2.94,.32,1.33,1.17,2.6), (3.16,.25,1.37,1.22,2.2),
    (3.24,.025,1.32,1.28,2.0),
]

def profile(points, y):
    ys = [p[0] for p in points]
    index = max(0, min(len(points)-2, int(np.searchsorted(ys,y)-1)))
    p0,p1 = points[index],points[index+1]
    t = (y-p0[0])/(p1[0]-p0[0])
    t = max(0,min(1,t))
    prev,nxt = points[max(0,index-1)],points[min(len(points)-1,index+2)]
    vals=[]
    for k in range(1,5):
        m0=(p1[k]-prev[k])/(p1[0]-prev[0])*(p1[0]-p0[0])
        m1=(nxt[k]-p0[k])/(nxt[0]-p0[0])*(p1[0]-p0[0])
        vals.append((2*t**3-3*t*t+1)*p0[k]+(t**3-2*t*t+t)*m0+
                    (-2*t**3+3*t*t)*p1[k]+(t**3-t*t)*m1)
    return vals

def mesh_object(name, vertices, faces, mat):
    data=bpy.data.meshes.new(name+' mesh'); data.from_pydata(vertices,[],faces); data.update()
    obj=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(obj)
    data.materials.append(mat)
    bm=bmesh.new(); bm.from_mesh(data); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bm.to_mesh(data); bm.free()
    for poly in data.polygons: poly.use_smooth=True
    return obj

def loft(name, points, rows, around, section, sculpt=False):
    verts=[]; faces=[]; vertex_uv=[]
    for row in range(rows):
        v0=row/(rows-1); y=points[0][0]+v0*(points[-1][0]-points[0][0])
        width,top,bottom,exponent=profile(points,y)
        centre=(top+bottom)*.5; half=(top-bottom)*.5
        for col in range(around):
            u0=col/around; theta=u0*2*math.pi
            ss,cc=math.sin(theta),-math.cos(theta)
            x=width*math.copysign(abs(ss)**(2/exponent),ss)
            z=centre+half*math.copysign(abs(cc)**(2/exponent),cc)
            if section=='Body':
                # Brow, cheek muscles, lip rolls and nasal channels sculpted
                # directly into a single continuous surface.
                brow=math.exp(-((y-1.36)/.24)**2)*math.exp(-((abs(u0-.5)-.13)/.065)**2)
                z+=.17*brow
                cheek=math.exp(-((y-1.33)/.32)**2)*math.exp(-((abs(u0-.5)-.28)/.065)**2)
                x*=1+.09*cheek
                nasal=-.018*math.exp(-((y-2.30)/.64)**2)*math.exp(-((abs(x)-.14)/.06)**2)*max(0,cc)**2
                z+=nasal
                lip=.015*math.sin(y*17)*math.exp(-((cc+.6)/.19)**2)
                x+=math.copysign(lip*abs(ss),ss)
            h=height[min(SIZE-1,int(v0*(SIZE-1))),int(u0*SIZE)%SIZE]-.55
            amount=.016 if sculpt else .0035
            x+=ss*h*amount; z+=cc*h*amount
            # Larger posterior dorsal scutes integrated as shallow ridges.
            if section=='Body':
                dorsal=max(0,cc)**8*math.exp(-((y-1.05)/.39)**2)
                z+=dorsal*.022*(.5+.5*math.cos(y*38))
            verts.append((x,y,z)); vertex_uv.append((u0,v0))
    for row in range(rows-1):
        for col in range(around):
            n=(col+1)%around
            faces.append((row*around+col,(row+1)*around+col,(row+1)*around+n,row*around+n))
    faces.append(tuple(reversed(range(around))))
    faces.append(tuple((rows-1)*around+c for c in range(around)))
    obj=mesh_object(name,verts,faces,skin)
    uv_layer=obj.data.uv_layers.new(name='UVMap')
    for poly in obj.data.polygons:
        wrap=any(vertex_uv[obj.data.loops[l].vertex_index][0]>.9 for l in poly.loop_indices)
        for loop_index in poly.loop_indices:
            uu,vv=vertex_uv[obj.data.loops[loop_index].vertex_index]
            if wrap and uu==0: uu=1
            uv_layer.data[loop_index].uv=(uu,vv)
    obj['bone_section']=section
    return obj

upper=loft('V06 continuous sculpted crocodile skull',UPPER,96,64,'Body')
lower=loft('V06 continuous sculpted crocodile mandible',LOWER,72,48,'Jaw')
details={'Body':[upper],'Jaw':[lower]}

def sphere(name, loc, scale, mat, section='Body', segments=20, rings=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=loc)
    obj=bpy.context.object; obj.name=name; obj.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    obj.data.materials.append(mat)
    for p in obj.data.polygons:p.use_smooth=True
    details[section].append(obj); return obj

def tube(name, points, radius, mat, section='Body'):
    data=bpy.data.curves.new(name,'CURVE'); data.dimensions='3D'; data.bevel_depth=radius
    data.bevel_resolution=2; data.use_fill_caps=True
    spline=data.splines.new('BEZIER'); spline.bezier_points.add(len(points)-1)
    for p,co in zip(spline.bezier_points,points):
        p.co=co; p.handle_left_type='AUTO'; p.handle_right_type='AUTO'
    obj=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(obj); data.materials.append(mat)
    bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True); bpy.context.view_layer.objects.active=obj
    bpy.ops.object.convert(target='MESH'); details[section].append(obj); return obj

for side in (-1,1):
    # Small amber eyes sit behind the brow overhang, never a large sphere on top.
    sphere('V06 recessed eye socket '+str(side),(side*.524,1.40,1.952),(.030,.102,.067),CREASE)
    sphere('V06 reptile iris '+str(side),(side*.548,1.427,1.961),(.018,.065,.043),IRIS)
    sphere('V06 vertical slit '+str(side),(side*.563,1.438,1.964),(.006,.026,.030),PUPIL,segments=12,rings=8)
    sphere('V06 sunken nostril '+str(side),(side*.18,3.045,1.744),(.050,.070,.006),CREASE)
    for section,offset in [('Body',0),('Jaw',.077)]:
        for i in range(12):
            y=1.63+i*.127+offset
            w,top,bottom,exp=profile(UPPER if section=='Body' else LOWER,y)
            x=side*w*.88
            root_z=bottom+.013 if section=='Body' else top-.010
            length=(.21 if i in (1,4,8) else .135)*(1-.18*i/11)
            direction=-1 if section=='Body' else 1
            # Curved conical teeth, slightly staggered between upper/lower jaw.
            verts=[]; faces=[]; count=10
            for ring in range(5):
                t=ring/4; radius=(.042 if i in (1,4,8) else .029)*(1-t)**1.15+.001
                for k in range(count):
                    angle=k/count*math.tau
                    verts.append((x+radius*math.cos(angle)-side*.018*t*t,
                                  y+radius*math.sin(angle)-.018*t*t,root_z+direction*length*t))
            for ring in range(4):
                for k in range(count):
                    n=(k+1)%count; faces.append((ring*count+k,ring*count+n,(ring+1)*count+n,(ring+1)*count+k))
            faces += [tuple(reversed(range(count))),tuple(4*count+k for k in range(count))]
            details[section].append(mesh_object('V06 curved tooth %s_%s_%s'%(section,side,i),verts,faces,TEETH))
    # Neck folds join the organic head to the aircraft body.
    for i in range(3):
        y=.75+i*.095
        tube('V06 neck skin fold %s_%s'%(side,i),[(side*.30,y,1.82),(side*.44,y+.035,1.68),
            (side*.39,y+.03,1.43)],.006,CREASE)

sphere('V06 mouth floor',(0,2.18,1.295),(.31,.77,.018),MOUTH,'Jaw')
sphere('V06 tongue',(0,2.02,1.32),(.22,.49,.027),MOUTH,'Jaw')

# Store a dense continuous sculpt surface for further Blender Sculpt Mode work.
# It is not exported to FBX, so Android never pays its polygon cost.
source_collection=bpy.data.collections.new('V06 HIGH DETAIL SCULPT SOURCE - not exported')
bpy.context.scene.collection.children.link(source_collection)
sculpt_sources=[]
for name,pts,section in [('V06 high detail skull',UPPER,'Body'),('V06 high detail jaw',LOWER,'Jaw')]:
    obj=loft(name,pts,288,256,section,True)
    for collection in list(obj.users_collection): collection.objects.unlink(obj)
    source_collection.objects.link(obj); obj.hide_render=True; obj.hide_set(True)
    obj['usage']='Unhide in Blender for Sculpt Mode editing; excluded from runtime export'
    sculpt_sources.append(obj)

# Join facial detail into the original rigidly skinned Body and Jaw sections.
for section,objects in details.items():
    target=bpy.data.objects['Combined_'+section]
    for obj in objects:
        group=obj.vertex_groups.new(name=section); group.add(list(range(len(obj.data.vertices))),1,'REPLACE')
        obj.parent=rig
    bpy.ops.object.select_all(action='DESELECT'); target.select_set(True)
    for obj in objects: obj.select_set(True)
    bpy.context.view_layer.objects.active=target; bpy.ops.object.join()
    target.name='Combined_'+section

runtime=[o for o in bpy.data.objects if o.type=='MESH' and o not in sculpt_sources]
blend_path=os.path.join(ROOT,'Bombardiro_v06.blend')
fbx_path=os.path.join(ROOT,'Bombardiro_v06.fbx')
bpy.ops.wm.save_as_mainfile(filepath=blend_path)
bpy.ops.object.select_all(action='DESELECT')
for obj in runtime:obj.select_set(True)
rig.select_set(True); bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=fbx_path,use_selection=True,object_types={'MESH','ARMATURE'},
    bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_all_bones=True,
    bake_anim_step=1,bake_anim_use_nla_strips=False,add_leaf_bones=False,
    apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',path_mode='STRIP')

# Reproducible studio views of the actual game-quality mesh with its skin maps.
previews=[]
def aim(obj,target):obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
def area(name,loc,energy,size,color):
    data=bpy.data.lights.new(name,'AREA'); data.energy=energy; data.shape='DISK'; data.size=size; data.color=color
    obj=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(obj); obj.location=loc
    aim(obj,(0,.3,1.6)); previews.append(obj)
area('V06 key',(3.8,5.2,6.2),1050,5,(1,.84,.69))
area('V06 fill',(-4,2,3.8),720,4,(.48,.68,1))
area('V06 rim',(2.5,-4.5,5.2),900,4,(.75,.90,1))
bpy.ops.mesh.primitive_plane_add(size=24,location=(0,0,.36)); floor=bpy.context.object
floor.data.materials.append(METAL); previews.append(floor)
cd=bpy.data.cameras.new('V06 studio'); cam=bpy.data.objects.new('V06 studio',cd)
bpy.context.collection.objects.link(cam); previews.append(cam); cd.type='ORTHO'
scene=bpy.context.scene; scene.camera=cam; scene.render.engine='BLENDER_EEVEE'
scene.render.resolution_x=1200; scene.render.resolution_y=900; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'; scene.render.film_transparent=False
scene.world.color=(.018,.025,.045); scene.view_settings.view_transform='AgX'
scene.view_settings.look='AgX - Medium High Contrast'
for name,loc,target,scale in [
    ('front_3q',(4.9,6.6,4.1),(0,.25,1.55),7.1),
    ('side',(7.3,.25,3.1),(0,.15,1.55),6.8),
    ('detail',(3.3,6,3.0),(0,1.95,1.63),3.6),
    ('rear',(-4,-6,3.8),(0,0,1.55),7.0)]:
    cam.location=loc; cd.ortho_scale=scale; aim(cam,target)
    scene.render.filepath=os.path.join(OUT,'Bombardiro_v06_'+name+'.png'); bpy.ops.render.render(write_still=True)
bpy.ops.object.select_all(action='DESELECT')
for obj in previews:obj.select_set(True)
bpy.ops.object.delete(use_global=False)
rig.animation_data.action=bpy.data.actions['Idle']; scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=blend_path)
report=dict(model='Bombardiro v06 continuous organic sculpt',source='Bombardiro_v04.blend',
    blender=bpy.app.version_string,normal_repairs=normal_repairs,
    runtime_sections={obj.name:len(obj.data.vertices) for obj in runtime},
    runtime_triangles=sum(sum(len(p.vertices)-2 for p in obj.data.polygons) for obj in runtime),
    sculpt_source_vertices=sum(len(obj.data.vertices) for obj in sculpt_sources),
    texture_size=SIZE,bones=[b.name for b in rig.data.bones],
    actions={a.name:list(a.frame_range) for a in bpy.data.actions})
with open(os.path.join(OUT,'Bombardiro_v06_report.json'),'w',encoding='utf-8') as f:json.dump(report,f,indent=2)
print('BOMBARDIRO_V06_READY',json.dumps(report))
