import json
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[2]
analysis = root / 'LocalTools/Analysis'
scope = {'__file__':str(analysis / 'bake_stride_trial.py')}
exec((analysis / 'bake_stride_trial.py').read_text().split('ns = read_parts()')[0],scope)
ns = scope['read_parts']()
poses = json.loads((analysis / 'walk-stride-trial-joints.json').read_text())
sheet = Image.open(root / 'Assets/RightWalkPrototype/Art/WalkStrideTrial.png').convert('RGBA')
output = Image.new('RGB',(1200,800),'#263c4a')
for row, (frame,side) in enumerate(((0,'far'),(20,'near'))):
    pose = poses[frame][side]
    thigh, patch = Image.new('RGBA',(512,512)),Image.new('RGBA',(512,512))
    ns['draw_part'](thigh,ns['fixed'][side],ns[side+'Hip'],pose['hip'],ns['angle'](pose['hip'],pose['knee'])-ns['angle'](ns[side+'Hip'],ns[side+'Knee']))
    ns['draw_part'](patch,ns['nearKneePatch'],ns['nearKnee'],pose['knee'],ns['angle'](pose['knee'],pose['ankle'])-ns['angle'](ns['nearKnee'],ns['nearAnkle']))
    actual = sheet.crop((frame%5*512,frame//5*512,frame%5*512+512,frame//5*512+512))
    for col,(label,im) in enumerate((('Thigh only',thigh),('Knee patch only',patch),('Displayed sheet',actual))):
        crop = im.crop((175,330,275,430)).resize((400,400))
        output.paste(crop,(col*400,row*400),crop)
        ImageDraw.Draw(output).text((col*400+5,row*400+5),f'{frame} {side}: {label}',fill='white')
output.save(analysis / 'thigh-seam-layers.png')
