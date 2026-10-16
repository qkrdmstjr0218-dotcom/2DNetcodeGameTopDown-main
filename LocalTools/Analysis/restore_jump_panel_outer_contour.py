"""Restore the approved exterior curve and its continuous original-width rim."""
from pathlib import Path
from PIL import Image, ImageDraw

out = Path(__file__).resolve().parent
before = Image.open(out / 'jump-ascent-cutout-v22.png').convert('RGBA')
after = before.copy()
scale = 8
p0, control, p1 = (310, 302), (318, 310), (318.5, 321)
curve = []
for step in range(257):
    t = step/256
    curve.append(tuple((1-t)**2*p0[i]+2*(1-t)*t*control[i]+t*t*p1[i]
                       for i in (0, 1)))
# Restore only the cut outer strip; do not repaint the continuous panel interior.
for y in range(303, 322):
    cx, _ = min(curve, key=lambda point: abs(point[1]-y))
    white = [x for x in range(303, 319)
             if before.getpixel((x, y))[3] > 230
             and min(before.getpixel((x, y))[:3]) > 165]
    if not white:
        continue
    edge = max(white)
    donor = before.getpixel((max(303, edge-1), y))
    for x in range(edge+1, int(cx)+1):
        after.putpixel((x, y), (*donor[:3], 255))
mask = Image.new('L', (512*scale, 512*scale))
ImageDraw.Draw(mask).line([(round(x*scale), round(y*scale)) for x, y in curve],
                          fill=255, width=20, joint='curve')
mask = mask.resize(before.size, Image.Resampling.LANCZOS)
outline = Image.new('RGBA', before.size, (25, 27, 30, 0))
outline.putalpha(mask)
after = Image.alpha_composite(after, outline)
after.save(out / 'jump-ascent-cutout-v24.png')
preview = Image.new('RGBA', (660, 440), (45, 62, 74, 255))
previous = Image.open(out / 'jump-ascent-cutout-v23.png').convert('RGBA')
for i, frame in enumerate((previous, after)):
    preview.alpha_composite(frame.crop((297, 295, 330, 335)).resize((297, 360),
                             Image.Resampling.NEAREST), (i*330, 24))
preview.save(out / 'jump-panel-outer-contour-thickness-before-after.png')
print('Approved curve retained; rim increased from 2px to 2.5px.')
