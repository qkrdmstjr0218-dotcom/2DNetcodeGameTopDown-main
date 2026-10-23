import sys
sys.path.insert(0,'LocalTools/Analysis/HomeVerification/CrouchSeam')
from bridge import call
code='var list=new System.Collections.Generic.List<string>(); foreach(var a in System.AppDomain.CurrentDomain.GetAssemblies())if(a.GetName().Name=="UnityEditor.PlayModeModule")foreach(var t in a.GetTypes())if(t.Name.Contains("Virtual")||t.Name.Contains("Player")||t.Name.Contains("Instance"))list.Add(t.FullName); return list;'
print(call('execute_code',{'action':'execute','code':code},6401))
