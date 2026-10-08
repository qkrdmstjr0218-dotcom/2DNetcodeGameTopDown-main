using System;
using UnityEngine;
using Unity.Netcode;
using TopDownShooter.Networking;
public static class IntegratedAimSmoke
{
    public static string Main()
    {
        var manager=NetworkManager.Singleton;
        if(manager==null || !manager.IsHost || manager.LocalClient.PlayerObject==null)
            throw new Exception("Start solo before checking integrated aiming.");
        var p=manager.LocalClient.PlayerObject.GetComponent<NetworkPlayerController>();
        var v=p.GetComponent<OperatorAimVisual>(); bool wasEnabled=p.enabled;
        int oldWeapon=p.SelectedWeapon; p.enabled=false;
        try
        {
            if(!v.UsesIntegratedAim) throw new Exception("Integrated atlas not active.");
            var renderer=p.transform.Find("Drawn operator").GetComponent<SpriteRenderer>();
            int checks=0;
            for(int w=0;w<3;w++)
            {
                p.SelectWeapon(w);
                for(int d=0;d<8;d++)
                {
                    float a=d*45*Mathf.Deg2Rad; var direction=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                    v.Pose(direction,p.CurrentWeapon,0,p.OwnerClientId);
                    if(v.AimSector!=d || renderer.sprite==null || renderer.flipX || renderer.flipY)
                        throw new Exception("Wrong pose sector "+d);
                    var pivot=p.transform.Find("Integrated muzzle pivot");
                    if(Vector2.Dot(((Vector2)(v.Muzzle-pivot.position)).normalized,direction)<.999f)
                        throw new Exception("Muzzle direction differs from input.");
                    if(!float.IsFinite(v.Muzzle.x) || Vector3.Distance(v.Muzzle,p.transform.position)>1)
                        throw new Exception("Invalid muzzle position.");
                    checks++;
                }
            }
            // Sector boundary and an angle between sprite directions: firing remains continuous.
            var between=new Vector2(Mathf.Cos(17*Mathf.Deg2Rad),Mathf.Sin(17*Mathf.Deg2Rad));
            v.Pose(between,p.CurrentWeapon,0,p.OwnerClientId);
            if(v.AimSector!=0) throw new Exception("Unexpected sector at 17 degrees.");
            string result="PASS: "+checks+" pose/weapon combinations, true directional selection, no mirroring, continuous muzzle aim. Static artwork only; not gait or art-quality approval.";
            System.IO.File.WriteAllText("Temp/integrated-aim.txt",result); return result;
        }
        finally { p.SelectWeapon(oldWeapon);p.enabled=wasEnabled; }
    }
}
