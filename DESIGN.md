# Dumbwaiter Mod — Design Specification

## Concept
A medieval-style dumbwaiter lift built into cliff faces or tall structures. The player steps onto a wooden platform, pulls a lever, and is "lifted" to the destination via a brief immersive transition. Under the hood it's a teleport, but the visual presentation — detailed block models, sound design, and a transition effect — makes it feel like a real mechanical lift.

The goal is **maximum immersion to disguise the teleport**. Every detail matters: rope textures, wooden joints, iron hardware, ambient sound.

---

## Block Types

### 1. `dumbwaiter-station` (Top & Bottom)
The main interaction block placed at each end of the lift shaft.

**Visual Design — must be highly detailed:**
- **Platform base:** Thick oak planks (3-plank wide) with visible wood grain, slight gaps between planks, iron corner brackets
- **Frame:** Four upright oak posts (slightly weathered texture) connected by crossbeams at the top
- **Pulley housing:** At the top crossbeam — a carved wooden wheel with an iron axle pin, rope wrapped around it. The pulley wheel should have visible spoke detail
- **Rope:** Thick hemp rope running from the pulley down both sides of the platform, coiled loosely at the base. Use a slightly frayed texture
- **Counterweight:** A cluster of stone blocks (cobblestone texture) hanging on the opposite rope, visible beside the frame
- **Lever:** A wooden lever with an iron handle on one side of the frame — this is the interaction point
- **Safety rail:** Low wooden railing on three sides of the platform (open front for entry)
- **Guide rails:** Two vertical timber rails flanking the platform, with iron brackets bolted to the cliff/wall at intervals — suggests the platform travels along these

**Block Entity Data (ITreeAttribute):**
- `pairedPos` — BlockPos of the linked station (top or bottom)
- `stationRole` — `"top"` or `"bottom"`
- `label` — optional player-set name, shown in the hover tooltip (max 4 lines / 200 chars)
- `inUse` — bool, prevents double-activation
- `lastUsedTime` — double, cooldown tracking

**Variants:**
- `dumbwaiter-station-oak` (default)
- `dumbwaiter-station-aged` (darker, more weathered — for old ruins aesthetic)
- Future: `dumbwaiter-station-pine`, `dumbwaiter-station-acacia`

### 2. `dumbwaiter-shaft` (Decorative Filler)
Purely decorative blocks placed between the two stations to create the visual shaft.

**Visual Design:**
- **Guide rail segment:** Two vertical timbers with iron bracket hardware, matching the station's guide rails
- **Rope segment:** Visible rope running through the center, taut
- **Wall mount variant:** Half-block that sits flush against a cliff face with iron bolts visible

These are cosmetic only — no block entity, no interaction. They just make the shaft between stations look convincing.

**Variants:**
- `dumbwaiter-shaft-open` — freestanding, both guide rails visible
- `dumbwaiter-shaft-wall` — one side flush for cliff mounting

### 3. `dumbwaiter-winch` (Optional Top Mechanism)
A decorative block placed at the very top of the shaft, above the top station.

**Visual Design:**
- Large horizontal drum/barrel with rope wound around it
- Hand crank on one side (iron)
- Wooden A-frame support straddling the shaft opening
- Adds to the illusion that something mechanical is happening up top

---

## Interaction Flow

### Linking Two Stations
1. Player crafts two `dumbwaiter-station` blocks
2. Places one at the bottom, one at the top
3. Player crafts a `dumbwaiter-linker` item (iron + rope recipe) — a rope coil item
4. Right-click the bottom station with the linker → chat message: *"Dumbwaiter anchor set."*
5. Right-click the top station with the linker → chat message: *"Dumbwaiter linked! Distance: {N} blocks."*
6. Both stations now store each other's `pairedPos`
7. The linker item is consumed on successful link
8. **Max vertical distance:** 64 blocks (configurable in mod config)
9. **Max horizontal offset:** 4 blocks (allows slight diagonal shafts)

### Labeling a Station
Labels live in the hover tooltip — no model changes, no sign-placement worries, and room for a real description.

1. **Sneak + right-click** a station **holding an ink and quill or charcoal** → opens the vanilla sign text dialog. Ink and quill is a reusable writing tool (never consumed). Charcoal (any pigment item) follows the vanilla labeled-chest economy: one is taken while writing, returned on cancel, 85% chance returned on save. Sneak + empty hand shows a hint about needing a writing tool. The tooltip label renders emphasized (bold, larger, parchment-gold) with the link-status line dimmed below it
2. Can be done any time after placement (before or after linking); edit or clear the same way
3. The tooltip shows the station's own label plus link status, e.g.:
   > Oak Dumbwaiter Station
   > Kitchen — sends meals down
   > Linked to "Root Cellar" — 14 blocks below
