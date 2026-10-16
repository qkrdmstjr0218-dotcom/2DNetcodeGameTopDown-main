import json
import math
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
ANALYSIS = ROOT / 'LocalTools/Analysis'
ART = ROOT / 'Assets/RightWalkPrototype/Art'
scope = {'__file__': str(ANALYSIS / 'bake_stride_trial.py')}
exec(compile((ANALYSIS / 'bake_stride_trial.py').read_text().split('ns = read_parts()')[0], '<parts>', 'exec'), scope)
ns = scope['read_parts']()
accepted = Image.open(ART / 'RightWalkSheet-Legs-FinalContour-v72.png').convert('RGBA')
sheet = Image.new('RGBA', (2560, 4096))
records = []


def foot(phase):
    t = phase % 1
    if t < .4:
        lift = 14 * ns['ease']((t - .3) / .1)
        return (312 - 130 * t / .4, 430 - lift), .16 * ns['ease']((t - .28) / .12)
    u = (t - .4) / .6
    blend = ns['ease'](u)
    return (182 + 130 * blend, 430 - 14 * (1-blend) - 42 * math.sin(math.pi*u)), .16*(1-blend)-.06*math.sin(math.pi*u)


for i in range(40):
    p = i / 40
    hip_y = 258 + 10 * math.sin(4*math.pi*p)
    tile = Image.new('RGBA', (512,512))
    record = {'frame': i, 'hipY': hip_y}
    for side in ('far', 'near'):
        hip, knee, ankle = (ns[side+n] for n in ('Hip','Knee','Ankle'))
        h = (hip[0]-12, hip_y)
        a, tilt = foot(p + (.5 if side=='far' else 0))
        l1, l2 = ns['dist'](hip,knee), ns['dist'](knee,ankle)
        d = ns['dist'](h,a)
        if d >= l1+l2:
            raise ValueError(f'Unreachable {i}/{side}: {d} >= {l1+l2}')
        phi = ns['angle'](h,a)
        bend = math.acos(max(-1,min(1,(l1*l1+d*d-l2*l2)/(2*l1*d))))
        k = (h[0]+l1*math.cos(phi-bend),h[1]+l1*math.sin(phi-bend))
        ns['draw_part'](tile,ns['fixed'][side],hip,h,ns['angle'](h,k)-ns['angle'](hip,knee))
        ns['draw_part'](tile,ns['calves'][side],knee,k,ns['angle'](k,a)-ns['angle'](knee,ankle))
        ns['draw_part'](tile,ns['new'],ns['nearAnkle'],(a[0]+(2 if side=='far' else 10),a[1]),tilt)
        ns['draw_part'](tile,ns['nearKneePatch'],ns['nearKnee'],k,ns['angle'](k,a)-ns['angle'](ns['nearKnee'],ns['nearAnkle']))
        record[side]={'hip':h,'knee':k,'ankle':a}
    tile.alpha_composite(accepted.crop((0,0,512,250)),(0,round(hip_y-252)))
    sheet.alpha_composite(tile,(i%5*512,i//5*512))
    records.append(record)
sheet.save(ART / 'RunTrial.png')
(ANALYSIS / 'run-trial-joints.json').write_text(json.dumps(records,indent=2))
print('40 run poses; unreachable feet=0; stance=40%, swing=60%; cycleDistance=1.5869140625')
