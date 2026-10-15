"""Fuse Tralalero's shoe details into the animated footwear.

The v06 sneaker contained many free-standing eyelets, laces and side tubes.
They were rigidly weighted, but their gaps became conspicuous during the
victory dance and looked like detached pieces.  This pass keeps the sculpted
upper, outsole and inset midsole seam, removes the floating shells and paints
flush lace bands directly onto the upper topology.  The skeleton, actions and
all non-foot meshes remain byte-for-byte equivalent Blender data.

Run with Blender --background --python Characters/fix_tralalero_shoes_0719.py
"""
import json
import os

import bmesh
import bpy
from mathutils import Vector


ROOT = os.path.dirname(os.path.abspath(__file__))
SOURCE = os.path.join(ROOT, "Tralalero", "Tralalero_v06.blend")
OUT_DIR = os.path.join(ROOT, "Tralalero", "V07")
BLEND = os.path.join(ROOT, "Tralalero", "Tralalero_v07.blend")
FBX = os.path.join(ROOT, "Tralalero", "Tralalero_v07.fbx")


def components(mesh):
    adjacency = [set() for _ in mesh.vertices]
    for edge in mesh.edges:
        a, b = edge.vertices
        adjacency[a].add(b)
        adjacency[b].add(a)
    unseen = set(range(len(mesh.vertices)))
    result = []
    while unseen:
        seed = unseen.pop()
        stack = [seed]
        found = {seed}
        while stack:
            current = stack.pop()
            for neighbour in adjacency[current]:
                if neighbour in unseen:
                    unseen.remove(neighbour)
                    found.add(neighbour)
                    stack.append(neighbour)
        result.append(found)
    return result


def materials_for_component(obj, component):
    names = set()
    for polygon in obj.data.polygons:
        if any(vertex in component for vertex in polygon.vertices):
            names.add(obj.material_slots[polygon.material_index].name)
    return names


def clean_shoe(obj, foot_bone):
    keep_materials = {
        "Ref63 cloth #056EAB",   # continuous stitched upper
        "Ref63 rubber #110E0A", # continuous outsole
    }
    pieces = components(obj.data)
    keep = set()
    removed = []
    for component in pieces:
        names = materials_for_component(obj, component)
        if names and names.issubset(keep_materials):
            keep.update(component)
        else:
            removed.append({"vertices": len(component), "materials": sorted(names)})

    before_vertices = len(obj.data.vertices)
    mesh = bmesh.new()
    mesh.from_mesh(obj.data)
    mesh.verts.ensure_lookup_table()
    bmesh.ops.delete(mesh, geom=[vertex for vertex in mesh.verts if vertex.index not in keep], context="VERTS")
    mesh.to_mesh(obj.data)
    mesh.free()
    obj.data.update()

    # The visible lace pattern is now a material inlay on the continuous upper,
    # not a second mesh floating above it.  It therefore cannot lag behind or
    # separate from the shoe in any animation.
    upper_index = next(i for i, slot in enumerate(obj.material_slots) if slot.name == "Ref63 cloth #056EAB")
    lace_index = next(i for i, slot in enumerate(obj.material_slots) if slot.name == "Ref63 cloth #ABD1D9")
    outsole_index = next(i for i, slot in enumerate(obj.material_slots) if slot.name == "Ref63 rubber #110E0A")
    trim_index = next(i for i, slot in enumerate(obj.material_slots) if slot.name == "Ref63 solid #E0DECC")
    xs = [vertex.co.x for vertex in obj.data.vertices]
    ys = [vertex.co.y for vertex in obj.data.vertices]
    center_x = (min(xs) + max(xs)) * 0.5
    center_y = (min(ys) + max(ys)) * 0.5
    lace_rows = [center_y - 0.055, center_y + 0.005, center_y + 0.065, center_y + 0.125]
    lace_faces = 0
    trim_faces = 0
    for polygon in obj.data.polygons:
        if polygon.material_index == outsole_index and polygon.center.z > 0.078:
            polygon.material_index = trim_index
            trim_faces += 1
            continue
        if polygon.material_index != upper_index:
            continue
        center = polygon.center
        if abs(center.x - center_x) > 0.135 or center.z < 0.255 or polygon.normal.z < 0.22:
            continue
        if min(abs(center.y - row) for row in lace_rows) < 0.024:
            polygon.material_index = lace_index
            lace_faces += 1

    group = obj.vertex_groups.get(foot_bone)
    if group is None:
        raise RuntimeError("Missing shoe group " + foot_bone)
    for other in list(obj.vertex_groups):
        if other != group:
            obj.vertex_groups.remove(other)
    group.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
    if lace_faces < 8:
        raise RuntimeError("Too few flush lace faces on " + obj.name + ": " + str(lace_faces))
    if trim_faces < 24:
        raise RuntimeError("Too few flush sole-trim faces on " + obj.name + ": " + str(trim_faces))
    if len(components(obj.data)) != 2:
        raise RuntimeError("Expected continuous upper and outsole on " + obj.name)
    return {
        "name": obj.name,
        "vertices_before": before_vertices,
        "vertices_after": len(obj.data.vertices),
        "removed_components": len(removed),
        "removed_vertices": before_vertices - len(obj.data.vertices),
        "flush_lace_faces": lace_faces,
        "flush_sole_trim_faces": trim_faces,
        "removed": removed,
    }


