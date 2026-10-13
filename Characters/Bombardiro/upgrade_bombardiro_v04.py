import bpy
import json
import math
import os
from mathutils import Vector


ROOT = r"C:\Rabota\projects\BrainrotPlanetUnity\Characters\Bombardiro"
SOURCE = os.path.join(ROOT, "Bombardiro_v03.blend")
BLEND_OUT = os.path.join(ROOT, "Bombardiro_v04.blend")
FBX_OUT = os.path.join(ROOT, "Bombardiro_v04.fbx")
PREVIEW_DIR = os.path.join(ROOT, "V04")
os.makedirs(PREVIEW_DIR, exist_ok=True)

if os.path.normcase(bpy.data.filepath) != os.path.normcase(SOURCE):
    bpy.ops.wm.open_mainfile(filepath=SOURCE)

rig = bpy.data.objects.get("Bombardiro_Armature")
if rig is None:
    raise RuntimeError("Bombardiro_Armature is missing from v03")


def material(fragment):
    fragment = fragment.lower()
    for item in bpy.data.materials:
        if fragment in item.name.lower():
            return item
    raise RuntimeError("Material not found: " + fragment)


GREEN = material("crocodile dark")
SNOUT = material("crocodile nose")
JAW = material("underjaw")
MOUTH = material("inner mouth")
TEETH = material("ivory teeth")
IRIS = material("yellow reptile")
PUPIL = material("black slit")
SHELL = material("brushed silver")
METAL = material("metal dark")
TRIM = material("silver edge")
BLACK = material("propeller nearly")
YELLOW = material("propeller yellow")
GLASS = material("cockpit")
CREASE = material("scale crevice")
PAINT = material("paint tank")

added = {name: [] for name in ("Body", "Jaw", "Prop_L", "Prop_R")}


def register(obj, name, mat, section="Body", smooth=True):
    obj.name = name
    if mat is not None:
        obj.data.materials.append(mat)
    if smooth and obj.type == "MESH":
        for polygon in obj.data.polygons:
            polygon.use_smooth = True
    added[section].append(obj)
    return obj


def sphere(name, location, scale, mat, section="Body", segments=16, rings=10, rotation=None):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=segments, ring_count=rings, location=location,
        rotation=rotation or (0.0, 0.0, 0.0)
    )
    obj = bpy.context.object
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return register(obj, name, mat, section)


def cone(name, location, radius_base, radius_tip, depth, mat, section="Body", rotation=None):
    bpy.ops.mesh.primitive_cone_add(
        vertices=12, radius1=radius_base, radius2=radius_tip, depth=depth,
        location=location, rotation=rotation or (0.0, 0.0, 0.0)
    )
    return register(bpy.context.object, name, mat, section)


def cylinder(name, location, radius, depth, mat, section="Body", rotation=None, vertices=16):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices, radius=radius, depth=depth, location=location,
        rotation=rotation or (0.0, 0.0, 0.0)
    )
    return register(bpy.context.object, name, mat, section)


def cube(name, location, scale, mat, section="Body", rotation=None, bevel=0.018):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location, rotation=rotation or (0.0, 0.0, 0.0))
    obj = bpy.context.object
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        modifier = obj.modifiers.new("Detail edge bevel", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
    return register(obj, name, mat, section, smooth=False)


def curve(name, points, radius, mat, section="Body", cyclic=False):
    data = bpy.data.curves.new(name, "CURVE")
    data.dimensions = "3D"
    data.resolution_u = 1
    data.bevel_depth = radius
    data.bevel_resolution = 2
    spline = data.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for point, coordinate in zip(spline.points, points):
        point.co = (*coordinate, 1.0)
    spline.use_cyclic_u = cyclic
    data.materials.append(mat)
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target="MESH")
    return register(bpy.context.object, name, None, section)


def torus(name, location, major_radius, minor_radius, mat, section="Body", rotation=None):
    bpy.ops.mesh.primitive_torus_add(
        major_segments=24, minor_segments=6, major_radius=major_radius,
        minor_radius=minor_radius, location=location,
        rotation=rotation or (0.0, 0.0, 0.0)
    )
    return register(bpy.context.object, name, mat, section)


def ellipse_ring(name, y, width, height, center_z, mat):
    points = []
    for index in range(28):
        angle = 2.0 * math.pi * index / 28.0
        points.append((width * math.cos(angle), y, center_z + height * math.sin(angle)))
    return curve(name, points, 0.010, mat, "Body", cyclic=True)


