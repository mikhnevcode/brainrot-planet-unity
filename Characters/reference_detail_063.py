"""Reference-driven character meshes; reusable Blender geometry and skin maps.

Meshes are rebuilt on the existing skeleton/action sources. Organic primary
forms are continuous lofts. Export merges by moving bone, not by tiny detail.
Run refine_references_063.py with Blender, which imports this helper.
"""
import bpy, bmesh, math, os, json
import numpy as np
from mathutils import Vector
from math import sin, cos, pi
ROOT=os.path.dirname(os.path.abspath(__file__))
TEXTURES=os.path.join(ROOT,'ReferenceDetail_063','Textures')
os.makedirs(TEXTURES,exist_ok=True)
PARTS={}; MATERIALS={}; MAPS={}; RIG=None

def make_maps():
    size=512
    u,v=np.meshgrid(np.arange(size,dtype=np.float32)/size,np.arange(size,dtype=np.float32)/size)
    noise=(np.sin(u*911+np.sin(v*271))*np.sin(v*887)+np.sin(u*343+v*653)*.5)/1.5
    for kind in ['wood','fur','hide','leather','cloth','leaf','fish','elephant','rubber','feather']:
        if kind=='wood':
            pattern=np.sin(u*233+1.1*np.sin(v*7)+.18*np.sin(v*107))
            height=.5+.20*pattern+.08*noise
        elif kind in ('fur','feather'):
            flow=u*370+4*np.sin(v*33)+3*np.sin(u*17)
            height=.5+.18*np.sin(flow)*(.5+.5*np.sin(v*251+u*63))+.035*noise
        elif kind=='cloth':height=.5+.16*np.sin(u*math.tau*128)*np.sin(v*math.tau*128)+.025*noise
        elif kind=='fish':
            row=np.floor(v*30); x=(u*36+row*.5)%1; y=(v*30)%1
            edge=np.exp(-((y-(.28+.46*np.sin(pi*x)))/.06)**2)
            height=.65-edge*.40+.02*noise
        elif kind=='rubber':
            tread=np.minimum(abs(np.sin((u*22+v*4)*pi)),abs(np.sin((u*22-v*4)*pi)))
            height=.65-.45*np.exp(-(tread/.18)**2)
        elif kind in ('hide','leather','elephant','leaf'):
            scale=34 if kind in ('elephant','leaf') else 73
            a=np.sin(u*scale*math.tau+1.4*np.sin(v*25))
            b=np.sin(v*scale*.67*math.tau+1.4*np.sin(u*37))
            cracks=np.exp(-(a/.15)**2)*.20+np.exp(-(b/.12)**2)*.15
            height=.6-cracks+noise*(.06 if kind=='leather' else .035)
        rgba=np.ones((size,size,4),np.float32)
        tint=np.clip(.84+(height-.5)*.23+noise*.03,.55,.99)
        rgba[:,:,:3]=tint[:,:,None]
        du=(np.roll(height,-1,axis=1)-np.roll(height,1,axis=1))*1.8
        dv=(np.roll(height,-1,axis=0)-np.roll(height,1,axis=0))*1.8
        vec=np.stack((-du,-dv,np.ones_like(du)),axis=-1); vec/=np.linalg.norm(vec,axis=-1)[:,:,None]
        norm=np.ones_like(rgba); norm[:,:,:3]=vec*.5+.5
        maps=[]
        for suffix,pixels,space in [('Albedo',rgba,'sRGB'),('Normal',norm,'Non-Color')]:
            name='Ref63_'+kind+'_'+suffix
            image=bpy.data.images.new(name,width=size,height=size,alpha=False)
            image.colorspace_settings.name=space;image.pixels.foreach_set(pixels.ravel())
            image.filepath_raw=os.path.join(TEXTURES,name+'.png');image.file_format='PNG'
            image.save();image.pack();maps.append(image)
        MAPS[kind]=maps

def palette(kind,color):
    rgb=tuple(color); hexcode=''.join('%02X'%round(max(0,min(1,c))*255) for c in rgb)
    name='Ref63 '+kind+' #'+hexcode
    if name in MATERIALS:return MATERIALS[name]
    mat=bpy.data.materials.new(name);mat.diffuse_color=(*rgb,1);mat.use_nodes=True
    bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*rgb,1)
    bs.inputs['Roughness'].default_value=.68 if kind!='solid' else .48
    if kind in MAPS:
        nodes,links=mat.node_tree.nodes,mat.node_tree.links
        tex=nodes.new('ShaderNodeTexImage');tex.image=MAPS[kind][0]
        mix=nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1
        mix.inputs[2].default_value=(*rgb,1)
        links.new(tex.outputs['Color'],mix.inputs[1]);links.new(mix.outputs[0],bs.inputs['Base Color'])
        nt=nodes.new('ShaderNodeTexImage');nt.image=MAPS[kind][1]
        nm=nodes.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=.6
        links.new(nt.outputs['Color'],nm.inputs['Color']);links.new(nm.outputs['Normal'],bs.inputs['Normal'])
    MATERIALS[name]=mat;return mat

