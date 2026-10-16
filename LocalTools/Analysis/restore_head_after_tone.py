from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parent
head = Image.open(OUT / 'front-head-restored-cutout-preview.png').convert('RGBA')
tone = Image.open('C:/Users/PC/.codex/generated_images/01a12d87-d09d-7f20-a556-97e2f4b8fa44/exec-00515aa4-8261-4555-9516-0a8b3c6b6546.png').convert('RGBA')
assert head.size == tone.size
mask = Image.new('L', head.size)
draw = ImageDraw.Draw(mask)
# Include the ponytail and face, exclude shoulders and chest below the hair.
draw.polygon([(0, 0), (1023, 0), (1023, 320), (650, 320),
              (636, 365), (609, 405), (570, 414), (504, 402),
              (469, 369), (449, 321), (380, 329), (373, 417), (0, 417)], fill=255)
a, b = np.array(head), np.array(tone)
selected = np.array(mask) > 0
pixels = b.copy()
pixels[selected] = a[selected]
result = Image.fromarray(pixels)
result.save(OUT / 'front-tone-head-preserved-preview.png')
background = Image.new('RGBA', result.size, (47, 62, 72, 255))
background.alpha_composite(result)
background.convert('RGB').save(OUT / 'front-tone-head-preserved-review.png')
original = Image.open(OUT / 'current-player-source-reference.png').convert('RGBA')
board = Image.new('RGBA', (1000, 580), (47, 62, 72, 255))
draw = ImageDraw.Draw(board)
baseline = 515
draw.line((20, baseline, 980, baseline), fill=(115, 161, 165), width=2)
for image, center in [(original, 245), (result, 735)]:
    box = image.getchannel('A').point(lambda x: 255 if x >= 128 else 0).getbbox()
    crop = image.crop(box)
    crop = crop.resize((round(crop.width * 436 / crop.height), 436), Image.Resampling.LANCZOS)
    board.alpha_composite(crop, (center - crop.width // 2, baseline - 436))
draw.text((55, 25), 'Existing player', fill='white')
draw.text((545, 25), 'Tone trial / restored source head', fill='white')
draw.text((25, 547), 'Display height normalized only. Perspective and anatomy still require review.', fill='white')
board.convert('RGB').save(OUT / 'front-tone-head-scale-review.png')
report = {'head_mask_rgba_exact': bool(np.array_equal(pixels[selected], a[selected])),
          'outside_head_mask_exact_to_tone': bool(np.array_equal(pixels[~selected], b[~selected])),
          'head_resampled': False, 'weapon_or_body_resized': False,
          'unity_imported': False, 'weapon_scale_validated': False,
          'proportion_adjusted': False,
          'limitations': ['Tone trial already contains small generated changes outside restored head.',
                          'A height-normalized view alone does not establish correct anatomical or weapon scale.']}
(OUT / 'front-tone-head-preservation-check.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report))
