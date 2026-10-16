"""Connect the user's two exterior edge points; do not paint inside white panel."""
from pathlib import Path
from PIL import Image, ImageDraw

out = Path(__file__).resolve().parent
before = Image.open(out / 'jump-ascent-cutout-v12.png').convert('RGBA')
scale = 16
mask = Image.new('L', (512*scale, 512*scale))
p0, p1, p2, p3 = (323, 320), (325.0, 322.5), (326.3, 326.5), (325.7, 330)
curve = []
for step in range(129):
    t = step/128
    curve.append(tuple(round(scale*((1-t)**3*p0[i]+3*(1-t)**2*t*p1[i]
                                     +3*(1-t)*t*t*p2[i]+t**3*p3[i])) for i in (0, 1)))
ImageDraw.Draw(mask).line(curve, fill=255, width=12, joint='curve')
mask = mask.resize(before.size, Image.Resampling.LANCZOS)
# Limit coverage to the exterior edge and outward pixels, never the inner face.
for y in range(319, 333):
    occupied = [x for x in range(318, 330) if before.getpixel((x, y))[3] >= 128]
    edge = max(occupied) if occupied else 324
    for x in range(318, edge):
        mask.putpixel((x, y), 0)
overlay = Image.new('RGBA', before.size, (22, 24, 27, 0))
overlay.putalpha(mask)
after = Image.alpha_composite(before, overlay)
after.save(out / 'jump-ascent-cutout-v16.png')
preview = Image.new('RGBA', (660, 440), (45, 62, 74, 255))
for i, frame in enumerate((before, after)):
    preview.alpha_composite(frame.crop((311, 315, 332, 343)).resize((294, 392),
                             Image.Resampling.NEAREST), (i*330, 24))
preview.save(out / 'jump-knee-edge-thin-curve-before-after.png')
print('Exterior curve connected. No pixels left of opaque right edge painted.')
