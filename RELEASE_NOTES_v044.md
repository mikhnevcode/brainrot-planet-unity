# Brainrot Planet.io 0.4.4 — smoother territory and trails

Date: 2026-10-12. Unity 6 / Android. APK versionCode 26.

## Rendering changes

- Territory ownership and flood fill remain at 256x128 logical cells. The gameplay and territory-join algorithm from 0.4.2 remain unchanged.
- PlanetSurface now creates a 512x256 RGBA32 visualization (2x each axis), with a small 3x3 smoothing pass and a fast integer bilinear upsampling pass. Source/output arrays are allocated once, reused for every update.
- Unfinished trails are rendered by the planet's existing LineRenderer using the actual sampled 3D directions, rather than quantized cell-center locations.
- Rounded line caps/corners and a 0.34 world-unit ribbon replace the square per-cell trail overlays. The hashed Trails arrays continue to serve gameplay collision/flood closure, but do not invoke texture uploads on every movement step.
- Final territory fills and special abilities still update the map texture. Longitude texture coordinates continue to wrap.

## Performance

Realme RMX3151 on-device testing:
- 512x256 texture update mean: 5.13 ms, max: 7.23 ms (31 updates sampled).
- Another initial test: mean 5.22 ms, max 7.3 ms (26 updates).
- CPU update cost was measured only for texture repaint, **not** a device-wide FPS estimate. There is no per-frame terrain repaint.
- 512x256 RGBA32 texture consumes ~512 KiB of raw GPU data vs ~128 KiB previously, before engine overhead and additional working arrays.

## Regression validation

Second Android path regression repeated the previous 12-swipe player path:
- 3 confirmed player captures with areaCells 394/240/248.
- 3 previous-territory seam-repair events: 28/24/29 cells.
- Automatic swipe movement continued and no fatal Android exception was detected.
- Smooth line and paint areas were visually inspected in actual Realme screenshots.
- Earlier route with one extra intermediate screenshot recorded zero captures due to changed timing; the faithful repeat restored captures. This is not evidence of a change in gameplay semantics.

Artifacts:
- Characters/Realme_Smooth_Territory_044.jpg
- Characters/Smooth_Territory_044_QA.json
- Characters/Smooth_Territory_044_Events.txt
- qa_smooth_v044.py, qa_smooth_v044_repeat.py

Release:
https://loadfiles.testmkn.ru/downloads/brainrot-planet/BrainrotPlanet-v0.4.4.apk
