"""Step 005: reconstruct the face at its target proportions, not a folded warp.

004 is rejected: its vertical deformation produced a crease below the eyes.
This source uses continuous anatomical fields followed by the same voxel fuse.
"""
import sys,json,math,random
from pathlib import Path
import bpy
from mathutils import Vector
from math import sin,cos,pi,sqrt
BASE=Path(__file__).resolve().parent
sys.path.insert(0,str(BASE))
import sculpt_rollo_v05 as A
import sculpt_rollo_reference_step003 as P
G=A.G;S=A.S
OUT=BASE/'ArtReview/RolloReferenceMatch'
original_ell=G.ell;original_tube=G.tube;original_loft=G.loft
original_patch=A.projected_patch


def sculpt(x,z):
    g=S.gaussian
    return (.16*(g(x,z,-.24,2.79,.30,.23)+g(x,z,.24,2.79,.30,.23))
            +.07*g(x,z,0,2.94,.22,.15)
            -.075*(g(x,z,-.35,3.03,.19,.15)+g(x,z,.35,3.03,.19,.15))
            +.055*(g(x,z,-.35,3.16,.24,.09)+g(x,z,.35,3.16,.24,.09))
            -.20*g(x,z,0,2.55,.22,.12)
            +.055*g(x,z,0,2.37,.35,.08)
            +.16*g(x,z,0,1.15,.87,.25))


def ell(name,loc,size,mat=None,bone='Body',segments=24,rings=14):
    if name=='Whisker-pad sculpt volume':
        loc=(math.copysign(.24,loc[0]),1.17,2.79);size=(.32,.25,.235)
    elif name=='Painted flush whisker pore':
        x,y,z=loc;x*=.90;z+=.205;loc=(x,S.surface(x,z)+.0015,z)
    return original_ell(name,loc,size,mat,bone,segments,rings)


def tube(name,points,radius,mat=None,bone='Body'):
    if name=='Tapered expressive brow':
        side=1 if points[0][0]>0 else -1
        points=[]
        for j in range(9):
            t=j/8;x=side*(.20+.29*t);z=3.20+.058*sin(pi*t)
            points.append((x,S.surface(x,z)+.006,z))
        radius=[.002,.013,.023,.027,.028,.025,.019,.010,.001]
    elif name=='Short philtrum':
        points=[(0,S.surface(0,z)+.004,z) for z in (2.80,2.76,2.72,2.695)]
    elif name=='Fine swept ivory whisker':
        side=1 if points[0][0]>0 else -1
        j=round((abs(points[0][0])-.27)/.035)
        x=side*(.24+.032*j);z=2.85-j*.034;y=S.surface(x,z)
        points=[(x,y+.008,z),(side*.61,y+.065,z+.08-j*.010),
                (side*(.96+j*.022),y+.035,z+.14-j*.043),
                (side*(1.12+j*.022),y-.04,z+.12-j*.075)]
    elif name=='Tongue crease':
        points=[(0,1.405,2.47),(0,1.42,2.49),(0,1.407,2.52)]
    return original_tube(name,points,radius,mat,bone)


def loft(name,points,mat=None,bone='Body',**kw):
    if name=='Rounded projecting tongue':
        points=[(0,1.31,2.43,.035,.025),(0,1.37,2.49,.112,.042),(0,1.32,2.61,.08,.018)]
    return original_loft(name,points,mat,bone,**kw)


def projected_patch(name,outline,mat,bone='Head',offset=.010,surface=None):
    if name=='Recessed open mouth':
        outline=[(-.21,2.65),(-.18,2.53),(0,2.40),(.18,2.53),(.21,2.65),(.09,2.64),(0,2.62),(-.09,2.64)]
        surface=lambda x,z:1.26+.42*(z-2.48)-.10*(x/.24)**2
    return original_patch(name,outline,mat,bone,offset,surface)