def aim(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


bpy.ops.wm.open_mainfile(filepath=SOURCE)
rig = next(obj for obj in bpy.data.objects if obj.type == "ARMATURE")
rest = {bone.name: [list(bone.head_local), list(bone.tail_local)] for bone in rig.data.bones}
actions = sorted(action.name for action in bpy.data.actions)
reports = []
for side in ("L", "R", "Rear"):
    reports.append(clean_shoe(bpy.data.objects["Combined_Foot_" + side], "Foot_" + side))

if rest != {bone.name: [list(bone.head_local), list(bone.tail_local)] for bone in rig.data.bones}:
    raise RuntimeError("Rest skeleton changed")
if actions != sorted(action.name for action in bpy.data.actions):
    raise RuntimeError("Animation actions changed")

meshes = [obj for obj in bpy.data.objects if obj.type == "MESH" and obj.parent == rig and not obj.name.startswith("SCULPT_SOURCE")]
triangles = sum(sum(len(polygon.vertices) - 2 for polygon in obj.data.polygons) for obj in meshes)
if triangles > 65020:
    raise RuntimeError("Mobile triangle budget exceeded: " + str(triangles))

os.makedirs(OUT_DIR, exist_ok=True)
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=BLEND)
bpy.ops.object.select_all(action="DESELECT")
rig.select_set(True)
for obj in meshes:
    obj.select_set(True)
for obj in bpy.data.objects:
    if obj.name in ("ReferenceFacingOrigin", "ReferenceFacingFront"):
        obj.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(
    filepath=FBX,
    use_selection=True,
    object_types={"MESH", "ARMATURE", "EMPTY"},
    bake_anim=True,
    bake_anim_use_all_actions=True,
    bake_anim_use_nla_strips=False,
    bake_anim_use_all_bones=True,
    add_leaf_bones=False,
    apply_scale_options="FBX_SCALE_NONE",
    axis_forward="-Z",
    axis_up="Y",
    path_mode="STRIP",
)

# Render close, animation-specific evidence of all three corrected shoes.
for location, energy, color in [
    ((3.4, 4.5, 3.5), 900, (1.0, 0.89, 0.78)),
    ((-3.0, 2.0, 2.7), 650, (0.63, 0.81, 1.0)),
    ((1.5, -3.0, 2.6), 700, (0.80, 0.90, 1.0)),
]:
    data = bpy.data.lights.new("Shoe QA light", "AREA")
    data.energy = energy
    data.size = 3.5
    data.color = color
    lamp = bpy.data.objects.new("Shoe QA light", data)
    bpy.context.collection.objects.link(lamp)
    lamp.location = location
    aim(lamp, (0, 0, 0.35))

camera_data = bpy.data.cameras.new("Shoe QA camera")
camera = bpy.data.objects.new("Shoe QA camera", camera_data)
bpy.context.collection.objects.link(camera)
camera_data.type = "ORTHO"
camera_data.ortho_scale = 2.45
scene = bpy.context.scene
scene.camera = camera
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 960
scene.render.resolution_y = 960
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
scene.world.color = (0.025, 0.03, 0.045)
scene.view_settings.view_transform = "AgX"
camera.location = (2.9, 4.2, 1.65)
aim(camera, (0, -0.02, 0.33))
rig.animation_data.action = bpy.data.actions["VictoryShow"]
for frame in (1, 19, 37, 55):
    scene.frame_set(frame)
    scene.render.filepath = os.path.join(OUT_DIR, "Tralalero_v07_victory_shoes_%02d.png" % frame)
    bpy.ops.render.render(write_still=True)

report = {
    "source": "Tralalero_v06.blend",
    "output": "Tralalero_v07.blend",
    "purpose": "remove floating shoe shells and use flush topology inlays",
    "triangles": triangles,
    "rest_skeleton_preserved": True,
    "actions": actions,
    "shoes": reports,
    "blender": bpy.app.version_string,
}
with open(os.path.join(OUT_DIR, "Tralalero_v07_report.json"), "w", encoding="utf-8") as handle:
    json.dump(report, handle, ensure_ascii=False, indent=2)
print("TRALALERO_SHOE_FUSION_PASS " + json.dumps(report, ensure_ascii=False), flush=True)
