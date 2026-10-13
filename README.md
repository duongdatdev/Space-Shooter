# Meteor Shooter

Meteor Shooter is a compact 2D arcade game for Unity. Pilot a starfighter freely through space, shoot asteroids and colorful rotating pixel-star hazards arriving from every direction, build score, and survive an increasingly fast barrage with three lives. The camera follows the player through the open world.

## Requirements

- Unity `6000.6.1f1`
- Windows, macOS, or Linux Unity Editor with the project packages restored
- Active Input Handling is set to the Input System package

## Open and run

1. Open this repository folder in Unity Hub with Unity `6000.6.1f1`.
2. Open `Assets/Scenes/MainMenu.unity`.
3. Press Play and select **PLAY**.

The build scene order is already configured as `MainMenu` then `Gameplay`.

## Controls

- `W` / `A` / `S` / `D` or the Arrow keys: move in any direction
- Mouse: aim; left click to fire (hold for repeated fire with cooldown)
- `Escape`: pause or resume

Diagonal movement is normalized to match straight-line speed. The ship turns toward the mouse while movement stays in world directions. The infinite tiled nebula and brighter nearby stars shift at different rates as the camera follows the ship. Asteroids and rotating pixel-star hazards enter from outside the current view and travel through the player's vicinity; they damage the ship on contact and can be destroyed for points. The best score is saved locally and displayed on the Main Menu and Game Over screen.

## Build

Use **File > Build Profiles**, select a desktop profile, then choose **Build**. The default player resolution is 720×1280; the background and UI also adapt to other aspect ratios.

## Project structure

- `Assets/Art`: accepted production sprites, background, and eight pixel-star hazard variants
- `Assets/Animations`: generated animation clips/controllers
- `Assets/Prefabs`: player, projectile, three asteroids, rotating star hazard, and explosion FX
- `Assets/Scenes`: `MainMenu` and `Gameplay`
- `Assets/Scripts`: focused gameplay, player, UI, and system components
- `Assets/Tests/EditMode`: lightweight gameplay-configuration tests
- `Assets/Tests/PlayMode`: spawn, collision, and high-score flow tests
- `Assets/Editor/MeteorShooterBuilder.cs`: reproducible scene/prefab/animation builder and scoped art refresh actions
- `Tools/compose_ship_assets.py`: regenerates the four supplied-art ship frames from the retained source pack
- `ASSET_PLAN.md`: art direction, generation layouts, import contract, and QC criteria

## Art and animation workflow

Three supplied source images are retained under `Assets/PNG`: `Example/02.png` for the ship hull, `Flame/06.png` for its flickering exhaust, and `Bullets/01.png` for the cyan projectile. `Tools/compose_ship_assets.py` uses Pillow to reproduce the four ship frames. Other visible game art is stored as finished assets under `Assets/Art`; temporary generation sheets are not part of the project.

The player fighter uses four supplied-art idle frames at 8 fps. Player and asteroid explosions use six-frame one-shots at 12 and 14 fps. Asteroids use three distinct static sprites with runtime rotation; pixel-star hazards use eight colors and rotate in play. See `ASSET_PLAN.md` for the complete source and QC details.

## Regenerating project content

After scripts and art have imported without compile errors, run **Meteor Shooter > Build Complete Game** in the Unity Editor. This rebuilds production prefabs, animation assets, both scenes, build settings, and desktop defaults from the accepted art. To add hazards and the Main Menu high-score label to an existing project without a full rebuild, use **Meteor Shooter > Upgrade Star Hazard and Main Menu High Score**. To update the eight pixel-star variants or the four player idle frames and cyan projectile in their existing prefabs without changing scene references, use **Meteor Shooter > Refresh Pixel Star Hazard Variants** or **Meteor Shooter > Refresh Player Ship Art**.

## Verification

Run EditMode tests from **Window > General > Test Runner**. The project was also checked in Play Mode for menu/HUD rendering, asteroid spawning, projectile collision and scoring, single-life damage with invulnerability, pause/resume, Game Over flow, and console errors.

## Known limitations

The game intentionally has one weapon and one survival loop. It uses simple generated visual FX and no background music; this keeps the project small and avoids third-party or copyrighted audio dependencies.
