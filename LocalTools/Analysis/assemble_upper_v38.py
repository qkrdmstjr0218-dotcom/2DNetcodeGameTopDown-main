from pathlib import Path
from PIL import Image
import numpy as np
root=Path.cwd();art=root/'Assets/RightWalkPrototype/Art'
def fit(name,rect):
    im=Image.open(art/name).convert('RGBA');a=np.array(im);ys,xs=np.where(a[:,:,3]>100);box=(int(xs.min())-2,int(ys.min())-2,int(xs.max())+3,int(ys.max())+3);a[a[:,:,3]<10]=0;im=Image.fromarray(a).crop(box)
    x,y,w,h=rect;im=im.resize((w,h),Image.Resampling.LANCZOS)
    out=Image.new('RGBA',(512,512));out.alpha_composite(im,(x,y));return out
def save(im,name):im.save(art/f'RightWalk-{name}-v38.png')
# Replace the complete torso, rather than covering a generated patch with old fragments.
save(fit('Torso-Repair-Reference-v37.png',(165,130,137,156)),'Torso')
save(fit('Head-Repair-Reference-v38.png',(117,28,210,146)),'Head')
def similarity(name,a,b,c,d):
    im=Image.open(art/name).convert('RGBA')
    a,b,c,d=[np.array(v,float) for v in (a,b,c,d)]
    u=b-a;v=d-c;z=complex(*v)/complex(*u)
    matrix=np.array([[z.real,-z.imag],[z.imag,z.real]])
    offset=c-matrix@a;inv=np.linalg.inv(matrix);shift=-inv@offset
    return im.transform((512,512),Image.Transform.AFFINE,(*inv[0],shift[0],*inv[1],shift[1]),Image.Resampling.BICUBIC)
save(similarity('Near-Upper-Repair-Reference-v38.png',(588,376),(681,517),(234,164),(265,194)),'NearUpperArm')
for name in ['NearForearm','NearHand','Weapon']:
    im=Image.open(art/f'RightWalk-{name}-v37.png').convert('RGBA')
    if name=='NearForearm':
        a=np.array(im);yy,xx=np.indices((512,512));a[(xx<272)&(yy<201)]=0;im=Image.fromarray(a)
    save(im,name)
# Register the far arm's shoulder, elbow and wrist to the existing IK rest joints.
im=Image.open(art/'Far-Arm-Repair-Reference-v38.png').convert('RGBA')
src=np.array([[610,380,1],[795,510,1],[1015,440,1]],float)
dst=np.array([[250,160],[313,184],[389,173]],float)
coef=np.linalg.solve(src,dst);matrix=coef[:2].T;offset=coef[2]
inv=np.linalg.inv(matrix);shift=-inv@offset
arm=np.array(im.transform((512,512),Image.Transform.AFFINE,(*inv[0],shift[0],*inv[1],shift[1]),Image.Resampling.BICUBIC))
yy,xx=np.indices((512,512))
projection=((xx-313)*76+(yy-184)*-11)/np.hypot(76,11)
handProjection=((xx-389)*76+(yy-173)*-11)/np.hypot(76,11)
for name,mask in [('FarUpperArm',projection<=7),('FarForearm',(projection>=-7)&(handProjection<=3)),('FarHand',handProjection>=-3)]:
    out=arm.copy();out[~mask]=0;save(Image.fromarray(out),name)
print('Whole torso replaced; head and both upper arms completed and registered to rig joints.')