4. The linked line uses the paired station's label (first line only) when it has one, otherwise falls back to *"Linked to a station N blocks below"*
5. Implementation: reuses `GuiDialogBlockEntityTextInput` + the sign `SaveText` block-entity packet from vanilla — no custom GUI or network channel

### Using the Lift
1. Player right-clicks the **lever** on a linked station
2. **Validation checks:**
   - Is it linked? (paired station exists and is loaded)
   - Is it already in use? (cooldown not expired)
   - Is the destination clear? (no blocks where player would arrive)
3. **Immersive sequence begins (client-side):**
   - **t=0.0s:** Lever click sound. Lever visually toggles (if animated, otherwise just sound)
   - **t=0.3s:** Chain/rope tension sound — a deep metallic creak
   - **t=0.5s:** Platform rumble — low wooden groan
   - **t=0.8s:** Movement sound begins — rhythmic pulley squeaking, rope sliding
   - **t=0.8s:** Screen begins to darken (fade to black over 0.7s)
   - **t=1.5s:** Full black — **TELEPORT HAPPENS HERE** (server-side `entity.TeleportTo()`)
   - **t=1.5s:** Movement sounds continue through black
   - **t=2.2s:** Platform arrival thud — heavy wood-on-wood impact
   - **t=2.2s:** Fade back in from black over 0.8s
   - **t=2.5s:** Rope settling sound — slack rattling
   - **t=3.0s:** Sequence complete, player has full control
4. **Total transition time: ~3 seconds** (configurable)
5. **Cooldown:** 2 seconds after arrival before either station can be used again

### Failure States
- **Unlinked station:** Chat message: *"This dumbwaiter isn't connected to anything."*
- **Destination obstructed:** Chat message: *"Something is blocking the other end."*
- **Destination unloaded:** Chat message: *"The other station is too far away."*
- **Cooldown active:** Chat message: *"The dumbwaiter is still settling..."*
- **Already in use:** Chat message: *"The dumbwaiter is already in motion."*

---

## Sound Design

Sounds are critical — they're 80% of the immersion. Each should be a separate `.ogg` file for mixing flexibility.

| Sound File | Description | Duration | Notes |
|---|---|---|---|
| `lever-pull.ogg` | Iron lever clunk with wooden frame resonance | 0.4s | Sharp metallic click, slight wood echo |
| `rope-tension.ogg` | Hemp rope going taut under load | 0.5s | Creaking, straining fiber sound |
| `platform-groan.ogg` | Wooden platform shifting under weight | 0.3s | Low creak, subtle |
| `pulley-loop.ogg` | Rhythmic pulley wheel turning with rope | 2.0s | Loopable; squeaky iron axle + rope friction |
| `counterweight-drop.ogg` | Stone counterweight descending | 1.0s | Heavy, muffled stone movement + chain |
| `platform-arrive.ogg` | Platform hitting the stop at destination | 0.5s | Solid wood thud + slight bounce |
| `rope-settle.ogg` | Slack rope and chains settling after stop | 0.8s | Metallic jingling, rope slap |

**Sound placement:** All sounds should play at the station's block position with appropriate falloff so nearby players hear the lift operating.

---

## Crafting Recipes

### Dumbwaiter Station
```
Grid (3x3):
[ plank ] [       ] [ plank ]
[ nails ] [ slab  ] [ nails ]
[ plank ] [       ] [ plank ]

Where:
- plank = any plank via wildcard (game:plank-*), quantity 2 per corner = 8 total
- nails = iron nails & strips (game:metalnailsandstrips-iron), quantity 2 per cell = 4 total
- slab  = any plank slab via wildcard (game:plankslab-*), quantity 1

Output: 1x dumbwaiterstation-oak (two needed per working setup)
```

### Dumbwaiter Linker
```
Grid (1x2):
[ temporal-gear ]
[ rope x12      ]

Where:
- temporal-gear = game:gear-temporal, quantity 1 — the teleport gate
- rope          = game:rope, quantity 12

Output: 1x dumbwaiter-linker (consumed on successful pairing)
```

### Dumbwaiter Shaft (decorative)
```
Grid (3x1):
[ plank ] [ rope ] [ plank ]

Output: 2x dumbwaiter-shaft
```

