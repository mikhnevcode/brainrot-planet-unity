"""Read-only Blender audit of exported-mobile source sections (hidden sculpt excluded)."""
import bpy,bmesh,json,os
root=os.path.dirname(os.path.abspath(__file__))
names=['Frigo','Tralalero','TungSahur','Ballerina','Patapim','Lirili','Trippi','Troppa','Assassino','Frulli','Boneca','Bombombini']
records=[]
for name in names:
    version='v05' if name=='Tralalero' else ('v01' if name=='Troppa' else 'v04')
    bpy.ops.wm.open_mainfile(filepath=os.path.join(root,name,name+'_'+version+'.blend'))
    sections=[]
    for obj in bpy.data.objects:
        if obj.type!='MESH' or not obj.name.startswith('Combined_'):continue
        bm=bmesh.new();bm.from_mesh(obj.data)
        boundary=sum(e.is_boundary for e in bm.edges)
        nonmanifold=sum(not e.is_manifold for e in bm.edges)
        volume=bm.calc_volume(signed=True);bm.free()
        sections.append(dict(name=obj.name,boundary_edges=boundary,nonmanifold_edges=nonmanifold,signed_volume=volume))
    records.append(dict(hero=name,sections=sections,actions=sorted(a.name for a in bpy.data.actions)))
    print('CAST_TOPOLOGY',name,sections,flush=True)
out=os.path.join(root,'ReferenceDetail_063','TopologyAudit.json')
with open(out,'w',encoding='utf-8') as stream:json.dump(records,stream,indent=2)
assert all(s['boundary_edges']==0 and s['nonmanifold_edges']==0 and s['signed_volume']>0 for r in records for s in r['sections'])
print('REFERENCE_TOPOLOGY_PASS heroes=12 boundaries=0 outward_volume=true')
