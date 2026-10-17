import assert from 'node:assert/strict';
import fs from 'node:fs';
import {aimPose,poseAt,weaponSweep,aimPivotOffset,imagePoint,wristTarget,handPlacement,projectArm,torsoPose,torsoPoint,torsoArm,shoulderPose,shoulderArm,shoulderBodyPoint,elbowFlex,upperArmPoint,around,rotate,airOffset,solveArm,fitArmProjection,anchoredUpperPoint,makePool,segmentHitsCircle} from './geometry.mjs';
const rig=JSON.parse(fs.readFileSync(new URL('./rig.json',import.meta.url)));
let maxGap=0;
let cases=0;
for(const mode of ['ground-extension','air-existing','legacy'])for(let degrees=-35;degrees<=35;degrees+=.5){
  const a=degrees*Math.PI/180;
  const expected=poseAt(a,rig,mode);
  const target=[expected.muzzle[0]+expected.ray[0]*1000,expected.muzzle[1]+expected.ray[1]*1000],pose=aimPose(target,rig,mode);
  assert.ok(pose.inRange,`Exact endpoint must remain shootable: ${mode} ${degrees}`);
  assert.ok(Math.abs(pose.angle-a)<1e-8,`${mode} ${degrees}`);
  const radius=Math.hypot(rig.muzzle[0]-pose.imagePivot[0],rig.muzzle[1]-pose.imagePivot[1]);
  assert.ok(Math.abs(Math.hypot(pose.muzzle[0]-pose.pivot[0],pose.muzzle[1]-pose.pivot[1])-radius)<1e-8);
  const toTarget=[target[0]-pose.muzzle[0],target[1]-pose.muzzle[1]];
  assert.ok(Math.abs(toTarget[0]*pose.ray[1]-toTarget[1]*pose.ray[0])<1e-8);
  for(const arm of rig.arms){const s=solveArm(arm,wristTarget(arm,pose,rig),{bend:arm.side==='support'?-arm.bend:arm.bend});assert.ok(Number.isFinite(s.gap));maxGap=Math.max(maxGap,s.gap);}
  cases++;
}
assert.deepEqual(airOffset(45),[0,0]);assert.deepEqual(airOffset(60),[.1,0]);assert.deepEqual(airOffset(75),[.225,.06]);assert.deepEqual(airOffset(90),[.45,.1]);
assert.ok(rig.pivot[1]<432,'Pivot must be behind stock');
for(const mode of ['ground-extension','air-existing'])assert.ok(Math.hypot(...poseAt(0,rig,mode).muzzle.map((v,i)=>v-rig.muzzle[i]-(rig.placementOffset?.[i]??0)))<1e-8);
let placementCases=0;
for(const mode of ['ground-extension','air-existing','legacy'])for(let degrees=-35;degrees<=35;degrees+=.5){
  const angle=degrees*Math.PI/180;
  const original=poseAt(angle,{...rig,placementOffset:[0,0]},mode);
  const lowered=poseAt(angle,{...rig,placementOffset:[0,72]},mode);
  for(const key of ['pivot','muzzle']){assert.ok(Math.abs(lowered[key][0]-original[key][0])<1e-8);assert.ok(Math.abs(lowered[key][1]-original[key][1]-72)<1e-8);}
  assert.deepEqual(original.ray,lowered.ray);assert.deepEqual(original.imagePivot,lowered.imagePivot);
  placementCases++;
}
const pool=makePool(2),refs=[...pool.items];assert.ok(pool.spawn([0,0],[0,1],10));assert.ok(pool.spawn([0,0],[0,1],10));assert.equal(pool.spawn([0,0],[0,1],10),false);pool.step(2);assert.ok(pool.spawn([0,0],[1,0],10));assert.equal(pool.items[0],refs[0]);
assert.ok(segmentHitsCircle([0,0],[100,0],[50,0],5));assert.equal(segmentHitsCircle([0,0],[100,0],[50,10],5),false);
const support=rig.arms.find(a=>a.side==='support');
const supportSnapshot=JSON.stringify(support);
const reachTarget=imagePoint(support.wrist,poseAt(-35*Math.PI/180,rig));
const outer=solveArm(support,reachTarget),inner=solveArm(support,reachTarget,{bend:-support.bend});
assert.ok(inner.elbow[0]<outer.elbow[0]);assert.ok(inner.elbow[1]>outer.elbow[1]);
for(const solution of [inner,outer]){
  assert.ok(Math.abs(Math.hypot(solution.elbow[0]-support.shoulder[0],solution.elbow[1]-support.shoulder[1])-Math.hypot(support.elbow[0]-support.shoulder[0],support.elbow[1]-support.shoulder[1]))<1e-8);
  assert.ok(Math.abs(Math.hypot(solution.solved[0]-solution.elbow[0],solution.solved[1]-solution.elbow[1])-Math.hypot(support.wrist[0]-support.elbow[0],support.wrist[1]-support.elbow[1]))<1e-8);
}
const downTarget=imagePoint(support.wrist,poseAt(0,rig));
const downInside=solveArm(support,downTarget,{bend:-support.bend});
assert.ok(downInside.gap>60,'Unreachable downward grip must not be hidden by stretching the arm.');
assert.equal(JSON.stringify(support),supportSnapshot);
let gripCases=0;
const unshiftedRig={...rig,gripOffsets:{support:[0,0]}};
for(const mode of ['ground-extension','air-existing','legacy'])for(let degrees=-35;degrees<=35;degrees+=.5){
  const pose=poseAt(degrees*Math.PI/180,rig,mode);
  assert.deepEqual(pose,poseAt(pose.angle,unshiftedRig,mode));
  const target=wristTarget(support,pose,rig),old=wristTarget(support,pose,unshiftedRig),delta=rotate(rig.gripOffsets.support,pose.angle);
  for(let i=0;i<2;i++)assert.ok(Math.abs(target[i]-old[i]-delta[i])<1e-8);
  const placement=handPlacement(support,pose,rig);
  const handWrist=rotate([support.wrist[0]-pose.imagePivot[0],support.wrist[1]-pose.imagePivot[1]],pose.angle).map((v,i)=>v+placement[i]);
  assert.ok(Math.hypot(...handWrist.map((v,i)=>v-target[i]))<1e-8);
  const trigger=rig.arms.find(a=>a.side==='trigger');assert.deepEqual(wristTarget(trigger,pose,rig),wristTarget(trigger,pose,unshiftedRig));
  gripCases++;
}
const raisedDown=solveArm(support,wristTarget(support,poseAt(0,rig),rig),{bend:-support.bend});
assert.ok(raisedDown.gap>0,'Reduced grip correction alone must not silently stretch the arm.');
assert.equal(rig.gripOffsets.support[1],-48);
let projectionCases=0,maxProjectedGap=0;
const rigBefore=JSON.stringify(rig);
for(let degrees=-35;degrees<=0;degrees+=.25){
  const pose=poseAt(degrees*Math.PI/180,rig),projected=projectArm(support,pose,rig);
  const solved=solveArm(projected,wristTarget(support,pose,rig),{bend:-support.bend});
  assert.ok(projected.foreScale>=.92&&projected.foreScale<=.96);
  const upperLength=Math.hypot(projected.elbow[0]-projected.shoulder[0],projected.elbow[1]-projected.shoulder[1]);
  assert.ok(Math.abs(upperLength-Math.hypot(support.elbow[0]-support.shoulder[0],support.elbow[1]-support.shoulder[1]))<1e-8);
  assert.ok(solved.gap<1e-6,`Projected support unreachable at ${degrees}`);
  const root=[support.shoulder[0],support.shoulder[1]-100];
  assert.deepEqual(upperArmPoint(root,support,projected,solved),root);
  for(const [source,destination] of [[support.shoulder,projected.shoulder],[support.elbow,solved.elbow]]){
    const point=upperArmPoint(source,support,projected,solved);
    assert.ok(Math.hypot(point[0]-destination[0],point[1]-destination[1])<1e-8);
  }
  assert.equal(projectArm(support,pose,rig,false),support);
  assert.equal(projectArm(rig.arms[0],pose,rig),rig.arms[0]);
  maxProjectedGap=Math.max(maxProjectedGap,solved.gap);projectionCases++;
}
assert.equal(JSON.stringify(rig),rigBefore);
const comparisonPose=poseAt(35*Math.PI/180,rig);
assert.ok(solveArm(projectArm(support,comparisonPose,rig),wristTarget(support,comparisonPose,rig),{bend:1}).gap>0,'Non-mirrored comparison must retain unreachable diagnostics.');
for(const degrees of [0,17.5,35]){
  const samples=[-1e-7,1e-7].map(epsilon=>projectArm(support,poseAt((degrees+epsilon)*Math.PI/180,rig),rig));
  assert.ok(Math.hypot(...samples[0].shoulder.map((v,i)=>v-samples[1].shoulder[i]))<1e-4);
}
let torsoCases=0,maxTorsoGap=0,maxTorsoFlex=0;
const torsoSamples=[];
for(let degrees=-35;degrees<=0;degrees+=.25){
  const pose=poseAt(degrees*Math.PI/180,rig),beforePose=JSON.stringify(pose);
  const target=wristTarget(support,pose,rig),hand=handPlacement(support,pose,rig);
  const torso=torsoPose(pose,rig),oldArm=projectArm(support,pose,rig),arm=torsoArm(oldArm,torso);
  const solved=solveArm(arm,target,{bend:1}),flex=elbowFlex(arm,solved);
  assert.ok(solved.gap<1e-6);
  assert.ok(flex<35&&flex>0,'Support elbow should remain slightly bent, not locked or sharply folded.');
  for(const [a,b] of [['shoulder','elbow'],['elbow','wrist']]){
    const size=j=>Math.hypot(j[a][0]-j[b][0],j[a][1]-j[b][1]);
    assert.ok(Math.abs(size(oldArm)-size(arm))<1e-8,'Torso rotation must not stretch arm segments.');
  }
  for(const point of [[300,900],[519,700],[750,1300]])assert.deepEqual(torsoPoint(point,torso),point);
  assert.deepEqual(torsoPoint(rig.torsoLean.pivot,torso),rig.torsoLean.pivot);
  assert.deepEqual(torsoPoint(support.shoulder,torso),around(support.shoulder,torso.pivot,torso.angle));
  const root=[support.shoulder[0],support.shoulder[1]-100];
  assert.deepEqual(upperArmPoint(root,support,arm,solved,torso),torsoPoint(root,torso));
  assert.equal(torsoPose(pose,rig,false).angle,0);
  assert.equal(torsoArm(oldArm,torsoPose(pose,rig,false)),oldArm);
  assert.equal(JSON.stringify(pose),beforePose);
  assert.deepEqual(wristTarget(support,pose,rig),target);assert.deepEqual(handPlacement(support,pose,rig),hand);
  maxTorsoGap=Math.max(maxTorsoGap,solved.gap);maxTorsoFlex=Math.max(maxTorsoFlex,flex);torsoCases++;
  if([0,-17.5,-35].includes(degrees))torsoSamples.push({aim:-90-degrees,lean:torso.angle*180/Math.PI,elbowFlex:flex});
}
for(const degrees of [0,17.5,35]){
  const values=[-1e-7,1e-7].map(epsilon=>torsoPose(poseAt((degrees+epsilon)*Math.PI/180,rig),rig).angle);
  assert.ok(Math.abs(values[0]-values[1])<1e-7);
}
assert.equal(torsoPose(poseAt(0,rig),rig).angle,0);
assert.equal(JSON.stringify(rig),rigBefore);
console.log(JSON.stringify({torsoCases,maxTorsoGap,maxTorsoFlex,torsoSamples}));
let shoulderCases=0,maxShoulderGap=0,maxShoulderFlex=0;
for(let degrees=-35;degrees<=0;degrees+=.25){
  const pose=poseAt(degrees*Math.PI/180,rig),before=JSON.stringify(pose),shoulders=shoulderPose(pose,rig);
  const original=projectArm(support,pose,rig),arm=shoulderArm(original,shoulders);
  const target=wristTarget(support,pose,rig),solved=solveArm(arm,target,{bend:1}),flex=elbowFlex(arm,solved);
  assert.ok(solved.gap<1e-6&&flex<35&&flex>0);
  assert.ok(shoulders.support[1]<=0&&shoulders.trigger[1]>=0);
  assert.equal(shoulderArm(original,shoulderPose(pose,rig,false)),original);
  for(const [a,b] of [['shoulder','elbow'],['elbow','wrist']]){
    const size=j=>Math.hypot(j[a][0]-j[b][0],j[a][1]-j[b][1]);
    assert.ok(Math.abs(size(original)-size(arm))<1e-8);
  }
  for(const point of [[600,200],[519,440],[500,385],[519,650],[300,900],[750,1300]])assert.deepEqual(shoulderBodyPoint(point,shoulders,rig),point);
  assert.equal(JSON.stringify(pose),before);assert.deepEqual(wristTarget(support,pose,rig),target);
  maxShoulderGap=Math.max(maxShoulderGap,solved.gap);maxShoulderFlex=Math.max(maxShoulderFlex,flex);shoulderCases++;
}
assert.deepEqual(shoulderPose(poseAt(0,rig),rig),{support:[0,0],trigger:[0,0]});
for(const degrees of [0,17.5,35]){
  const values=[-1e-7,1e-7].map(epsilon=>shoulderPose(poseAt((degrees+epsilon)*Math.PI/180,rig),rig));
  for(const side of ['support','trigger'])assert.ok(Math.hypot(...values[0][side].map((v,i)=>v-values[1][side][i]))<1e-4);
}
const html=fs.readFileSync(new URL('./index.html',import.meta.url),'utf8');
assert.ok(!html.match(/<input id="torso"[^>]*>/)[0].includes('checked'),'Rejected whole-torso lean must stay off by default.');
assert.ok(html.match(/<input id="shoulders"[^>]*>/)[0].includes('checked'));
assert.equal(JSON.stringify(rig),rigBefore);
console.log(JSON.stringify({shoulderCases,maxShoulderGap,maxShoulderFlex,headAndTorsoAnchorsFixed:true,wholeTorsoDefault:false}));
let raisedCases=0;
for(const rise of [0,64,96,128,192,220,320,640])for(let degrees=-35;degrees<=35;degrees+=.5){
  const trial={...rig,raisedPivot:{rise}},angle=degrees*Math.PI/180;
  const raised=poseAt(angle,trial,'raised-body'),stock=imagePoint(rig.stockRear,raised);
  const target=raised.muzzle.map((v,i)=>v+raised.ray[i]*1000),solved=aimPose(target,trial,'raised-body');
  assert.ok(solved.inRange&&Math.abs(solved.angle-angle)<1e-8);
  const toTarget=target.map((v,i)=>v-solved.muzzle[i]);
  assert.ok(Math.abs(toTarget[0]*solved.ray[1]-toTarget[1]*solved.ray[0])<1e-7);
  assert.ok(Math.abs(Math.hypot(...raised.muzzle.map((v,i)=>v-stock[i]))-Math.hypot(...rig.muzzle.map((v,i)=>v-rig.stockRear[i])))<1e-8,'Raising the pivot must not resize the weapon.');
  const original=poseAt(angle,rig,'ground-extension');
  if(!rise)assert.deepEqual(raised,original);
  for(const arm of rig.arms){
    const target=wristTarget(arm,raised,trial),placement=handPlacement(arm,raised,trial);
    const wrist=rotate(arm.wrist.map((v,i)=>v-raised.imagePivot[i]),angle).map((v,i)=>v+placement[i]);
    assert.ok(Math.hypot(...target.map((v,i)=>v-wrist[i]))<1e-8);
  }
  const down=poseAt(0,trial,'raised-body');
  assert.deepEqual(down.muzzle,poseAt(0,rig,'ground-extension').muzzle);
  raisedCases++;
}
const oldRest=poseAt(0,rig,'ground-extension'),oldEdge=poseAt(-35*Math.PI/180,rig,'ground-extension');
const newRest=poseAt(0,rig,'raised-body'),newEdge=poseAt(-35*Math.PI/180,rig,'raised-body');
const stockTravel=(a,b,point)=>Math.hypot(...imagePoint(point,a).map((v,i)=>v-imagePoint(point,b)[i]));
assert.ok(stockTravel(oldRest,oldEdge,rig.pivot)<1e-8);
assert.ok(stockTravel(newRest,newEdge,rig.pivot)>50,'The previous rear-stock attachment must now move.');
assert.ok(newRest.pivot[1]<oldRest.pivot[1]);
const raisedTrigger=rig.arms[0],raisedJoint=shoulderArm(projectArm(raisedTrigger,newEdge,rig),shoulderPose(newEdge,rig));
const raisedTriggerGap=solveArm(raisedJoint,wristTarget(raisedTrigger,newEdge,rig),{bend:raisedTrigger.bend}).gap;
assert.ok(raisedTriggerGap>2,'Keep right-arm mismatch visible; changing the pivot is not an arm repair.');
assert.equal(rig.trajectory.defaultMode,'raised-body');
assert.equal(JSON.stringify(rig),rigBefore);
console.log(JSON.stringify({raisedCases,raisedPivot:newRest.pivot,oldRearAttachmentTravel:stockTravel(oldRest,oldEdge,rig.pivot),newRearAttachmentTravel:stockTravel(newRest,newEdge,rig.pivot),raisedTriggerGap,weaponScalePreserved:true}));
let sweepCases=0;
for(const rise of [0,96,192,320,640]){
  const trial={...rig,raisedPivot:{rise}},sweep=weaponSweep(trial);
  const distance=(a,b)=>Math.hypot(...a.map((v,i)=>v-b[i]));
  for(let i=0;i<71;i++){
    const angle=(i-35)*Math.PI/180,pose=poseAt(angle,trial,'raised-body');
    assert.deepEqual(pose.pivot,sweep.pivot,'Virtual center must not follow the cursor or aim angle.');
    assert.deepEqual(sweep.stock[i],imagePoint(rig.stockRear,pose));assert.deepEqual(sweep.muzzle[i],pose.muzzle);
    for(const path of [sweep.stock,sweep.muzzle])assert.ok(Math.abs(distance(path[i],sweep.pivot)-distance(path[35],sweep.pivot))<1e-8,'Each displayed arc must have a constant radius.');
    assert.ok(Math.abs(distance(sweep.stock[i],sweep.muzzle[i])-distance(rig.stockRear,rig.muzzle))<1e-8);
    sweepCases++;
  }
  for(const degrees of [-35.01,35.01]){
    const pose=poseAt(degrees*Math.PI/180,trial,'raised-body');
    const target=pose.muzzle.map((v,i)=>v+pose.ray[i]*2000);
    assert.equal(aimPose(target,trial,'raised-body').inRange,false,'Do not widen the angular firing limits.');
  }
}
assert.ok(!html.includes('value="aim-follow"'),'The rejected translating-axis trial must not remain in the active UI.');
assert.ok(html.match(/<input id="paths"[^>]*>/)[0].includes('checked'));
assert.equal(rig.raisedPivot.rise,192);
console.log(JSON.stringify({sweepCases,fixedCenter:true,angularLimitDegrees:35,displayedArcsMatchWeapon:true}));
const partConfig=JSON.parse(fs.readFileSync(new URL('./parts-v2.json',import.meta.url)));
const connectedArms=rig.arms.map(arm=>({...arm,
  shoulder:partConfig.parts.find(p=>p.id===`${arm.side}-upper`).anchors.shoulder,
  elbow:partConfig.parts.find(p=>p.id===`${arm.side}-fore`).anchors.elbow
}));
let connectedCases=0,connectedMaxGap=0;
const connectedRanges={trigger:[Infinity,0],support:[Infinity,0]};
for(const mode of ['raised-body','ground-extension'])for(let degrees=-35;degrees<=0;degrees+=.25)for(const arm of connectedArms){
  const pose=poseAt(degrees*Math.PI/180,rig,mode),target=wristTarget(arm,pose,rig);
  const before=JSON.stringify(arm),fitted=fitArmProjection(arm,target,rig.armConnectionV2),solved=solveArm(fitted,target,{bend:1});
  assert.equal(JSON.stringify(arm),before);
  assert.deepEqual(fitted.shoulder,arm.shoulder);
  assert.ok(solved.gap<1e-6,`Connected ${arm.side}: ${mode} ${degrees}`);
  if(mode==='raised-body')assert.ok(Math.abs(elbowFlex(fitted,solved)-rig.armConnectionV2.flexDegrees)<1e-6);
  else assert.ok(Number.isFinite(elbowFlex(fitted,solved)));
  assert.ok(fitted.projectionScale>=rig.armConnectionV2.minScale&&fitted.projectionScale<=rig.armConnectionV2.maxScale);
  assert.deepEqual(wristTarget(rig.arms.find(a=>a.side===arm.side),pose,rig),target,'Do not move the established gun grip to repair arm reach.');
  const first=arm.elbow.map((v,i)=>v-arm.shoulder[i]),size=Math.hypot(...first),unit=first.map(v=>v/size);
  const root=arm.shoulder.map((v,i)=>v-unit[i]*100);
  assert.deepEqual(anchoredUpperPoint(root,arm,solved,rig.armConnectionV2),root);
  assert.deepEqual(anchoredUpperPoint(arm.shoulder,arm,solved,rig.armConnectionV2),arm.shoulder);
  const tip=anchoredUpperPoint(arm.elbow,arm,solved,rig.armConnectionV2);
  assert.ok(Math.hypot(...tip.map((v,i)=>v-solved.elbow[i]))<1e-8);
  const width=[-8,8].map(sign=>anchoredUpperPoint([arm.elbow[0]-unit[1]*sign,arm.elbow[1]+unit[0]*sign],arm,solved,rig.armConnectionV2));
  assert.ok(Math.abs(Math.hypot(...width[0].map((v,i)=>v-width[1][i]))-16)<1e-8,'Length projection must preserve the distal arm width.');
  assert.equal(fitArmProjection(arm,target,rig.armConnectionV2,false).projectionScale,1);
  connectedRanges[arm.side][0]=Math.min(connectedRanges[arm.side][0],fitted.projectionScale);
  connectedRanges[arm.side][1]=Math.max(connectedRanges[arm.side][1],fitted.projectionScale);
  connectedMaxGap=Math.max(connectedMaxGap,solved.gap);connectedCases++;
}
const extremePose=poseAt(-35*Math.PI/180,{...rig,raisedPivot:{rise:640}},'raised-body');
const extremeTarget=wristTarget(connectedArms[0],extremePose,rig),capped=fitArmProjection(connectedArms[0],extremeTarget,rig.armConnectionV2);
assert.equal(capped.projectionScale,rig.armConnectionV2.maxScale);
assert.ok(solveArm(capped,extremeTarget).gap>2,'Keep unreachable extreme settings visible instead of stretching without a bound.');
assert.ok(html.match(/<input id="armV2"[^>]*>/)[0].includes('checked'));
assert.equal(JSON.stringify(rig),rigBefore);
console.log(JSON.stringify({connectedCases,connectedMaxGap,connectedRanges,gripAnchorsPreserved:true,shoulderRootsFixed:true,projectionCap:rig.armConnectionV2.maxScale}));
let followCases=0;
for(const travel of [0,24,80])for(const rise of [0,96,220])for(let degrees=-35;degrees<=35;degrees+=.5){
  const trial={...rig,raisedPivot:{rise},aimFollow:{travel}},angle=degrees*Math.PI/180;
  const follow=poseAt(angle,trial,'aim-follow'),fixed=poseAt(angle,trial,'raised-body');
  const offset=aimPivotOffset(angle,trial);
  assert.ok(Math.abs(offset[0])<=travel+1e-8&&Math.abs(offset[1])<=travel*.25+1e-8);
  assert.deepEqual(follow.imagePivot,fixed.imagePivot);
  for(const key of ['pivot','muzzle'])for(let i=0;i<2;i++)assert.ok(Math.abs(follow[key][i]-fixed[key][i]-offset[i])<1e-8);
  if(!travel||!degrees)assert.deepEqual(follow,fixed);
  const stock=imagePoint(rig.stockRear,follow);
  assert.ok(Math.abs(Math.hypot(...follow.muzzle.map((v,i)=>v-stock[i]))-Math.hypot(...rig.muzzle.map((v,i)=>v-rig.stockRear[i])))<1e-8);
  for(const distance of [50,1000,3000]){
    const target=follow.muzzle.map((v,i)=>v+follow.ray[i]*distance),solved=aimPose(target,trial,'aim-follow');
    assert.ok(solved.inRange&&Math.abs(solved.angle-angle)<1e-8,`Moving pivot: ${rise} ${travel} ${degrees} ${distance}`);
    const delta=target.map((v,i)=>v-solved.muzzle[i]);
    assert.ok(Math.abs(delta[0]*solved.ray[1]-delta[1]*solved.ray[0])<1e-7);
    const mirroredTarget=[2*rig.referenceAxisX-target[0],target[1]],mirroredMuzzle=[2*rig.referenceAxisX-solved.muzzle[0],solved.muzzle[1]];
    const mirroredDelta=mirroredTarget.map((v,i)=>v-mirroredMuzzle[i]);
    assert.ok(Math.abs(mirroredDelta[0]*solved.ray[1]+mirroredDelta[1]*solved.ray[0])<1e-7);
  }
  for(const arm of rig.arms){
    const placement=handPlacement(arm,follow,trial),target=wristTarget(arm,follow,trial);
    const wrist=rotate(arm.wrist.map((v,i)=>v-follow.imagePivot[i]),angle).map((v,i)=>v+placement[i]);
    assert.ok(Math.hypot(...wrist.map((v,i)=>v-target[i]))<1e-8);
  }
  followCases++;
}
for(const degrees of [-35,0,35]){
  const [left,right]=[-1e-7,1e-7].map(epsilon=>poseAt((degrees+epsilon)*Math.PI/180,rig,'aim-follow'));
  assert.ok(Math.hypot(...left.pivot.map((v,i)=>v-right.pivot[i]))<1e-4);
  assert.ok(Math.hypot(...left.muzzle.map((v,i)=>v-right.muzzle[i]))<1e-4);
}
const movingEdge=poseAt(-35*Math.PI/180,rig,'aim-follow');
const movingTriggerGap=solveArm(raisedJoint,wristTarget(raisedTrigger,movingEdge,rig),{bend:raisedTrigger.bend}).gap;
assert.ok(movingTriggerGap>2,'Pivot translation must not conceal the unresolved trigger-arm mismatch.');
assert.equal(JSON.stringify(rig),rigBefore);
console.log(JSON.stringify({followCases,followRayCases:followCases*3,followTravel:rig.aimFollow.travel,movingTriggerGap,fixedComparisonUnchanged:true}));
console.log(JSON.stringify({supportInsideBranchPassed:true,oldDownSupportGap:downInside.gap,unprojectedReducedGripGap:raisedDown.gap,gripCases,projectionCases,maxProjectedGap,legacyTrajectoryRegressionPassed:true}));
console.log(JSON.stringify({angleCases:cases,placementCases,placementOffset:rig.placementOffset,pivot:rig.pivot,rayTestsPassed:true,poolReusePassed:true,serializedAirOffsetsPassed:true,maxArmReachGap:maxGap,unityTest:false},null,2));
