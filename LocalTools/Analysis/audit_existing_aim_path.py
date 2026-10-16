"""Offline direction-mode audit of serialized aim geometry, not live Unity capture."""
import csv
import json
import math
import re
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent/'AimPathAudit'
OUT.mkdir(exist_ok=True)
text=(ROOT/'Assets/RightWalkPrototype/SideScrollNetworkPlayer.prefab').read_text(encoding='utf-8-sig')
# Read only Unity's known flat serialized vector/reference fields, with strict assertions.
objects={int(m[1]):m[2] for m in re.finditer(r'--- !u!\d+ &(\d+)\n(.*?)(?=\n--- !u!|\Z)',text,re.S)}
def vector(block,field):
    match=re.search(r'^  '+re.escape(field)+r': \{x: ([^,]+), y: ([^,}]+)',block,re.M)
    assert match,field
    return np.array([float(match[1]),float(match[2])])
def ref(block,field):
    match=re.search(r'^  '+re.escape(field)+r': \{fileID: (\d+)\}',block,re.M)
    assert match,field
    return int(match[1])
def transform(file_id):
    block=objects[file_id]
    assert block.startswith('Transform:')
    return vector(block,'m_LocalPosition')
aim_id=4144584592635094242
weapon_id=7637028848145071171
body_id=ref(objects[aim_id],'m_Father')
aim=transform(aim_id)
body=transform(body_id)
weapon=transform(weapon_id)
assert ref(objects[weapon_id],'m_Father')==aim_id
rig=next(b for b in objects.values() if 'Assembly-CSharp::RightWalkPrototype.RightFacingAimRig\n' in b)
waist=vector(rig,'upperBodyLeanPivot')
ground_offset=vector(rig,'groundedDownGunOffset')
def scalar(name):
    return float(re.search(r'^  '+name+r': ([^\n]+)',rig,re.M)[1])
grips={name:weapon+transform(identifier) for name,identifier in [('muzzle',ref(rig,'muzzle')),('trigger',612175190512775546),('support',5759644583038611293)]}
for identifier in (ref(rig,'muzzle'),612175190512775546,5759644583038611293):
    assert ref(objects[identifier],'m_Father')==weapon_id
arms=[]
for block in objects.values():
    if 'Assembly-CSharp::RightWalkPrototype.RightFacingArmIK\n' not in block:continue
    t=next(b for b in objects.values() if b.startswith('Transform:') and ref(b,'m_GameObject')==ref(block,'m_GameObject'))
    offset=vector(t,'m_LocalPosition')
    arms.append({'grip':'trigger' if ref(block,'grip')==612175190512775546 else 'support',
                 'offset':offset,'shoulder':vector(block,'shoulder'),'elbow':vector(block,'referenceElbow'),'wrist':vector(block,'referenceWrist')})
def rot(degrees):
    a=math.radians(degrees);c,s=math.cos(a),math.sin(a)
    return np.array([[c,-s],[s,c]])
def smooth(a,b,x):
    t=min(1,max(0,(x-a)/(b-a)))
    return t*t*(3-2*t)
def air_offset(degrees):
    d=-degrees
    if d<=60:return np.array((.10,0))*smooth(45,60,d)
    if d<=75:return np.array((.10,0))+np.array((.125,.06))*smooth(60,75,d)
    return np.array((.225,.06))+np.array((.225,.04))*smooth(75,90,d)
