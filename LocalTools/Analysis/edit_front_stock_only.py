"""Restrict buttstock correction to its local silhouette and attachment."""
import sys
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw,ImageFilter

OUT=Path(__file__).resolve().parent
base=Image.open(OUT/'front-design-rifle-review.png').convert('RGBA')
box=(475,430,580,650)
crop=base.crop(box)
if len(sys.argv)==1:
    crop.resize((420,880),Image.Resampling.LANCZOS).save(OUT/'front-stock-local-input.png')
else:
    edit=Image.open(sys.argv[1]).convert('RGBA').resize(crop.size,Image.Resampling.LANCZOS)
    mask=Image.new('L',crop.size)
    ImageDraw.Draw(mask).polygon([(6,8),(64,8),(80,79),(73,117),(17,119),(6,63)],fill=255)
    mask=mask.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(1))
    result=base.copy();result.paste(Image.composite(edit,crop,mask),box[:2])
    changed=np.any(np.asarray(base)!=np.asarray(result),axis=2)
    allowed=np.zeros(changed.shape,dtype=bool)
    allowed[430:650,475:580]=np.asarray(mask)>0
    assert not np.any(changed&~allowed)
    result.save(OUT/'front-stock-corrected-preview.png')
    result.crop(box).resize((420,880),Image.Resampling.NEAREST).save(OUT/'front-stock-corrected-detail.png')
    print('PASS: only stock-region pixels changed.')
