"""Non-destructive visible-pixel partition; hidden surfaces remain unfinished."""
import json
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw

HERE=Path(__file__).resolve().parent
OUT=HERE/'FrontDesignParts'
OUT.mkdir(exist_ok=True)
source=Image.open(HERE/'front-receiver-corrected-preview.png').convert('RGBA')
pixels=np.asarray(source)
polygons={
 'weapon':[(490,441),(519,441),(523,491),(534,516),(541,578),(549,682),
           (551,808),(560,843),(558,880),(546,907),(527,914),(508,908),
           (497,881),(499,844),(501,813),(499,702),(498,602),(489,526),(483,484)],
 'trigger-hand':[(480,603),(499,605),(504,629),(502,674),(497,698),
                  (481,691),(470,670),(466,634)],
 'support-hand':[(558,694),(590,700),(608,738),(600,773),(582,799),
                  (557,813),(545,799),(548,765),(549,722)],
}
owner=np.zeros((source.height,source.width),dtype=np.uint8)
for index,(name,poly) in enumerate(polygons.items(),1):
    mask=Image.new('L',source.size);ImageDraw.Draw(mask).polygon(poly,fill=255)
    selection=np.asarray(mask)>0
    if name!='weapon':selection &= owner!=1
    owner[selection]=index
layers={}
for index,name in enumerate(['body-visible','weapon','trigger-hand','support-hand']):
    part=np.zeros_like(pixels)
    part[owner==index]=pixels[owner==index]
    layers[name]=Image.fromarray(part)
    layers[name].save(OUT/(name+'.png'))
# Verify exact visible-pixel reconstruction, not inferred hidden anatomy.
reconstruction=np.zeros_like(pixels)
for index,name in enumerate(layers):
    reconstruction[owner==index]=np.asarray(layers[name])[owner==index]
assert np.array_equal(reconstruction,pixels)
Image.fromarray(reconstruction).save(OUT/'reassembled.png')
board=Image.new('RGBA',(1000,700),(45,62,74,255))
draw=ImageDraw.Draw(board)
reference=source.copy();reference.thumbnail((380,640),Image.Resampling.LANCZOS)
board.alpha_composite(reference,(0,40));draw.text((15,15),'Full-body review',fill='white')
for x,name in [(390,'weapon'),(600,'trigger-hand'),(800,'support-hand')]:
    part=layers[name]
    bounds=part.getchannel('A').getbbox()
    detail=part.crop(bounds)
    scale=min(185/detail.width,590/detail.height)
    detail=detail.resize((round(detail.width*scale),round(detail.height*scale)),Image.Resampling.NEAREST)
    board.alpha_composite(detail,(x+(185-detail.width)//2,60))
    draw.text((x,20),name,fill='white')
draw.text((390,665),'Visible cutouts only / hidden grips and body not reconstructed.',fill='white')
board.save(OUT/'parts-review.png')
(OUT/'partition-review.json').write_text(json.dumps({'source':str(HERE/'front-receiver-corrected-preview.png'),
    'reassembled_pixel_difference':0,'runtime_installed':False,'segmentation_review_required':True,
    'limitations':['Polygon boundary estimates require visual refinement.','Hidden trigger/grip and hand/body surfaces not reconstructed.','Black muzzle-region composite background is still present in source and is not weapon art.']},indent=2))
print('PASS: zero changed pixels after reassembly. Not certified production segmentation; no Unity changes.')
