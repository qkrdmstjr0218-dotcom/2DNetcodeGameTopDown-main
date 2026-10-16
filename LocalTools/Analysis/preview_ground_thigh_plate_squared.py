"""Preview-only squared thigh-end plate on the representative planted frame."""
from pathlib import Path
from PIL import Image, ImageDraw
import json, math

root=Path(__file__).resolve().parents[2]
out=root/'LocalTools/Analysis'
original=Image.open(root/'Assets/RightWalkPrototype/Art/WalkStrideTrial.png').convert('RGBA')
expanded=Image.open(out/'WalkStrideTrial-ThighPlatePreview.png').convert('RGBA')
index=20
box=(index%5*512,index//5*512,index%5*512+512,index//5*512+512)
before=expanded.crop(box)
after=original.crop(box)
pose=json.loads((out/'walk-stride-trial-joints.json').read_text())[index]['far']
source_hip=(263,276)
source_knee=(199,337)
angle=math.atan2(pose['knee'][1]-pose['hip'][1],pose['knee'][0]-pose['hip'][0])-math.atan2(61,-64)
# Same attachment region as the expanded plate, but a short chamfered rectangle.
local=[(229,317),(238,321),(239,329),(234,334),(225,330),(224,322)]
def registered(point):
    x,y=point[0]-source_hip[0],point[1]-source_hip[1]
    return (pose['hip'][0]+x*math.cos(angle)-y*math.sin(angle)+10,
            pose['hip'][1]+x*math.sin(angle)+y*math.cos(angle)+7)
points=[registered(point) for point in local]
scale=8
mask=Image.new('L',(4096,4096))
ImageDraw.Draw(mask).polygon([(round(x*scale),round(y*scale)) for x,y in points],fill=255)
plate=Image.new('RGBA',mask.size)
draw=ImageDraw.Draw(plate)
bounds=mask.getbbox()
for y in range(bounds[1],bounds[3]+1):
    t=(y-bounds[1])/max(1,bounds[3]-bounds[1])
    color=tuple(round(a*(1-t)+b*t) for a,b in zip((251,253,255),(201,211,237)))
    draw.line((bounds[0],y,bounds[2],y),fill=(*color,255))
plate.putalpha(mask)
ImageDraw.Draw(plate).line([(round(x*scale),round(y*scale)) for x,y in points+[points[0]]],
                          fill=(27,29,32,255),width=10,joint='curve')
after.alpha_composite(plate.resize(after.size,Image.Resampling.LANCZOS))
after.save(out/'standing-thigh-plate-squared-preview.png')
preview=Image.new('RGBA',(800,360),(45,62,74,255))
for i,image in enumerate((before,after)):
    preview.alpha_composite(image.crop((220,270,320,355)).resize((400,340),Image.Resampling.NEAREST),(i*400,20))
preview.save(out/'standing-thigh-plate-squared-before-after.png')
print('Squared plate preview only. Unity assets unchanged.')
