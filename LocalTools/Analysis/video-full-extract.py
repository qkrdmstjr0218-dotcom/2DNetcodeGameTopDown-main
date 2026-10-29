from pathlib import Path
import subprocess,json,concurrent.futures,time
from PIL import Image,ImageDraw,ImageFont
cfg=json.loads(Path('LocalTools/Analysis/VideoReview-2026-10-27/decoder.json').read_text());out=Path('LocalTools/Analysis/VideoReview-2026-10-28');out.mkdir(parents=True,exist_ok=True)
times=list(range(0,1679,5));font=ImageFont.truetype('C:/Windows/Fonts/consola.ttf',18)
def grab(t):
 p=out/f'frame-{t:04}.jpg'
 r=subprocess.run([cfg['exe'],'-hide_banner','-loglevel','error','-threads','1','-ss',str(t),'-i',cfg['source'],'-frames:v','1','-vf','scale=768:-2','-threads','1','-q:v','3','-y',str(p)],capture_output=True)
 return t,r.returncode
with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
 done=list(pool.map(grab,times));assert all(code==0 for _,code in done)
for page in range((len(times)+23)//24):
 subset=times[page*24:(page+1)*24];sheet=Image.new('RGB',(1536,240*((len(subset)+3)//4)),(18,24,29));d=ImageDraw.Draw(sheet)
 for i,t in enumerate(subset):
  im=Image.open(out/f'frame-{t:04}.jpg');im.thumbnail((384,216));x=(i%4)*384;y=(i//4)*240;sheet.paste(im,(x,y+24));d.text((x+8,y+2),f'{t//60:02}:{t%60:02}',font=font,fill='white')
 sheet.save(out/f'timeline-{page+1:02}.jpg',quality=90)
(out/'coverage.json').write_text(json.dumps({'duration':1679.08,'stepSeconds':5,'frames':len(times),'pages':(len(times)+23)//24}))
print('336 chronological frames / 14 sheets prepared')
