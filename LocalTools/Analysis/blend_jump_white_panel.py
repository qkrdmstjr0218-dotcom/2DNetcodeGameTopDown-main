"""Blend repaired interior shading while preserving the original outer contour."""
from pathlib import Path
from PIL import Image

out = Path(__file__).resolve().parent
before = Image.open(out / 'jump-ascent-cutout-v17.png').convert('RGBA')
after = before.copy()
for y in range(308, 323):
    for x in range(309, 315):
        samples = []
        for sy in range(y-2, y+3):
            for sx in range(x-4, x+4):
                pixel = before.getpixel((sx, sy))
                if pixel[3] > 200 and min(pixel[:3]) > 175:
                    weight = 1 / (1+(sx-x)**2+(sy-y)**2)
                    samples.append((pixel, weight))
        if not samples:
            continue
        total = sum(weight for _, weight in samples)
        rgb = tuple(round(sum(pixel[c]*weight for pixel, weight in samples)/total)
                    for c in range(3))
        old = before.getpixel((x, y))
        if old[3] > 200 and min(old[:3]) > 150:
            # Feather ends into the untouched rows; keep the interior gradient smooth.
            weight = min(1, (y-307)/3, (323-y)/3)
            color = tuple(round(a*(1-weight)+b*weight) for a, b in zip(old[:3], rgb))
            after.putpixel((x, y), (*color, old[3]))
assert before.getchannel('A').tobytes() == after.getchannel('A').tobytes()
after.save(out / 'jump-ascent-cutout-v18.png')
preview = Image.new('RGBA', (660, 440), (45, 62, 74, 255))
for i, frame in enumerate((before, after)):
    preview.alpha_composite(frame.crop((297, 295, 330, 335)).resize((297, 360),
                             Image.Resampling.NEAREST), (i*330, 24))
preview.save(out / 'jump-white-panel-blend-before-after.png')
print('Repaired interior gradient blended; alpha unchanged.')
