import {clamp} from './geometry.mjs';

export const headAimAmount=(gunAngle,config)=>clamp(-gunAngle/(config.gunDegrees*Math.PI/180),0,1);
export const easeHead=(current,target,dt,response)=>current+(target-current)*(1-Math.exp(-response*Math.max(0,dt)));

export function buildHeadLayers(body,source,size){
  const make=()=>{const c=document.createElement('canvas');[c.width,c.height]=size;return c;};
  const head=make(),p=head.getContext('2d');
  p.beginPath();p.moveTo(0,0);p.lineTo(1024,0);p.lineTo(1024,325);
  p.lineTo(710,325);p.lineTo(690,345);p.lineTo(662,366);p.lineTo(640,379);
  p.lineTo(618,384);p.bezierCurveTo(595,391,573,405,550,400);
  p.bezierCurveTo(520,395,491,381,480,365);p.lineTo(463,350);p.lineTo(459,332);
  p.lineTo(434,328);p.lineTo(420,330);p.lineTo(411,343);p.lineTo(395,377);
  p.lineTo(380,403);p.lineTo(363,419);p.lineTo(340,425);p.lineTo(0,430);p.closePath();p.clip();
  p.drawImage(source,0,0);
  const pixels=p.getImageData(0,0,...size),[width,height]=size;
  // Keep the connected head/hair island; exclude isolated old shoulder pixels inside the crop.
  const seen=new Uint8Array(width*height),queue=new Int32Array(width*height);
  const seed=200*width+545;queue[0]=seed;seen[seed]=1;let end=1;
  for(let start=0;start<end;start++){
    const index=queue[start],x=index%width;
    for(const next of [x?index-1:-1,x<width-1?index+1:-1,index-width,index+width]){
      if(next<0||next>=seen.length||seen[next]||!pixels.data[next*4+3])continue;
      seen[next]=1;queue[end++]=next;
    }
  }
  const base=make(),b=base.getContext('2d');b.drawImage(body,0,0);
  const basePixels=b.getImageData(0,0,...size);let left=width,top=height,right=0,bottom=0;
  for(let i=0;i<seen.length;i++){
    if(!seen[i])pixels.data[i*4+3]=0;
    else if(pixels.data[i*4+3]){
      basePixels.data[i*4+3]=0;
      const x=i%width,y=Math.floor(i/width);left=Math.min(left,x);top=Math.min(top,y);right=Math.max(right,x+1);bottom=Math.max(bottom,y+1);
    }
  }
  p.putImageData(pixels,0,0);b.putImageData(basePixels,0,0);
  b.save();b.globalCompositeOperation='destination-over';b.beginPath();
  b.moveTo(503,374);b.bezierCurveTo(530,397,566,414,610,378);
  b.lineTo(610,402);b.bezierCurveTo(580,421,533,419,505,396);b.closePath();b.clip();
  b.drawImage(source,530,411,30,12,500,373,114,49);b.restore();
  return {body:base,head,bounds:[left,top,right,bottom]};
}

export function drawAimedHead(paint,layers,amount,config){
  const [x,y]=config.pivot,[left,top,right,bottom]=layers.bounds;
  paint.save();paint.translate(x+config.offset[0]*amount,y+config.offset[1]*amount);
  // Canvas has a downward Y axis: negative rotation matches Unity's positive Z turn.
  paint.rotate(-config.maxDegrees*Math.PI/180*amount);paint.translate(-x,-y);
  paint.drawImage(layers.head,left,top,right-left,bottom-top,left,top,right-left,bottom-top);paint.restore();
}
