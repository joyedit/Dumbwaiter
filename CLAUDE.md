# Dumbwaiter Mod — Claude Code Project Context

## Developer
- **Handle:** Feyd (mods.vintagestory.at)
- **Style:** AI-assisted modding; strong on concepts, leans on AI for C# implementation details
- **Communication:** Direct, no filler. Step-by-step guidance preferred over full rewrites. Confirm before irreversible actions.
- **Colorblind:** Avoid relying on color alone for feedback or UI cues.

## Environment
- **OS:** Pop!_OS 24.04 LTS (COSMIC desktop)
- **Username:** deftones
- **VS Installation:** `~/Apps/vintagestory`
- **VINTAGE_STORY_PATH:** Set in `~/.bashrc` → `~/Apps/vintagestory`
- **Mod Source Directory:** `~/Dev/dumbwaiter/`
- **Mods Output:** `~/.config/VintagestoryData/Mods/`
- **.NET SDK:** 10.0.201 at `/usr/share/dotnet/sdk/`
- **Target Framework:** `net10.0` (VS 1.22+)

## Build & Deploy
```bash
# Build
dotnet build

# Deploy — copy zip to mods folder
./deploy.sh
```
The `deploy.sh` script should:
1. `dotnet build` the project
2. Create a zip with the correct structure: `mod-root/` containing `modinfo.json`, `assets/`, and the built DLL from `bin/`
3. Copy the zip to `~/.config/VintagestoryData/Mods/`

## VS Modding Conventions
- **Mod ID format:** `dumbwaiter` (lowercase, no spaces)
- **Namespace:** `Dumbwaiter`
- **modinfo.json:** Must include `dependencies` with `game` version requirement
- **API reference repos:**
  - `github.com/anegostudios/vsapi`
  - `github.com/anegostudios/vssurvivalmod`
  - `github.com/anegostudios/vsessentialsmod`
- **API docs:** `apidocs.vintagestory.at`
- **JSON docs:** `apidocs.vintagestory.at/json-docs/index.html`
- **Always confirm target framework and VINTAGE_STORY_PATH before starting a build session**

## VS 1.22 Gotchas
- Entity position: use `entity.Pos` — `entity.ServerPos` is marked obsolete in 1.22 ("Use Pos instead", CS0618)
- Build against net10.0 DLLs
- Include `<AllowMissingPrunePackageData>true</AllowMissingPrunePackageData>` in `.csproj`

## Project Architecture
This mod uses a **teleport-disguised-as-lift** approach:
- Player interacts with a dumbwaiter block at top or bottom
- Brief immersive delay with sound effects (chain rattle, wood creak, pulley squeal)
- Player is teleported to the paired block location
- The visual design must be exceptional since we're faking the movement

See `DESIGN.md` for the full specification.

## Asset Generation
- **Sound effects:** Generate with ElevenLabs sound effects API or source from freesound.org (CC0)
- **Textures:** Can use fal.ai for base generation, then clean up manually
- **Block models:** VS JSON format — see vsmodelcreator or hand-author

## Code Style
- Prefer `ICoreServerAPI` / `ICoreClientAPI` split
- Register blocks via `RegisterBlockClass` in `StartServerSide` / `StartClientSide`
- Use `ITreeAttribute` for block entity data persistence
- Log sparingly with `Mod.Logger`
