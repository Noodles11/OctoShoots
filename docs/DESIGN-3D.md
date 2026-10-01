# Clementine's Quest 3D — Game Design Document (v0.1, draft)

> A first-person roguelite shooter set in a stylized 3D reef. You are
> **Clementine**, a bioluminescent octopus. You swim through procedural
> reefs, squirt ink at hostile sea life and dive toward the bottom of the ocean.

This document covers **only what changes** from the 2D design
([`DESIGN-2D.md`](DESIGN-2D.md)). Every rule not mentioned here is inherited
unchanged: lore, core loop, items, synergies, transformations, seeds, Menace,
numeric health, pickups, economy, special rooms, meta progression, saves.

---

## 0. Decisions

| Topic | Decision |
|---|---|
| Platform | **Desktop** (Windows, Linux, macOS). Steam-ready exports. |
| Engine | **Godot 4** (Forward+ renderer), **C#** (.NET 8). |
| Camera | **First person**, toggle to **third person** (§3). |
| Movement | Free swimming, **level horizon** — yaw + pitch, no roll (§2). |
| Aiming | Mouse aim, **aim assist** with a strength option (§4.2). |
| Art | **Stylized**: toon-shaded, rim-lit, bold silhouettes, neon glow (§8). |
| Input | Keyboard + mouse. Gamepad post-v1 (aim assist already supports it). |
| Determinism | Gameplay sim is engine-independent and seeded (§10). |

## 1. Scope of change

| Kept as-is | Redesigned |
|---|---|
| Items, modifiers, triggers, synergies, transformations | Camera, controls, aiming |
| Seeded RNG streams, Menace curve | Level generation (3D chambers, SDF terrain) |
| HP, pickups, shops, clams, Sea Snacks | Destructible reef (3D craters) |
| Bosses roster and story, The Tank | Enemy navigation and boss patterns |
| Achievements, unlocks, Sea-pedia, saves | Map, lighting, water, all art |

## 2. Movement — level horizon

- **Mouse** turns (yaw) and looks up/down (pitch, clamped ±85°). **No roll** ever.
- **W/S** swim along the view direction (pitch included). **A/D** strafe on the horizontal plane.
- **Space** rises, **Ctrl** descends (world-vertical, independent of pitch).
- **Slow sink** (inherited): no input → drifts down at 0.15 m/s, eased in over 0.5 s; settles on the floor.
- **Jet start** (inherited §20): the first stroke from rest is a 0.38 s burst at ~1.8× speed. Sharp turns (>110°) trigger another.
- **Ink dash** (inherited §30): **Shift**, ~3.4× speed along the movement direction (or view direction if idle), 0.32 s invulnerable, 0.85 s cooldown, leaves a slowing ink cloud.
- Base swim speed **4.5 m/s**; item `speed` scales it. Inertia: 0.12 s to full speed, 0.25 s to stop. Responsive first.
- Collision: Clementine is a **sphere, r = 0.35 m**, slides along terrain.

## 3. Camera

### 3.1 First person (default)
- FOV **90°** (option 70–110). Light head bob on strokes (option: off).
- **Viewmodel arms**: 2–4 arm tips at the lower screen edges.
  - Idle: slow curling. Stroke: flare and sweep. Dash: whip shut.
  - Shooting: the siphon squirt shows at bottom center with a small recoil.
  - **Items change the arms**: glow color, spines (Fire Urchin Spine), crusts (Barnacle Armor), etc.
- Her glow is a **point light** at the camera — the main light in dark depths.
- Hurt: magenta screen-edge pulse, arms whip outward, short chromatic wobble.

### 3.2 Third person (toggle: **V**)
- Spring-arm camera 3.5 m behind and 0.6 m above, pulls in on terrain collision.
- Shows the full octopus body (mantle, eye, eight verlet arms, item cosmetics).
- Aiming always comes from the **screen-center reticle**; shots fire from her siphon toward the reticle hit point.
- Gameplay is identical in both modes.

