from pathlib import Path
import math
import numpy as np
from PIL import Image,ImageDraw,ImageFilter
root=Path(__file__).resolve().parents[2];art=root/'Assets/RightWalkPrototype/Art'
source=Image.open(art/'RightWalkSheet-Redrawn-v5.png').convert('RGBA').crop((0,0,512,512))
repair=Image.open(art/'Armor-Repair-Reference-v18.png').convert('RGBA').resize((512,512),Image.Resampling.LANCZOS)
mask=Image.new('L',(512,512));d=ImageDraw.Draw(mask)
d.rectangle((212,315,236,350),fill=255)
d.rectangle((360,402,395,432),fill=255)
mask=mask.filter(ImageFilter.GaussianBlur(1.2))
source=Image.composite(repair,source,mask)
def cut(poly):
    mask=Image.new('L',(512,512));ImageDraw.Draw(mask).polygon(poly,fill=255)
    im=source.copy();im.putalpha(Image.fromarray(np.minimum(np.array(mask),np.array(source)[:,:,3])))
    return im
upper=cut([(0,0),(512,0),(512,228),(294,228),(280,244),(241,245),(234,280),(210,299),(160,280),(0,275)])
upperRaised=Image.new('RGBA',(512,512));upperRaised.alpha_composite(upper,(0,-10))
nearHip=(265,260);nearKnee=(300,335);nearAnkle=(342,430)
farHip=(263,276);farKnee=(199,337);farAnkle=(131,410)
nearThigh=cut([(240,242),(278,242),(327,291),(325,337),(294,355),(255,315),(234,280)])
nearCalf=cut([(282,314),(319,308),(340,343),(365,391),(364,429),(337,441),(313,408),(289,370),(274,340)])
nearBoot=cut([(315,420),(323,409),(338,398),(353,399),(366,404),(372,415),(386,419),(409,425),(445,428),(448,474),(393,480),(312,470),(309,437)])
farThigh=cut([(239,263),(278,276),(252,307),(225,337),(199,355),(178,338),(205,298)])
farCalf=cut([(180,326),(215,319),(238,338),(208,375),(165,422),(128,427),(112,408),(133,371)])
farBoot=cut([(115,397),(145,400),(152,422),(182,432),(207,438),(205,468),(163,472),(98,444),(89,426)])
# Cut each segment at its joint plane, excluding pixels from neighbouring parts.
def trim_end(im,start,end):
    a=np.array(im);yy,xx=np.indices(a.shape[:2]);dx=end[0]-start[0];dy=end[1]-start[1]
    beyond=((xx-end[0])*dx+(yy-end[1])*dy)/math.hypot(dx,dy)>5
    a[beyond]=0;return Image.fromarray(a)
nearThigh=trim_end(nearThigh,nearHip,nearKnee)
farThigh=trim_end(farThigh,farHip,farKnee)
nearCalf=trim_end(nearCalf,nearKnee,nearAnkle)
farCalf=trim_end(farCalf,farKnee,farAnkle)
def trim_start(im,start,end):
    a=np.array(im);yy,xx=np.indices(a.shape[:2]);dx=end[0]-start[0];dy=end[1]-start[1]
    above=((xx-start[0])*dx+(yy-start[1])*dy)/math.hypot(dx,dy)<-18
    a[above]=0;return Image.fromarray(a)
nearCalf=trim_start(nearCalf,nearKnee,nearAnkle)
farCalf=trim_start(farCalf,farKnee,farAnkle)
a=np.array(farThigh);yy,xx=np.indices(a.shape[:2]);a[(xx>230)&(yy<300)&(a[:,:,:3].mean(2)>130)]=0
farThigh=Image.fromarray(a)
def extend_existing_edge(im,rect,steps):
    # Fill only adjacent missing pixels with their neighbouring source colors.
    a=np.array(im);yy,xx=np.indices(a.shape[:2]);x0,y0,x1,y1=rect
    region=(xx>=x0)&(xx<=x1)&(yy>=y0)&(yy<=y1)
    for _ in range(steps):
        old=a.copy();chosen=np.zeros(a.shape[:2],dtype=bool)
        for dy,dx in [(0,-1),(1,0),(-1,0),(0,1),(-1,-1),(1,-1),(-1,1),(1,1)]:
            n=np.roll(old,(dy,dx),(0,1))
            take=region&(old[:,:,3]<20)&(n[:,:,3]>200)&~chosen
            a[take]=n[take];chosen|=take
    return Image.fromarray(a)
