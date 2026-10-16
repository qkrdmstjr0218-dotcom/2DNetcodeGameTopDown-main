from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageFilter

OUT = Path(__file__).resolve().parent / 'FrontPartsSourceV3'
source = Image.open(OUT / 'weapon-visible-source.png').convert('RGBA')
pixels = np.array(source)
alpha = source.getchannel('A')
inside = alpha.point(lambda v: 255 if v else 0)
interior = np.array(inside.filter(ImageFilter.MinFilter(5))) > 0
edge = (np.array(inside) > 0) & ~interior
soft = np.array(alpha.filter(ImageFilter.GaussianBlur(0.55)))
result = pixels.copy()
# Only attenuate existing boundary pixels: never enlarge the silhouette or
# sample neighboring character/background colors into the weapon layer.
result[:, :, 3][edge] = np.minimum(pixels[:, :, 3][edge], soft[edge])
image = Image.fromarray(result)
image.save(OUT / 'weapon-edge-softened-source.png')
bounds = alpha.getbbox()
image.crop(bounds).save(OUT / 'weapon-edge-softened-tight.png')
board = Image.new('RGBA', (600, 1080), '#eeeeee')
for im, x in [(source, 0), (image, 300)]:
    detail = im.crop((445,425,595,955)).resize((300,1060), Image.Resampling.NEAREST)
    board.alpha_composite(detail, (x, 10))
board.convert('RGB').save(OUT / 'weapon-edge-before-after.png')
report = {'rgb_pixels_exact': bool(np.array_equal(result[:,:,:3],pixels[:,:,:3])),
          'interior_alpha_exact': bool(np.array_equal(result[:,:,3][interior],pixels[:,:,3][interior])),
          'modified_alpha_pixels': int(np.count_nonzero(result[:,:,3] != pixels[:,:,3])),
          'outside_original_silhouette_added_pixels': int(np.count_nonzero((pixels[:,:,3]==0)&(result[:,:,3]>0))),
          'source_bounds': bounds, 'output_bounds': image.getchannel('A').getbbox(),
          'unity_imported': False,
          'limitations': ['Antialias cleanup does not restore occluded surfaces or correct uncertain segmentation.',
                          'Generated edge-cleanup image was rejected because it changed internal design.']}
(OUT / 'weapon-edge-softening-check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
