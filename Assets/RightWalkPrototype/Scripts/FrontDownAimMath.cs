using System;
using UnityEngine;

namespace RightWalkPrototype
{
    public static class FrontDownAimMath
    {
        [Serializable] public sealed class Data
        {
            public int version;
            public float[] sourceSize, atlasSize, origin, imagePivot, placementOffset, muzzle;
            public float pixelsPerUnit, minScale, maxScale, flexDegrees;
            public Head headAim;
            public Layer[] layers;
            public Arm[] arms;
            public Sample[] samples;
        }
        [Serializable] public sealed class Head { public float[] pivot, offset; public float maxDegrees, gunDegrees, response; }
        [Serializable] public sealed class Layer { public string id; public float[] bounds, atlas; }
        [Serializable] public sealed class Seam { public float along, center, halfWidth; }
        [Serializable] public sealed class Arm
        {
            public string side;
            public float[] shoulder, elbow, wrist, gripOffset;
            public int bend;
            public Seam upperSeam, foreSeam;
            public float targetHalfWidth;
        }
        [Serializable] public sealed class Sample { public float degrees; public float[] muzzle; public ArmSample[] arms; }
        [Serializable] public sealed class ArmSample { public string side; public float[] wrist, upperMatrix, foreMatrix; }
        public struct Affine
        {
            public float a,b,c,d,e,f;
            public Vector2 Apply(Vector2 p) => new Vector2(a*p.x+c*p.y+e,b*p.x+d*p.y+f);
            public static Affine Identity => new Affine {a=1,d=1};
        }
        public sealed class ArmPose { public Affine upper,fore,hand; public Vector2 wrist,joint; }
        public sealed class Pose
        {
            public Affine weapon;
            public Vector2 muzzle,direction;
            public ArmPose[] arms;
            public Pose(int count) { arms=new ArmPose[count];for(int i=0;i<count;i++)arms[i]=new ArmPose(); }
        }
        public static Vector2 V(float[] p) => new Vector2(p[0],p[1]);
        public static Vector2 Rotate(Vector2 p,float radians)
        {
            float c=Mathf.Cos(radians),s=Mathf.Sin(radians);
            return new Vector2(c*p.x-s*p.y,s*p.x+c*p.y);
        }
        public static Affine Rotation(Vector2 pivot,Vector2 translation,float angle)
        {
            float c=Mathf.Cos(angle),s=Mathf.Sin(angle);
            return new Affine {a=c,b=s,c=-s,d=c,e=pivot.x+translation.x-c*pivot.x+s*pivot.y,f=pivot.y+translation.y-s*pivot.x-c*pivot.y};
        }
        public static Affine Segment(Vector2 from,Vector2 end,Vector2 to,Vector2 target,float width=1)
        {
            Vector2 src=end-from,dst=target-to,u=src.normalized,n=new Vector2(-u.y,u.x),v=dst.normalized,z=new Vector2(-v.y,v.x);
            float ratio=dst.magnitude/src.magnitude;
            var m=new Affine {a=v.x*ratio*u.x+z.x*width*n.x,b=v.y*ratio*u.x+z.y*width*n.x,
                c=v.x*ratio*u.y+z.x*width*n.y,d=v.y*ratio*u.y+z.y*width*n.y};
            m.e=to.x-m.a*from.x-m.c*from.y;m.f=to.y-m.b*from.x-m.d*from.y;return m;
        }
        private static Vector2 Section(Vector2 origin,Vector2 axis,Seam seam)
        {
            var u=axis.normalized;return origin+u*seam.along+new Vector2(-u.y,u.x)*seam.center;
        }
        public static void Evaluate(Data data,float radians,Pose pose)
        {
            pose.weapon=Rotation(V(data.imagePivot),V(data.placementOffset),radians);
            pose.muzzle=pose.weapon.Apply(V(data.muzzle));pose.direction=Rotate(Vector2.up,radians);
            for(int i=0;i<data.arms.Length;i++)
            {
                var arm=data.arms[i];var p=pose.arms[i];Vector2 root=V(arm.shoulder),elbow=V(arm.elbow),wrist=V(arm.wrist);
                Vector2 target=pose.weapon.Apply(wrist+V(arm.gripOffset)),delta=target-root;
                float first=(elbow-root).magnitude,second=(wrist-elbow).magnitude;
                float reachAtFlex=Mathf.Sqrt(first*first+second*second+2*first*second*Mathf.Cos(data.flexDegrees*Mathf.Deg2Rad));
                float scale=Mathf.Clamp(delta.magnitude/reachAtFlex,data.minScale,data.maxScale),l1=first*scale,l2=second*scale;
                float reach=Mathf.Clamp(delta.magnitude,Mathf.Abs(l1-l2)+.00001f,l1+l2-.00001f);
                float phi=Mathf.Atan2(delta.y,delta.x),bend=Mathf.Acos(Mathf.Clamp((l1*l1+reach*reach-l2*l2)/(2*l1*reach),-1,1));
                int sign=arm.side=="support" ? -arm.bend : arm.bend;
                Vector2 joint=root+new Vector2(Mathf.Cos(phi+sign*bend),Mathf.Sin(phi+sign*bend))*l1;
                p.wrist=root+delta.normalized*reach;p.upper=Segment(root,elbow,root,joint);
                p.joint=p.upper.Apply(Section(root,elbow-root,arm.upperSeam));
                p.fore=Segment(Section(elbow,wrist-elbow,arm.foreSeam),wrist,p.joint,p.wrist,arm.targetHalfWidth/arm.foreSeam.halfWidth);
                p.hand=pose.weapon;var handOffset=Rotate(V(arm.gripOffset),radians);p.hand.e+=handOffset.x;p.hand.f+=handOffset.y;
            }
        }
        public static float RequestedAngle(Data data,Vector2 target)
        {
            var delta=target-V(data.imagePivot)-V(data.placementOffset);
            float lateral=data.muzzle[0]-data.imagePivot[0],radius=Mathf.Max(delta.magnitude,Mathf.Abs(lateral)+.00001f);
            return Mathf.Asin(Mathf.Clamp(lateral/radius,-1,1))-Mathf.Atan2(delta.x,delta.y);
        }
    }
}
