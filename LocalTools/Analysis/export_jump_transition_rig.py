"""Export original rigid pieces and gait joint data without touching gait sheets."""
import json
import math
import ast
import argparse
import numpy as np
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[2]
analysis = root / 'LocalTools/Analysis'
parser=argparse.ArgumentParser()
parser.add_argument('--install',action='store_true',help='Explicitly install approved outputs into Unity assets.')
args=parser.parse_args()
dest = root / 'Assets/RightWalkPrototype/Resources/JumpTransition' if args.install else analysis/'JumpTransitionPreview'
dest.mkdir(parents=True, exist_ok=True)
loader = (analysis / 'bake_stride_trial.py').read_text().split('\nns = read_parts()')[0]
scope = {'__file__': str(analysis / 'bake_stride_trial.py')}
exec(compile(loader, '<part-loader>', 'exec'), scope)
ns = scope['read_parts']()
air = Image.open(analysis / 'jump-ascent-cutout-v29.png').convert('RGBA')
accepted_joints = json.loads((analysis/'jump-ascent-cutout-v5.json').read_text())

def transfer_approved_region(part, source_pivot, target_pivot, rotation, polygon):
    """Bring final approved pixels back into the original part's registration."""
    region = Image.new('RGBA',(512,512))
    ImageDraw.Draw(region).polygon(polygon,fill=(255,255,255,255))
    restored = Image.new('RGBA',(512,512))
    coverage = Image.new('RGBA',(512,512))
    ns['draw_part'](restored,air,target_pivot,source_pivot,-rotation)
    ns['draw_part'](coverage,region,target_pivot,source_pivot,-rotation)
    old=np.array(part)
    new=np.array(restored)
    selection=(np.asarray(coverage)[:,:,3]>200)&(new[:,:,3]>100)&(old[:,:,3]>100)
    old[selection,:3]=new[selection,:3]
    return Image.fromarray(old)

# The manually repaired continuous white plate belongs to the far shin cutout.
far = accepted_joints['far']
far_rotation=ns['angle'](far['knee'],far['ankle'])-ns['angle'](ns['farKnee'],ns['farAnkle'])
ns['calves']['far']=transfer_approved_region(ns['calves']['far'],ns['farKnee'],far['knee'],far_rotation,
    [(296,294),(309,294),(322,321),(318,324),(297,322)])
# Preserve the separately approved near knee tip and outline as well.
near = accepted_joints['near']
knee_rotation=ns['angle'](near['knee'],near['ankle'])-ns['angle'](ns['nearKnee'],ns['nearAnkle'])
ns['calves']['near']=transfer_approved_region(ns['calves']['near'],ns['nearKnee'],near['knee'],knee_rotation,
    [(267,337),(290,340),(254,393),(222,393),(230,369)])
ns['nearKneePatch']=transfer_approved_region(ns['nearKneePatch'],ns['nearKnee'],near['knee'],knee_rotation,
    [(307,323),(320,318),(328,323),(328,341),(313,347),(306,338)])
alternate_angles = {'far': [40,130,16], 'near': [85,108,23]}
# Transfer the approved preview back into rigid-part registration, without redrawing.
ta=math.radians(alternate_angles['far'][0])
fh=(ns['farHip'][0]-12,254)
fk=(fh[0]+ns['dist'](ns['farHip'],ns['farKnee'])*math.cos(ta),
    fh[1]+ns['dist'](ns['farHip'],ns['farKnee'])*math.sin(ta))
rotation=ta-ns['angle'](ns['farHip'],ns['farKnee'])
baseline=Image.open(analysis/'jump-right-foot-angle-minus-15-preview.png').convert('RGBA')
approved=baseline.copy()
final_pixels=np.array(approved)
changed=np.any(final_pixels!=np.asarray(baseline),axis=2)
yy,xx=np.indices(changed.shape)
def apply_preview_delta(part,src_pivot,dst_pivot,angle,zone):
    patch=final_pixels.copy()
    patch[~(changed&zone)]=0
    registered=Image.new('RGBA',(512,512))
    ns['draw_part'](registered,Image.fromarray(patch),dst_pivot,src_pivot,-angle)
    result=part.copy()
    result.alpha_composite(registered)
    return result
far_knee_rotation=math.radians(alternate_angles['far'][1])-ns['angle'](ns['nearKnee'],ns['nearAnkle'])
far_knee=ns['nearKneePatch'].copy()
ns['fixed']['far']=apply_preview_delta(ns['fixed']['far'],ns['farHip'],fh,rotation,
    (xx>270)&(yy<fk[1]-20))
nh=(ns['nearHip'][0]-12,254)
near_thigh_rotation=math.radians(alternate_angles['near'][0])-ns['angle'](ns['nearHip'],ns['nearKnee'])
ns['fixed']['near']=apply_preview_delta(ns['fixed']['near'],ns['nearHip'],nh,near_thigh_rotation,
    (xx<260)&(yy<315))
for side in ('far', 'near'):
    ns['fixed'][side].save(dest / (side+'Thigh.png'))
    ns['calves'][side].save(dest / (side+'Shin.png'))
