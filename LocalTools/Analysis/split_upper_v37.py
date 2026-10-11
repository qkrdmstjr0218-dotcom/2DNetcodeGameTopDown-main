from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np
root=Path.cwd();art=root/'Assets/RightWalkPrototype/Art'
ns={'__file__':str(root/'LocalTools/Analysis/bake_walk_v32.py')}
exec((root/'LocalTools/Analysis/bake_walk_v32.py').read_text().split('frames=[];points=[]')[0],ns)
source=np.array(ns['upperRaised']); yy,xx=np.indices((512,512))
def polygon(points):
    m=Image.new('L',(512,512));ImageDraw.Draw(m).polygon(points,fill=255);return np.array(m)>0
# Cut the head at the neck, retaining the complete ponytail.
headmask=polygon([(0,0),(330,0),(330,109),(320,121),(308,131),(294,136),(279,136),(267,132),(264,123),(251,123),(244,119),(234,116),(222,113),(215,132),(200,148),(186,166),(0,180)])
gunmask=polygon([(292,135),(318,132),(318,110),(480,110),(480,169),(416,169),(401,165),(334,165),(318,161),(292,159)])&~headmask
near=polygon([(210,141),(237,140),(251,150),(250,167),(242,175),(263,186),(281,186),(299,175),(320,166),(337,159),(354,162),(361,173),(354,193),(340,207),(319,215),(295,215),(276,214),(249,204),(227,202),(211,188),(205,165)])&~headmask&~gunmask
far=polygon([(268,169),(288,169),(311,163),(335,166),(378,156),(418,159),(421,183),(397,187),(376,182),(353,184),(327,195),(309,201),(282,191),(266,177)])&~headmask&~gunmask&~near
# Joint cuts are perpendicular to the reference bone, not horizontal image rows.
nearUpper=near&(((xx-265)*73+(yy-194)*-14)<=0)
nearHand=near&polygon([(329,159),(365,158),(365,193),(337,196),(328,181)])
nearLower=near&~nearUpper&~nearHand
farUpper=far&(((xx-313)*76+(yy-184)*-11)<=0)
farHand=far&(xx>=376)
farLower=far&~farUpper&~farHand
masks={'Head':headmask,'Weapon':gunmask,'NearUpperArm':nearUpper,'NearForearm':nearLower,'NearHand':nearHand,'FarUpperArm':farUpper,'FarForearm':farLower,'FarHand':farHand}
occupied=np.zeros((512,512),bool)
for name,mask in masks.items():
    # Core masks partition the artwork before adding joint overlaps.
    occupied|=mask
    out=source.copy();out[~mask]=0;Image.fromarray(out).save(art/f'RightWalk-{name}-v37.png')
# Remove disconnected cut remnants from each layer; these are not anatomical parts.
def clean(out):
    seen=np.zeros((512,512),bool)
    for y,x in zip(*np.where(out[:,:,3]>0)):
        if seen[y,x]:continue
        stack=[(y,x)];seen[y,x]=True;group=[]
        while stack:
            qy,qx=stack.pop();group.append((qy,qx))
            for dy,dx in [(0,1),(0,-1),(1,0),(-1,0)]:
                ny,nx=qy+dy,qx+dx
                if 0<=ny<512 and 0<=nx<512 and not seen[ny,nx] and out[ny,nx,3]>0:
                    seen[ny,nx]=True;stack.append((ny,nx))
        if len(group)<45:
            for qy,qx in group:out[qy,qx]=0
    return out
# Conceal small joint cuts by retaining original edge pixels on both adjacent pieces.
for side,whole,cx,cy in [('Near',near,265,194),('Far',far,313,184)]:
    overlap=whole&(((xx-cx)**2+(yy-cy)**2)<8**2)
    masks[side+'UpperArm']|=overlap;masks[side+'Forearm']|=overlap
for name,mask in masks.items():
    out=source.copy();out[~mask]=0
    Image.fromarray(clean(out)).save(art/f'RightWalk-{name}-v37.png')
torso=source.copy();torso[occupied]=0
# The torso ends at its garment outline; residual hand/forearm slivers belong to arms.
torso[(xx>302)&(yy>195)&(yy<232)]=0
torso[(xx<205)&(yy<210)]=0
torso[(xx>291)&(yy>169)&(yy<193)]=0
torso[(xx>289)&(yy>211)&(yy<232)]=0
base=Image.new('RGBA',(512,512))
repair=Image.open(art/'Torso-Repair-Reference-v37.png').convert('RGBA')
repair=repair.crop(repair.getbbox()).resize((137,156),Image.Resampling.LANCZOS)
base.alpha_composite(repair,(165,130))
a=np.array(base)
fill=polygon([(218,132),(251,135),(276,145),(289,158),(295,184),(290,202),(278,225),(237,227),(216,204),(207,173)])
a[~fill]=0;base=Image.fromarray(a)
base.alpha_composite(Image.fromarray(clean(torso)))
base.save(art/'RightWalk-Torso-v37.png')
print('Corrected head/gun ownership, chest boundary, disconnected scraps and elbow overlap.')
