from pathlib import Path
import json
import numpy as np
from PIL import Image

OUT = Path(__file__).resolve().parent / 'FrontPartsSourceV3'
source = Image.open(OUT / 'weapon-symmetric-silhouette-trial.png').convert('RGBA')
a = np.array(source)
axis = 519
result = a.copy()
# The requested fold copies the more intact viewer-right half exactly.
# The center column is shared; no resampling, re-generation or scaling.
for x in range(axis):
    mirror = 2*axis-x
    result[:,x] = a[:,mirror] if mirror < source.width else 0
image = Image.fromarray(result)
image.save(OUT / 'weapon-right-half-folded.png')
bounds = image.getchannel('A').getbbox()
image.crop(bounds).save(OUT / 'weapon-right-half-folded-tight.png')
board = Image.new('RGBA',(600,1080),(47,62,72,255))
for im,x in [(source,0),(image,300)]:
    detail = im.crop((445,425,595,955)).resize((300,1060),Image.Resampling.LANCZOS)
    board.alpha_composite(detail,(x,10))
board.convert('RGB').save(OUT / 'weapon-folded-symmetry-review.png')
radius = min(axis,source.width-1-axis)
left = result[:,axis-radius:axis]
right = result[:,axis+1:axis+radius+1][:,::-1]
report = {'axis_x':axis,'copied_half':'viewer-right',
          'mirror_pixel_difference':int(np.any(left!=right,axis=2).sum()),
          'right_half_rgba_exact':bool(np.array_equal(result[:,axis:],a[:,axis:])),
          'source_bounds':source.getchannel('A').getbbox(),'output_bounds':bounds,
          'resampled':False,'unity_imported':False,
          'limitations':['Mirrors lighting as well as geometry.',
                         'Symmetry does not establish original-design fidelity or other-view consistency.']}
(OUT / 'folded-symmetry-check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
