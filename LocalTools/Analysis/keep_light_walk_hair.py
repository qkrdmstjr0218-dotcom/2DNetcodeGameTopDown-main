from pathlib import Path
from PIL import Image, ImageFilter
from collections import deque
import numpy as np

root=Path(__file__).resolve().parents[2]
im=Image.open(root/'Assets/RightWalkPrototype/Art/RightWalkSheet-v6-hair.png').convert('RGBA')
a=np.array(im);cw,ch=im.width/4,im.height/2
removed=0
for row in range(2):
    for col in range(4):
        x0,y0=round(col*cw),round(row*ch)
        cell=a[y0:round((row+1)*ch),x0:round((col+1)*cw)]
        yy,xx=np.indices(cell.shape[:2])
        # Hair only; protect the face, shoulder, rifle and black/gold hair tie.
        hair=((xx<185)&(yy<175))|((xx>=185)&(xx<200)&(yy>=78)&(yy<132))
        tie=((xx-202)/17)**2+((yy-49)/24)**2<1
        hair &= ~tie
        lum=np.array(Image.fromarray(cell).convert('L').filter(ImageFilter.GaussianBlur(.8)))
        keep=np.clip((lum.astype(float)-150)/12,0,1)
        alpha=cell[:,:,3].copy()
        cell[:,:,3][hair]=(alpha[hair]*keep[hair]).astype(np.uint8)
        # Drop detached scraps left by removing the dark tail mass.
        solid=cell[:,:,3]>30
        seen=np.zeros(solid.shape,dtype=bool); groups=[]
        for y,x in zip(*np.where(solid)):
            if seen[y,x]:continue
            q=deque([(y,x)]);seen[y,x]=True; group=[]
            while q:
                py,px=q.popleft();group.append((py,px))
                for dy,dx in ((-1,0),(1,0),(0,-1),(0,1)):
                    ny,nx=py+dy,px+dx
                    if 0<=ny<solid.shape[0] and 0<=nx<solid.shape[1] and solid[ny,nx] and not seen[ny,nx]:
                        seen[ny,nx]=True;q.append((ny,nx))
            groups.append(group)
        for group in groups:
            if len(group)<100:
                for y,x in group:cell[y,x,3]=0
        removed+=int(((alpha>128)&(cell[:,:,3]<8)&hair).sum())
a[a[:,:,3]<8]=0
out=Image.fromarray(a)
out.save(root/'Assets/RightWalkPrototype/Art/RightWalkSheet-v7-light-hair.png')
crop=out.crop((0,0,round(cw),round(ch)))
preview=Image.new('RGB',(crop.width*2,crop.height))
for i,color in enumerate([(245,245,245),(20,27,35)]):
    bg=Image.new('RGBA',crop.size,color+(255,));bg.alpha_composite(crop)
    preview.paste(bg.convert('RGB'),(i*crop.width,0))
preview.save(root/'LocalTools/Captures/light-hair-v7.png')
print({'dark_hair_pixels_removed':removed})
