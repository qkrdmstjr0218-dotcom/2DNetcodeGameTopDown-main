using UnityEngine;
namespace RightWalkPrototype
{
    // Ready-room preview only. Never registered in ArenaObjectiveLayout or awarded score.
    public sealed class ArenaReadyRoomHacking : MonoBehaviour
    {
        ArenaHackingConsole console;
        public int Occupants{get;private set;}
        public static void Build(Transform room,Sprite sprite,Font font)
        {
            var root=new GameObject("Ready room hacking practice");root.transform.SetParent(room,false);root.transform.position=new Vector3(100,0,0);
            var preview=root.AddComponent<ArenaReadyRoomHacking>();
            SpriteRenderer Part(string name,Vector2 position,Vector2 size,Color color,int order)
            {
                var go=new GameObject(name);go.transform.SetParent(root.transform,false);go.transform.localPosition=position;go.transform.localScale=size;
                var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=color;r.sortingOrder=order;return r;
            }

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

        }
        void Update()
        {
            Occupants=0;console.Show(ArenaObjectiveMatch.PracticeRoom,false,new Color(.25f,.85f,.82f));
        }
    }
}