### 3.3 Always third person
Title screen, Tide Pool hub, cutscenes, boss intros, death cam.

## 4. Shooting

### 4.1 Ink projectiles
- **LMB** (hold for auto-fire at the fire-rate stat). Charge items (Pearl Diver, Sunbeam): hold LMB to charge.
- Projectiles are **visible, slow, physical** — never hitscan. Keeps the Isaac feel.
- Stat mapping (1 tile in 2D = **1.5 m**):

| Stat | Base | 3D meaning |
|---|---|---|
| Damage | 3.5 | Unchanged |
| Fire rate | 2.7 /s | Unchanged |
| Shot speed | 1.0 | ×14 m/s |
| Range | 6.5 | ×1.5 m → 9.75 m, then the blob dissolves |

- Shots inherit 30% of Clementine's velocity (inherited rule).
- Blob hit radius **0.25 m** (generous on purpose).

### 4.2 Aim assist
Option: **Off / Low / Medium (default) / High**.

| Setting | Magnetism cone | Bullet bend | Sensitivity slow on target |
|---|---|---|---|
| Off | — | — | — |
| Low | 3° | 2°/s | 15% |
| Medium | 5° | 6°/s | 25% |
| High | 8° | 12°/s | 35% |

- **Magnetism**: on fire, the shot direction snaps toward the nearest visible enemy inside the cone.
- **Bullet bend**: projectiles curve slightly toward targets in the cone (cosmetic-safe, deterministic).
- **Slowdown**: mouse sensitivity drops while the reticle is over an enemy.
- Homing items stack on top of assist; they don't replace it.
- Seeded runs record the assist setting; it doesn't affect unlocks.

### 4.3 Shot modifiers in 3D

| Modifier | 3D behavior |
|---|---|
| Spiral (Nautilus) | Helix around the flight axis |
| Wave (Double Helix) | Two shots in counter-phase helices |
| Bounce (Mirror Scale) | Reflects off the terrain SDF normal |
| Split (Mitosis) | Splits into 2 at ±25° in the plane facing the camera |
| Multishot / Triple | Horizontal fan; Hammerhead = cone |
| Boomerang | Returns to Clementine's current position |
| Orbit | Ring around Clementine in the horizontal plane |
| Laser (Sunbeam) | Beam from the siphon to the reticle point |
| Rear Fin | Fires opposite the view direction |
| Kraken Form (8-way) | 6 axis directions + 2 diagonals of the view |

Other modifiers (homing, pierce, spectral, freeze, burn, poison, charm, chain, explosive, grow) work unchanged.

## 5. Controls

| Action | Key |
|---|---|
| Look | Mouse |
| Swim / strafe | W A S D |
| Rise / descend | Space / Ctrl |
| Shoot / charge | LMB |
| Ink dash | Shift |
| Active item | F |
| Ink Bomb | E |
| Consumable | Q |
| Camera toggle | V |
| Map (hold) | Tab |
| Pause | Esc |
| Restart (hold) | R |

All rebindable.

## 6. World — 3D reefs

### 6.1 Macro structure
- Each reef is a **3D grid of chambers** (Depth 1: 4×3×3, growing deeper).
- Chamber size ~**24×16×24 m**. Linked by a seeded spanning tree + ~12% loops (inherited §19).
- **Vertical bias**: the start is in the top layer, the boss arena in the bottom layer, farthest from the start. The dive goes down.
- Three reefs per depth, the Tank stays single (inherited §31).

### 6.2 Terrain
- Terrain is a **signed distance field** in 32³ voxel chunks at 0.5 m resolution.
- Carving: chambers = noise-perturbed ellipsoids, tunnels = wandering tube splines (≥3 m wide), plus rock pillars, arches, overhangs.
- Mesh: **marching cubes** per chunk on worker threads; triplanar stylized materials.
- Validation: flood fill on the voxel grid guarantees every chamber is reachable with Clementine-sized clearance.
- Floors exist in every chamber (a flattened bottom), so sinking pickups and walkers have somewhere to rest.

