import sys,json
sys.path.insert(0,'LocalTools/Analysis/HomeVerification/CrouchSeam')
from bridge import call
r=call('execute_code',{'action':'execute','safety_checks':False,'code':'UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes(); var exe=UnityEditor.EditorApplication.applicationPath; UnityEditor.EditorApplication.delayCall+=()=>UnityEditor.EditorApplication.Exit(0); return exe;'},6401)
open('LocalTools/Analysis/unity-restart-exe.json','w').write(json.dumps(r))
print(r)

