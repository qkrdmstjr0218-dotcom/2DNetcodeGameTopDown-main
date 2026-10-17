function surface(width,height){
  const canvas=document.createElement('canvas');canvas.width=width;canvas.height=height;
  return {canvas,ctx:canvas.getContext('2d',{willReadFrequently:true})};
}
async function loadImage(url){const image=new Image();image.src=url;await image.decode();return image;}
export async function loadPartition(configUrl){
  const config=await fetch(configUrl,{cache:'no-store'}).then(r=>{if(!r.ok)throw new Error(`Mask config: ${r.status}`);return r.json();});
  const base=new URL(configUrl,location.href);
  const [source,exclusion]=await Promise.all([loadImage(new URL(config.source,base)),loadImage(new URL(config.weaponExclusion,base))]);
  const [width,height]=config.sourceSize;
  if(source.width!==width||source.height!==height||exclusion.width!==width||exclusion.height!==height)throw new Error('Source/mask dimensions differ.');
  const original=surface(width,height);original.ctx.drawImage(source,0,0);
  const sourcePixels=original.ctx.getImageData(0,0,width,height);
  const mask=surface(width,height);mask.ctx.drawImage(exclusion,0,0);
  const excluded=mask.ctx.getImageData(0,0,width,height).data;
  const owner=new Uint8Array(width*height);
  for(const [i,part] of config.parts.entries()){
    mask.ctx.clearRect(0,0,width,height);mask.ctx.fillStyle='#fff';mask.ctx.fill(new Path2D(part.path));
    const selection=mask.ctx.getImageData(0,0,width,height).data;
    for(let p=0;p<owner.length;p++)if(selection[p*4+3]>=128&&excluded[p*4]<128)owner[p]=i+1;
  }
  const layers=Array.from({length:config.parts.length+1},()=>surface(width,height));
  const data=layers.map(layer=>layer.ctx.createImageData(width,height));
  const counts=Array(layers.length).fill(0),bounds=layers.map(()=>[width,height,0,0]);
  for(let p=0;p<owner.length;p++){
    const i=owner[p],offset=p*4;
    for(let c=0;c<4;c++)data[i].data[offset+c]=sourcePixels.data[offset+c];
    if(sourcePixels.data[offset+3]){
      counts[i]++;const x=p%width,y=Math.floor(p/width),b=bounds[i];
      b[0]=Math.min(b[0],x);b[1]=Math.min(b[1],y);b[2]=Math.max(b[2],x+1);b[3]=Math.max(b[3],y+1);
    }
  }
  layers.forEach((layer,i)=>layer.ctx.putImageData(data[i],0,0));
  const reconstructed=surface(width,height);
  layers.forEach(layer=>reconstructed.ctx.drawImage(layer.canvas,0,0));
  const actual=reconstructed.ctx.getImageData(0,0,width,height).data;
  const armData=data.slice(1);
  let reconstructionDifference=0,weaponOwnedPixels=0;
  for(let p=0;p<owner.length;p++){
    if(excluded[p*4]>=128&&armData.some(layer=>layer.data[p*4+3]>0))weaponOwnedPixels++;
    for(let c=0;c<4;c++)if(actual[p*4+c]!==sourcePixels.data[p*4+c]){reconstructionDifference++;break;}
  }
  const parts=config.parts.map((part,i)=>({...part,canvas:layers[i+1].canvas,bounds:bounds[i+1],pixels:counts[i+1]}));
  return {config,source:original.canvas,remainder:layers[0].canvas,parts,reconstructionDifference,weaponOwnedPixels};
}
