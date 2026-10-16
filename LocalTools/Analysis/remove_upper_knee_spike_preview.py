"""Preview-only textured cleanup of the removed upper armor spike."""
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

out=Path(__file__).resolve().parent
before=Image.open(out/'jump-left-knee-glossy-plate-preview-v6.png').convert('RGBA')
original=Image.open(out/'jump-alternate-before-left-knee-white-plate.png').convert('RGBA')
after=before.copy()
knee=(251+math.hypot(64,61)*math.cos(math.radians(40)),
      254+math.hypot(64,61)*math.sin(math.radians(40)))
mask=Image.new('L',before.size)
polygon=[(knee[0]+x,knee[1]+y) for x,y in [(-20,-38),(-10,-35),(0,-27),(-3,-22),(-17,-25)]]
ImageDraw.Draw(mask).polygon(polygon,fill=255)
mask=mask.filter(ImageFilter.GaussianBlur(.5))
for y in range(int(knee[1]-39),int(knee[1]-20)):
    for x in range(int(knee[0]-21),int(knee[0]+2)):
        old=before.getpixel((x,y))
        if old[3]<100:continue
        if old[0]>110 and old[1]>55 and old[2]<80:continue
        weight=mask.getpixel((x,y))/255
        if not weight:continue
        # Sample nearby dark armor texture, excluding trim and the new white shell.
        samples=[]
        for sy in range(y-4,y+5):
            for sx in range(x-6,x+7):
                pixel=original.getpixel((sx,sy))
                if pixel[3]>230 and 15<max(pixel[:3])<65 and max(pixel[:3])-min(pixel[:3])<20:
                    samples.append((1/(1+(sx-x)**2+(sy-y)**2),pixel))
        if samples:
            total=sum(weight for weight,_ in samples)
            donor=tuple(round(sum(weight*pixel[c] for weight,pixel in samples)/total) for c in range(3))
        else:
            donor=(37,39,41)
        rgb=tuple(round(a*(1-weight)+b*weight) for a,b in zip(old[:3],donor[:3]))
        after.putpixel((x,y),(*rgb,old[3]))
after.save(out/'jump-left-knee-glossy-plate-preview-v8.png')
box=(int(knee[0]-35),int(knee[1]-45),int(knee[0]+45),int(knee[1]+35))
preview=Image.new('RGBA',(960,480),(45,62,74,255))
previous=Image.open(out/'jump-left-knee-glossy-plate-preview-v7.png').convert('RGBA')
for i,image in enumerate((previous,after)):
    preview.alpha_composite(image.crop(box).resize((480,480),Image.Resampling.NEAREST),(i*480,0))
preview.save(out/'jump-upper-spike-cleanup-detail-v8.png')
print('Preview-only upper spike texture cleanup saved.')
