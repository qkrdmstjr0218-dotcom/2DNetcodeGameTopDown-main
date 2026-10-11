from pathlib import Path
from PIL import Image, ImageDraw
import numpy as np
from collections import deque
root=Path(__file__).resolve().parents[2]
sheet=Image.open(root/'Assets/RightWalkPrototype/Art/RightWalkSheet-Redrawn-v1.png').convert('RGBA')
fix=Image.open('C:/Users/82102/.codex/generated_images/01a12032-7676-7571-845e-57aac0c34cd2/exec-aadbbd56-0a04-4e9c-81d5-286cf730c889.png').convert('RGBA').resize((443,443),Image.Resampling.LANCZOS)
frames=[]
for i in range(8):
    x0=round(i%4*sheet.width/4);y0=round(i//4*sheet.height/2)
    frame=sheet.crop((x0,y0,round((i%4+1)*sheet.width/4),round((i//4+1)*sheet.height/2)))
    if i==6:
        f=np.array(frame);g=np.array(fix.resize(frame.size,Image.Resampling.LANCZOS))
        yy,xx=np.indices(f.shape[:2])
        use=(yy>=193)&((xx>=198)&(yy<267)|(xx>=232)&(yy>=267))
        f[use]=g[use];frame=Image.fromarray(f)
    a=np.array(frame);a[a[:,:,3]<8]=0
    # Remove detached fragments from a neighboring row, not character parts.
    solid=a[:,:,3]>20;seen=np.zeros(solid.shape,dtype=bool)
    for sy,sx in zip(*np.where(solid)):
        if seen[sy,sx]:continue
        q=deque([(sy,sx)]);seen[sy,sx]=True;group=[]
        while q:
            y,x=q.popleft();group.append((y,x))
            for dy,dx in ((-1,0),(1,0),(0,-1),(0,1)):
                ny,nx=y+dy,x+dx
                if 0<=ny<solid.shape[0] and 0<=nx<solid.shape[1] and solid[ny,nx] and not seen[ny,nx]:
                    seen[ny,nx]=True;q.append((ny,nx))
        if len(group)<300 and min(y for y,x in group)>400:
            for y,x in group:a[y,x]=0
    frames.append(Image.fromarray(a))
def feature(im):
    a=np.array(im,dtype=float);return a[:,:,:3]*(a[:,:,3:4]/255)
ref=feature(frames[0])[90:150,235:405]
out=Image.new('RGBA',(2048,1024));offsets=[];stable_upper=None
for i,frame in enumerate(frames):
    f=feature(frame);best=(float('inf'),0,0)
    for dy in range(-14,15):
        for dx in range(-14,15):
            patch=f[90+dy:150+dy,235+dx:405+dx]
            score=float(np.mean((patch-ref)**2))
            if score<best[0]:best=(score,dx,dy)
    score,dx,dy=best;offsets.append((dx,dy,round(score,1)))
    cell=Image.new('RGBA',(512,512));cell.alpha_composite(frame,(34-dx,34-dy))
    if stable_upper is None:stable_upper=np.array(cell)
    else:
        pixels=np.array(cell)
        pixels[:220]=stable_upper[:220]
        # Premultiplied crossfade at the waist, avoiding a hard rectangular seam.
        for y in range(220,238):
            t=(y-220)/18
            aa=stable_upper[y,:,3:4].astype(float)/255
            bb=pixels[y,:,3:4].astype(float)/255
            alpha=aa*(1-t)+bb*t
            rgb=(stable_upper[y,:,:3]*aa*(1-t)+pixels[y,:,:3]*bb*t)/np.maximum(alpha,1e-6)
            pixels[y,:,:3]=np.clip(rgb,0,255).astype(np.uint8)
            pixels[y,:,3]=(alpha[:,0]*255).astype(np.uint8)
        cell=Image.fromarray(pixels)
    a=np.array(cell)
    assert not np.any(a[:16,:,3]) and not np.any(a[-16:,:,3])
    assert not np.any(a[:,:16,3]) and not np.any(a[:,-16:,3])
    out.alpha_composite(cell,(i%4*512,i//4*512))
out.save(root/'Assets/RightWalkPrototype/Art/RightWalkSheet-Redrawn-v2.png')
bg=Image.new('RGBA',out.size,'white');bg.alpha_composite(out)
bg.resize((1280,640)).convert('RGB').save(root/'LocalTools/Captures/aligned-walk-v2.jpg')
print({'gun_registration_offsets':offsets,'canvas':out.size,'verified_empty_gutter':16})
