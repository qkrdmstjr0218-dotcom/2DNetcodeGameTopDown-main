"""Clean residual lower haze with adjacent white shading; preserve silver stroke."""
from pathlib import Path
from PIL import Image

out = Path(__file__).resolve().parent
before = Image.open(out / 'jump-ascent-cutout-v26.png').convert('RGBA')
after = before.copy()
for y in range(318, 324):
    donor = before.getpixel((306, y))
    if donor[3] < 230 or min(donor[:3]) < 190:
        continue
    for x in range(309, 316):
        old = before.getpixel((x, y))
        if old[3] < 230 or min(old[:3]) < 170:
            continue
        weight = min(1, (y-317)/2, (324-y)/2, (x-308)/2, (316-x)/2)
        target = tuple(max(value, minimum) for value, minimum in zip(donor[:3], (240, 241, 247)))
        rgb = tuple(round(a*(1-weight)+b*weight) for a, b in zip(old[:3], target))
        after.putpixel((x, y), (*rgb, old[3]))
assert before.getchannel('A').tobytes() == after.getchannel('A').tobytes()
after.save(out / 'jump-ascent-cutout-v28.png')
preview = Image.new('RGBA', (660, 440), (45, 62, 74, 255))
previous = Image.open(out / 'jump-ascent-cutout-v27.png').convert('RGBA')
for i, frame in enumerate((previous, after)):
    preview.alpha_composite(frame.crop((297, 295, 330, 335)).resize((297, 360),
                             Image.Resampling.NEAREST), (i*330, 24))
preview.save(out / 'jump-lower-white-brighter-before-after.png')
print('Lower white haze cleaned; silver segment and alpha retained.')
