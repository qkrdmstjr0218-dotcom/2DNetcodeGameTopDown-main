from pathlib import Path
import math
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[2]
scope = {'__file__': str(root / 'LocalTools/Analysis/bake_stride_trial.py')}
exec((root / 'LocalTools/Analysis/bake_stride_trial.py').read_text().split('ns = read_parts()')[0], scope)
ns = scope['read_parts']()
out = Image.new('RGB', (1200, 1400), '#526467')
for row, (side, phase) in enumerate((('near',0),('far',0),('near',.5),('far',.5))):
    hip, knee, ankle = [ns[side+n] for n in ('Hip', 'Knee', 'Ankle')]
    h = (hip[0]-12, 260)
    front = (side == 'near') == (phase == 0)
    a = (322,430) if front else (197,430)
    l1, l2 = ns['dist'](hip,knee), ns['dist'](knee,ankle)
    for column, (dx,dy) in enumerate(((-10,0),(0,0),(10,0),(10,-10))):
        dest = (a[0]+dx, a[1]+dy)
        d = ns['dist'](h,dest)
        phi = ns['angle'](h,dest)
        bend = math.acos(max(-1,min(1,(l1*l1+d*d-l2*l2)/(2*l1*d))))
        k = (h[0]+l1*math.cos(phi-bend),h[1]+l1*math.sin(phi-bend))
        im = Image.new('RGBA',(512,512))
        ns['draw_part'](im, ns['calves'][side], knee, k, ns['angle'](k,dest)-ns['angle'](knee,ankle))
        ns['draw_part'](im, ns['new'], ns['nearAnkle'], (a[0]+(2 if side=='far' else 10),a[1]), 0)
        crop=im.crop((int(a[0]-60),360,int(a[0]+65),480)).resize((300,288))
        out.paste(crop,(column*300,row*350+35),crop)
        ImageDraw.Draw(out).text((column*300+5,row*350+5),f'{side} phase {phase}: {dx}, {dy}',fill='white')
out.save(root / 'LocalTools/Analysis/standing-calibration.png')
