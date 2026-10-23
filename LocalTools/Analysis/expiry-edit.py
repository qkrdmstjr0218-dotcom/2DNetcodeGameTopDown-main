from pathlib import Path
p=Path('Assets/RightWalkPrototype/Editor/ArenaReadyRoomReview.cs');s=p.read_text(encoding='utf-8');s=s.replace('stack.Push(File.Exists(folder+"/health.txt")?CheckHealthPickups():','stack.Push(File.Exists(folder+"/expiry.txt")?CheckDropExpiration():File.Exists(folder+"/health.txt")?CheckHealthPickups():');p.write_text(s,encoding='utf-8')
