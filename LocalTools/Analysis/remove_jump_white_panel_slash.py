"""Remove only the stray interior slash using adjacent original panel texture."""
from pathlib import Path
from PIL import Image, ImageDraw

out = Path(__file__).resolve().parent
before = Image.open(out / 'jump-ascent-cutout-v16.png').convert('RGBA')
after = before.copy()
mask = Image.new('L', before.size)
ImageDraw.Draw(mask).rectangle((310, 308, 313, 322), fill=255)
changed = 0
for y in range(308, 323):
    for x in range(310, 314):
        if mask.getpixel((x, y)):
            old = before.getpixel((x, y))
            donor = before.getpixel((x-4, y))
            if old[3] > 200 and donor[3] > 200:
                after.putpixel((x, y), (*donor[:3], old[3]))
                changed += old[:3] != donor[:3]
assert before.getchannel('A').tobytes() == after.getchannel('A').tobytes()
after.save(out / 'jump-ascent-cutout-v17.png')
preview = Image.new('RGBA', (660, 440), (45, 62, 74, 255))
for i, frame in enumerate((before, after)):
    preview.alpha_composite(frame.crop((297, 295, 330, 335)).resize((297, 360),
                             Image.Resampling.NEAREST), (i*330, 24))
preview.save(out / 'jump-white-panel-slash-before-after.png')
print(f'{changed} interior pixels repaired from adjacent panel; alpha unchanged.')
