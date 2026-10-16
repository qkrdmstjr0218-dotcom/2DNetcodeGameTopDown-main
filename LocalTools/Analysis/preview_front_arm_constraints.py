"""Use source cutout joint estimates to expose, not conceal, impossible arm poses."""
import json
import math
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw
from front_arm_constraints import solve

OUT=Path(__file__).resolve().parent/'FrontAimPreview'
OUT.mkdir(exist_ok=True)
pivot=np.array((214.,361.))
arms=[('Trigger',np.array((150.,259.)),np.array((127.,309.)),np.array((196.,361.))),
      ('Support',np.array((326.,279.)),np.array((324.,347.)),np.array((283.,435.)))]
def rotation(d):
    a=math.radians(d);c,s=math.cos(a),math.sin(a)
    return np.array(((c,-s),(s,c)))
def sample(d):
    result=[]
    for name,h,e,w in arms:
        target=pivot+rotation(d)@(w-pivot)
        check=solve(h,e,w,target,d,wrist_limit=45)
        slide=0
        if name=='Support' and not check['valid']:
            barrel=np.array((51.,214.));barrel/=np.linalg.norm(barrel)
            # Only support-hand contact may slide along the fore-end. Gun path stays fixed.
            for candidate in sorted(range(-40,41),key=abs):
                moved=target+rotation(d)@barrel*candidate
                solved=solve(h,e,w,moved,d,wrist_limit=45)
                if solved['valid']:
                    target,check,slide=moved,solved,candidate
                    break
        check['support_contact_slide_px']=slide
        result.append((name,h,target,check))
    return result

rows=[];frames=[]
for i in range(72):
    degrees=35*math.sin(i*math.pi*2/72)
    image=Image.new('RGB',(460,570),(245,247,249));draw=ImageDraw.Draw(image)
    draw.text((15,15),'Front arm constraints / estimated source joints',fill=(25,35,40))
    draw.text((15,38),f'Gun rotation from source: {degrees:+.1f} deg',fill=(25,35,40))
    # Keep source joint coordinates; diagram deliberately excludes unapproved artwork.
    draw.line(((150,259),(326,279)),fill=(165,175,185),width=2)
    muzzle=pivot+rotation(degrees)@(np.array((265.,575.))-pivot)
    draw.line((tuple(pivot),tuple(muzzle)),fill=(55,65,75),width=9)
    records=[]
    for n,(name,h,target,result) in enumerate(sample(degrees)):
        color=(20,145,115) if result['valid'] else (200,65,55)
        joint=result['elbow']
        if joint is not None:
            draw.line([tuple(h),tuple(joint),tuple(target)],fill=color,width=6)
            assert abs(np.linalg.norm(np.array(joint)-h)-np.linalg.norm(arms[n][2]-h))<1e-6
            assert abs(np.linalg.norm(target-np.array(joint))-np.linalg.norm(arms[n][3]-arms[n][2]))<1e-6
        else:draw.line((tuple(h),tuple(target)),fill=color,width=1)
        for p in (h,target):draw.ellipse((p[0]-5,p[1]-5,p[0]+5,p[1]+5),fill=color)
        delta=result.get('wrist_delta')
        diagnostic='no reach' if delta is None else f'wrist {delta:+.1f} deg'
        draw.text((15,72+n*22),f'{name}: {result["reason"]} / {diagnostic}',fill=color)
        records.append({'arm':name,**result})
    rows.append({'gun_delta':degrees,'arms':records})
    frames.append(image)
frames[0].save(OUT/'arm-constraint-study.gif',save_all=True,append_images=frames[1:],duration=70,loop=0)
keys=[]
for d in (-35,0,35):keys.append({'gun_delta':d,'arms':[{'name':n,**r} for n,h,t,r in sample(d)]})
(OUT/'arm-constraint-study.json').write_text(json.dumps({'wrist_limit_deg':45,'elbow_limit_deg':155,'support_slide_limit_px':40,'limits_are_provisional':True,'reference':'front-aim-source-crop, manually estimated joints','production_art':False,'key_angles':keys,'samples':rows},indent=2))
for name,h,e,w in arms:
    r=solve(h,e,w,w,0)
    assert r['valid'] and abs(r['wrist_delta'])<1e-5
    assert not solve(h,e,w,h+np.array((1000,0)),0)['valid']
    assert not solve(h,e,w,h,0)['valid']
print(json.dumps(keys,indent=2))
print('PASS: baseline, unreachable targets, zero-distance targets and limb-length invariants.')
