from PIL import Image
import numpy as np, json
from pathlib import Path
src=Image.open('C:/Users/82102/AppData/Local/Temp/codex-clipboard-7147317d-36b0-48e9-a53c-08f73aa4ac0a.png').convert('RGBA')
target=np.array(Image.open('C:/Users/82102/Pictures/Screenshots/스크린샷 2026-10-09 234213 - 복사본.png').convert('RGB')).astype(float)
results=[]
for frame in range(4,8):
    cell=src.crop((frame*60,440,frame*60+60,550))
    best=None
    for scale in np.arange(2.7,2.91,.025):
        head=cell.crop((10,10,43,43))
        bg=Image.new('RGBA',head.size,'white');bg.alpha_composite(head)
        a=np.array(bg.convert('RGB').resize((round(33*scale),round(33*scale)),Image.Resampling.NEAREST)).astype(float)
        yy,xx=np.mgrid[0:a.shape[0]:5,0:a.shape[1]:5]
        # Search only each frame's unmarked head, never the leg silhouette.
        dx,dy=np.meshgrid(np.arange(frame%4*185+15,frame%4*185+85),np.arange(345,375))
        errors=np.zeros(dx.shape)
        for y,x in zip(yy.ravel(),xx.ravel()):
            errors+=np.mean((target[dy+y,dx+x]-a[y,x])**2,axis=-1)
        loc=np.unravel_index(np.argmin(errors),errors.shape)
        score=float(errors[loc]/xx.size)
        if best is None or score<best['score']:
            best=dict(frame=frame+1,scale=float(scale),offset=[float(dx[loc]-10*scale),float(dy[loc]-10*scale)],score=score)
    results.append(best)
print(json.dumps(results,indent=2))
Path('LocalTools/Analysis/markup-registration.json').write_text(json.dumps(results,indent=2))
