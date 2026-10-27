using System;
using System.Globalization;
using UnityEngine;
namespace RightWalkPrototype
{
    // Display and adjudication use the same tenth-point buckets. Equality is
    // transitive, unlike comparing each neighbouring pair with an epsilon.
    public static class ArenaScoreRules
    {
        public static int Units(float total)=>Mathf.FloorToInt(Mathf.Max(0,total)*10f+.5f);
        public static string Format(float total)=>(Units(total)/10f).ToString("0.0",CultureInfo.InvariantCulture);
        public static int Compare(ArenaObjectiveMatch.Score a,ArenaObjectiveMatch.Score b)
        {int score=Units(b.Total).CompareTo(Units(a.Total));return score!=0?score:a.Number.CompareTo(b.Number);}
        public static int Rank(ArenaObjectiveMatch.Score[] sorted,int index)
        {int first=index;while(first>0&&Units(sorted[first-1].Total)==Units(sorted[index].Total))first--;return first+1;}
        public static string Result(ArenaObjectiveMatch.Score[] rows)
        {
            var sorted=(ArenaObjectiveMatch.Score[])rows.Clone();Array.Sort(sorted,Compare);
            if(sorted.Length==0||Units(sorted[0].Total)==0)return "획득 점수 없음 · 무승부";
            string winners="";int count=0,best=Units(sorted[0].Total);
            foreach(var row in sorted){if(Units(row.Total)!=best)break;winners+=(count++>0?" / ":"")+"P"+row.Number;}
            return winners+(count==1?" 승리":" 공동 1위");
        }
    }
}