### 6.3 Destructible reef
- Explosions subtract **spheres** from the SDF (ink bomb r = 2.5 m, explosive ink r = 0.6 m, charged pearl r = 0.8 m; beams burn r = 0.4 m slowly).
- Only affected chunks remesh. Outer 2 m shell is indestructible.
- Craters are stored per reef and saved (inherited §22).
- Sealed pockets and buried coins (inherited §26) work the same, now in 3D.

### 6.4 Special chambers
Treasure, shop (safe water), curse den, secret cave, Mermaid's Grotto — inherited. Each has a **landmark light color** so you can spot it from a distance.

### 6.5 Map
- **Tab**: rotatable 3D map of seen terrain (low-poly hull), fog of war, chamber icons, Clementine's position and facing. Mouse drag rotates, wheel zooms, layer slider isolates one vertical level.
- **Compass strip** at the top of the HUD shows nearby landmarks and the rift after the boss falls.

## 7. Enemies & bosses in 3D

### 7.1 Movement classes
| Class | 3D behavior |
|---|---|
| Swimmers | Free 3D flight on a voxel **flow field** toward Clementine |
| Walkers | Crawl on floors and slopes (surface navmesh) |
| Clingers | Stick to any surface — floor, wall or ceiling |
| Burrowers | Hide in sand or holes, burst out |

### 7.2 Fairness in first person
- Every attack has a **3D positional sound** telegraph.
- **Danger markers** at the screen edge for attacks from outside the view.
- Telegraph minimum is **0.45 s** (2D: 0.35 s).
- Max **3 enemies** attack from outside the view at once; others wait.
- Enemy projectiles glow in their own color and leave short trails.

### 7.3 Menace
All parameters inherited (§5.3 of the 2D doc). Visual menace: darker bodies, glowing slit eyes, sharper silhouettes, stronger rim light.

### 7.4 Bosses
- Arenas are **spherical caverns** (~40 m) with the Crack in the floor.
- Bullet patterns become **rings, spheres and sweeping planes**, readable from inside.
- Examples:
  - **Old Gus**: suction pulls you toward his mouth; gravel spit in a cone.
  - **Giant Squid**: tentacles sweep through the arena as planes; ink blackouts cut the light.
  - **Hollow Maw**: drinks the light; arena darkens in phases.
  - **The Hand**: you look **up** through the water surface. A giant finger descends. Glass taps shake the camera. The net sweeps as a curved wall.

## 8. Art direction — stylized

- **Toon shading**: 3-band diffuse ramp, strong rim light, soft fog.
- **Outlines**: thin dark post-process edges on gameplay objects only (echo of the comic look). Ambient stays soft.
- **Readable silhouettes**: enemies have simple, bold shapes and saturated accents.
- **Palette per depth** (inherited §5): bright turquoise shallows → neon-on-black Abyss → plastic LED Tank.
- **Neon glow**: bloom on shots, hits, pickups, synergies (inherited §11.3 event language).
- **Clementine** stays the warm orange light in every scene. Never scary.

### 8.1 Asset strategy
| Asset | Approach |
|---|---|
| Terrain | Procedural (SDF + triplanar) |
| Coral, kelp, grass, anemones | Procedural meshes (L-systems, verlet chains), instanced |
| Fish, eels, salps, jellies | Procedural lofted / tube meshes + shader animation |
| Clementine | Hand-modeled mantle + procedural verlet arm meshes |
| Crabs, shrimp, squid, bosses | Hand-modeled, rigged, stylized low-poly |
| The Tank props | Hand-modeled, deliberately plastic-looking |

## 9. Water, light & atmosphere