def reset(source):
    global PARTS,MATERIALS,RIG,MAPS
    bpy.ops.wm.open_mainfile(filepath=source)
    RIG=next(o for o in bpy.data.objects if o.type=='ARMATURE')
    for obj in list(bpy.data.objects):
        if obj!=RIG:bpy.data.objects.remove(obj,do_unlink=True)
    PARTS={};MATERIALS={};MAPS={}
    make_maps()
    RIG.animation_data.action=bpy.data.actions['Idle'];bpy.context.scene.frame_set(1)
    return RIG

def record(obj,mat,bone):
    if mat:obj.data.materials.append(mat)
    for p in obj.data.polygons:p.use_smooth=True
    PARTS.setdefault(bone,[]).append(obj);return obj

def mesh(name,vertices,faces,mat,bone,uvs=None):
    data=bpy.data.meshes.new(name);data.from_pydata(vertices,[],faces);data.update()
    bm=bmesh.new();bm.from_mesh(data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(data);bm.free()
    obj=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(obj)
    layer=data.uv_layers.new(name='UVMap')
    for p in data.polygons:
        for loop in p.loop_indices:
            idx=data.loops[loop].vertex_index
            layer.data[loop].uv=uvs[idx] if uvs else (vertices[idx][0]*.5+.5,vertices[idx][2]*.4)
    return record(obj,mat,bone)

def interpolate(points,t):
    f=max(0,min(len(points)-1-1e-7,t*(len(points)-1)));i=int(f);s=f-i
    a,b=points[i],points[i+1];p=points[max(0,i-1)];n=points[min(len(points)-1,i+2)]
    return [(.5*((2*a[k])+(-p[k]+b[k])*s+(2*p[k]-5*a[k]+4*b[k]-n[k])*s*s+
                  (-p[k]+3*a[k]-3*b[k]+n[k])*s*s*s)) for k in range(len(a))]

def loft(name,points,mat,bone='Body',rows=42,around=32,vertical=False,ribs=0,face=None):
    # points: x,y,z, two radii. Continuous cubic centreline, rounded caps.
    vs=[];fs=[];uv=[]
    for i in range(rows):
        t=i/(rows-1);values=interpolate(points,t);c=Vector(values[:3]);a,b=values[3:5]
        c0=Vector(interpolate(points,max(0,t-.001))[:3]);c1=Vector(interpolate(points,min(1,t+.001))[:3])
        tangent=(c1-c0).normalized()
        axis1=Vector((1,0,0)) if abs(tangent.x)<.8 else Vector((0,1,0))
        axis1=(axis1-tangent*axis1.dot(tangent)).normalized();axis2=tangent.cross(axis1).normalized()
        for j in range(around):
            u=j/around;theta=2*pi*u
            factor=1+.055*cos(theta*ribs) if ribs else 1
            if mat.name.startswith('Ref63 leaf'):
                factor*=1+.045*sin(theta*23+t*79)*sin(theta*11-t*43)
            pt=c+axis1*(max(.001,a)*cos(theta)*factor)+axis2*(max(.001,b)*sin(theta)*factor)
            if face:
                # Face sculpt fields deform the front surface of the main mesh.
                x,z=pt.x,pt.z
                forward=max(0,pt.y-c.y)/max(.01,b)
                pt.y+=forward**3*face(x,z)
            vs.append(tuple(pt));uv.append((u,t))
    for i in range(rows-1):
        for j in range(around):
            k=(j+1)%around
            fs.append((i*around+j,i*around+k,(i+1)*around+k,(i+1)*around+j))
    fs.extend([tuple(reversed(range(around))),tuple((rows-1)*around+j for j in range(around))])
    obj=mesh(name,vs,fs,mat,bone,uv)
    # Correct wrapping at the U seam, without changing geometric topology.
    layer=obj.data.uv_layers.active
    for poly in obj.data.polygons:
        if any(uv[obj.data.loops[l].vertex_index][0]>.9 for l in poly.loop_indices):
            for l in poly.loop_indices:
                idx=obj.data.loops[l].vertex_index
                if uv[idx][0]==0:layer.data[l].uv=(1,uv[idx][1])
    return obj

def ell(name,loc,scale,mat,bone='Body',seg=24,rings=14):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg,ring_count=rings,location=loc)
    o=bpy.context.object;o.name=name;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return record(o,mat,bone)

