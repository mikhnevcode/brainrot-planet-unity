"""010: correct the roller proportions and rebuild its solid axial end pieces."""
import sys,json,math
from pathlib import Path
import bpy
BASE=Path(__file__).resolve().parent
sys.path.insert(0,str(BASE))
import sculpt_rollo_reference_step009 as F
A=F.A;G=F.G;S=F.S;OUT=F.OUT


def lathe(name,side,sections,mat,bone):
    verts=[];faces=[];n=80
    for x,r in sections:
        for j in range(n):
            a=j/n*math.tau;verts.append((side*x,.79+r*math.cos(a),.47+r*math.sin(a)))
    for i in range(len(sections)-1):
        for j in range(n):
            k=i*n+j;q=i*n+(j+1)%n;faces.append((k,q,q+n,k+n))
    faces.extend([tuple(reversed(range(n))),tuple((len(sections)-1)*n+j for j in range(n))])
    return G.D.mesh(name,verts,faces,mat,bone)


def make():
    body=F.make()
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    drum=next(o for o in meshes if o.name.startswith('Porous orange'))
    ivory=next(o.data.materials[0] for o in meshes if o.name.startswith('Ivory roller cheek'))
    foam=drum.data.materials[0]
    for vertex in drum.data.vertices:
        vertex.co.y=.79+(vertex.co.y-.79)*.85
        vertex.co.z=.47+(vertex.co.z-.55)*.85
    drum.data.update()
    old=[o for o in meshes if o.name.startswith(('Ivory roller cheek','Rounded guard attachment','Bright orange axle','Axle center'))]
    for bone,parts in G.D.PARTS.items():G.D.PARTS[bone]=[o for o in parts if o not in old]
    for obj in old:bpy.data.objects.remove(obj,do_unlink=True)
    for side in (-1,1):
        lathe('Ivory roller cheek guard',side,[(1.15,.31),(1.17,.35),(1.205,.36),(1.23,.35),(1.24,.31)],ivory,'Body')
        # A narrow rounded bracket no longer cuts across the orange axle hub.
        F.F.F.F.original_loft('Rounded guard attachment',[(side*1.20,.69,.68,.085,.07),(side*1.19,.61,.91,.095,.07),(side*1.12,.49,1.15,.065,.045),(side*1.08,.46,1.22,.008,.008)],ivory,'Body',rows=32,around=28)
        lathe('Bright orange axle hub',side,[(1.237,.14),(1.25,.17),(1.30,.17),(1.325,.15),(1.33,.13)],foam,'Appendage_0')
        lathe('Axle center',side,[(1.329,.065),(1.346,.074),(1.36,.065)],foam,'Appendage_0')
    # Soft belly drape onto the smaller roller, restricted to the lower front.
    for vertex in body.data.vertices:
        x,y,z=vertex.co
        front=max(0,min(1,(y-.45)/.65))
        vertex.co.z-=.105*math.exp(-((z-1.0)/.35)**4)*front
    body.data.update();G.BONES['Appendage_0']=(0,.79,.47)
    return body


if __name__=='__main__':
    make();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'source-010.blend'))
    config=json.loads((OUT/'iteration-009.json').read_text())
    config['master']='ArtReview/RolloReferenceMatch/source-010.blend'
    config['changed']=['Roller radius -15%, solid lathed cheek guards and hubs, narrower support brackets and lower belly contact. Other regions/material/camera unchanged.']
    config['sourceLandmarks'].update(hubNear=[1.30,.79,.47],hubFar=[-1.30,.79,.47],bellyBottom=[0,1.30,1.015])
    (OUT/'iteration-010.json').write_text(json.dumps(config,indent=2)+'\n')
    print('ROLLO_REFERENCE_SOURCE_PASS 010',flush=True)
