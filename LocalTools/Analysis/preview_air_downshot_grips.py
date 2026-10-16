"""Compare two-hand reach and released support arm using existing cutout art only."""
import math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

root=Path(__file__).resolve().parents[2]
out=root/'LocalTools/Analysis'
art=root/'Assets/RightWalkPrototype/Art'
scope={'__file__':str(out/'bake_stride_trial.py')}
exec(compile((out/'bake_stride_trial.py').read_text().split('\nns = read_parts()')[0],
             '<part-loader>','exec'),scope)
ns=scope['read_parts']()
legs=Image.open(root/'Assets/RightWalkPrototype/Resources/JumpTransition/AlternateAir.png').convert('RGBA')
weapon=Image.open(art/'RightWalk-WeaponHands-v44.png').convert('RGBA')
handmask=Image.new('L',(512,512))
ImageDraw.Draw(handmask).polygon([(386,153),(426,151),(427,174),(387,179)],fill=255)
pixels=np.array(weapon)
hand_pixels=pixels.copy()
hand_pixels[np.asarray(handmask)==0]=0
free_hand=Image.fromarray(hand_pixels)
pixels[np.asarray(handmask)>0]=0
one_hand_weapon=Image.fromarray(pixels)
near_grip=(311.45,166.33)
far_grip=(392,165.32)
trigger=(314,227)
rotation=math.pi/2-ns['angle'](near_grip,(464.15,143.84))
def rotate(v,a):
    return (v[0]*math.cos(a)-v[1]*math.sin(a),v[0]*math.sin(a)+v[1]*math.cos(a))
v=rotate((far_grip[0]-near_grip[0],far_grip[1]-near_grip[1]),rotation)
support=(trigger[0]+v[0],trigger[1]+v[1])
def layer(canvas,name,offset):
    canvas.alpha_composite(Image.open(art/(name+'.png')).convert('RGBA'),offset)
def arm(canvas,side,target):
    if side=='Near':
        shoulder,elbow,wrist=(234,164),(265,194),(338,180)
        offset=(3,4)
        upper='RightWalk-NearUpperArm-v47'
    else:
        shoulder,elbow,wrist=(250,160),(313,184),(389,173)
        offset=(13,3)
        upper='RightWalk-FarUpperArm-v48'
    h=(shoulder[0]+offset[0],shoulder[1]+offset[1])
    first,second=ns['dist'](shoulder,elbow),ns['dist'](elbow,wrist)
    distance=ns['dist'](h,target)
    solved=min(first+second-.01,max(abs(first-second)+.01,distance))
    phi=ns['angle'](h,target)
    bend=math.acos(max(-1,min(1,(first*first+solved*solved-second*second)/(2*first*solved))))
    sign=1 if (wrist[0]-shoulder[0])*(elbow[1]-shoulder[1])-(wrist[1]-shoulder[1])*(elbow[0]-shoulder[0])>0 else -1
    e=(h[0]+first*math.cos(phi+sign*bend),h[1]+first*math.sin(phi+sign*bend))
    w=(h[0]+solved*math.cos(phi),h[1]+solved*math.sin(phi))
    ns['draw_part'](canvas,Image.open(art/(upper+'.png')).convert('RGBA'),shoulder,h,ns['angle'](h,e)-ns['angle'](shoulder,elbow))
    ns['draw_part'](canvas,Image.open(art/('RightWalk-'+side+'Forearm-v43.png')).convert('RGBA'),elbow,e,ns['angle'](e,w)-ns['angle'](elbow,wrist))
    return max(0,distance-first-second),w,ns['angle'](e,w)

preview=Image.new('RGBA',(1024,580),(45,62,74,255))
draw=ImageDraw.Draw(preview)
for i,released in enumerate((False,True)):
    image=Image.new('RGBA',(512,512))
    layer(image,'RightWalk-Pelvis-Underlay-v46',(0,0))
    gap,w,fore_angle=arm(image,'Far',(214,251) if released else support)
    if released:
        ns['draw_part'](image,free_hand,far_grip,w,fore_angle)
    image.alpha_composite(legs)
    layer(image,'RightWalk-Hip-Clothing-v43',(0,0))
    layer(image,'RightWalk-Torso-v42',(10,0))
    layer(image,'RightWalk-HeadFace-v46',(2,4))
    layer(image,'RightWalk-CollarFront-v45',(10,0))
    ns['draw_part'](image,one_hand_weapon if released else weapon,near_grip,trigger,rotation)
    near_gap,_,_=arm(image,'Near',trigger)
    image.save(out/('air-downshot-one-hand-study.png' if released else 'air-downshot-two-hand-study.png'))
    preview.alpha_composite(image,(i*512,45))
    draw.text((i*512+15,15),'Support released / one hand' if released else f'Two hands / support reach gap {gap:.1f}px',fill='white')
    print(f'{"one" if released else "two"} hands: support reach gap={gap:.2f}px, trigger gap={near_gap:.2f}px')
preview.save(out/'air-downshot-grip-comparison.png')
print('Static study only; no Unity asset modifications.')
