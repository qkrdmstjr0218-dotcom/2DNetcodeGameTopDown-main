using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
namespace RightWalkPrototype
{
    // Local-only teaching course; uses the real player, weapons and NGO damage.
    public sealed partial class ArenaTutorial : MonoBehaviour
    {
        public static ArenaTutorial Instance {get;private set;}
        public static bool Active=>Instance!=null&&!Instance.ending;
        public SideScrollNetworkSession Session {get;private set;}
        public const int LessonCount=7;
        public int Lesson {get;private set;}
        public bool LessonComplete {get;private set;}
        public Vector3 Checkpoint {get;private set;}=new Vector3(200,.04f,0);
        public SideScrollDamageTarget Target {get;private set;}
        public int CompletedCount {get{int n=0;foreach(bool b in completed)if(b)n++;return n;}}
        readonly bool[] completed=new bool[LessonCount];
        SideScrollNetworkPlayer player;
        GameObject geometry;
        Sprite sprite;
        Rigidbody2D dummy;
        bool ending,initialized,walked,sprinted,doubleJump,crouched,normalSpent,selfHurt,healed,pushed,normalHit,rifleHit,grenadeHit;
        int grenadeStart;
        float nextHeal,noticeUntil;string notice="";
        int pendingLesson=-1,pendingExit;
        float targetResetAt;
        public static void Begin(SideScrollNetworkSession session)
        {
            if(Active)return;
            var t=session.gameObject.AddComponent<ArenaTutorial>();Instance=t;t.Session=session;t.BuildLesson();
        }
        void Update()
        {
            if(ending||NetworkManager.Singleton==null||!NetworkManager.Singleton.IsServer)return;
            if(pendingExit!=0){if(pendingExit==2)Session.TutorialToPractice();else Session.DisconnectLocal();return;}
            if(pendingLesson>=0){int next=pendingLesson;pendingLesson=-1;EnterLesson(next);return;}
            if(player==null)foreach(var p in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))if(p.IsSpawned&&p.IsOwner){player=p;break;}
            if(player==null)return;
            if(!initialized){initialized=true;EnterLesson(0);return;}
            if(RelayArenaPrototype.ControlsBlocked)return;
            if(targetResetAt>0&&Time.time>=targetResetAt&&Target!=null)
            {targetResetAt=0;Target.ResetTarget();normalHit=rifleHit=grenadeHit=false;}
            if(player.transform.position.y< -2.5f||player.transform.position.x<193||player.transform.position.x>218)
            {EnterLesson(Lesson);notice="구간 시작점으로 돌아왔습니다. 다시 도전해 보세요.";noticeUntil=Time.unscaledTime+3;return;}
            if(Lesson>=LessonCount)return;
            var move=player.GetComponent<SideScrollMapPreview>();float x=player.transform.position.x,y=player.transform.position.y;
            if(Lesson==0){walked|=x>=204;sprinted|=walked&&player.Stamina<95;if(walked&&sprinted&&x>=210)Complete();}
            if(Lesson==1){doubleJump|=move.JumpCount>=2;if(doubleJump&&move.Grounded&&move.GroundCollider==goalLedge)Complete();}
            if(Lesson==2){crouched|=player.GetComponent<CrouchTrial>().IsCrouched&&move.GroundCollider==goalLedge;if(crouched&&dropped&&move.Grounded&&y<.6f)Complete();}
            if(Lesson==3){normalSpent|=player.NormalRounds<player.NormalCapacity;if(normalHit&&normalSpent&&Target!=null&&Target.Health.Value==0&&player.NormalRounds==player.NormalCapacity)Complete();}
            if(Lesson==4&&Target!=null)
            {
                if(pushed&&Target.transform.position.y< -2)Complete();
            }
            if(Lesson==5&&rifleHit&&Target!=null&&Target.Health.Value==0)Complete();
            if(Lesson==6)
            {
                selfHurt|=player.Health<player.MaxHealth&&player.ReceivedGrenadeBursts>grenadeStart;
                if(selfHurt&&Mathf.Abs(x-198)<.85f&&move.Grounded&&y<.6f&&Time.time>=nextHeal)
                {nextHeal=Time.time+.75f;if(player.GrantHealthServer(25,player.LifeVersion)>0)healed=true;}
                if(grenadeHit&&Target!=null&&Target.Health.Value==0&&selfHurt&&healed&&player.Health==player.MaxHealth)Complete();
            }
        }
        void Complete(){LessonComplete=true;completed[Lesson]=true;}
        public void NextLesson(){if(LessonComplete&&Lesson<LessonCount)EnterLesson(Lesson+1);}
        public void SkipLesson(){if(Lesson<LessonCount)EnterLesson(Lesson+1);}
        public void RetryLesson()=>EnterLesson(Lesson);
        public void EnterLesson(int lesson)
        {
            if(ending||lesson<0||lesson>LessonCount)return;
            Lesson=lesson;LessonComplete=false;
            walked=sprinted=doubleJump=crouched=normalSpent=selfHurt=healed=pushed=normalHit=rifleHit=grenadeHit=dropped=false;nextHeal=0;targetResetAt=0;
            Checkpoint=new Vector3(lesson==2?208.5f:lesson==4?203:200,lesson==2?3.24f:.04f,0);
            ClearLesson();BuildLesson();
            var m=ArenaObjectiveMatch.Instance;
            if(m!=null&&m.IsSpawned&&m.IsServer){var s=m.State.Value;s.Version++;m.State.Value=s;}
            if(player==null)return;
            player.GetComponent<ArenaPlatformMotor>()?.RefreshPlatforms();
            player.ResetForArenaRound();player.SetSpecialHeld(false);
            var aim=player.GetComponentInChildren<RightFacingAimRig>();bool mouseAim=aim.mouseAim;
            aim.SetAimDirection(Vector2.right);aim.mouseAim=mouseAim;
            grenadeStart=player.ReceivedGrenadeBursts;
            if(lesson==3)MakeTarget(new Vector2(207,1),3);
            if(lesson==4){MakeTarget(new Vector2(208,1),20);if(Target!=null){var image=Target.GetComponent<SpriteRenderer>();var shape=Target.GetComponent<BoxCollider2D>();shape.offset=image.sprite.bounds.center;shape.size=image.sprite.bounds.size;dummy=Target.gameObject.AddComponent<Rigidbody2D>();dummy.constraints=RigidbodyConstraints2D.FreezeRotation;dummy.collisionDetectionMode=CollisionDetectionMode2D.Continuous;}}
            if(lesson==5)MakeTarget(new Vector2(208,1),3);
            if(lesson==6)MakeTarget(new Vector2(207,1),3);
        }
        void MakeTarget(Vector2 at,int hp)
        {
            var prefab=Session.tutorialTargetPrefab!=null?Session.tutorialTargetPrefab:Session.targetPrefab;
            if(prefab==null){notice="연습 표적이 연결되지 않았습니다. 로비로 돌아가 설정을 확인해 주세요.";noticeUntil=float.PositiveInfinity;return;}
            var go=Instantiate(prefab,at,Quaternion.identity);go.name="Tutorial practice target";
            foreach(var r in go.GetComponentsInChildren<SpriteRenderer>())r.sortingOrder=10;
            Target=go.GetComponent<SideScrollDamageTarget>();Target.maxHealth=hp;Target.Damaged+=TargetDamaged;go.GetComponent<NetworkObject>().Spawn();
        }
        void TargetDamaged(SideScrollNetworkPlayer source,SideScrollNetworkPlayer.AmmoKind kind)
        {
            if(source!=player||Target==null)return;
            normalHit=kind==SideScrollNetworkPlayer.AmmoKind.Normal;
            rifleHit=kind==SideScrollNetworkPlayer.AmmoKind.Rifle;
            grenadeHit=kind==SideScrollNetworkPlayer.AmmoKind.Grenade;
            if(Target.Health.Value==0&&((Lesson==3&&!normalHit)||(Lesson==5&&!rifleHit)||(Lesson==6&&!grenadeHit)))
            {
                targetResetAt=Time.time+1;
                notice=(Lesson==3?"일반탄":Lesson==5?"관통 라이플":"유탄")+"으로 표적을 파괴해 보세요. 표적을 다시 준비합니다.";
                noticeUntil=Time.unscaledTime+4;
            }
            if(Lesson==4&&kind==SideScrollNetworkPlayer.AmmoKind.InstantShotgun&&dummy!=null)
            {pushed=true;dummy.linearVelocity=new Vector2(Mathf.Sign(Target.transform.position.x-player.transform.position.x)*SideScrollNetworkPlayer.ShotgunPush,1);}
        }
        bool dropped;
        public void RecordDrop(Collider2D support){if(Lesson==2&&support==goalLedge)dropped=true;}
        void ClearLesson()
        {
            if(Target!=null){Target.Damaged-=TargetDamaged;var n=Target.GetComponent<NetworkObject>();if(n.IsSpawned&&NetworkManager.Singleton!=null&&NetworkManager.Singleton.IsServer)n.Despawn(true);else Destroy(Target.gameObject);Target=null;dummy=null;}
            if(geometry!=null){geometry.SetActive(false);Destroy(geometry);geometry=null;}
            if(sprite!=null){Destroy(sprite);sprite=null;}
        }
        public void End()
        {
            if(ending)return;ending=true;ClearLesson();if(Instance==this)Instance=null;Destroy(this);
        }
        void OnDestroy(){ClearLesson();if(Instance==this)Instance=null;}
    }
}
