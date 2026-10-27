using System;
using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class ArenaObjectiveMatch
    {
        void DrawResults()
        {
            if(RelayArenaPrototype.Instance!=null&&RelayArenaPrototype.Instance.MenuOpen)return;
            var old=GUI.color;GUI.color=new Color(.008f,.02f,.025f,.88f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=old;
            using(ArenaUiTheme.Use())
            {
                float width=Mathf.Min(880,Screen.width-32),height=Mathf.Min(620,Screen.height-32);
                var panel=new Rect((Screen.width-width)/2,(Screen.height-height)/2,width,height);GUI.Box(panel,GUIContent.none);
                GUI.BeginGroup(panel);
                GUI.Label(new Rect(22,16,width-44,36),"경기 결과",ArenaUiTheme.Title);
                ArenaPlayerNames.Label(new Rect(22,55,width-44,32),ArenaPlayerNames.Result(this),18);
                float start=104,available=width-start-22;float[] fractions={0,.38f,.54f,.67f,.84f,1};string[] headers={"참가자","해킹 점수","처치","지원 드론 점수","총점"};
                var number=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=17};
                for(int col=0;col<5;col++)GUI.Label(new Rect(start+available*fractions[col],96,available*(fractions[col+1]-fractions[col]),26),headers[col],new GUIStyle(ArenaUiTheme.Muted){alignment=col==0?TextAnchor.MiddleLeft:TextAnchor.MiddleCenter});
                var sorted=new Score[Scores.Count];for(int i=0;i<sorted.Length;i++)sorted[i]=Scores[i];Array.Sort(sorted,ArenaScoreRules.Compare);
                for(int i=0;i<sorted.Length;i++)
                {
                    var row=sorted[i];int rank=ArenaScoreRules.Rank(sorted,i);
                    float y=130+i*76;GUI.Box(new Rect(22,y,64,64),rank.ToString(),ArenaUiTheme.Static(new GUIStyle(GUI.skin.box){alignment=TextAnchor.MiddleCenter,fontSize=30,normal={textColor=ArenaPlayerAppearance.Accent(row.Number)}}));
                    GUI.Box(new Rect(start,y,available,64),GUIContent.none);
                    var portraits=RelayArenaPrototype.Instance!=null?RelayArenaPrototype.Instance.hudPortraits:null;
                    if(portraits!=null&&row.Number>0&&row.Number<=portraits.Length)ArenaUiTheme.DrawPortrait(new Rect(start+10,y+4,44,56),portraits[row.Number-1]);
                    ArenaPlayerNames.Label(new Rect(start+62,y+8,available*.38f-66,48),"P"+row.Number+(row.Client==NetworkManager.LocalClientId?" · 나":"")+"\n"+ArenaPlayerNames.Name(row.Nickname),14);
                    string[] values={"",ArenaScoreRules.Format(row.Seconds),row.Kills.ToString(),row.DronePoints.ToString("0"),ArenaScoreRules.Format(row.Total)};
                    for(int col=1;col<5;col++)GUI.Label(new Rect(start+available*fractions[col],y+16,available*(fractions[col+1]-fractions[col]),32),values[col],number);
                }
                GUILayout.BeginArea(new Rect(22,height-103,width-44,94));GUILayout.Label("해킹 1초 = 1점   ·   플레이어 처치 +5   ·   지원 드론 격추 +5",ArenaUiTheme.Muted);
                if(IsServer){if(GUILayout.Button("대기방으로 돌아가기",GUILayout.Height(40)))ReturnToRoom();}else GUILayout.Label("방장이 대기방으로 돌아가기를 기다립니다.",ArenaUiTheme.Muted);
                GUILayout.Label("ESC · 설정",ArenaUiTheme.Muted);GUILayout.EndArea();GUI.EndGroup();
            }
        }
    }
}
