from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageFilter

OUT = Path(__file__).resolve().parent / 'FrontPartsSourceV3'
source = Image.open(OUT / 'weapon-right-half-folded.png').convert('RGBA')
a = np.array(source)
result = a.copy()
alpha = source.getchannel('A')
inside = alpha.point(lambda v:255 if v else 0)
edge = (a[:,:,3]>0) & (np.array(inside.filter(ImageFilter.MinFilter(3)))==0)
partial = (a[:,:,3]>0)&(a[:,:,3]<255)
# A mild symmetric contrast curve tightens antialias coverage without a
# global texture sharpen, dark halo, or enlarged silhouette.
t = a[:,:,3].astype(np.float64)/255
coverage = np.clip((t-0.5)*1.18+0.5,0,1)
result[:,:,3][partial] = np.rint(coverage[partial]*255).astype(np.uint8)
result[:,:,:3][edge] = np.rint(result[:,:,:3][edge].astype(np.float64)*0.94).astype(np.uint8)
image = Image.fromarray(result)
image.save(OUT / 'weapon-folded-crisp-edges.png')
image.crop(source.getchannel('A').getbbox()).save(OUT / 'weapon-folded-crisp-edges-tight.png')
board = Image.new('RGBA',(600,1080),(47,62,72,255))
for im,x in [(source,0),(image,300)]:
    detail=im.crop((445,425,595,955)).resize((300,1060),Image.Resampling.LANCZOS)
    board.alpha_composite(detail,(x,10))
board.convert('RGB').save(OUT / 'weapon-crisp-edges-review.png')
axis=519
radius=min(axis,source.width-1-axis)
left=result[:,axis-radius:axis]
right=result[:,axis+1:axis+radius+1][:,::-1]
report={'mirror_pixel_difference':int(np.any(left!=right,axis=2).sum()),
        'interior_rgb_exact':bool(np.array_equal(result[:,:,:3][~edge],a[:,:,:3][~edge])),
        'source_size':source.size,'rescaled':False,'unity_imported':False,
        'scope':'Mild alpha contrast plus 6 percent darkening of the outermost edge only.'}
(OUT/'crisp-edge-check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
