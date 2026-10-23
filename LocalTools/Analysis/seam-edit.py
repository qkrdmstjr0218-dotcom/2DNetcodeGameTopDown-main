from pathlib import Path
p=Path('Assets/RightWalkPrototype/Editor/ArenaReadyRoomReview.Seams.cs');s=p.read_text(encoding='utf-8');s=s.replace('float until=Time.realtimeSinceStartup+6,peakSpeed=0;','float until=Time.realtimeSinceStartup+6,peakSpeed=0,minJoinSpeed=float.MaxValue;')
s=s.replace('peakSpeed=Mathf.Max(peakSpeed,Mathf.Abs(body.linearVelocity.x));','peakSpeed=Mathf.Max(peakSpeed,Mathf.Abs(body.linearVelocity.x));\n                if(Mathf.Abs(body.position.x)>6.55f&&Mathf.Abs(body.position.x)<7.3f)minJoinSpeed=Mathf.Min(minJoinSpeed,Mathf.Abs(body.linearVelocity.x));')
s=s.replace('allReached&=Mathf.Abs(body.position.x-end)<.4f;','File.AppendAllText(folder+"/seam-speed-"+own.PlayerNumber+".txt",side+","+uphill+","+seated+",min="+minJoinSpeed+"\\n");\n            allReached&=Mathf.Abs(body.position.x-end)<.4f;')
p.write_text(s,encoding='utf-8')
