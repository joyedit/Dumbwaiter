# Changelog

## 0.1.0 — 2026-07-11

Initial release.

### Features
- **Dumbwaiter Station** block in Oak and Aged variants, with detailed multi-element models and custom textures.
- **Dumbwaiter Rope Link** item: right-click one station to set the anchor, right-click a second to pair them (vertical range limit, horizontal offset limit, obstruction checks).
- **Lift transition**: operating a linked station plays the lift sound with a screen fade, then teleports you to the paired station — a teleport disguised as a moving platform.
- **Station labels**: write a label on a station with an ink & quill (reusable) or charcoal (consumed, vanilla labeled-chest economy). Labels render emphasized (bold, larger cream-colored font) in the block info overlay.
- **Block info overlay** shows the station's label and its link status — direction and distance to the paired station, including the paired station's label when it has one.
- Grid recipes for the stations and the rope link.
- Configurable via `ModConfig/dumbwaiter.json`.
- Safety checks: blocked-shaft detection, in-use lockout, settling cooldown, platform-presence check, broken block-entity recovery messaging.

### Requirements
- Vintage Story 1.22.0+
