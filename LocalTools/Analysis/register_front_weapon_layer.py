from pathlib import Path
import json
from PIL import Image

OUT = Path(__file__).resolve().parent / 'FrontPartsTrialV2'
OUT.mkdir(exist_ok=True)
im = Image.open('C:/Users/PC/.codex/generated_images/01a12d87-d09d-7f20-a556-97e2f4b8fa44/exec-3b26e556-00c6-420d-bb68-f22267cdce89.png').convert('RGBA')
box = im.getchannel('A').point(lambda v: 255 if v >= 128 else 0).getbbox()
assert box is not None
crop = im.crop(box)
target_top, target_bottom, center_x = 449, 942, 519
scale = (target_bottom - target_top) / crop.height
crop = crop.resize((round(crop.width * scale), target_bottom - target_top), Image.Resampling.LANCZOS)
layer = Image.new('RGBA', (1024, 1536))
layer.alpha_composite(crop, (center_x - crop.width // 2, target_top))
layer.save(OUT / 'weapon-registration-trial.png')
preview = Image.new('RGBA', layer.size, (47, 62, 72, 255))
preview.alpha_composite(layer)
preview.crop((445, 420, 595, 975)).resize((300, 1110), Image.Resampling.NEAREST).convert('RGB').save(OUT / 'weapon-registration-review.png')
report = {'generated_bounds': box, 'uniform_registration_scale': scale,
          'registered_stock_top': target_top, 'registered_muzzle_bottom': target_bottom,
          'axis_x': center_x, 'unity_imported': False,
          'approved_production_layer': False,
          'limitations': ['Generated rifle is not an exact source-pixel extraction.',
                          'Length registration does not validate weapon width or original game-world dimensions.',
                          'Hands/arms/body layers still need separation and hidden-surface repair.']}
(OUT / 'registration-check.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report))
