from pathlib import Path
import json,re,copy
q=Path('LocalTools/Analysis/right-walk-reference-joints.json')
d=json.loads(q.read_text(encoding='utf-8'));before=copy.deepcopy(d['frames'])
d['frames'][1]['b'][1]=[42,76]
d['frames'][1]['caption']='붉은 무릎을 오른쪽 위로 소폭 이동'
d['frames'][4]['a'][2]=[20.5,95.5]
d['frames'][4]['a'][3]=[32,102]
d['frames'][4]['caption']='파란 발목을 위·뒤로 이동 · 발끝으로 이어지는 발 각도 수정'
d['latestAdjustment']='Frame 2 B knee (41,78)->(42,76). Frame 5 A ankle (22.39,97.79)->(20.5,95.5), toe (32.65,101.33)->(32,102).'
q.write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf-8')
p=Path('C:/Users/82102/.codex/visualizations/2026/10/09/01a12032-7676-7571-845e-57aac0c34cd2/walk-landmarks.html')
s=p.read_text(encoding='utf-8-sig')
s=re.sub(r' const data=\[.*?\];',' const data='+json.dumps(d['frames'],ensure_ascii=False)+';',s,flags=re.S)
s=s.replace('1–4번 무릎 높이·중심선 수정','2번 무릎 · 5번 발 수정')
p.write_text(s,encoding='utf-8')
print('Updated only frame 2 B knee and frame 5 A foot joints.')
