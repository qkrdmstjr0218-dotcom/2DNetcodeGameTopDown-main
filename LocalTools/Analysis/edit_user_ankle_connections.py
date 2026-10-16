"""Protect the user-selected ankle design; replace only two connection windows."""
import sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT=Path(__file__).resolve().parent
SOURCE='C:/Users/PC/AppData/Local/Temp/codex-clipboard-d1ed40d5-dd0a-4fb7-9c05-0393eb62e2fe.png'
base=Image.open(SOURCE).convert('RGBA')
enlarge=len(sys.argv)>2 and sys.argv[2]=='enlarge'
if enlarge:
    base=Image.open(OUT/'user-ankle-connections-only.png').convert('RGBA')
if len(sys.argv)>1:
    edited=Image.open(sys.argv[1]).convert('RGBA').resize(base.size,Image.Resampling.LANCZOS)
    mask=Image.new('L',base.size)
    draw=ImageDraw.Draw(mask)
    w,h=base.size
    # Only the last seam segment and cap apex; exclude collars and ornaments.
    draw.polygon([(w*.087,h*.19),(w*.122,h*.19),(w*.133,h*.39),
                  (w*.118,h*.445),(w*.09,h*.40)],fill=255)
    draw.polygon([(w*.866,h*.19),(w*.905,h*.19),(w*.912,h*.40),
                  (w*.88,h*.445),(w*.858,h*.39)],fill=255)
    if enlarge:
        draw.polygon([(w*.082,h*.10),(w*.128,h*.10),(w*.14,h*.39),(w*.12,h*.46),(w*.084,h*.43)],fill=255)
        draw.polygon([(w*.853,h*.10),(w*.91,h*.10),(w*.917,h*.43),(w*.88,h*.46),(w*.848,h*.39)],fill=255)
    soft=mask.filter(ImageFilter.MinFilter(5)).filter(ImageFilter.GaussianBlur(2))
    mask=Image.fromarray(np.minimum(np.asarray(mask),np.asarray(soft)))
    result=Image.composite(edited,base,mask)
    changed=np.any(np.asarray(base)!=np.asarray(result),axis=2)
    assert not np.any(changed&(np.asarray(mask)==0))
    stem='user-ankle-cap-enlarged' if enlarge else 'user-ankle-connections'
    result.save(OUT/(stem+'-only.png'))
    result.resize((1031,382),Image.Resampling.LANCZOS).save(OUT/(stem+'-display.png'))
    print(f'PASS: {changed.sum()} changed pixels within connection masks only; selected design/background preserved elsewhere.')
