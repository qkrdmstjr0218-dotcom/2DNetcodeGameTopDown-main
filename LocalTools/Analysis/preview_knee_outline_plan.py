"""Annotation only: proposed tip contour over the unedited reference."""
from pathlib import Path
from PIL import Image, ImageDraw

out = Path(__file__).resolve().parent
source = Image.open(out / 'jump-ascent-cutout-v5.png').convert('RGBA')
box = (275, 305, 330, 365)
zoom = 10
preview = Image.new('RGBA', (550, 600), (45, 62, 74, 255))
preview.alpha_composite(source.crop(box).resize(preview.size, Image.Resampling.NEAREST))
# Follow the short rounded outer tip only; never bridge across the white face.
points = [(322.2, 324.0), (323.1, 325.2), (324.0, 326.8),
          (324.7, 328.5), (324.9, 330.2), (324.6, 332.0),
          (324.0, 334.0), (323.1, 336.0)]
draw = ImageDraw.Draw(preview)
draw.line([((x-box[0])*zoom, (y-box[1])*zoom) for x, y in points],
          fill=(40, 230, 235, 255), width=4, joint='curve')
preview.save(out / 'jump-knee-tip-outline-plan.png')
print('Annotation only. Source and candidate pixels unchanged.')
