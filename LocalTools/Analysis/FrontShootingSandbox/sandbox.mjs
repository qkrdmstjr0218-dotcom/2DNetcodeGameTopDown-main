import {aimPose,poseAt,weaponSweep,wristTarget,handPlacement,projectArm,torsoPose,torsoPoint,torsoArm,shoulderPose,shoulderArm,shoulderBodyPoint,elbowFlex,upperArmPoint,solveArm,fitArmProjection,anchoredUpperPoint,direction,makePool,segmentHitsCircle} from './geometry.mjs?v=rigid-arm-9';
import {paintForearm,paintElbowBacking,paintShoulderSocket,crossSectionSpan,opaqueBounds,auditForearm} from './arm-contact.mjs?v=arm-volume-11';
import {armLayers,drawArmArt} from './arm-render.mjs?v=arm-volume-11';
import {headAimAmount,easeHead,buildHeadLayers,drawAimedHead} from './head-aim.mjs?v=head-aim-30';
const canvas=document.querySelector('#view'),ctx=canvas.getContext('2d');
const $=id=>document.getElementById(id);
const rig=await fetch('./rig.json',{cache:'no-store'}).then(r=>r.json());
const partsV2=await fetch('./parts-v2.json',{cache:'no-store'}).then(r=>r.json());
const foreArt=await fetch('./forearm-art-v9.json',{cache:'no-store'}).then(r=>r.json());
const armsV2=rig.arms.map(arm=>{
  const upper=partsV2.parts.find(p=>p.id===`${arm.side}-upper`),fore=partsV2.parts.find(p=>p.id===`${arm.side}-fore`);
  // Keep established hand/grip anchors; recalibrate the shoulder and elbow only.
  // The trigger arm turns at the inner shoulder socket, not the outer deltoid.
  return {...arm,sourceShoulder:upper.anchors.shoulder,shoulder:rig.armConnectionV2.shoulderRoots?.[arm.side]??upper.anchors.shoulder,elbow:fore.anchors.elbow};
});
$('trajectory').value=rig.trajectory.defaultMode;
rig.raisedPivot??={rise:96};$('pivotRise').value=rig.raisedPivot.rise;
function updatePivotRise(){rig.raisedPivot.rise=Number($('pivotRise').value);$('pivotRiseValue').textContent=`${rig.raisedPivot.rise} px`;}
updatePivotRise();
rig.placementOffset??=[0,72];
$('drop').value=rig.placementOffset[1];
function updateDrop(){rig.placementOffset[1]=Number($('drop').value);$('dropValue').textContent=`${rig.placementOffset[1]} px`;}
$('drop').addEventListener('input',updateDrop);updateDrop();
rig.gripOffsets??={support:[0,-48]};
$('gripRise').value=-(rig.gripOffsets.support?.[1]??0);
function updateGrip(){rig.gripOffsets.support=[0,-Number($('gripRise').value)];$('gripRiseValue').textContent=`${-rig.gripOffsets.support[1]} px`;}
$('gripRise').addEventListener('input',updateGrip);updateGrip();
$('shoulders').addEventListener('change',()=>{if($('shoulders').checked)$('torso').checked=false;});
$('torso').addEventListener('change',()=>{if($('torso').checked)$('shoulders').checked=false;});
const defaultControls=[...document.querySelectorAll('aside input,aside select')].map(element=>({element,value:element.value,checked:element.checked}));
const names=['body','weapon','trigger-upper','trigger-fore','trigger-hand','support-upper','support-fore','support-hand'];
const images=Object.fromEntries(await Promise.all([...names,...names.filter(n=>n.includes('-')).map(n=>`${n}-v2`),'parts-source-v2','body-underlay-reference','torso-side-fill-v4','body-detail-v13',...Object.values(foreArt).map(p=>p.image)].map(async name=>{
  const image=new Image();image.src=`./${name}.png`;await image.decode();return[name,image];
})));
$('loading').remove();
let W=1,H=1,scale=1,origin=[0,0],pointer=[519,1600],held=false,shots=0,hits=0,lastShot=-1,flash=0,now=0;
let pose=aimPose(pointer,rig),maxGap=0;
const pool=makePool(96),targets=Array.from({length:7},(_,i)=>({x:0,y:0,r:19,flash:0,id:i}));
function resize(){
  const rect=canvas.getBoundingClientRect(),dpr=Math.min(devicePixelRatio||1,2);
  W=rect.width;H=rect.height;canvas.width=Math.round(W*dpr);canvas.height=Math.round(H*dpr);ctx.setTransform(dpr,0,0,dpr,0,0);
  scale=H*Number($('scale').value)/100/1536;
  origin=[W*.48-519*scale,Math.max(15,H*.025)];
  for(const t of targets){t.x=W*(.18+t.id*.095);t.y=H*.94;t.r=Math.max(12,Math.min(19,W/38));}
}
new ResizeObserver(resize).observe(canvas);$('scale').addEventListener('input',resize);
function input(e){$('posePreset').value='mouse';const r=canvas.getBoundingClientRect();pointer=[(e.clientX-r.left-origin[0])/scale,(e.clientY-r.top-origin[1])/scale];}
function refreshPreset(){
  if($('posePreset').value==='mouse')return;
  const sample=poseAt(Number($('posePreset').value)*Math.PI/180,rig,$('trajectory').value);
  pointer=sample.muzzle.map((v,i)=>v+sample.ray[i]*1100);
}
$('posePreset').addEventListener('change',refreshPreset);
function updateTrajectoryControls(){
  $('pivotRise').disabled=$('trajectory').value!=='raised-body';
  $('shoulders').disabled=$('torso').disabled=$('armV2').checked;
  $('foreArt').disabled=!$('armV2').checked;
  $('headFollow').disabled=!$('armV2').checked;
}
$('armV2').addEventListener('change',updateTrajectoryControls);
$('trajectory').addEventListener('change',()=>{updateTrajectoryControls();refreshPreset();});
$('pivotRise').addEventListener('input',()=>{updatePivotRise();refreshPreset();});
$('drop').addEventListener('input',refreshPreset);
$('posePreset').disabled=false;
updateTrajectoryControls();
canvas.addEventListener('pointermove',input);
canvas.addEventListener('pointerdown',e=>{input(e);held=true;canvas.setPointerCapture(e.pointerId);fire();});
canvas.addEventListener('pointerup',()=>held=false);canvas.addEventListener('pointercancel',()=>held=false);
window.addEventListener('blur',()=>held=false);
function resetRecords(){
  held=false;shots=hits=0;lastShot=-1;flash=0;pool.dropped=0;
  for(const p of pool.items)p.active=false;
  for(const target of targets)target.flash=0;
}
$('resetRecords').onclick=resetRecords;
$('reset').onclick=()=>{
  for(const {element,value,checked} of defaultControls){element.value=value;if(element.type==='checkbox')element.checked=checked;}
  updatePivotRise();updateDrop();updateGrip();updateTrajectoryControls();resize();
  pointer=[519,1600];headAmount=0;resetRecords();invalidateArtAudit();
};
function screen(p){return[origin[0]+p[0]*scale,origin[1]+p[1]*scale];}
function calculate(){
  const axis=rig.referenceAxisX,flipped=$('mirror').checked&&pointer[0]<axis;
  const localPointer=flipped?[2*axis-pointer[0],pointer[1]]:pointer;
  const localPose=aimPose(localPointer,rig,$('trajectory').value,$('mirror').checked?[-35*Math.PI/180,0]:undefined);
  const globalPose=flipped?{...localPose,angle:-localPose.angle,muzzle:[2*axis-localPose.muzzle[0],localPose.muzzle[1]],ray:[-localPose.ray[0],localPose.ray[1]]}:localPose;
  return {flipped,localPose,globalPose};
}
function fire(){
  const current=calculate().globalPose;if(!current.inRange||now-lastShot<.1)return;
  const point=screen(current.muzzle);
  if(pool.spawn(point,current.ray,1000)){shots++;flash=.05;lastShot=now;}
}
function part(name,from,to,angle){
  ctx.save();ctx.translate(...to);ctx.rotate(angle);ctx.translate(-from[0],-from[1]);ctx.drawImage(images[name],0,0);ctx.restore();
}
function limbPart(name,from,end,to,target,image=images[name]){
  const sourceAngle=direction(from,end),targetAngle=direction(to,target);
  const ratio=Math.hypot(target[0]-to[0],target[1]-to[1])/Math.hypot(end[0]-from[0],end[1]-from[1]);
  ctx.save();ctx.translate(...to);ctx.rotate(targetAngle);ctx.scale(ratio,1);ctx.rotate(-sourceAngle);ctx.translate(-from[0],-from[1]);ctx.drawImage(image,0,0);ctx.restore();
}
function surface(){
  const canvas=document.createElement('canvas');[canvas.width,canvas.height]=rig.sourceSize;
  return {canvas,context:canvas.getContext('2d'),key:''};
}
const upperSurfaces={trigger:surface(),support:surface()},bodySurface=surface();
const socketSource=surface();socketSource.context.drawImage(images.body,0,0);
// Old skin slivers lie outside the provisional arm cutouts; do not carry them into a raised socket.
socketSource.context.save();clipBehindHead(socketSource.context);socketSource.context.globalCompositeOperation='destination-out';
for(const points of [[[690,388],[717,400],[725,445],[714,481],[695,482],[697,432]],[[357,376],[377,376],[367,432],[379,468],[356,468]]]){
  socketSource.context.beginPath();points.forEach((p,i)=>i?socketSource.context.lineTo(...p):socketSource.context.moveTo(...p));
  socketSource.context.closePath();socketSource.context.fill();
}
// Remove the small old-arm residue below the ponytail, without touching its strands.
socketSource.context.beginPath();socketSource.context.rect(356,430,22,63);socketSource.context.fill();
socketSource.context.restore();
const connectedBody=surface();connectedBody.context.drawImage(socketSource.canvas,0,0);
{
  const original=surface(),under=surface(),mask=surface();
  original.context.drawImage(images['parts-source-v2'],0,0);under.context.drawImage(images['body-underlay-reference'],0,0);
  for(const arm of armsV2)for(const part of ['upper','fore','hand'])mask.context.drawImage(images[`${arm.side}-${part}-v2`],0,0);
  const body=connectedBody.context.getImageData(0,0,...rig.sourceSize),source=original.context.getImageData(0,0,...rig.sourceSize).data;
  const replacement=under.context.getImageData(0,0,...rig.sourceSize).data,selected=mask.context.getImageData(0,0,...rig.sourceSize).data;
  // Replace leftover source-arm pixels only, reusing the underlay already used by the existing body.
  for(let p=0;p<body.data.length;p+=4)if(selected[p+3]&&body.data[p]===source[p]&&body.data[p+1]===source[p+1]&&body.data[p+2]===source[p+2]&&body.data[p+3]===source[p+3]){
    for(let c=0;c<4;c++)body.data[p+c]=replacement[p+c];
  }
  connectedBody.context.putImageData(body,0,0);
  // Restore only the exposed vest interior. Generated hair, limbs and backdrop are never composited.
  const paint=connectedBody.context;
  // The final fleck is on the hair/body layer, outside the old arm-removal strip.
  paint.save();paint.globalCompositeOperation='destination-out';paint.beginPath();
  paint.ellipse(367,420,8,11,0,0,Math.PI*2);paint.fill();paint.restore();
  paint.clearRect(670,558,20,27);
  paint.save();paint.beginPath();
  paint.moveTo(425,444);paint.lineTo(638,444);paint.lineTo(645,485);
  paint.lineTo(644,530);paint.lineTo(639,567);paint.lineTo(635,604);
  paint.lineTo(592,640);paint.lineTo(451,640);paint.lineTo(411,604);
  paint.bezierCurveTo(416,585,431,572,438,550);
  paint.bezierCurveTo(443,532,436,512,433,495);
  paint.bezierCurveTo(429,480,422,461,425,444);paint.closePath();paint.clip();
  paint.globalCompositeOperation='destination-over';paint.drawImage(images['torso-side-fill-v4'],0,0);paint.restore();
  paint.save();clipBehindHead(paint);paint.globalCompositeOperation='destination-over';
  for(const arm of armsV2)paintShoulderSocket(paint,{...arm,shoulder:arm.sourceShoulder},images['parts-source-v2']);
  paint.restore();
  // A fixed skin backing joins the rotating cap to the torso beneath the collar.
  paint.save();clipBehindHead(paint);paint.beginPath();
  paint.moveTo(395,349);paint.bezierCurveTo(412,337,439,340,454,350);
  paint.bezierCurveTo(468,361,475,388,472,415);
  paint.bezierCurveTo(450,432,416,436,392,411);
  paint.bezierCurveTo(384,393,384,369,395,349);paint.closePath();paint.clip();
  paint.drawImage(images['body-detail-v13'],385,369,43,40,382,335,98,108);
  paint.beginPath();paint.moveTo(454,350);paint.bezierCurveTo(439,340,412,337,395,349);
  paint.bezierCurveTo(384,369,384,393,392,411);
  paint.strokeStyle='#503b35';paint.lineWidth=4;paint.lineCap='round';paint.lineJoin='round';paint.stroke();
  paint.restore();
  // The missing pixels belong to the fixed body, above the separate moving upper arm.
  // Source-over also fills transparent pixels, unlike the previous source-atop patch.
  paint.save();paint.beginPath();paint.moveTo(670,386);
  paint.bezierCurveTo(676,384,683,384,688,387);
  paint.bezierCurveTo(691,390,692,394,690,398);
  paint.bezierCurveTo(688,402,684,404,681,405);
  paint.bezierCurveTo(673,405,666,398,666,392);
  paint.closePath();paint.clip();
  paint.drawImage(images['body-detail-v13'],391,379,25,22,663,382,44,40);
  paint.beginPath();paint.moveTo(670,386);paint.bezierCurveTo(676,384,683,384,688,387);
  paint.bezierCurveTo(691,390,692,394,690,398);
  paint.strokeStyle='#503b35';paint.lineWidth=4;paint.lineCap='round';paint.lineJoin='round';paint.stroke();paint.restore();
  // Include both lower abdominal plates above the buckle as well as the pelvic plates.
  paint.save();paint.beginPath();
  paint.moveTo(486,590);paint.bezierCurveTo(500,585,537,585,551,590);
  paint.lineTo(565,626);paint.lineTo(555,648);paint.lineTo(603,650);paint.lineTo(611,710);
  paint.lineTo(568,770);paint.lineTo(551,809);paint.lineTo(490,809);
  paint.lineTo(468,765);paint.lineTo(431,716);paint.lineTo(433,650);
  paint.lineTo(483,648);paint.lineTo(472,626);paint.closePath();paint.clip();
  paint.drawImage(images['body-detail-v13'],0,0,...rig.sourceSize);
  paint.save();paint.beginPath();paint.rect(519,590,49,60);paint.clip();
  paint.translate(1038,0);paint.scale(-1,1);
  paint.drawImage(images['body-detail-v13'],0,0,...rig.sourceSize);paint.restore();
  paint.restore();
  // Discard the old weapon outline inside the empty space between the trouser legs.
  paint.save();paint.globalCompositeOperation='destination-out';paint.beginPath();
  paint.moveTo(500,815);paint.lineTo(540,815);
  paint.bezierCurveTo(544,836,550,858,558,878);
  paint.bezierCurveTo(567,900,579,923,589,946);
  paint.lineTo(602,980);paint.lineTo(440,980);paint.lineTo(454,946);
  paint.bezierCurveTo(466,917,478,897,484,878);
  paint.bezierCurveTo(492,856,497,834,500,815);
  paint.closePath();paint.fill();paint.restore();
}
const headLayers=buildHeadLayers(connectedBody.canvas,images.body,rig.sourceSize);
let headAmount=0;
const upperSourcesV2=Object.fromEntries(armsV2.map(arm=>{
  const cached=surface();paintElbowBacking(cached.context,arm,images,rig.jointSleeves[arm.side]);
  cached.context.drawImage(images[`${arm.side}-upper-v2`],0,0);return [arm.side,cached.canvas];
}));
const rigidUpperSources=Object.fromEntries(armsV2.map(arm=>{
  const cached=surface();cached.context.drawImage(images[`${arm.side}-upper-v2`],0,0);
  if(arm.side==='trigger'){
    const paint=cached.context;
    // Skin continues through the socket; a dark outline here reads as a detached limb.
    paint.save();paint.globalCompositeOperation='source-atop';
    paint.beginPath();paint.moveTo(366,344);paint.lineTo(446,344);
    paint.lineTo(453,373);paint.bezierCurveTo(433,365,392,367,366,382);
    paint.closePath();paint.clip();
    paint.drawImage(images['body-detail-v13'],382,367,50,38,365,340,90,48);
    paint.restore();
    paint.save();paint.globalCompositeOperation='source-atop';paint.beginPath();
    paint.rect(430,355,27,83);paint.clip();
    paint.drawImage(images['body-detail-v13'],391,379,25,22,430,355,27,83);paint.restore();
    // Trim the cap to a continuous curve instead of carrying tiny extraction spikes.
    paint.save();paint.globalCompositeOperation='destination-in';paint.beginPath();
    paint.moveTo(414,347);paint.bezierCurveTo(393,345,379,356,373,371);
    paint.bezierCurveTo(364,392,365,416,370,438);
    paint.lineTo(350,450);paint.lineTo(330,600);paint.lineTo(480,600);
    paint.lineTo(480,440);paint.lineTo(451,423);
    paint.bezierCurveTo(453,392,449,366,435,354);
    paint.bezierCurveTo(429,349,422,347,414,347);paint.closePath();paint.fill();paint.restore();
    paint.save();paint.globalCompositeOperation='source-atop';paint.beginPath();
    paint.moveTo(402.248,347.288);paint.bezierCurveTo(387.72,349.56,377.8,359,373,371);
    paint.bezierCurveTo(364,392,365,416,370,438);
    paint.strokeStyle='#c49381';paint.lineWidth=1.25;paint.lineCap='round';paint.lineJoin='round';
    paint.stroke();paint.restore();
  }else{
    const paint=cached.context;
    paint.save();paint.globalCompositeOperation='source-atop';paint.beginPath();
    paint.ellipse(680,399,18,14,0,0,Math.PI*2);paint.clip();
    paint.drawImage(images['body-detail-v13'],391,379,25,22,661,384,38,30);paint.restore();
    paint.save();paint.globalCompositeOperation='source-atop';paint.beginPath();
    paint.moveTo(660,392);paint.bezierCurveTo(635,411,620,441,614,471);
    paint.lineTo(631,478);paint.bezierCurveTo(639,447,655,419,675,405);paint.closePath();paint.clip();
    paint.drawImage(images['body-detail-v13'],391,379,25,22,610,390,67,90);paint.restore();
    paint.save();paint.globalCompositeOperation='source-atop';paint.beginPath();
    paint.moveTo(666,394);paint.bezierCurveTo(645,407,628,433,620,467);
    paint.strokeStyle='#c49381';paint.lineWidth=1.25;paint.lineCap='round';paint.stroke();paint.restore();
  }
  const pixels=cached.context.getImageData(0,0,...rig.sourceSize).data;
  return [arm.side,{canvas:cached.canvas,bounds:opaqueBounds(pixels,...rig.sourceSize)}];
}));
const foreSurfacesV2=Object.fromEntries(armsV2.map(arm=>{
  const cached=surface(),paint=cached.context;
  paintForearm(paint,arm,images,rig.jointSleeves[arm.side]);
  const pixels=paint.getImageData(0,0,...rig.sourceSize).data;
  return [arm.side,{canvas:cached.canvas,bounds:opaqueBounds(pixels,...rig.sourceSize)}];
}));
const foreSurfacesV5=Object.fromEntries(armsV2.map(arm=>{
  const cached=surface();paintForearm(cached.context,arm,images,rig.jointSleeves[arm.side],foreArt[arm.side]);
  const pixels=cached.context.getImageData(0,0,...rig.sourceSize).data;
  return [arm.side,{canvas:cached.canvas,bounds:opaqueBounds(pixels,...rig.sourceSize)}];
}));
const armSeams=Object.fromEntries(armsV2.map(arm=>{
  const axis=arm.elbow.map((v,i)=>v-arm.shoulder[i]),foreAxis=arm.wrist.map((v,i)=>v-arm.elbow[i]);
  const upper=rigidUpperSources[arm.side].canvas.getContext('2d').getImageData(0,0,...rig.sourceSize).data;
  const fore=foreSurfacesV5[arm.side].canvas.getContext('2d').getImageData(0,0,...rig.sourceSize).data;
  const upperSpan=crossSectionSpan(upper,...rig.sourceSize,arm.shoulder,axis,Math.hypot(...axis)-(rig.armConnectionV2.elbowOverlap?.[arm.side]??0));
  const armorSpan=crossSectionSpan(upper,...rig.sourceSize,arm.shoulder,axis,Math.hypot(...axis)-34);
  const foreSpan=crossSectionSpan(fore,...rig.sourceSize,arm.elbow,foreAxis,foreArt[arm.side].joinAlong);
  foreSurfacesV5[arm.side].targetHalfWidth=armorSpan.halfWidth*foreArt[arm.side].cuffWidthRatio;
  return [arm.side,{upper:upperSpan,fore:foreSpan}];
}));
const activeForearms=()=>$('foreArt').checked?foreSurfacesV5:foreSurfacesV2;
let auditRevision=0;
function invalidateArtAudit(){auditRevision++;$('contactAuditResult').textContent='미검사';}
for(const id of ['armV2','foreArt','projection','pivotRise','trajectory','drop','gripRise','supportInside'])$(id).addEventListener('input',invalidateArtAudit);
$('contactAudit').onclick=async()=>{
  if(!$('armV2').checked){$('contactAuditResult').textContent='새 팔 모드 꺼짐';return;}
  const revision=++auditRevision,settings=structuredClone(rig),mode=$('trajectory').value,projection=$('projection').checked,inside=$('supportInside').checked;
  const canvas=document.createElement('canvas'),paint=canvas.getContext('2d',{willReadFrequently:true});
  let total=0,passed=0;const failures=[];$('contactAudit').disabled=true;
  try{
    for(let degrees=-35;degrees<=0;degrees+=.5){
      const pose=poseAt(degrees*Math.PI/180,settings,mode);
      for(const arm of armsV2){
        const target=wristTarget(arm,pose,settings),fitted=fitArmProjection(arm,target,settings.armConnectionV2,projection);
        const solved=solveArm(fitted,target,{bend:arm.side==='support'&&inside?-arm.bend:arm.bend});
        for(const scale of [.5,1])for(const mirrored of [false,true]){
          const complete=$('foreArt').checked,layers=complete?rigidLayers(arm,solved):undefined;
          const art=layers?.fore??activeForearms()[arm.side];
          const ok=auditForearm(paint,canvas,art,arm,solved,target,scale,mirrored,layers?.upper??connectedUpper(arm,solved),true,false,layers?{center:layers.joint}:undefined);
          total++;if(ok)passed++;else if(failures.length<3)failures.push(`${arm.side==='trigger'?'오른팔':'왼팔'} ${degrees}°`);
        }
      }
      if(revision!==auditRevision)return;
      $('contactAuditResult').textContent=`${passed} / ${total}`;
      await new Promise(requestAnimationFrame);
    }
    const bodyChecks=[[445,530],[450,545],[445,560],[620,530]];
    const bodyPassed=bodyChecks.filter(([x,y])=>connectedBody.context.getImageData(x,y,1,1).data[3]>=220).length;
    const shoulderChecks=[[434,354],[440,380],[445,400],[644,404],[641,425],[635,446]];
    const shouldersPassed=shoulderChecks.filter(([x,y])=>connectedBody.context.getImageData(x,y,1,1).data[3]>=220).length;
    $('contactAuditResult').textContent=`${passed} / ${total}${failures.length?' · 끊김: '+failures.join(', '):' · 통과'} · 몸통 ${bodyPassed}/${bodyChecks.length} · 어깨 ${shouldersPassed}/${shoulderChecks.length}`;
  }finally{$('contactAudit').disabled=false;}
};
$('contactAudit').disabled=false;
const upperSurfacesV2={trigger:surface(),support:surface()};
function connectedUpper(arm,solved){
  const cached=upperSurfacesV2[arm.side],key=[$('foreArt').checked,...solved.elbow.map(v=>v.toFixed(4)),...solved.solved.map(v=>v.toFixed(4))].join(',');
  if(cached.key!==key){
    cached.context.clearRect(0,0,...rig.sourceSize);
    const source=upperSourcesV2[arm.side];
    const mapPoint=p=>anchoredUpperPoint(p,arm,solved,rig.armConnectionV2);
    meshImage(cached.context,source,arm.side==='trigger'?[340,336,140,220]:[600,380,132,216],6,mapPoint);
    cached.key=key;
  }
  return cached.canvas;
}
const rigidLayers=(arm,solved)=>armLayers(arm,solved,rigidUpperSources[arm.side],foreSurfacesV5[arm.side],armSeams[arm.side]);
function meshImage(ctx,image,bounds,step,mapPoint){
  const [left,top,width,height]=bounds;
  for(let y=top;y<top+height;y+=step)for(let x=left;x<left+width;x+=step){
    const right=Math.min(x+step,left+width),bottom=Math.min(y+step,top+height);
    const points=[[x,y],[right,y],[x,bottom],[right,bottom]];
    const mapped=points.map(mapPoint);
    for(const indices of [[0,1,2],[3,2,1]]){
      const [a,b,c]=indices.map(i=>points[i]),[u,v,w]=indices.map(i=>mapped[i]);
      const det=(b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0]);
      const A=((v[0]-u[0])*(c[1]-a[1])-(w[0]-u[0])*(b[1]-a[1]))/det;
      const B=((v[1]-u[1])*(c[1]-a[1])-(w[1]-u[1])*(b[1]-a[1]))/det;
      const C=((w[0]-u[0])*(b[0]-a[0])-(v[0]-u[0])*(c[0]-a[0]))/det;
      const D=((w[1]-u[1])*(b[0]-a[0])-(v[1]-u[1])*(c[0]-a[0]))/det;
      const center=[(u[0]+v[0]+w[0])/3,(u[1]+v[1]+w[1])/3];
      ctx.save();ctx.beginPath();
      for(const [i,p] of [u,v,w].entries()){
        const delta=[p[0]-center[0],p[1]-center[1]],n=Math.hypot(...delta);
        const q=p.map((value,j)=>value+delta[j]/n*1.5);
        if(i===0)ctx.moveTo(...q);else ctx.lineTo(...q);
      }
      ctx.closePath();ctx.clip();ctx.transform(A,B,C,D,u[0]-A*a[0]-C*a[1],u[1]-B*a[0]-D*a[1]);
      ctx.drawImage(image,...bounds,...bounds);ctx.restore();
    }
  }
}
function drawUpper(arm,projected,solved,torso,shoulders){
  const cached=upperSurfaces[arm.side];
  const key=[...projected.shoulder,...solved.elbow,torso.angle*1000].map(v=>v.toFixed(2)).join(',');
  if(key!==cached.key){
    cached.context.clearRect(0,0,...rig.sourceSize);
    const bounds=arm.side==='support'?[608,372,120,216]:[360,336,120,180];
    meshImage(cached.context,images[`${arm.side}-upper`],bounds,12,p=>upperArmPoint(p,arm,projected,solved,torso,shoulders,rig));
    cached.key=key;
  }
  ctx.drawImage(cached.canvas,0,0);
}
function clipBehindHead(paint){
  paint.beginPath();paint.rect(0,0,...rig.sourceSize);
  const outline=[[0,0],[1024,0],[1024,330],[705,330],[690,370],[635,390],[550,383],[480,365],[460,335],[350,335],[350,430],[0,430]];
  outline.forEach((p,i)=>i?paint.lineTo(...p):paint.moveTo(...p));paint.closePath();paint.clip('evenodd');
}
function drawBody(torso,shoulders){
  const active=Object.values(shoulders).some(offset=>offset.some(Boolean));
  if(!torso.angle&&!active){ctx.drawImage(images.body,0,0);return;}
  const key=[torso.angle.toFixed(7),...shoulders.support,...shoulders.trigger].join(',');
  if(key!==bodySurface.key){
    const paint=bodySurface.context;paint.clearRect(0,0,...rig.sourceSize);
    if(active){
      paint.drawImage(socketSource.canvas,0,0);paint.save();clipBehindHead(paint);
      for(const bounds of [[330,372,150,168],[610,372,150,168]]){
        paint.clearRect(...bounds);
        meshImage(paint,socketSource.canvas,bounds,12,p=>shoulderBodyPoint(p,shoulders,rig));
      }
      paint.restore();
    }else{
      meshImage(paint,images.body,[0,0,1024,704],32,p=>torsoPoint(p,torso));
      paint.drawImage(images.body,0,704,1024,832,0,704,1024,832);
    }
    bodySurface.key=key;
  }
  if(active){
    // Composite only the socket patches; untouched hair, face and legs keep their original sampling.
    ctx.drawImage(images.body,0,0);ctx.save();ctx.beginPath();
    ctx.rect(330,330,150,246);ctx.rect(610,300,150,276);ctx.clip();clipBehindHead(ctx);
    ctx.clearRect(0,0,...rig.sourceSize);ctx.fillStyle='#253741';ctx.fillRect(0,0,...rig.sourceSize);
    ctx.drawImage(bodySurface.canvas,0,0);ctx.restore();
  }else ctx.drawImage(bodySurface.canvas,0,0);
}
let sweepKey='',sweep;
function drawWeaponPaths(){
  if(!$('paths').checked)return;
  const key=[$('trajectory').value,rig.raisedPivot.rise,...rig.placementOffset].join(',');
  if(key!==sweepKey){sweep=weaponSweep(rig,$('trajectory').value);sweepKey=key;}
  const trace=points=>points.forEach((p,i)=>i?ctx.lineTo(...p):ctx.moveTo(...p));
  ctx.save();ctx.lineWidth=1.2/scale;
  ctx.beginPath();trace(sweep.stock);for(const point of [...sweep.muzzle].reverse())ctx.lineTo(...point);
  ctx.closePath();ctx.fillStyle='#80b7bd0b';ctx.fill();
  for(const [points,color] of [[sweep.stock,'#eac263'],[sweep.muzzle,'#80b7bd']]){
    ctx.beginPath();trace(points);ctx.strokeStyle=color;ctx.stroke();
    for(const point of [points[0],points.at(-1)]){ctx.beginPath();ctx.arc(...point,2.5/scale,0,Math.PI*2);ctx.fillStyle=color;ctx.fill();}
  }
  ctx.setLineDash([4/scale,4/scale]);ctx.strokeStyle='#b4c9cf80';ctx.beginPath();
  for(const i of [0,70]){ctx.moveTo(...sweep.stock[i]);ctx.lineTo(...sweep.muzzle[i]);}ctx.stroke();ctx.setLineDash([]);
  const [x,y]=sweep.pivot;ctx.strokeStyle='#eac263';ctx.beginPath();
  ctx.moveTo(x-5/scale,y);ctx.lineTo(x+5/scale,y);ctx.moveTo(x,y-5/scale);ctx.lineTo(x,y+5/scale);ctx.stroke();
  ctx.restore();
}
function render(dt){
  now+=dt;const {flipped,localPose,globalPose}=calculate();pose=globalPose;if(held||$('auto').checked)fire();pool.step(dt);flash=Math.max(0,flash-dt);
  ctx.clearRect(0,0,W,H);ctx.fillStyle='#253741';ctx.fillRect(0,0,W,H);
  ctx.strokeStyle='#38505b';ctx.lineWidth=1;ctx.beginPath();ctx.moveTo(0,H*.88);ctx.lineTo(W,H*.88);ctx.stroke();
  ctx.save();ctx.translate(...origin);ctx.scale(scale,scale);
  if(flipped){ctx.translate(2*rig.referenceAxisX,0);ctx.scale(-1,1);}
  const connected=$('armV2').checked;
  const headEnabled=connected&&$('headFollow').checked;
  headAmount=headEnabled?easeHead(headAmount,headAimAmount(localPose.angle,rig.headAim),dt,rig.headAim.response):0;
  const movingHead=headAmount>1e-4;
  const torso=torsoPose(localPose,rig,!connected&&$('torso').checked);
  const shoulders=shoulderPose(localPose,rig,!connected&&$('shoulders').checked);
  const shoulderActive=Object.values(shoulders).some(offset=>offset.some(Boolean));
  if(connected)ctx.drawImage(movingHead?headLayers.body:connectedBody.canvas,0,0);else drawBody(torso,shoulders);maxGap=0;
  let supportGap=0,triggerGap=0,supportFlex=0,supportScale=1,triggerScale=1;
  const solutions=[];
  ctx.save();if(!torso.angle&&!movingHead)clipBehindHead(ctx);
  const activeArms=connected?armsV2:rig.arms;
  for(const arm of $('joints').checked?activeArms:[]){
    const target=wristTarget(arm,localPose,rig);
    const projected=connected?fitArmProjection(arm,target,rig.armConnectionV2,$('projection').checked):shoulderArm(torsoArm(projectArm(arm,localPose,rig,$('projection').checked),torso),shoulders);
    const bend=arm.side==='support'&&$('supportInside').checked?-arm.bend:arm.bend;
    const solved=solveArm(projected,target,{bend});solutions.push({arm:projected,target,solved});maxGap=Math.max(maxGap,solved.gap);
    if(arm.side==='support'){supportGap=solved.gap;supportFlex=elbowFlex(projected,solved);supportScale=projected.projectionScale??1;}else{triggerGap=solved.gap;triggerScale=projected.projectionScale??1;}
    if(connected){
      if($('foreArt').checked){
        const layers=rigidLayers(arm,solved);
        drawArmArt(ctx,layers.fore);drawArmArt(ctx,layers.upper);
      }else{
        limbPart('',arm.elbow,arm.wrist,solved.elbow,solved.solved,activeForearms()[arm.side].canvas);
        ctx.drawImage(connectedUpper(arm,solved),0,0);
      }
    }else{
      if(torso.angle||shoulderActive||(arm.side==='support'&&$('projection').checked))drawUpper(arm,projected,solved,torso,shoulders);
      else limbPart(`${arm.side}-upper`,arm.shoulder,arm.elbow,projected.shoulder,solved.elbow);
      limbPart(`${arm.side}-fore`,arm.elbow,arm.wrist,solved.elbow,solved.solved);
    }
  }
  ctx.restore();
  if(movingHead)drawAimedHead(ctx,headLayers,headAmount,rig.headAim);
  if($('joints').checked)for(const arm of activeArms)part(`${arm.side}-hand${connected?'-v2':''}`,localPose.imagePivot,handPlacement(arm,localPose,rig),localPose.angle);
  part('weapon',localPose.imagePivot,localPose.pivot,localPose.angle);
  drawWeaponPaths();
  if($('joints').checked&&$('guides').checked){
    ctx.lineWidth=3;for(const {arm,target,solved} of solutions){ctx.strokeStyle=solved.gap>2?'#ef9d84':'#5be0b9';ctx.beginPath();ctx.moveTo(...arm.shoulder);ctx.lineTo(...solved.elbow);ctx.lineTo(...solved.solved);ctx.stroke();for(const p of [arm.shoulder,solved.elbow,target]){ctx.fillStyle='#eac263';ctx.beginPath();ctx.arc(...p,7,0,Math.PI*2);ctx.fill();}}
  }
  ctx.restore();
  for(const t of targets){
    t.flash=Math.max(0,t.flash-dt);ctx.strokeStyle=t.flash?'#eac263':'#80b7bd';ctx.lineWidth=2;ctx.beginPath();ctx.arc(t.x,t.y,t.r,0,Math.PI*2);ctx.stroke();ctx.beginPath();ctx.arc(t.x,t.y,t.r*.45,0,Math.PI*2);ctx.stroke();
  }
  for(const p of pool.items){if(!p.active)continue;
    for(const t of targets){if(segmentHitsCircle([p.oldX,p.oldY],[p.x,p.y],[t.x,t.y],t.r)){p.active=false;t.flash=.2;hits++;break;}}
    if(!p.active)continue;ctx.strokeStyle='#f1d674';ctx.lineWidth=2;ctx.beginPath();ctx.moveTo(p.x-p.vx*.012,p.y-p.vy*.012);ctx.lineTo(p.x,p.y);ctx.stroke();
    if(p.x<0||p.x>W||p.y<0||p.y>H)p.active=false;
  }
  const m=screen(pose.muzzle);if(flash){ctx.fillStyle='#ffe59a';ctx.beginPath();ctx.arc(...m,5,0,Math.PI*2);ctx.fill();}
  const p=screen(pointer);ctx.strokeStyle=pose.inRange?'#d4ebe9':'#db8b74';ctx.lineWidth=1.5;ctx.beginPath();ctx.arc(...p,8,0,Math.PI*2);ctx.moveTo(p[0]-13,p[1]);ctx.lineTo(p[0]+13,p[1]);ctx.moveTo(p[0],p[1]-13);ctx.lineTo(p[0],p[1]+13);ctx.stroke();
  $('angle').textContent=`${(-90-pose.angle*180/Math.PI).toFixed(1)}°`;
  $('shots').textContent=shots;$('hits').textContent=hits;$('error').textContent=$('joints').checked?`${supportGap.toFixed(1)} px`:'미검증';
  $('triggerError').textContent=$('joints').checked?`${triggerGap.toFixed(1)} px`:'미검증';
  $('pool').textContent=`${pool.items.filter(p=>p.active).length} / 96`;
  $('lean').textContent=`${Math.abs(torso.angle*180/Math.PI).toFixed(1)}°`;
  $('flex').textContent=$('joints').checked?`${supportFlex.toFixed(1)}°`:'미검증';
  $('triggerScale').textContent=connected&&$('joints').checked?`${Math.round(triggerScale*100)}%`:'-';
  $('supportScale').textContent=connected&&$('joints').checked?`${Math.round(supportScale*100)}%`:'-';
  $('state').textContent=!pose.inRange?'조준 범위 밖':supportGap>2?'왼팔 도달 범위 초과':triggerGap>2?'오른팔 도달 범위 초과':pose.nearAim?'근거리 조준':connected?'팔 연결 시험':'총 궤적 확인';
}
let previous=performance.now(),lastMeasuredPose='',peakFrameMs=0;
function frame(time){
  const dt=Math.min((time-previous)/1000,.04);previous=time;
  const start=performance.now();render(dt);const cost=performance.now()-start;
  const key=[pose.angle,$('foreArt').checked,$('armV2').checked,rig.raisedPivot.rise,rig.placementOffset[1],rig.gripOffsets.support[1]].join(',');
  if(key!==lastMeasuredPose){canvas.dataset.poseBuildMs=cost.toFixed(2);lastMeasuredPose=key;}
  peakFrameMs=Math.max(peakFrameMs,cost);canvas.dataset.frameMs=cost.toFixed(2);canvas.dataset.framePeakMs=peakFrameMs.toFixed(2);
  requestAnimationFrame(frame);
}
resize();requestAnimationFrame(frame);
window.shootingSandbox={setAim(x,y){pointer=[x,y];},fire(){lastShot=-1;fire();},snapshot(){return{shots,hits,maxGap,angle:pose.angle,muzzle:pose.muzzle,poolActive:pool.items.filter(p=>p.active).length,ready:true};}};
if(new URLSearchParams(location.search).has('jointReview')){
  const {showJointReview}=await import('./joint-review.mjs');
  showJointReview({rig,arms:armsV2,body:connectedBody.canvas,images,layers:rigidLayers,clipBehindHead,headLayers});
}
if(new URLSearchParams(location.search).has('unityExport')){
  const {exportUnityPose}=await import('./unity-export.mjs');
  await exportUnityPose({rig,arms:armsV2,body:connectedBody.canvas,headLayers,images,upper:rigidUpperSources,fore:foreSurfacesV5,seams:armSeams,layers:rigidLayers});
}
