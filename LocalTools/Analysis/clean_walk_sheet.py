from pathlib import Path
from PIL import Image, ImageFilter
import numpy as np

root = Path(__file__).resolve().parents[2]
source = root / 'Assets/RightWalkPrototype/Art/RightWalkSheet-v4-draft.png'
target = root / 'Assets/RightWalkPrototype/Art/RightWalkSheet-v4-clean.png'
im = Image.open(source).convert('RGBA')
a = np.array(im)
r, g, b = [a[:, :, i].astype(np.int16) for i in range(3)]
alpha = a[:, :, 3].copy()
# Pure primary-color contamination is unlike the muted material colors.
red = (r > 160) & (r > g * 2.1) & (r > b * 1.8)
blue = (b > 150) & (b > r * 2.2) & (b > g * 1.6)
cyan = (g > 160) & (b > 160) & (r < 70)
y = np.arange(a.shape[0])[:, None] % (a.shape[0] / 2)
hair_band = y < a.shape[0] / 2 * .43
yellow = (r > 180) & (g > 170) & (b < 85) & hair_band
bad = red | blue | cyan | yellow
# Red eyes/ear highlights belong to the face, not the unwanted outer halo.
x = np.arange(a.shape[1])[None, :] % (a.shape[1] / 4)
face = (x > a.shape[1] / 4 * .43) & (x < a.shape[1] / 4 * .70) & (y > a.shape[0] / 2 * .13) & (y < a.shape[0] / 2 * .35)
bad &= ~face
alpha[bad] = 0
# Remove low-opacity dust, then a small contour contraction; preserve opaque art.
solid = Image.fromarray((alpha > 195).astype(np.uint8) * 255)
solid = solid.filter(ImageFilter.MedianFilter(3)).filter(ImageFilter.MinFilter(3))
edge = np.array(solid.filter(ImageFilter.GaussianBlur(.45)))
edge[edge < 12] = 0
edge[edge > 243] = 255
a[:, :, 3] = edge
a[edge == 0, :3] = 0
Image.fromarray(a).save(target)
out = root / 'LocalTools/Captures'
out.mkdir(parents=True, exist_ok=True)
# Same crop against both backgrounds for edge inspection.
crop = Image.fromarray(a).crop((0, 0, round(im.width / 4), round(im.height / 2)))
preview = Image.new('RGB', (crop.width * 2, crop.height), 'white')
for i, color in enumerate([(245, 245, 245), (20, 27, 35)]):
    bg = Image.new('RGBA', crop.size, color + (255,))
    bg.alpha_composite(crop)
    preview.paste(bg.convert('RGB'), (i * crop.width, 0))
preview.save(out / 'walk-cutout-light-dark.png')
print({'source': im.size, 'color_noise_removed': int((bad & (a[:, :, 3] == 0)).sum()),
       'alpha_zero': int((edge == 0).sum()), 'output': str(target)})
