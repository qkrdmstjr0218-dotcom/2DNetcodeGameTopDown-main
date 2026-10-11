from pathlib import Path
import math
from PIL import Image, ImageDraw
import numpy as np

root=Path.cwd()
art=root/'Assets/RightWalkPrototype/Art'
# Reuse the accepted gait and its original torso/clothing cut, without rebaking v32.
ns={'__file__':str(root/'LocalTools/Analysis/bake_walk_v32.py')}
code=(root/'LocalTools/Analysis/bake_walk_v32.py').read_text()
exec(code.split('frames=[];points=[]')[0],ns)
upper=ns['upperRaised'].copy()
gunmask=Image.new('L',(512,512))
ImageDraw.Draw(gunmask).polygon([(292,135),(318,132),(318,110),(480,110),(480,169),(416,169),(401,165),(334,165),(318,161),(292,159)],fill=255)
g=np.array(gunmask)>0
u=np.array(upper);gun=u.copy();gun[~g]=0;weapon=Image.fromarray(gun)
weapon.save(art/'RightWalk-Weapon-v35.png')
u[g]=0
mask=Image.new('L',(512,512))
ImageDraw.Draw(mask).polygon([(236,167),(253,158),(268,163),(291,174),(313,166),(335,162),(355,162),(377,159),(402,160),(418,161),(421,182),(377,191),(350,202),(320,220),(288,222),(261,210),(244,194),(234,181)],fill=255)
m=np.array(mask)>0
arms=u.copy();arms[~m]=0
torso=u.copy();torso[m]=0
# The shoulder cap remains on the torso and overlaps the moving arm root.
shoulder=np.zeros((512,512),dtype=bool)
yy,xx=np.indices(shoulder.shape)
shoulder=((xx-234)**2+(yy-164)**2)<15**2
torso[shoulder]=u[shoulder]
Image.fromarray(torso).save(art/'RightWalk-Torso-v35.png')
Image.fromarray(arms).save(art/'RightWalk-Arms-v35.png')
sheet=Image.new('RGBA',(2560,2048))
for i in range(20):
    p=i/20;drop=1+math.cos(4*math.pi*p)
    frame=Image.new('RGBA',(512,512))
    frame.alpha_composite(ns['pelvis'].transform((512,512),Image.Transform.AFFINE,(1,0,0,0,1,-drop),Image.Resampling.BICUBIC))
    ns['leg'](frame,p+.5,ns['farHip'],ns['farKnee'],ns['farAnkle'],ns['farThigh'],ns['farCalf'],ns['farBoot'],drop)
    ns['leg'](frame,p,ns['nearHip'],ns['nearKnee'],ns['nearAnkle'],ns['nearThigh'],ns['nearCalf'],ns['nearBoot'],drop)
    sheet.alpha_composite(frame,(i%5*512,i//5*512))
sheet.save(art/'RightWalkSheet-Legs-v35.png')
preview=Image.new('RGBA',(1536,512),(14,19,25,255))
for j,angle in enumerate([-12,0,12]):
    frame=sheet.crop((0,0,512,512));frame.alpha_composite(Image.fromarray(torso))
    frame.alpha_composite(weapon.rotate(angle,Image.Resampling.BICUBIC,center=(234,164)))
    frame.alpha_composite(Image.fromarray(arms).rotate(angle,Image.Resampling.BICUBIC,center=(234,164)))
    preview.alpha_composite(frame,(512*j,0))
preview.convert('RGB').save(root/'LocalTools/Captures/arms-preview-v35.jpg')
print('Separated fixed torso/clothing and shoulder aim arms; accepted leg trajectories retained.')
