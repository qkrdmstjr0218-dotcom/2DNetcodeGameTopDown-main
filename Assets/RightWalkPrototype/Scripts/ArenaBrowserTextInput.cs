using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;
namespace RightWalkPrototype
{
    // A real HTML input lets the browser/OS finish IME composition before saving.
    // The ordinary desktop IMGUI text fields keep their existing input path.
    public sealed class ArenaBrowserTextInput:MonoBehaviour
    {
        [Serializable] sealed class Request {public int ticket;public string title,text,hint;}
        [Serializable] sealed class Reply {public int ticket;public bool submitted;public string text;}
        int ticket;Action<string> completed;bool previousCapture;
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void ArenaTextOpen(string receiver,string request);
        [DllImport("__Internal")] static extern void ArenaTextClose();
#endif
        public static void Open(SideScrollNetworkSession owner,string title,string value,string hint,Action<string> completed)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var bridge=owner.GetComponent<ArenaBrowserTextInput>();if(bridge==null)bridge=owner.gameObject.AddComponent<ArenaBrowserTextInput>();
            bridge.Cancel();bridge.completed=completed;bridge.previousCapture=WebGLInput.captureAllKeyboardInput;
            WebGLInput.captureAllKeyboardInput=false;
            ArenaTextOpen(bridge.gameObject.name,JsonUtility.ToJson(new Request{ticket=++bridge.ticket,title=title,text=value,hint=hint}));
#endif
        }
        [Preserve] public void OnBrowserTextResult(string json)
        {
            if(completed==null)return;
            Reply reply;try{reply=JsonUtility.FromJson<Reply>(json);}catch(ArgumentException){Cancel();return;}
            if(reply==null||reply.ticket!=ticket)return;
            var callback=completed;completed=null;RestoreCapture();
            if(reply.submitted&&SideScrollNetworkSession.LobbyVisible)callback(reply.text??"");
        }
        void Update(){if(completed!=null&&!SideScrollNetworkSession.LobbyVisible)Cancel();}
        void RestoreCapture()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.captureAllKeyboardInput=previousCapture;
#endif
        }
        void Cancel()
        {
            if(completed==null)return;
            completed=null;ticket++;
#if UNITY_WEBGL && !UNITY_EDITOR
            ArenaTextClose();
#endif
            RestoreCapture();
        }
        void OnDisable()=>Cancel();
    }
}
