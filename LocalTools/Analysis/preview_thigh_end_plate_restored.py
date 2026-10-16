"""Preview only: restore/expand the thigh-end plate; leave knee armor untouched."""
import math
from pathlib import Path
from PIL import Image, ImageDraw
import numpy as np

root=Path(__file__).resolve().parents[2]
out=root/'LocalTools/Analysis'
baseline=Image.open(out/'jump-alternate-before-left-knee-white-plate.png').convert('RGBA')
after=baseline.copy()
k=(251+math.hypot(64,61)*math.cos(math.radians(40)),
   254+math.hypot(64,61)*math.sin(math.radians(40)))
def q(a,c,b):
    return [((1-t)**2*a[0]+2*(1-t)*t*c[0]+t*t*b[0],
             (1-t)**2*a[1]+2*(1-t)*t*c[1]+t*t*b[1]) for t in np.linspace(0,1,33)]
local=q((-16,-36),(-8,-35),(3,-26))
local+=q((3,-26),(5,-24),(1,-24))[1:]
local+=q((1,-24),(-8,-23),(-13,-28))[1:]
local+=q((-13,-28),(-16,-31),(-16,-36))[1:]
scale=8
points=[(round((k[0]+x)*scale),round((k[1]+y)*scale)) for x,y in local]
mask=Image.new('L',(4096,4096))
ImageDraw.Draw(mask).polygon(points,fill=255)
plate=Image.new('RGBA',mask.size)
draw=ImageDraw.Draw(plate)
box=mask.getbbox()
for y in range(box[1],box[3]+1):
    t=(y-box[1])/max(1,box[3]-box[1])
    color=tuple(round(a*(1-t)+b*t) for a,b in zip((253,254,255),(202,211,236)))
    draw.line((box[0],y,box[2],y),fill=(*color,255))
plate.putalpha(mask)
ImageDraw.Draw(plate).line(points+[points[0]],fill=(26,28,31,255),width=10,joint='curve')
after.alpha_composite(plate.resize(after.size,Image.Resampling.LANCZOS))
after.save(out/'jump-thigh-end-white-plate-preview-v11.png')
old=Image.open(out/'jump-left-knee-glossy-plate-preview-v10.png').convert('RGBA')
crop=(int(k[0]-35),int(k[1]-45),int(k[0]+45),int(k[1]+35))
preview=Image.new('RGBA',(960,480),(45,62,74,255))
for i,image in enumerate((old,after)):
    preview.alpha_composite(image.crop(crop).resize((480,480),Image.Resampling.NEAREST),(i*480,0))
preview.save(out/'jump-thigh-end-white-plate-detail-v11.png')
scope={'__file__':str(out/'preview_jump_full_body.py')}
exec(compile((out/'preview_jump_full_body.py').read_text().split('\nwalk = ')[0],
             '<static-upper-layers>','exec'),scope)
full=Image.new('RGBA',(512,512),(45,62,74,255))
full.alpha_composite(scope['compose'](after))
full.crop((95,25,485,485)).resize((585,690),Image.Resampling.LANCZOS).save(out/'jump-thigh-end-white-plate-full-body-v11.png')
print('Preview only. Lower knee returned to unmodified baseline; upper thigh plate restored/expanded.')
