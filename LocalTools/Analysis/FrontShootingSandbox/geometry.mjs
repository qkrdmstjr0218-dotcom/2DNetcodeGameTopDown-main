export const clamp=(v,a,b)=>Math.max(a,Math.min(b,v));
export const add=(a,b)=>[a[0]+b[0],a[1]+b[1]];
export const sub=(a,b)=>[a[0]-b[0],a[1]-b[1]];
export const length=a=>Math.hypot(...a);
export function rotate(p,angle){const c=Math.cos(angle),s=Math.sin(angle);return[c*p[0]-s*p[1],s*p[0]+c*p[1]];}
export const around=(point,pivot,angle)=>add(pivot,rotate(sub(point,pivot),angle));
export const direction=(a,b)=>Math.atan2(b[1]-a[1],b[0]-a[0]);
export function segmentHitsCircle(a,b,center,radius){
  const v=sub(b,a),den=v[0]*v[0]+v[1]*v[1];
  const t=den?clamp(((center[0]-a[0])*v[0]+(center[1]-a[1])*v[1])/den,0,1):0;
  return Math.hypot(a[0]+v[0]*t-center[0],a[1]+v[1]*t-center[1])<=radius;
}
export function airOffset(degrees){
  const keys=[[45,0,0],[60,.10,0],[75,.225,.06],[90,.45,.10]];
  if(degrees<=45)return[0,0];if(degrees>=90)return[.45,.10];
  const i=keys.findIndex((k,n)=>n<keys.length-1&&degrees<=keys[n+1][0]);
  const a=keys[i],b=keys[i+1],t=clamp((degrees-a[0])/(b[0]-a[0]),0,1),s=t*t*(3-2*t);
  return[a[1]+(b[1]-a[1])*s,a[2]+(b[2]-a[2])*s];
}
export function aimPivotOffset(angle,rig){
  const limit=35*Math.PI/180,a=clamp(angle,-limit,limit),travel=clamp(rig.aimFollow?.travel??24,0,80);
  // Follow aim direction within a small body-space arc, never cursor distance.
  return [-Math.sin(a)/Math.sin(limit)*travel,(Math.cos(a)-1)/(1-Math.cos(limit))*travel*.25];
}
export function poseAt(angle,rig,mode='ground-extension'){
  const raised=mode==='raised-body'||mode==='aim-follow';
  const imagePivot=mode==='legacy'?rig.legacyPivot:raised?add(rig.pivot,[0,-(rig.raisedPivot?.rise??96)]):rig.pivot;
  let translation=[...(rig.placementOffset??[0,0])];
  if(mode==='aim-follow')translation=add(translation,aimPivotOffset(angle,rig));
  if(mode==='air-existing'){
    const offset=airOffset(90-Math.abs(angle)*180/Math.PI),rest=airOffset(90);
    const scale=rig.trajectory.previewPixelsPerUnit,side=angle<=0?1:-1;
    translation=add(translation,[(offset[0]-rest[0])*scale*side,-(offset[1]-rest[1])*scale]);
  }
  const pivot=add(imagePivot,translation);
  const muzzle=add(around(rig.muzzle,imagePivot,angle),translation);
  return {angle,imagePivot,pivot,translation,muzzle,ray:rotate([0,1],angle)};
}
export const imagePoint=(point,pose)=>add(around(point,pose.imagePivot,pose.angle),pose.translation);
export function weaponSweep(rig,mode='raised-body'){
  const poses=Array.from({length:71},(_,i)=>poseAt((i-35)*Math.PI/180,rig,mode));
  return {pivot:poses[35].pivot,stock:poses.map(p=>imagePoint(rig.stockRear,p)),muzzle:poses.map(p=>p.muzzle)};
}
export const gripOffset=(arm,rig)=>rig.gripOffsets?.[arm.side]??[0,0];
export const wristTarget=(arm,pose,rig)=>imagePoint(add(arm.wrist,gripOffset(arm,rig)),pose);
export const handPlacement=(arm,pose,rig)=>add(pose.pivot,rotate(gripOffset(arm,rig),pose.angle));
export function projectArm(arm,pose,rig,enabled=true){
  const keys=rig.supportProjection?.keys;
  if(!enabled||arm.side!=='support'||!keys?.length)return arm;
  const degrees=clamp(Math.abs(pose.angle)*180/Math.PI,keys[0].degrees,keys.at(-1).degrees);
  const i=Math.max(0,keys.findIndex((key,n)=>n<keys.length-1&&degrees<=keys[n+1].degrees));
  const a=keys[i],b=keys[i+1]??a,t=a===b?0:(degrees-a.degrees)/(b.degrees-a.degrees),s=t*t*(3-2*t);
  const offset=a.shoulderOffset.map((v,n)=>v+(b.shoulderOffset[n]-v)*s);
  const foreScale=a.foreScale+(b.foreScale-a.foreScale)*s;
  const shoulder=add(arm.shoulder,offset),elbow=add(arm.elbow,offset);
  const wrist=add(elbow,sub(arm.wrist,arm.elbow).map(v=>v*foreScale));
  return {...arm,shoulder,elbow,wrist,foreScale};
}
export function torsoPose(pose,rig,enabled=true){
  const config=rig.torsoLean;
  if(!config||!enabled)return {angle:0,pivot:config?.pivot??[519,650],blendStartY:560,blendEndY:700};
  const degrees=clamp(Math.abs(pose.angle)*180/Math.PI,config.keys[0][0],config.keys.at(-1)[0]);
  const i=Math.max(0,config.keys.findIndex((key,n)=>n<config.keys.length-1&&degrees<=config.keys[n+1][0]));
  const a=config.keys[i],b=config.keys[i+1]??a,t=a===b?0:(degrees-a[0])/(b[0]-a[0]),s=t*t*(3-2*t);
  return {...config,angle:(a[1]+(b[1]-a[1])*s)*Math.PI/180};
}
export function torsoPoint(point,torso){
  const t=clamp((torso.blendEndY-point[1])/(torso.blendEndY-torso.blendStartY),0,1),weight=t*t*(3-2*t);
  return around(point,torso.pivot,torso.angle*weight);
}
export function torsoArm(arm,torso){
  if(!torso.angle)return arm;
  return {...arm,...Object.fromEntries(['shoulder','elbow','wrist'].map(key=>[key,around(arm[key],torso.pivot,torso.angle)]))};
}
export function elbowFlex(arm,solved){
  const a=sub(arm.shoulder,solved.elbow),b=sub(solved.solved,solved.elbow);
  const cosine=(a[0]*b[0]+a[1]*b[1])/(length(a)*length(b));
  return 180-Math.acos(clamp(cosine,-1,1))*180/Math.PI;
}
export function shoulderPose(pose,rig,enabled=true){
  const keys=rig.shoulderPose?.keys;
  if(!enabled||!keys?.length)return {support:[0,0],trigger:[0,0]};
  const degrees=clamp(Math.abs(pose.angle)*180/Math.PI,keys[0].degrees,keys.at(-1).degrees);
  const i=Math.max(0,keys.findIndex((key,n)=>n<keys.length-1&&degrees<=keys[n+1].degrees));
  const a=keys[i],b=keys[i+1]??a,t=a===b?0:(degrees-a.degrees)/(b.degrees-a.degrees),s=t*t*(3-2*t);
  return Object.fromEntries(['support','trigger'].map(side=>[side,a[side].map((v,n)=>v+(b[side][n]-v)*s)]));
}
export function shoulderArm(arm,shoulders){
  const offset=shoulders[arm.side];
  if(!offset?.some(Boolean))return arm;
  return {...arm,...Object.fromEntries(['shoulder','elbow','wrist'].map(key=>[key,add(arm[key],offset)]))};
}
export function shoulderBodyPoint(point,shoulders,rig){
  const [x,y]=point;
  if(y<372||y>540||(x>=460&&x<=610)||(y<400&&x>=450&&x<=700))return point;
  let result=[...point];
  for(const arm of rig.arms){
    const [cx,cy]=arm.shoulder;
    const radial=clamp(1-Math.hypot((x-cx)/110,(y-cy)/160),0,1);
    const border=clamp((y-372)/32,0,1)*clamp((540-y)/36,0,1);
    const weight=radial*radial*(3-2*radial)*border;
    result=add(result,shoulders[arm.side].map(v=>v*weight));
  }
  return result;
}
export function upperArmPoint(point,arm,projected,solved,torso,shoulders,rig){
  const angle=direction(projected.shoulder,solved.elbow)-direction(arm.shoulder,arm.elbow);
  const moved=add(projected.shoulder,rotate(sub(point,arm.shoulder),angle));
  const axis=sub(arm.elbow,arm.shoulder),size=length(axis);
  const along=((point[0]-arm.shoulder[0])*axis[0]+(point[1]-arm.shoulder[1])*axis[1])/size;
  // Keep the skin at the torso socket fixed while the lower upper-arm follows its posed joint.
  const t=clamp((along+64)/64,0,1),weight=t*t*(3-2*t);
  const fixed=shoulders?shoulderBodyPoint(point,shoulders,rig):point;
  const root=torso?torsoPoint(fixed,torso):fixed;
  return root.map((v,i)=>v+(moved[i]-v)*weight);
}
export function aimPose(pointer,rig,mode='ground-extension',range){
  const limit=35*Math.PI/180;
  const evaluate=angle=>{
    const p=poseAt(angle,rig,mode),q=sub(pointer,p.muzzle),body=sub(pointer,p.pivot);
    return {pose:p,cross:q[0]*p.ray[1]-q[1]*p.ray[0],forward:q[0]*p.ray[0]+q[1]*p.ray[1],bodyForward:body[0]*p.ray[0]+body[1]*p.ray[1]};
  };
  // A cursor between the body pivot and muzzle still defines a valid aiming line.
  const result=e=>({...e.pose,inRange:e.bodyForward>0,nearAim:e.bodyForward>0&&e.forward<=0});
  let lo=range?.[0]??-limit,hi=range?.[1]??limit,a=evaluate(lo),b=evaluate(hi);
  if(['raised-body','ground-extension','legacy'].includes(mode)){
    const rest=poseAt(0,rig,mode),q=sub(pointer,rest.pivot),radius=length(q);
    if(radius<1e-6)return {...poseAt(clamp(0,lo,hi),rig,mode),inRange:false,nearAim:true};
    const lateral=rig.muzzle[0]-rest.imagePivot[0];
    // Solve the forward body ray directly; a near cursor can have two line roots.
    const requested=Math.asin(clamp(lateral/Math.max(radius,Math.abs(lateral)+1e-6),-1,1))-Math.atan2(q[0],q[1]);
    const chosen=evaluate(clamp(requested,lo,hi));
    return {...result(chosen),inRange:requested>=lo-1e-9&&requested<=hi+1e-9&&chosen.bodyForward>0};
  }
  const endpoints=[a,b];
  const behindFallback=()=>{
    const score=e=>e.forward/Math.max(length(sub(pointer,e.pose.muzzle)),1e-8);
    const near=score(endpoints[0])>score(endpoints[1])?endpoints[0]:endpoints[1];
    return {...near.pose,inRange:false};
  };
  for(const endpoint of [a,b])if(Math.abs(endpoint.cross)<1e-7)return result(endpoint);
  if(a.cross*b.cross>0){const near=Math.abs(a.cross)<Math.abs(b.cross)?a:b;return {...near.pose,inRange:false};}
  for(let i=0;i<48;i++){const mid=(lo+hi)/2,c=evaluate(mid);if(a.cross*c.cross<=0){hi=mid;b=c;}else{lo=mid;a=c;}}
  const solved=evaluate((lo+hi)/2);
  return solved.bodyForward>0?result(solved):behindFallback();
}
export function solveArm(arm,target,options={}){
  const first=length(sub(arm.elbow,arm.shoulder)),second=length(sub(arm.wrist,arm.elbow));
  const distance=length(sub(target,arm.shoulder));
  const reach=clamp(distance,Math.abs(first-second)+1e-6,first+second-1e-6);
  const phi=direction(arm.shoulder,target);
  const bend=Math.acos(clamp((first*first+reach*reach-second*second)/(2*first*reach),-1,1));
  const bendSign=options.bend??arm.bend;
  const elbow=add(arm.shoulder,[first*Math.cos(phi+bendSign*bend),first*Math.sin(phi+bendSign*bend)]);
  const solved=add(arm.shoulder,[reach*Math.cos(phi),reach*Math.sin(phi)]);
  return {elbow,solved,gap:Math.abs(distance-reach)};
}
export function fitArmProjection(arm,target,config,enabled=true){
  if(!enabled)return {...arm,projectionScale:1};
  const first=length(sub(arm.elbow,arm.shoulder)),second=length(sub(arm.wrist,arm.elbow));
  const flex=config.flexDegrees*Math.PI/180;
  const projectedReach=Math.sqrt(first*first+second*second+2*first*second*Math.cos(flex));
  const projectionScale=clamp(length(sub(target,arm.shoulder))/projectedReach,config.minScale,config.maxScale);
  return {...arm,projectionScale,...Object.fromEntries(['elbow','wrist'].map(key=>[key,add(arm.shoulder,sub(arm[key],arm.shoulder).map(v=>v*projectionScale))]))};
}
export function anchoredUpperPoint(point,arm,solved,config){
  const axis=sub(arm.elbow,arm.shoulder),size=length(axis),unit=axis.map(v=>v/size);
  const delta=sub(point,arm.shoulder),along=delta[0]*unit[0]+delta[1]*unit[1],across=-delta[0]*unit[1]+delta[1]*unit[0];
  const target=sub(solved.elbow,arm.shoulder),targetSize=length(target),targetUnit=target.map(v=>v/targetSize);
  const moved=add(arm.shoulder,[targetUnit[0]*along*targetSize/size-targetUnit[1]*across,targetUnit[1]*along*targetSize/size+targetUnit[0]*across]);
  // Preserve the shoulder attachment; only the distal arm fully follows the solved bone.
  const t=clamp((along-config.rootBlendStart)/(config.rootBlendEnd-config.rootBlendStart),0,1),weight=t*t*(3-2*t);
  return point.map((v,i)=>v+(moved[i]-v)*weight);
}
export function upperJoinFrame(arm,solved,config,seam){
  const bone=sub(arm.elbow,arm.shoulder),size=length(bone),unit=bone.map(v=>v/size),normal=[-unit[1],unit[0]];
  const source=across=>arm.shoulder.map((v,i)=>v+unit[i]*seam.upper.along+normal[i]*across);
  const center=anchoredUpperPoint(source(seam.upper.center),arm,solved,config);
  const edge=anchoredUpperPoint(source(seam.upper.center+seam.upper.halfWidth),arm,solved,config);
  const offset=sub(edge,center),halfWidth=length(offset),posedNormal=offset.map(v=>v/halfWidth);
  return {center,normal:posedNormal,axis:[posedNormal[1],-posedNormal[0]],halfWidth};
}
export function joinedForePoint(point,arm,solved,config,seam){
  const bone=sub(arm.wrist,arm.elbow),size=length(bone),unit=bone.map(v=>v/size),normal=[-unit[1],unit[0]];
  const delta=sub(point,arm.elbow),along=delta[0]*unit[0]+delta[1]*unit[1],across=delta[0]*normal[0]+delta[1]*normal[1];
  const target=sub(solved.solved,solved.elbow),targetSize=length(target),axis=target.map(v=>v/targetSize),ratio=targetSize/size;
  const base=solved.elbow.map((v,i)=>v+axis[i]*along*ratio+[-axis[1],axis[0]][i]*across);
  // Fit the proximal lower-arm art to the intact upper cuff, fading out before the wrist.
  const frame=upperJoinFrame(arm,solved,config,seam);
  const joined=frame.center.map((v,i)=>v+frame.axis[i]*(along-seam.fore.along)*ratio+frame.normal[i]*(across-seam.fore.center)*frame.halfWidth/seam.fore.halfWidth);
  const end=Math.max(seam.fore.along+48,size-24),t=clamp((along-seam.fore.along)/(end-seam.fore.along),0,1),weight=1-t*t*(3-2*t);
  return base.map((v,i)=>v+(joined[i]-v)*weight);
}
export function armJoinFrame(arm,solved,seam){
  const fore=sub(solved.solved,solved.elbow),size=length(fore),axis=fore.map(v=>v/size),normal=[-axis[1],axis[0]];
  const ratio=size/length(sub(arm.wrist,arm.elbow));
  const center=add(solved.elbow,axis.map((v,i)=>v*seam.fore.along*ratio+normal[i]*seam.fore.center));
  return {center,axis,normal,halfWidth:seam.fore.halfWidth};
}
export function registeredForePoint(point,arm,seam){
  const source=sub(arm.wrist,arm.elbow),size=length(source),axis=source.map(v=>v/size),normal=[-axis[1],axis[0]];
  const delta=sub(point,arm.elbow),along=delta[0]*axis[0]+delta[1]*axis[1],across=delta[0]*normal[0]+delta[1]*normal[1];
  const t=clamp((along-(seam.fore.along+4))/50,0,1),weight=1-t*t*(3-2*t);
  const widthScale=1+(seam.fore.halfWidth/seam.fore.sourceHalfWidth-1)*weight;
  const fitted=seam.fore.center+(across-seam.fore.center)*widthScale;
  return arm.elbow.map((v,i)=>v+axis[i]*along+normal[i]*fitted);
}
export function stitchedUpperPoint(point,arm,solved,config,seam){
  const source=sub(arm.elbow,arm.shoulder),size=length(source),axis=source.map(v=>v/size),normal=[-axis[1],axis[0]];
  const delta=sub(point,arm.shoulder),along=delta[0]*axis[0]+delta[1]*axis[1],across=delta[0]*normal[0]+delta[1]*normal[1];
  const frame=armJoinFrame(arm,solved,seam),widthScale=frame.halfWidth/seam.upper.halfWidth;
  const alongScale=length(sub(solved.elbow,arm.shoulder))/size;
  const joined=frame.center.map((v,i)=>v+frame.axis[i]*(along-seam.upper.along)*alongScale+frame.normal[i]*(across-seam.upper.center)*widthScale);
  const base=anchoredUpperPoint(point,arm,solved,config);
  const t=clamp((along-(seam.upper.along-42))/34,0,1),weight=t*t*(3-2*t);
  return base.map((v,i)=>v+(joined[i]-v)*weight);
}
export function makePool(capacity){
  const items=Array.from({length:capacity},()=>({active:false,x:0,y:0,oldX:0,oldY:0,vx:0,vy:0,age:0}));
  return {items,dropped:0,spawn(position,direction,speed){
    const item=items.find(p=>!p.active);if(!item){this.dropped++;return false;}
    Object.assign(item,{active:true,x:position[0],y:position[1],oldX:position[0],oldY:position[1],vx:direction[0]*speed,vy:direction[1]*speed,age:0});return true;
  },step(dt){for(const p of items){if(!p.active)continue;p.oldX=p.x;p.oldY=p.y;p.x+=p.vx*dt;p.y+=p.vy*dt;p.age+=dt;if(p.age>1.8)p.active=false;}}};
}
