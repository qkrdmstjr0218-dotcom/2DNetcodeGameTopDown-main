from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = Path(__file__).resolve().parent
OUT = HERE / 'FrontPartsSourceV3'
source = Image.open(OUT / 'weapon-visible-source.png').convert('RGBA')
a = np.array(source)
result = a.copy()
added = np.zeros(a.shape[:2], dtype=bool)
# Continue the foreend edges between the visible endpoints. Only source
# boundary colors are extended; no pixels from the rejected AI image are used.
for y in range(709, 759):
    left = round(492 + (y-709) / 49 * 2)
    right = round(545 - (y-709) / 49)
    row = np.flatnonzero(a[y, 488:551, 3] >= 128) + 488
    if not len(row):
        continue
    for x in range(left, right+1):
        if a[y,x,3] != 0:
            continue
        nearest = int(row[np.argmin(np.abs(row-x))])
        result[y,x] = a[y,nearest]
        # The hidden extension is the black outer guard, not the adjacent white
        # panel; repeating that white panel would create an unsupported stripe.
        result[y,x,:3] = a[645,500,:3]
        added[y,x] = True

alpha = Image.fromarray(result[:,:,3])
inside = alpha.point(lambda v:255 if v else 0)
interior = np.array(inside.filter(ImageFilter.MinFilter(3))) > 0
edge = (result[:,:,3] > 0) & ~interior
soft = np.array(alpha.filter(ImageFilter.GaussianBlur(0.45)))
result[:,:,3][edge] = np.minimum(result[:,:,3][edge],soft[edge])
image = Image.fromarray(result)
image.save(OUT / 'weapon-contour-clean-registered.png')
box = source.getchannel('A').getbbox()
image.crop(box).save(OUT / 'weapon-contour-clean-tight.png')

board = Image.new('RGBA',(600,1080),(47,62,72,255))
for im,x in [(source,0),(image,300)]:
    detail = im.crop((445,425,595,955)).resize((300,1060),Image.Resampling.NEAREST)
    board.alpha_composite(detail,(x,10))
board.convert('RGB').save(OUT / 'weapon-contour-clean-review.png')

full = Image.open(HERE / 'front-proportion-local-trial.png').convert('RGBA')
foreground = Image.open(OUT / 'body-visible-with-weapon-hole.png').convert('RGBA')
# Restore original alpha in visible selected pixels for exact static assembly;
# edge antialias is for the independent weapon layer, not a re-render of the body.
layer = result.copy()
selection = np.array(Image.open(OUT / 'weapon-selection-mask.png')) > 0
layer[selection] = a[selection]
combined = Image.fromarray(layer)
combined.alpha_composite(foreground)
combined.save(OUT / 'weapon-contour-hand-overlap-preview.png')
overlap = Image.new('RGBA',combined.size,(47,62,72,255))
overlap.alpha_composite(combined)
overlap.crop((400,420,645,950)).resize((490,1060),Image.Resampling.NEAREST).convert('RGB').save(OUT / 'weapon-contour-hand-overlap-review.png')

opaque = a[:,:,3] > 0
report = {'existing_weapon_rgb_exact': bool(np.array_equal(result[:,:,:3][opaque],a[:,:,:3][opaque])),
          'added_foreend_pixels': int(added.sum()),
          'rejected_ai_image_used': False,
          'source_bounds': box,'output_bounds': image.getchannel('A').getbbox(),
          'weapon_scale_changed': False,'unity_imported': False,
          'status': 'Completed visible-view contour trial; not approved runtime asset.',
          'limitations': ['Hidden edge material is a conservative source-color continuation, not recovered original art.',
                          'Overlap preview still uses an unseparated foreground body/arms.',
                          'Underside grip and other-angle weapon views remain unvalidated.']}
(OUT / 'weapon-contour-clean-check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
