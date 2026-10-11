from pathlib import Path
from PIL import Image, ImageFilter, ImageDraw
import numpy as np

root = Path(__file__).resolve().parents[2]
im = Image.open(root / 'Assets/RightWalkPrototype/Art/RightWalkSheet-v4-clean.png').convert('RGBA')
a = np.array(im)
cw, ch = im.width / 4, im.height / 2
for row in range(2):
    for col in range(4):
        x0, y0 = round(col*cw), round(row*ch)
        x1, y1 = round((col+1)*cw), round((row+1)*ch)
        cell = a[y0:y1, x0:x1]
        original = Image.fromarray(cell[:, :, 3])
        contracted = np.array(original.filter(ImageFilter.MinFilter(7)).filter(ImageFilter.GaussianBlur(.45)))
        # Trim the head contour, with a gradual transition to the unmodified body.
        ys = np.arange(cell.shape[0])[:, None]
        weight = np.clip((190-ys)/25, 0, 1)
        cell[:, :, 3] = np.minimum(cell[:, :, 3],
            (contracted*weight + cell[:, :, 3]*(1-weight)).astype(np.uint8))
        # The gray crescent behind the cheek/chin is not face or weapon.
        mask = Image.new('L', (cell.shape[1]*4, cell.shape[0]*4), 255)
        d = ImageDraw.Draw(mask)
        points = [(288, 78), (283, 93), (277, 104), (272, 113), (269, 117),
                  (268, 119), (279, 115), (305, 113), (311, 100), (311, 78)]
        d.polygon([(int(x*4), int(y*4)) for x,y in points], fill=0)
        m = np.array(mask.resize((cell.shape[1],cell.shape[0]), Image.Resampling.LANCZOS))
        cell[:, :, 3] = (cell[:, :, 3].astype(np.uint16)*m//255).astype(np.uint8)
a[a[:, :, 3]<8] = 0
out = Image.fromarray(a)
out.save(root / 'Assets/RightWalkPrototype/Art/RightWalkSheet-v5-clean.png')
crop = out.crop((0,0,round(cw),round(ch)))
preview = Image.new('RGB',(crop.width*2,crop.height))
for i,color in enumerate([(245,245,245),(20,27,35)]):
    bg=Image.new('RGBA',crop.size,color+(255,)); bg.alpha_composite(crop)
    preview.paste(bg.convert('RGB'),(i*crop.width,0))
preview.save(root/'LocalTools/Captures/head-cutout-light-dark-v5.png')
print('Saved v5; head contour contracted 3px, cheek background trimmed; body unchanged.')
