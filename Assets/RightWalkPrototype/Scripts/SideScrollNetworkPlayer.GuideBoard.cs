using System.Collections;
using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class SideScrollNetworkPlayer
    {
        int GuideHit(Vector2 start,Vector2 direction,float range,out float distance,bool piercing=false)
        {
            distance=range;if(!piercing||!IsServer||IsBot||!ArenaObjectiveMatch.PracticeRoom)return -1;
            int topic=-1;
            foreach(var target in FindObjectsByType<ArenaGuideTarget>(FindObjectsSortMode.None))
            {
                if(!target.isActiveAndEnabled||!target.HitBounds.IntersectRay(new Ray(start,direction),out float hit)||hit<0||hit>=distance)continue;
                distance=hit;topic=target.Topic;
            }
            return topic;
        }
        IEnumerator ApplyGuideImpact(int topic,float delay)
        {
            var match=ArenaObjectiveMatch.Instance;int version=match!=null?match.State.Value.Version:-1;
            if(delay>0)yield return new WaitForSeconds(delay);
            if(IsSpawned&&IsServer&&match!=null&&match.IsSpawned&&match.State.Value.Version==version)match.SetGuideTopicServer(topic);
        }
    }
}
