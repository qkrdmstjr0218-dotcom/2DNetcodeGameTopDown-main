"""Full-body preview only; no Unity asset writes."""
from pathlib import Path
from PIL import Image

root=Path(__file__).resolve().parents[2]
out=root/'LocalTools/Analysis'
scope={'__file__':str(out/'preview_jump_full_body.py')}
exec(compile((out/'preview_jump_full_body.py').read_text().split('\nwalk = ')[0],
             '<static-upper-layers>','exec'),scope)
legs=Image.open(out/'jump-left-knee-glossy-plate-preview-v9.png').convert('RGBA')
full=scope['compose'](legs)
preview=Image.new('RGBA',(512,512),(45,62,74,255))
preview.alpha_composite(full)
preview=preview.crop((95,25,485,485)).resize((585,690),Image.Resampling.LANCZOS)
preview.save(out/'jump-left-knee-glossy-plate-full-body-v9.png')
print('Full-body preview saved. Unity assets unchanged.')
