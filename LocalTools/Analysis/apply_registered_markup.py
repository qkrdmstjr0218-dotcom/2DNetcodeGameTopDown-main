from pathlib import Path
import json,re
records=json.loads(Path('LocalTools/Analysis/markup-registration.json').read_text())
out=Path('LocalTools/Analysis/right-walk-reference-joints.json')
data=json.loads(out.read_text(encoding='utf-8'))
# Joint centers read directly on the user-marked screenshot, in screenshot pixels.
marks=[
dict(a=[[80,508],[101,547],[73,597],[102,607]],b=[[76,512],[81,552],[69,602],[92,606]]),
dict(a=[[277,510],[299,543],[287,599],[319,607]],b=[[272,499],[267,542],[248,587],[279,597]]),
dict(a=[[455,518],[480,551],[499,593],[524,583]],b=[[451,510],[435,544],[412,581],[439,600]]),
dict(a=[[660,533],[677,559],[671,600],[704,609]],b=[[660,516],[650,552],[612,575],[629,598]])]
for index,(reg,mark) in enumerate(zip(records,marks),4):
    sx,sy=reg['offset'];scale=reg['scale']
    for leg in ('a','b'):
        data['frames'][index][leg]=[[round((x-sx)/scale,2),round((y-sy)/scale,2)] for x,y in mark[leg]]
    data['frames'][index]['caption']=f'{index+1}번 · 보강본의 선을 머리 위치 기준으로 정렬해 옮김'
data['registration']=records
data['markupJointPixels']=marks
data['status']='Frames 1-4 retained. Frames 5-8 transferred using per-frame head template registration; user markup readings approximate.'
out.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
p=Path('C:/Users/82102/.codex/visualizations/2026/10/09/01a12032-7676-7571-845e-57aac0c34cd2/walk-landmarks.html')
s=p.read_text(encoding='utf-8-sig')
s=re.sub(r' const data=\[.*?\];',' const data='+json.dumps(data['frames'],ensure_ascii=False)+';',s,flags=re.S)
s=s.replace('보강한 표시 기준 · 관절 중심 연결','5–8번 수정 · 보강본과 원본 정렬')
p.write_text(s,encoding='utf-8')
print(json.dumps(data['frames'][4:],ensure_ascii=False))
