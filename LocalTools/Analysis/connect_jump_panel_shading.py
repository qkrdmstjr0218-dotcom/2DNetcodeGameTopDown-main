"""Continue the upper blue shading through the repaired white panel junction."""
from pathlib import Path
from PIL import Image

out = Path(__file__).resolve().parent
before = Image.open(out / 'jump-ascent-cutout-v18.png').convert('RGBA')
after = before.copy()
for y in range(305, 323):
    t = (y-305)/17
    for x in range(301, 315):
        old = before.getpixel((x, y))
        if old[3] < 230 or min(old[:3]) < 160:
            continue
        # Follow the slight rightward drift of the panel instead of a vertical fill.
        sx = x - t*1.5
        ix = int(sx)
        fraction = sx-ix
        a, b = before.getpixel((ix, 304)), before.getpixel((ix+1, 304))
        if min(a[:3]) < 170 or min(b[:3]) < 170:
            continue
        upper = [a[c]*(1-fraction)+b[c]*fraction for c in range(3)]
        # Let the blue highlight fade gradually into the lower white face.
        target = [upper[c]*(1-t*.55)+old[c]*(t*.55) for c in range(3)]
        feather_x = min(1, (x-300)/3, (315-x)/2)
        feather_y = min(1, (y-304)/3, (323-y)/3)
        weight = max(0, feather_x*feather_y)*.85
        rgb = tuple(round(old[c]*(1-weight)+target[c]*weight) for c in range(3))
        after.putpixel((x, y), (*rgb, old[3]))
assert before.getchannel('A').tobytes() == after.getchannel('A').tobytes()
after.save(out / 'jump-ascent-cutout-v19.png')
preview = Image.new('RGBA', (660, 440), (45, 62, 74, 255))
for i, frame in enumerate((before, after)):
    preview.alpha_composite(frame.crop((297, 295, 330, 335)).resize((297, 360),
                             Image.Resampling.NEAREST), (i*330, 24))
preview.save(out / 'jump-panel-shading-connect-before-after.png')
print('Upper shading and left junction blended; alpha preserved.')
