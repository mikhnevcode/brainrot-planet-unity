"""017: smiling almond eye openings and asymmetric, tapered expressive brows."""
import sys,json,math
from pathlib import Path
import bpy
BASE=Path(__file__).resolve().parent
sys.path.insert(0,str(BASE))
import geometry as G
import sculpt_rollo_v02 as S
OUT=BASE/'ArtReview/RolloReferenceMatch'


def eye_outline(side,a):
    dx=.127*math.cos(a)
    h=.120 if math.sin(a)>=0 else .065
    dz=h*math.sin(a)+(.005*(1-abs(math.cos(a))) if math.sin(a)<0 else 0)+side*dx*.06
    return side*.35+dx,3.035+dz


def replace_expression(body):
    S.BODY=body
    old=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith(('Convex cornea','Soft hooded upper eyelid','Tapered expressive brow','Smiling raised lower eyelid','Small natural eye catchlight'))]
    cap=next(o for o in old if o.name.startswith('Convex cornea'))
    pupil,iris=cap.data.materials[:2]
    dark=next(o.data.materials[0] for o in old if o.name.startswith('Tapered expressive brow'))
    skin=body.data.materials[0]
    for bone,parts in G.D.PARTS.items():G.D.PARTS[bone]=[o for o in parts if o not in old]
    for obj in old:bpy.data.objects.remove(obj,do_unlink=True)
    for side in (-1,1):
        ex=side*.35;ez=3.035;ey=S.surface(ex,ez)
        sx=(S.surface(ex+.01,ez)-S.surface(ex-.01,ez))/.02
        sz=(S.surface(ex,ez+.01)-S.surface(ex,ez-.01))/.02
        verts=[];faces=[];around=64;rings=16
        for i in range(rings):
            r=max(.0001,i/(rings-1))
            for j in range(around):
                x,z=eye_outline(side,math.tau*j/around);dx=(x-ex)*r;dz=(z-ez)*r
                verts.append((ex+dx,ey+sx*dx+sz*dz+.078*math.sqrt(max(0,1-r*r))-.008,ez+dz))
        for i in range(rings-1):
            for j in range(around):
                k=i*around+j;n=i*around+(j+1)%around;faces.append((k,n,n+around,k+around))
        faces.append(tuple(reversed(range(around))))
        obj=G.D.mesh('Convex cornea inside sculpted socket',verts,faces,pupil,'Head');obj.data.materials.append(iris)
        for face in obj.data.polygons:
            if (rings-6)*around<=face.index<(rings-1)*around:face.material_index=1
        highlight=G.material('eyeHighlight','#FFF9E9','solid')
        dx=-.031;dz=.043;r2=(dx/.127)**2+(dz/.120)**2
        y=ey+sx*dx+sz*dz+.078*math.sqrt(1-r2)-.006
        G.ell('Small natural eye catchlight',(ex+dx,y,ez+dz),(.010,.006,.012),highlight,'Head',20,12)
        for upper in (True,False):
            points=[]
            for j in range(25):
                a=math.pi*j/24+(0 if upper else math.pi);x,z=eye_outline(side,a)
                points.append((x,S.surface(x,z)+.006,z))
            G.tube('Soft hooded upper eyelid' if upper else 'Smiling raised lower eyelid',points,
                   [.002+(.013 if upper else .017)*math.sin(math.pi*j/24)**.7 for j in range(25)],skin,'Head')
        points=[];radii=[]
        for j in range(25):
            t=j/24;x=side*((.08+.25*t) if side>0 else (.24+.22*t))
            z=(3.18+.055*t if side>0 else 3.20+.025*t)+.040*math.sin(math.pi*t)
            points.append((x,S.surface(x,z)+.006,z))
            radii.append(.001+.028*math.sin(math.pi*t)**.8*(.80+.30*t))
        G.tube('Tapered expressive brow',points,radii,dark,'Head')


if __name__=='__main__':
    bpy.ops.wm.open_mainfile(filepath=str(OUT/'source-016.blend'))
    body=next(o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith('Rollo reference anatomical'))
    replace_expression(body);bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'source-017.blend'))
    import review_rollo_reference as R
    config=json.loads((OUT/'iteration-016.json').read_text());config['master']='ArtReview/RolloReferenceMatch/source-017.blend'
    config['changed']=['Expression only: horizontally elongated eye openings, lifted lower lids, more visible brown iris edge and arched/tapered brows with a lower inner end.']
    config['sourceLandmarks'].update(R.landmarks())
    (OUT/'iteration-017.json').write_text(json.dumps(config,indent=2)+'\n')
    print('ROLLO_REFERENCE_SOURCE_PASS 017',flush=True)
