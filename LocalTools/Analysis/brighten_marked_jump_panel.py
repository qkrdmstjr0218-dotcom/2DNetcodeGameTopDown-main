"""Brighten the user's marked broad interior haze, with feathered edges."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

out = Path(__file__).resolve().parent
before = Image.open(out / 'jump-ascent-cutout-v28.png').convert('RGBA')
after = before.copy()
mask = Image.new('L', before.size)
ImageDraw.Draw(mask).polygon([(302, 306), (305, 305), (307, 307),
                             (309, 311), (312, 315), (313, 319),
                             (310, 319), (308, 321), (305, 319),
                             (303, 315), (302, 311)], fill=255)
mask = mask.filter(ImageFilter.GaussianBlur(.85))
changed = 0
for y in range(302, 324):
    for x in range(299, 316):
        old = before.getpixel((x, y))
        if old[3] > 230 and min(old[:3]) > 175:
            weight = mask.getpixel((x, y))/255*.75
            rgb = tuple(round(value*(1-weight)+target*weight)
                        for value, target in zip(old[:3], (246, 247, 251)))
            new = (*rgb, old[3])
            after.putpixel((x, y), new)
            changed += new != old
assert before.getchannel('A').tobytes() == after.getchannel('A').tobytes()
after.save(out / 'jump-ascent-cutout-v29.png')
preview = Image.new('RGBA', (660, 440), (45, 62, 74, 255))
for i, frame in enumerate((before, after)):
    preview.alpha_composite(frame.crop((297, 295, 330, 335)).resize((297, 360),
                             Image.Resampling.NEAREST), (i*330, 24))
preview.save(out / 'jump-marked-panel-whiten-before-after.png')
print(f'Brightened {changed} interior pixels; alpha unchanged.')
