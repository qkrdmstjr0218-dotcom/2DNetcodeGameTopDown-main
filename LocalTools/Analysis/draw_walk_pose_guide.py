from PIL import Image, ImageDraw
from pathlib import Path
root=Path(__file__).resolve().parents[2]
im=Image.new('RGB',(1024,640),'white');d=ImageDraw.Draw(im)
poses=[((160,230),(185,280),(106,230),(90,280)),
       ((140,230),(155,280),(126,220),(112,250)),
       ((125,230),(115,280),(162,205),(160,230)),
       ((108,235),(85,280),(164,227),(190,275))]
for i in range(8):
    ox=(i%4)*256;oy=(i//4)*320
    n1,n2,f1,f2=poses[i%4]
    if i>=4:n1,n2,f1,f2=f1,f2,n1,n2
    def line(points,color,width):d.line([(x+ox,y+oy) for x,y in points],fill=color,width=width)
    d.text((ox+10,oy+10),f'Frame {i+1}',fill='black')
    d.ellipse((ox+113,oy+55,ox+151,oy+93),outline='gray',width=3)
    line([(130,93),(130,180)],'gray',8)
    line([(130,118),(188,118),(220,118)],'gray',6)
    line([(126,180),f1,f2,(f2[0]+15,f2[1])],'orange',8)
    line([(134,180),n1,n2,(n2[0]+15,n2[1])],'blue',8)
    for x,y in (n1,n2):d.ellipse((ox+x-5,oy+y-5,ox+x+5,oy+y+5),fill='blue')
    line([(60,286),(220,286)],'#dddddd',1)
im.save(root/'LocalTools/Analysis/right-walk-clean-pose-guide.png')
