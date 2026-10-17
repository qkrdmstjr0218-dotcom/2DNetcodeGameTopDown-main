import {drawArmArt,transformPoint} from './arm-render.mjs?v=rigid-arm-9';

export function hasAlphaPath(data,width,height,from,to,threshold=64){
  const index=point=>{
    const [x,y]=point.map(Math.round);
    return x>=0&&y>=0&&x<width&&y<height?y*width+x:-1;
  };
  const start=index(from),end=index(to);
  if(start<0||end<0||data[start*4+3]<threshold||data[end*4+3]<threshold)return false;
  const seen=new Uint8Array(width*height),queue=new Int32Array(width*height);
  let head=0,tail=1;queue[0]=start;seen[start]=1;
  while(head<tail){
    const p=queue[head++];if(p===end)return true;
    const x=p%width;
    for(const next of [x? p-1:-1,x<width-1?p+1:-1,p-width,p+width]){
      if(next<0||next>=seen.length||seen[next]||data[next*4+3]<threshold)continue;
      seen[next]=1;queue[tail++]=next;
    }
  }
  return false;
}

export function hasOpaqueDisc(data,width,height,center,radius,threshold=180){
  const [cx,cy]=center;
  for(let y=Math.floor(cy-radius);y<=Math.ceil(cy+radius);y++)for(let x=Math.floor(cx-radius);x<=Math.ceil(cx+radius);x++){
    if((x-cx)**2+(y-cy)**2>radius**2)continue;
    if(x<0||y<0||x>=width||y>=height||data[(y*width+x)*4+3]<threshold)return false;
  }
  return true;
}

export function paintForearm(paint,arm,images,sleeve,complete){
  if(complete){
    const source=complete.wrist.map((v,i)=>v-complete.elbow[i]),target=arm.wrist.map((v,i)=>v-arm.elbow[i]);
    const a=Math.atan2(source[1],source[0]),b=Math.atan2(target[1],target[0]);
    paint.save();paint.translate(...arm.elbow);paint.rotate(b);
    paint.beginPath();paint.rect(complete.joinAlong-complete.joinOverlap,-200,Math.hypot(...target)+200,400);paint.clip();
    paint.scale(Math.hypot(...target)/Math.hypot(...source),complete.widthScale*(complete.mirror?-1:1));
    paint.rotate(-a);paint.translate(-complete.elbow[0],-complete.elbow[1]);
    paint.beginPath();paint.rect(...complete.clip);paint.clip();paint.drawImage(images[complete.image],0,0);paint.restore();
    if(complete.includeHandPatch!==false){
      paint.save();paint.beginPath();paint.arc(...arm.wrist,16,0,Math.PI*2);paint.clip();
      paint.drawImage(images[`${arm.side}-hand-v2`],0,0);paint.restore();
    }
    return;
  }
  paintElbowBacking(paint,arm,images,sleeve);
  const delta=sleeve.socket.map((v,i)=>v-arm.elbow[i]),distance=Math.hypot(...delta);
  // Extend the original black joint fabric underneath the armor, not a detached joint island.
  paint.save();paint.translate(...arm.elbow);paint.rotate(Math.atan2(delta[1],delta[0]));
  paint.beginPath();paint.roundRect(-8,-sleeve.halfWidth,distance+16,sleeve.halfWidth*2,sleeve.halfWidth);paint.clip();
  paint.drawImage(images[`${arm.side}-upper-v2`],...sleeve.texture,-8,-sleeve.halfWidth,distance+16,sleeve.halfWidth*2);
  paint.restore();paint.drawImage(images[`${arm.side}-fore-v2`],0,0);
  paint.save();paint.beginPath();paint.arc(...arm.wrist,20,0,Math.PI*2);paint.clip();
  paint.drawImage(images[`${arm.side}-hand-v2`],0,0);paint.restore();
}

