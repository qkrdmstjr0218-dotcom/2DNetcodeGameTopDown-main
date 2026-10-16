"""Continue the existing soft silver-blue shading without adding a hard seam."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

out = Path(__file__).resolve().parent
before = Image.open(out / 'jump-ascent-cutout-v20.png').convert('RGBA')
after = before.copy()
scale = 8
mask = Image.new('L', (512*scale, 512*scale))
points = []
for step in range(81):
    t = step/80
    x = (1-t)**2*307.5 + 2*(1-t)*t*309 + t*t*313
    y = (1-t)**2*304 + 2*(1-t)*t*313 + t*t*321
    points.append((round(x*scale), round(y*scale)))
ImageDraw.Draw(mask).line(points, fill=255, width=12, joint='curve')
mask = mask.resize(before.size, Image.Resampling.LANCZOS).filter(ImageFilter.GaussianBlur(.6))
for y in range(302, 323):
    fade = min(1, (y-301)/3, (323-y)/2)
    for x in range(304, 316):
        old = before.getpixel((x, y))
        if old[3] > 230 and min(old[:3]) > 175:
            weight = mask.getpixel((x, y))/255 * max(0, fade)*.5
            rgb = tuple(round(v*(1-weight)+c*weight)
                        for v, c in zip(old[:3], (195, 203, 225)))
            after.putpixel((x, y), (*rgb, old[3]))
assert before.getchannel('A').tobytes() == after.getchannel('A').tobytes()
after.save(out / 'jump-ascent-cutout-v22.png')
preview = Image.new('RGBA', (660, 440), (45, 62, 74, 255))
for i, frame in enumerate((before, after)):
    preview.alpha_composite(frame.crop((297, 295, 330, 335)).resize((297, 360),
                             Image.Resampling.NEAREST), (i*330, 24))
preview.save(out / 'jump-silver-highlight-late-fade-before-after.png')
print('Soft silver-blue shading extended; silhouette unchanged.')
