from pathlib import Path
import sys
from PIL import Image,ImageFilter
import numpy as np
root=Path(__file__).resolve().parents[2]
art=root/'Assets/RightWalkPrototype/Art'
sheet=Image.open(art/'RightWalkSheet-Redrawn-v5.png').convert('RGBA')
frames=[sheet.crop((i%5*512,i//5*512,(i%5+1)*512,(i//5+1)*512)) for i in range(10)]
source=Image.open(sys.argv[1]).convert('RGBA').resize((1024,512),Image.Resampling.LANCZOS)
for j,i in enumerate([6,7]):
    im=source.crop((j*512,0,(j+1)*512,512));a=np.array(im);ref=np.array(frames[i])
    f=a[:,:,:3].astype(float)*a[:,:,3:4]/255
    r=ref[:,:,:3].astype(float)*ref[:,:,3:4]/255
    best=(float('inf'),0,0)
    for dy in range(-16,17):
        for dx in range(-16,17):
            score=np.mean((f[130+dy:190+dy,285+dx:450+dx]-r[130:190,285:450])**2)
            if score<best[0]:best=(score,dx,dy)
    _,dx,dy=best
    aligned=Image.new('RGBA',(512,512));aligned.alpha_composite(im,(-dx,-dy));a=np.array(aligned)
    a[a[:,:,3]<8]=0;a[:245]=ref[:245]
    # Remove the generated thigh plate before applying the shared source.
    yy,xx=np.indices(a.shape[:2]);rgb=a[:,:,:3].astype(float)
    bright=(rgb.mean(2)>175)&(rgb.max(2)-rgb.min(2)<70)&(a[:,:,3]>220)
    bright&=(xx>=180)&(xx<=325)&(yy>=245)&(yy<=312)
    mask=np.array(Image.fromarray(bright.astype(np.uint8)*255).filter(ImageFilter.MaxFilter(9)))>0
    a[mask,:3]=[38,34,35]
    part=Image.open(art/'ThighPlate-Canonical-v1.png').convert('RGBA')
    angle=[-49,-19][j];cx,cy=[(249,283),(264,278)][j]
    part=part.rotate(angle,Image.Resampling.BICUBIC,expand=True)
    cell=Image.fromarray(a);cell.alpha_composite(part,(round(cx-part.width/2),round(cy-part.height/2)))
    frames[i]=cell
out=Image.new('RGBA',(2560,1024))
for i,cell in enumerate(frames):out.alpha_composite(cell,(i%5*512,i//5*512))
out.save(art/'RightWalkSheet-Redrawn-v6.png')
bg=Image.new('RGBA',out.size,(14,19,25,255));bg.alpha_composite(out)
bg.resize((1500,600)).convert('RGB').save(root/'LocalTools/Captures/support-leg-v6.jpg')
print('Saved v6: opposite support-leg travel in frames 7 and 8.')
