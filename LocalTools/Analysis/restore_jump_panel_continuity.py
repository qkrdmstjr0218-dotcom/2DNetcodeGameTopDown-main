"""Harmonic restoration of one continuous armor panel, not a pasted strip."""
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

out = Path(__file__).resolve().parent
before = Image.open(out / 'jump-ascent-cutout-v16.png').convert('RGBA')
pixels = np.array(before)
mask_image = Image.new('L', before.size)
ImageDraw.Draw(mask_image).polygon([(309, 305), (312, 307), (314, 315),
                                   (314, 321), (312, 322), (308, 320),
                                   (307, 312)], fill=255)
repair = np.asarray(mask_image) > 0
repair &= pixels[:, :, 3] > 230
# Only connected light panel pixels can supply boundary colors; dark trim cannot.
valid = (pixels[:, :, :3].min(axis=2) > 160) & (pixels[:, :, 3] > 230)
valid |= repair
rgb = pixels[:, :, :3].astype(float)
rgb[repair] = np.mean(rgb[valid & ~repair & (np.indices(valid.shape)[1] > 300)], axis=0)
count = np.zeros(valid.shape)
neighbors = []
for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
    allowed = np.roll(valid, (dy, dx), (0, 1))
    neighbors.append((dy, dx, allowed))
    count += allowed
for _ in range(800):
    total = np.zeros_like(rgb)
    for dy, dx, allowed in neighbors:
        total += np.roll(rgb, (dy, dx), (0, 1))*allowed[:, :, None]
    rgb[repair] = total[repair] / count[repair, None]
pixels[repair, :3] = np.rint(rgb[repair]).astype('uint8')
after = Image.fromarray(pixels)
assert before.getchannel('A').tobytes() == after.getchannel('A').tobytes()
after.save(out / 'jump-ascent-cutout-v20.png')
preview = Image.new('RGBA', (660, 440), (45, 62, 74, 255))
last = Image.open(out / 'jump-ascent-cutout-v19.png').convert('RGBA')
for i, frame in enumerate((last, after)):
    preview.alpha_composite(frame.crop((297, 295, 330, 335)).resize((297, 360),
                             Image.Resampling.NEAREST), (i*330, 24))
preview.save(out / 'jump-panel-continuity-before-after.png')
print(f'Restored {repair.sum()} pixels from surrounding connected panel shading.')
