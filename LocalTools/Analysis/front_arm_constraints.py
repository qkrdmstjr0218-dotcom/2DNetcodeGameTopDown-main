"""Fixed-length two-bone feasibility checks; limits are tunable, not anatomical proof."""
import math
import numpy as np

def signed_angle(a,b):
    return math.degrees(math.atan2(a[0]*b[1]-a[1]*b[0],float(a@b)))

def solve(shoulder, rest_elbow, rest_wrist, target, gun_delta, wrist_limit=60, elbow_limit=155):
    h,e,w,t=map(lambda v:np.asarray(v,dtype=float),(shoulder,rest_elbow,rest_wrist,target))
    l1,l2=np.linalg.norm(e-h),np.linalg.norm(w-e)
    delta=t-h;distance=np.linalg.norm(delta)
    if distance<1e-8 or distance>l1+l2+1e-6 or distance<abs(l1-l2)-1e-6:
        return {'valid':False,'reason':'reach','elbow':None,'wrist_delta':None}
    direction=delta/distance
    along=(l1*l1-l2*l2+distance*distance)/(2*distance)
    height=math.sqrt(max(0,l1*l1-along*along))
    rest_sign=np.sign((w-h)[0]*(e-h)[1]-(w-h)[1]*(e-h)[0])
    joint=h+direction*along+np.array((-direction[1],direction[0]))*height*rest_sign
    elbow_bend=abs(signed_angle(joint-h,t-joint))
    fore_delta=signed_angle(w-e,t-joint)
    wrist_delta=(gun_delta-fore_delta+180)%360-180
    valid=abs(wrist_delta)<=wrist_limit and elbow_bend<=elbow_limit
    return {'valid':bool(valid),'reason':'ok' if valid else 'wrist' if abs(wrist_delta)>wrist_limit else 'elbow',
            'elbow':joint.tolist(),'wrist_delta':float(wrist_delta),'elbow_bend':float(elbow_bend)}
