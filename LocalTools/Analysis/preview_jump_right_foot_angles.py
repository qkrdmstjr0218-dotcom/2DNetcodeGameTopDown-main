"""Preview right boot rotation around its cuff socket; no Unity writes."""
import json
import math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

root=Path(__file__).resolve().parents[2]
out=root/'LocalTools/Analysis'
resources=root/'Assets/RightWalkPrototype/Resources/JumpTransition'
scope={'__file__':str(out/'bake_stride_trial.py')}
exec(compile((out/'bake_stride_trial.py').read_text().split('\nns = read_parts()')[0],
             '<part-loader>','exec'),scope)
ns=scope['read_parts']()
data=json.loads((resources/'Joints.json').read_text())
baseline=Image.open(resources/'AlternateAir.png').convert('RGBA')
boot=Image.open(resources/'Boot.png').convert('RGBA')
body=Image.new('RGBA',(512,512))
rest_boot=Image.new('RGBA',(512,512))
for side in ('far','near'):
    thigh_angle,shin_angle,boot_angle=data['alternateAngles'][side]
    hip,knee,ankle=[data['source'][side][name] for name in ('hip','knee','ankle')]
    h=(hip[0]-12,254)
    ta,sa=math.radians(thigh_angle),math.radians(shin_angle)
    k=(h[0]+ns['dist'](hip,knee)*math.cos(ta),h[1]+ns['dist'](hip,knee)*math.sin(ta))
    a=(k[0]+ns['dist'](knee,ankle)*math.cos(sa),k[1]+ns['dist'](knee,ankle)*math.sin(sa))
    ns['draw_part'](body,Image.open(resources/(side+'Thigh.png')).convert('RGBA'),hip,h,ta-ns['angle'](hip,knee))
    ns['draw_part'](body,Image.open(resources/(side+'Shin.png')).convert('RGBA'),knee,k,sa-ns['angle'](knee,ankle))
    position=(a[0]+2,a[1])
    if side=='far':
        ns['draw_part'](body,boot,ns['nearAnkle'],position,math.radians(boot_angle))
    else:
        rest_position=position
        rest_angle=math.radians(boot_angle)
        ns['draw_part'](rest_boot,boot,ns['nearAnkle'],position,rest_angle)
    ns['draw_part'](body,Image.open(resources/('FarKnee.png' if side=='far' else 'Knee.png')).convert('RGBA'),
                    ns['nearKnee'],k,sa-ns['angle'](ns['nearKnee'],ns['nearAnkle']))
body.alpha_composite(Image.open(resources/'Waist.png').convert('RGBA'))
base=np.array(baseline)
selection=np.asarray(rest_boot)[:,:,3]>8
base[selection]=np.asarray(body)[selection]
base=Image.fromarray(base)
def rotate(v,a):
    return (v[0]*math.cos(a)-v[1]*math.sin(a),v[0]*math.sin(a)+v[1]*math.cos(a))
socket=(350-ns['nearAnkle'][0],417-ns['nearAnkle'][1])
offset=rotate(socket,rest_angle)
contact=(rest_position[0]+offset[0],rest_position[1]+offset[1])
upper={'__file__':str(out/'preview_jump_full_body.py')}
exec(compile((out/'preview_jump_full_body.py').read_text().split('\nwalk = ')[0],
             '<upper-layers>','exec'),upper)
preview=Image.new('RGBA',(1536,560),(45,62,74,255))
draw=ImageDraw.Draw(preview)
for i,extra in enumerate((0,15,25)):
    image=baseline.copy() if extra==0 else base.copy()
    if extra:
        angle=rest_angle+math.radians(extra)
        offset=rotate(socket,angle)
        position=(contact[0]-offset[0],contact[1]-offset[1])
        ns['draw_part'](image,boot,ns['nearAnkle'],position,angle)
    image.save(out/f'jump-right-foot-angle-minus-{extra}-preview.png')
    preview.alpha_composite(upper['compose'](image),(i*512,35))
    draw.text((i*512+20,10),'Current' if extra==0 else f'Right foot: -{extra} degrees',fill='white')
preview.save(out/'jump-right-foot-angle-comparison.png')
print('Current, -15 and -25 degree right-foot previews saved; cuff socket fixed.')
