"""Apply the confirmed angular tip path without changing silhouette or pose."""
from pathlib import Path
from PIL import Image, ImageDraw

out = Path(__file__).resolve().parent
source = Image.open(out / 'jump-ascent-cutout-v5.png').convert('RGBA')
result = source.copy()
scale = 8
mask = Image.new('L', (512 * scale, 512 * scale))
points = [(315, 323), (324, 321.5), (326, 330)]
ImageDraw.Draw(mask).line([(round(x*scale), round(y*scale)) for x, y in points],
                          fill=255, width=15, joint='curve')
mask = mask.resize((512, 512), Image.Resampling.LANCZOS)
changed = 0
for y in range(319, 333):
    for x in range(312, 329):
        weight = mask.getpixel((x, y)) / 255
        r, g, b, a = source.getpixel((x, y))
        if weight and a:
            pixel = (*[round(v*(1-weight)+c*weight)
                       for v, c in zip((r, g, b), (29, 30, 33))], a)
            result.putpixel((x, y), pixel)
            changed += pixel != (r, g, b, a)
assert source.getchannel('A').tobytes() == result.getchannel('A').tobytes()
result.save(out / 'jump-ascent-cutout-v12.png')
preview = Image.new('RGBA', (1020, 500), (45, 62, 74, 255))
previous = Image.open(out / 'jump-ascent-cutout-v11.png').convert('RGBA')
for i, frame in enumerate((previous, result)):
    preview.alpha_composite(frame.crop((275, 305, 330, 365)).resize((440, 480),
                             Image.Resampling.NEAREST), (i*510, 20))
preview.save(out / 'jump-knee-tip-thickness-before-after.png')
print(f'{changed} local RGB pixels changed; alpha and all other pixels preserved.')
