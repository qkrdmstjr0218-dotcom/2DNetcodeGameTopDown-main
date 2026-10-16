"""Annotation only: proposed notch restoration and continuous outer outline."""
from pathlib import Path
from PIL import Image, ImageDraw

out = Path(__file__).resolve().parent
source = Image.open(out / 'jump-ascent-cutout-v22.png').convert('RGBA')
box = (297, 295, 330, 335)
zoom = 15
canvas = Image.new('RGBA', (33, 40), (45, 62, 74, 255))
canvas.alpha_composite(source.crop(box))
canvas = canvas.resize((495, 600), Image.Resampling.NEAREST)
overlay = Image.new('RGBA', canvas.size)
draw = ImageDraw.Draw(overlay)
def p(x, y):
    return ((x-box[0])*zoom, (y-box[1])*zoom)
draw.polygon([p(312, 305), p(314, 307), p(315, 310), p(316, 313),
              p(314, 312), p(313, 309), p(312, 309)],
             fill=(40, 230, 235, 100))
p0, control, p1 = (310, 302), (318, 310), (318.5, 321)
points = []
for step in range(161):
    t = step / 160
    points.append(tuple((1-t)**2*p0[i]+2*(1-t)*t*control[i]
                        +t*t*p1[i] for i in (0, 1)))
draw.line([p(x, y) for x, y in points], fill=(255, 135, 55, 255),
          width=4, joint='curve')
canvas = Image.alpha_composite(canvas, overlay)
canvas.save(out / 'jump-panel-outer-contour-plan-v6.png')
print('Annotation only. Source pixels unchanged.')
