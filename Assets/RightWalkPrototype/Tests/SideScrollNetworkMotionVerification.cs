#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;

namespace RightWalkPrototype
{
    // Opt-in two-process integration check; never runs during normal play.
    public static class SideScrollNetworkMotionVerification
    {
        static readonly string[] Names={"idle","walk","run","crouch-forward","crouch-backward","crouch-stop","ground-down","moving-down","single-jump","double-jump","air-down-turn","rest"};
        public static IEnumerator Run(SideScrollNetworkSession session)
        {
            var manager=NetworkManager.Singleton;
            string folder="Temp/NetworkMotion";
            foreach(var arg in Environment.GetCommandLineArgs())if(arg.StartsWith("--motion-output="))folder=arg.Substring(16);
            Directory.CreateDirectory(folder);
            bool host=Array.IndexOf(Environment.GetCommandLineArgs(),"--side-host")>=0;
            string role=host?"host":"client",result=Path.Combine(folder,role+"-result.txt");
            string startFile=Path.Combine(folder,"start.txt");
            var checks=new HashSet<int>();var log=new List<string>();
            float deadline=Time.realtimeSinceStartup+95;
            double start=-1;int lastStage=-1;bool jumpedAgain=false;
            int shotBaseline=0;float nextShot=0;bool sawFirst=false,firstAlternate=false,lateJoined=false;
            int previousRemoteJump=-1;
            int firstCrouchFrame=-1;bool crouchAdvanced=false;
            while(Time.realtimeSinceStartup<deadline)
            {
                var players=UnityEngine.Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None);
                SideScrollNetworkPlayer local=null,remote=null;
                foreach(var p in players){if(p.IsOwner)local=p;else remote=p;}
                if(players.Length!=2||local==null||remote==null){yield return null;continue;}
                if(start<0)
                {
                    if(host){start=manager.ServerTime.Time+2;File.WriteAllText(startFile,start.ToString("R",System.Globalization.CultureInfo.InvariantCulture));}
                    else if(File.Exists(startFile))start=double.Parse(File.ReadAllText(startFile),System.Globalization.CultureInfo.InvariantCulture);
                    else{yield return null;continue;}
                }
                double elapsed=manager.ServerTime.Time-start;
                if(elapsed<0){yield return null;continue;}
                int stage=(int)(elapsed/2);float t=(float)(elapsed-stage*2);
                var move=local.GetComponent<SideScrollMapPreview>();var crouch=local.GetComponent<CrouchTrial>();
                var aim=local.GetComponentInChildren<RightFacingAimRig>();var shot=aim.GetComponent<RightFacingShotPreview>();
                float sign=host?1:-1;
                if(stage!=lastStage)
                {
                    lastStage=stage;jumpedAgain=false;
                    if(stage==7)sawFirst=false;
                    if(stage<Names.Length)
                    {
                        var body=local.GetComponent<Rigidbody2D>();body.position=new Vector2(host?-1.5f:1.5f,0);body.linearVelocity=Vector2.zero;
                        move.SetInput(0);crouch.SetRequested(false);shot.readMouse=false;
                        if(stage>=8)local.GetComponent<RightFacingWalker>().ResumeFromPlantedPose(0);
                        local.GetComponent<SideFacingPreview>().SetFacing((int)sign);aim.SetAimDirection(Vector2.right*sign);
                        shotBaseline=remote.ReceivedShots;nextShot=0;
                    }
                }
                if(stage<Names.Length)
                {
                    bool down=stage==6||stage==7||stage==10;
                    if(stage!=10)aim.SetAimDirection(down?Vector2.down:Vector2.right*sign);
                    if(stage==1||stage==2)move.SetInput(sign,false,stage==2);
                    if(stage>=3&&stage<=5){crouch.SetRequested(true);move.SetInput(stage==5?0:stage==3?sign:-sign);}
                    if(stage==7)move.SetInput(sign);
                    if(stage>=8&&stage<=10)
                    {
                        // Let physics settle after the stage's teleport before testing a ground jump.
                        if(t>.15f&&t<.3f&&move.Grounded&&move.JumpCount==0)move.SetInput(0,true);
                        if(stage!=8&&t>.4f&&t<.75f&&!jumpedAgain&&!move.Grounded&&move.JumpCount==1){move.SetInput(0,true);jumpedAgain=true;}
                        if(stage==10)aim.SetAimDirection(t<.45f?Vector2.down:new Vector2(-sign*.3f,-1).normalized);
                    }
                    if((stage==6||stage==7||stage==10)&&t>.35f&&t<1.1f&&Time.time>=nextShot)
                    {shot.Fire();nextShot=Time.time+.22f;}
                }
                else
                {
                    if(checks.Count!=Names.Length||!crouchAdvanced)
                    {File.WriteAllText(result,"FAIL: missing stages "+string.Join(",",Missing(checks))+"; crouchAdvanced="+crouchAdvanced+"\n"+string.Join("\n",log));Application.Quit(2);yield break;}
                    // Reconnect while the host stays crouched, exercising initial-state replay.
                    if(host){crouch.SetRequested(true);move.SetInput(0);aim.SetAimDirection(Vector2.right);}
                    if(!host&&!lateJoined)
                    {
                        lateJoined=true;manager.Shutdown();
                        yield return new WaitForSecondsRealtime(1);
                        session.StartLocal(false);continue;
                    }
                    if(elapsed>Names.Length*2+3&&remote.GetComponent<CrouchTrial>().IsCrouched&&!host)
                    {
                        yield return new WaitForEndOfFrame();Capture(folder,role+"-latejoin",remote);
                        File.WriteAllText(Path.Combine(folder,"latejoin.txt"),"PASS: newly connected client displays crouched host");
                    }
                    if(File.Exists(Path.Combine(folder,"latejoin.txt"))&&elapsed>Names.Length*2+4)
                    {
                        File.WriteAllText(result,"PASS: "+string.Join(", ",Names)+"; crouch phase advanced; late join while crouched; remote input isolated\n"+string.Join("\n",log));
                        yield return new WaitForSecondsRealtime(1);Application.Quit(0);yield break;
                    }
                    yield return null;continue;
                }
                yield return new WaitForEndOfFrame();
                var rm=remote.GetComponent<SideScrollMapPreview>();var rc=remote.GetComponent<CrouchTrial>();
                var rw=remote.GetComponent<RightFacingWalker>();var ra=remote.GetComponentInChildren<RightFacingAimRig>();
                var front=remote.GetComponent<FrontDownAimOverlay>();var air=remote.GetComponent<AirDownShotPose>();
                bool isolated=!rm.enabled&&!rm.readKeyboard&&!rw.enabled&&!rc.readKeyboard&&!ra.mouseAim&&!remote.GetComponent<Rigidbody2D>().simulated;
                if(!isolated){File.WriteAllText(result,"FAIL: remote input/physics isolation");Application.Quit(2);yield break;}
                if(stage==3&&rc.IsCrouched)
                {if(firstCrouchFrame<0)firstCrouchFrame=rc.FrameIndex;else if(firstCrouchFrame!=rc.FrameIndex)crouchAdvanced=true;}
                if(rm.JumpCount!=previousRemoteJump)
                {previousRemoteJump=rm.JumpCount;log.Add("remote jump="+rm.JumpCount+" stage="+stage+" t="+t.ToString("F2")+" alternate="+air.UseAlternatePose+" takeoff="+air.TakeoffFrame);}
                bool passed=false;
                switch(stage)
                {
                    case 0:passed=rm.Grounded&&!rc.IsCrouched&&!front.IsActive;break;
                    case 1:passed=rm.Grounded&&!rc.IsCrouched&&Mathf.Abs(rw.Velocity.x)>.6f&&!remote.GetComponent<RunWalkPreview>().IsRunning;break;
                    case 2:passed=remote.GetComponent<RunWalkPreview>().IsRunning&&Mathf.Abs(rw.Velocity.x)>2;break;
                    case 3:case 4:passed=rc.IsCrouched&&rc.MotionAmount>.9f&&Mathf.Abs(rw.Velocity.x)>.5f&&!front.IsActive;break;
                    case 5:passed=rc.IsCrouched&&rc.MotionAmount<.05f&&Mathf.Abs(rw.Velocity.x)<.01f;break;
                    case 6:passed=!rc.IsCrouched&&rm.Grounded&&front.IsActive&&!front.MovingPoseActive&&remote.ReceivedShots>shotBaseline;break;
                    case 7:passed=rm.Grounded&&front.MovingPoseActive&&remote.ReceivedShots>shotBaseline;break;
                    case 8:passed=!rm.Grounded&&rm.JumpCount==1;break;
                    case 9:passed=!rm.Grounded&&rm.JumpCount==2&&sawFirst&&air.UseAlternatePose!=firstAlternate;break;
                    case 10:passed=!rm.Grounded&&rm.JumpCount==2&&front.AirbornePoseActive&&remote.ReceivedShots>shotBaseline&&t>.55f;break;
                    case 11:passed=rm.Grounded&&!rc.IsCrouched&&!front.IsActive;break;
                }
                if(passed&&t>(stage==8||stage==9?.25f:.5f)&&t<1.5f&&checks.Add(stage))
                {
                    if(stage==8){sawFirst=true;firstAlternate=air.UseAlternatePose;}
                    log.Add(Names[stage]+": frame="+rw.FrameIndex+" jump="+rm.JumpCount+" crouch="+rc.IsCrouched+" front="+front.IsActive+" shots="+remote.ReceivedShots);
                    Capture(folder,role+"-"+Names[stage]+"-remote",remote);
                    Capture(folder,role+"-"+Names[stage]+"-local",local);
                }
            }
            File.WriteAllText(result,"FAIL: timeout, checks="+string.Join(",",checks));Application.Quit(2);
        }
        static IEnumerable<string> Missing(HashSet<int> checks)
        {for(int i=0;i<Names.Length;i++)if(!checks.Contains(i))yield return Names[i];}
        static void Capture(string folder,string name,SideScrollNetworkPlayer player)
        {
            var camera=Camera.main;var p=camera.transform.position;var size=camera.orthographicSize;
            var old=camera.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(640,560,24);
            try
            {
                camera.orthographicSize=1.5f;camera.transform.position=new Vector3(player.transform.position.x+.15f,player.transform.position.y+1.32f,-10);
                camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                var image=new Texture2D(640,560,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,640,560),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());UnityEngine.Object.Destroy(image);
            }
            finally{camera.targetTexture=old;camera.transform.position=p;camera.orthographicSize=size;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);}
        }
    }
}
#endif
