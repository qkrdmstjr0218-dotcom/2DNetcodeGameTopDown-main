using UnityEngine;

namespace RightWalkPrototype
{
    // Static decoration only. Gameplay geometry stays owned by ArenaReadyRoom.
    public static class ArenaReadyRoomArt
    {
        public static void Build(Transform room,Sprite solid,Font font)
        {
            if(room.Find("Training bay interior")!=null)return;
            var root=new GameObject("Training bay interior").transform;
            root.SetParent(room,false);root.position=new Vector3(100,0,0);
            Color steel=new(.18f,.28f,.33f),edge=new(.32f,.44f,.49f),dark=new(.07f,.12f,.15f),cyan=new(.29f,.77f,.76f);
            SpriteRenderer Part(string name,Vector2 position,Vector2 size,Color color,int order,Sprite image=null)
            {
                if(image==null)image=solid;
                var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=position;
                go.transform.localScale=new Vector3(size.x/image.bounds.size.x,size.y/image.bounds.size.y,1);
                var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=image;renderer.color=color;renderer.sortingOrder=order;return renderer;
            }
            void Beam(string name,Vector2 a,Vector2 b,float width,Color color,int order)
            {
                var direction=b-a;var renderer=Part(name,(a+b)*.5f,new Vector2(direction.magnitude,width),color,order);
                renderer.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
            }
            // Replace the prototype wall rectangles with the same artwork as the arena.
            foreach(var renderer in room.GetComponentsInChildren<SpriteRenderer>())
            {
                if(renderer.name=="Wall bay"||renderer.name=="Wall vent")renderer.enabled=false;
                else if(renderer.name=="Floor"||renderer.name=="West wall"||renderer.name=="East wall"||renderer.name=="Ceiling")renderer.color=steel;
            }
            var wall=Resources.Load<Sprite>("FoundryArt/DepotWall");
            if(wall!=null)Part("Foundry wall panels",new Vector2(0,4.45f),new Vector2(20,9),new Color(.86f,.92f,.96f),-94,wall);

            // Structural rails and inset screen surround attach the guide to the wall.
            foreach(float side in new[]{-1f,1f})
            {
                Part("Screen mounting column",new Vector2(side*8.05f,4.35f),new Vector2(.25f,8.6f),dark,-89);
                Part("Column inner flange",new Vector2(side*8.16f,4.35f),new Vector2(.035f,8.6f),edge,-88);
                foreach(float y in new[]{.65f,3.1f,5.5f,8.1f})
                    Part("Column fixing",new Vector2(side*8.05f,y),new Vector2(.07f,.07f),edge,-86);
                Part("Wall inner bevel",new Vector2(side*10.02f,4.425f),new Vector2(.04f,8.8f),edge,2);
                Part("Ceiling corner plate",new Vector2(side*10.2f,8.72f),new Vector2(.4f,.26f),edge,3);
                Part("Ceiling corner bolt",new Vector2(side*10.2f,8.72f),new Vector2(.055f,.055f),dark,4);
                Part("Side light housing",new Vector2(side*9.65f,6.5f),new Vector2(.22f,1.6f),dark,-87);
                Part("Side light rim",new Vector2(side*9.65f,6.5f),new Vector2(.13f,1.38f),steel,-86);
                Part("Side light strip",new Vector2(side*9.65f,6.5f),new Vector2(.045f,1.06f),new Color(.65f,.84f,.84f),-85);
                Part("Overhead lamp housing",new Vector2(side*7.95f,8.755f),new Vector2(1.45f,.19f),dark,-86);
                Part("Overhead lamp",new Vector2(side*7.95f,8.70f),new Vector2(1.12f,.035f),new Color(.76f,.85f,.78f),-85);
            }
            Part("Guide wall recess",new Vector2(0,5.87f),new Vector2(15.5f,5.4f),new Color(.022f,.04f,.055f),-90);
            foreach(float side in new[]{-1f,1f})
            {
                Part("Guide mounting upright",new Vector2(side*7.67f,5.87f),new Vector2(.10f,5.4f),steel,-84);
                Part("Guide mount inner edge",new Vector2(side*7.61f,5.87f),new Vector2(.018f,5.12f),edge,-83);
                foreach(float y in new[]{3.36f,8.38f})
                    Part("Guide fastening bolt",new Vector2(side*7.67f,y),new Vector2(.055f,.055f),edge,-82);
            }
            Part("Guide upper mounting rail",new Vector2(0,8.53f),new Vector2(15.44f,.10f),steel,-84);
            Part("Guide lower mounting rail",new Vector2(0,3.23f),new Vector2(15.44f,.10f),steel,-84);
            var guide=room.GetComponentInChildren<ArenaReadyRoomControls>();
            if(guide!=null){guide.transform.position=new Vector3(100,.3f,0);guide.transform.localScale=new Vector3(.92f,.92f,1);}

            // One-way ledges retain the original cyan top dashes and exact collider owners.
            foreach(var collider in room.GetComponentsInChildren<BoxCollider2D>())
            {
                if(!collider.name.StartsWith("Jump ledge "))continue;
                float x=collider.transform.position.x-100,y=collider.transform.position.y;
                collider.GetComponent<SpriteRenderer>().color=new Color(.23f,.33f,.38f);
                Part("Ledge face inset",new Vector2(x,y-.015f),new Vector2(2.9f,.055f),dark,2);
                Part("Ledge lower bevel",new Vector2(x,y-.06f),new Vector2(2.94f,.018f),edge,2);
                foreach(float side in new[]{-1f,1f})
                {
                    Part("Ledge wall bracket",new Vector2(x+side*1.10f,y-.31f),new Vector2(.13f,.37f),new Color(.09f,.15f,.18f),-9);
                    Beam("Ledge diagonal brace",new Vector2(x+side*1.28f,y-.10f),new Vector2(x+side*.88f,y-.44f),.055f,new Color(.13f,.21f,.25f),-8);
                }
            }
            // All walking-floor detail remains beneath its existing top surface.
            Part("Floor service inset",new Vector2(0,-.24f),new Vector2(19.8f,.23f),dark,2);
            for(int i=-3;i<=3;i++)
            {
                float x=i*2.8f;
                Part("Floor front plate",new Vector2(x,-.25f),new Vector2(2.68f,.17f),new Color(.16f,.25f,.29f),3);
                foreach(float side in new[]{-1f,1f})
                    Part("Floor plate bolt",new Vector2(x+side*1.17f,-.25f),new Vector2(.035f,.035f),edge,4);
            }
            Part("Floor bottom bevel",new Vector2(0,-.37f),new Vector2(19.8f,.025f),edge,3);
            for(int i=1;i<=4;i++)
            {
                float x=ArenaReadyRoom.Spawn(i).x-100;
                Part("Arrival floor inset",new Vector2(x,-.14f),new Vector2(1.52f,.10f),dark,3);
                Part("Arrival floor strip",new Vector2(x,-.14f),new Vector2(1.22f,.022f),cyan,4);
                foreach(float side in new[]{-1f,1f})
                    Part("Arrival floor end cap",new Vector2(x+side*.71f,-.14f),new Vector2(.025f,.075f),edge,4);
            }

            // Re-skin the existing solid practice crate, not its collider or cover component.

            var caseArt=Resources.Load<Sprite>("FoundryArt/Case_120_240");
            foreach(Transform cover in room)
            {
                if(!cover.name.StartsWith("Rifle practice solid crate ")||caseArt==null)continue;
                var collider=cover.GetComponent<BoxCollider2D>();var scale=cover.lossyScale;var at=cover.TransformPoint(collider.offset);
                Part("Armoured practice crate",new Vector2(at.x-100,at.y),new Vector2(Mathf.Abs(collider.size.x*scale.x),Mathf.Abs(collider.size.y*scale.y)),Color.white,4,caseArt);
                cover.GetComponent<SpriteRenderer>().enabled=false;
                foreach(string name in new[]{"Crate face","Piercing mark"}){var legacy=room.Find(name);if(legacy!=null)legacy.GetComponent<SpriteRenderer>().enabled=false;}
            }
        }
    }
}
