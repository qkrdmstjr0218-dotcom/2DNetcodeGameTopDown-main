"""Local registered raster cleanup; preserve all pixels outside the marked knee."""
from pathlib import Path
from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parent
original = Image.open(OUT / 'jump-ascent-cutout-v5.png').convert('RGBA')
image = original.copy()
draw = ImageDraw.Draw(image)
# Remove the cap's foreign brass/black spike and restore its short dark tip.
draw.polygon([(318, 318), (324, 318), (324, 325), (321, 325),
              (321, 322), (318, 323)], fill=(0, 0, 0, 0))
draw.line([(311, 324), (317, 322), (320, 322), (321, 325)],
          fill=(29, 30, 33, 255), width=1)
# Replace the abrupt thick thigh edge with a narrow, continuous dark boundary.
draw.polygon([(301, 315), (304, 316), (309, 321), (311, 322),
              (309, 324), (304, 321), (301, 318)],
             fill=image.getpixel((298, 314)))
draw.line([(301, 315), (304, 318), (308, 321), (311, 322)],
          fill=(30, 31, 34, 255), width=1)
image.save(OUT / 'jump-ascent-cutout-v7.png')
preview = Image.new('RGBA', (1020, 500), (45, 62, 74, 255))
for index, frame in enumerate((original, image)):
    crop = frame.crop((275, 305, 330, 365)).resize((440, 480), Image.Resampling.NEAREST)
    preview.alpha_composite(crop, (index * 510, 20))
preview.save(OUT / 'jump-knee-pixel-cleanup-before-after.png')
changed = sum(a != b for a, b in zip(original.get_flattened_data(), image.get_flattened_data()))
print(f'Changed {changed} local pixels; pose and all other parts untouched.')
