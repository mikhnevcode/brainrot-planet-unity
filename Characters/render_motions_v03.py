import bpy, os, math, traceback
from mathutils import Vector
BASE=r"C:\Rabota\projects\BrainrotPlanetUnity\Characters"
OUT=os.path.join(BASE,"MotionPreviews_059")
os.makedirs(OUT,exist_ok=True)
CONFIG=[
("Tralalero","Tralalero_v04"),("Bombardiro","Bombardiro_v03"),("Frigo","Frigo_v03"),
("Bombombini","Bombombini_v03"),("Ballerina","Ballerina_v03"),("Assassino","Assassino_v03"),
("Patapim","Patapim_v03"),("TungSahur","TungSahur_v03"),("Lirili","Lirili_v03"),
("Frulli","Frulli_v03"),("Boneca","Boneca_v03"),("Trippi","Trippi_v03")]
preview_count=0
def camera_setup(hero):
    scene=bpy.context.scene
    for n,loc,energy,size in [
        ("Hero Key",(-4,6,7),800,4.5),
        ("Hero Fill",(4,4,4),500,5),
        ("Hero Rim",(0,-5,6),500,4)]:
        data=bpy.data.lights.new(n,"AREA");data.energy=energy;data.size=size
        light=bpy.data.objects.new(n,data);scene.collection.objects.link(light)
        light.location=loc
        light.rotation_euler=(Vector((0,0,1.7))-light.location).to_track_quat("-Z","Y").to_euler()
    camData=bpy.data.cameras.new("Motion Preview Camera")
    cam=bpy.data.objects.new("Motion Preview Camera",camData)
    scene.collection.objects.link(cam);scene.camera=cam
    camData.type="ORTHO";camData.ortho_scale=5.4 if hero not in ("Frigo","Lirili") else 6.0
    cam.location=(4.4,6.8,4.5)
    cam.rotation_euler=(Vector((0,.1,1.65))-cam.location).to_track_quat("-Z","Y").to_euler()
    scene.render.engine="BLENDER_EEVEE"
    scene.render.resolution_x=440;scene.render.resolution_y=440
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format="PNG"
    scene.render.film_transparent=True
    scene.view_settings.view_transform="AgX"
    return scene,cam
def render(name,action,frame,scene,rig):
    if action not in bpy.data.actions:raise RuntimeError("Missing "+action)
    rig.animation_data.action=bpy.data.actions[action]
    scene.frame_set(frame)
    scene.render.filepath=os.path.join(OUT,name+"_"+action+"_"+str(frame).zfill(3)+".png")
    bpy.ops.render.render(write_still=True)
    global preview_count;preview_count+=1
    print("POSE_PREVIEW",name,action,frame,flush=True)
for name,version in CONFIG:
    try:
        bpy.ops.wm.open_mainfile(filepath=os.path.join(BASE,name,version+".blend"))
        rig=next(o for o in bpy.data.objects if o.type=="ARMATURE")
        scene,cam=camera_setup(name)
        for action,frame in [("Drop",25),("Special",10),("VictoryShow",41)]:
            render(name,action,frame,scene,rig)
        if name in ("Ballerina","Bombardiro","Boneca","Tralalero"):
            for action in ("Drop","VictoryShow"):
                for frame in (1,9,17,33,49,57,73):
                    render(name,action,frame,scene,rig)
    except Exception:
        print("PREVIEW_FAILED",name,traceback.format_exc(),flush=True)
        raise
print("MOTION_PREVIEWS_DONE",preview_count,flush=True)