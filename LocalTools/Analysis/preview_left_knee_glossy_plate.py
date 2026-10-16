"""Design preview only: remove upper white spike; add glossy rounded knee shell."""
import math
from pathlib import Path
from PIL import Image, ImageDraw
import numpy as np

root=Path(__file__).resolve().parents[2]
out=root/'LocalTools/Analysis'
before=Image.open(out/'jump-alternate-before-left-knee-white-plate.png').convert('RGBA')
after=before.copy()
knee=(251+math.hypot(64,61)*math.cos(math.radians(40)),
      254+math.hypot(64,61)*math.sin(math.radians(40)))
# Remove only light pixels in the original little upper spike, not its gold fastener.
dark=before.getpixel((round(knee[0]-3),round(knee[1]-24)))[:3]
if min(dark)>110: dark=(40,42,44)
for y in range(round(knee[1]-36),round(knee[1]-22)):
    for x in range(round(knee[0]-17),round(knee[0]+1)):
        pixel=before.getpixel((x,y))
        if pixel[3]>180 and min(pixel[:3])>150:
            after.putpixel((x,y),(*dark,pixel[3]))

def quadratic(a,c,b):
    return [((1-t)**2*a[0]+2*(1-t)*t*c[0]+t*t*b[0],
             (1-t)**2*a[1]+2*(1-t)*t*c[1]+t*t*b[1]) for t in np.linspace(0,1,33)]
local=quadratic((0,-26),(12,-26),(21,-18))
local+=quadratic((21,-18),(24,-15),(19,-14))[1:]
local+=quadratic((19,-14),(8,-10),(1,-17))[1:]
local+=quadratic((1,-17),(-1,-22),(0,-26))[1:]
scale=8
points=[(round((knee[0]+x)*scale),round((knee[1]+y)*scale)) for x,y in local]
mask=Image.new('L',(512*scale,512*scale))
ImageDraw.Draw(mask).polygon(points,fill=255)
bounds=mask.getbbox()
texture=Image.new('RGBA',mask.size)
draw=ImageDraw.Draw(texture)
for y in range(bounds[1],bounds[3]+1):
    t=(y-bounds[1])/max(1,bounds[3]-bounds[1])
    color=tuple(round(a*(1-t)+b*t) for a,b in zip((253,254,255),(195,204,233)))
    draw.line((bounds[0],y,bounds[2],y),fill=(*color,255))
texture.putalpha(mask)
draw=ImageDraw.Draw(texture)
draw.line(points+[points[0]],fill=(27,29,32,255),width=10,joint='curve')
after.alpha_composite(texture.resize(after.size,Image.Resampling.LANCZOS))
after.save(out/'jump-left-knee-glossy-plate-preview-v6.png')
box=(int(knee[0]-35),int(knee[1]-45),int(knee[0]+45),int(knee[1]+35))
preview=Image.new('RGBA',(960,480),(45,62,74,255))
for i,image in enumerate((before,after)):
    preview.alpha_composite(image.crop(box).resize((480,480),Image.Resampling.NEAREST),(i*480,0))
preview.save(out/'jump-left-knee-glossy-plate-detail-v6.png')
print('Design preview only. Unity assets untouched.')
