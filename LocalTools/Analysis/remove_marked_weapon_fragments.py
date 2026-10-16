from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parent / 'FrontPartsSourceV3'
source = Image.open(OUT / 'weapon-contour-clean-registered.png').convert('RGBA')
pixels = np.array(source)
result = pixels.copy()
removed = np.zeros(pixels.shape[:2],dtype=bool)

def trim_left(y0,y1,limits):
    for y in range(y0,y1):
        minimum = round(float(np.interp(y,[p[0] for p in limits],[p[1] for p in limits])))
        active = result[y,:minimum,3] > 0
        removed[y,:minimum] |= active
        result[y,:minimum] = 0

# Local cuts correspond to the three viewer-left circles in the user's review.
trim_left(464,510,[(464,489),(477,491),(493,493),(503,494),(509,494)])
trim_left(627,650,[(627,491),(639,492),(649,492)])
trim_left(690,706,[(690,494),(698,493),(705,493)])
# Follow-up circles: lower stock edge and the two small receiver-side remnants.
trim_left(492,529,[(492,494),(507,495),(519,495),(528,493)])
trim_left(624,641,[(624,493),(632,494),(640,493)])
trim_left(647,663,[(647,494),(655,495),(662,495)])
image = Image.fromarray(result)
image.save(OUT / 'weapon-marked-fragments-cleaned.png')
image.crop(source.getchannel('A').getbbox()).save(OUT / 'weapon-marked-fragments-cleaned-tight.png')
board = Image.new('RGBA',(600,1080),(47,62,72,255))
for im,x in [(source,0),(image,300)]:
    detail = im.crop((445,425,595,955)).resize((300,1060),Image.Resampling.NEAREST)
    board.alpha_composite(detail,(x,10))
board.convert('RGB').save(OUT / 'weapon-marked-fragments-review.png')
details = Image.new('RGBA',(720,600),(47,62,72,255))
for i,box in enumerate([(479,456,508,515),(482,617,505,657),(484,680,508,710)]):
    for im,j in [(source,0),(image,1)]:
        detail = im.crop(box).resize(((box[2]-box[0])*4,(box[3]-box[1])*4),Image.Resampling.NEAREST)
        details.alpha_composite(detail,(i*240+j*120,25))
ImageDraw.Draw(details).text((15,400),'Each pair: before / after. Local cuts only.',fill='white')
details.convert('RGB').save(OUT / 'weapon-three-fragments-details.png')
report = {'removed_pixels':int(removed.sum()),
          'retained_rgba_exact':bool(np.array_equal(result[~removed],pixels[~removed])),
          'canvas_unchanged':True,'weapon_rescaled':False,'unity_imported':False,
          'scope':'Three user-marked left-edge fragment regions only.',
          'remaining_review':'These cuts do not validate other-angle geometry or all unmarked edges.'}
(OUT / 'marked-fragments-cleanup-check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
