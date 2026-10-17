import assert from 'node:assert/strict';
import fs from 'node:fs';
import {hasAlphaPath,hasOpaqueDisc,hasOpaqueCorridor,opaqueBounds,limbPaintPoint,paintForearm} from './arm-contact.mjs';
import {aimPose,poseAt} from './geometry.mjs';
const width=7,height=7,pixels=new Uint8ClampedArray(width*height*4);
const set=(x,y,a=255)=>pixels[(y*width+x)*4+3]=a;
for(let x=1;x<=5;x++)set(x,3);
assert.ok(hasAlphaPath(pixels,width,height,[1,3],[5,3]));
assert.deepEqual(opaqueBounds(pixels,width,height),[1,3,6,4]);
set(3,3,0);assert.equal(hasAlphaPath(pixels,width,height,[1,3],[5,3]),false);
set(3,3,63);assert.equal(hasAlphaPath(pixels,width,height,[1,3],[5,3]),false);
set(3,3,64);assert.ok(hasAlphaPath(pixels,width,height,[1,3],[5,3]));
assert.equal(hasAlphaPath(pixels,width,height,[-1,3],[5,3]),false);
assert.equal(hasAlphaPath(pixels,width,height,[1,3],[7,3]),false);
pixels.fill(0);set(0,0);set(1,1);
assert.equal(hasAlphaPath(pixels,width,height,[0,0],[1,1]),false,'A corner touch is not a robust painted joint.');
pixels.fill(255);assert.ok(hasOpaqueDisc(pixels,width,height,[3,3],2));
set(3,3,0);assert.equal(hasOpaqueDisc(pixels,width,height,[3,3],2),false,'An opaque ring must not hide an empty elbow center.');
set(3,3,179);assert.equal(hasOpaqueDisc(pixels,width,height,[3,3],2),false);
set(3,3,180);assert.ok(hasOpaqueDisc(pixels,width,height,[3,3],2));
pixels.fill(255);assert.ok(hasOpaqueCorridor(pixels,width,height,[1,3],[5,3],1));
set(3,2,0);assert.equal(hasOpaqueCorridor(pixels,width,height,[1,3],[5,3],1),false,'A connected centerline must not conceal a missing inner-arm surface.');
const arm={elbow:[10,10],wrist:[10,20]},solved={elbow:[100,200],solved:[120,200]};
for(const [source,destination] of [[arm.elbow,solved.elbow],[arm.wrist,solved.solved],[[12,15],[110,198]]]){
  const p=limbPaintPoint(source,arm,solved);assert.ok(Math.hypot(...p.map((v,i)=>v-destination[i]))<1e-8);
}
const rig=JSON.parse(fs.readFileSync(new URL('./rig.json',import.meta.url)));
const foreConfig=JSON.parse(fs.readFileSync(new URL('./forearm-art-v9.json',import.meta.url)));
for(const [side,config] of Object.entries(foreConfig)){
  const drawn=[],paint={save(){},restore(){},translate(){},rotate(){},beginPath(){},rect(){},clip(){},scale(){},arc(){},drawImage(image){drawn.push(image);}};
  const images={[config.image]:'gauntlet',[`${side}-hand-v2`]:'hand'};
  paintForearm(paint,{...arm,side},images,{},config);
  assert.deepEqual(drawn,['gauntlet'],'Enlarging a gauntlet must never enlarge a copied hand patch.');
}
const range=[-35*Math.PI/180,0];let sectorCases=0;
for(let degrees=-35;degrees<=0;degrees+=.25){
  const p=poseAt(degrees*Math.PI/180,rig,'raised-body');
  const pointer=p.muzzle.map((v,i)=>v+p.ray[i]*1000),q=aimPose(pointer,rig,'raised-body',range);
  assert.ok(q.inRange&&Math.abs(q.angle-p.angle)<1e-8);sectorCases++;
}
for(const pointer of [[519,-1000],[1000,-500],[700,100]]){
  const p=aimPose(pointer,rig,'raised-body',range);
  assert.equal(p.inRange,false);
  assert.ok(p.angle>=range[0]&&p.angle<=range[1],'Out-of-range mouse positions must not select the opposite anatomical pose.');
}
let nearCases=0;
for(let degrees=-35;degrees<=0;degrees+=.5){
  const p=poseAt(degrees*Math.PI/180,rig,'raised-body');
  for(const offset of [-700,-500,-100,0,100]){
    const pointer=p.muzzle.map((v,i)=>v+p.ray[i]*offset),q=aimPose(pointer,rig,'raised-body',range);
    assert.ok(q.inRange&&Math.abs(q.angle-p.angle)<1e-8,`Near aim must follow the same line at ${degrees}/${offset}`);nearCases++;
  }
}
console.log(JSON.stringify({alphaConnectivityTestsPassed:true,paintTransformMatchesBones:true,sectorCases,nearCases,outOfRangePoseGuardPassed:true}));