def pose(degrees,mode):
    if mode=='air-existing': lean=8*smooth(55,90,degrees);offset=air_offset(degrees);barrel=degrees
    else:
        blend=smooth(35,scalar('groundedDownAngle'),-degrees)
        lean=8*smooth(55,90,degrees)-scalar('groundedDownLean')*blend
        offset=ground_offset*blend
        barrel=max(degrees,-scalar('groundedDownAngle')) if mode=='ground-existing' else degrees
    body_origin=body+waist-rot(lean)@waist
    pivot=body_origin+rot(lean)@(aim+offset)
    points={name:pivot+rot(barrel)@v for name,v in grips.items()}
    row={'mode':mode,'requested_deg':degrees,'barrel_deg':barrel,'pivot_x':float(pivot[0]),'pivot_y':float(pivot[1])}
    for name,p in points.items():row.update({name+'_x':float(p[0]),name+'_y':float(p[1])})
    for arm in arms:
        target=rot(-lean)@(points[arm['grip']]-body_origin)-arm['offset']
        h,e,w=arm['shoulder'],arm['elbow'],arm['wrist']
        l1,l2=np.linalg.norm(e-h),np.linalg.norm(w-e)
        delta=target-h;distance=np.linalg.norm(delta)
        reach=np.clip(distance,abs(l1-l2)+1e-5,l1+l2-1e-5)
        direction=delta/distance;along=(l1*l1-l2*l2+reach*reach)/(2*reach)
        a,b=w-h,e-h
        bend=np.sign(a[0]*b[1]-a[1]*b[0])
        joint=h+direction*along+np.array((-direction[1],direction[0]))*math.sqrt(max(0,l1*l1-along*along))*bend
        solved=h+direction*reach
        fore=math.degrees(math.atan2(*(solved-joint)[::-1]))+lean
        rest=math.degrees(math.atan2(*(w-e)[::-1]))
        wrist_delta=(barrel-fore+rest+180)%360-180
        row[arm['grip']+'_reach_gap']=float(abs(distance-reach))
        row[arm['grip']+'_wrist_rotation_delta']=wrist_delta
    return row
rows=[pose(d,mode) for mode in ['ground-existing','air-existing','fixed-ground-extension'] for d in range(90,-91,-1)]
with (OUT/'angle-samples.csv').open('w',newline='') as f:
    writer=csv.DictWriter(f,fieldnames=rows[0].keys());writer.writeheader();writer.writerows(rows)
snapshot={str(d):{mode:pose(d,mode) for mode in ['ground-existing','air-existing','fixed-ground-extension']} for d in [0,-55,-70,-90]}
(OUT/'audit.json').write_text(json.dumps({'space':'character-local, facing right, neutral hip; direction-mode, no recoil','live_capture':False,'snapshot':snapshot},indent=2))
board=Image.new('RGB',(1100,650),(245,247,249));draw=ImageDraw.Draw(board)
def pixel(x,y):return (80+int((x+1.1)*360),570-int((y-.35)*260))
colors={'ground-existing':(50,65,80),'air-existing':(210,105,35),'fixed-ground-extension':(20,145,115)}
for mode,color in colors.items():
    series=[row for row in rows if row['mode']==mode and row['requested_deg']<=0]
    draw.line([pixel(r['muzzle_x'],r['muzzle_y']) for r in series],fill=color,width=3)
    for degrees in [0,-55,-70,-90]:
        r=pose(degrees,mode);p=pixel(r['muzzle_x'],r['muzzle_y'])
        draw.ellipse((p[0]-4,p[1]-4,p[0]+4,p[1]+4),fill=color)
        if mode!='ground-existing':draw.text((p[0]+6,p[1]-8),str(degrees),fill=color)
for i,(mode,color) in enumerate(colors.items()):
    draw.line((40,65+i*30,70,65+i*30),fill=color,width=3)
    draw.text((80,58+i*30),mode,fill=color)
draw.text((30,20),'Muzzle path audit / serialized geometry, NOT live gameplay',fill=(25,35,40))
draw.text((40,180),'Ground currently clamps at -55.',fill=(25,35,40))
draw.text((40,205),'Air moves the pivot below -45.',fill=(25,35,40))
draw.text((40,230),'Green: fixed -55 ground pivot extension.',fill=(25,35,40))
draw.text((40,275),'Wrist delta is a rotation diagnostic,',fill=(25,35,40))
draw.text((40,300),'not an anatomical safety test.',fill=(25,35,40))
board.save(OUT/'muzzle-path-comparison.png')
# A fixed pivot extension has constant radius and exactly matches -55 ground pose.
anchor=pose(-55,'fixed-ground-extension')
for d in range(-55,-91,-1):
    p=pose(d,'fixed-ground-extension')
    assert np.allclose([p['pivot_x'],p['pivot_y']],[anchor['pivot_x'],anchor['pivot_y']])
    assert abs(np.linalg.norm(np.array([p['muzzle_x']-p['pivot_x'],p['muzzle_y']-p['pivot_y']]))-np.linalg.norm(grips['muzzle']))<1e-8
for key in ['pivot_x','pivot_y','muzzle_x','muzzle_y','trigger_x','trigger_y']:
    assert abs(anchor[key]-pose(-55,'ground-existing')[key])<1e-8
print(json.dumps(snapshot,indent=2))
print('PASS: fixed-radius extension and exact -55 boundary match. No runtime files changed.')
