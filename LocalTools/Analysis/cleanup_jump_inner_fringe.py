"""Only darken the circled inner fringe; retain the complete v5 silhouette."""
from pathlib import Path
from PIL import Image, ImageDraw

out = Path(__file__).resolve().parent
before = Image.open(out / 'jump-ascent-cutout-v5.png').convert('RGBA')
after = before.copy()
scale = 8
mask = Image.new('L', (512 * scale, 512 * scale))
boundary = [(305, 326), (312, 324), (319, 323), (322, 324),
            (324, 328), (323, 334), (320, 341), (316, 347),
            (310, 352), (304, 355)]
ImageDraw.Draw(mask).line([(round(x * scale), round(y * scale)) for x, y in boundary],
                          fill=255, width=12, joint='curve')
mask = mask.resize((512, 512), Image.Resampling.LANCZOS)
changed = []
for y in range(321, 357):
    for x in range(303, 327):
        weight = mask.getpixel((x, y)) / 255
        r, g, b, a = before.getpixel((x, y))
        if a and weight:
            rgb = tuple(round(v * (1 - weight) + c * weight)
                        for v, c in zip((r, g, b), (29, 30, 33)))
            after.putpixel((x, y), (*rgb, a))
            changed.append((x, y))
assert after.getchannel('A').tobytes() == before.getchannel('A').tobytes()
after.save(out / 'jump-ascent-cutout-v9.png')
preview = Image.new('RGBA', (1020, 500), (45, 62, 74, 255))
for i, frame in enumerate((before, after)):
    crop = frame.crop((275, 305, 330, 365)).resize((440, 480), Image.Resampling.NEAREST)
    preview.alpha_composite(crop, (i * 510, 20))
preview.save(out / 'jump-knee-continuous-outline-before-after.png')
print(f'Changed RGB at {len(changed)} pixels; alpha and silhouette identical to v5.')
