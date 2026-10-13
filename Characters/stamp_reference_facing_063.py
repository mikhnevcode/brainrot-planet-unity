"""Add two non-rendering facing markers to existing verified mobile meshes."""
import bpy,os,sys,json
sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
from reference_detail_063 import facing_markers,ROOT
names=['Frigo','Tralalero','TungSahur','Ballerina','Patapim','Lirili','Trippi','Troppa','Assassino','Frulli','Boneca','Bombombini']
for name in names:
    version='v05' if name=='Tralalero' else ('v01' if name=='Troppa' else 'v04')
    path=os.path.join(ROOT,name,name+'_'+version)
    bpy.ops.wm.open_mainfile(filepath=path+'.blend')
    rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
    markers=facing_markers(rig)
    rig.animation_data.action=bpy.data.actions['Idle'];bpy.context.scene.frame_set(1)
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
    for o in list(bpy.data.objects):
        if o in markers or (o.type=='MESH' and o.name.startswith('Combined_')):o.select_set(True)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.fbx(filepath=path+'.fbx',use_selection=True,object_types={'MESH','ARMATURE','EMPTY'},
        bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,
        bake_anim_use_all_bones=True,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
    bpy.ops.wm.save_as_mainfile(filepath=path+'.blend')
    print('REFERENCE_FACING_STAMPED',name,flush=True)
