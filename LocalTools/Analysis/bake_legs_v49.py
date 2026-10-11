from pathlib import Path
from PIL import Image, ImageFilter
import math, numpy as np
root=Path(__file__).resolve().parents[2]
p=root/'Assets/RightWalkPrototype/Art'
ns={'__file__':str(root/'LocalTools/Analysis/bake_walk_v32.py')}
prefix=(root/'LocalTools/Analysis/bake_walk_v32.py').read_text().split('frames=[];points=[]')[0].replace('v32','v49')
exec(prefix,ns)
nearCalf=Image.open(p/'WalkPart-Calf-Refined-v49.png').convert('RGBA')
boot=Image.open(p/'WalkPart-Boot-Refined-v49.png').convert('RGBA')
def mapped(im,a,b,c,d):
 a,b,c,d=[np.array(v,float) for v in (a,b,c,d)]
 z=complex(*(d-c))/complex(*(b-a));m=np.array([[z.real,-z.imag],[z.imag,z.real]])
 inv=np.linalg.inv(m);shift=-inv@(c-m@a)
 return im.transform((512,512),Image.Transform.AFFINE,(*inv[0],shift[0],*inv[1],shift[1]),Image.Resampling.BICUBIC)
farCalf=mapped(nearCalf,ns['nearKnee'],ns['nearAnkle'],ns['farKnee'],ns['farAnkle'])
# Preserve existing textured trousers at the hip overlap, rather than drawing a new cover.
a=np.array(ns['source']);y,x=np.indices((512,512));keep=((x-265)**2+(y-260)**2<28**2)&(a[:,:,:3].mean(2)<140)
a[~keep]=0;hipCap=Image.fromarray(a)
draw=ns['draw_part'];angle=ns['angle'];dist=ns['dist']
def leg(canvas,phase,hip,knee,foot,thigh,calf,drop):
 target,tilt=ns['ankle'](phase);target=(target[0]+2,target[1]);dst=(hip[0]-12,250+drop)
 l1=dist(hip,knee);l2=dist(knee,foot);dx=target[0]-dst[0];dy=target[1]-dst[1];length=min(math.hypot(dx,dy),l1+l2-.01)
 phi=math.atan2(dy,dx);bend=math.acos(max(-1,min(1,(l1*l1+length*length-l2*l2)/(2*l1*length))))
 k=(dst[0]+l1*math.cos(phi-bend),dst[1]+l1*math.sin(phi-bend))
 th=angle(dst,k)-angle(hip,knee)
 draw(canvas,hipCap,ns['nearHip'],dst,th)
 draw(canvas,thigh,hip,dst,th)
 draw(canvas,calf,knee,k,angle(k,target)-angle(knee,foot))
 draw(canvas,boot,ns['nearAnkle'],(target[0]+10,target[1]),tilt)
 draw(canvas,ns['nearKneePatch'],ns['nearKnee'],k,angle(k,target)-angle(ns['nearKnee'],ns['nearAnkle']))
sheet=Image.new('RGBA',(2560,2048))
for i in range(20):
 phase=i/20;drop=1+math.cos(4*math.pi*phase);im=Image.new('RGBA',(512,512))
 leg(im,phase+.5,ns['farHip'],ns['farKnee'],ns['farAnkle'],ns['farThigh'],farCalf,drop)
 leg(im,phase,ns['nearHip'],ns['nearKnee'],ns['nearAnkle'],ns['nearThigh'],nearCalf,drop)
 a=np.array(im);a[a[:,:,3]==0]=0;im=Image.fromarray(a)
 sheet.alpha_composite(im,(i%5*512,i//5*512))
sheet.save(p/'RightWalkSheet-Legs-v49.png')
print('20 frames: unchanged v32 trajectories, fixed-size refined calf and boots, hip overlap.')
