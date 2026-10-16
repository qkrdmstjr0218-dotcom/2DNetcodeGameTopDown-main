"""Preview-only half-pixel exterior outline increase on the thigh silhouettes."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageChops

out=Path(__file__).resolve().parent
before=Image.open(out/'jump-left-knee-glossy-plate-preview-v9.png').convert('RGBA')
alpha=before.getchannel('A')
expanded=alpha.resize((4096,4096),Image.Resampling.BICUBIC).filter(ImageFilter.MaxFilter(9))
expanded=expanded.resize(before.size,Image.Resampling.LANCZOS)
region=Image.new('L',before.size)
draw=ImageDraw.Draw(region)
draw.polygon([(270,265),(292,273),(337,293),(339,302),(317,305),(278,289)],fill=255)
draw.polygon([(228,258),(248,259),(251,285),(238,310),(223,313),(222,285)],fill=255)
region=region.filter(ImageFilter.GaussianBlur(1))
rim=ImageChops.multiply(ImageChops.subtract(expanded,alpha),region)
outline=Image.new('RGBA',before.size,(23,25,28,0))
outline.putalpha(rim)
after=Image.alpha_composite(outline,before)
after.save(out/'jump-left-knee-glossy-plate-preview-v10.png')
scope={'__file__':str(out/'preview_jump_full_body.py')}
exec(compile((out/'preview_jump_full_body.py').read_text().split('\nwalk = ')[0],
             '<static-upper-layers>','exec'),scope)
full=Image.new('RGBA',(512,512),(45,62,74,255))
full.alpha_composite(scope['compose'](after))
full.crop((95,25,485,485)).resize((585,690),Image.Resampling.LANCZOS).save(out/'jump-thigh-outline-full-body-v10.png')
preview=Image.new('RGBA',(840,450),(45,62,74,255))
for i,image in enumerate((before,after)):
    preview.alpha_composite(image.crop((205,245,345,395)).resize((420,450),Image.Resampling.NEAREST),(i*420,0))
preview.save(out/'jump-thigh-outline-before-after-v10.png')
print('Thigh exterior rim increased about 0.5px. Preview only.')
