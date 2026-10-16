"""Preview only: place the white plate where the user painted it."""
import math
from pathlib import Path
from PIL import Image, ImageDraw

root=Path(__file__).resolve().parents[2]
out=root/'LocalTools/Analysis'
before=Image.open(out/'jump-alternate-before-left-knee-white-plate.png').convert('RGBA')
after=before.copy()
knee=(251+math.hypot(64,61)*math.cos(math.radians(40)),
      254+math.hypot(64,61)*math.sin(math.radians(40)))
scale=8
plate=Image.new('RGBA',(512*scale,512*scale))
def quadratic(a, control, b, steps=32):
    return [((1-t)**2*a[0]+2*(1-t)*t*control[0]+t*t*b[0],
             (1-t)**2*a[1]+2*(1-t)*t*control[1]+t*t*b[1])
            for t in [i/steps for i in range(steps+1)]]
local=quadratic((-17,-34),(-16,-36),(-12,-33))
local+=quadratic((-12,-33),(7,-30),(17,-22))[1:]
local+=quadratic((17,-22),(20,-20),(17,-19))[1:]
local+=[(5,-17),(-10,-22)]
local+=quadratic((-10,-22),(-17,-23),(-17,-34))[1:]
points=[(knee[0]+x,knee[1]+y) for x,y in local]
draw=ImageDraw.Draw(plate)
draw.polygon([(round(x*scale),round(y*scale)) for x,y in points],fill=(237,240,249,255))
draw.line([(round(x*scale),round(y*scale)) for x,y in points+[points[0]]],
          fill=(27,29,32,255),width=10,joint='curve')
after.alpha_composite(plate.resize(after.size,Image.Resampling.LANCZOS))
after.save(out/'jump-left-knee-white-plate-preview-v5.png')
box=(int(knee[0]-35),int(knee[1]-45),int(knee[0]+45),int(knee[1]+35))
preview=Image.new('RGBA',(960,480),(45,62,74,255))
for i,image in enumerate((before,after)):
    preview.alpha_composite(image.crop(box).resize((480,480),Image.Resampling.NEAREST),(i*480,0))
preview.save(out/'jump-left-knee-white-plate-detail-v5.png')
print('Preview only. No Unity assets changed by this script.')
