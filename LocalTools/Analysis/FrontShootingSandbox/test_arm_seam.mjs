import assert from 'node:assert/strict';
import fs from 'node:fs';
import {poseAt,wristTarget,fitArmProjection,solveArm} from './geometry.mjs';
import {crossSectionSpan} from './arm-contact.mjs';
import {armLayers,transformPoint,drawArmArt} from './arm-render.mjs';

const rig=JSON.parse(fs.readFileSync(new URL('./rig.json',import.meta.url)));
const parts=JSON.parse(fs.readFileSync(new URL('./parts-v2.json',import.meta.url)));
const foreConfig=JSON.parse(fs.readFileSync(new URL('./forearm-art-v9.json',import.meta.url)));
const arms=rig.arms.map(arm=>({...arm,shoulder:rig.armConnectionV2.shoulderRoots?.[arm.side]??parts.parts.find(p=>p.id===`${arm.side}-upper`).anchors.shoulder,elbow:parts.parts.find(p=>p.id===`${arm.side}-fore`).anchors.elbow}));
const close=(a,b)=>assert.ok(Math.hypot(...a.map((v,i)=>v-b[i]))<1e-8,`${a} differs from ${b}`);
let frames=0;
for(const arm of arms){
  const delta=arm.elbow.map((v,i)=>v-arm.shoulder[i]),size=Math.hypot(...delta),axis=delta.map(v=>v/size),normal=[-axis[1],axis[0]];
  const seam={upper:{along:size-(rig.armConnectionV2.elbowOverlap?.[arm.side]??0),center:arm.side==='trigger'?-4.5:20.5,halfWidth:arm.side==='trigger'?23.5:33.5},fore:{along:0,center:-3,halfWidth:24}};
  const armorHalfWidth=arm.side==='trigger'?41:39.5,cuffWidthRatio=foreConfig[arm.side].cuffWidthRatio;
  assert.equal(cuffWidthRatio,1.15);assert.equal(foreConfig[arm.side].includeHandPatch,false);
  const upperArt={canvas:{},bounds:[340,336,740,600]},foreArt={canvas:{},bounds:[340,450,740,740],targetHalfWidth:armorHalfWidth*cuffWidthRatio};
  for(let degrees=-35;degrees<=0;degrees+=.25){
    const pose=poseAt(degrees*Math.PI/180,rig,'raised-body'),target=wristTarget(arm,pose,rig);
    const fitted=fitArmProjection(arm,target,rig.armConnectionV2),solved=solveArm(fitted,target,{bend:arm.side==='support'?-arm.bend:arm.bend});
    const layers=armLayers(arm,solved,upperArt,foreArt,seam);
    close(transformPoint(arm.shoulder,layers.upper.matrix),arm.shoulder);
    close(transformPoint(arm.elbow,layers.upper.matrix),solved.elbow);
    close(transformPoint(arm.wrist,layers.fore.matrix),solved.solved);
    const previous=armLayers(arm,solved,upperArt,{...foreArt,targetHalfWidth:undefined,thicknessScale:arm.side==='trigger'?1.35:1},seam);
    assert.equal(layers.widthScale,foreArt.targetHalfWidth/seam.fore.halfWidth);
    assert.ok(layers.widthScale>previous.widthScale*1.2);
    close(layers.joint,previous.joint);
    assert.deepEqual(layers.upper.matrix,previous.upper.matrix);
    const fore=arm.wrist.map((v,i)=>v-arm.elbow[i]),foreLength=Math.hypot(...fore),foreAxis=fore.map(v=>v/foreLength),foreNormal=[-foreAxis[1],foreAxis[0]];
    const sourceUpper=arm.shoulder.map((v,i)=>v+axis[i]*seam.upper.along+normal[i]*seam.upper.center);
    const sourceFore=arm.elbow.map((v,i)=>v+foreAxis[i]*seam.fore.along+foreNormal[i]*seam.fore.center);
    const lowerSource=arm.wrist.map((v,i)=>v-sourceFore[i]),lowerLength=Math.hypot(...lowerSource),across=[-lowerSource[1]/lowerLength,lowerSource[0]/lowerLength];
    const edge=arm.wrist.map((v,i)=>v+across[i]*10),a=transformPoint(edge,layers.fore.matrix),b=transformPoint(arm.wrist,layers.fore.matrix);
    const oldA=transformPoint(edge,previous.fore.matrix),oldB=transformPoint(arm.wrist,previous.fore.matrix);
    assert.ok(Math.abs(Math.hypot(...a.map((v,i)=>v-b[i]))/Math.hypot(...oldA.map((v,i)=>v-oldB[i]))-layers.widthScale/previous.widthScale)<1e-8);
    close(transformPoint(sourceUpper,layers.upper.matrix),layers.joint);
    close(transformPoint(sourceFore,layers.fore.matrix),layers.joint);
    for(const [start,end,matrix] of [[arm.shoulder,arm.elbow,layers.upper.matrix],[sourceFore,arm.wrist,layers.fore.matrix]]){
      const mappedStart=transformPoint(start,matrix),mappedEnd=transformPoint(end,matrix);
      for(const t of [.1,.25,.5,.75,.9]){
        const p=start.map((v,i)=>v+(end[i]-v)*t);
        close(transformPoint(p,matrix),mappedStart.map((v,i)=>v+(mappedEnd[i]-v)*t));
      }
      // Parallel plate edges must remain straight and parallel, never bowing into a mesh curve.
      const offset=end.map((v,i)=>v+normal[i]*12),offsetStart=start.map((v,i)=>v+normal[i]*12);
      const a=transformPoint(offset,matrix),b=transformPoint(offsetStart,matrix);
      close(a.map((v,i)=>v-b[i]),mappedEnd.map((v,i)=>v-mappedStart[i]));
    }
    let draws=0;
    const paint={save(){},restore(){},transform(...values){assert.equal(values.length,6);},drawImage(){draws++;},getImageData(){throw new Error('No pixel readback while aiming.');}};
    drawArmArt(paint,layers.fore);drawArmArt(paint,layers.upper);assert.equal(draws,2);frames++;
  }
}
const data=new Uint8ClampedArray(15*15*4);
for(let x=3;x<=11;x++)data[(7*15+x)*4+3]=255;
assert.deepEqual(crossSectionSpan(data,15,15,[7,7],[0,1],0,7),{along:0,center:0,halfWidth:4});
const source=fs.readFileSync(new URL('./sandbox.mjs',import.meta.url),'utf8');
assert.ok(!source.includes('stitchedUpperPoint'),'Rejected upper-cuff deformation must not be used by the renderer.');
assert.ok(!source.includes('joinedForePoint'),'Rubbery lower-arm mesh must not be used by the renderer.');
assert.ok(!source.includes('function connectedFore'),'No dynamic forearm canvas rebuild/readback.');
console.log(JSON.stringify({affineArmFrames:frames,straightPlateEdges:true,shouldersAndWristsFixed:true,drawsPerArm:2,pixelReadbacksPerPose:0}));