# Recover only the small marked notch at the existing instep's upper edge.
nearBoot=extend_existing_edge(nearBoot,(365,408,392,428),3)
# Restore original ankle/instep pixels in the boot cutout.
# Both feet use the same boot source and pivot, keeping identical sole height.
farBoot=nearBoot.copy()
parts=[nearThigh,nearCalf,nearBoot,farThigh,farCalf,farBoot]
names=['near-thigh','near-calf','near-boot','far-thigh','far-calf','far-boot']
for name,im in zip(names,parts):im.save(art/('WalkPart-'+name+'-v18.png'))
def joint_patch(center,radius):
    mask=Image.new('L',(512,512));ImageDraw.Draw(mask).ellipse((center[0]-radius,center[1]-radius,center[0]+radius,center[1]+radius),fill=255)
    mask=mask.filter(ImageFilter.GaussianBlur(.65))
    im=source.copy();im.putalpha(Image.fromarray(np.minimum(np.array(mask),np.array(source)[:,:,3])))
    return im
# Restore the intact opposite knee guard, including its white shell above the hinge.
farKneePatch=joint_patch(farKnee,27)
farKneePatch=extend_existing_edge(farKneePatch,(215,319,234,342),3)
nearKneePatch=farKneePatch.copy()
nearKneePatch=extend_existing_edge(nearKneePatch,(214,316,237,343),3)
anklePatch=joint_patch(nearAnkle,14)
def dist(a,b):return math.hypot(b[0]-a[0],b[1]-a[1])
def angle(a,b):return math.atan2(b[1]-a[1],b[0]-a[0])
def draw_part(canvas,part,src,dst,theta):
    # Inverse rigid transform: rotation and translation, no resizing.
    c=math.cos(theta);s=math.sin(theta)
    tx=src[0]-c*dst[0]-s*dst[1];ty=src[1]+s*dst[0]-c*dst[1]
    transformed=part.transform((512,512),Image.Transform.AFFINE,(c,s,tx,-s,c,ty),Image.Resampling.BICUBIC)
    canvas.alpha_composite(transformed)
def ankle(phase):
    t=phase%1
    if t<.6:return (305-80*t/.6,430),0
    u=(t-.6)/.4;ease=u*u*(3-2*u)
    return (225+80*ease,430-18*math.sin(math.pi*u)),.12*math.sin(2*math.pi*u)
def leg(canvas,phase,hip,knee,foot,thigh,calf,boot):
    target,bootangle=ankle(phase);l1=dist(hip,knee);l2=dist(knee,foot)
    target=(target[0]-12,target[1])
    dstHip=(hip[0]-12,250)
    dx=target[0]-dstHip[0];dy=target[1]-dstHip[1];d=math.hypot(dx,dy)
    d=min(d,l1+l2-.01)
    phi=math.atan2(dy,dx);bend=math.acos(max(-1,min(1,(l1*l1+d*d-l2*l2)/(2*l1*d))))
    # Knee bends towards the facing direction throughout support and swing.
    k=(dstHip[0]+l1*math.cos(phi-bend),dstHip[1]+l1*math.sin(phi-bend))
    draw_part(canvas,thigh,hip,dstHip,angle(dstHip,k)-angle(hip,knee))
    draw_part(canvas,calf,knee,k,angle(k,target)-angle(knee,foot))
    draw_part(canvas,boot,nearAnkle,target,bootangle)
    # Reuse original joint armor over cut edges, preserving its texture and size.
    kneePatch=nearKneePatch if hip==nearHip else farKneePatch
    draw_part(canvas,kneePatch,farKnee,k,angle(k,target)-angle(farKnee,farAnkle))
    # The intact original boot cuff overlaps the calf; no separate instep guard.
    return {'hip':dstHip,'knee':k,'ankle':target}
frames=[];points=[]
for i in range(20):
    p=i/20;im=Image.new('RGBA',(512,512))
    far=leg(im,p+.5,farHip,farKnee,farAnkle,farThigh,farCalf,farBoot)
    near=leg(im,p,nearHip,nearKnee,nearAnkle,nearThigh,nearCalf,nearBoot)
    # Torso and belt cover both leg layers, with no fixed thigh pixels on the torso.
    im.alpha_composite(upperRaised)
    a=np.array(im);a[:232]=np.array(upperRaised)[:232];im=Image.fromarray(a)
    frames.append(im);points.append({'frame':i,'near':near,'far':far})
sheet=Image.new('RGBA',(2560,2048))
for i,im in enumerate(frames):sheet.alpha_composite(im,(i%5*512,i//5*512))
sheet.save(art/'RightWalkSheet-Redrawn-v18.png')
preview=Image.new('RGBA',sheet.size,(14,19,25,255));preview.alpha_composite(sheet)
preview.resize((1250,1000)).convert('RGB').save(root/'LocalTools/Captures/baked-walk-v18.jpg')
import json
(root/'LocalTools/Analysis/baked-walk-v18.json').write_text(json.dumps(points,indent=2))
print('20 frames baked with fixed-size parts and continuous opposite foot trajectories.')
