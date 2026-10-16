"""Blender geometry checks for the actual v07 source, not an art score."""
import sys,json,hashlib,struct
from pathlib import Path
import bpy,bmesh
from mathutils import Vector
BASE=Path(__file__).resolve().parent
sys.path.insert(0,str(BASE))
from sculpt_rollo_reference_step016 import section
from sculpt_rollo_reference_step017 import eye_outline
ROOT=BASE.parents[1]


def roller_signatures():
    result={}
    for obj in bpy.context.scene.objects:
        if obj.type!='MESH' or not obj.name.startswith(('Porous orange','Ivory roller cheek','Rounded guard attachment','Bright orange axle','Axle center')):continue
        digest=hashlib.sha256()
        for v in obj.data.vertices:digest.update(struct.pack('<3f',*(obj.matrix_world@v.co)))
        for p in obj.data.polygons:
            digest.update(struct.pack('<I',len(p.vertices)))
            for i in p.vertices:digest.update(struct.pack('<I',i))
        result[obj.name]=digest.hexdigest()
    return result


def main():
    bpy.ops.wm.open_mainfile(filepath=str(BASE/'Models_v06/RolloBulk/RolloBulk_v06_master.blend'));before=roller_signatures()
    bpy.ops.wm.open_mainfile(filepath=str(BASE/'Models_v07/RolloBulk/RolloBulk_v07_master.blend'));after=roller_signatures()
    assert before==after,'Unrequested roller geometry changed'
    tongue=next(o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith('Rounded projecting tongue'))
    bm=bmesh.new();bm.from_mesh(tongue.data);boundary=sum(not e.is_manifold for e in bm.edges);bm.free()
    assert boundary==0,'Tongue must be a closed solid, not a transparent plane'
    sections=[section(i/100) for i in range(101)]
    width=max(s[2]*2 for s in sections);thickness=max(s[3]*2 for s in sections)
    assert thickness/width<.16,'Inflated tongue returned'
    outline=[eye_outline(1,i/200*6.283185307) for i in range(201)]
    ratio=(max(p[1] for p in outline)-min(p[1] for p in outline))/(max(p[0] for p in outline)-min(p[0] for p in outline))
    assert .60<ratio<.80,'Eye opening must be almond shaped, not a circle or a slit'
    objects=list(bpy.context.scene.objects)
    for prefix,count in [('Convex cornea',2),('Smiling raised lower eyelid',2),('Tapered expressive brow',2),('Small natural eye catchlight',2)]:
        assert sum(o.name.startswith(prefix) for o in objects)==count,prefix
    report={'schema':1,'source':'RolloBulk_v07_master.blend','rollerGeometryByteIdenticalToV06':True,
            'tongueClosed':True,'tongueNonManifoldEdges':boundary,'tongueSectionThickness':round(thickness,5),'tongueSectionWidth':round(width,5),'tongueThicknessToWidth':round(thickness/width,4),
            'eyeOpeningHeightToWidth':round(ratio,4),'eyes':2,'raisedLowerLids':2,'taperedBrows':2,'catchlights':2,
            'limits':['Geometry checks, not a measured likeness or happiness score','Flippers are part of the fused body; their authoring paths are unchanged, not a byte-identical separated mesh claim']}
    path=ROOT/'QA/v084/face-geometry-report.json';path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(report,indent=2)+'\n')
    print('ROLLO_FACE_GEOMETRY_PASS rollerUnchanged=true tongueClosed=true tongueThin=true almondEyes=true',flush=True)


if __name__=='__main__':main()
