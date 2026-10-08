using System;
using System.Reflection;
using UnityEngine;
using Unity.Netcode;
using TopDownShooter.Networking;
public static class ControlledLegSmoke
{
    public static string Main()
    {
        var p=NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayerController>();
        var v=p.GetComponent<OperatorAimVisual>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        bool enabled=p.enabled;p.enabled=false;
        var right=p.transform.Find("Right leg target");var left=p.transform.Find("Left leg target");
        if(right==null || left==null)throw new Exception("Leg rig missing.");
        Vector3[] r=new Vector3[4],l=new Vector3[4];
        try
        {
            for(int i=0;i<4;i++)
            {
                typeof(OperatorAimVisual).GetField("previousPosition",flags).SetValue(v,p.transform.position-Vector3.right*.01f);
                typeof(OperatorAimVisual).GetField("rightWalkPhase",flags).SetValue(v,i*.25f-.01f/2.2f);
                v.Pose(Vector2.right,p.CurrentWeapon,5,p.OwnerClientId);
                r[i]=p.transform.InverseTransformPoint(right.position);l[i]=p.transform.InverseTransformPoint(left.position);
            }
            if(!(r[0].x>l[0].x && r[2].x<l[2].x))throw new Exception("Feet did not exchange front/back positions.");
            if(!(l[1].y>r[1].y+.04f && r[3].y>l[3].y+.04f))throw new Exception("Opposite feet did not lift.");
            typeof(OperatorAimVisual).GetField("previousPosition",flags).SetValue(v,p.transform.position+Vector3.right*.02f);
            typeof(OperatorAimVisual).GetField("rightWalkPhase",flags).SetValue(v,.25f);
            v.Pose(Vector2.right,p.CurrentWeapon,5,p.OwnerClientId);
            if((float)typeof(OperatorAimVisual).GetField("rightWalkPhase",flags).GetValue(v)>=.25f)throw new Exception("Retreat did not reverse leg trajectory.");
            typeof(OperatorAimVisual).GetField("previousPosition",flags).SetValue(v,p.transform.position);
            v.Pose(Vector2.right,p.CurrentWeapon,0,p.OwnerClientId);
            if(v.AnimationRow!=0 || p.transform.Find("Right leg upper art").GetComponent<SpriteRenderer>().enabled)throw new Exception("Idle did not restore whole standing pose.");
            return "PASS: same two feet exchange front/back, alternate raised foot, reverse on retreat, idle restores original art. Does not certify art fidelity.";
        }
        finally{p.enabled=enabled;}
    }
}
