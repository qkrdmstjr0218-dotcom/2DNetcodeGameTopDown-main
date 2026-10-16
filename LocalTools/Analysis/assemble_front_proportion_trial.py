from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT = Path(__file__).resolve().parent
base = Image.open(OUT / 'front-tone-head-preserved-preview.png').convert('RGBA')
trial = Image.open('C:/Users/PC/.codex/generated_images/01a12d87-d09d-7f20-a556-97e2f4b8fa44/exec-2eda318d-7f2d-40ba-b284-ec3383245780.png').convert('RGBA')
assert base.size == trial.size
mask = Image.new('L', base.size)
draw = ImageDraw.Draw(mask)
draw.rectangle((0, 0, 1023, 510), fill=255)
mask = mask.filter(ImageFilter.GaussianBlur(3))
# Keep the approved weapon and grip area independent of the proportion edit.
ImageDraw.Draw(mask).rectangle((477, 433, 564, 1535), fill=0)
result = Image.composite(trial, base, mask)
result.save(OUT / 'front-proportion-local-trial.png')
background = Image.new('RGBA', result.size, (47, 62, 72, 255))
background.alpha_composite(result)
background.convert('RGB').save(OUT / 'front-proportion-local-review.png')
a, b = np.array(base), np.array(result)
report = {'lower_region_exact': bool(np.array_equal(a[530:], b[530:])),
          'weapon_protected_region_exact': bool(np.array_equal(a[433:, 477:565], b[433:, 477:565])),
          'unity_imported': False,
          'status': 'Local head/shoulder proportion trial; hair is generated and not byte-preserved.',
          'limitations': ['Blend at shoulders needs visual review.', 'World scale and projected rifle length still unvalidated.']}
(OUT / 'front-proportion-local-check.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report))
