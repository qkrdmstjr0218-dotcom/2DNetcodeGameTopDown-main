import json
import argparse
import math
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter

root = Path(__file__).resolve().parents[2]
analysis = root / 'LocalTools/Analysis'
parser = argparse.ArgumentParser()
parser.add_argument('--walk',action='store_true')
parser.add_argument('--fresh',action='store_true')
args = parser.parse_args()
scope = {'__file__': str(analysis / 'bake_stride_trial.py')}
exec((analysis / 'bake_stride_trial.py').read_text().split('ns = read_parts()')[0], scope)
ns = scope['read_parts']()
old = json.loads((analysis / 'baked-walk-v32.json').read_text())[0]['far']
old_angle = ns['angle'](old['knee'],old['ankle'])
base = np.array(old['knee'])
poses = json.loads((analysis / ('walk-stride-trial-joints.json' if args.walk else 'run-recovery-trial-joints.json')).read_text())
audit = {'__file__': str(analysis / 'audit_run_ankles.py')}
exec((analysis / 'audit_run_ankles.py').read_text().split('poses = json.loads')[0], audit)
boot_env = audit['env']
path = root / 'Assets/RightWalkPrototype/Art' / ('WalkStrideTrial.png' if args.walk else 'RunRecoveryTrial.png')
backup = analysis / ('walk-before-contour-repair.png' if args.walk else 'run-before-contour-repair.png')
sheet = Image.open(backup if backup.exists() and not args.fresh else path).convert('RGBA')
before = np.array(sheet)
sheet.save(backup)
yy, xx = np.indices((512,512))
for i, pose in enumerate(poses):
    x, y = i%5*512,i//5*512
    tile = np.array(sheet.crop((x,y,x+512,y+512)))
    far = pose['far']
    angle = ns['angle'](far['knee'],far['ankle'])-old_angle
    rotation = np.array(((math.cos(angle),-math.sin(angle)),(math.sin(angle),math.cos(angle))))
    start = np.array(far['knee'])+rotation@(np.array((255.,351.))-base)
    end = np.array(far['knee'])+rotation@(np.array((239.,377.))-base)
    vector = end-start
    length = np.linalg.norm(vector)
    normal = np.array((-vector[1],vector[0]))/length
    along = ((xx-start[0])*vector[0]+(yy-start[1])*vector[1])/(length*length)
    distance = (xx-start[0])*normal[0]+(yy-start[1])*normal[1]
    near = Image.new('RGBA',(512,512))
    p = pose['near']
    ns['draw_part'](near,ns['fixed']['near'],ns['nearHip'],p['hip'],ns['angle'](p['hip'],p['knee'])-ns['angle'](ns['nearHip'],ns['nearKnee']))
    ns['draw_part'](near,ns['calves']['near'],ns['nearKnee'],p['knee'],ns['angle'](p['knee'],p['ankle'])-ns['angle'](ns['nearKnee'],ns['nearAnkle']))
    for side in ('near','far'):
        p = pose[side]
        phase = i/40+(.5 if side=='far' else 0)
        if args.walk:
            phase %= 1
            rotation_angle = ns['ankle'](phase)[1]
            if phase < .6:
                target = (322-150*phase/.6,430-10*ns['ease']((phase-.5)/.1))
            else:
                u = (phase-.6)/.4
                target = (172+150*ns['ease'](u),420-22*math.sin(math.pi*u)+10*ns['ease'](u))
            position = (target[0]+(2 if side=='far' else 10)+p.get('bootShiftX',0),target[1])
        else:
            _, tilt = boot_env['foot'](phase)
            position, rotation_angle = boot_env['boot_transform'](side,p['ankle'],p['knee'],phase,tilt)
        ns['draw_part'](near,ns['new'],ns['nearAnkle'],position,rotation_angle)
    calf = Image.new('RGBA',(512,512))
    ns['draw_part'](calf,ns['calves']['far'],ns['farKnee'],far['knee'],
                    ns['angle'](far['knee'],far['ankle'])-ns['angle'](ns['farKnee'],ns['farAnkle']))
    alpha = calf.getchannel('A')
    edge = np.maximum(0,np.asarray(alpha).astype(float)-np.asarray(alpha.filter(ImageFilter.MinFilter(3))).astype(float))/255
    axis = np.array(far['ankle'])-far['knee']
    calf_length = np.linalg.norm(axis)
    axis /= calf_length
    projection = (xx-far['knee'][0])*axis[0]+(yy-far['knee'][1])*axis[1]
    visible = (1-np.asarray(near)[:,:,3]/255)*(tile[:,:,3]/255)
    # Keep the original knee ornament, including its lower black/gold rim.
    reference_y = base[1] - math.sin(angle)*(xx-far['knee'][0]) + math.cos(angle)*(yy-far['knee'][1])
    visible *= reference_y > 354
    band = (along>=0)*(along<=1)*(abs(distance)<5.5)
    # Replace the old segmented stroke with the neighbouring armor color.
    lx = np.clip(np.rint(xx+normal[0]*8).astype(int),0,511)
    ly = np.clip(np.rint(yy+normal[1]*8).astype(int),0,511)
    rx = np.clip(np.rint(xx-normal[0]*8).astype(int),0,511)
    ry = np.clip(np.rint(yy-normal[1]*8).astype(int),0,511)
    left, right = tile[ly,lx,:3].astype(float),tile[ry,rx,:3].astype(float)
    armor = (left.min(axis=2)>110)*(right.min(axis=2)>110)
    # Use the intact armor side where an old stroke contaminates the other sample.
    left_white, right_white = left.min(axis=2)>150, right.min(axis=2)>150
    surface = np.where(left_white[:,:,None] & ~right_white[:,:,None],left,
                       np.where(right_white[:,:,None] & ~left_white[:,:,None],right,(left+right)/2))
    armor = armor | left_white | right_white
    erase = band*armor*visible
    tile[:,:,:3] = np.rint(tile[:,:,:3]*(1-erase[:,:,None])+surface*erase[:,:,None]).astype('uint8')
    coverage = np.clip(1.5-abs(distance),0,1)*(along>=0)*(along<=1)*visible
    tile[:,:,:3] = np.rint(tile[:,:,:3]*(1-coverage[:,:,None])
                           + np.array((23,27,28))*coverage[:,:,None]).astype('uint8')
    sheet.paste(Image.fromarray(tile),(x,y))
after = np.array(sheet)
assert np.array_equal(before[:,:,3],after[:,:,3])
sheet.save(path)
print('Contour-only changed pixels:',np.count_nonzero(np.any(before!=after,axis=2)))
print('Alpha, dimensions and frame placement unchanged')
