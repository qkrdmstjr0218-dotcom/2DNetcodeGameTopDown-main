"""Connect only the marked silver shading segment, retaining the outer rim."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

out = Path(__file__).resolve().parent
before = Image.open(out / 'jump-ascent-cutout-v24.png').convert('RGBA')
after = before.copy()
scale = 12
mask = Image.new('L', (512*scale, 512*scale))
p0, control, p1 = (308.7, 305.8), (313.5, 310.5), (314, 316.7)
curve = []
for step in range(129):
    t = step/128
    curve.append(tuple(round(scale*((1-t)**2*p0[i]+2*(1-t)*t*control[i]
                                     +t*t*p1[i])) for i in (0, 1)))
ImageDraw.Draw(mask).line(curve, fill=255, width=18, joint='curve')
mask = mask.resize(before.size, Image.Resampling.LANCZOS).filter(ImageFilter.GaussianBlur(.35))
sample = before.getpixel((308, 304))[:3]
color = tuple(max(0, value-10) for value in sample)
for y in range(304, 319):
    for x in range(306, 316):
        old = before.getpixel((x, y))
        if old[3] > 230 and min(old[:3]) > 170:
            weight = mask.getpixel((x, y))/255*.85
            rgb = tuple(round(v*(1-weight)+c*weight) for v, c in zip(old[:3], color))
            after.putpixel((x, y), (*rgb, old[3]))
assert before.getchannel('A').tobytes() == after.getchannel('A').tobytes()
after.save(out / 'jump-ascent-cutout-v26.png')
preview = Image.new('RGBA', (660, 440), (45, 62, 74, 255))
previous = Image.open(out / 'jump-ascent-cutout-v25.png').convert('RGBA')
for i, frame in enumerate((previous, after)):
    preview.alpha_composite(frame.crop((297, 295, 330, 335)).resize((297, 360),
                             Image.Resampling.NEAREST), (i*330, 24))
preview.save(out / 'jump-silver-line-darker-before-after.png')
print(f'Marked silver segment connected with original upper shading color {color}.')
