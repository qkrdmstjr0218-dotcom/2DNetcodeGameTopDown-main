from pathlib import Path
import subprocess,json,concurrent.futures
from PIL import Image,ImageDraw,ImageFont
cfg=json.loads(Path('LocalTools/Analysis/VideoReview-2026-10-27/decoder.json').read_text());out=Path('LocalTools/Analysis/VideoReview-2026-10-28');sets={'stamina':list(range(970,994)),'respawn':list(range(404,416)),'end-transition':[748+i*.5 for i in range(16)],'grenade':[383+i*.5 for i in range(12)],'healing':list(range(624,636))};ts=sorted(set(sum(sets.values(),[])))
def name(t):return f'detail-{round(t*10):05}.jpg'
def grab(t):
 p=out/name(t);r=subprocess.run([cfg['exe'],'-hide_banner','-loglevel','error','-threads','1','-ss',str(t),'-i',cfg['source'],'-frames:v','1','-vf','scale=960:-2','-threads','1','-q:v','2','-y',str(p)],capture_output=True);return r.returncode
with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:assert not any(pool.map(grab,ts))
f=ImageFont.truetype('C:/Windows/Fonts/consola.ttf',18)
for key,subset in sets.items():
 sheet=Image.new('RGB',(1536,240*((len(subset)+3)//4)),(18,24,29));d=ImageDraw.Draw(sheet)
 for i,t in enumerate(subset):
  im=Image.open(out/name(t));im.thumbnail((384,216));x=i%4*384;y=i//4*240;sheet.paste(im,(x,y+24));d.text((x+8,y+2),f'{int(t)//60:02}:{t%60:04.1f}',font=f,fill='white')
 sheet.save(out/f'detail-{key}.jpg',quality=92)
(out/'details.json').write_text(json.dumps(sets,indent=2));print(len(ts),'additional time samples')
