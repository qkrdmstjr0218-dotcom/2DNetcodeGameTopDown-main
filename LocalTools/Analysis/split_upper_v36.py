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
headmask=polygon([(0,0),(512,0),(512,133),(322,133),(300,141),(265,141),(240,127),(217,129),(209,166),(0,180)])
gunmask=polygon([(292,135),(318,132),(318,110),(480,110),(480,169),(416,169),(401,165),(334,165),(318,161),(292,159)])&~headmask
near=polygon([(210,141),(237,140),(253,148),(256,158),(270,168),(281,181),(299,175),(320,166),(337,159),(354,162),(361,173),(354,193),(340,207),(319,215),(295,215),(276,214),(249,204),(227,202),(211,188),(205,165)])&~headmask&~gunmask
far=polygon([(265,155),(281,154),(313,164),(335,166),(378,156),(418,159),(421,183),(397,187),(376,182),(353,184),(327,195),(309,201),(282,191),(266,177)])&~headmask&~gunmask&~near
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
    assert not (occupied&mask).any(),name
    occupied|=mask
    out=source.copy();out[~mask]=0;Image.fromarray(out).save(art/f'RightWalk-{name}-v36.png')
torso=source.copy();torso[occupied]=0
Image.fromarray(torso).save(art/'RightWalk-Torso-v36.png')
rest=torso.copy()
for mask in masks.values():rest[mask]=source[mask]
assert np.array_equal(rest,source)
print('Nine disjoint layers reconstruct the original upper body exactly at rest.')
