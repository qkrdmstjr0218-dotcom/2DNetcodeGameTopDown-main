using UnityEngine;
namespace RightWalkPrototype
{
    // Decorative shared hardware. Safe to bake into the scene and reuse in ready-room clones.
    public static class ArenaFoundryLandmarkArt
    {
        static readonly Color Steel=new(.22f,.31f,.36f),Dark=new(.07f,.12f,.16f),Rim=new(.42f,.53f,.57f),Accent=new(.85f,.65f,.29f);
        static SpriteRenderer Part(Transform parent,Sprite sprite,string name,Vector2 at,Vector2 size,Color color,int order)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=at;
            go.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);
            var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=color;r.sortingOrder=order;return r;
        }
        public static void Apply(ArenaMapSettings map)
        {
            if(map==null)return;
            var consoles=map.GetComponentsInChildren<ArenaHackingConsole>(true);
            Sprite sprite=consoles.Length>0&&consoles[0].screen!=null?consoles[0].screen.sprite:null;
            if(sprite==null){var existing=map.GetComponentInChildren<SpriteRenderer>();if(existing==null)return;sprite=existing.sprite;}
            foreach(var console in map.GetComponentsInChildren<ArenaHackingConsole>(true))Console(console,sprite);
            if(map.transform.Find("Foundry spawn housings")!=null)return;
            var root=new GameObject("Foundry spawn housings").transform;root.SetParent(map.transform,false);
            foreach(var r in map.GetComponentsInChildren<SpriteRenderer>(true))
                if(r.name=="Door frame"||r.name=="Drone exit frame"||r.name=="Drone exit recess"||r.name=="Turret floor hatch"||r.name=="Turret hatch warning")r.enabled=false;
            foreach(var at in map.groundEnemySpawns)
            {
                var door=new GameObject("Armoured robot doorway").transform;door.SetParent(root,false);door.position=at;
                foreach(float side in new[]{-1f,1f})
                {
                    Part(door,sprite,"Door pillar shadow",new Vector2(side*.8f,.7f),new Vector2(.22f,1.45f),Dark,-34);
                    Part(door,sprite,"Door pillar face",new Vector2(side*.8f,.72f),new Vector2(.15f,1.39f),Steel,-33);
                    Part(door,sprite,"Door pillar bevel",new Vector2(side*.88f,.72f),new Vector2(.024f,1.3f),Rim,-32);
                    foreach(float y in new[]{.13f,1.3f})Part(door,sprite,"Door fastening",new Vector2(side*.8f,y),new Vector2(.045f,.045f),Rim,-31);
                    for(int i=0;i<3;i++){var stripe=Part(door,sprite,"Door caution insert",new Vector2(side*.8f,.37f+i*.13f),new Vector2(.13f,.045f),Accent,-31);stripe.transform.localRotation=Quaternion.Euler(0,0,-side*25);}
                }
                Part(door,sprite,"Door lintel casing",new Vector2(0,1.39f),new Vector2(1.86f,.17f),Steel,-33);
                Part(door,sprite,"Door threshold",new Vector2(0,.025f),new Vector2(1.73f,.045f),Rim,-31);
                Part(door,sprite,"Door sign backing",new Vector2(0,1.57f),new Vector2(1.85f,.22f),Dark,-59);
            }
            foreach(var at in map.turretSpawns)
            {
                var hatch=new GameObject("Reinforced turret socket").transform;hatch.SetParent(root,false);hatch.position=at;
                Part(hatch,sprite,"Socket bed",new Vector2(0,-.07f),new Vector2(1.65f,.14f),Dark,4);
                Part(hatch,sprite,"Socket lip",new Vector2(0,-.015f),new Vector2(1.52f,.03f),Rim,5);
                foreach(float side in new[]{-1f,1f})
                {
                    Part(hatch,sprite,"Socket anchor",new Vector2(side*.76f,.025f),new Vector2(.16f,.05f),Steel,6);
                    Part(hatch,sprite,"Socket caution",new Vector2(side*.6f,.015f),new Vector2(.12f,.03f),Accent,7);
                    Part(hatch,sprite,"Socket anchor bolt",new Vector2(side*.76f,.035f),new Vector2(.035f,.025f),Rim,7);
                }
            }
            foreach(var at in map.droneSpawns)
            {
                var vent=new GameObject("Armoured drone duct").transform;vent.SetParent(root,false);vent.position=at+Vector2.up*.55f;
                Part(vent,sprite,"Duct frame shadow",new Vector2(0,.15f),new Vector2(1.85f,1.45f),Dark,-43);
                Part(vent,sprite,"Duct outer rim",new Vector2(0,.15f),new Vector2(1.73f,1.33f),Rim,-42);
                Part(vent,sprite,"Duct inner rim",new Vector2(0,.15f),new Vector2(1.61f,1.21f),Steel,-41);
                Part(vent,sprite,"Duct opening",new Vector2(0,.15f),new Vector2(1.39f,.99f),Dark,-40);
                for(int i=0;i<5;i++)Part(vent,sprite,"Duct recessed louver",new Vector2(0,-.22f+i*.18f),new Vector2(1.27f,.045f),new Color(.13f,.21f,.25f),-39);
                foreach(float side in new[]{-1f,1f}){Part(vent,sprite,"Duct caution",new Vector2(side*.78f,.15f),new Vector2(.035f,.5f),Accent,-38);foreach(float y in new[]{-.4f,.7f})Part(vent,sprite,"Duct fastening",new Vector2(side*.77f,y),new Vector2(.045f,.045f),Dark,-37);}
            }
        }
        static void Console(ArenaHackingConsole console,Sprite sprite)
        {
            if(console.transform.Find("Foundry console finish")!=null)return;
            foreach(var r in console.GetComponentsInChildren<SpriteRenderer>())if(r!=console.screen&&r!=console.status&&r!=console.glow&&r.name!="Screen scan line")r.enabled=false;
            var root=new GameObject("Foundry console finish").transform;root.SetParent(console.transform,false);
            Part(root,sprite,"Console foot bevel",new Vector2(0,.07f),new Vector2(.8f,.14f),Rim,-6);
            Part(root,sprite,"Console foot face",new Vector2(0,.095f),new Vector2(.7f,.09f),Steel,-5);
            Part(root,sprite,"Console riser shadow",new Vector2(0,.38f),new Vector2(.3f,.5f),Dark,-7);
            Part(root,sprite,"Console riser face",new Vector2(0,.38f),new Vector2(.22f,.48f),Steel,-6);
            Part(root,sprite,"Console riser inlay",new Vector2(0,.37f),new Vector2(.04f,.33f),Rim,-5);
            Part(root,sprite,"Console armoured casing",new Vector2(0,.85f),new Vector2(.86f,.64f),Dark,-7);
            Part(root,sprite,"Console casing bevel",new Vector2(0,.87f),new Vector2(.81f,.59f),Rim,-6);
            Part(root,sprite,"Console fascia",new Vector2(0,.87f),new Vector2(.73f,.51f),Steel,-5);
            Part(root,sprite,"Console upper inset",new Vector2(0,1.06f),new Vector2(.57f,.025f),Dark,-4);
            foreach(float side in new[]{-1f,1f})
            {
                Part(root,sprite,"Console grip",new Vector2(side*.42f,.86f),new Vector2(.06f,.23f),Steel,-5);
                foreach(float y in new[]{.64f,1.1f})Part(root,sprite,"Console fastening",new Vector2(side*.32f,y),new Vector2(.035f,.035f),Dark,-3);
            }
            Part(root,sprite,"Console control key",new Vector2(.29f,.68f),new Vector2(.04f,.05f),Accent,-3);
        }
    }
}
