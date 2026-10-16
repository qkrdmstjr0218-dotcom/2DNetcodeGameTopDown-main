"""Mark physical weapon contacts separately from runtime wrist transforms."""
import json
from pathlib import Path
from PIL import Image,ImageDraw

ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent
original=Image.open(ROOT/'Assets/RightWalkPrototype/Art/RightWalk-WeaponHands-v44.png').convert('RGBA')
front=Image.open('C:/Users/PC/.codex/generated_images/01a12d87-d09d-7f20-a556-97e2f4b8fa44/exec-1bb67ba9-ab53-4ad9-8729-2fd031c1e8fa.png').convert('RGBA')
board=Image.new('RGBA',(1150,1050),(45,62,74,255));draw=ImageDraw.Draw(board)
cropbox=(260,110,475,195)
side=original.crop(cropbox).resize((860,340),Image.Resampling.NEAREST)
board.alpha_composite(side,(25,65))
colors=[(255,170,60),(80,225,195)]
contacts=[('R / pistol grip + trigger',(334,160)),('L / fore-end UNDERSIDE',(410,157))]
for i,(label,p) in enumerate(contacts):
    x=25+(p[0]-260)*4;y=65+(p[1]-110)*4
    draw.ellipse((x-12,y-12,x+12,y+12),outline=colors[i],width=3)
    draw.line((x,y+12,x,420+i*30),fill=colors[i],width=2)
    draw.text((35,420+i*30),label,fill=colors[i])
draw.text((25,20),'Actual source weapon / physical contact anchors',fill='white')
draw.text((25,490),'R: palm around pistol grip; index finger at trigger.',fill='white')
draw.text((25,515),'L: palm under forward handguard, nearer muzzle.',fill='white')
draw.text((25,555),'Physical contact != sprite wrist pivot.',fill='white')
draw.text((25,580),'No costume/weapon repaint. No Unity installation.',fill='white')
# Front anchors transfer the original stock-to-muzzle proportions, not the old bad hands.
front_scale=900/front.height
small=front.resize((round(front.width*front_scale),900),Image.Resampling.LANCZOS)
fx,fy=800,75
board.alpha_composite(small,(fx,fy))
axis_x=front.width*.487
butt_y=100;muzzle_y=1880
for i,(label,fraction) in enumerate([('R',74/204),('L',150/204)]):
    y=butt_y+(muzzle_y-butt_y)*fraction
    x=axis_x+(-28 if i==0 else 16)
    px=fx+x*front_scale;py=fy+y*front_scale
    draw.ellipse((px-10,py-10,px+10,py+10),outline=colors[i],width=3)
    draw.text((px+14,py-6),label,fill=colors[i])
draw.text((805,20),'Front / PROVISIONAL',fill='white')
draw.text((795,1000),'L contact is behind visible rail.',fill=colors[1])
board.save(OUT/'weapon-two-grip-anchors.png')
(OUT/'weapon-two-grip-anchors.json').write_text(json.dumps({
 'original_texture':'RightWalk-WeaponHands-v44.png','physical_contacts_estimated_px':{
 'right_pistol_grip':[334,160],'left_foreend_underside':[410,157]},
 'runtime_wrist_pivots_are_different':True,'front_transfer':'provisional stock-to-muzzle proportions; not exact projection',
 'needs_confirmation':'trigger/handle hidden faces must be reconstructed before anatomy validation'},indent=2))
print('Saved original contact anchors and provisional front placement.')
