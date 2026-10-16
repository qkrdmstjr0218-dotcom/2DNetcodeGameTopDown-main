"""Preview only: remove the stray gold dot and smooth the marked lower remnant."""
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

out=Path(__file__).resolve().parent
before=Image.open(out/'jump-left-knee-glossy-plate-preview-v8.png').convert('RGBA')
after=before.copy()
k=(251+math.hypot(64,61)*math.cos(math.radians(40)),
   254+math.hypot(64,61)*math.sin(math.radians(40)))
mask=Image.new('L',before.size)
draw=ImageDraw.Draw(mask)
draw.ellipse((k[0]-16,k[1]-41,k[0]-12,k[1]-37),fill=255)
draw.polygon([(k[0]+x,k[1]+y) for x,y in [(-17,-33),(-8,-32),(-7,-26),
                                                       (-10,-20),(-17,-23)]],fill=255)
mask=mask.filter(ImageFilter.GaussianBlur(.6))
for y in range(int(k[1]-43),int(k[1]-17)):
    for x in range(int(k[0]-20),int(k[0]-4)):
        old=before.getpixel((x,y))
        weight=mask.getpixel((x,y))/255
        if old[3]<100 or not weight:continue
        samples=[]
        for sy in range(y-5,y+6):
            for sx in range(x-6,x+5):
                pixel=before.getpixel((sx,sy))
                if pixel[3]>230 and 18<max(pixel[:3])<70 and max(pixel[:3])-min(pixel[:3])<18:
                    w=1/(1+(sx-x)**2+(sy-y)**2)
                    samples.append((w,pixel))
        total=sum(w for w,_ in samples)
        donor=tuple(round(sum(w*pixel[c] for w,pixel in samples)/total) for c in range(3)) if total else (38,39,41)
        rgb=tuple(round(a*(1-weight)+b*weight) for a,b in zip(old[:3],donor))
        after.putpixel((x,y),(*rgb,old[3]))
after.save(out/'jump-left-knee-glossy-plate-preview-v9.png')
box=(int(k[0]-35),int(k[1]-45),int(k[0]+45),int(k[1]+35))
preview=Image.new('RGBA',(960,480),(45,62,74,255))
for i,image in enumerate((before,after)):
    preview.alpha_composite(image.crop(box).resize((480,480),Image.Resampling.NEAREST),(i*480,0))
preview.save(out/'jump-knee-marked-remnants-detail-v9.png')
print('Marked remnants cleaned in preview only.')
