#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RightWalkPrototype
{
    // Opt-in integration check: real rigidbody, current character, original movement.
    public static class RelayArenaVerification
    {
        private static string folder;
        private static List<string> passed;
        public static IEnumerator Run(string output,bool downhillOnly=false)
        {
            folder=output;Directory.CreateDirectory(folder);passed=new List<string>();
            var stack=new Stack<IEnumerator>();stack.Push(Check(downhillOnly));
            while(stack.Count>0)
            {
                bool more=false;object next=null;Exception failure=null;
                try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}
                catch(Exception error){failure=error;}
                if(failure!=null){File.WriteAllText(folder+"/result.txt","FAIL: "+failure+"\n"+string.Join("\n",passed));yield break;}
                if(!more){stack.Pop();continue;}
                if(next is IEnumerator nested)stack.Push(nested);else yield return next;
            }
            File.WriteAllText(folder+"/result.txt","PASS\n"+string.Join("\n",passed));
        }
        private static IEnumerator Until(Func<bool> condition,string label,float seconds=12)
        {
            float deadline=Time.realtimeSinceStartup+seconds;
            while(!condition()&&Time.realtimeSinceStartup<deadline)yield return null;
            if(!condition())throw new Exception(label);
            passed.Add(label);
            File.WriteAllLines(folder+"/progress.txt",passed);
        }
        private static IEnumerator Traverse(SideScrollNetworkPlayer player,Vector2 start,float end,int direction,float expectedY)
        {
            var move=player.GetComponent<SideScrollMapPreview>();
            move.SetSpawnPosition(start);move.SetInput(0);yield return new WaitForSeconds(.4f);
            // Choosing an ascending ramp now requires a jump onto its upper surface.
            move.SetInput(direction,expectedY>start.y+.5f,true);
            var samples=new List<string>{"time,x,y,vx,vy,grounded,support,jumps,gravity"};
            try { yield return Until(()=>{
                var body=player.GetComponent<Rigidbody2D>();
                samples.Add(Time.time+","+player.transform.position.x+","+player.transform.position.y+","+body.linearVelocity.x+","+body.linearVelocity.y+","+move.Grounded+","+(move.GroundCollider!=null?move.GroundCollider.name:"none")+","+move.JumpCount+","+body.gravityScale);
                return direction>0?player.transform.position.x>=end:player.transform.position.x<=end;
            },"ramp travel "+start+" -> "+end,14); }
            finally { File.WriteAllLines(folder+"/ramp-"+start.x+".csv",samples); }
            move.SetInput(0);yield return new WaitForSeconds(.4f);
            if(player.transform.position.y<expectedY-.35f||!move.Grounded)throw new Exception("Ramp height/grounded: "+player.transform.position+"; jump blocked="+move.JumpInputBlocked+"; count="+move.JumpCount);
            passed.Add("ramp height and grounded "+start);
            if(expectedY>start.y+.5f)
            {
                var before=player.transform.position;
                yield return new WaitForSeconds(1.2f);
                if(Vector2.Distance(before,player.transform.position)>.015f)throw new Exception("Slope idle drift "+(player.transform.position-before));
                passed.Add("slope idle stable "+start);
            }
        }
        private static IEnumerator Check(bool downhillOnly)
        {
            SideScrollNetworkPlayer own=null;
            yield return Until(()=>{foreach(var p in UnityEngine.Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))if(p.IsOwner){own=p;return true;}return false;},"owner spawned");
            var rules=UnityEngine.Object.FindFirstObjectByType<RelayArenaPrototype>();
            var move=own.GetComponent<SideScrollMapPreview>();var body=own.GetComponent<Rigidbody2D>();
            var aim=own.GetComponentInChildren<RightFacingAimRig>();
            aim.mouseAim=false;aim.GetComponent<RightFacingShotPreview>().readMouse=false;
            if(move.followCamera!=null||Mathf.Abs(aim.previewAimRange-24)>.01f)throw new Exception("Arena camera/range setup");
            if(Mathf.Abs(move.speed-rules.walkSpeed)>.01f||Mathf.Abs(move.jumpSpeed-rules.jumpSpeed)>.01f)throw new Exception("Arena movement tuning");
            if(downhillOnly)
            {
                yield return Traverse(own,new Vector2(-2.5f,6.43f),-12.3f,-1,3.05f);
                yield return Traverse(own,new Vector2(2.5f,6.43f),12.3f,1,3.05f);
                yield break;
            }
            var posture=own.GetComponent<CrouchTrial>();
            move.SetSpawnPosition(new Vector2(-20.5f,.03f));move.SetInput(0);yield return new WaitForSeconds(.5f);
            posture.SetRequested(true);yield return Until(()=>posture.IsCrouched,"crouch beneath ramp");
            posture.SetRequested(false);yield return Until(()=>!posture.IsCrouched,"stand beneath ramp without leaving it");
            move.SetSpawnPosition(new Vector2(3.6f,1.25f));move.SetInput(0);yield return new WaitForSeconds(.5f);
            posture.SetRequested(true);yield return Until(()=>posture.IsCrouched,"crouch on cover beneath deck");
            posture.SetRequested(false);yield return Until(()=>!posture.IsCrouched,"stand on cover beneath central grate");
            move.SetInput(0,true);yield return Until(()=>move.JumpCount==1&&!move.Grounded,"cover jump started");
            yield return Until(()=>body.linearVelocity.y<.5f,"cover jump apex");move.SetInput(0,true);
            yield return Until(()=>own.transform.position.y>3.25f,"double jump upward through mezzanine");
            yield return Until(()=>move.Grounded,"land above mezzanine");
            if(own.transform.position.y<3)throw new Exception("Did not land on mezzanine");
            ScreenCapture.CaptureScreenshot(folder+"/one-way-deck.png");yield return new WaitForSeconds(.3f);
            move.SetSpawnPosition(new Vector2(0,.03f));move.SetInput(0);yield return new WaitForSeconds(.5f);
            posture.SetRequested(true);yield return Until(()=>posture.IsCrouched,"solid ceiling crouch setup");
            var ceiling=new GameObject("Review solid ceiling");ceiling.transform.position=new Vector3(0,1.85f,0);
            ceiling.AddComponent<BoxCollider2D>().size=new Vector2(2,.1f);Physics2D.SyncTransforms();
            posture.SetRequested(false);yield return new WaitForSeconds(.3f);
            if(!posture.IsCrouched)throw new Exception("Solid ceiling allowed standing");
            UnityEngine.Object.Destroy(ceiling);yield return Until(()=>!posture.IsCrouched,"solid ceiling removal permits standing");
            // A shallow ramp may have less than a character-height gap below it.
            // Ignoring the selected one-way surface makes that a valid short drop.
            foreach(float x in new[]{19f,-19f})
            {
                move.SetSpawnPosition(new Vector2(x,2.5f));move.SetInput(0);
                yield return Until(()=>move.Grounded&&move.GroundCollider!=null&&move.GroundCollider.name==(x>0?"East lower ramp":"West lower ramp"),"short drop slope setup "+x);
                posture.SetRequested(true);yield return Until(()=>posture.IsCrouched,"short drop crouch "+x);
                move.SetInput(0,true);
                yield return Until(()=>move.Grounded&&own.transform.position.y<.1f,"short ramp drop reaches ground "+x);
                if(own.IsDead)throw new Exception("Short ramp drop killed player");
                posture.SetRequested(false);yield return Until(()=>!posture.IsCrouched,"short drop standing "+x);
                // Jump on the first available fixed tick, without waiting out
                // the old 1.5 second deliberate-drop timeout.
                move.SetInput(0,true);
                float reentryDeadline=Time.time+1.45f;
                yield return Until(()=>move.Grounded&&move.GroundCollider!=null&&move.GroundCollider.name==(x>0?"East lower ramp":"West lower ramp"),"immediate ramp reentry after drop "+x,1.45f);
                if(Time.time>reentryDeadline+.05f)throw new Exception("Ramp reentry waited for drop timeout");
            }
            // Covers block weapons while allowing bodies through their sides.
            move.SetSpawnPosition(new Vector2(-10,3.23f));move.SetInput(0);yield return new WaitForSeconds(.4f);
            move.SetInput(1,false,true);yield return Until(()=>own.transform.position.x>-6.5f,"walk through middle cover");
            move.SetInput(0);if(own.transform.position.y>3.4f)throw new Exception("Cover auto-mounted from side");
            move.SetSpawnPosition(new Vector2(-8,4.5f));move.SetInput(0);
            yield return Until(()=>move.Grounded&&move.GroundCollider!=null&&move.GroundCollider.GetComponent<ArenaCover>()!=null,"land on pass-through cover");
            aim.TraceShot(new Vector2(-10,3.5f),Vector2.right,4,out Collider2D coverHit,true);
            if(coverHit==null||coverHit.GetComponent<ArenaCover>()==null)throw new Exception("Cover failed rightward shot block");
            aim.TraceShot(new Vector2(-6,3.5f),Vector2.left,4,out coverHit,true);
            if(coverHit==null||coverHit.GetComponent<ArenaCover>()==null)throw new Exception("Cover failed leftward shot block");
            aim.TraceShot(new Vector2(7,2.5f),Vector2.up,2,out coverHit,true);
            if(coverHit==null||coverHit.name!="East mezzanine")throw new Exception("Solid mezzanine failed shot block");
            passed.Add("covers block both shot directions; solid deck blocks shots");
            posture.SetRequested(true);yield return Until(()=>posture.IsCrouched,"cover drop crouch");move.SetInput(0,true);
            yield return Until(()=>move.Grounded&&move.GroundCollider!=null&&move.GroundCollider.name=="West mezzanine","drop from cover to solid deck");
            posture.SetRequested(false);yield return Until(()=>!posture.IsCrouched,"stand after cover drop");
            ScreenCapture.CaptureScreenshot(folder+"/cover-and-surface-types.png");yield return new WaitForSeconds(.3f);
            // Walk underneath the east ramp without jumping: do not auto-mount it.
            move.SetSpawnPosition(new Vector2(22.55f,.03f));move.SetInput(0);yield return new WaitForSeconds(.4f);move.SetInput(-1,false,true);
            yield return Until(()=>own.transform.position.x<18.5f,"ramp underpass horizontal travel");
            move.SetInput(0);yield return new WaitForSeconds(.4f);
            if(Mathf.Abs(own.transform.position.y+.155f)>.12f)throw new Exception("Ramp auto-mounted from below");
            passed.Add("ramp underpass stays on ground");
            // At a far boundary, aim and rendering must use exactly the same camera.
            var probe=own.gameObject.AddComponent<ArenaCameraVerificationProbe>();
            aim.SetAimTarget((Vector2)own.transform.position+new Vector2(-8,1.5f));
            yield return new WaitForSeconds(1.5f);
            Vector3 steady=rules.arenaCamera.transform.position;Vector2 direction=aim.AimDirection;
            float endTime=Time.time+1;
            while(Time.time<endTime)
            {
                if(Vector3.Distance(probe.aimFramePosition,rules.arenaCamera.transform.position)>.0001f||Vector3.Distance(steady,rules.arenaCamera.transform.position)>.005f||Vector2.Distance(direction,aim.AimDirection)>.002f)
                    throw new Exception("Camera/aim frame jitter");
                yield return null;
            }
            UnityEngine.Object.Destroy(probe);passed.Add("stationary camera/aim stable at far boundary");
            rules.SetMenuOpen(true);Vector3 menuPosition=own.transform.position;move.SetInput(1,true,true);
            yield return new WaitForSeconds(.7f);
            if(Cursor.lockState!=CursorLockMode.None||Vector2.Distance(menuPosition,own.transform.position)>.02f||!RelayArenaPrototype.ControlsBlocked)
                throw new Exception("Menu cursor/input gate");
            ScreenCapture.CaptureScreenshot(folder+"/escape-menu.png");yield return new WaitForSeconds(.3f);
            rules.SetMenuOpen(false);move.SetInput(0);passed.Add("menu releases cursor and blocks own controls");
            yield return Traverse(own,new Vector2(-21.8f,.03f),-12.5f,1,3.05f);
            move.SetSpawnPosition(new Vector2(-17.5f,2));move.SetInput(0);
            yield return Until(()=>move.Grounded&&move.GroundCollider!=null&&move.GroundCollider.name=="West lower ramp","slope idle setup");
            yield return new WaitForSeconds(.4f);
            Vector3 slopePosition=own.transform.position;yield return new WaitForSeconds(1.5f);
            if(Vector3.Distance(slopePosition,own.transform.position)>.015f)throw new Exception("Idle slope sliding");
            passed.Add("actual incline idle has no sliding");
            yield return Traverse(own,new Vector2(-11.8f,3.23f),-3.5f,1,6.25f);
            yield return Traverse(own,new Vector2(21.8f,.03f),12.5f,-1,3.05f);
            yield return Traverse(own,new Vector2(11.8f,3.23f),3.5f,-1,6.25f);
            yield return Traverse(own,new Vector2(-2.5f,6.43f),-12.3f,-1,3.05f);
            yield return Traverse(own,new Vector2(-12.7f,3.23f),-22.1f,-1,-.15f);
            move.SetSpawnPosition(new Vector2(1,.03f));move.SetInput(0);yield return new WaitForSeconds(.4f);
            move.SetInput(0,true);
            yield return Until(()=>move.JumpCount==1&&!move.Grounded&&own.transform.position.y>.3f,"flat ground jump");
            yield return Until(()=>body.linearVelocity.y<.5f,"first jump apex");
            move.SetInput(0,true);
            yield return Until(()=>move.JumpCount==2&&own.transform.position.y>1.15f,"double jump");
            yield return Until(()=>body.linearVelocity.y<.5f,"second jump apex");
            float velocity=body.linearVelocity.y;move.SetInput(0,true);yield return new WaitForFixedUpdate();yield return null;
            if(body.linearVelocity.y>velocity+.3f||move.JumpCount!=2)throw new Exception("Third jump accepted");
            passed.Add("third jump rejected");
            yield return Until(()=>move.Grounded,"jump landing");
            // A controlled fall checks the grated centre route independently of ramps.
            move.SetSpawnPosition(new Vector2(1,3.6f));move.SetInput(0);
            yield return new WaitForSeconds(.4f);
            yield return Until(()=>move.Grounded,"grate landing");
            if(Mathf.Abs(own.transform.position.y-3.045f)>.2f)throw new Exception("Grate landing height "+own.transform.position);
            var crouch=own.GetComponent<CrouchTrial>();crouch.SetRequested(true);
            yield return Until(()=>crouch.IsCrouched,"crouch before platform drop");
            move.SetInput(0,true);
            yield return Until(()=>move.Grounded&&own.transform.position.y<.1f,"crouch jump drops to safe lower floor");
            if(own.IsDead)throw new Exception("Safe platform drop killed owner");
            crouch.SetRequested(false);yield return new WaitForSeconds(.3f);
            move.SetSpawnPosition(new Vector2(1,3.6f));move.SetInput(0);yield return new WaitForSeconds(.5f);
            move.SetSpawnPosition(new Vector2(1,2.5f));move.SetInput(0);body.linearVelocity=Vector2.up*5;
            yield return Until(()=>own.transform.position.y>3.3f,"grate upward pass");
            yield return Until(()=>move.Grounded,"grate relanding");
            var end=aim.TraceShot(new Vector2(1,2),Vector2.up,2,out Collider2D hit,true);
            if(hit!=null||Vector2.Distance(end,new Vector2(1,4))>.05f)throw new Exception("Grate blocked bullet");
            passed.Add("grate bullet pass in upward direction");
            aim.TraceShot(new Vector2(-6,.6f),Vector2.right,4,out hit,true);
            if(hit==null||!hit.name.Contains("Ground west cover"))throw new Exception("Solid cover failed to block bullet");
            passed.Add("solid cover blocks bullet");
            yield return Until(()=>rules.CycleTime>1&&rules.CycleTime<7,"bridge closed phase",18);
            move.SetSpawnPosition(new Vector2(14,.03f));move.SetInput(0);yield return new WaitForSeconds(.8f);
            if(!move.Grounded||own.IsDead||own.transform.position.y<-.3f)throw new Exception("Closed bridge unsupported");
            passed.Add("closed bridge supports character");
            rules.overview=true;yield return null;Capture("overview");
            yield return Until(()=>rules.CycleTime>10.6f&&rules.CycleTime<12,"bridge open phase",18);
            move.SetSpawnPosition(new Vector2(14,.03f));move.SetInput(0);
            yield return Until(()=>own.IsDead&&own.GetComponent<CharacterDeathTrial>().IsDead,"pit fall server death");
            yield return new WaitForSeconds(.8f);own.RequestRevive();
            yield return Until(()=>!own.IsDead&&!own.GetComponent<CharacterDeathTrial>().IsDead&&move.enabled,"pit revive and controls");
            var session=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>();
            Vector3 spawn=session.playerSpawns[own.PlayerNumber-1].position;
            if(Mathf.Abs(own.transform.position.x-spawn.x)>.2f)throw new Exception("Revive did not use arena spawn");
            move.SetSpawnPosition(spawn);move.SetInput(0);move.readKeyboard=true;aim.mouseAim=true;aim.GetComponent<RightFacingShotPreview>().readMouse=true;
            rules.overview=false;yield return new WaitForSeconds(1);Capture("play-view");
        }
        private static void Capture(string name)
        {
            var camera=Camera.main;var previous=camera.targetTexture;var active=RenderTexture.active;
            var target=new RenderTexture(1600,900,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();
            File.WriteAllBytes(folder+"/"+name+".png",texture.EncodeToPNG());
            camera.targetTexture=previous;RenderTexture.active=active;target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(texture);
        }
    }
    [DefaultExecutionOrder(150)]
    public sealed class ArenaCameraVerificationProbe : MonoBehaviour
    {
        public Vector3 aimFramePosition;
        private void LateUpdate(){aimFramePosition=Camera.main.transform.position;}
    }
}
#endif
