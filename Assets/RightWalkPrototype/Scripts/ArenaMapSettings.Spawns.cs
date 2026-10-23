using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class ArenaMapSettings
    {
        static readonly Collider2D[] spawnOverlaps=new Collider2D[64];
        public bool SpawnCandidateValid(Vector2 at)
        {
            if(!ContainsCombatPosition(at)||RelayArenaPrototype.Instance!=null&&!RelayArenaPrototype.Instance.IsSafeLanding(at))return false;
            if(!ArenaEnemyNavigation.FloorBelow(at+Vector2.up*.15f,.45f,out var floor)||Mathf.Abs(floor.point.y-(at.y-.04f))>.15f)return false;
            int count=Physics2D.OverlapBoxNonAlloc(at+Vector2.up*1.1f,new Vector2(.65f,2),0,spawnOverlaps,1);
            if(count>=spawnOverlaps.Length)return false;
            for(int i=0;i<count;i++)
            {
                var c=spawnOverlaps[i];
                if(!c.enabled||c.isTrigger||c.GetComponentInParent<SideScrollNetworkPlayer>()!=null||c.GetComponentInParent<ArenaPatrolEnemy>()!=null||c.GetComponentInParent<SideScrollDamageTarget>()!=null)continue;
                if(c.usedByEffector&&c.GetComponent<ArenaCover>()==null)continue;
                return false;
            }
            return true;
        }
        public float SpawnSafetyScore(Vector2 at,SideScrollNetworkPlayer requester)
        {
            if(!SpawnCandidateValid(at))return float.NegativeInfinity;
            Vector2 centre=at+Vector2.up*1.2f;float nearest=36,penalty=0;
            foreach(var other in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))
            {
                if(other==requester||!other.IsSpawned||other.IsDead)continue;
                Vector2 target=other.ArenaHitBounds.center;float d=Vector2.Distance(centre,target);nearest=Mathf.Min(nearest,d);
                if(d<8)penalty+=100+(8-d)*10;
                if(d<=SideScrollNetworkPlayer.RifleRange&&!ArenaGrenadeBallistics.WorldTrace(centre,target,0,out _))penalty+=30;
            }
            var match=ArenaObjectiveMatch.Instance;
            if(match!=null)foreach(var e in match.Patrols)
            {
                if(e.Health<=0||e.Medical)continue;Vector2 target=match.PatrolPosition(e)+Vector3.up*.6f;float d=Vector2.Distance(centre,target);
                if(d<6)penalty+=(6-d)*10;
                if(d<10&&!ArenaGrenadeBallistics.WorldTrace(centre,target,0,out _))penalty+=15;
            }
            return nearest-penalty;
        }
        public bool TryChooseRespawn(SideScrollNetworkPlayer player,out Vector3 spawn)
        {
            spawn=default;if(respawnPoints==null||respawnPoints.Length==0)return false;
            int first=Random.Range(0,respawnPoints.Length);float best=float.NegativeInfinity;bool found=false;
            for(int n=0;n<respawnPoints.Length;n++)
            {
                Vector2 at=respawnPoints[(first+n)%respawnPoints.Length];float score=SpawnSafetyScore(at,player);
                if(score<=best)continue;best=score;spawn=at;found=true;
            }
            return found;
        }
    }
}
