import sys
sys.path.insert(0,'LocalTools/Analysis/HomeVerification/CrouchSeam')
from bridge import call
code='if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Stop play first"); UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes(); var project=System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName; UnityEditor.EditorApplication.delayCall+=()=>UnityEditor.EditorApplication.OpenProject(project); return "reopening saved project";'
print(call('execute_code',{'action':'execute','code':code},6401))