def eye_cap(side,skin,iris,pupil):
    ex=side*.35;ez=3.035;ey=S.surface(ex,ez)
    sx=(S.surface(ex+.01,ez)-S.surface(ex-.01,ez))/.02
    sz=(S.surface(ex,ez+.01)-S.surface(ex,ez-.01))/.02
    verts=[];faces=[];around=64;rings=16
    for i in range(rings):
        r=max(.0001,i/(rings-1))
        for j in range(around):
            a=2*pi*j/around;dx=.118*cos(a)*r
            dz=(.127 if sin(a)>0 else .105)*sin(a)*r+side*dx*.10
            verts.append((ex+dx,ey+sx*dx+sz*dz+.075*sqrt(max(0,1-r*r))-.014,ez+dz))
    for i in range(rings-1):
        for j in range(around):
            k=i*around+j;n=i*around+(j+1)%around;faces.append((k,n,n+around,k+around))
    faces.append(tuple(reversed(range(around))))
    obj=G.D.mesh('Convex cornea inside sculpted socket',verts,faces,pupil,'Head');obj.data.materials.append(iris)
    for f in obj.data.polygons:
        if (rings-4)*around<=f.index<(rings-1)*around:f.material_index=1
    points=[]
    for j in range(25):
        a=pi*j/24;x=ex+.127*cos(a);z=ez+.131*sin(a)+side*(x-ex)*.10
        points.append((x,S.surface(x,z)+.004,z))
    original_tube('Soft hooded upper eyelid',points,[.002+.012*sin(pi*j/24) for j in range(25)],skin,'Head')
    return obj


def seal_nose(material,nostril):
    outline=[(-.14,2.91),(-.09,2.935),(0,2.92),(.09,2.935),(.14,2.91),(.085,2.84),(0,2.79),(-.085,2.84)]
    verts=[];faces=[];around=80;rows=18
    for i in range(rows):
        r=max(.001,i/(rows-1))
        for j in range(around):
            t=j/around*len(outline);k=int(t);s=t-k;p,a,b,n=[outline[q%len(outline)] for q in (k-1,k,k+1,k+2)]
            x,z=[.5*(2*a[c]+(-p[c]+b[c])*s+(2*p[c]-5*a[c]+4*b[c]-n[c])*s*s+(-p[c]+3*a[c]-3*b[c]+n[c])*s*s*s) for c in range(2)]
            x*=r;z=2.87+(z-2.87)*r;verts.append((x,S.surface(x,z)+.082*sqrt(max(0,1-r*r))-.004,z))
    for i in range(rows-1):
        for j in range(around):
            k=i*around+j;n=i*around+(j+1)%around;faces.append((k,n,n+around,k+around))
    faces.extend([tuple(reversed(range(around))),tuple((rows-1)*around+j for j in range(around))])
    obj=G.D.mesh('Triangular seal nose with nostrils',verts,faces,material,'Head')
    for side in (-1,1):
        # Flattened dark insets, not tall protruding beads on top of the nose.
        original_ell('Small embedded nostril',(side*.061,S.surface(side*.061,2.895)+.073,2.895),(.022,.006,.020),nostril,'Head',24,16)
    return obj


def make():
    A.sculpt=sculpt;A.flipper=P.reference_flipper;A.eye_cap=eye_cap;A.seal_nose=seal_nose;A.projected_patch=projected_patch
    G.ell=ell;G.tube=tube;G.loft=loft
    return A.make()


if __name__=='__main__':
    make();OUT.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'source-005.blend'))
    import review_rollo_reference as R
    config=json.loads((OUT/'iteration-003.json').read_text())
    config['master']='ArtReview/RolloReferenceMatch/source-005.blend'
    config['changed']=['Reconstructed face: closer-set smaller eyes, higher muzzle/nose/mouth, embedded nostrils, shorter tongue. Replaces rejected 004 warp. Body/flippers/roller/camera unchanged.']
    config['sourceLandmarks'].update(R.landmarks())
    (OUT/'iteration-005.json').write_text(json.dumps(config,indent=2)+'\n')
    print('ROLLO_REFERENCE_SOURCE_PASS 005',flush=True)
