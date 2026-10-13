"""Bake the supplied Example 02 fighter and engine flicker into complete sprites.

Run from any directory with Python + Pillow. Source PNGs stay untouched; existing
Unity .meta files keep animation and prefab references stable.
"""

from pathlib import Path

from PIL import Image, ImageChops, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets/PNG"
OUTPUT = ROOT / "Assets/Art"


def main():
    hull = Image.open(SOURCE / "Example/02.png").convert("RGBA")
    hull = hull.resize((212, 181), Image.Resampling.LANCZOS)
    flame = Image.open(SOURCE / "Flame/06.png").convert("RGBA")
    previews = Image.new("RGB", (1024, 282), (22, 29, 40))
    labels = ImageDraw.Draw(previews)
    first_hull_region = None
    for index, height in enumerate((42, 52, 46, 56), start=1):
        frame = Image.new("RGBA", (256, 256))
        exhaust = flame.resize((32, height), Image.Resampling.LANCZOS)
        frame.alpha_composite(exhaust, (112, 196))
        frame.alpha_composite(hull, (22, 29))
        bounds = frame.getbbox()
        assert bounds and all(0 < p < 256 for p in bounds), bounds
        # Exhaust starts below this region; the visible hull never shifts.
        hull_region = frame.crop((0, 0, 256, 196))
        if first_hull_region is None:
            first_hull_region = hull_region
        else:
            assert ImageChops.difference(first_hull_region, hull_region).getbbox() is None
        target = OUTPUT / "Player" / f"player_idle_{index:02}.png"
        frame.save(target)
        previews.paste(frame, ((index - 1) * 256, 24), frame)
        labels.text(((index - 1) * 256 + 8, 6), f"Frame {index:02}", fill="white")
        print(f"{target.relative_to(ROOT)}: {frame.size}, bounds={bounds}")
    bullet = Image.open(SOURCE / "Bullets/01.png").convert("RGBA")
    bullet.save(OUTPUT / "Projectiles/laser.png")
    preview_path = ROOT / "Temp/Validation/example02-player-frames.png"
    preview_path.parent.mkdir(parents=True, exist_ok=True)
    previews.save(preview_path)
    print(f"Preview: {preview_path}")


if __name__ == "__main__":
    main()
