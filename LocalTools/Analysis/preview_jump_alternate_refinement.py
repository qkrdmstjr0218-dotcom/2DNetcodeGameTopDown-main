"""Compare only the alternate-foot pose, using unchanged upper-body layers."""
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[2]
out = root / 'LocalTools/Analysis'
scope = {'__file__':str(out/'preview_jump_full_body.py')}
prefix = (out/'preview_jump_full_body.py').read_text().split('\nwalk = ')[0]
exec(compile(prefix,'<static-upper-layer-loader>','exec'),scope)
compose = scope['compose']
before = Image.open(out/'jump-alternate-before-left-knee-white-plate.png').convert('RGBA')
after = Image.open(root/'Assets/RightWalkPrototype/Resources/JumpTransition/AlternateAir.png').convert('RGBA')
preview = Image.new('RGBA',(1024,580),(45,62,74,255))
preview.alpha_composite(compose(before),(0,45))
preview.alpha_composite(compose(after),(512,45))
draw = ImageDraw.Draw(preview)
draw.text((20,15),'Alternate pose: before',fill='white')
draw.text((532,15),'Alternate pose: left knee white plate',fill='white')
preview.save(out/'jump-alternate-left-knee-white-plate-comparison.png')
compose(after).save(out/'jump-alternate-left-knee-white-plate-full-body.png')
print('Static alternate-pose comparison saved.')
