"""Two-segment annotation only; source pixels are not edited."""
from pathlib import Path
from PIL import Image, ImageDraw

out = Path(__file__).resolve().parent
source = Image.open(out / 'jump-ascent-cutout-v5.png').convert('RGBA')
box = (300, 313, 333, 344)
zoom = 18
canvas = Image.new('RGBA', (box[2]-box[0], box[3]-box[1]), (45, 62, 74, 255))
canvas.alpha_composite(source.crop(box))
canvas = canvas.resize((canvas.width*zoom, canvas.height*zoom), Image.Resampling.NEAREST)
def point(x, y):
    return ((x-box[0])*zoom, (y-box[1])*zoom)
draw = ImageDraw.Draw(canvas)
tip = point(324, 321.5)
draw.line([point(315, 323), tip], fill=(40, 230, 235, 255), width=3)
draw.line([tip, point(326, 330)], fill=(255, 90, 90, 255), width=3)
draw.ellipse((tip[0]-4, tip[1]-4, tip[0]+4, tip[1]+4), fill=(255, 230, 80, 255))
canvas.save(out / 'jump-knee-sharp-tip-plan-v4.png')
print('Annotation only; original and all sprite candidates unchanged.')
