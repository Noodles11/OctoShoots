# Clementine's Quest 3D

A first-person roguelite shooter in a stylized 3D reef. Design: [`docs/DESIGN-3D.md`](docs/DESIGN-3D.md)
(on top of [`docs/DESIGN-2D.md`](docs/DESIGN-2D.md)).

**Done:** step 1 (feel prototype, see [`docs/M0-CHECKLIST.md`](docs/M0-CHECKLIST.md)), step 2
(Core port: stats, items, shot modifiers, synergies, transformations, actives, pools, seeds, saves) and
step 3 (open-sea reefs: seabed, reef formations with caves, sparse SDF, chunked meshing, validation,
craters, ink bombs; items as pearls in shells, the inventory tentacle, chests, pickups, drops;
bubbles thrown from the right tentacle, regrowing on its suckers;
sea anemones and the first mob: clownfish ninjas hiding among neutral clownfish;
circular minimap; vision mist). The game starts in a generated reef. **F1** opens the debug panel:
level (depth, reef, grey-box cave, teleports), item picker, run seed, save/load/export, tuning.

## Running

```bash
godot
```

From this folder, `godot.cmd` opens the editor with the real Godot .NET executable
(`godot --run` starts the game). `dotnet test` runs the Core tests without Godot.

## Layout

```
project.godot, OctoShoots.csproj   Godot 4.7 (.NET) project; compiles src/Game
data/items.json   60 items, 14 synergies, 5 transformations (validated on load)
src/Core/         pure C# simulation (no Godot types)
  Sim/              fixed 60 Hz World: player, creatures, weapon + shot modifiers, statuses, actives, nests
  Items/            item model, catalog, loadout (stat order, synergies, transformations), pools, captions
  Run/              seed codes and per-system RNG streams
  Saves/            versioned save format, migrations, export codes
  Gen/              open-sea reef generator: seabed, formations, caves, secrets, loot plan, validation
  Loot/             pickups, shells, chests, drop tables
  Terrain/          voxel SDF (craters), grey-box cave, surface-nets meshing, reachability
src/Core.Tests/   xUnit tests for Core
src/Game/         Godot layer: scene, camera + viewmodel, terrain meshes, FX, HUD, debug panel
assets/shaders/   terrain, bubble, pearl, mist, sea surface and screen shaders
docs/             design documents and milestone checklists
```

## Requirements

- Godot 4.7.2 **.NET** edition
- .NET 8 SDK