### Dumbwaiter Winch (decorative)
```
Grid (3x2):
[ iron  ] [ plank ] [ iron  ]
[ plank ] [ rope  ] [ plank ]

Output: 1x dumbwaiter-winch
```

---

## File Structure

```
dumbwaiter/
├── CLAUDE.md
├── DESIGN.md                          ← this file
├── Dumbwaiter.csproj
├── deploy.sh
├── modinfo.json
├── src/
│   ├── DumbwaiterMod.cs               ← ModSystem entry point
│   ├── BlockDumbwaiterStation.cs       ← Block class with interaction handling
│   ├── BlockEntityDumbwaiterStation.cs ← Block entity with pairing data
│   ├── ItemDumbwaiterLinker.cs         ← Linker item for pairing stations
│   ├── LiftTransition.cs              ← Client-side transition manager (fade, sounds, timing)
│   └── Config/
│       └── DumbwaiterConfig.cs         ← Configurable values (max distance, cooldown, transition time)
├── assets/
│   └── dumbwaiter/
│       ├── blocktypes/
│       │   ├── dumbwaiter-station.json
│       │   ├── dumbwaiter-shaft.json
│       │   └── dumbwaiter-winch.json
│       ├── itemtypes/
│       │   └── dumbwaiter-linker.json
│       ├── recipes/
│       │   └── grid/
│       │       ├── dumbwaiter-station.json
│       │       ├── dumbwaiter-linker.json
│       │       ├── dumbwaiter-shaft.json
│       │       └── dumbwaiter-winch.json
│       ├── shapes/
│       │   ├── block/
│       │   │   ├── dumbwaiter-station.json     ← detailed multi-element shape
│       │   │   ├── dumbwaiter-shaft-open.json
│       │   │   ├── dumbwaiter-shaft-wall.json
│       │   │   └── dumbwaiter-winch.json
│       │   └── item/
│       │       └── dumbwaiter-linker.json
│       ├── textures/
│       │   ├── block/
│       │   │   ├── oak-plank-weathered.png      ← 32x32, detailed grain
│       │   │   ├── oak-plank-aged.png           ← darker variant
│       │   │   ├── iron-bracket.png             ← rustic iron hardware
│       │   │   ├── iron-axle.png                ← pulley axle detail
│       │   │   ├── rope-hemp.png                ← thick braided rope
│       │   │   ├── rope-frayed.png              ← worn rope ends
│       │   │   ├── counterweight-stone.png      ← cobblestone cluster
│       │   │   ├── pulley-wheel.png             ← wooden wheel with spokes
│       │   │   └── guide-rail.png               ← vertical timber texture
│       │   └── item/
│       │       └── rope-coil.png                ← linker item icon
│       ├── sounds/
│       │   ├── lever-pull.ogg
│       │   ├── rope-tension.ogg
│       │   ├── platform-groan.ogg
│       │   ├── pulley-loop.ogg
│       │   ├── counterweight-drop.ogg
│       │   ├── platform-arrive.ogg
│       │   └── rope-settle.ogg
│       └── lang/
│           └── en.json
```

---

## Model Detail Guidelines

Since the teleport mechanic means the **visual presentation is everything**, the block models need to be significantly more detailed than typical VS blocks.

### Station Model Elements (dumbwaiter-station.json)
The shape file should contain these named elements as separate cuboid groups:

1. **platform_base** — 3 planks side by side, each 5px wide × 16px deep × 2px tall, with 0.5px gaps
2. **platform_edge_iron** — thin iron L-brackets at all four corners (1px × 1px × 2px)
3. **post_fl, post_fr, post_bl, post_br** — four vertical posts, 2px × 2px × 16px, slightly tapered at top
4. **crossbeam_front, crossbeam_back** — horizontal beams connecting top of posts, 2px × 2px
5. **crossbeam_left, crossbeam_right** — side beams
6. **pulley_wheel** — centered on top crossbeam, 4px diameter circle approximated with rotated cuboids (8-sided minimum)
7. **pulley_axle** — iron rod through the wheel center, 1px × 6px
8. **rope_left, rope_right** — vertical rope elements from pulley down each side, 1px × 1px cross-section
9. **rope_coil_base** — small coiled rope pile at platform level (decorative cluster of small cuboids)
10. **counterweight** — 3px × 3px × 4px stone block cluster hanging on one rope side, offset from platform
11. **lever_post** — 1px × 1px × 6px vertical on one side
12. **lever_handle** — angled iron bar, 1px × 1px × 4px, rotated ~30 degrees
13. **rail_left, rail_right** — low safety rails, 1px × 16px × 4px on sides
14. **guide_rail_l, guide_rail_r** — full-height vertical rails on back/wall side, 2px × 2px × 16px
15. **guide_bracket_l, guide_bracket_r** — iron bracket details on guide rails, 2px × 1px × 1px

