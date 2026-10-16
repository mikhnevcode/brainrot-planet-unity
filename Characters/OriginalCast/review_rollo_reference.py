"""Small, reversible reference-pose iterations. Does not modify runtime assets.

Blender --background --python this_file.py -- --extract / --iteration 000
The concept is a visual reference only; never a texture projected on the model.
"""
import sys,json,math,argparse
from pathlib import Path
import bpy
from mathutils import Vector,Matrix

BASE=Path(__file__).resolve().parent
OUT=BASE/'ArtReview'/'RolloReferenceMatch'
MASTER=BASE/'Models_v05/RolloBulk/RolloBulk_v05_master.blend'
POSE_CACHE={}


def smooth(t):
    t=max(0,min(1,t));return t*t*(3-2*t)


def posed(point,pose):
    p=Vector(point)
    grab=pose.get('nearShoulderGrab')
    if grab is not None:
        # One localized sculpt-style grab: bring the proximal near flipper
        # toward the chest. The distal end and the opposite side stay put.
        falloff=math.exp(-((p.x-1.05)/.40)**2-((p.z-2.05)/.40)**2-((p.y-.65)/.55)**2)
        falloff*=smooth((p.y-.25)/.35)
        p+=Vector(grab)*falloff
    head=smooth((p.z-1.85)/.65)
    face=smooth((p.z-2.00)/.20)*smooth((p.y-.90)/.30)*(1-smooth((abs(p.x)-.50)/.30))
    head=max(head,face)
    key=tuple(pose.get(k,0) for k in ('headRoll','headYaw','leanForward'))
    if key not in POSE_CACHE:
        pivot=Vector((0,.30,2.30));rotation=Matrix.Rotation(math.radians(key[1]),3,'Z')@Matrix.Rotation(math.radians(key[0]),3,'Y')
        POSE_CACHE[key]=(rotation,pivot-rotation@pivot,Matrix.Rotation(math.radians(-key[2]),3,'X'))
    rotation,offset,lean=POSE_CACHE[key]
    turned=rotation@p+offset
    p=p.lerp(turned,head)
    pivot=Vector((0,0,.90))
    p=pivot+lean@(p-pivot)
    return p


def landmarks():
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    body=next(o for o in meshes if o.name.startswith('Rollo reference anatomical'))
    eyes=sorted([o for o in meshes if o.name.startswith('Convex cornea')],key=lambda o:sum(v.co.x for v in o.data.vertices)/len(o.data.vertices))
    nose=next(o for o in meshes if o.name.startswith('Triangular seal nose'))
    def average(o):return sum((o.matrix_world@v.co for v in o.data.vertices),Vector())/len(o.data.vertices)
    return {'hubNear':[1.30,.79,.55],'hubFar':[-1.30,.79,.55],
            'eyeNear':list(average(eyes[1])),'eyeFar':list(average(eyes[0])),
            'nose':list(average(nose)),
            'crown':list(max((o.matrix_world@v.co for o in [body] for v in o.data.vertices),key=lambda p:p.z)),
            'bellyBottom':[0,1.30,1.12],
            'flipperNearRoot':[.97,.54,2.15],'flipperNearTip':[1.76,.96,1.37],
            'flipperFarRoot':[-.97,.54,2.15],'flipperFarTip':[-1.76,.96,1.37]}