- **God rays + volumetric fog** (Godot built-in), density rises with Menace.
- **Caustics**: projected light texture on terrain, fades with depth.
- **Ink shots are point lights** (pooled, max 32 active; rest fake it with emissive only).
- **Twilight Trench**: ambient near zero, only glows light the world.
- **Particles** (GPU): marine snow, bubbles rising and pooling under overhangs, sand puffs, ink clouds.
- **Water field**: low-res 3D velocity grid (24³) following the camera; impulses from strokes, shots, explosions. Drives particles, kelp and fish. **Cosmetic only.**
- **Fish schools**: 3D boids, flee Clementine and explosions.
- **Ink stains** (inherited §21): decals on terrain, newest 260 kept.
- Post: bloom, color grade per depth, vignette, light grain. Options for each.

## 10. Technical architecture

| Concern | Choice |
|---|---|
| Engine | Godot 4.x, Forward+ |
| Language | C# (.NET 8) |
| Sim | **`Core` library**: pure C#, no Godot types. Fixed 60 Hz tick. Seeded RNG streams. |
| Gameplay collision | Custom sphere-vs-SDF and sphere-vs-sphere in `Core` (deterministic) |
| Cosmetic physics | Godot/Jolt for debris; verlet arms, kelp, boids in `Game` |
| Terrain | Chunked SDF, marching cubes on worker threads |
| Navigation | Voxel flow fields (swimmers), surface navmesh (walkers) |
| Data | Items, enemies, bosses, pools as **JSON** — ported from the 2D game |
| Audio | Synth-style SFX + 3D positional audio (Godot AudioStreamPlayer3D) |
| Saves | JSON in user dir, versioned, export/import codes (inherited) |
| Tests | xUnit on `Core`: RNG, generation determinism, modifiers, saves |
| CI | GitHub Actions: tests + headless export for 3 desktop platforms |

```
project.godot
src/
  Core/          sim: rng, stats, items, modifiers, entities, combat, gen graph, saves
  Core.Tests/    xUnit
  Game/          Godot layer
    Player/      controller, cameras, viewmodel, aim assist
    Terrain/     sdf, chunks, meshing, craters
    Enemies/     visuals, animation, nav bridges
    Fx/          ink, glow, particles, water field, boids, plants
    UI/          hud, map, menus, sea-pedia
    Scenes/      title, hub, run, pause, gameover
data/            items.json, enemies.json, bosses.json, pools.json
assets/          models, shaders, materials, audio
```

Performance target: **60 FPS at 1080p** on a mid-range GPU (GTX 1660 / RX 5600 class) with 200 projectiles, 40 enemies, full particles. Quality presets Low/Medium/High/Auto (inherited).

## 11. Options & comfort

- FOV slider, head bob off, camera shake off, motion blur off (default).
- Aim assist level, mouse sensitivity, invert Y.
- Reduced flash, reduced ambient motion, colorblind-safe shot outlines (inherited).
- Center dot always on (reduces motion sickness).

## 12. Delivery plan

1. **Feel prototype** — grey-box cave, swim, sink, jet, dash, shoot, one enemy, aim assist. **Go/no-go gate.**
2. **Core port** — RNG, stats, items, modifiers, saves in `Core` with tests.
3. **3D reef generation** — chamber graph, SDF carving, meshing, validation, craters.
4. **Enemies** — four movement classes, flow fields, telegraphs, danger markers.
5. **Depth 1 vertical slice** — 3 reefs, 3 bosses, ~15 items, pickups, shop, third-person toggle.
6. **Look** — toon shading, outlines, fog, god rays, caustics, particles, boids, plants.
7. **Meta** — hub, unlocks, 3D map, Sea-pedia, saves.
8. **Depths 2–6**, then **The Tank** and The Hand.
9. **Polish** — comfort options, presets, balance, desktop exports.

## 13. Open questions

- Gamepad support timing (post-v1 proposed).
- Hub: free-swim 3D Tide Pool or a menu scene?
- Steam release vs. itch.io first?
