using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class ArenaReadyRoomControls
    {
        public static readonly string[] Topics={"조작 가이드","경기 규칙","무기 · 보급","위치 안내"};
        Transform contentRoot;
        readonly GameObject[] pages=new GameObject[4];
        readonly ArenaGuideTarget[] targets=new ArenaGuideTarget[4];
        TextMesh heading;
        int revision=-1;
        public int DisplayedTopic{get;private set;}
        void BuildTopic(int i)
        {
            pages[i]=new GameObject("Guide page "+i);pages[i].transform.SetParent(transform,false);contentRoot=pages[i].transform;
            if(i==0)DrawControls();
            else if(i==1)
            {
                Text("해킹 구역을 혼자 점유하면 초당 1점",-7.1f,7.25f,.063f,Ink);
                Text("2명 이상이면 득점 중단 · 구역은 경기 중 이동합니다",-7.1f,6.5f,.05f,Muted);
                Text("플레이어 처치 +5점 · 지원 드론 격추 +5점",-7.1f,5.75f,.055f,Accent);
                Text("시간 종료 시 최고 점수 승리 · 사망하면 자동 부활",-7.1f,5f,.05f,Muted);
                Text("시작 · 부활 후 2초 보호 / 사격하면 보호 해제",-7.1f,4.55f,.046f,Muted);
            }
            else if(i==2)
            {
                Text("1 샷건    2 관통 라이플    3 유탄",-7.1f,7.25f,.065f,Ink);
                Text("Q/E · 휠로 선택 / 우클릭을 누르는 동안 특수탄 사용",-7.1f,6.5f,.048f,Muted);
                Text("일반탄 12발 · 사격을 멈춘 뒤 1초 후 초당 4발 충전",-7.1f,5.75f,.048f,Accent);
                Text("특수탄은 보급 · 적 드롭으로 보충합니다",-7.1f,5f,.05f,Muted);
                Text("지원 드론 격추 → 보호막 투하 / 최대 50 · 중첩 없음",-7.1f,4.55f,.046f,Muted);
            }
            else
            {
                Text("해킹 구역 · 청록색 단말 마크",-7.1f,7.25f,.065f,Accent);
                Text("현재 점령할 위치 · 밑의 막대는 이동까지 남은 시간",-7.1f,6.5f,.048f,Muted);
                Text("지원 드론 · 파란 방패 마크",-7.1f,5.75f,.065f,new Color(.55f,.75f,1));
                Text("보호막을 투하하는 비무장 보급 기체의 위치",-7.1f,5f,.05f,Muted);
                Text("Tab · 전장 현황에서 전체 구역 위치 확인",-7.1f,4.55f,.046f,Muted);
            }
            contentRoot=null;
        }
        void BuildTargets()
        {
            for(int i=0;i<4;i++)
            {
                var root=new GameObject("Guide target "+i);root.transform.SetParent(transform,false);root.transform.localPosition=new Vector3(-4.8f+i*3.2f,10.2f,0);contentRoot=root.transform;
                Panel("Target frame",0,0,3f,.66f,.07f,Steel,-64);Panel("Target face",0,0,2.92f,.58f,.05f,Face,-63);
                var label=Text(Topics[i],-.12f,0,.042f,Ink,true);label.GetComponent<MeshRenderer>().sortingOrder=-60;Box("Target light",1.26f,0,.07f,.30f,Accent,-62);
                targets[i]=root.AddComponent<ArenaGuideTarget>();targets[i].Initialize(i,root.GetComponentInChildren<SpriteRenderer>());contentRoot=null;
            }
            Box("Ceiling target rail",0,9.9f,12.8f,.07f,Steel,-64);
        }
        void Update()
        {
            var match=ArenaObjectiveMatch.Instance;int next=match!=null&&match.IsSpawned?match.GuideRevision:0;int topic=next&3;
            bool changed=revision!=next;
            if(changed)
            {
                bool sound=revision>=0&&ArenaObjectiveMatch.PracticeRoom;revision=next;DisplayedTopic=topic;
                heading.text=Topics[topic];for(int i=0;i<pages.Length;i++)pages[i].SetActive(i==topic);
                if(sound)ArenaAudio.World(ArenaAudio.Cue.UiClick,transform.position+Vector3.up*4,GetInstanceID());
            }
            for(int i=0;i<targets.Length;i++)targets[i].Show(i==topic,changed&&i==topic);
        }
    }
}