def tube(name,points,radius,mat,bone='Body'):
    if isinstance(radius,(int,float)):radius=[radius]*len(points)
    return loft(name,[(*p,r,r) for p,r in zip(points,radius)],mat,bone,rows=max(12,len(points)*6),around=12)

def box(name,loc,scale,mat,bone='Body',bevel=.025):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        m=o.modifiers.new('Machined edge radius','BEVEL');m.width=bevel;m.segments=3
        bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
    return record(o,mat,bone)

def torus(name,loc,major,minor,mat,bone='Body',axis='Z'):
    rot=(pi/2,0,0) if axis=='Y' else ((0,pi/2,0) if axis=='X' else (0,0,0))
    bpy.ops.mesh.primitive_torus_add(major_segments=40,minor_segments=10,location=loc,
        major_radius=major,minor_radius=minor,rotation=rot)
    o=bpy.context.object;o.name=name;return record(o,mat,bone)

def fan(name,points,thickness,mat,bone='Body',axis='X'):
    # Thin curved membrane with a rounded middle ridge, for fins/ears/feathers.
    contour=[]
    for i,a in enumerate(points):
        p=points[(i-1)%len(points)];b=points[(i+1)%len(points)];n=points[(i+2)%len(points)]
        for j in range(4):
            t=j/4
            contour.append(tuple(.5*(2*a[k]+(-p[k]+b[k])*t+
                (2*p[k]-5*a[k]+4*b[k]-n[k])*t*t+(-p[k]+3*a[k]-3*b[k]+n[k])*t*t*t) for k in range(3)))
    points=contour
    centre=sum((Vector(p) for p in points),Vector())/len(points)
    normal=Vector((1,0,0)) if axis=='X' else Vector((0,0,1))
    verts=[];uv=[]
    for side in (-1,1):
        verts.append(tuple(centre+normal*side*thickness));uv.append((.5,.5))
        for p in points:
            verts.append(tuple(Vector(p)+normal*side*thickness*.18));uv.append((p[1]*.4+.5,p[2]*.4))
    n=len(points)+1;faces=[]
    for i in range(1,n):
        k=1 if i==n-1 else i+1
        faces += [(0,i,k),(n,n+k,n+i),(i,n+i,n+k,k)]
    return mesh(name,verts,faces,mat,bone,uv)

def eyes(x,y,z,scale,iris,white=None,bone='Head'):
    black=palette('solid',(.025,.016,.009));cream=white or palette('solid',(.92,.90,.80))
    for s in (-1,1):
        ell('Anatomical eye socket '+str(s),(s*x,y-.025,z),(.17*scale,.07*scale,.20*scale),black,bone)
        ell('Eye white '+str(s),(s*x,y,z),(.145*scale,.056*scale,.175*scale),cream,bone)
        ell('Eye iris '+str(s),(s*x,y+.044*scale,z),(.099*scale,.030*scale,.127*scale),iris,bone)
        ell('Eye pupil '+str(s),(s*x,y+.069*scale,z),(.061*scale,.014*scale,.096*scale),black,bone)
        ell('Eye catchlight '+str(s),(s*x-.022*scale,y+.083*scale,z+.047*scale),(.019*scale,.009*scale,.023*scale),cream,bone,16,8)

def hand(name,loc,mat,bone,scale=1):
    x,y,z=loc
    ell(name+' palm',loc,(.095*scale,.069*scale,.14*scale),mat,bone)
    for i in range(4):
        dx=(i-1.5)*.044*scale;length=(.15-abs(i-1.5)*.017)*scale
        tube(name+' finger '+str(i),[(x+dx,y,z-.07*scale),(x+dx,y+.025*scale,z-.15*scale),
            (x+dx,y+.035*scale,z-.07*scale-length)],[.022*scale,.020*scale,.014*scale],mat,bone)
    tube(name+' thumb',[(x-.07*scale,y+.01*scale,z),(x-.125*scale,y+.045*scale,z-.06*scale),
        (x-.13*scale,y+.05*scale,z-.12*scale)],[.032*scale,.026*scale,.018*scale],mat,bone)