export function paintUpperSource(paint,arm,image,sleeve){
  const delta=arm.elbow.map((v,i)=>v-arm.shoulder[i]),size=Math.hypot(...delta),a=Math.atan2(delta[1],delta[0]);
  paint.save();paint.translate(...arm.shoulder);paint.rotate(a);
  // Both the cloth backing and the visible cuff stop at the same registered seam.
  paint.beginPath();paint.rect(-200,-200,size+200-32,400);paint.clip();
  paint.drawImage(image,...sleeve.texture,size-52,-22,20,44);
  paint.rotate(-a);paint.translate(-arm.shoulder[0],-arm.shoulder[1]);paint.drawImage(image,0,0);paint.restore();
}

export function paintShoulderSocket(paint,arm,image){
  const bone=arm.elbow.map((v,i)=>v-arm.shoulder[i]),angle=Math.atan2(bone[1],bone[0]);
  const outline=arm.side==='trigger'?[[400,338],[448,338],[466,365],[466,419],[450,435],[427,425],[416,392]]:[[637,370],[686,382],[681,428],[660,463],[631,448],[622,409]];
  paint.save();paint.beginPath();outline.forEach((p,i)=>i?paint.lineTo(...p):paint.moveTo(...p));paint.closePath();paint.clip();
  paint.translate(...arm.shoulder);paint.rotate(angle);
  // Restore original inner shoulder skin and strap pixels beneath the moving upper arm.
  paint.beginPath();paint.rect(-65,arm.side==='trigger'?-58:-8,90,66);paint.clip();
  paint.scale(1,1.18);paint.rotate(-angle);paint.translate(-arm.shoulder[0],-arm.shoulder[1]);
  paint.drawImage(image,0,0);paint.restore();
}

export function crossSectionSpan(data,width,height,origin,axis,along,limit=110){
  const size=Math.hypot(...axis),unit=axis.map(v=>v/size),normal=[-unit[1],unit[0]],runs=[];
  for(let across=-limit;across<=limit;across++){
    const x=Math.round(origin[0]+unit[0]*along+normal[0]*across),y=Math.round(origin[1]+unit[1]*along+normal[1]*across);
    if(x<0||y<0||x>=width||y>=height||data[(y*width+x)*4+3]<180)continue;
    const previous=runs.at(-1);
    if(previous&&previous[1]===across-1)previous[1]=across;else runs.push([across,across]);
  }
  const span=runs.sort((a,b)=>(b[1]-b[0])-(a[1]-a[0]))[0];
  if(!span||span[1]-span[0]<8)throw new Error('No solid arm seam cross-section.');
  return {along,center:(span[0]+span[1])/2,halfWidth:(span[1]-span[0])/2};
}

export function hasOpaqueCorridor(data,width,height,from,to,halfWidth,threshold=180){
  const delta=to.map((v,i)=>v-from[i]),size=Math.hypot(...delta),normal=[-delta[1]/size,delta[0]/size];
  for(let i=1;i<20;i++)for(let j=-halfWidth;j<=halfWidth;j++){
    const p=from.map((v,k)=>Math.round(v+delta[k]*i/20+normal[k]*j));
    if(p[0]<0||p[1]<0||p[0]>=width||p[1]>=height||data[(p[1]*width+p[0])*4+3]<threshold)return false;
  }
  return true;
}

export function paintElbowBacking(paint,arm,images,sleeve){
  const r=sleeve.elbowRadius,[x,y]=arm.elbow;
  paint.save();paint.beginPath();paint.arc(x,y,r,0,Math.PI*2);paint.clip();
  paint.drawImage(images[`${arm.side}-upper-v2`],...sleeve.texture,x-r,y-r,r*2,r*2);paint.restore();
}

export function opaqueBounds(data,width,height){
  let left=width,top=height,right=0,bottom=0;
  for(let y=0;y<height;y++)for(let x=0;x<width;x++)if(data[(y*width+x)*4+3]){
    left=Math.min(left,x);top=Math.min(top,y);right=Math.max(right,x+1);bottom=Math.max(bottom,y+1);
  }
  return [left,top,right,bottom];
}

