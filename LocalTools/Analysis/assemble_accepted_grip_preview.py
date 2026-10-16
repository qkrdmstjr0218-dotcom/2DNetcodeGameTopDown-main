from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFilter
import numpy as np

OUT = Path(__file__).resolve().parent
GEN = Path('C:/Users/PC/.codex/generated_images/01a12d87-d09d-7f20-a556-97e2f4b8fa44')
base = Image.open(GEN / 'exec-9ac857e7-79f5-43d3-b62d-bbaaf61f9a4f.png').convert('RGBA')
patch = Image.open(GEN / 'exec-9e2dc3ae-c397-447e-b581-712db89343af.png').convert('RGBA')
box = (395, 425, 650, 950)
patch = patch.resize((255, 525), Image.Resampling.LANCZOS)
mask = Image.new('L', patch.size)
draw = ImageDraw.Draw(mask)
# The patch is the approved generated edit, not a newly repainted character.
# Blend within the crop only; keep the head and lower body byte-identical.
draw.polygon([(2, 16), (228, 16), (249, 77), (249, 310),
              (173, 355), (171, 514), (95, 514), (94, 397),
              (10, 331), (4, 150)], fill=255)
mask = mask.filter(ImageFilter.GaussianBlur(2))
result = base.copy()
result.paste(patch, box[:2], mask)
result.save(OUT / 'front-accepted-grip-assembled.png')
result.crop((350, 400, 700, 960)).resize((700, 1120), Image.Resampling.LANCZOS).save(OUT / 'front-accepted-grip-connection-review.png')
a, b = np.array(base), np.array(result)
outside = np.ones(a.shape[:2], dtype=bool)
outside[425:950, 395:650] = False
report = {'outside_crop_changed_pixels': int(np.any(a != b, axis=2)[outside].sum()),
          'head_region_identical': bool(np.array_equal(a[:425], b[:425])),
          'lower_body_identical': bool(np.array_equal(a[950:], b[950:])),
          'unity_assets_changed': False,
          'status': 'static assembly preview; not segmented or animated'}
(OUT / 'front-accepted-grip-assembly-check.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report))
