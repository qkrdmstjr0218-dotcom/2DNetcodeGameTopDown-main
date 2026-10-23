using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;
public static partial class ArenaReadyRoomReview
{
    [MenuItem("Tools/Right Walk Prototype/Verify Approved Player Palettes")]
    public static void BeginAppearance(){BeginFoundry();File.WriteAllText(folder+"/appearance.txt","appearance");}
    static bool AllPalettesVisible()
    {
        var all=Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None);
        if(all.Length!=4)return false;
        var lib=Resources.Load<ArenaPlayerPaletteLibrary>("PlayerPaletteLibrary");if(lib==null)return false;
        var block=new MaterialPropertyBlock();
        foreach(var p in all)
        {
            if(p.PlayerNumber<1||p.PlayerNumber>4||p.GetComponent<ArenaPlayerAppearance>()==null)return false;
            int count=0;
            foreach(var r in p.GetComponentsInChildren<Renderer>(true))
            {
                if(r is not SpriteRenderer && r is not MeshRenderer||r.GetComponent<TextMesh>()!=null||r.sharedMaterial==null)continue;
                r.GetPropertyBlock(block);
                var sr=r as SpriteRenderer;Texture source=sr!=null&&sr.sprite!=null?sr.sprite.texture:block.GetTexture("_MainTex")??r.sharedMaterial.mainTexture;
                var expected=lib.Variant(source,p.PlayerNumber);
                if(p.PlayerNumber==1){if(block.GetFloat("_IdentityEnabled")>.5f)return false;}
                else if(expected!=null&&r.sharedMaterial.shader.name!="RightWalk/MovingWaistUnderlay")
                {
                    if(block.GetFloat("_IdentityEnabled")<.5f||block.GetTexture("_IdentityTexture")!=expected)return false;
                    // Color is white/transparent for native fades, never a body-wide tint.
                    Color c=block.GetColor("_Color");if(c.a>0&&(c.r!=1||c.g!=1||c.b!=1))return false;
                    count++;
                }
                block.Clear();
            }
            if(p.PlayerNumber>1&&count<8)return false;
        }
        return true;
    }
    static void AppearanceCommand(SideScrollNetworkPlayer own,int action)
    {
        var aim=own.GetComponentInChildren<RightFacingAimRig>();var move=own.GetComponent<SideScrollMapPreview>();var crouch=own.GetComponent<CrouchTrial>();
        if(action==120){crouch.SetRequested(false);aim.SetAimDirection(Vector2.right);}
        if(action==121)aim.SetAimDirection(new Vector2(.4f,-1));
        if(action==122){aim.SetAimDirection(Vector2.right);crouch.SetRequested(true);}
        if(action==123){crouch.SetRequested(false);aim.SetAimDirection(Vector2.left);}
        if(action==124){crouch.SetRequested(false);aim.SetAimDirection(Vector2.right);move.SetInput(0,true);}
        if(action==125){aim.SetAimDirection(new Vector2(.3f,-1));move.SetInput(0,true);}
    }
    static IEnumerator CapturePalettePose(int command,string name,float delay=.7f)
    {
        Send(0,command);
        if(command>=124)yield return Wait(()=>Player(2)!=null&&!Player(2).GetComponent<SideScrollMapPreview>().Grounded&&Player(2).transform.position.y>.3f,"actual colored participant airborne pose "+name,8);
        else yield return new WaitForSecondsRealtime(delay);
        yield return Wait(()=>All(4,p=>p.palettes),"palette bindings survive "+name,8);
        if(command>=124)CapturePaletteCharacter(Player(2),name+"-close");
        ScreenCapture.CaptureScreenshot(folder+"/"+name+".png");yield return new WaitForSecondsRealtime(.15f);
    }
    static IEnumerator CheckAppearance()
    {
        yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4&&p.palettes),"all peers see P1 original / pastel red / slate blue / violet without uniform body tint",50);
        yield return CapturePalettePose(120,"palettes-standing");
        yield return CapturePalettePose(121,"palettes-front-down");
        yield return CapturePalettePose(120,"palettes-side-restored");
        yield return CapturePalettePose(122,"palettes-crouch");
        yield return CapturePalettePose(123,"palettes-left");
        yield return CapturePalettePose(120,"palettes-right");
        yield return CapturePalettePose(124,"palettes-jump",.22f);
        yield return CapturePalettePose(125,"palettes-air-down",.20f);
        yield return new WaitForSecondsRealtime(.9f);Send(0,120);
        yield return Wait(()=>Player(2).GetComponent<SideScrollMapPreview>().Grounded,"participant lands before alternate tile fixture",8);
        CaptureAlternateTileFixture(Player(2));
        yield return new WaitForSecondsRealtime(1);Send(0,120);
        yield return CheckAmmoPreselection();
        yield return CheckAmmoHint();
        var m=ArenaObjectiveMatch.Instance;Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"appearance peers ready");Require(m.StartRound(),"appearance match starts");
        yield return Wait(()=>All(4,p=>p.phase==1&&p.palettes),"palettes persist through transition and gameplay",20);
        // Isolate appearance/respawn from NPC attacks during the fixture.
        for(int i=0;i<m.Patrols.Count;i++){var e=m.Patrols[i];e.Health=0;e.RespawnAt=m.Now+300;m.Patrols[i]=e;}
        var map=Object.FindFirstObjectByType<ArenaMapSettings>();var balconies=map.transform.Find(FoundryEntryBuilder.RootName);
        foreach(int side in new[]{-1,1})
        {
            var deck=balconies.Find("Outer spawn balcony "+side);var bounds=deck.GetComponent<BoxCollider2D>().bounds;var art=deck.GetComponent<SpriteRenderer>().bounds;
            Require(Mathf.Abs((side<0?bounds.min.x:bounds.max.x)-side*36)<.001f&&Mathf.Abs(bounds.size.x-art.size.x)<.001f,"balcony collider and visible art reach boundary "+side);
            var hit=Physics2D.Raycast(new Vector2(side*35.8f,10.95f),Vector2.down,.3f,1);
            Require(hit.collider==deck.GetComponent<BoxCollider2D>()&&Mathf.Abs(hit.point.y-10.8f)<.001f,"former end gap has real visible support "+side);
        }
        Send(0,35);yield return new WaitForSecondsRealtime(.8f);
        var client=Player(2);uint life=client.LifeVersion;client.ReceiveServerDamage(999,life);
        yield return Wait(()=>P(2).hp==0&&All(4,p=>p.palettes),"red participant native death keeps palette");
        yield return new WaitForSecondsRealtime(1.3f);
        CapturePaletteCharacter(client,"palettes-death-close");
        ScreenCapture.CaptureScreenshot(folder+"/palettes-death.png");yield return new WaitForSecondsRealtime(.2f);
        yield return Wait(()=>P(2).hp>0&&P(2).life>life&&All(4,p=>p.palettes),"automatic respawn preserves palettes",12);
        var state=m.State.Value;state.End=m.Now+.1;m.State.Value=state;
        yield return Wait(()=>All(4,p=>p.phase==2&&p.palettes),"palettes persist at results");Require(m.ReturnToRoom(),"appearance return to room");
        yield return Wait(()=>All(4,p=>p.phase==0&&p.palettes),"palettes persist after room return",12);
    }
    static void CaptureAlternateTileFixture(SideScrollNetworkPlayer player)
    {
        // Isolate the exact reported tile, independent of gait takeoff timing.
        var renderer=player.GetComponent<SpriteRenderer>();var saved=renderer.sprite;var texture=Resources.Load<Texture2D>("JumpTransition/AlternateAir");
        var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.025f),204.8f);
        var mesh=player.GetComponent<RightFacingPelvisPreview>();
        player.GetComponentInChildren<RightFacingAimRig>().SetAimDirection(Vector2.right);player.GetComponent<FrontDownAimOverlay>().Deactivate();
        bool suppressed=mesh.SuppressDisplay;mesh.SuppressDisplay=false;
        try{renderer.sprite=sprite;mesh.UpdateGeometry();player.GetComponent<ArenaPlayerAppearance>().RefreshPalette();var b=new MaterialPropertyBlock();renderer.GetPropertyBlock(b);Require(b.GetTexture("_IdentityTexture")==Resources.Load<ArenaPlayerPaletteLibrary>("PlayerPaletteLibrary").Variant(texture,player.PlayerNumber),"reported AlternateAir source has correct palette");CapturePaletteCharacter(player,"palettes-alternate-jump-close");}
        finally{renderer.sprite=saved;mesh.SuppressDisplay=suppressed;mesh.UpdateGeometry();player.GetComponent<ArenaPlayerAppearance>().RefreshPalette();Object.DestroyImmediate(sprite);}
    }
    static void CapturePaletteCharacter(SideScrollNetworkPlayer player,string name)
    {
        var objects=player.GetComponentsInChildren<Transform>(true);
        var layers=new int[objects.Length];for(int i=0;i<objects.Length;i++){layers[i]=objects[i].gameObject.layer;objects[i].gameObject.layer=30;}
        var go=new GameObject("Palette inspection camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(Camera.main);
        camera.cullingMask=1<<30;camera.orthographic=true;camera.orthographicSize=1.6f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.07f,.1f,.13f,1);
        camera.transform.position=player.transform.position+new Vector3(-.4f,.65f,-10);camera.transform.rotation=Quaternion.identity;
        var rt=new RenderTexture(1024,768,24);var previous=RenderTexture.active;var texture=new Texture2D(1024,768,TextureFormat.RGBA32,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1024,768),0,0);texture.Apply();File.WriteAllBytes(folder+"/"+name+".png",texture.EncodeToPNG());}
        finally{RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(texture);Object.DestroyImmediate(rt);Object.DestroyImmediate(go);for(int i=0;i<objects.Length;i++)if(objects[i]!=null)objects[i].gameObject.layer=layers[i];}
    }
}
