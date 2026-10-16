from pathlib import Path
import json
import numpy as np
from PIL import Image

OUT = Path(__file__).resolve().parent
earlier = Image.open('C:/Users/PC/.codex/generated_images/01a12d87-d09d-7f20-a556-97e2f4b8fa44/exec-6417323b-8078-49c7-9757-b5061129bb74.png').convert('RGBA')
current = Image.open(OUT / 'front-accepted-grip-cutout-preview.png').convert('RGBA')
assert earlier.size == current.size, 'Restore requires identical pixel registration.'
a, b = np.array(earlier), np.array(current)
restored = b.copy()
# Reuse registered source colors without resampling or re-generation.
# Keep the existing extraction alpha so the old backdrop is not reintroduced.
restored[:425, :, :3] = a[:425, :, :3]
result = Image.fromarray(restored)
result.save(OUT / 'front-head-restored-cutout-preview.png')
background = Image.new('RGBA', result.size, (174, 184, 191, 255))
background.alpha_composite(result)
background.convert('RGB').save(OUT / 'front-head-restored-review.png')
left = earlier.crop((180, 10, 700, 420))
right = background.crop((180, 10, 700, 420)).convert('RGB')
comparison = Image.new('RGB', (1040, 410))
comparison.paste(left, (0, 0))
comparison.paste(right, (520, 0))
comparison.save(OUT / 'front-head-source-restoration-comparison.png')
report = {'source_head_rgb_exact': bool(np.array_equal(restored[:425, :, :3], a[:425, :, :3])),
          'below_head_exact': bool(np.array_equal(restored[425:], b[425:])),
          'alpha_unchanged': bool(np.array_equal(restored[:, :, 3], b[:, :, 3])),
          'resized': False, 'unity_imported': False,
          'limitation': 'Restores earlier front-view source colors only; does not recover detail missing from that source. Hair-edge alpha still uses the existing cutout.'}
(OUT / 'front-head-restoration-check.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report))
