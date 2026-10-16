"""Darken the indicated right edge on actual opaque pixels, not outside alpha."""
from pathlib import Path
from PIL import Image

out = Path(__file__).resolve().parent
before = Image.open(out / 'jump-ascent-cutout-v12.png').convert('RGBA')
after = before.copy()
changed = 0
for y in range(325, 340):
    occupied = [x for x in range(319, 330) if before.getpixel((x, y))[3] >= 128]
    if not occupied:
        continue
    edge = max(occupied)
    for x in (edge,):
        pixel = before.getpixel((x, y))
        result = (22, 24, 27, pixel[3])
        after.putpixel((x, y), result)
        changed += pixel != result
assert before.getchannel('A').tobytes() == after.getchannel('A').tobytes()
after.save(out / 'jump-ascent-cutout-v14.png')
preview = Image.new('RGBA', (660, 440), (45, 62, 74, 255))
for i, frame in enumerate((before, after)):
    preview.alpha_composite(frame.crop((311, 315, 332, 343)).resize((294, 392),
                             Image.Resampling.NEAREST), (i * 330, 24))
preview.save(out / 'jump-knee-outermost-edge-before-after.png')
print(f'{changed} right-edge pixels darkened; silhouette unchanged.')