### Texture Requirements
- **Resolution:** 32×32 for block faces (double the VS default 16×16 for extra detail)
- **Style:** Must match VS vanilla aesthetic — painterly, slightly desaturated, medieval
- **Wood grain:** Visible but not photorealistic. Follow the style of VS oak plank textures
- **Iron/metal:** Dark gunmetal with slight rust spots, matching VS iron textures
- **Rope:** Tan/brown braided texture, slight fraying at edges

---

## Configuration (DumbwaiterConfig.cs)

```csharp
public class DumbwaiterConfig
{
    // Maximum vertical distance between paired stations
    public int MaxVerticalDistance { get; set; } = 64;

    // Maximum horizontal offset (allows slightly angled shafts)
    public int MaxHorizontalOffset { get; set; } = 4;

    // Total transition duration in seconds
    public float TransitionDuration { get; set; } = 3.0f;

    // Cooldown between uses in seconds
    public float UseCooldown { get; set; } = 2.0f;

    // Whether to play the screen fade effect
    public bool EnableScreenFade { get; set; } = true;

    // Sound volume multiplier (0.0 - 1.0)
    public float SoundVolume { get; set; } = 0.8f;
}
```

---

## Implementation Notes

### Teleport Mechanism
- Use `entity.TeleportTo(pairedPos.ToVec3d().Add(0.5, 1.0, 0.5))` — offset to center of platform, one block above station base
- Teleport must happen **server-side only**; client handles the transition visuals
- Use a network packet to coordinate: server sends "begin transition" → client fades → client sends "ready" → server teleports → server sends "arrive" → client fades in

### Screen Fade (Client-Side)
- Register a `IRenderer` with `EnumRenderStage.Ortho`
- Draw a full-screen black quad with variable alpha
- Animate alpha: 0 → 1 over 0.7s, hold at 1 for 0.7s, 1 → 0 over 0.8s
- Use `capi.Event.RegisterGameTickListener` for the animation loop

### Pairing Data Persistence
- Store `pairedPos` in the block entity's `ITreeAttribute`
- On block break: clear the paired station's reference too (notify both ends)
- On world load: validate that paired station still exists

### Multiplayer Considerations
- `inUse` flag prevents two players activating simultaneously
- Cooldown is per-station, tracked server-side
- Transition visuals are per-player (client-side renderer)

### Sound Playback
- Use `capi.World.PlaySoundAt()` for positioned audio
- `pulley-loop.ogg` should be played with looping enabled during the transition
- All sounds use the station's BlockPos as origin

---

## Language File (en.json)

```json
{
    "block-dumbwaiter-station-oak": "Oak Dumbwaiter Station",
    "block-dumbwaiter-station-aged": "Aged Dumbwaiter Station",
    "block-dumbwaiter-shaft-open": "Dumbwaiter Shaft",
    "block-dumbwaiter-shaft-wall": "Dumbwaiter Shaft (Wall Mount)",
    "block-dumbwaiter-winch": "Dumbwaiter Winch",
    "item-dumbwaiter-linker": "Dumbwaiter Rope Link",
    "dumbwaiter-anchor-set": "Dumbwaiter anchor set.",
    "dumbwaiter-linked": "Dumbwaiter linked! Distance: {0} blocks.",
    "dumbwaiter-not-linked": "This dumbwaiter isn't connected to anything.",
    "dumbwaiter-obstructed": "Something is blocking the other end.",
    "dumbwaiter-too-far": "The other station is too far away.",
    "dumbwaiter-settling": "The dumbwaiter is still settling...",
    "dumbwaiter-in-use": "The dumbwaiter is already in motion.",
    "dumbwaiter-link-too-far": "These stations are too far apart to link.",
    "dumbwaiter-link-failed": "Could not link — is there a station at both ends?"
}
```

---

## Future Enhancements (Post-MVP)
- **Item transport mode:** Send items up/down without the player riding (actual dumbwaiter use case — inventory slot on the platform)
- **Animated rope:** Client-side particle rope that visually moves during transition
- **Platform entity:** Replace teleport with a rideable entity for short distances (complex but possible)
- **Multi-stop shaft:** More than two stations on the same shaft with a selection UI
- **Sound variety:** Random variation on pulley squeaks and wood creaks to avoid repetition
- **Redstone-style automation:** Activate via mechanical power (if compatible with VS mechanics)
