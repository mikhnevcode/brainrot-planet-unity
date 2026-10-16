"""016: replace the ellipsoid with a genuinely thin, bent, rounded tongue."""
import sys,json,math
from pathlib import Path
import bpy
from mathutils import Vector
BASE=Path(__file__).resolve().parent
sys.path.insert(0,str(BASE))
import geometry as G
OUT=BASE/'ArtReview/RolloReferenceMatch'


def section(t):
    center=Vector((0,1.30+.22*math.sin(t*math.pi/2),2.65-.20*t+.015*math.sin(math.pi*t)))
    dy=.22*math.pi/2*math.cos(t*math.pi/2);dz=-.20+.015*math.pi*math.cos(math.pi*t)
    normal=Vector((0,-dz,dy)).normalized()
    rounded=math.sqrt(max(1e-6,1-((t-.65)/.35)**2)) if t>.65 else 1
    width=(.085+.035*math.sin(min(1,t/.65)*math.pi/2))*rounded
    depth=.016*(.85+.15*math.sin(math.pi*t))*rounded
    return center,normal,width,depth


def replace_tongue():
    old=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith(('Rounded projecting tongue','Tongue crease'))]
    tongue=next(o for o in old if o.name.startswith('Rounded projecting tongue'))
    shade=next(o.data.materials[0] for o in old if o.name.startswith('Tongue crease'))
    material=tongue.data.materials[0]
    for bone,parts in G.D.PARTS.items():G.D.PARTS[bone]=[o for o in parts if o not in old]
    for obj in old:bpy.data.objects.remove(obj,do_unlink=True)
    verts=[];faces=[];rows=60;around=48
    for i in range(rows):
        center,normal,width,depth=section(i/(rows-1))
        for j in range(around):
            a=math.tau*j/around
            verts.append(tuple(center+Vector((width*math.cos(a),0,0))+normal*(depth*math.sin(a))))
    for i in range(rows-1):
        for j in range(around):
            k=i*around+j;n=i*around+(j+1)%around;faces.append((k,n,n+around,k+around))
    faces.extend([tuple(reversed(range(around))),tuple((rows-1)*around+j for j in range(around))])
    obj=G.D.mesh('Rounded projecting tongue',verts,faces,material,'Head')
    points=[]
    for t in (.10,.25,.45,.62,.77):
        center,normal,width,depth=section(t);points.append(tuple(center+normal*(depth+.0015)))
    G.tube('Tongue crease',points,[.0008,.0018,.0020,.0018,.0004],shade,'Head')
    return obj


if __name__=='__main__':
    bpy.ops.wm.open_mainfile(filepath=str(OUT/'source-015.blend'));replace_tongue()
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'source-016.blend'))
    config=json.loads((OUT/'iteration-015.json').read_text());config['master']='ArtReview/RolloReferenceMatch/source-016.blend'
    config['changed']=['Tongue only: closed thin ribbon volume, bent forward/down, rounded distal tip and shallow center groove. No ellipsoid or inflated ball.']
    (OUT/'iteration-016.json').write_text(json.dumps(config,indent=2)+'\n')
    print('ROLLO_REFERENCE_SOURCE_PASS 016',flush=True)
