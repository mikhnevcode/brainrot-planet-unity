# Brainrot Planet.io — character personalities and skills (v0.5.0)

Date: 2026-10-12. Unity 6; Realme RMX3151 acceptance.

## Intent

All twelve selectable heroes must have personality, not identical wooden running cycles.
Preserve physics/collider, spherical territory, 145-second match, swipe steering,
three-page hero selection, 2.9-second orbit drop, and matte character materials.
Make posture and motion expressive without affecting gameplay coordinates.

## Animation system

`Assets/Scripts/HeroMotion.cs` provides an **additive procedural layer**
*after* existing imported Blender Animator clips (Idle, Run, Jump, Land, Victory).
The original FBX / .blend geometry and skeletons are not overwritten.

One HeroMotion instance is attached to each player and AI visual Animator
during `PlanetActor.Configure`, using cached references to torso, head,
wing/arm, tail/skirt and leg/foot bones. It uses a shared set of motion rules
with twelve styles: varying stride, waddle, wing flutter, body tilt,
tail swish, rhythmic bobbing, head gestures and victory choreography.

- **Idle:** breathing, spontaneous small gestures and head turns.
- **Running:** hero-specific sway, arm/wing motion, weight shifting and leg cycles.
- **Landing:** short comic body wobble in addition to Animator Land.
- **Drop:** frantic leg bicycling and arm/wing flailing while the actor physically
  falls from 9 units above ground during the 2.9-second camera approach.
- **Special:** up to 1.1 seconds of signature posturing/attack body motion.
- **Victory:** about 2.55 seconds of unique dance on a significant capture
  (at least 150 new cells) or on match victory. The motion remains visual:
  Collider/Rigidbody and camera remain untouched.

## Character-specific abilities (single 12s cooldown)

| ID | Hero | Gameplay special | Choreography / silhouette |
|---|---|---|---|
| 0 | Tralalero | Shark sprint, speed 3.5s | Three-leg kicking and fin freestyle |
| 1 | Bombardiro | Triple paint bombs spread -16/0/+16°, radius .070 each | Wing rocking and victory barrel roll |
| 2 | Frigo | Ice wave slows enemies within 21° for 4s | Refrigerator robotic shoulder shimmy |
| 3 | Bombombini | Jet rush, speed 4s | Turbine & wing flutter, spinning flyby |
| 4 | Ballerina | Coffee whirl paints a .125rad disk | Pirouette, pointe dance, spins |
| 5 | Assassino | Shadow dash + .08rad paint pocket | Stealthy lean and crossed-sword salute |
| 6 | Patapim | Forest roots: .10rad paint + slow within 15° | Branch-arm monkey swing |
| 7 | Tung Sahur | Drum shock: .09rad paint + slow within 18° | Four-beat drum step and staff swing |
| 8 | Lirili | Cactus storm: .11rad paint + slow within 13° | Trunk gestures and cactus shuffle |
| 9 | Frulli | Wing dance: speed 3.5s + .075rad paint | Wing-flap twirl |
| 10 | Boneca | Spring launch: extra upward impulse + .115rad paint **on landing** | Wheel roll and rhythmic pogo |
| 11 | Trippi | Three paint splashes across an arc (3x .068rad) | Shrimp tail wiggle and cat shuffle |

The original ability symbols remain on the skill button. The 12s cooldown
remains in effect, including no special activation during the descent.

## Effects and performance

`PlanetEffects.HeroAbility` adds a one-off hero-colored circular wave and a
small particle burst. It allocates temporary objects only when a skill is
activated; existing `PaintWave` destroys itself after its short lifetime.
`HeroMotion.LateUpdate` only modifies cached visual transforms, avoiding
per-frame prefab instantiation, scene-wide bone searches, or new trail textures.

Do not claim measured FPS without running an on-device performance trace.
Check actual animation and gameplay behavior on Realme before marking PASS.

## Source files

- `Assets/Scripts/HeroMotion.cs`
- `Assets/Scripts/PlanetActor.cs`
- `Assets/Scripts/PlanetGame.cs`
- `Assets/Scripts/PlanetEffects.cs`
- `qa_hero_motion_050.py`

The visual animation layer can later be replaced or augmented with
higher-quality hand-keyframed Blender clips. That is a future art pass,
not part of this source-level implementation.

## QA

The script `qa_hero_motion_050.py` launches each selectable hero on Realme,
checks hero selection, landing, locomotion, ability triggers, and screenshot
pairs with diagnostic logs. The report is stored under
`Characters/Hero_Motion_050_QA.json` after device testing.
