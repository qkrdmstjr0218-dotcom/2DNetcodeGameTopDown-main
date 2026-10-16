"""Rigid cutout/IK feasibility study; never installs Unity assets."""
import json
import math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parent / 'FrontAimPreview'
OUT.mkdir(exist_ok=True)
src = Image.open(OUT.parent / 'front-aim-source-crop.png').convert('RGBA')
clean = Image.open('C:/Users/PC/.codex/generated_images/01a12d87-d09d-7f20-a556-97e2f4b8fa44/exec-3303aec3-fb3f-466a-b3a7-da1f42504720.png').convert('RGBA').resize(src.size, Image.Resampling.LANCZOS)
polygons = {
    'left-upper': [(124,232),(175,237),(183,273),(163,310),(125,310),(110,277)],
    'left-fore': [(98,283),(133,286),(168,310),(194,321),(198,369),(167,379),(124,365),(90,348),(81,326)],
    'right-upper': [(299,252),(347,257),(355,302),(348,341),(306,352),(287,316)],
    'right-fore': [(303,330),(347,340),(354,372),(338,412),(305,448),(281,450),(261,423),(282,387)],
    'weapon-hands': [(191,268),(222,273),(233,322),(247,362),(263,409),(288,445),(297,460),(280,490),(286,539),(280,586),(251,592),(232,573),(227,495),(213,440),(204,403),(180,397),(174,371),(167,345),(190,326)],
}
parts = {}
union = Image.new('L', src.size)
for name, polygon in polygons.items():
    mask = Image.new('L', src.size)
    ImageDraw.Draw(mask).polygon(polygon, fill=255)
    ImageDraw.Draw(union).polygon(polygon, fill=255)
    part = src.copy()
    part.putalpha(mask)
    part.save(OUT / (name + '.png'))
    parts[name] = part
body = Image.composite(clean, src, union)
body.save(OUT / 'body-underlay.png')
pivot = np.array((214.,361.))
arms = [
    ('left', np.array((150.,259.)), np.array((127.,309.)), np.array((196.,361.)), 1),
    ('right', np.array((326.,279.)), np.array((324.,347.)), np.array((283.,435.)), -1),
]

def rotation(angle):
    c,s=math.cos(angle),math.sin(angle)
    return np.array([[c,-s],[s,c]])

def place(canvas, part, origin, target, angle):
    r=rotation(-angle)
    offset=origin-r@target
    coefficients=(r[0,0],r[0,1],offset[0],r[1,0],r[1,1],offset[1])
    canvas.alpha_composite(part.transform(canvas.size, Image.Transform.AFFINE, coefficients, Image.Resampling.BICUBIC))

def direction(a,b):
    v=b-a
    return math.atan2(v[1],v[0])

max_gap=0
def frame(degrees):
    global max_gap
    angle=math.radians(degrees)
    result=body.copy()
    r=rotation(angle)
    for side,shoulder,elbow,wrist,sign in arms:
        target=pivot+r@(wrist-pivot)
        l1,l2=np.linalg.norm(elbow-shoulder),np.linalg.norm(wrist-elbow)
        distance=np.linalg.norm(target-shoulder)
        reach=min(l1+l2-.001,max(abs(l1-l2)+.001,distance))
        max_gap=max(max_gap,float(abs(distance-reach)))
        phi=direction(shoulder,target)
        bend=math.acos(np.clip((l1*l1+reach*reach-l2*l2)/(2*l1*reach),-1,1))
        joint=shoulder+l1*np.array((math.cos(phi+sign*bend),math.sin(phi+sign*bend)))
        solved=shoulder+reach*(target-shoulder)/distance
        place(result,parts[side+'-upper'],shoulder,shoulder,direction(shoulder,joint)-direction(shoulder,elbow))
        place(result,parts[side+'-fore'],elbow,joint,direction(joint,solved)-direction(elbow,wrist))
    place(result,parts['weapon-hands'],pivot,pivot,angle)
    return result

# Offset rotation by original projected barrel angle, not by the artwork rectangle.
barrel_tilt=math.degrees(math.atan2(265-pivot[0],575-pivot[1]))
keys=[frame(35+barrel_tilt),frame(barrel_tilt),frame(-35+barrel_tilt)]
board=Image.new('RGBA',(1380,930),(45,62,74,255))
for i,(key,label) in enumerate(zip(keys,['Down-left 35','Straight down','Down-right 35'])):
    board.alpha_composite(key,(i*460,40))
    ImageDraw.Draw(board).text((i*460+15,12),label,fill='white')
board.save(OUT / 'three-directions.png')
frames=[]
for i in range(48):
    degrees=35*math.sin(2*math.pi*i/48)+barrel_tilt
    frames.append(frame(degrees).convert('RGB').resize((345,660),Image.Resampling.LANCZOS))
frames[0].save(OUT / 'front-aim-motion.gif',save_all=True,append_images=frames[1:],duration=65,loop=0)
(OUT / 'study.json').write_text(json.dumps({'preview_only':True,'pivot':pivot.tolist(),'max_wrist_reach_gap_px':max_gap,'frame_count':len(frames),'notes':'Polygon cutouts and generated occluded torso. Not production art; no Unity install.'},indent=2))
print(f'Preview saved. Max wrist reach gap: {max_gap:.1f} px. No Unity assets changed.')
