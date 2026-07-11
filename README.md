# Dumbwaiter

A Vintage Story 1.22+ mod adding a medieval-style dumbwaiter lift. Step onto the platform, pull the lever, and a brief immersive transition (sound, screen fade, server-side teleport) drops you at the paired station — typically at the top or bottom of a shaft.

The mechanic is a **teleport disguised as a lift** — a detailed block model, sound, and a fade transition carry the illusion that the platform actually moves.

## Features

- **Dumbwaiter Station** block (Oak and Aged variants) with a 27-element model and custom textures
- **Dumbwaiter Rope Link** item for pairing two stations
- Distance-scaled transition: short hops are quick, long hauls take the full ride
- Writable **station labels** (ink & quill or charcoal), shown emphasized in the block info overlay along with link direction and distance
- Safety checks: shaft obstruction scan, destination clearance, in-use lockout, settling cooldown, must-be-standing-on-platform check
- Fully configurable via `ModConfig/dumbwaiter.json`

## Using in-game

1. **Craft two stations** (3×3): 4× any planks in the corners (2 each), 2× metal nails & strips on the left/right middle, 1× plank slab in the center. (The Aged variant is currently creative-only.)
2. **Place them** at the two ends of your shaft — up to 192 blocks vertical, 4 blocks horizontal offset (configurable). Keep the column between them clear.
3. **Craft a Dumbwaiter Rope Link** (1×2 vertical): 2× rusty gear over 12× rope.
4. Right-click the first station with the link — *anchor set* (the anchor coordinates show in the item tooltip). Right-click the second — *linked*. The link is consumed.
5. **Ride it:** stand on a station and right-click. After a fade-and-sound transition scaled to the distance, you arrive at the other end. Each end then needs a moment to settle before the next trip.
6. **Label it (optional):** sneak + right-click a station while holding an ink & quill (reusable) or charcoal (consumed, with an 85% return chance on save — same economy as vanilla labeled chests). The label appears bold in the block info overlay, and the paired station shows it by name.

If something's wrong — station unlinked, shaft walled off, destination blocked, platform still settling — you get a chat message explaining what.

## Configuration

On first run, `~/.config/VintagestoryData/ModConfig/dumbwaiter.json` is written with defaults:

| Key | Default | Effect |
| --- | --- | --- |
| `MaxVerticalDistance` | 192 | Max vertical blocks between paired stations |
| `MaxHorizontalOffset` | 4 | Max horizontal misalignment when linking |
| `TransitionDuration` | 3.0 | Transition time (seconds) for a maximum-distance trip |
| `MinTransitionDuration` | 1.0 | Transition floor for the shortest trips |
| `UseCooldown` | 2.0 | Settling time (seconds) after a maximum-distance trip |
| `MinUseCooldown` | 0.5 | Cooldown floor for the shortest trips |
| `EnableScreenFade` | true | Set false for a faster, less immersive trip |
| `SoundVolume` | 0.8 | 0–1 multiplier for transition audio |

Transition time and cooldown both scale linearly with station distance between the min and max values.

## Building

Requires .NET SDK 10 and `$VINTAGE_STORY_PATH` pointing to your VS install (e.g. `~/Apps/vintagestory`).

```bash
dotnet build              # compiles to bin/Debug/Dumbwaiter.dll
./deploy.sh               # builds, zips with modinfo + assets + DLL, copies to ~/.config/VintagestoryData/Mods/
```

## Project layout

```
src/
├── DumbwaiterMod.cs                  ModSystem entry — block/BE/item registration, network channel, config
├── BlockDumbwaiterStation.cs         Interaction logic: operate, label, safety checks, teleport scheduling
├── BlockEntityDumbwaiterStation.cs   Pairing data, label storage/editing, block info text
├── ItemDumbwaiterLinker.cs           Rope link: anchor + pair flow
├── LiftTransition.cs                 Client-side fade renderer + sound playback
├── TransitionPacket.cs               Server → client transition trigger
└── Config/DumbwaiterConfig.cs

assets/dumbwaiter/
├── blocktypes/ itemtypes/ shapes/    Station block + linker item definitions
├── recipes/grid/                     Station and rope link recipes
├── sounds/lift-operate.ogg
├── textures/                         Block, item, and fade textures
└── lang/en.json
```

## Reference

- [`CHANGELOG.md`](CHANGELOG.md) — release history
- [`DESIGN.md`](DESIGN.md) — original design spec
- [`CLAUDE.md`](CLAUDE.md) — environment + conventions for AI-assisted development on this project
- VS API docs: <https://apidocs.vintagestory.at>
- JSON docs: <https://apidocs.vintagestory.at/json-docs/index.html>
