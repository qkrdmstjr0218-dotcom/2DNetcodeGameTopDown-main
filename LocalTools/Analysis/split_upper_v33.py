from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np
root=Path.cwd();art=root/'Assets/RightWalkPrototype/Art';src=Image.open(art/'RightWalkSheet-Redrawn-v32.png').convert('RGBA')
mask=Image.new('L',(512,512));ImageDraw.Draw(mask).polygon([(292,135),(333,132),(336,119),(359,118),(372,132),(459,140),(467,145),(468,162),(438,166),(401,165),(392,161),(371,163),(347,163),(334,166),(318,161),(292,159)],fill=255)
m=np.array(mask)>0;first=np.array(src.crop((0,0,512,512)));gun=first.copy();gun[~m]=0;Image.fromarray(gun).save(art/'RightWalk-Weapon-v33.png')
sheet=np.array(src).copy()
for i in range(20):
 y=i//5*512;x=i%5*512;frame=sheet[y:y+512,x:x+512];frame[m]=0
 assert np.array_equal(np.where(m[:,:,None],gun,frame),np.array(src.crop((x,y,x+512,y+512))))
upper=sheet[:512,:512].copy();upper[232:]=0;Image.fromarray(upper).save(art/'RightWalk-Upper-v33.png')
for i in range(20):
 y=i//5*512;x=i%5*512;sheet[y:y+232,x:x+512]=0
Image.fromarray(sheet).save(art/'RightWalkSheet-Body-v33.png')
print('20-frame lossless body/weapon split verified.')
