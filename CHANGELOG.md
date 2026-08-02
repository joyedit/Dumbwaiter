# Changelog

## 0.3.5 — 2026-08-02

- Fixed linking being impossible since 0.3.3: the sneak + right-click
  "clear anchor" gesture fired before the link logic, so the second station
  click always cleared the anchor instead of completing the link. Clearing
  now only happens when sneak + right-clicking something that is *not* a
  dumbwaiter station; sneak + right-clicking a station sets the anchor or
  completes the link.
- The "anchor set" message now explains both gestures.

## 0.3.4 — 2026-07-30

- Fixed small player models (e.g. scaled-down races via Player Model lib, like
  the Avali) being unable to operate a station while standing on it. The
  station's selection box was a full cube, so a small rider's eye ended up
  *inside* it and the picking ray could never target the block — right-clicks
  silently went to whatever was behind it. The selection boxes now mirror the
  actual frame (platform, corner posts, top beams), leaving the rider space
  hollow.

## 0.3.3 — 2026-07-29

- Clearer link failures: separate messages for exceeding the vertical distance
  limit and the horizontal offset limit, each stating the actual distance, the
  limit, and the anchor's coordinates — so a stale anchor (e.g. left over from
  a previous install) is immediately visible.
- The "anchor set" message now includes the anchor's coordinates.
- Sneak + right-click with the rope link clears a set anchor, so a mis-clicked
  or stale anchor no longer requires crafting a new link.
- If the anchor station no longer exists, the failure message now says so and
  notes the anchor was cleared.

## 0.3.2 — 2026-07-28

- Raised the default `MaxVerticalDistance` from 192 to 250 blocks. Existing
  config files keep their stored value; delete the entry (or the file) to pick
  up the new default.

## 0.3.1 — 2026-07-23

- Compatibility release for Vintage Story 1.22.5. Rebuilt against the 1.22.5
  assemblies; no code changes.

## 0.3.0 — 2026-07-18

- **Cargo sends**: place a chest, basket or storage vessel on top of a linked station and right-click the station while standing beside it — the container is hauled to the cell above the paired station with its contents (and chest label) intact. The usual transition sounds play, without the screen fade.
  - Refused for trunks (two-block furniture) and reinforced/locked containers; the destination cell must be clear, re-checked at departure so the send jams safely (container stays put) if conditions change mid-transition.
- The station's top face is now solid, so containers that need ground support can be placed on the hoist frame.
- Sneak + right-click on a station is only intercepted when holding a writing tool (label editing); empty-handed or other-item sneak clicks pass through, so CarryOn pickup/put-down and block placement against the station work.

## 0.2.0 — 2026-07-11

First version published to the mod DB.

- Removed the unfinished Aged station variant (it had no recipe and shared the oak model). The Oak station's block codes are unchanged, so existing placements are unaffected.

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