# Crocodile silhouette: a hard brow, dorsal armour and layered cheek plates.
for side in (-1, 1):
    sphere(
        "V04 heavy armoured brow " + str(side),
        (side * 0.34, 1.55, 1.965), (0.22, 0.30, 0.105), GREEN,
        rotation=(0.0, 0.0, side * 0.10)
    )
    sphere(
        "V04 eye socket shadow " + str(side),
        (side * 0.472, 1.57, 1.93), (0.052, 0.087, 0.061), CREASE
    )
    sphere(
        "V04 amber eye " + str(side),
        (side * 0.505, 1.60, 1.945), (0.035, 0.061, 0.048), IRIS
    )
    sphere(
        "V04 vertical eye slit " + str(side),
        (side * 0.536, 1.615, 1.947), (0.012, 0.041, 0.022), PUPIL,
        segments=12, rings=8
    )

    # Large readable lateral scutes, staggered from neck to muzzle.
    for row, z in enumerate((1.48, 1.66, 1.83)):
        for column in range(6):
            y = 0.86 + column * 0.27 + (row % 2) * 0.11
            x = side * (0.475 - 0.035 * max(0, column - 3))
            scale = 0.085 if column < 3 else 0.070
            sphere(
                "V04 cheek scale %s_%s_%s" % (side, row, column),
                (x, y, z), (0.026, scale * 1.10, scale),
                GREEN if (row + column) % 2 else SNOUT,
                segments=12, rings=8
            )

    # Dark lip crease gives the jaw a clear mouth line at small scale.
    curve(
        "V04 deep lip crease " + str(side),
        [
            (side * 0.405, 1.50, 1.445),
            (side * 0.405, 1.90, 1.430),
            (side * 0.365, 2.35, 1.430),
            (side * 0.285, 2.75, 1.475),
        ],
        0.022, CREASE
    )

    # Larger alternating teeth closely follow the reference silhouette.
    for index in range(9):
        y = 1.53 + index * 0.145
        taper = index / 8.0
        x = side * (0.385 - 0.095 * taper)
        upper_depth = 0.23 if index in (1, 4, 7) else 0.17
        lower_depth = 0.20 if index in (2, 5, 8) else 0.15
        cone(
            "V04 upper fang %s_%02d" % (side, index),
            (x, y, 1.43), 0.0, 0.047 if upper_depth > 0.2 else 0.035,
            upper_depth, TEETH
        )
        cone(
            "V04 lower fang %s_%02d" % (side, index),
            (x * 0.985, y + 0.052, 1.34),
            0.043 if lower_depth > 0.17 else 0.032, 0.0,
            lower_depth, TEETH, "Jaw"
        )

    # Nostrils and raised nose armour.
    sphere(
        "V04 deep nostril " + str(side),
        (side * 0.205, 2.785, 1.715), (0.050, 0.070, 0.022), CREASE,
        segments=12, rings=8
    )
    sphere(
        "V04 nose armour plate " + str(side),
        (side * 0.185, 2.63, 1.735), (0.12, 0.16, 0.042), GREEN,
        segments=14, rings=8
    )

# Raised dorsal scutes reinforce the crocodile-to-airframe transition.
for index, (y, size) in enumerate(((0.72, 0.14), (0.97, 0.17), (1.22, 0.19), (1.48, 0.18), (1.72, 0.15))):
    cone(
        "V04 dorsal skull scute %02d" % index,
        (0.0, y, 2.02 + size * 0.35), size * 0.72, 0.02, size * 0.58,
        GREEN, rotation=(0.0, 0.0, 0.0)
    )

# Aircraft skin: panel rings, longitudinal seams, rivets and inspection plates.
for index, profile in enumerate((
    (-1.86, 0.34, 0.32, 1.64),
    (-1.28, 0.44, 0.43, 1.66),
    (-0.65, 0.52, 0.51, 1.67),
    (0.00, 0.54, 0.52, 1.68),
    (0.62, 0.46, 0.43, 1.71),
)):
    ellipse_ring("V04 fuselage panel ring %02d" % index, *profile, METAL)

