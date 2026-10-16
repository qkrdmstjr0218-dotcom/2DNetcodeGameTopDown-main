import json
import math
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
ANALYSIS = ROOT / 'LocalTools/Analysis'
ART = ROOT / 'Assets/RightWalkPrototype/Art'
loader = (ANALYSIS / 'bake_stride_trial.py').read_text().split('ns = read_parts()')[0]
scope = {'__file__': str(ANALYSIS / 'bake_stride_trial.py')}
exec(compile(loader, '<part-loader>', 'exec'), scope)
ns = scope['read_parts']()
accepted = Image.open(ART / 'RightWalkSheet-Legs-FinalContour-v72.png').convert('RGBA')
wide = Image.open(ART / 'WalkStrideTrial.png').convert('RGBA')
records = []
for variant in range(2):
    sheet = Image.new('RGBA', (2560, 2560))
    for i in range(25):
        progress = i / 24
        rear_progress = ns['ease'](min(1, progress * 2))
        front_progress = ns['ease'](max(0, progress * 2 - 1))
        hip_y = 260 - 6 * front_progress
        tile = Image.new('RGBA', (512, 512))
        record = {'variant': variant, 'frame': i, 'hipY': hip_y}
        for side in ('far', 'near'):
            front = (side == 'near') == (variant == 0)
            t = front_progress if front else rear_progress
            x = 322 + (277 - 322) * t if front else 197 + (227 - 197) * t
            stage = max(0, progress * 2 - 1) if front else min(1, progress * 2)
            a = (x, 430 - 12 * math.sin(math.pi * stage))
            hip, knee, ankle = (ns[side + name] for name in ('Hip', 'Knee', 'Ankle'))
            h = (hip[0] - 12, hip_y)
            l1, l2 = ns['dist'](hip, knee), ns['dist'](knee, ankle)
            d = ns['dist'](h, a)
            if d >= l1 + l2:
                raise ValueError(f'Unreachable {variant}/{i}/{side}')
            phi = ns['angle'](h, a)
            bend = math.acos(max(-1, min(1, (l1*l1+d*d-l2*l2)/(2*l1*d))))
            k = (h[0]+l1*math.cos(phi-bend), h[1]+l1*math.sin(phi-bend))
            ns['draw_part'](tile, ns['fixed'][side], hip, h, ns['angle'](h,k)-ns['angle'](hip,knee))
            ns['draw_part'](tile, ns['calves'][side], knee, k, ns['angle'](k,a)-ns['angle'](knee,ankle))
            ns['draw_part'](tile, ns['new'], ns['nearAnkle'], (a[0]+(2 if side=='far' else 10),a[1]), 0)
            ns['draw_part'](tile, ns['nearKneePatch'], ns['nearKnee'], k, ns['angle'](k,a)-ns['angle'](ns['nearKnee'],ns['nearAnkle']))
            record[side] = {'hip': h, 'knee': k, 'ankle': a}
        waist = accepted.crop((0,0,512,250))
        tile.alpha_composite(waist, (0, round(hip_y-252)))
        # The first transition sample is the existing planted pose, pixel for pixel.
        if i == 0:
            f = variant * 20
            tile = wide.crop((f%5*512,f//5*512,f%5*512+512,f//5*512+512))
        sheet.alpha_composite(tile, (i%5*512,i//5*512))
        records.append(record)
    sheet.save(ART / f'ShootingStanceTrial-{variant}.png')
(ANALYSIS / 'shooting-stance-trial-joints.json').write_text(json.dumps(records, indent=2))
print('50 stance/transition samples; unreachable feet=0')
