using System.Globalization;
using System.Text;
using Unity.Collections;
using UnityEngine;
namespace RightWalkPrototype
{
    public static class ArenaPlayerNames
    {
        public const string PreferenceKey="RightWalk.Nickname";
        public static string Normalize(string value)
        {
            var result=new StringBuilder();int count=0;
            var elements=StringInfo.GetTextElementEnumerator((value??"").Trim());
            while(elements.MoveNext()&&count<12)
            {
                string element=elements.GetTextElement();bool allowed=true;
                foreach(char c in element)if(char.IsControl(c)||char.GetUnicodeCategory(c)==UnicodeCategory.Format||c=='<'||c=='>')allowed=false;
                if(!allowed)continue;
                if(Encoding.UTF8.GetByteCount(result.ToString()+element)>60)break;
                result.Append(element);count++;
            }
            return result.ToString().Trim();
        }
        public static string Local=>Name(new FixedString64Bytes(Normalize(PlayerPrefs.GetString(PreferenceKey,"플레이어"))));
        public static string Name(FixedString64Bytes value)=>value.IsEmpty?"플레이어":value.ToString();
        public static void Save(string value){PlayerPrefs.SetString(PreferenceKey,Name(new FixedString64Bytes(Normalize(value))));PlayerPrefs.Save();}
        public static void Label(Rect rect,string value,int fontSize,bool centred=false)
        {
            var style=ArenaUiTheme.Static(new GUIStyle(GUI.skin.label){fontSize=fontSize,richText=false,alignment=centred?TextAnchor.MiddleCenter:TextAnchor.MiddleLeft});
            foreach(string line in value.Split('\n'))while(style.fontSize>8&&style.CalcSize(new GUIContent(line)).x>rect.width)style.fontSize--;
            GUI.Label(rect,new GUIContent(value,value),style);
        }
        public static string Result(ArenaObjectiveMatch match)
        {
            var rows=new ArenaObjectiveMatch.Score[match.Scores.Count];for(int i=0;i<rows.Length;i++)rows[i]=match.Scores[i];
            string original=match.Result.Value.ToString();
            if(original!=ArenaScoreRules.Result(rows))return original;
            System.Array.Sort(rows,ArenaScoreRules.Compare);
            if(rows.Length==0||ArenaScoreRules.Units(rows[0].Total)==0)return original;
            var names=new StringBuilder();int count=0,best=ArenaScoreRules.Units(rows[0].Total);
            foreach(var row in rows){if(ArenaScoreRules.Units(row.Total)!=best)break;if(count++>0)names.Append(" / ");names.Append("P").Append(row.Number).Append(' ').Append(Name(row.Nickname));}
            return names+(count==1?" 승리":" 공동 1위");
        }
    }
}
