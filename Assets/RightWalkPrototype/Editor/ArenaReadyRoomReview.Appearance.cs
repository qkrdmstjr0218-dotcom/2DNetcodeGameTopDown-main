using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;
public static partial class ArenaReadyRoomReview
{
    [MenuItem("Tools/Right Walk Prototype/Verify Original Character Colors")]
    public static void BeginAppearance(){BeginFoundry();File.WriteAllText(folder+"/appearance.txt","appearance");}
    static bool AllPalettesVisible()
    {
        var all=Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None);
        if(all.Length!=4)return false;
        var block=new MaterialPropertyBlock();
        foreach(var p in all)
        {
            if(p.PlayerNumber<1||p.PlayerNumber>4)return false;
            bool torso=false;
            foreach(var r in p.GetComponentsInChildren<Renderer>(true))
            {
                r.GetPropertyBlock(block);
                if(r.name=="Torso and Hip Clothing")
                {
                    Color expected=Color.clear;
                    Color actual=block.GetColor("_Color");
                    if(Vector4.Distance(actual,expected)>.01f)return false;
                    // The source material must remain shared and un-tinted.
                    if(r.sharedMaterial.GetColor("_Color")!=Color.white)return false;
                    torso=true;
                }
                if(r.name=="Head and Front Hair"&&block.GetColor("_Color")!=Color.clear)return false;
                block.Clear();
            }
            if(!torso)return false;
        }
        return true;
    }
    static IEnumerator CheckAppearance()
    {
        yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4&&p.palettes),"all four peers retain original character materials without body or face tint",45);
        ScreenCapture.CaptureScreenshot(folder+"/original-character-colors.png");yield return new WaitForSecondsRealtime(.3f);
        yield return CheckAmmoPreselection();
        var m=ArenaObjectiveMatch.Instance;Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"appearance peers ready");Require(m.StartRound(),"appearance match starts");
        yield return Wait(()=>All(4,p=>p.phase==1&&p.palettes),"original character colors persist through transition and gameplay",20);
        var own=Player(1);uint life=own.LifeVersion;own.ReceiveServerDamage(999,life);
        yield return Wait(()=>P(1).hp==0&&All(4,p=>p.palettes),"dead character retains its original colors");
        yield return new WaitForSecondsRealtime(1);
        ScreenCapture.CaptureScreenshot(folder+"/original-colors-death.png");yield return new WaitForSecondsRealtime(.2f);
        yield return Wait(()=>P(1).hp>0&&P(1).life>life&&All(4,p=>p.palettes),"automatic respawn preserves original character colors",12);
        var state=m.State.Value;state.End=m.Now+.1;m.State.Value=state;
        yield return Wait(()=>All(4,p=>p.phase==2&&p.palettes),"original character colors persist at results");Require(m.ReturnToRoom(),"appearance return to room");
        yield return Wait(()=>All(4,p=>p.phase==0&&p.palettes),"original character colors persist after room return",12);
    }
}
