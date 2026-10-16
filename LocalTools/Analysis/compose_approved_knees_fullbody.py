"""Register knee detail only; explicitly exclude the old calves in its crop."""
from pathlib import Path
import math
import numpy as np
from PIL import Image,ImageDraw,ImageFilter

OUT=Path(__file__).resolve().parent
BASE='C:/Users/PC/.codex/generated_images/01a12d87-d09d-7f20-a556-97e2f4b8fa44/exec-6417323b-8078-49c7-9757-b5061129bb74.png'
DETAIL='C:/Users/PC/.codex/generated_images/01a12d87-d09d-7f20-a556-97e2f4b8fa44/exec-9fd2f12f-b3a3-4c47-8111-d4b1f729df86.png'
base=Image.open(BASE).convert('RGBA')
detail=Image.open(DETAIL).convert('RGBA')
# Register using lateral gold knee-joint centers rather than the crop's bounds.
source=np.array([[435.,424.],[1719.,426.]])
target=np.array([[333.,991.],[723.,991.]])
sv=source[1]-source[0];tv=target[1]-target[0]
scale=np.linalg.norm(tv)/np.linalg.norm(sv)
angle=math.atan2(tv[1],tv[0])-math.atan2(sv[1],sv[0])
c,s=math.cos(angle),math.sin(angle)
forward=scale*np.array(((c,-s),(s,c)))
inverse=np.linalg.inv(forward)
offset=source[0]-inverse@target[0]
coeff=(inverse[0,0],inverse[0,1],offset[0],inverse[1,0],inverse[1,1],offset[1])
registered=detail.transform(base.size,Image.Transform.AFFINE,coeff,Image.Resampling.BICUBIC)
mask=Image.new('L',base.size)
draw=ImageDraw.Draw(mask)
draw.polygon([(355,887),(450,887),(450,963),(437,1008),(410,1053),
              (381,1070),(359,1048),(344,1004),(346,944)],fill=255)
draw.polygon([(593,887),(691,887),(703,947),(708,1008),(688,1047),
              (661,1070),(633,1051),(609,1007),(593,961)],fill=255)
soft=mask.filter(ImageFilter.MinFilter(5)).filter(ImageFilter.GaussianBlur(2))
mask=Image.fromarray(np.minimum(np.asarray(mask),np.asarray(soft)))
pixels=np.asarray(registered)[:,:,:3].astype(int)
old=np.asarray(base)[:,:,:3]
paper=(pixels.min(axis=2)>244)&((pixels.max(axis=2)-pixels.min(axis=2))<5)&(old.max(axis=2)<210)
coverage=np.array(mask);coverage[paper]=0
mask=Image.fromarray(coverage)
result=Image.composite(registered,base,mask)
before,after=np.asarray(base),np.asarray(result)
changed=np.any(before!=after,axis=2)
assert not np.any(changed&(np.asarray(mask)==0))
assert np.array_equal(before[1071:],after[1071:])
result.save(OUT/'front-approved-knees-fullbody-preview.png')
result.crop((295,875,755,1100)).resize((920,450),Image.Resampling.NEAREST).save(OUT/'front-approved-knees-composite-detail.png')
print('PASS: existing calves and boots preserved exactly; only knee/thigh-end masks changed.')
