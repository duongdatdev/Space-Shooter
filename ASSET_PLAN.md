# Meteor Shooter Asset Plan

## Art direction

Clean stylized HD 2D arcade space art viewed from directly above. Shapes are compact, confident, and instantly readable at gameplay scale. The player uses the supplied broad-winged gray-and-red fighter from `Assets/PNG/Example/02.png`, with a cyan cockpit and a flickering golden exhaust from `Assets/PNG/Flame/06.png`. The palette also uses deep navy space, warm orange explosions, and gray-brown asteroids. Lighting, soft outlines, and saturation stay consistent across the set.

## Production set

| Asset | Raw layout | Frames | Anchor | Playback | Production output |
| --- | --- | ---: | --- | --- | --- |
| Player idle | 4 precomposed frames | 4 | center | 8 fps loop | `Assets/Art/Player/player_idle_01..04.png` |
| Player hurt | Runtime white/cyan flash using the accepted idle frames | 4-state blink | center | 12 fps during invulnerability | Uses idle sheet; no identity drift |
| Player explosion | 2x3 | 6 | center | 12 fps one-shot | `Assets/Art/FX/player_explosion.png` |
| Asteroid explosion | 2x3 | 6 | center | 14 fps one-shot | `Assets/Art/FX/asteroid_explosion.png` |
| Asteroid small/medium/large | 2x2 prop pack | 3 static cells | center | static | Individual PNGs under `Assets/Art/Asteroids` |
| Projectile | Supplied cyan bolt | 1 | center | static | `Assets/Art/Projectiles/laser.png` |
| Space background | square tileable image | 1 | center | infinite tiling with two-layer world-motion parallax | `Assets/Art/Background/space_background.png` |

## Identity and scale rules

- Player: one canonical gray-and-red broad-winged hull, bright cyan cockpit, and single golden exhaust, with the nose pointing straight up in every frame.
- Idle motion changes the golden exhaust flicker; hull geometry, camera, center, and scale remain fixed.
- Generated sprite subjects occupy roughly 60-70% of each raw cell with magenta padding on every side; supplied ship/projectile artwork follows the dimensions of its source pack.
- Explosion sheets contain only one centered effect per cell, with no baked asteroid/player body after the early frames.
- Asteroid variants share the same material and lighting but have distinct silhouettes and crater patterns.
- Production gameplay sizes: visible player hull about 1.4-1.6 world units tall; asteroid diameters about 0.65/0.95/1.3 units; projectile including glow about 0.26 x 0.48 units.

## Art sources and production workflow

Only three supplied source images are retained under `Assets/PNG`: `Example/02.png` (ship hull), `Flame/06.png` (exhaust), and `Bullets/01.png` (projectile). `Tools/compose_ship_assets.py` uses Pillow to composite the hull and exhaust at four flicker phases into `Assets/Art/Player/player_idle_01.png` through `player_idle_04.png`. The projectile output is `Assets/Art/Projectiles/laser.png`. The Unity refresh action configures these finished sprites and updates their existing prefab references.

Other game art is retained as finished production assets under `Assets/Art`. Temporary generation sheets and processing workspaces are not retained in the project.

For generated visual updates, create and review the artwork before placing accepted production PNGs under `Assets/Art`. Check identity, frame order, cropping, edge contact, scale consistency, transparency, and stray objects. The Unity importer uses Sprite texture type, alpha transparency, bilinear filtering, no compression, centered pivots, 128 pixels per unit for ship frames, 80 for the projectile, 128 for other gameplay sprites, and 155 for the background. Animation clips reference the individual ship frames.

## QC acceptance

- Transparent output and no visible magenta/fringe.
- No source subject is cropped and no processed frame touches an output edge.
- Exactly the requested grid/frame count; left-to-right, top-to-bottom order.
- Stable player identity, orientation, camera distance, center, and hull scale.
- Explosions expand then fade naturally without crossing cell boundaries.
- Static prop cells contain exactly one isolated asset each.
- Background remains low-noise and leaves the central playfield readable.
