"""Reuse one down-aim half by mirroring only arm/weapon layers."""
import json
import math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

HERE=Path(__file__).resolve().parent
# Reuse the existing cutout construction and transform helpers, not its output loop.
scope={'__file__':str(HERE/'preview_front_aim_motion.py')}
source=(HERE/'preview_front_aim_motion.py').read_text().split('# Offset rotation')[0]
exec(compile(source,'<front-cutout-helpers>','exec'),scope)
parts,arms,pivot=scope['parts'],scope['arms'],scope['pivot']
body=scope['body']
OUT=HERE/'FrontAimPreview'
original=scope['src']
head_mask=Image.new('L',original.size)
head_draw=ImageDraw.Draw(head_mask)
head_draw.rectangle((0,0,459,232),fill=255)
head_draw.polygon([(173,208),(304,196),(332,215),(326,251),(298,282),
                   (256,286),(220,274),(188,250)],fill=255)
protected=np.asarray(head_mask)>0
# Exclude fixed head pixels from every moving source, not only from the final render.
for name,part in list(parts.items()):
    pixels=np.array(part)
    pixels[protected,3]=0
    parts[name]=Image.fromarray(pixels)
    assert not np.any(np.asarray(parts[name])[protected,3])
body=Image.composite(original,body,head_mask)
head_layer=original.copy();head_layer.putalpha(head_mask)
head_layer.save(OUT/'fixed-head-layer.png')
tilt=math.degrees(math.atan2(265-pivot[0],575-pivot[1]))
mirror_coeff=(-1,0,float(2*pivot[0]),0,1,0)
torso_mask=Image.new('L',body.size)
ImageDraw.Draw(torso_mask).polygon([(112,235),(353,235),(372,405),
                                   (328,462),(286,470),(199,470),(147,439),(92,405)],fill=255)
torso_pixels=np.array(body)
torso_pixels[:,:,3]=np.asarray(torso_mask)
torso_pixels[protected,3]=0
torso_layer=Image.fromarray(torso_pixels)
torso_layer.save(OUT/'torso-layer.png')
# Torso and moving parts share one reflection axis to avoid independent alignment shifts.
reflected_torso=torso_layer.transform(body.size,Image.Transform.AFFINE,mirror_coeff,Image.Resampling.BICUBIC)

def overlay(angle):
    canvas=Image.new('RGBA',body.size)
    r=scope['rotation'](math.radians(angle))
    gaps=[]
    for side,shoulder,elbow,wrist,sign in arms:
        target=pivot+r@(wrist-pivot)
        first,second=np.linalg.norm(elbow-shoulder),np.linalg.norm(wrist-elbow)
        distance=np.linalg.norm(target-shoulder)
        reach=np.clip(distance,abs(first-second)+.001,first+second-.001)
        gaps.append(float(abs(distance-reach)))
        phi=scope['direction'](shoulder,target)
        bend=math.acos(np.clip((first*first+reach*reach-second*second)/(2*first*reach),-1,1))
        joint=shoulder+first*np.array((math.cos(phi+sign*bend),math.sin(phi+sign*bend)))
        solved=shoulder+reach*(target-shoulder)/distance
        scope['place'](canvas,parts[side+'-upper'],shoulder,shoulder,scope['direction'](shoulder,joint)-scope['direction'](shoulder,elbow))
        scope['place'](canvas,parts[side+'-fore'],elbow,joint,scope['direction'](joint,solved)-scope['direction'](elbow,wrist))
    scope['place'](canvas,parts['weapon-hands'],pivot,pivot,math.radians(angle))
    return canvas,gaps

def frame(offset):
    # Both halves reuse the same cutout pose and solve; no opposite reach extreme.
    moving,gaps=overlay(tilt-abs(offset))
    if offset>0:
        moving=moving.transform(moving.size,Image.Transform.AFFINE,mirror_coeff,Image.Resampling.BICUBIC)
    result=body.copy()
    if offset>0:
        result.alpha_composite(reflected_torso)
    result.alpha_composite(moving)
    result.alpha_composite(head_layer)
    assert np.array_equal(np.asarray(result)[protected],np.asarray(original)[protected])
    return result,gaps

frames=[];maximum_gap=0
for i in range(36):
    offset=12*math.sin(i*math.pi*2/36)
    image,gaps=frame(offset);maximum_gap=max(maximum_gap,*gaps)
    image=image.convert('RGB').resize((345,660),Image.Resampling.LANCZOS)
    draw=ImageDraw.Draw(image)
    draw.text((10,10),f'{offset:+.1f} deg / '+('mirrored' if offset>0 else 'source half'),fill='white')
    frames.append(image)
palette=frames[0].quantize(colors=256)
frames=[f.quantize(palette=palette) for f in frames]
frames[0].save(OUT/'front-aim-torso-mirrored-motion.gif',save_all=True,append_images=frames[1:],duration=110,loop=0)
board=Image.new('RGBA',(1380,920),(45,62,74,255))
for i,d in enumerate((-35,0,35)):
    image,_=frame(d);board.alpha_composite(image,(i*460,30))
    ImageDraw.Draw(board).text((i*460+15,10),f'{d:+d} deg'+(' / mirrored' if d>0 else ''),fill='white')
board.save(OUT/'front-aim-torso-mirrored-directions.png')
raw,_=overlay(tilt)
flipped=raw.transform(raw.size,Image.Transform.AFFINE,mirror_coeff,Image.Resampling.BICUBIC)
seam_changes=int(np.count_nonzero(np.any(np.asarray(raw)!=np.asarray(flipped),axis=2)))
assert np.array_equal(np.asarray(frame(-20)[0])[:230],np.asarray(frame(20)[0])[:230])
# Barrel vector is centered on the reflection axis at zero, so muzzle is continuous.
v=scope['rotation'](math.radians(tilt))@(np.array((265.,575.))-pivot)
assert abs(v[0])<1e-8
report={'preview_only':True,'torso_flipped_with_arms':True,'head_and_legs_flipped':False,'mirror_axis_x':float(pivot[0]),
        'head_pixels_preserved':True,'head_pixels_in_moving_cutouts':0,
        'slow_motion_range_deg':12,'shared_gif_palette':True,
        'muzzle_jump_at_zero_px':float(abs(2*v[0])),
        'arm_overlay_changed_pixels_at_zero':seam_changes,
        'max_reach_gap_px':maximum_gap,
        'limitations':['Asymmetric source hands/arms still change at mirror seam.','Approximate polygon cutouts; not final artwork.','No wrist-limit certification or live Unity test.']}
(OUT/'front-aim-torso-mirrored-report.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
