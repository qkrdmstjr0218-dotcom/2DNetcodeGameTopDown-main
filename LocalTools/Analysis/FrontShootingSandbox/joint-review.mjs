import {poseAt,wristTarget,fitArmProjection,solveArm,handPlacement} from './geometry.mjs';
import {drawArmArt} from './arm-render.mjs';
import {headAimAmount,drawAimedHead} from './head-aim.mjs';

// Optional developer view: inspect the actual cached art at source-pixel scale.
export function showJointReview({rig,arms,body,images,layers,clipBehindHead,headLayers}){
  const panel=document.createElement('section');
  panel.style.cssText='position:fixed;inset:0;z-index:100;overflow:auto;background:#253741';
  document.body.append(panel);
  for(const degrees of [0,-35]){
    const canvas=document.createElement('canvas');canvas.width=520;canvas.height=340;
    canvas.style.cssText='display:block;width:520px;height:340px;max-width:100%';panel.append(canvas);
    const p=canvas.getContext('2d');p.fillStyle='#253741';p.fillRect(0,0,520,340);
    p.fillStyle='white';p.font='14px sans-serif';p.fillText(`${-90-degrees} deg`,12,20);
    const pose=poseAt(degrees*Math.PI/180,rig,'raised-body');
    const amount=headLayers?headAimAmount(pose.angle,rig.headAim):0;
    p.save();p.beginPath();p.rect(0,25,520,315);p.clip();
    if(headLayers){p.translate(10-190*.7,30);p.scale(.7,.7);}
    else{p.translate(10-330*1.2,30-330*1.2);p.scale(1.2,1.2);}
    p.drawImage(amount?headLayers.body:body,0,0);
    p.save();if(!amount)clipBehindHead(p);
    for(const arm of arms){
      const target=wristTarget(arm,pose,rig),fitted=fitArmProjection(arm,target,rig.armConnectionV2);
      const solved=solveArm(fitted,target,{bend:arm.side==='support'?-arm.bend:arm.bend});
      const art=layers(arm,solved);drawArmArt(p,art.fore);drawArmArt(p,art.upper);
    }
    p.restore();
    if(amount)drawAimedHead(p,headLayers,amount,rig.headAim);
    for(const arm of arms){
      p.save();p.translate(...handPlacement(arm,pose,rig));p.rotate(pose.angle);
      p.translate(-pose.imagePivot[0],-pose.imagePivot[1]);p.drawImage(images[`${arm.side}-hand-v2`],0,0);p.restore();
    }
    p.translate(...pose.pivot);p.rotate(pose.angle);p.translate(-pose.imagePivot[0],-pose.imagePivot[1]);
    p.drawImage(images.weapon,0,0);p.restore();
  }
}
