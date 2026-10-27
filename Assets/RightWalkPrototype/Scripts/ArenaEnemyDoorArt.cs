using System.Collections.Generic;
using UnityEngine;
namespace RightWalkPrototype
{
    // Shared match timestamps drive decorative shutters; no new collision or RPC.
    [DefaultExecutionOrder(170)]
    public sealed class ArenaEnemyDoorArt:MonoBehaviour
    {
        sealed class Door{public Vector2 Origin;public Transform Left,Right;public SpriteRenderer Lamp;}
        readonly List<Door> doors=new();
        readonly List<SpriteRenderer> hidden=new();
        Sprite unit;
        public static void Build(ArenaMapSettings map)
        {
            if(map==null||!map.enemyEntrances||map.groundEnemySpawns==null||map.GetComponentInChildren<ArenaEnemyDoorArt>()!=null)return;
            var go=new GameObject("Animated robot entrances");go.transform.SetParent(map.transform,false);
            go.AddComponent<ArenaEnemyDoorArt>().Create(map);
        }
        SpriteRenderer Part(Transform parent,string name,Vector2 at,Vector2 size,Color color,int order,bool masked=false)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=at;go.transform.localScale=size;
            var r=go.AddComponent<SpriteRenderer>();r.sprite=unit;r.color=color;r.sortingOrder=order;
            if(masked)r.maskInteraction=SpriteMaskInteraction.VisibleInsideMask;
            return r;
        }
        void Create(ArenaMapSettings map)
        {
            unit=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);
            foreach(var r in map.GetComponentsInChildren<SpriteRenderer>())
                if(r.name=="Ground enemy door"||r.name=="Door seam"){hidden.Add(r);r.enabled=false;}
            foreach(var at in map.groundEnemySpawns)
            {
                var root=new GameObject("Robot sliding door").transform;root.SetParent(transform,false);root.position=at;
                Part(root,"Dark doorway",new Vector2(0,.65f),new Vector2(1.42f,1.3f),new Color(.025f,.045f,.055f),-40);
                var maskObject=new GameObject("Door opening mask");maskObject.transform.SetParent(root,false);maskObject.transform.localPosition=new Vector2(0,.65f);maskObject.transform.localScale=new Vector3(1.4f,1.3f,1);
                var mask=maskObject.AddComponent<SpriteMask>();mask.sprite=unit;mask.isCustomRangeActive=true;mask.backSortingOrder=-39;mask.frontSortingOrder=-34;
                Transform Leaf(int side)
                {
                    var leaf=new GameObject(side<0?"Left shutter":"Right shutter").transform;leaf.SetParent(root,false);
                    Part(leaf,"Shutter panel",new Vector2(0,.65f),new Vector2(.7f,1.28f),new Color(.15f,.23f,.28f),-37,true);
                    foreach(float y in new[]{.18f,.44f,.82f,1.08f})Part(leaf,"Shutter rib",new Vector2(0,y),new Vector2(.59f,.018f),new Color(.22f,.32f,.37f),-36,true);
                    Part(leaf,"Shutter inner edge",new Vector2(-side*.33f,.65f),new Vector2(.025f,1.21f),new Color(.42f,.49f,.48f),-36,true);
                    Part(leaf,"Shutter handle",new Vector2(-side*.22f,.61f),new Vector2(.045f,.21f),new Color(.075f,.12f,.15f),-36,true);
                    Part(leaf,"Shutter upper bevel",new Vector2(0,1.23f),new Vector2(.61f,.025f),new Color(.38f,.48f,.52f),-35,true);
                    Part(leaf,"Shutter recessed foot",new Vector2(0,.085f),new Vector2(.61f,.08f),new Color(.08f,.14f,.18f),-35,true);
                    foreach(float x in new[]{-.25f,.25f})foreach(float y in new[]{.16f,1.14f})Part(leaf,"Shutter rivet",new Vector2(x,y),new Vector2(.03f,.03f),new Color(.4f,.49f,.51f),-35,true);
                    return leaf;
                }
                var door=new Door{Origin=at,Left=Leaf(-1),Right=Leaf(1)};
                Part(root,"Door lintel",new Vector2(0,1.3f),new Vector2(1.52f,.07f),new Color(.22f,.32f,.37f),-33);
                door.Lamp=Part(root,"Door status light",new Vector2(0,1.32f),new Vector2(.35f,.035f),new Color(.22f,.4f,.4f),-32);
                doors.Add(door);Pose(door,0,false,0);
            }
        }
        public static float Opening(float progress)
        {
            if(progress<.4f)return Mathf.SmoothStep(0,1,Mathf.InverseLerp(.15f,.4f,progress));
            return progress<=1?1:1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1,1.25f,progress));
        }
        void Pose(Door door,float opening,bool active,double now)
        {
            door.Left.localPosition=Vector2.left*(.35f+.72f*opening);door.Right.localPosition=Vector2.right*(.35f+.72f*opening);
            door.Lamp.color=active?Color.Lerp(new Color(1,.55f,.16f),new Color(1,.86f,.4f),Mathf.PingPong((float)now*4,1)):new Color(.22f,.4f,.4f);
        }
        void Update()
        {
            if(ArenaObjectiveMatch.Ended)return;
            var match=ArenaObjectiveMatch.Instance;
            foreach(var door in doors)
            {
                float open=0;bool active=false;double now=match!=null?match.Now:0;
                if(match!=null&&match.IsSpawned&&match.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing)
                    foreach(var state in match.Patrols)
                    {
                        if(state.Medical||state.Flying||state.Turret||state.EntrySeconds<=0||Mathf.Abs(state.Left-door.Origin.x)>.1f||Mathf.Abs(state.Floor-door.Origin.y)>.1f)continue;
                        float progress=ArenaObjectiveMatch.EntryProgress(state,now);
                        if(progress>=0&&progress<=1.25f){open=Mathf.Max(open,Opening(progress));active=true;}
                    }
                Pose(door,open,active,now);
            }
        }
        void OnDestroy(){foreach(var r in hidden)if(r!=null)r.enabled=true;if(unit!=null)Destroy(unit);}
    }
}
