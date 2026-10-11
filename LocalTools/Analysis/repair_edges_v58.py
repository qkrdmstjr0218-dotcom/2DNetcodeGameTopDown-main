from pathlib import Path
from PIL import Image,ImageDraw,ImageFilter
import numpy as np,json,math
root=Path.cwd();p=root/'Assets/RightWalkPrototype/Art';ns={'__file__':str(root/'LocalTools/Analysis/bake_walk_v32.py')};s=(root/'LocalTools/Analysis/bake_walk_v32.py').read_text().split('frames=[];points=[]')[0];exec('\n'.join(l for l in s.splitlines() if '.save(' not in l),ns)
# Restore the original trouser texture outside the overly tight thigh masks.
polys={'near':[(235,245),(278,242),(327,291),(325,337),(294,355),(249,315),(228,279)],'far':[(233,258),(280,275),(252,307),(225,337),(199,355),(174,338),(199,295)]}
patches={}
for side,poly in polys.items():
 expanded=ns['cut'](poly);a=np.array(expanded);old=np.array(ns[side+'Thigh']);y,x=np.indices((512,512));edge=np.array(Image.fromarray(old[:,:,3]).filter(ImageFilter.MinFilter(7)));keep=(edge<240)&(a[:,:,3]>0)&(a[:,:,:3].mean(2)<170)&~((a[:,:,0].astype(int)>a[:,:,1].astype(int)+12)&(a[:,:,1]>65))&(y>249)&(y<310);keep &= (x < (248 if side=="near" else 240));a[~keep]=0;patches[side]=Image.fromarray(a);patches[side].save(p/('Thigh-Edge-Patch-'+side+'-v58.png'))
pts=json.load(open(root/'LocalTools/Analysis/baked-walk-v32.json'));src=Image.open(p/'RightWalkSheet-Legs-LeftBootShift-v56.png').convert('RGBA');out=Image.new('RGBA',src.size)
for i,f in enumerate(pts):
 im=Image.new('RGBA',(512,512))
 for side in ['far','near']:
  hip=ns[side+'Hip'];knee=ns[side+'Knee'];ns['draw_part'](im,patches[side],hip,f[side]['hip'],ns['angle'](f[side]['hip'],f[side]['knee'])-ns['angle'](hip,knee))
 tile=src.crop((i%5*512,i//5*512,(i%5+1)*512,(i//5+1)*512));pa=np.array(im);ca=np.array(tile);pa[(ca[:,:,3]>240)&(ca[:,:,:3].mean(2)>140)]=0;tile.alpha_composite(Image.fromarray(pa))
 # Smooth only the tiny rear ankle spur, leaving the full calf and shoe placement intact.
 ar=np.array(tile);alpha=Image.fromarray(ar[:,:,3]);opened=np.array(alpha.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.MaxFilter(3)));yy,xx=np.indices((512,512))
 for side in ['far','near']:
  ax,ay=f[side]['ankle'];roi=(xx>ax-28)&(xx<ax-12)&(yy>ay-31)&(yy<ay-10);ar[:,:,3][roi]=np.minimum(ar[:,:,3][roi],opened[roi])
 out.alpha_composite(Image.fromarray(ar),(i%5*512,i//5*512))
out.save(p/'RightWalkSheet-Legs-EdgeCleanup-v58.png')
preview=Image.new('RGBA',(1200,350),'white')
for j,i in enumerate([0,5,10,15]):preview.alpha_composite(out.crop((i%5*512,i//5*512,(i%5+1)*512,(i//5+1)*512)).crop((215,235,320,345)).resize((300,314)),(300*j,0))
preview.save(root/'LocalTools/Analysis/thigh-edges-v58.png')



