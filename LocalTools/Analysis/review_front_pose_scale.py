from pathlib import Path
import json
from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parent
original = Image.open(OUT / 'current-player-source-reference.png').convert('RGBA')
front = Image.open(OUT / 'front-head-restored-cutout-preview.png').convert('RGBA')

def visible_bounds(image):
    return image.getchannel('A').point(lambda x: 255 if x >= 128 else 0).getbbox()

old_box = visible_bounds(original)
new_box = visible_bounds(front)
old_height = old_box[3] - old_box[1]
new_height = new_box[3] - new_box[1]
ratio = old_height / new_height
board = Image.new('RGBA', (1000, 580), (47, 62, 72, 255))
draw = ImageDraw.Draw(board)
baseline = 510
draw.line((20, baseline, 980, baseline), fill=(115, 161, 165), width=2)
for image, box, center, title in [(original, old_box, 245, 'Current player / source pixels'),
                                  (front, new_box, 735, 'Front pose / height-normalized REVIEW')]:
    crop = image.crop(box)
    if image is front:
        crop = crop.resize((round(crop.width * ratio), old_height), Image.Resampling.LANCZOS)
    board.alpha_composite(crop, (center - crop.width // 2, baseline - crop.height))
    draw.text((center - 190, 25), title, fill='white')
draw.text((25, 540), 'Preview only: matching silhouette height is NOT a world-scale or weapon-size validation.', fill='white')
board.convert('RGB').save(OUT / 'front-pose-scale-review.png')
report = {'original_visible_bounds': old_box, 'front_visible_bounds': new_box,
          'review_only_display_ratio': ratio,
          'source_images_resized_or_overwritten': False,
          'unity_root_scale_changed': False,
          'weapon_size_validated': False,
          'next_checks': ['Match head/sole landmarks, excluding ponytail height.',
                          'Confirm rifle dimensions accounting for camera foreshortening.',
                          'Separate complete layers and reconstruct hidden surfaces before motion tests.']}
(OUT / 'front-pose-scale-review.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report))
