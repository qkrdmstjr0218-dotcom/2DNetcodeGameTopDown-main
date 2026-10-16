"""Local receiver/sight geometry repair without a full-character repaint."""
import sys
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw,ImageFilter

OUT=Path(__file__).resolve().parent
base=Image.open(OUT/'front-stock-corrected-preview.png').convert('RGBA')
box=(480,560,580,725)
crop=base.crop(box)
if len(sys.argv)==1:
    crop.resize((500,825),Image.Resampling.LANCZOS).save(OUT/'front-receiver-local-input.png')
else:
    edit=Image.open(sys.argv[1]).convert('RGBA').resize(crop.size,Image.Resampling.LANCZOS)
    mask=Image.new('L',crop.size)
    ImageDraw.Draw(mask).polygon([(25,4),(70,4),(79,80),(78,139),(25,140),(18,81)],fill=255)
    mask=mask.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(1))
    result=base.copy();result.paste(Image.composite(edit,crop,mask),box[:2])
    changed=np.any(np.asarray(base)!=np.asarray(result),axis=2)
    allowed=np.zeros(changed.shape,dtype=bool);allowed[560:725,480:580]=np.asarray(mask)>0
    assert not np.any(changed&~allowed)
    result.save(OUT/'front-receiver-corrected-preview.png')
    result.crop(box).resize((500,825),Image.Resampling.NEAREST).save(OUT/'front-receiver-corrected-detail.png')
    print('PASS: receiver mask only; other pixels preserved.')