def barefoot(name,x,y,mat,bone,width=.23,length=.42):
    loft(name+' sculpted foot',[(x,y-.14,.14,.07,.08),(x,y-.07,.16,width*.7,.13),
        (x,y+.15,.13,width,.115),(x,y+length*.72,.12,width*.91,.10),
        (x,y+length,.115,width*.5,.075)],mat,bone,rows=32,around=24)
    for k in range(5):
        xx=x+(k-2)*width*.39
        ell(name+' toe '+str(k),(xx,y+length-.025-abs(k-1)*.025,.12),
            (width*.22,.125 if k==0 else .09,.08),mat,bone,20,10)
        ell(name+' toenail '+str(k),(xx,y+length+.028-abs(k-1)*.025,.176),
            (width*.14,.040,.008),palette('solid',(.64,.49,.34)),bone,16,8)

def shoe(name,x,y,mat,bone,boot=False,width=.23,length=.37):
    white=palette('solid',(.88,.87,.80));dark=palette('rubber',(.065,.054,.041))
    loft(name+' outsole',[(x,y-.29,.065,width*.6,.045),(x,y-.15,.065,width,.055),
        (x,y+.20,.065,width*1.05,.045),(x,y+length,.065,width*.55,.04)],dark,bone,rows=28,around=24)
    loft(name+' stitched upper',[(x,y-.27,.20,width*.53,.09),(x,y-.12,.23,width*.83,.17),
        (x,y+.10,.205,width,.155),(x,y+.30,.16,width*.86,.10),(x,y+length,.14,width*.35,.04)],mat,bone,rows=32,around=24)
    if boot:
        loft(name+' ankle cuff',[(x,y-.10,.18,width*.82,width*.75),
            (x,y-.10,.43,width*.81,width*.74),(x,y-.10,.48,width*.76,width*.70)],mat,bone,rows=18)
    else:
        tube(name+' midsole seam',[(x-width,y+.30,.102),(x-width,y-.11,.11),
            (x,y-.29,.11),(x+width,y-.11,.11),(x+width,y+.30,.102)],.023,white,bone)
    lace=palette('cloth',(.57,.33,.13)) if boot else palette('cloth',(.67,.82,.85))
    for k in range(5):
        yy=y-.04+k*.045;zz=(.44-k*.023) if boot else (.37-k*.023)
        tube(name+' crossed lace '+str(k),[(x-.11,yy,zz),(x+.11,yy+.025,zz-.009)],.013,lace,bone)
        for s in (-1,1):torus(name+' lace eyelet', (x+s*.117,yy,zz-.006),.016,.004,palette('solid',(.28,.26,.20)),bone)
    for s in (-1,1):
        tube(name+' side stitching '+str(s),[(x+s*width*.88,y+.24,.23),
            (x+s*width*.9,y-.04,.31),(x+s*width*.69,y-.20,.32)],.004,lace,bone)

def assemble():
    meshes=[]
    for bone,objects in PARTS.items():
        if bone not in RIG.data.bones:raise ValueError('Missing source bone '+bone)
        for obj in objects:
            group=obj.vertex_groups.new(name=bone);group.add(list(range(len(obj.data.vertices))),1,'REPLACE')
        bpy.ops.object.select_all(action='DESELECT')
        for obj in objects:obj.select_set(True)
        bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join()
        obj=bpy.context.object;obj.name='Combined_'+bone
        modifier=obj.modifiers.new('Preserved authored skeleton','ARMATURE');modifier.object=RIG
        obj.parent=RIG;meshes.append(obj)
    return meshes

