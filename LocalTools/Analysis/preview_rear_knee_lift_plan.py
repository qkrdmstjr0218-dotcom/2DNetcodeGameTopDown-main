"""Mark the rear/left knee itself before any further pose edit."""
import math
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[2]
out = root/'LocalTools/Analysis'
scope = {'__file__':str(out/'preview_jump_full_body.py')}
exec(compile((out/'preview_jump_full_body.py').read_text().split('\nwalk = ')[0],
             '<upper-layer-loader>','exec'),scope)
legs = Image.open(out/'jump-alternate-before-knee-lift.png').convert('RGBA')
image = scope['compose'](legs)
preview = Image.new('RGBA',(512,512),(45,62,74,255))
preview.alpha_composite(image)
draw = ImageDraw.Draw(preview)
hip=(251,254)
length=math.hypot(-64,61)
x=hip[0]+length*math.cos(math.radians(75))
y=hip[1]+length*math.sin(math.radians(75))
draw.ellipse((x-8,y-8,x+8,y+8),outline=(255,85,85,255),width=2)
preview.crop((165,225,345,475)).resize((540,750),Image.Resampling.NEAREST).save(out/'jump-left-knee-target-only.png')
print('Annotation only: far/left knee location. No movement arrow or pose change.')
