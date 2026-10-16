"""Composite only the two seam-to-cap junctions; preserve background and boot pixels."""
import sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT=Path(__file__).resolve().parent
base=Image.open(OUT/'front-ankle-side-preview.png').convert('RGBA')
box=(230,1120,825,1340)
if len(sys.argv)>1:
    generated=Image.open(sys.argv[1]).convert('RGBA').resize((595,220),Image.Resampling.LANCZOS)
    mask=Image.new('L',(595,220));draw=ImageDraw.Draw(mask)
    draw.polygon([(53,44),(76,44),(87,72),(85,100),(60,98),(52,73)],fill=255)
    draw.polygon([(507,44),(535,44),(546,73),(535,100),(510,98),(503,72)],fill=255)
    soft=mask.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(1))
    mask=Image.fromarray(np.minimum(np.asarray(mask),np.asarray(soft)))
    result=base.copy();result.paste(Image.composite(generated,base.crop(box),mask),box[:2])
    changed=np.any(np.asarray(base)!=np.asarray(result),axis=2)
    allowed=np.zeros(changed.shape,dtype=bool)
    allowed[1120:1340,230:825]=np.asarray(mask)>0
    assert not np.any(changed&~allowed)
    result.save(OUT/'front-ankle-junction-only-preview.png')
    result.crop(box).resize((1190,440),Image.Resampling.NEAREST).save(OUT/'front-ankle-junction-only-detail.png')
    print(f'PASS: {changed.sum()} changed pixels only inside two junction masks; background/boots outside masks unchanged.')
