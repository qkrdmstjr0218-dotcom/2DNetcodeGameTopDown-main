"""Edit only calf-end caps, not the accepted boot tongue or knee artwork."""
import sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT=Path(__file__).resolve().parent
original_design=len(sys.argv)>2 and sys.argv[2]=='original-design'
base=Image.open(OUT/('front-ankle-junction-only-preview.png' if original_design else 'front-approved-knees-fullbody-preview.png')).convert('RGBA')
box=(230,1120,825,1340)
crop=base.crop(box)
if len(sys.argv)==1:
    crop.resize((1190,440),Image.Resampling.LANCZOS).save(OUT/'front-ankle-side-input.png')
else:
    edit=Image.open(sys.argv[1]).convert('RGBA').resize(crop.size,Image.Resampling.LANCZOS)
    mask=Image.new('L',crop.size)
    draw=ImageDraw.Draw(mask)
    draw.polygon([(42,15),(172,15),(180,117),(151,146),(70,145),(42,112)],fill=255)
    draw.polygon([(411,15),(549,15),(565,112),(540,146),(455,146),(417,116)],fill=255)
    if original_design:
        draw.polygon([(34,75),(175,75),(185,178),(155,194),(40,185)],fill=255)
        draw.polygon([(414,75),(570,75),(578,182),(466,194),(413,176)],fill=255)
    soft=mask.filter(ImageFilter.MinFilter(7)).filter(ImageFilter.GaussianBlur(2))
    mask=Image.fromarray(np.minimum(np.asarray(mask),np.asarray(soft)))
    coverage=np.array(mask)
    coverage[np.max(np.asarray(edit)[:,:,:3],axis=2)<12]=0
    mask=Image.fromarray(coverage)
    result=base.copy();result.paste(Image.composite(edit,crop,mask),box[:2])
    allowed=np.zeros((base.height,base.width),dtype=bool)
    allowed[box[1]:box[3],box[0]:box[2]]=np.asarray(mask)>0
    changed=np.any(np.asarray(base)!=np.asarray(result),axis=2)
    assert not np.any(changed&~allowed)
    stem='front-ankle-original-design' if original_design else 'front-ankle-side'
    result.save(OUT/(stem+'-preview.png'))
    result.crop(box).resize((1190,440),Image.Resampling.NEAREST).save(OUT/(stem+'-detail.png'))
    print('Verified no changes outside calf-end cap masks.')
