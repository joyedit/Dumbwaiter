# Dumbwaiter

A Vintage Story 1.22+ mod adding a medieval-style dumbwaiter lift. Step onto a wooden platform, pull the lever, and a brief immersive transition (sound, screen fade, server-side teleport) drops you at the paired station — typically at the top or bottom of a shaft.

The mechanic is a **teleport disguised as a lift** — detailed block models, layered sound design, and a fade transition carry the illusion that the platform actually moves.

## Status — early development

| Component | State |
| --- | --- |
| Station block: JSON + 27-element shape + 8 placeholder textures | renders in-world |
| `ModSystem` entry, block + block-entity class registration | done |
| `BlockEntityDumbwaiterStation` (paired pos, role, in-use, last-used) | data model only, no behavior |
| `BlockDumbwaiterStation.OnBlockInteractStart` | stub (no lever logic) |
| `ItemDumbwaiterLinker` (rope-coil pairing item) | C# stub — no JSON, texture, or recipe |
| `LiftTransition` client renderer + sound timeline | stub (no fade, no audio) |
| Network packets (begin → ready → teleport → arrive) | not implemented |
| Sound files (`lever-pull.ogg`, `pulley-loop.ogg`, …) | not authored |
| Recipes (station, linker, shaft, winch) | not authored |
| `dumbwaiter-shaft`, `dumbwaiter-winch` decorative blocks | not started |
| `DumbwaiterConfig` | loaded from `ModConfig/dumbwaiter.json` |

Full spec — including planned interaction flow, sound timeline, model element list, and network protocol — lives in [`DESIGN.md`](DESIGN.md).

## Building

Requires .NET SDK 10 and `$VINTAGE_STORY_PATH` pointing to your VS install (e.g. `~/Apps/vintagestory`).

```bash
dotnet build              # compiles to bin/Debug/Dumbwaiter.dll
./deploy.sh               # builds, zips with modinfo + assets + DLL, copies to ~/.config/VintagestoryData/Mods/
```

## Using in-game

**Right now: placement only.** In creative, search "dumbwaiter" in inventory — place an Oak or Aged Dumbwaiter Station. The block renders but the lever does nothing yet.

**Planned flow** (per DESIGN.md):
1. Place two stations, one at the top of your shaft, one at the bottom. Max 64 blocks vertical, 4 blocks horizontal offset.
2. Craft a Dumbwaiter Rope Link (1 iron nail + 2 rope, vertical).
3. Right-click the bottom station with the linker — *anchor set.*
4. Right-click the top station with the linker — *linked, distance N.* Link item is consumed.
5. Step onto either platform, right-click the lever. ~3-second transition (lever clunk → rope tension → pulley loop + screen fade → arrival thud → fade in) drops you at the paired station. 2-second cooldown.

## Project layout

```
src/
├── DumbwaiterMod.cs                  ModSystem entry
├── BlockDumbwaiterStation.cs         Block class
├── BlockEntityDumbwaiterStation.cs   BE — pairing data
├── ItemDumbwaiterLinker.cs           Linker item (stub)
├── LiftTransition.cs                 Client fade + sound timeline (stub)
└── Config/DumbwaiterConfig.cs

assets/dumbwaiter/
├── blocktypes/dumbwaiter-station.json
├── shapes/block/dumbwaiter-station.json
├── textures/block/                   8 placeholder PNGs (real art TBD)
└── lang/en.json
```

## Configuration

On first run, `~/.config/VintagestoryData/ModConfig/dumbwaiter.json` is written with defaults:

| Key | Default | Effect |
| --- | --- | --- |
| `MaxVerticalDistance` | 64 | Max blocks between paired stations |
| `MaxHorizontalOffset` | 4 | Max horizontal misalignment when linking |
| `TransitionDuration` | 3.0 | Total transition time, seconds |
| `UseCooldown` | 2.0 | Seconds between uses |
| `EnableScreenFade` | true | Set false for a faster, less immersive trip |
| `SoundVolume` | 0.8 | 0–1 multiplier for transition audio |

## Reference

- [`DESIGN.md`](DESIGN.md) — full design spec
- [`CLAUDE.md`](CLAUDE.md) — environment + conventions for AI-assisted development on this project
- VS API docs: <https://apidocs.vintagestory.at>
- JSON docs: <https://apidocs.vintagestory.at/json-docs/index.html>
