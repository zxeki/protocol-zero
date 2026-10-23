# Protocol Zero — Art Bible

## Source

All game art comes from `Protocol Zero_ Cyberpunk Pixel Asset Pack.png` (1536x1024, repo root),
a custom-made pixel asset pack created for this project (see README). No third-party art.
The sheet is sliced into `assets/**/*.png` by `tools/AssetTool` (see *Extraction* below).

## Display rules

- Logical resolution 480x270, window 960x540, `canvas_items` stretch x2, nearest filtering,
  pixel snap on (see `project.godot`).
- **All pack art is displayed at native x1 logical scale.** No upscaling, no downscaling —
  fractional scaling of pixel art is forbidden. Panels/rows are sized to fit art, never the
  reverse.
- Procedural fallback art (`src/Util/PixelArt.cs`) remains for any missing PNG so the game
  never renders a hole.

## Palette

| Use | Hex |
|---|---|
| Sheet background (extracted away) | `#011427` `#001020` `#0c111f` `#000e1c` |
| Striker accent | `#e04848` |
| Volt accent | `#4a9eff` |
| Hawk accent | `#3ecfb2` |
| Vex accent | `#e05ac8` |
| Torque accent | `#f0a030` |
| UI text slate | `#8c9bb0` |
| Credits gold | `#ffd75e` |
| Rarity common / rare / epic / legendary | `#9fb2c8` / `#4a9eff` / `#c06ae0` / `#ffd75e` |

Enemy faction palette: gunmetal gray + red optics (all four enemies).
Each operator's sheet art matches their `accent_color` exactly.

## Inventory (`assets/`)

### Sprites — operators (`operators/{id}/`)
5 operators (striker, volt, hawk, vex, torque) x 5 rows (idle, move, attack, hurt, defeat).
4 frames per row, native ~16-27 px tall, idle ≈ 25 px. Integrated as animated combat
sprites (idle loop, attack/hurt/defeat poses).

### Sprites — enemies (`enemies/{id}/`)
| id | frames/row | rows | native |
|---|---|---|---|
| watcher | 2 | idle, move, attack, hurt, defeat | ~15 px |
| hunter_drone | 3 | idle, move, attack, hurt, defeat | ~20 px |
| enforcer | 3 | idle, move, attack, hurt, defeat | ~25 px |
| the_warden | 1 | idle, attack | ~75-86 px (boss; chain prop omitted) |

### Portraits (`portraits/`)
Full-body bust art, 5 operators, ~90x140. **Available, not yet wired** (no panel large
enough at native scale; reserved for a future full-screen roster/menu).

### Equipment icons (`weapons/`, `armors/`, `implants/`)
Native 20-77 px wide, named exactly after `data/equipment.json` ids
(e.g. `weapons/rail_driver.png`). Used in Armory slot cards at native size.
Note: `arc_blade` uses a tech-tool stand-in (pack has no melee blade art).

### Ability icons (`operators/{id}/ability_0..3`)
20 icons (4 per operator), ~40x40, framed rounded-square, glyph in operator accent color.
Row order per sheet: ability_0 = signature/active, ability_3 = ultimate-flavored.
Used in the Armory operator selector row (icon strip beside the operator buttons).

### Rarity badges (`rarity/`)
common / rare / epic / legendary, ~34x34. Available; rarity is currently conveyed by
border color in Armory (badges reserved for future use).

### VFX (`vfx/`)
- `hit_orange`, `hit_blue`, `burst`, `hit_teal` — 35-55 px impact bursts (death bursts in game).
- `status_0..5` — 23-25 px status marks (slow/burn/etc). Available, not yet wired.

### UI (`ui/`)
`menu_0..4` menu glyphs (squad, inventory, upgrades, recruitment, settings), ~22 px.
Available, not yet wired.

### HUD (`hud/`)
`coin`, `xp`, `drop` reward icons ~30 px. Available (HUD uses text labels at 9 px font;
these are too large for the current label rows).

### Tiles (`tile/`)
7 wall/floor panels, ~37-42 px (frame, X-plate, hazard, dark, bracket, core, glow).
Panel-repeat friendly (square edges).

### Props (`props/`)
21 industrial props: crates, barrels (teal/red/blue/rad), pillars, bins, rails.
Native 19-79 px. Placed at x1 scale as arena decoration (crate ≈ 1.75x operator height —
chest-high cover proportions).

### Arenas (`arenas/`)
Three scene paintings: `rooftop` 163x126, `factory` 162x138, `dock` 161x81.
**Reference art only** — at 480x270 no integer scale fits, so they are not drawn in the
battlefield. Arena identity in game comes from palette + prop selection instead
(see `Battlefield`). The scenes may back a future full-screen sector-select UI.

### Unused sheet regions (available)
Projectiles (5 bolts), skill effects (5), consumables (16), pack demo HUD mockup,
spritesheet demos (bottom strip).

## Style rules

1. Black-ish outlines, saturated accent cores, cool dark metal bases — keep that contrast.
2. Glow effects sit on dark navy backgrounds; on gameplay panels keep backgrounds within
   the sheet background family so glow art reads correctly.
3. One accent color per operator across sprite, ability icons, recruit icon, HUD card border.
4. New art must be sliced from the pack via AssetTool; hand-authored pixels are a last
   resort and must match the palette table above.

## Extraction pipeline

```
dotnet run --project tools/AssetTool -- sample <pack> <x,y>...            # probe palette
dotnet run --project tools/AssetTool -- boxes  <pack> <x,y,w,h> [minW minH minFill]
dotnet run --project tools/AssetTool -- frames <pack> frames_spec.json manifest_frames.json
dotnet run --project tools/AssetTool -- extract <pack> <manifest.json> assets
dotnet run --project tools/AssetTool -- montage <manifest.json> assets <out.png> [scale]
```

- Background model: dark flats (histogram count >= 400, max channel <= 100), match
  distance <= 24; extraction flood-fills border-reachable background to alpha 0,
  interior pixels stay opaque; output is trimmed to content.
- Manifests live in `tools/AssetTool/manifest_frames.json` (grid-sliced sprites) and
  `manifest_manual.json` (hand-mapped icons/props). Re-run extract after edits, then
  montage to verify.
