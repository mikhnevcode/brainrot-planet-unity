"""Preserve meshes and limb choreography; replace full-body rolls by a readable sway.
New .blend sources and animation-only FBX files; original models are untouched.
"""
import bpy,math,os,json
ROOT=os.path.dirname(os.path.abspath(__file__))
heroes=[('Tralalero','v05'),('Bombardiro','v06'),('Frigo','v04'),('Bombombini','v04'),('Ballerina','v04'),('Assassino','v04'),('Patapim','v04'),('TungSahur','v04'),('Lirili','v04'),('Frulli','v04'),('Boneca','v04'),('Trippi','v04'),('Troppa','v01')]
dest=os.path.join(ROOT,'Victory064');os.makedirs(dest,exist_ok=True)
reports=[]
for hero,(name,version) in enumerate(heroes):
    bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT,name,name+'_'+version+'.blend'))
    rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
    original=bpy.data.actions['VictoryShow']
    action=original.copy();action.name='VictoryStage064';rig.animation_data.action=action
    if hasattr(action,'slots') and action.slots:rig.animation_data.action_slot=action.slots[0]
    # Keep all wing, propeller, hand, foot, facial and skirt animation.
    # Local Y of the Body bone is not world-up for every imported rig.
    # Removing multi-turn Body channels avoids rolling aircraft onto their sides.
    for frame in range(1,74):
        bpy.context.scene.frame_set(frame)
        t=(frame-1)/72;envelope=math.sin(math.pi*t)**2
        body=rig.pose.bones.get('Body')
        if body:
            body.rotation_mode='XYZ'
            body.rotation_euler=(math.radians(4)*math.sin(t*math.tau*3)*envelope,
                                 math.radians(7)*math.sin(t*math.tau*2)*envelope,
                                 math.radians(5)*math.sin(t*math.tau*2)*envelope)
            body.keyframe_insert('rotation_euler',frame=frame,group='Body')
        root=rig.pose.bones.get('Root')
        if root:
            root.rotation_mode='XYZ';root.rotation_euler=(0,0,0)
            root.keyframe_insert('rotation_euler',frame=frame,group='Root')
    bpy.context.scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(dest,name+'_stage064.blend'))
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.fbx(filepath=os.path.join(dest,name+'_stage064.fbx'),use_selection=True,
        object_types={'ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,
        bake_anim_use_nla_strips=False,bake_anim_use_all_bones=True,
        # Match the renderable FBX: Bombardiro stores unit scale in the FBX header,
        # while the newer reference cast bakes it into the armature transform.
        apply_scale_options='FBX_SCALE_ALL' if name=='Bombardiro' else 'FBX_SCALE_NONE',
        axis_forward='-Z',axis_up='Y')
    reports.append({'hero':name,'source':name+'_'+version,'action':'VictoryStage064','frames':73,'body_max_degrees':[4,7,5],'limb_animation':'preserved'})
    print('VICTORY_STAGE_AUTHORED',name,flush=True)
with open(os.path.join(dest,'report.json'),'w',encoding='utf-8') as f:json.dump(reports,f,indent=2)
