import ast
import json
import math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[2]
analysis = root / 'LocalTools/Analysis'
scope = {'__file__': str(analysis / 'bake_stride_trial.py')}
exec((analysis / 'bake_stride_trial.py').read_text().split('ns = read_parts()')[0], scope)
ns = scope['read_parts']()
tree = ast.parse((analysis / 'bake_run_recovery_trial.py').read_text())
helper = next(n for n in tree.body if isinstance(n, ast.Assign) and any(isinstance(t,ast.Name) and t.id=='helper' for t in n.targets))
functions = ast.parse(helper.value.value.args[0].value)
function = next(n for n in functions.body if isinstance(n,ast.FunctionDef) and n.name=='boot_transform')
env = {'ns': ns, 'math':math}
exec(compile(ast.Module(body=[function],type_ignores=[]),'<boot transform>','exec'),env)
replacement = next(n for n in tree.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='replacement' for t in n.targets))
exec(compile(ast.parse(replacement.value.value.value.args[0].value),'<foot path>','exec'),env)
poses = json.loads((analysis / 'run-recovery-trial-joints.json').read_text())
checks = json.loads((analysis / 'run-boot-clearance.json').read_text())
results = []
for page in range(4):
    montage = Image.new('RGB',(1200,1000),'#526467')
    for local in range(10):
        index = page*10+local
        for column, side in enumerate(('near','far')):
            pose = poses[index][side]
            phase = (index/40+(.5 if side=='far' else 0))%1
            check = next(c for c in checks if c['side']==side and abs(c['phase']-phase)<1e-6)
            rotation = check['rotation']
            ankle, knee = pose['ankle'],pose['knee']
            _, tilt = env['foot'](phase)
            position, rotation = env['boot_transform'](side,ankle,knee,phase,tilt)
            calf, boot = Image.new('RGBA',(512,512)),Image.new('RGBA',(512,512))
            ns['draw_part'](calf,ns['calves'][side],ns[side+'Knee'],knee,ns['angle'](knee,ankle)-ns['angle'](ns[side+'Knee'],ns[side+'Ankle']))
            ns['draw_part'](boot,ns['new'],ns['nearAnkle'],position,rotation)
            combined = Image.alpha_composite(calf,boot)
            x,y = ankle
            crop = combined.crop((int(x)-65,int(y)-65,int(x)+85,int(y)+60)).resize((240,200))
            slot = local*2+column
            montage.paste(crop,(slot%4*300,slot//4*200),crop)
            ImageDraw.Draw(montage).text((slot%4*300+5,slot//4*200+5),f'{index} {side} phase {phase:.3f}',fill='white')
    montage.save(analysis / f'run-ankle-audit-{page}.png')
