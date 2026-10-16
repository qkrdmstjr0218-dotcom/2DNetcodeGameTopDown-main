"""Assemble accepted local artwork without globally regenerating the character."""
import sys
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw,ImageFilter

OUT=Path(__file__).resolve().parent
base=Image.open(OUT/'front-approved-knees-fullbody-preview.png').convert('RGBA')
ankles=Image.open(OUT/'user-ankle-cap-enlarged-only.png').convert('RGBA').resize((595,220),Image.Resampling.LANCZOS)
box=(230,1120,825,1340)
mask=Image.new('L',(595,220));draw=ImageDraw.Draw(mask)
draw.polygon([(40,5),(133,5),(139,113),(120,147),(34,147)],fill=255)
draw.polygon([(458,5),(561,5),(565,147),(464,147),(454,111)],fill=255)
mask=mask.filter(ImageFilter.GaussianBlur(2))
base.paste(Image.composite(ankles,base.crop(box),mask),box[:2])
gunbox=(465,430,625,930)
if len(sys.argv)==1:
    base.save(OUT/'front-design-assembled-review.png')
    base.crop(gunbox).resize((480,1500),Image.Resampling.LANCZOS).save(OUT/'front-rifle-local-input.png')
else:
    edit=Image.open(sys.argv[1]).convert('RGBA').resize((160,500),Image.Resampling.LANCZOS)
    mask=Image.new('L',(160,500));draw=ImageDraw.Draw(mask)
    # Include both the old slanted barrel and the corrected straight muzzle footprint.
    draw.polygon([(18,6),(87,6),(93,90),(112,205),(140,445),
                  (143,496),(35,496),(35,373),(35,230),(21,108)],fill=255)
    mask=mask.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(1))
    before=np.array(base)
    base.paste(Image.composite(edit,base.crop(gunbox),mask),gunbox[:2])
    allowed=np.zeros(before.shape[:2],dtype=bool)
    allowed[430:930,465:625]=np.asarray(mask)>0
    assert not np.any(np.any(before!=np.asarray(base),axis=2)&~allowed)
    base.save(OUT/'front-design-rifle-review.png')
    base.crop(gunbox).resize((320,1000),Image.Resampling.NEAREST).save(OUT/'front-rifle-local-detail.png')
    print('PASS: rifle edit restricted to mask; all other assembled pixels preserved.')
