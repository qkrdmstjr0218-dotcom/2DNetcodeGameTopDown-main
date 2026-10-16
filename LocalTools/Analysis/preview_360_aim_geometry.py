"""Validate a mouse-target barrel solve without installing an unapproved pose."""
import contextlib
import io
import json
import math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

with contextlib.redirect_stdout(io.StringIO()):
    import audit_existing_aim_path as rig

OUT=Path(__file__).resolve().parent/'AimPathAudit'
anchor=rig.pose(-55,'fixed-ground-extension')
pivot=np.array((anchor['pivot_x'],anchor['pivot_y']))
muzzle_local=rig.grips['muzzle']

def solve(target):
    delta=target-pivot
    distance=float(np.linalg.norm(delta))
    # A target behind the muzzle along its barrel cannot be shot by this pose.
    if distance<=abs(muzzle_local[1]):return None
    forward=math.sqrt(distance*distance-muzzle_local[1]**2)-muzzle_local[0]
    if forward<=0:return None
    angle=math.atan2(delta[1],delta[0])-math.asin(muzzle_local[1]/distance)
    degrees=math.degrees(angle)
    muzzle=pivot+rig.rot(degrees)@muzzle_local
    direction=np.array((math.cos(angle),math.sin(angle)))
    return degrees,muzzle,direction,forward

def pixel(point):
    return (320+int((point[0]-pivot[0])*85),320-int((point[1]-pivot[1])*85))

frames=[]
errors=[]
for i in range(72):
    requested=i*5
    radians=math.radians(requested)
    target=pivot+2.6*np.array((math.cos(radians),math.sin(radians)))
    angle,muzzle,direction,forward=solve(target)
    predicted=muzzle+direction*forward
    error=float(np.linalg.norm(predicted-target))
    assert error<1e-9
    assert abs(np.linalg.norm(direction)-1)<1e-9
    errors.append(error)
    image=Image.new('RGB',(640,680),(245,247,249))
    draw=ImageDraw.Draw(image)
    draw.ellipse((99,99,541,541),outline=(190,198,205),width=1)
    center=pixel(pivot);mp=pixel(muzzle);tp=pixel(target)
    draw.line((center,mp),fill=(55,65,72),width=8)
    draw.line((mp,tp),fill=(20,145,115),width=2)
    for t in (.15,.4,.65,.9):
        shot=pixel(muzzle+direction*forward*t)
        draw.ellipse((shot[0]-3,shot[1]-3,shot[0]+3,shot[1]+3),fill=(210,145,25))
    draw.ellipse((center[0]-5,center[1]-5,center[0]+5,center[1]+5),fill=(70,90,180))
    draw.line((tp[0]-6,tp[1]-6,tp[0]+6,tp[1]+6),fill=(190,65,65),width=2)
    draw.line((tp[0]-6,tp[1]+6,tp[0]+6,tp[1]-6),fill=(190,65,65),width=2)
    draw.text((20,15),'360 mouse-target solve / geometry study only',fill=(30,40,45))
    draw.text((20,610),f'Target {requested:03d} deg / barrel {angle:.2f} deg',fill=(30,40,45))
    draw.text((20,635),'Target is on forward muzzle ray. No sprite flipping.',fill=(30,40,45))
    frames.append(image)
frames[0].save(OUT/'mouse-target-360.gif',save_all=True,append_images=frames[1:],duration=70,loop=0)
for radius in (1.05,1.4,2.6,5):
    for d in range(360):
        a=math.radians(d)
        target=pivot+radius*np.array((math.cos(a),math.sin(a)))
        angle,muzzle,direction,distance=solve(target)
        errors.append(float(np.linalg.norm(muzzle+direction*distance-target)))
assert solve(pivot) is None
assert solve(pivot+np.array((.2,0))) is None

arm_rows=[]
for d in range(-55,-91,-1):
    row=rig.pose(d,'fixed-ground-extension')
    arm_rows.append({k:v for k,v in row.items() if 'reach_gap' in k or 'wrist_rotation' in k or k=='requested_deg'})
report={'runtime_installed':False,'live_game_tested':False,
        'tested_target_angles':360,'tested_radii':[1.05,1.4,2.6,5],
        'max_target_ray_error_units':max(errors),
        'near_target_policy':'reject targets inside muzzle reach; cannot fire backwards along barrel',
        'arm_geometry':'Existing side rig ONLY. Not a validation of front-facing cutout anatomy.',
        'down_sector':arm_rows}
(OUT/'mouse-target-360-report.json').write_text(json.dumps(report,indent=2))
print(f'PASS: 1440 angle/radius cases; max target-ray error {max(errors):.3g} units.')
print('PASS: near-pivot and inside-muzzle targets rejected.')
print('No runtime scripts, scenes, or character textures modified.')