ns['new'].save(dest / 'Boot.png')
ns['nearKneePatch'].save(dest / 'Knee.png')
far_knee.save(dest / 'FarKnee.png')
waist = Image.new('RGBA', (512, 512))
waist.alpha_composite(air.crop((0, 0, 512, 244)))
waist.save(dest / 'Waist.png')
alternate = Image.new('RGBA', (512,512))
for side in ('far','near'):
    thigh_angle, shin_angle, boot_angle = alternate_angles[side]
    hip, knee, ankle = [ns[side+name] for name in ('Hip','Knee','Ankle')]
    h = (hip[0]-12,254)
    ta, sa = math.radians(thigh_angle), math.radians(shin_angle)
    k = (h[0]+ns['dist'](hip,knee)*math.cos(ta),h[1]+ns['dist'](hip,knee)*math.sin(ta))
    a = (k[0]+ns['dist'](knee,ankle)*math.cos(sa),k[1]+ns['dist'](knee,ankle)*math.sin(sa))
    ns['draw_part'](alternate,ns['fixed'][side],hip,h,ta-ns['angle'](hip,knee))
    ns['draw_part'](alternate,ns['calves'][side],knee,k,sa-ns['angle'](knee,ankle))
    ns['draw_part'](alternate,ns['new'],ns['nearAnkle'],(a[0]+2,a[1]),math.radians(boot_angle))
    ns['draw_part'](alternate,far_knee if side=='far' else ns['nearKneePatch'],ns['nearKnee'],k,sa-ns['angle'](ns['nearKnee'],ns['nearAnkle']))
alternate.alpha_composite(waist)
# Keep the settled pose pixel-identical to the preview approved by the user.
approved.save(dest / 'AlternateAir.png')
def frames(name):
    return json.loads((analysis / name).read_text())
data = {'walk': frames('walk-stride-trial-joints.json'),
        'run': frames('run-recovery-trial-joints.json'),
        'alternateAngles': alternate_angles,
        'source': {side: {joint.lower(): ns[side+joint] for joint in ('Hip','Knee','Ankle')}
                   for side in ('far','near')}}
socket=np.array((350,417))-ns['nearAnkle']
def rotate_socket(degrees):
    a=math.radians(degrees)
    return np.array((socket[0]*math.cos(a)-socket[1]*math.sin(a),
                     socket[0]*math.sin(a)+socket[1]*math.cos(a)))
near_boot_offset=np.array((2,0))+rotate_socket(8)-rotate_socket(23)
data['alternateBootOffsets']={'far':[2,0],'near':near_boot_offset.tolist()}
target_node = next(node for node in ast.parse((analysis/'bake_stride_trial.py').read_text()).body
                   if isinstance(node,ast.FunctionDef) and node.name=='target')
target_scope = {'ns':ns,'math':math}
exec(compile(ast.Module(body=[target_node],type_ignores=[]),'<walk-foot>', 'exec'),target_scope)
run_boots = frames('run-boot-clearance.json')
boot_y,boot_x=np.nonzero(np.asarray(ns['new'])[:,:,3]>220)
boot_relative_x=boot_x-ns['nearAnkle'][0]
boot_relative_y=boot_y-ns['nearAnkle'][1]
for gait, poses in [('walk',data['walk']),('run',data['run'])]:
    for index, pose in enumerate(poses):
        for side_index, side in enumerate(('far','near')):
            leg=pose[side]
            if gait=='walk':
                foot,rotation=target_scope['target'](index/40+(.5 if side=='far' else 0))
                position=(foot[0]+(2 if side=='far' else 10)+leg['bootShiftX'],foot[1])
            else:
                rotation=run_boots[index*2+side_index]['rotation']
                source_knee,source_ankle=np.array(ns[side+'Knee']),np.array(ns[side+'Ankle'])
                ys,xs=np.nonzero(np.asarray(ns['calves'][side])[:,:,3]>220)
                axis=(source_ankle-source_knee)/np.linalg.norm(source_ankle-source_knee)
                projection=(xs-source_knee[0])*axis[0]+(ys-source_knee[1])*axis[1]
                distal=projection>projection.max()-12
                center=np.array((xs[distal].mean(),ys[distal].mean()))
                def rotate(v,a):
                    return np.array((v[0]*math.cos(a)-v[1]*math.sin(a),v[0]*math.sin(a)+v[1]*math.cos(a)))
                angle=ns['angle'](leg['knee'],leg['ankle'])-ns['angle'](source_knee,source_ankle)
                socket=rotate(center-source_ankle,angle)
                boot_socket=rotate(np.array((350,417))-ns['nearAnkle'],rotation)
                position=np.array(leg['ankle'])+socket-boot_socket
            leg['boot']=[float(v) for v in position]
            leg['bootAngle']=math.degrees(rotation)
            leg['contactY']=float(position[1]+np.max(boot_relative_x*math.sin(rotation)
                                                   +boot_relative_y*math.cos(rotation)))
(dest / 'Joints.json').write_text(json.dumps(data))
print('Exported parts repaired from accepted v29 art, two poses and 80 gait joint frames.')
