"""Bake a separate long-stride trial from existing rigid limb parts."""
import ast
import json
import math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'Assets/RightWalkPrototype/Art'
ANALYSIS = ROOT / 'LocalTools/Analysis'


class NoLegacyWrites(ast.NodeTransformer):
    def visit_Expr(self, node):
        if isinstance(node.value, ast.Call) and isinstance(node.value.func, ast.Attribute):
            if node.value.func.attr in ('save', 'write_text', 'write_bytes'):
                return ast.Pass()
        return self.generic_visit(node)


def read_parts():
    source = (ANALYSIS / 'repair_rear_contour_v69.py').read_text()
    source = source.split("src=np.array(Image.open(p/'RightWalkSheet-Legs-LeftBootShift-v56.png'))")[0]
    tree = ast.fix_missing_locations(NoLegacyWrites().visit(ast.parse(source)))
    ns = {'__file__': str(ANALYSIS / 'repair_rear_contour_v69.py')}
    exec(compile(tree, '<legacy-part-loader>', 'exec'), ns)
    return {**ns['ns'], **ns}


ns = read_parts()
accepted = Image.open(ART / 'RightWalkSheet-Legs-FinalContour-v72.png').convert('RGBA')
sheet = Image.new('RGBA', (2560, 4096))
poses = []
max_length_error = 0
repaired_pinholes = 0


def close_ankle_pinholes(tile, ankles):
    pixels = np.array(tile)
    empty = pixels[:, :, 3] < 230
    exterior = Image.fromarray((empty*255).astype('uint8'))
    ImageDraw.floodfill(exterior, (0,0), 127)
    holes = np.asarray(exterior) == 255
    count = 0
    for ax, ay in ankles:
        yy, xx = np.indices(pixels.shape[:2])
        rgb = pixels[:, :, :3].astype(int)
        fringe = ((abs(xx-ax)<40) & (yy>ay-22) & (yy<ay+4)
                  & (rgb[:,:,0]<190) & (rgb[:,:,1]>rgb[:,:,0]+25)
                  & (rgb[:,:,2]>rgb[:,:,0]+40) & (pixels[:,:,3]>100))
        # Feather the local fringe repair instead of cutting a hard color boundary.
        feather = Image.fromarray((fringe*255).astype('uint8'))
        feather = feather.filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.GaussianBlur(.65))
        weight = np.asarray(feather).astype(float)/255
        weight *= ((abs(xx-ax)<42) & (yy>ay-24) & (yy<ay+6))
        pixels[:,:,:3] = np.rint(pixels[:,:,:3]*(1-weight[:,:,None])
                                + np.array((24,27,30))*weight[:,:,None]).astype('uint8')
        count += int(np.count_nonzero(fringe))
        for y in range(max(1,int(ay)-40), min(511,int(ay)+20)):
            for x in range(max(1,int(ax)-45), min(511,int(ax)+45)):
                if not holes[y,x]:
                    continue
                pending, component = [(x,y)], []
                holes[y,x] = False
                while pending:
                    px, py = pending.pop()
                    component.append((px,py))
                    for nx, ny in ((px-1,py),(px+1,py),(px,py-1),(px,py+1)):
                        if 0 <= nx < 512 and 0 <= ny < 512 and holes[ny,nx]:
                            holes[ny,nx] = False
                            pending.append((nx,ny))
                if len(component) > 20:
                    continue
                border = []
                for px, py in component:
                    for nx, ny in ((px-1,py),(px+1,py),(px,py-1),(px,py+1)):
                        if pixels[ny,nx,3] >= 230:
                            border.append(pixels[ny,nx,:3])
                if border:
                    color = np.median(border,axis=0).astype('uint8')
                    for px, py in component:
                        pixels[py,px] = (*color,255)
                    count += len(component)
    return Image.fromarray(pixels), count


def target(phase):
    t = phase % 1
    if t < .6:
        u = t / .6
        lift = 10 * ns['ease']((t - .5) / .1)
        return (172 + 150 * (1 - u), 430 - lift), ns['ankle'](t)[1]
    u = (t - .6) / .4
    blend = ns['ease'](u)
    return (172 + 150 * blend, 430 - 10 * (1 - blend) - 22 * math.sin(math.pi * u)), ns['ankle'](t)[1]


