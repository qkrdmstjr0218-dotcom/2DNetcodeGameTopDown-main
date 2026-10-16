from pathlib import Path
import json
import numpy as np
from PIL import Image

HERE = Path(__file__).resolve().parent
OUT = HERE / 'FrontPartsSourceV3'
base = Image.open(OUT / 'weapon-edge-softened-source.png').convert('RGBA')
donor = Image.open('C:/Users/PC/.codex/generated_images/01a12d87-d09d-7f20-a556-97e2f4b8fa44/exec-f52b26f9-4893-4740-ae18-7eb43eb3f743.png').convert('RGBA')
scale = 510 / donor.height
registered = Image.new('RGBA', base.size)
small = donor.resize((round(donor.width * scale), 510), Image.Resampling.LANCZOS)
registered.alpha_composite(small, (519-small.width//2,432))
a, d = np.array(base), np.array(registered)
zones = np.zeros(a.shape[:2],dtype=bool)
zones[712:758,490:501] = True
zones[716:755,541:548] = True
addition = zones & (a[:,:,3] == 0) & (d[:,:,3] >= 128)
result = a.copy()
result[addition] = d[addition]
completed = Image.fromarray(result)
completed.save(OUT / 'weapon-hidden-sides-trial.png')
source = Image.open(HERE / 'front-proportion-local-trial.png').convert('RGBA')
source_pixels = np.array(source)
selection = np.array(Image.open(OUT / 'weapon-selection-mask.png')) > 0
# This visibility check retains the original foreground occluders. It is not
# a completed body/hand separation or a demonstration of animated overlap.
visible = source_pixels.copy()
visible[selection] = np.array(Image.open(OUT / 'weapon-visible-source.png'))[selection]
assert np.array_equal(visible, source_pixels)
Image.fromarray(visible).save(OUT / 'hidden-side-static-occlusion-check.png')
board = Image.new('RGBA',(600,1080),(47,62,72,255))
for im,x in [(base,0),(completed,300)]:
    board.alpha_composite(im.crop((445,425,595,955)).resize((300,1060),Image.Resampling.NEAREST),(x,10))
board.convert('RGB').save(OUT / 'hidden-side-before-after.png')
report = {'added_pixels': int(addition.sum()),
          'existing_weapon_rgba_exact': bool(np.array_equal(result[a[:,:,3]>0],a[a[:,:,3]>0])),
          'generated_artifact_outside_zones_used': False,
          'stock_and_muzzle_bounds_unchanged': bool(np.array_equal(result[:712],a[:712]) and np.array_equal(result[758:],a[758:])),
          'static_foreground_reassembly_difference': int(np.any(visible != source_pixels,axis=2).sum()),
          'unity_imported': False, 'production_ready': False,
          'limitations': ['Only two small foreend occlusion zones reconstructed.',
                          'Hidden trigger grip remains unresolved.',
                          'Static visibility preservation does not validate complete layer boundaries or motion.']}
(OUT / 'hidden-side-completion-check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