def finish(name,version,source):
    meshes=assemble();out=os.path.join(ROOT,name,'V'+version[1:]);os.makedirs(out,exist_ok=True)
    source_collection=bpy.data.collections.new('High density editable sculpt source - not exported')
    bpy.context.scene.collection.children.link(source_collection);sculpts=[]
    for obj in meshes:
        if obj.name not in ('Combined_Body','Combined_Head','Combined_Neck'):continue
        if len(obj.data.vertices)>15000:continue
        copy=obj.copy();copy.data=obj.data.copy();copy.name='SCULPT_SOURCE '+obj.name
        copy.parent=None;copy.modifiers.clear();source_collection.objects.link(copy)
        bpy.context.view_layer.objects.active=copy
        sub=copy.modifiers.new('Dense editable surface','SUBSURF');sub.levels=1
        bpy.ops.object.modifier_apply(modifier=sub.name)
        copy.hide_render=True;copy.hide_set(True);sculpts.append(copy)
    triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
    if triangles>65000:
        ratio=65000/triangles
        for obj in meshes:
            bpy.context.view_layer.objects.active=obj
            reduce=obj.modifiers.new('Mobile mesh budget; dense source retained','DECIMATE')
            reduce.ratio=ratio
            bpy.ops.object.modifier_move_up(modifier=reduce.name)
            bpy.ops.object.modifier_apply(modifier=reduce.name)
    RIG.animation_data.action=bpy.data.actions['Idle'];bpy.context.scene.frame_set(1)
    blend=os.path.join(ROOT,name,name+'_'+version+'.blend');fbx=os.path.join(ROOT,name,name+'_'+version+'.fbx')
    bpy.ops.wm.save_as_mainfile(filepath=blend)
    markers=facing_markers(RIG)
    bpy.ops.wm.save_as_mainfile(filepath=blend)
    bpy.ops.object.select_all(action='DESELECT');RIG.select_set(True)
    for marker in markers:marker.select_set(True)
    for obj in meshes:obj.select_set(True)
    bpy.context.view_layer.objects.active=RIG
    bpy.ops.export_scene.fbx(filepath=fbx,use_selection=True,object_types={'MESH','ARMATURE','EMPTY'},
        bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,
        bake_anim_use_all_bones=True,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
    preview=[]
    def aim(obj,target):obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
    for loc,energy,color in [((3.8,5.2,6.2),900,(1,.87,.75)),((-4,2,3.8),650,(.61,.77,1)),((2.5,-4.5,5.2),750,(.75,.9,1))]:
        d=bpy.data.lights.new('Studio light','AREA');d.energy=energy;d.size=4;d.color=color
        o=bpy.data.objects.new('Studio light',d);bpy.context.collection.objects.link(o);o.location=loc;aim(o,(0,0,1.5));preview.append(o)
    bpy.ops.mesh.primitive_plane_add(size=24,location=(0,0,-.03));floor=bpy.context.object
    floor.data.materials.append(palette('solid',(.095,.13,.16)));preview.append(floor)
    cd=bpy.data.cameras.new('Reference studio');cam=bpy.data.objects.new('Reference studio',cd)
    bpy.context.collection.objects.link(cam);preview.append(cam);cd.type='ORTHO'
    sc=bpy.context.scene;sc.camera=cam;sc.render.engine='BLENDER_EEVEE';sc.render.resolution_x=1000;sc.render.resolution_y=1000
    sc.render.resolution_percentage=100;sc.render.image_settings.file_format='PNG';sc.render.film_transparent=False
    sc.world.color=(.025,.031,.045);sc.view_settings.view_transform='AgX';sc.view_settings.look='AgX - Medium High Contrast'
    # Bounds must include the current pose, but not the hidden sculpt copies.
    coords=[obj.matrix_world @ Vector(c) for obj in meshes for c in obj.bound_box]
    lower=Vector([min(c[k] for c in coords) for k in range(3)]);upper=Vector([max(c[k] for c in coords) for k in range(3)])
    target=(lower+upper)*.5;scale=max((upper-lower).z,(upper-lower).x)*1.23
    for view,loc in [('front',(4.0,7.0,3.5)),('side',(8,0,3.2)),('run',(4,7,3.5))]:
        if view=='run':RIG.animation_data.action=bpy.data.actions['Run'];sc.frame_set(9)
        cam.location=target+Vector(loc);cd.ortho_scale=scale;aim(cam,target)
        sc.render.filepath=os.path.join(out,name+'_'+version+'_'+view+'.png');bpy.ops.render.render(write_still=True)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in preview:obj.select_set(True)
    bpy.ops.object.delete(use_global=False)
    RIG.animation_data.action=bpy.data.actions['Idle'];sc.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=blend)
    report=dict(hero=name,version=version,source=source,rig=RIG.name,
        bones=[b.name for b in RIG.data.bones],actions=[a.name for a in bpy.data.actions],
        render_sections=len(meshes),vertices=sum(len(o.data.vertices) for o in meshes),
        triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes),
        sculpt_vertices=sum(len(o.data.vertices) for o in sculpts),blender=bpy.app.version_string)
    with open(os.path.join(out,name+'_'+version+'_report.json'),'w',encoding='utf-8') as f:json.dump(report,f,indent=2)
    print('REFERENCE_HERO_READY',json.dumps(report),flush=True)
    return report

def facing_markers(rig):
    markers=[]
    for name,position in [('ReferenceFacingOrigin',(0,0,0)),('ReferenceFacingFront',(0,1,0))]:
        previous=bpy.data.objects.get(name)
        if previous:bpy.data.objects.remove(previous,do_unlink=True)
        marker=bpy.data.objects.new(name,None);bpy.context.scene.collection.objects.link(marker)
        marker.parent=rig;marker.location=position;marker.empty_display_type='ARROWS';marker.empty_display_size=.05
        markers.append(marker)
    return markers
