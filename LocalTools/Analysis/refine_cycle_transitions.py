from pathlib import Path
from PIL import Image
import numpy as np
root=Path(__file__).resolve().parents[2]
base=Image.open(root/'Assets/RightWalkPrototype/Art/RightWalkSheet-Redrawn-v3.png').convert('RGBA')
paths={2:'exec-c78a782e-6c10-4843-b1d4-661ec68f793c.png',3:'exec-b6c2daf7-f12d-4a29-b17c-ebb013fed800.png',7:'exec-92997df6-dbd4-453d-9807-920f99bf0422.png'}
def feature(im):
    a=np.array(im,dtype=float);return a[:,:,:3]*(a[:,:,3:4]/255)
ref=feature(base.crop((0,0,512,512)))[130:190,285:450]
shifts=[]
for i in (2,3,7):
    source=Image.open('C:/Users/82102/.codex/generated_images/01a12032-7676-7571-845e-57aac0c34cd2/'+paths[i]).convert('RGBA').resize((512,512),Image.Resampling.LANCZOS)
    f=feature(source);best=(float('inf'),0,0)
    for dy in range(-20,21):
        for dx in range(-20,21):
            score=float(np.mean((f[130+dy:190+dy,285+dx:450+dx]-ref)**2))
            if score<best[0]:best=score,dx,dy
    score,dx,dy=best;shifts.append((i+1,dx,dy))
    aligned=Image.new('RGBA',(512,512));aligned.alpha_composite(source,(-dx,-dy))
    a=np.array(aligned);a[a[:,:,3]<8]=0
    x,y=i%4*512,i//4*512
    old=np.array(base.crop((x,y,x+512,y+512)))
    # Keep identical upper body and a smooth transition below the waist.
    out=a.copy();out[:220]=old[:220]
    for yy in range(220,245):
        t=(yy-220)/25
        aa=old[yy,:,3:4].astype(float)/255;bb=a[yy,:,3:4].astype(float)/255
        alpha=aa*(1-t)+bb*t
        rgb=(old[yy,:,:3]*aa*(1-t)+a[yy,:,:3]*bb*t)/np.maximum(alpha,1e-6)
        out[yy,:,:3]=np.clip(rgb,0,255).astype(np.uint8)
        out[yy,:,3]=(alpha[:,0]*255).astype(np.uint8)
    base.paste(Image.fromarray(out),(x,y))
base.save(root/'Assets/RightWalkPrototype/Art/RightWalkSheet-Redrawn-v4.png')
preview=Image.new('RGBA',base.size,'white');preview.alpha_composite(base)
preview.resize((1280,640)).convert('RGB').save(root/'LocalTools/Captures/cycle-transitions-v4.jpg')
print({'registered_frames':shifts})
