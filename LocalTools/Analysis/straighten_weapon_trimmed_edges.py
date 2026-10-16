from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageFilter

OUT = Path(__file__).resolve().parent / 'FrontPartsSourceV3'
source = Image.open(OUT / 'weapon-marked-fragments-cleaned.png').convert('RGBA')
a = np.array(source)
result = a.copy()

def straight_trim(points):
    for y in range(points[0][0],points[-1][0]+1):
        left = round(float(np.interp(y,[p[0] for p in points],[p[1] for p in points])))
        result[y,:left] = 0

# Straight runs and short bevels replace discontinuities between local cuts.
straight_trim([(491,493),(493,495),(522,495),(525,493),(531,493)])
straight_trim([(622,492),(624,494),(650,494),(652,495),(674,495),(676,494)])
straight_trim([(687,495),(704,495),(707,493)])
alpha = Image.fromarray(result[:,:,3])
inside = alpha.point(lambda v:255 if v else 0)
edge = (result[:,:,3]>0) & (np.array(inside.filter(ImageFilter.MinFilter(3)))==0)
# Antialias only the three trimmed regions; do not soften the rifle interior.
zones = np.zeros(edge.shape,dtype=bool)
zones[489:534,480:501] = True
zones[620:679,482:502] = True
zones[685:710,485:502] = True
edge &= zones
soft = np.array(alpha.filter(ImageFilter.GaussianBlur(0.55)))
result[:,:,3][edge] = np.minimum(result[:,:,3][edge],soft[edge])
im = Image.fromarray(result)
im.save(OUT / 'weapon-straight-edge-trial.png')
board = Image.new('RGBA',(600,1080),(47,62,72,255))
for image,x in [(source,0),(im,300)]:
    detail = image.crop((445,425,595,955)).resize((300,1060),Image.Resampling.NEAREST)
    board.alpha_composite(detail,(x,10))
board.convert('RGB').save(OUT / 'weapon-straight-edge-review.png')
kept = result[:,:,3]>0
report = {'retained_rgb_exact':bool(np.array_equal(result[:,:,:3][kept],a[:,:,:3][kept])),
          'removed_pixels':int(np.count_nonzero((a[:,:,3]>0)&(result[:,:,3]==0))),
          'changed_alpha_pixels':int(np.count_nonzero(a[:,:,3]!=result[:,:,3])),
          'canvas_and_weapon_scale_unchanged':True,'unity_imported':False,
          'scope':'Straight local viewer-left edges with short bevel transitions; other regions unchanged.'}
(OUT / 'straight-edge-check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
