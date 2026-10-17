import {opaqueBounds} from './arm-contact.mjs';
import {poseAt,wristTarget,fitArmProjection,solveArm} from './geometry.mjs';

export async function exportUnityPose({rig,arms,body,headLayers,images,upper,fore,seams,layers}){
  const canvas=document.createElement('canvas');canvas.width=canvas.height=2048;
  const paint=canvas.getContext('2d'),items=[];
  const add=(id,source,bounds)=>{
    if(!bounds){
      const c=document.createElement('canvas');c.width=1024;c.height=1536;
      const p=c.getContext('2d');p.drawImage(source,0,0);source=c;
      bounds=opaqueBounds(p.getImageData(0,0,1024,1536).data,1024,1536);
    }
    items.push({id,source,bounds});
  };
  add('body',headLayers.body);add('head',headLayers.head,headLayers.bounds);add('weapon',images.weapon);
  for(const arm of arms){
    add(`${arm.side}-upper`,upper[arm.side].canvas,upper[arm.side].bounds);
    add(`${arm.side}-fore`,fore[arm.side].canvas,fore[arm.side].bounds);
    add(`${arm.side}-hand`,images[`${arm.side}-hand-v2`]);
  }
  items.sort((a,b)=>(b.bounds[3]-b.bounds[1])-(a.bounds[3]-a.bounds[1]));
  let x=4,y=4,rowHeight=0;
  for(const item of items){
    const [l,t,r,b]=item.bounds,w=r-l,h=b-t;
    if(x+w+4>2048){x=4;y+=rowHeight+8;rowHeight=0;}
    if(y+h+4>2048)throw new Error('Unity pose atlas overflow.');
    paint.drawImage(item.source,l,t,w,h,x,y,w,h);item.atlas=[x,y,w,h];x+=w+8;rowHeight=Math.max(rowHeight,h);
  }
  const bodyCanvas=document.createElement('canvas');bodyCanvas.width=1024;bodyCanvas.height=1536;
  const bodyPaint=bodyCanvas.getContext('2d');bodyPaint.drawImage(body,0,0);
  const bodyBounds=opaqueBounds(bodyPaint.getImageData(0,0,1024,1536).data,1024,1536);
  const data={version:1,sourceSize:rig.sourceSize,atlasSize:[2048,2048],origin:[rig.referenceAxisX,bodyBounds[3]-1],
    pixelsPerUnit:rig.trajectory.previewPixelsPerUnit,imagePivot:[rig.pivot[0],rig.pivot[1]-rig.raisedPivot.rise],
    placementOffset:rig.placementOffset,muzzle:rig.muzzle,headAim:rig.headAim,
    minScale:rig.armConnectionV2.minScale,maxScale:rig.armConnectionV2.maxScale,flexDegrees:rig.armConnectionV2.flexDegrees,
    layers:items.map(({id,bounds,atlas})=>({id,bounds,atlas})),
    arms:arms.map(arm=>({...arm,gripOffset:rig.gripOffsets?.[arm.side]??[0,0],upperSeam:seams[arm.side].upper,
      foreSeam:seams[arm.side].fore,targetHalfWidth:fore[arm.side].targetHalfWidth})),samples:[]};
  for(const degrees of [0,-17.5,-35]){
    const pose=poseAt(degrees*Math.PI/180,rig,'raised-body');
    data.samples.push({degrees,muzzle:pose.muzzle,arms:arms.map(arm=>{
      const target=wristTarget(arm,pose,rig),fitted=fitArmProjection(arm,target,rig.armConnectionV2);
      const solved=solveArm(fitted,target,{bend:arm.side==='support'?-arm.bend:arm.bend}),art=layers(arm,solved);
      return {side:arm.side,wrist:target,upperMatrix:art.upper.matrix,foreMatrix:art.fore.matrix};
    })});
  }
  const panel=document.createElement('section');panel.style.cssText='position:fixed;inset:0;z-index:100;background:#253741;color:white;padding:20px';
  document.body.append(panel);
  const link=(label,name,blob)=>{
    const a=document.createElement('a');a.textContent=label;a.download=name;a.href=URL.createObjectURL(blob);
    a.style.cssText='display:block;color:white;margin:20px';panel.append(a);
  };
  link('Download Unity atlas','front-down-atlas-v30.png',await new Promise(resolve=>canvas.toBlob(resolve,'image/png')));
  link('Download Unity pose data','front-down-pose-v30.json',new Blob([JSON.stringify(data,null,2)],{type:'application/json'}));
  panel.append(canvas);canvas.style.cssText='width:600px;max-width:100%;height:auto';
}
