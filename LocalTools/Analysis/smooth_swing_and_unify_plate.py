from pathlib import Path
from collections import deque
import json
import numpy as np
from PIL import Image, ImageFilter
root=Path(__file__).resolve().parents[2]
art=root/'Assets/RightWalkPrototype/Art'
sheet=Image.open(art/'RightWalkSheet-Redrawn-v4.png').convert('RGBA')
old=[sheet.crop((i%4*512,i//4*512,(i%4+1)*512,(i//4+1)*512)) for i in range(8)]
def feature(im):
    a=np.array(im,dtype=float);return a[:,:,:3]*a[:,:,3:4]/255
ref=feature(old[0])[130:190,285:450]
bridges=[]
for name in ['exec-4c7cca48-f633-493b-817b-2dd49548b986.png','exec-3741b636-f9a4-4ff9-a78f-964c4e73d384.png']:
    im=Image.open('C:/Users/82102/.codex/generated_images/01a12032-7676-7571-845e-57aac0c34cd2/'+name).convert('RGBA').resize((512,512),Image.Resampling.LANCZOS)
    f=feature(im);best=(float('inf'),0,0)
    for dy in range(-16,17):
        for dx in range(-16,17):
            score=float(np.mean((f[130+dy:190+dy,285+dx:450+dx]-ref)**2))
            if score<best[0]:best=(score,dx,dy)
    _,dx,dy=best;canvas=Image.new('RGBA',(512,512));canvas.alpha_composite(im,(-dx,-dy))
    a=np.array(canvas);a[a[:,:,3]<8]=0;a[:220]=np.array(old[0])[:220]
    # Smooth the waist without changing the common upper body.
    o=np.array(old[0])
    for y in range(220,245):
        t=(y-220)/25;aa=o[y,:,3:4]/255;bb=a[y,:,3:4]/255;alpha=aa*(1-t)+bb*t
        a[y,:,:3]=np.clip((o[y,:,:3]*aa*(1-t)+a[y,:,:3]*bb*t)/np.maximum(alpha,1e-6),0,255)
        a[y,:,3]=(alpha[:,0]*255).astype(np.uint8)
    bridges.append(Image.fromarray(a))
frames=old[:6]+bridges+old[6:]
def plate_mask(im):
    a=np.array(im);rgb=a[:,:,:3].astype(float);yy,xx=np.indices(a.shape[:2])
    bright=(rgb.mean(2)>175)&(rgb.max(2)-rgb.min(2)<70)&(a[:,:,3]>220)
    bright&=(xx>=180)&(xx<=325)&(yy>=245)&(yy<=320)
    seen=np.zeros(bright.shape,dtype=bool);groups=[]
    for sy,sx in zip(*np.where(bright)):
        if seen[sy,sx]:continue
        q=deque([(sy,sx)]);seen[sy,sx]=True;g=[]
        while q:
            y,x=q.popleft();g.append((y,x))
            for dy,dx in ((-1,0),(1,0),(0,-1),(0,1)):
                ny,nx=y+dy,x+dx
                if 0<=ny<512 and 0<=nx<512 and bright[ny,nx] and not seen[ny,nx]:
                    seen[ny,nx]=True;q.append((ny,nx))
        groups.append(g)
    g=max(groups,key=len);mask=np.zeros((512,512),dtype=np.uint8)
    for y,x in g:mask[y,x]=255
    return Image.fromarray(mask).filter(ImageFilter.MaxFilter(9))
mask=plate_mask(frames[0]);part=frames[0].copy();part.putalpha(mask)
part=part.crop(mask.getbbox())
part.save(art/'ThighPlate-Canonical-v1.png')
angles=[45,72,100,124,124,90,94,64,48,45]
centers=[(269,275),(272,281),(252,286),(231,284),(231,284),(241,286),(249,283),(264,278),(275,273),(278,273)]
stats=[];out=Image.new('RGBA',(2560,1024))
for i,im in enumerate(frames):
    a=np.array(im);m=np.array(plate_mask(im))
    # Replace each old plate with dark suit material before applying the shared part.
    rgb=a[:,:,:3];near=np.array(Image.fromarray(m).filter(ImageFilter.MaxFilter(17)))>0
    valid=near&(m==0)&(a[:,:,3]>220)&(rgb.mean(2)>15)&(rgb.mean(2)<90)
    color=np.median(rgb[valid],axis=0) if valid.any() else np.array([40,36,37])
    opacity=m.astype(float)/255
    a[:,:,:3]=(rgb*(1-opacity[:,:,None])+color*opacity[:,:,None]).astype(np.uint8)
    cell=Image.fromarray(a)
    rotated=part.rotate(45-angles[i],Image.Resampling.BICUBIC,expand=True)
    cx,cy=centers[i];cell.alpha_composite(rotated,(round(cx-rotated.width/2),round(cy-rotated.height/2)))
    out.alpha_composite(cell,(i%5*512,i//5*512))
    stats.append({'frame':i+1,'plate_center':[cx,cy],'rotation':45-angles[i]})
out.save(art/'RightWalkSheet-Redrawn-v5.png')
preview=Image.new('RGBA',out.size,'white');preview.alpha_composite(out)
preview.resize((1500,600)).convert('RGB').save(root/'LocalTools/Captures/swing-plate-v5.jpg')
(root/'LocalTools/Analysis/swing-plate-v5.json').write_text(json.dumps({'canonical_size':part.size,'frames':stats,'durations':[1,1,1,1,1,1/3,1/3,1/3,1,1]},indent=2))
print({'canonical_size':part.size,'frames':len(frames),'frame_weights':[1,1,1,1,1,1/3,1/3,1/3,1,1]})
