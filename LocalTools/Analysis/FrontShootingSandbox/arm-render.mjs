import {clamp} from './geometry.mjs';

export const transformPoint=(point,matrix)=>[
  matrix[0]*point[0]+matrix[2]*point[1]+matrix[4],
  matrix[1]*point[0]+matrix[3]*point[1]+matrix[5]
];

export function segmentMatrix(from,end,to,target,widthScale=1){
  const source=end.map((v,i)=>v-from[i]),destination=target.map((v,i)=>v-to[i]);
  const size=Math.hypot(...source),targetSize=Math.hypot(...destination);
  const u=source.map(v=>v/size),n=[-u[1],u[0]],v=destination.map(x=>x/targetSize),normal=[-v[1],v[0]],ratio=targetSize/size;
  const a=v[0]*ratio*u[0]+normal[0]*widthScale*n[0],b=v[1]*ratio*u[0]+normal[1]*widthScale*n[0];
  const c=v[0]*ratio*u[1]+normal[0]*widthScale*n[1],d=v[1]*ratio*u[1]+normal[1]*widthScale*n[1];
  return [a,b,c,d,to[0]-a*from[0]-c*from[1],to[1]-b*from[0]-d*from[1]];
}

function sectionPoint(origin,axis,section){
  const size=Math.hypot(...axis),u=axis.map(v=>v/size),n=[-u[1],u[0]];
  return origin.map((v,i)=>v+u[i]*section.along+n[i]*section.center);
}

export function armLayers(arm,solved,upper,fore,seam){
  const upperMatrix=segmentMatrix(arm.shoulder,arm.elbow,arm.shoulder,solved.elbow);
  const upperBone=arm.elbow.map((v,i)=>v-arm.shoulder[i]),foreBone=arm.wrist.map((v,i)=>v-arm.elbow[i]);
  const joint=transformPoint(sectionPoint(arm.shoulder,upperBone,seam.upper),upperMatrix);
  const from=sectionPoint(arm.elbow,foreBone,seam.fore);
  // Each armor piece stays affine and straight; overlap is hidden underneath the original elbow cap.
  const widthScale=fore.targetHalfWidth?fore.targetHalfWidth/seam.fore.halfWidth:clamp(seam.upper.halfWidth/seam.fore.halfWidth,.9,1.25)*(fore.thicknessScale??1);
  const lowerMatrix=segmentMatrix(from,arm.wrist,joint,solved.solved,widthScale);
  return {upper:{...upper,matrix:upperMatrix},fore:{...fore,matrix:lowerMatrix},joint,widthScale};
}

export function drawArmArt(paint,art){
  const [left,top,right,bottom]=art.bounds;
  paint.save();paint.transform(...art.matrix);
  paint.drawImage(art.canvas,left,top,right-left,bottom-top,left,top,right-left,bottom-top);
  paint.restore();
}