for side in (-1, 1):
    curve(
        "V04 longitudinal hull seam " + str(side),
        [
            (side * 0.31, -2.08, 1.85),
            (side * 0.46, -1.25, 1.97),
            (side * 0.53, -0.35, 2.00),
            (side * 0.43, 0.55, 1.98),
        ],
        0.010, METAL
    )
    for index in range(10):
        y = -1.75 + index * 0.245
        width = 0.38 + 0.15 * math.sin((index / 9.0) * math.pi)
        sphere(
            "V04 hull rivet %s_%02d" % (side, index),
            (side * width, y, 1.76), (0.019, 0.020, 0.014), TRIM,
            segments=10, rings=6
        )

    # Four exhaust stubs per side make the powerplant read from the menu view.
    for index in range(4):
        cylinder(
            "V04 exhaust stack %s_%02d" % (side, index),
            (side * 0.51, 0.18 + index * 0.18, 1.93),
            0.035, 0.18, BLACK,
            rotation=(0.0, side * 0.30, 0.0)
        )

    # Engine cowling bands and bolt heads.
    x = side * 1.48
    for index, y in enumerate((-0.68, -0.42, -0.16)):
        torus(
            "V04 engine cowling band %s_%02d" % (side, index),
            (x, y, 1.52), 0.322, 0.014, TRIM,
            rotation=(math.pi / 2.0, 0.0, 0.0)
        )
    for index in range(8):
        angle = 2.0 * math.pi * index / 8.0
        sphere(
            "V04 engine front bolt %s_%02d" % (side, index),
            (x + 0.275 * math.cos(angle), 0.058, 1.52 + 0.275 * math.sin(angle)),
            (0.026, 0.018, 0.026), TRIM,
            segments=10, rings=6
        )

    # Streamlined three-bomb load visually supports the actual ability.
    bomb_x = side * 0.72
    sphere(
        "V04 wing paint bomb " + str(side),
        (bomb_x, -0.62, 1.02), (0.12, 0.34, 0.12), PAINT,
        segments=16, rings=10
    )
    cone(
        "V04 wing bomb nose " + str(side),
        (bomb_x, -0.30, 1.02), 0.11, 0.015, 0.22, PAINT,
        rotation=(-math.pi / 2.0, 0.0, 0.0)
    )
    for fin_side in (-1, 1):
        cube(
            "V04 wing bomb fin %s_%s" % (side, fin_side),
            (bomb_x + fin_side * 0.10, -0.89, 1.02),
            (0.10, 0.09, 0.018), METAL,
            rotation=(0.0, 0.0, fin_side * 0.20), bevel=0.012
        )

    # More substantial pointed propeller spinner, weighted to the prop bone.
    prop_section = "Prop_L" if side < 0 else "Prop_R"
    cone(
        "V04 propeller spinner " + str(side),
        (x, 0.51, 1.52), 0.16, 0.025, 0.30, TRIM, prop_section,
        rotation=(-math.pi / 2.0, 0.0, 0.0)
    )
    sphere(
        "V04 propeller hub cap " + str(side),
        (x, 0.40, 1.52), (0.17, 0.10, 0.17), METAL, prop_section,
        segments=16, rings=10
    )

# Centre bomb is slightly larger than the two wing bombs.
sphere("V04 centre paint bomb", (0.0, -0.36, 0.92), (0.14, 0.40, 0.14), PAINT)
cone(
    "V04 centre bomb nose", (0.0, 0.02, 0.92), 0.13, 0.018, 0.25, PAINT,
    rotation=(-math.pi / 2.0, 0.0, 0.0)
)
for side in (-1, 1):
    cube(
        "V04 centre bomb fin " + str(side),
        (side * 0.12, -0.70, 0.92), (0.12, 0.10, 0.020), METAL,
        rotation=(0.0, 0.0, side * 0.18), bevel=0.012
    )


def apply_modifiers(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    for modifier in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=modifier.name)


def merge_section(section, target_name):
    target = bpy.data.objects.get(target_name)
    if target is None:
        raise RuntimeError("Target section missing: " + target_name)
    details = added[section]
    if not details:
        return target
    for obj in details:
        apply_modifiers(obj)
        group = obj.vertex_groups.new(name=section)
        group.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
        obj.parent = rig
    bpy.ops.object.select_all(action="DESELECT")
    target.select_set(True)
    for obj in details:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = target
    bpy.ops.object.join()
    target.name = target_name
    return target


sections = {
    "Body": merge_section("Body", "Combined_Body"),
    "Jaw": merge_section("Jaw", "Combined_Jaw"),
    "Prop_L": merge_section("Prop_L", "Combined_Prop_L"),
    "Prop_R": merge_section("Prop_R", "Combined_Prop_R"),
}

