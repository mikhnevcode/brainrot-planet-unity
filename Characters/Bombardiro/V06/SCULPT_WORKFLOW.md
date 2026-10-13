# Bombardiro v06: continuous head and Blender sculpt source

The head and mandible are continuous curved meshes with anatomical profiles,
integrated brow ridges, cheek volume and nose channels. No rectangular skull
overlays or spherical skin scutes are used. The head was constructed by the
reproducible Blender Python script `sculpt_bombardiro_v06.py`; it is not a claim
of manual brush sculpting.

The `.blend` includes a hidden collection named
`V06 HIGH DETAIL SCULPT SOURCE - not exported`: 147,456 vertices for the skull
and mandible together. Unhide one object, select it and enter Blender Sculpt
Mode to continue editing the dense surface with sculpt brushes.

The mobile FBX contains only seven rigged sections, 70,264 triangles in total,
and the original eight actions. The dense sculpt sources are excluded from FBX.
The skin uses explicit UVs and 1024×1024 albedo and tangent-space normal maps.
These maps are generated directly from the surface relief function, not baked
from a manually edited sculpt. If the sculpt is edited manually, bake its
normals back to the low mesh before exporting the next version.

The aircraft fuselage had inward-facing polygons and two open end loops in v04.
v06 closes 224 boundary edges and recalculates outward-facing normals. It can
therefore render using ordinary back-face culling in Unity. Forcing a transparent
material to opaque or disabling culling was not the actual geometry fix.

Rebuild with Blender 5.2:

```text
blender --background --python Characters/Bombardiro/sculpt_bombardiro_v06.py
```

The script starts from the preserved v04 source and creates new v06 files.
Unity loads `Bombardiro_v06.fbx`; `BombardiroRigSetup` imports the normal map
and creates a Resources material to retain the normal-map shader variant.