def fit_cuff(side, hip_target, foot_target, boot_angle, l1, l2):
    source_knee, source_ankle = ns[side+'Knee'], ns[side+'Ankle']
    ys, xs = np.nonzero(np.asarray(ns['calves'][side])[:, :, 3] > 220)
    axis = (np.array(source_ankle)-source_knee) / l2
    projection = (xs-source_knee[0])*axis[0] + (ys-source_knee[1])*axis[1]
    distal = projection > l2-28
    points = np.stack((xs[distal]-source_knee[0], ys[distal]-source_knee[1]))
    boot = Image.new('RGBA', (512,512))
    ns['draw_part'](boot, ns['new'], ns['nearAnkle'],
                    (foot_target[0]+(2 if side=='far' else 10),foot_target[1]), boot_angle)
    coverage = np.asarray(boot)[:, :, 3]
    best = None
    for dx in range(-16, 17, 2):
        for dy in range(-12, 5, 2):
            a = (foot_target[0]+dx, foot_target[1]+dy)
            d = ns['dist'](hip_target,a)
            if d >= l1+l2:
                continue
            phi = ns['angle'](hip_target,a)
            bend = math.acos(max(-1,min(1,(l1*l1+d*d-l2*l2)/(2*l1*d))))
            k = (hip_target[0]+l1*math.cos(phi-bend),hip_target[1]+l1*math.sin(phi-bend))
            angle = ns['angle'](k,a)-ns['angle'](source_knee,source_ankle)
            transform = np.array(((math.cos(angle),-math.sin(angle)),(math.sin(angle),math.cos(angle))))
            transformed = transform @ points + np.array(k)[:,None]
            x, y = np.rint(transformed).astype(int)
            for boot_shift in range(-12,13,2):
                exposed = int(np.count_nonzero(coverage[np.clip(y,0,511),np.clip(x-boot_shift,0,511)] < 200))
                score = exposed + .025*(dx*dx+dy*dy+boot_shift*boot_shift)
                if best is None or score < best[0]:
                    best = (score,a,exposed,boot_shift)
    return best[1], best[2], best[3]


for i in range(40):
    phase = i / 40
    hip_y = 254 + 6 * math.cos(4 * math.pi * phase)
    tile = Image.new('RGBA', (512, 512))
    record = {'frame': i, 'hipY': hip_y}
    ankle_centers = []
    for side in ('far', 'near'):
        hip, knee, ankle = (ns[side + name] for name in ('Hip', 'Knee', 'Ankle'))
        h = (hip[0] - 12, hip_y)
        a, boot_angle = target(phase + (.5 if side == 'far' else 0))
        boot_target = a
        ankle_centers.append(boot_target)
        l1, l2 = ns['dist'](hip, knee), ns['dist'](knee, ankle)
        a, exposed, boot_shift = fit_cuff(side,h,boot_target,boot_angle,l1,l2)
        d = ns['dist'](h, a)
        if d >= l1 + l2:
            raise ValueError(f'Unreachable foot: frame {i}, {side}, {d} >= {l1+l2}')
        phi = ns['angle'](h, a)
        bend = math.acos(max(-1, min(1, (l1*l1 + d*d - l2*l2)/(2*l1*d))))
        k = (h[0] + l1 * math.cos(phi-bend), h[1] + l1 * math.sin(phi-bend))
        max_length_error = max(max_length_error, abs(ns['dist'](h, k)-l1), abs(ns['dist'](k, a)-l2))
        ns['draw_part'](tile, ns['fixed'][side], hip, h, ns['angle'](h,k)-ns['angle'](hip,knee))
        ns['draw_part'](tile, ns['calves'][side], knee, k, ns['angle'](k,a)-ns['angle'](knee,ankle))
        ns['draw_part'](tile, ns['new'], ns['nearAnkle'], (boot_target[0]+(2 if side=='far' else 10)+boot_shift,boot_target[1]), boot_angle)
        ns['draw_part'](tile, ns['nearKneePatch'], ns['nearKnee'], k, ns['angle'](k,a)-ns['angle'](ns['nearKnee'],ns['nearAnkle']))
        record[side] = {'hip': h, 'knee': k, 'ankle': a, 'cuffExposedPixels': exposed, 'bootShiftX': boot_shift}
    # Keep the accepted waist texture, moving it with the trial hip height.
    original_index = i // 2
    waist = accepted.crop((original_index%5*512, original_index//5*512,
                           original_index%5*512+512, original_index//5*512+250))
    original_hip = 251 + math.cos(4*math.pi*original_index/20)
    tile.alpha_composite(waist, (0, round(hip_y-original_hip)))
    tile, pinholes = close_ankle_pinholes(tile, ankle_centers)
    repaired_pinholes += pinholes
    sheet.alpha_composite(tile, (i%5*512, i//5*512))
    poses.append(record)

sheet.save(ART / 'WalkStrideTrial.png')
(ANALYSIS / 'walk-stride-trial-joints.json').write_text(json.dumps(poses, indent=2))
print(f'40 poses, unreachable feet=0, max rigid bone length error={max_length_error:.3g}px')
print(f'Closed ankle pinhole pixels: {repaired_pinholes}')
print('Cycle distance=150/204.8/.6=1.220703125; 1.6 speed cadence=2.62144 steps/sec')
preview = Image.new('RGBA', (1600, 600), 'white')
for j, i in enumerate((0, 10, 20, 30)):
    preview.alpha_composite(sheet.crop((i%5*512, i//5*512, i%5*512+512, i//5*512+512)).crop((150,230,400,490)).resize((400,416)), (j*400, 0))
preview.save(ANALYSIS / 'walk-stride-trial-keyposes.png')
import subprocess
import sys
subprocess.run([sys.executable, str(ANALYSIS / 'repair_run_contour.py'), '--walk', '--fresh'], check=True)
