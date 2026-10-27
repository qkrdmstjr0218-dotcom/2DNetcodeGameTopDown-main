using UnityEngine;
namespace RightWalkPrototype
{
    // Ready-room preview only. Never registered in ArenaObjectiveLayout or awarded score.
    public sealed class ArenaReadyRoomHacking : MonoBehaviour
    {
        ArenaObjectivePoint point;TextMesh status;ArenaHackingConsole console;
        public int Occupants{get;private set;}
        public static void Build(Transform room,Sprite sprite,Font font)
        {
            var root=new GameObject("Ready room hacking practice");root.transform.SetParent(room,false);root.transform.position=new Vector3(100,0,0);
            var preview=root.AddComponent<ArenaReadyRoomHacking>();preview.point=root.AddComponent<ArenaObjectivePoint>();
            SpriteRenderer Part(string name,Vector2 position,Vector2 size,Color color,int order)
            {
                var go=new GameObject(name);go.transform.SetParent(root.transform,false);go.transform.localPosition=position;go.transform.localScale=size;
                var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=color;r.sortingOrder=order;return r;
            }
            var area=Part("Zone fill",new Vector2(0,1.05f),new Vector2(4,2.1f),new Color(.2f,.8f,.8f,.07f),-12);
            preview.point.area=new[]{area};preview.point.halfWidth=2;
            var layout=FindFirstObjectByType<ArenaObjectiveLayout>();ArenaHackingConsole template=null;
            if(layout!=null&&layout.points!=null)foreach(var site in layout.points)if(site!=null&&site.console!=null){template=site.console;break;}
            if(template!=null)
            {
                preview.console=Instantiate(template,root.transform,false);preview.console.name="Practice console - shared map design";
                preview.console.transform.localPosition=Vector3.zero;preview.console.transform.localRotation=Quaternion.identity;
                preview.console.transform.localScale=template.transform.lossyScale;
            }
            else
            {
                preview.console=root.AddComponent<ArenaHackingConsole>();
                Part("Practice terminal case",new Vector2(0,.29f),new Vector2(.38f,.58f),new Color(.18f,.28f,.32f),-8);
                preview.console.screen=Part("Practice terminal screen",new Vector2(0,.34f),new Vector2(.26f,.25f),Color.cyan,-7);
                preview.console.status=Part("Practice terminal status",new Vector2(0,.12f),new Vector2(.18f,.035f),Color.cyan,-7);
                preview.console.glow=Part("Practice terminal glow",new Vector2(0,.29f),new Vector2(.58f,.7f),Color.clear,-9);
            }
            var label=new GameObject("Hacking practice status");label.transform.SetParent(root.transform,false);label.transform.localPosition=new Vector3(0,1.91f,0);
            preview.status=label.AddComponent<TextMesh>();preview.status.font=font;preview.status.fontSize=64;preview.status.characterSize=.032f;preview.status.anchor=TextAnchor.MiddleCenter;
            var renderer=label.GetComponent<MeshRenderer>();renderer.sharedMaterial=font.material;renderer.sortingOrder=-5;
        }
        void Update()
        {
            bool visible=ArenaObjectiveMatch.PracticeRoom;Occupants=0;
            if(visible)foreach(var player in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))if(point.Contains(player))Occupants++;
            var color=Occupants==1?new Color(.3f,.95f,.62f):Occupants>1?new Color(1,.65f,.2f):new Color(.25f,.85f,.82f);
            point.Show(visible,false,color);console.Show(visible,false,color);status.gameObject.SetActive(visible&&Occupants==0);
            if(visible){status.text=Occupants==1?"단독 점유 · 연습 중 (득점 없음)":Occupants>1?"경합 · 실제 경기에서는 득점 중단":"해킹 연습 · 구역 안으로 이동";status.color=color;}
        }
    }
}
