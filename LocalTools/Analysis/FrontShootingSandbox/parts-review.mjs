import {loadPartition} from './segmentation.mjs';
const $=id=>document.getElementById(id);
try{
  const partition=await loadPartition('./parts-v2.json');
  const old=Object.fromEntries(await Promise.all(partition.parts.map(async part=>{
    const image=new Image();image.src=`./${part.id}.png`;await image.decode();return [part.id,image];
  })));
  const controls=['assembly','focus','contours','anchors','old'];
  const thumbnails=[];
  function cross(ctx,point,color,size=5){ctx.strokeStyle=color;ctx.lineWidth=1.5;ctx.beginPath();ctx.moveTo(point[0]-size,point[1]);ctx.lineTo(point[0]+size,point[1]);ctx.moveTo(point[0],point[1]-size);ctx.lineTo(point[0],point[1]+size);ctx.stroke();}
  function joints(ctx,part){
    ctx.save();ctx.strokeStyle=part.color;ctx.lineWidth=1;ctx.setLineDash([4,3]);ctx.beginPath();
    Object.values(part.anchors).forEach((p,i)=>i?ctx.lineTo(...p):ctx.moveTo(...p));ctx.stroke();ctx.setLineDash([]);
    Object.values(part.anchors).forEach(p=>cross(ctx,p,part.color));ctx.restore();
  }
  for(const part of partition.parts){
    const option=document.createElement('option');option.value=part.id;option.textContent=part.label;$('focus').append(option);
    const figure=document.createElement('figure');figure.className='part';
    const caption=document.createElement('figcaption'),title=document.createElement('span'),button=document.createElement('a');
    title.textContent=part.label;title.style.color=part.color;button.textContent='PNG';button.title=`${part.label} 새 분리본 v2 · 원본 해상도 내보내기`;
    button.href=part.canvas.toDataURL('image/png');button.download=`${part.id}-v2.png`;
    caption.append(title,button);const canvas=document.createElement('canvas');canvas.width=canvas.height=400;canvas.dataset.part=part.id;
    const info=document.createElement('output');info.textContent=`${part.pixels.toLocaleString()} px · ${part.bounds[2]-part.bounds[0]} × ${part.bounds[3]-part.bounds[1]}`;
    figure.append(caption,canvas,info);$('parts').append(figure);thumbnails.push({part,canvas,info});
  }
  function render(){
    const canvas=$('assemblyView'),ctx=canvas.getContext('2d'),mode=$('assembly').value;
    ctx.clearRect(0,0,canvas.width,canvas.height);ctx.save();ctx.translate(canvas.width/2,canvas.height/2);
    const focus=partition.parts.find(part=>part.id===$('focus').value);
    const [x,y,right,bottom]=focus?.bounds??[0,0,0,0];
    const viewScale=mode==='exploded'?.8:focus?canvas.width/(Math.max(right-x,bottom-y)+64):1.5;
    ctx.scale(viewScale,viewScale);ctx.translate(...(focus&&mode!=='exploded'?[-(x+right)/2,-(y+bottom)/2]:[-519,-580]));
    if(mode==='source')ctx.drawImage(partition.source,0,0);
    else if(mode==='remainder')ctx.drawImage(partition.remainder,0,0);
    else if(mode==='exploded'){
      ctx.globalAlpha=.18;ctx.drawImage(partition.remainder,0,0);ctx.globalAlpha=1;
    }
    for(const part of partition.parts){
      ctx.save();if(mode==='exploded')ctx.translate(...part.offset);
      if(mode==='arms'||mode==='exploded')ctx.drawImage($('old').checked?old[part.id]:part.canvas,0,0);
      if($('contours').checked){ctx.strokeStyle=part.color;ctx.lineWidth=1;ctx.stroke(new Path2D(part.path));}
      if($('anchors').checked)joints(ctx,part);ctx.restore();
    }
    ctx.restore();
    for(const {part,canvas,info} of thumbnails){
      const ctx=canvas.getContext('2d'),[x,y,right,bottom]=part.bounds;
      const size=Math.max(right-x,bottom-y)+40,scale=canvas.width/size;
      ctx.clearRect(0,0,canvas.width,canvas.height);ctx.save();ctx.translate(canvas.width/2,canvas.height/2);ctx.scale(scale,scale);ctx.translate(-(x+right)/2,-(y+bottom)/2);
      ctx.drawImage($('old').checked?old[part.id]:part.canvas,0,0);
      if($('anchors').checked)joints(ctx,part);ctx.restore();
      info.textContent=$('old').checked?'이전 분리본':`${part.pixels.toLocaleString()} px · ${right-x} × ${bottom-y}`;
    }
  }
  controls.forEach(id=>$(id).addEventListener('change',render));
  $('reconstruction').textContent=`${partition.reconstructionDifference} px`;$('reconstruction').dataset.ok=partition.reconstructionDifference===0;
  $('weaponPixels').textContent=`${partition.weaponOwnedPixels} px`;$('weaponPixels').dataset.ok=partition.weaponOwnedPixels===0;
  $('status').remove();document.querySelector('main').hidden=false;render();
}catch(error){$('status').textContent=`불러오기 실패: ${error.message}`;console.error(error);}
