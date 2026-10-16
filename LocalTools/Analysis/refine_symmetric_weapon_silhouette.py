from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parent / 'FrontPartsSourceV3'
source = Image.open(OUT / 'weapon-straight-edge-trial.png').convert('RGBA')
a = np.array(source)
axis = 519
# Symmetric exterior profile only. Preserve the existing rail, optic, panels,
# screws and muzzle artwork instead of mirroring the entire weapon.
profile = [(432,22),(433,25),(436,29),(441,33),(448,35),(459,34),
           (470,31),(489,28),(510,27),(533,25),(550,22),(559,21),
           (584,21),(592,24),(604,29),(614,31),(628,31),(641,29),
           (653,27),(670,25),(688,25),(700,28),(707,28),(713,25),
           (750,25),(759,27),(773,28),(790,28),(799,29),(812,32),
           (828,32),(842,32),(854,32),(870,31),(882,29),(892,27),
           (902,25),(916,25),(924,23),(931,20),(936,15),(940,8),(941,4)]
scale = 8
mask_hi = Image.new('L',(source.width*scale,source.height*scale))
poly = [(int((axis-w)*scale),y*scale) for y,w in profile]
poly += [(int((axis+w)*scale),y*scale) for y,w in reversed(profile)]
ImageDraw.Draw(mask_hi).polygon(poly,fill=255)
mask = np.array(mask_hi.resize(source.size,Image.Resampling.LANCZOS))
result = np.zeros_like(a)
inside = mask > 0
existing = (a[:,:,3]>0) & inside
result[existing,:3] = a[existing,:3]
filled = inside & (a[:,:,3]==0)
for y in range(430,944):
    row = np.flatnonzero(a[y,:,3]>=128)
    if not len(row):
        continue
    for x in np.flatnonzero(filled[y]):
        # Missing exterior is the black casing, not a repeated gold accessory.
        # Extending individual highlights here would create new-looking shards.
        result[y,x,:3] = a[645,500,:3]
result[:,:,3] = mask
image = Image.fromarray(result)
image.save(OUT / 'weapon-symmetric-silhouette-trial.png')
box = image.getchannel('A').getbbox()
tight = image.crop(box)
tight.save(OUT / 'weapon-symmetric-silhouette-tight.png')
# Supersampled silhouette is for inspection, not new source texture detail.
tight.resize((tight.width*4,tight.height*4),Image.Resampling.LANCZOS).save(OUT / 'weapon-symmetric-silhouette-4x-review.png')
board = Image.new('RGBA',(600,1080),(47,62,72,255))
for im,x in [(source,0),(image,300)]:
    detail = im.crop((445,425,595,955)).resize((300,1060),Image.Resampling.LANCZOS)
    board.alpha_composite(detail,(x,10))
board.convert('RGB').save(OUT / 'weapon-symmetric-silhouette-review.png')
report = {'axis_x':axis,'internal_existing_rgb_exact':bool(np.array_equal(result[existing,:3],a[existing,:3])),
          'filled_boundary_pixels':int(filled.sum()),
          'trimmed_old_boundary_pixels':int(np.count_nonzero((a[:,:,3]>0)&~inside)),
          'source_length_landmarks_unchanged':[432,941],
          'generated_artifact_image_used':False,'unity_imported':False,
          'status':'Symmetric mechanical exterior trial, not approved runtime art.',
          'limitations':['Previously missing exterior is inferred as black casing using an existing casing color.',
                         '4x review interpolates existing texture; it does not recover missing detail.',
                         'Silhouette is symmetric; lighting and internal panel artwork are intentionally not mirrored.']}
(OUT / 'symmetric-silhouette-check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
