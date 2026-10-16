"""Crop and composite knee-only edits, preserving all pixels outside masks."""
import sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT=Path(__file__).resolve().parent
SOURCE=Path('C:/Users/PC/.codex/generated_images/01a12d87-d09d-7f20-a556-97e2f4b8fa44/exec-6417323b-8078-49c7-9757-b5061129bb74.png')
BOX=(300,875,750,1090)
base=Image.open(SOURCE).convert('RGBA')
crop=base.crop(BOX)
if len(sys.argv)==1:
    crop.resize((900,430),Image.Resampling.LANCZOS).save(OUT/'front-knees-local-input.png')
else:
    edit=Image.open(sys.argv[1]).convert('RGBA')
    arr=np.asarray(edit)
    rows=np.flatnonzero(np.any(arr[:,:,:3]>18,axis=(1,2)))
    edit=edit.crop((0,int(rows[0]),edit.width,int(rows[-1])+1)).resize(crop.size,Image.Resampling.LANCZOS)
    mask=Image.new('L',crop.size)
    draw=ImageDraw.Draw(mask)
    draw.polygon([(60,14),(145,14),(148,91),(133,111),(64,108),(49,78)],fill=255)
    draw.polygon([(295,14),(383,14),(404,78),(389,109),(318,112),(294,90)],fill=255)
    # Blend inside the mask to avoid a hard horizontal join across shield shading.
    soft=mask.filter(ImageFilter.MinFilter(9)).filter(ImageFilter.GaussianBlur(3))
    mask=Image.fromarray(np.minimum(np.asarray(mask),np.asarray(soft)))
    result=base.copy()
    result.paste(Image.composite(edit,crop,mask),BOX[:2])
    changed=np.any(np.asarray(result)!=np.asarray(base),axis=2)
    allowed=np.zeros(changed.shape,dtype=bool)
    allowed[BOX[1]:BOX[3],BOX[0]:BOX[2]]=np.asarray(mask)>0
    assert not np.any(changed&~allowed)
    result.save(OUT/'front-knees-local-preview.png')
    result.crop(BOX).resize((900,430),Image.Resampling.NEAREST).save(OUT/'front-knees-local-detail.png')
    print(f'Changed {changed.sum()} pixels; zero changes outside knee masks.')