export function limbPaintPoint(point,arm,solved){
  const source=arm.wrist.map((v,i)=>v-arm.elbow[i]),target=solved.solved.map((v,i)=>v-solved.elbow[i]);
  const a=Math.atan2(source[1],source[0]),b=Math.atan2(target[1],target[0]),ratio=Math.hypot(...target)/Math.hypot(...source);
  const dx=point[0]-arm.elbow[0],dy=point[1]-arm.elbow[1];
  const along=(dx*Math.cos(a)+dy*Math.sin(a))*ratio,across=-dx*Math.sin(a)+dy*Math.cos(a);
  return [solved.elbow[0]+along*Math.cos(b)-across*Math.sin(b),solved.elbow[1]+along*Math.sin(b)+across*Math.cos(b)];
}

export function auditForearm(paint,canvas,art,arm,solved,target,scale=1,mirrored=false,upper,checkWidth=false,upperBehind=false,seam){
  if(Math.hypot(...solved.solved.map((v,i)=>v-target[i]))>2)return false;
  const [left,top,right,bottom]=art.bounds;
  const points=[[left,top],[right,top],[left,bottom],[right,bottom]].map(p=>art.matrix?transformPoint(p,art.matrix):art.posed?p:limbPaintPoint(p,arm,solved));
  if(upper?.matrix){const [l,t,r,b]=upper.bounds;for(const p of [[l,t],[r,t],[l,b],[r,b]])points.push(transformPoint(p,upper.matrix));}
  if(upper)for(const p of [arm.shoulder,solved.elbow])for(const dx of [-90,90])for(const dy of [-90,90])points.push([p[0]+dx,p[1]+dy]);
  const x=Math.floor(Math.min(...points.map(p=>p[0])))-4,y=Math.floor(Math.min(...points.map(p=>p[1])))-4;
  const width=Math.ceil((Math.max(...points.map(p=>p[0]))-x+4)*scale),height=Math.ceil((Math.max(...points.map(p=>p[1]))-y+4)*scale);
  canvas.width=width;canvas.height=height;
  const drawUpper=()=>{
    paint.save();if(mirrored){paint.translate(width,0);paint.scale(-1,1);}paint.scale(scale,scale);paint.translate(-x,-y);
    if(upper.matrix)drawArmArt(paint,upper);else paint.drawImage(upper,0,0);paint.restore();
  };
  if(upper&&upperBehind)drawUpper();
  paint.save();if(mirrored){paint.translate(width,0);paint.scale(-1,1);}paint.scale(scale,scale);paint.translate(-x,-y);
  const sourceAngle=Math.atan2(arm.wrist[1]-arm.elbow[1],arm.wrist[0]-arm.elbow[0]);
  const targetAngle=Math.atan2(solved.solved[1]-solved.elbow[1],solved.solved[0]-solved.elbow[0]);
  const ratio=Math.hypot(...solved.solved.map((v,i)=>v-solved.elbow[i]))/Math.hypot(...arm.wrist.map((v,i)=>v-arm.elbow[i]));
  if(art.matrix)drawArmArt(paint,art);else{
    if(!art.posed){paint.translate(...solved.elbow);paint.rotate(targetAngle);paint.scale(ratio,1);paint.rotate(-sourceAngle);paint.translate(-arm.elbow[0],-arm.elbow[1]);}
    paint.drawImage(art.canvas,0,0);
  }paint.restore();
  if(upper&&!upperBehind)drawUpper();
  const locate=point=>{const px=(point[0]-x)*scale;return [mirrored?width-px:px,(point[1]-y)*scale];};
  const pixels=paint.getImageData(0,0,width,height).data;
  if(!hasAlphaPath(pixels,width,height,locate(upper?arm.shoulder:solved.elbow),locate(target)))return false;
  if(checkWidth&&!hasOpaqueCorridor(pixels,width,height,locate(seam?.center??solved.elbow),locate(target),10*scale))return false;
  if(seam?.axis){
    const edge=sign=>locate(seam.center.map((v,i)=>v+seam.axis[i]*3*sign));
    if(!hasOpaqueCorridor(pixels,width,height,edge(-1),edge(1),Math.max(2,seam.halfWidth-4)*scale))return false;
  }
  // A one-pixel bridge can pass connectivity while still leaving the elbow visibly hollow.
  return !upper||hasOpaqueDisc(pixels,width,height,locate(seam?.center??solved.elbow),8*scale);
}
