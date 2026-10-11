from pathlib import Path
from PIL import Image, ImageDraw
import numpy as np

root=Path(__file__).resolve().parents[2]
im=Image.open(root/'Assets/RightWalkPrototype/Art/RightWalkSheet-v5-clean.png').convert('RGBA')
a=np.array(im); cw,ch=im.width/4,im.height/2
removed=0; repaired=0
for row in range(2):
    for col in range(4):
        x0,y0=round(col*cw),round(row*ch)
        cell=a[y0:round((row+1)*ch),x0:round((col+1)*cw)]
        h,w=cell.shape[:2]
        mask=Image.new('L',(w*4,h*4),0); d=ImageDraw.Draw(mask)
        # Retain a tapered ponytail rather than the heavy lower gray mass.
        tail=[(174,17),(196,21),(211,32),(211,44),(200,60),(189,76),
              (181,89),(173,98),(162,108),(154,119),(150,129),(153,134),
              (166,129),(166,138),(158,148),(146,156),(130,162),(112,161),
              (99,154),(87,141),(84,127),(88,113),(98,97),(111,82),
              (127,67),(140,53),(151,40),(161,29)]
        d.polygon([(x*4,y*4) for x,y in tail],fill=255)
        # Preserve the main head cap and all body pixels below the hair.
        d.rectangle((185*4,0,w*4,h*4),fill=255)
        d.rectangle((0,175*4,w*4,h*4),fill=255)
        d.rectangle((175*4,135*4,w*4,h*4),fill=255)
        m=np.array(mask.resize((w,h),Image.Resampling.LANCZOS))
        before=cell[:,:,3].copy()
        cell[:,:,3]=(before.astype(np.uint16)*m//255).astype(np.uint8)
        removed+=int(((before>128)&(cell[:,:,3]<20)).sum())
        rgb=cell[:,:,:3].astype(np.int16)
        yy,xx=np.indices((h,w))
        # Repair red bangs, excluding the eye line and ear.
        bad=(rgb[:,:,0]>145)&(rgb[:,:,0]>rgb[:,:,1]*1.65)&(rgb[:,:,0]>rgb[:,:,2]*1.5)
        band=((yy<=98)|((xx>=259)&(yy<=107)))
        bad &= (xx>=247)&(xx<=275)&(yy>=65)&band&(cell[:,:,3]>0)
        for y,x in zip(*np.where(bad)):
            ys=slice(max(0,y-5),min(h,y+6)); xs=slice(max(0,x-5),min(w,x+6))
            near=rgb[ys,xs]; valid=(~bad[ys,xs])&(cell[ys,xs,3]>220)
            valid &= (near[:,:,0]>100)&(np.max(near,axis=2)-np.min(near,axis=2)<85)
            if valid.any():
                cell[y,x,:3]=np.median(near[valid],axis=0).astype(np.uint8); repaired+=1
a[a[:,:,3]<8]=0
out=Image.fromarray(a);out.save(root/'Assets/RightWalkPrototype/Art/RightWalkSheet-v6-hair.png')
crop=out.crop((0,0,round(cw),round(ch)))
preview=Image.new('RGB',(crop.width*2,crop.height))
for i,color in enumerate([(245,245,245),(20,27,35)]):
    bg=Image.new('RGBA',crop.size,color+(255,));bg.alpha_composite(crop)
    preview.paste(bg.convert('RGB'),(i*crop.width,0))
preview.save(root/'LocalTools/Captures/hair-simplified-light-dark-v6.png')
print({'hair_pixels_trimmed':removed,'red_bang_pixels_repaired':repaired})
