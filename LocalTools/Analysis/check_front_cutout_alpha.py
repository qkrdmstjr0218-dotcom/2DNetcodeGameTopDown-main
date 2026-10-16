from pathlib import Path
from PIL import Image, ImageDraw
import json

OUT = Path(__file__).resolve().parent
source = Path('C:/Users/PC/.codex/generated_images/01a12d87-d09d-7f20-a556-97e2f4b8fa44/exec-411711fd-a073-439c-a652-e092bc96e94e.png')
im = Image.open(source).convert('RGBA')
checker = Image.new('RGBA', im.size, '#d8dde1')
draw = ImageDraw.Draw(checker)
for y in range(0, im.height, 32):
    for x in range(0, im.width, 32):
        if (x // 32 + y // 32) % 2:
            draw.rectangle((x, y, x + 31, y + 31), fill='#aeb8bf')
checker.alpha_composite(im)
checker.convert('RGB').save(OUT / 'front-cutout-alpha-review.png')
im.save(OUT / 'front-accepted-grip-cutout-preview.png')
alpha = im.getchannel('A')
report = {'alpha_range': alpha.getextrema(),
          'background_samples': {str(p): im.getpixel(p)[3] for p in [(0, 0), (100, 300), (500, 1100), (500, 1500)]},
          'size': im.size, 'unity_imported': False,
          'scale_match': 'pending; do not infer world scale from canvas size'}
(OUT / 'front-cutout-alpha-check.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report))