def studio(config):
    from bpy_extras.object_utils import world_to_camera_view
    scene=bpy.context.scene
    scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
    scene.render.resolution_x=1254;scene.render.resolution_y=1254;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.world=bpy.data.worlds.new('Fixed comparison studio');scene.world.color=(.36,.42,.46)
    scene.world.use_nodes=True;nodes=scene.world.node_tree.nodes;links=scene.world.node_tree.links;nodes.clear()
    output=nodes.new('ShaderNodeOutputWorld');path=nodes.new('ShaderNodeLightPath');mix=nodes.new('ShaderNodeMixShader')
    ambient=nodes.new('ShaderNodeBackground');ambient.inputs['Color'].default_value=(.05,.05,.05,1)
    background=nodes.new('ShaderNodeBackground');background.inputs['Color'].default_value=(.55,.85,.95,1);background.inputs['Strength'].default_value=1.5
    links.new(path.outputs['Is Camera Ray'],mix.inputs[0]);links.new(ambient.outputs[0],mix.inputs[1]);links.new(background.outputs[0],mix.inputs[2]);links.new(mix.outputs[0],output.inputs[0])
    scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.view_settings.exposure=-.7
    for location,power,size in [((4,5,7),1050,4.),((-4,3,4),650,4.),((2,-4,5),1250,3.)]:
        data=bpy.data.lights.new('Fixed comparison softbox','AREA');data.energy=power;data.shape='DISK';data.size=size
        ob=bpy.data.objects.new('Fixed comparison softbox',data);scene.collection.objects.link(ob);ob.location=location
        ob.rotation_euler=(Vector((0,0,1.6))-ob.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.008));floor=bpy.context.object;floor.name='Comparison floor'
    floor_mat=bpy.data.materials.new('Unchanged comparison floor');floor_mat.use_nodes=True
    floor_mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.48,.72,.77,1)
    floor_mat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.8;floor.data.materials.append(floor_mat)
    data=bpy.data.cameras.new('Locked reference camera');camera=bpy.data.objects.new('Locked reference camera',data);scene.collection.objects.link(camera);scene.camera=camera
    c=config['camera'];target=Vector((0,.15,1.70));yaw=math.radians(c['yaw']);pitch=math.radians(c['pitch'])
    offset=Vector((math.sin(yaw)*math.cos(pitch),math.cos(yaw)*math.cos(pitch),math.sin(pitch)))*10
    camera.location=target+offset;camera.rotation_euler=(-offset).to_track_quat('-Z','Y').to_euler()
    data.type='ORTHO';data.ortho_scale=c['scale'];data.shift_x=-c['shiftX'];data.shift_y=c['shiftY']
    bpy.context.view_layer.update()
    return {name:[world_to_camera_view(scene,camera,posed(p,config['pose'])).x*1254,
                  (1-world_to_camera_view(scene,camera,posed(p,config['pose'])).y)*1254]
            for name,p in config['sourceLandmarks'].items()}


def main():
    args=argparse.ArgumentParser();args.add_argument('--extract',action='store_true');args.add_argument('--iteration',default='000')
    args=args.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    OUT.mkdir(parents=True,exist_ok=True)
    if args.extract:
        bpy.ops.wm.open_mainfile(filepath=str(MASTER))
        (OUT/'source-landmarks.json').write_text(json.dumps(landmarks(),indent=2)+'\n')
        print('ROLLO_REFERENCE_LANDMARKS_EXTRACTED',flush=True);return
    config=json.loads((OUT/('iteration-'+args.iteration+'.json')).read_text())
    master=(BASE/config.get('master','Models_v05/RolloBulk/RolloBulk_v05_master.blend')).resolve()
    if not master.is_relative_to(BASE):raise ValueError('Review source outside original-cast workspace')
    bpy.ops.wm.open_mainfile(filepath=str(master))
    for obj in list(bpy.context.scene.objects):
        if obj.type!='MESH':continue
        inverse=obj.matrix_world.inverted()
        for vertex in obj.data.vertices:vertex.co=inverse@posed(obj.matrix_world@vertex.co,config['pose'])
        obj.data.update()
    projected=studio(config)
    report={'status':'art-review-only-not-runtime','iteration':args.iteration,'camera':config['camera'],'pose':config['pose'],
            'changed':config['changed'],'projectedLandmarks':projected,'referenceLandmarks':config['referenceLandmarks'],
            'reference':'OriginalConcepts_20261015_v1/01-rollo-bulk.png','source':master.relative_to(BASE).as_posix()}
    (OUT/('iteration-'+args.iteration+'-report.json')).write_text(json.dumps(report,indent=2)+'\n')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/('iteration-'+args.iteration+'.blend')))
    bpy.context.scene.render.filepath=str(OUT/('iteration-'+args.iteration+'.png'));bpy.ops.render.render(write_still=True)
    print('ROLLO_REFERENCE_ITERATION_PASS '+args.iteration,flush=True)


if __name__=='__main__':main()
