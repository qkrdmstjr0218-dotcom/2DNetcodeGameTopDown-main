from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
OUT = HERE / 'FrontPartsSourceV3'
OUT.mkdir(exist_ok=True)
source = Image.open(HERE / 'front-proportion-local-trial.png').convert('RGBA')
pixels = np.array(source)
# Trace the currently visible weapon; do not invent hidden grip geometry.
outline = [
    (495,432),(535,432),(541,434),(546,439),(549,446),(550,459),
    (548,470),(546,480),(545,494),(544,510),(543,535),
    (542,550),(541,558),(542,585),(542,592),(546,601),
    (548,609),(549,618),(548,626),(549,636),(547,644),
    (546,654),(543,666),(543,680),(545,696),(546,705),
    (542,717),(541,730),(542,747),(545,758),(547,769),
    (546,790),(546,802),(549,814),(550,826),(550,837),
    (548,846),(550,858),(550,869),(548,880),(546,889),
    (544,901),(544,914),(542,927),(538,933),(533,938),
    (526,941),(517,941),(510,939),(504,936),(499,931),
    (496,924),(495,913),(495,902),(492,891),(490,882),
    (489,873),(489,863),(491,850),(490,841),(489,831),
    (490,819),(493,808),(490,800),(491,785),(491,770),
    (494,757),(498,750),(498,741),(494,733),(497,725),
    (496,714),(492,706),(491,700),(494,688),(495,675),
    (493,665),(490,655),(489,644),(489,636),(488,626),
    (487,615),(489,605),(491,598),(494,588),(493,575),
    (493,560),(496,549),(495,539),(491,531),(490,516),
    (490,500),(489,490),(486,482),(488,474),(486,465),
    (484,454),(486,446),(490,438)
]
mask = Image.new('L', source.size)
ImageDraw.Draw(mask).polygon(outline, fill=255)
selected = np.array(mask) > 0
weapon = np.zeros_like(pixels)
weapon[selected] = pixels[selected]
body = pixels.copy()
body[selected] = 0
Image.fromarray(weapon).save(OUT / 'weapon-visible-source.png')
weapon_image = Image.fromarray(weapon)
tight_box = weapon_image.getchannel('A').getbbox()
weapon_image.crop(tight_box).save(OUT / 'weapon-visible-tight.png')
Image.fromarray(body).save(OUT / 'body-visible-with-weapon-hole.png')
reassembled = body.copy()
reassembled[selected] = weapon[selected]
assert np.array_equal(reassembled, pixels)
Image.fromarray(reassembled).save(OUT / 'reassembled-check.png')
mask.save(OUT / 'weapon-selection-mask.png')
board = Image.new('RGBA', (640, 1080), (47,62,72,255))
for image, x in [(source, 0), (Image.fromarray(weapon), 320)]:
    detail = image.crop((440,425,600,955)).resize((320,1060), Image.Resampling.NEAREST)
    board.alpha_composite(detail, (x,20))
board.convert('RGB').save(OUT / 'weapon-extraction-review.png')
edge_board = Image.new('RGBA', (320, 540), '#eeeeee')
for image, x in [(weapon_image, 0), (weapon_image, 160)]:
    detail = image.crop((440,425,600,955))
    if x:
        ImageDraw.Draw(edge_board).rectangle((160,0,319,539), fill='#28333b')
    edge_board.alpha_composite(detail, (x,5))
edge_board.convert('RGB').save(OUT / 'weapon-light-dark-edge-review.png')
report = {'source': str(HERE / 'front-proportion-local-trial.png'),
          'source_size': source.size, 'resampled_layer': False,
          'reassembly_changed_pixels': int(np.any(reassembled != pixels,axis=2).sum()),
          'weapon_selected_pixels': int(selected.sum()),
          'tight_crop_bounds': tight_box,
          'tight_crop_original_offset': tight_box[:2],
          'unity_imported': False, 'production_ready': False,
          'limitations': ['Manual visible contour needs edge review.',
                          'Hand-occluded rifle surfaces and underside grip are not reconstructed.',
                          'Body has a weapon-shaped hole; not an animation-ready body layer.',
                          'Reassembly equality verifies preservation, not segmentation accuracy.']}
(OUT / 'source-extraction-check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