# Preserve the eight authored actions and export only the animated character.
if rig.animation_data is None:
    rig.animation_data_create()
idle = bpy.data.actions.get("Idle")
rig.animation_data.action = idle
bpy.context.scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUT)

bpy.ops.object.select_all(action="DESELECT")
for obj in bpy.data.objects:
    if obj.type == "MESH" or obj == rig:
        obj.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(
    filepath=FBX_OUT,
    use_selection=True,
    object_types={"MESH", "ARMATURE"},
    bake_anim=True,
    bake_anim_use_all_actions=True,
    bake_anim_use_all_bones=True,
    bake_anim_step=1,
    bake_anim_use_nla_strips=False,
    add_leaf_bones=False,
    apply_scale_options="FBX_SCALE_ALL",
    axis_forward="-Z",
    axis_up="Y",
)

# Studio previews are excluded from the saved character source and FBX.
preview_objects = []
bpy.ops.mesh.primitive_plane_add(size=24, location=(0.0, 0.0, 0.36))
floor = bpy.context.object
floor.name = "V04 PREVIEW FLOOR"
floor.data.materials.append(METAL)
preview_objects.append(floor)


def aim(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def area(name, location, energy, size, color):
    data = bpy.data.lights.new(name, "AREA")
    data.energy = energy
    data.shape = "DISK"
    data.size = size
    data.color = color
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    aim(obj, (0.0, 0.35, 1.60))
    preview_objects.append(obj)


area("V04 key softbox", (3.8, 5.2, 6.2), 1050, 5.0, (1.0, 0.84, 0.69))
area("V04 cool fill", (-4.0, 2.0, 3.8), 720, 4.0, (0.48, 0.68, 1.0))
area("V04 rim", (2.5, -4.5, 5.2), 900, 4.0, (0.75, 0.90, 1.0))

camera_data = bpy.data.cameras.new("V04 studio camera")
camera = bpy.data.objects.new("V04 studio camera", camera_data)
bpy.context.collection.objects.link(camera)
camera_data.type = "ORTHO"
preview_objects.append(camera)
bpy.context.scene.camera = camera

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 1024
scene.render.resolution_y = 1024
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
scene.world.color = (0.018, 0.025, 0.045)
scene.view_settings.view_transform = "AgX"
scene.view_settings.look = "AgX - Medium High Contrast"

views = (
    ("Bombardiro_v04_front_3q", (4.9, 6.6, 4.1), (0.0, 0.15, 1.55), 7.1),
    ("Bombardiro_v04_side", (7.3, 0.25, 3.4), (0.0, 0.05, 1.55), 6.8),
    ("Bombardiro_v04_detail", (3.3, 6.0, 3.0), (0.0, 1.28, 1.58), 4.1),
)
for name, location, target, scale in views:
    camera.location = location
    camera_data.ortho_scale = scale
    aim(camera, target)
    scene.render.filepath = os.path.join(PREVIEW_DIR, name + ".png")
    bpy.ops.render.render(write_still=True)

bpy.ops.object.select_all(action="DESELECT")
for obj in preview_objects:
    obj.select_set(True)
bpy.ops.object.delete(use_global=False)
rig.animation_data.action = idle
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUT)

report = {
    "model": "Bombardiro Crocodilo v04 detailed",
    "source": os.path.basename(SOURCE),
    "blend": os.path.basename(BLEND_OUT),
    "fbx": os.path.basename(FBX_OUT),
    "blender": bpy.app.version_string,
    "bones": [bone.name for bone in rig.data.bones],
    "actions": {
        action.name: [float(action.frame_range[0]), float(action.frame_range[1])]
        for action in bpy.data.actions
    },
    "mesh_sections": {
        obj.name: len(obj.data.vertices)
        for obj in bpy.data.objects if obj.type == "MESH"
    },
    "details_added": sum(len(items) for items in added.values()),
    "renderer_sections": len([obj for obj in bpy.data.objects if obj.type == "MESH"]),
}
with open(os.path.join(PREVIEW_DIR, "Bombardiro_v04_report.json"), "w", encoding="utf-8") as handle:
    json.dump(report, handle, ensure_ascii=False, indent=2)
print("BOMBARDIRO_V04_READY", json.dumps(report, ensure_ascii=False))
