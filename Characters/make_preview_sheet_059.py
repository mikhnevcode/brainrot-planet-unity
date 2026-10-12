from PIL import Image,ImageDraw
from pathlib import Path
base=Path(r"C:\Rabota\projects\BrainrotPlanetUnity\Characters\MotionPreviews_059")
heroes=["Tralalero","Bombardiro","Frigo","Bombombini","Ballerina","Assassino","Patapim","TungSahur","Lirili","Frulli","Boneca","Trippi"]
poses=[("Drop",25),("Special",10),("VictoryShow",41)]
cell_w,cell_h=230,255
sheet=Image.new("RGB",(6*cell_w,6*cell_h),"#101522")
draw=ImageDraw.Draw(sheet)
for i,hero in enumerate(heroes):
    group_col=(i%2)*3
    row=i//2
    for j,(action,frame) in enumerate(poses):
        path=base/(hero+"_"+action+"_"+str(frame).zfill(3)+".png")
        im=Image.open(path).convert("RGBA")
        bg=Image.new("RGB",im.size,"#1d2538")
        bg.paste(im,mask=im.getchannel("A"))
        bg.thumbnail((210,210))
        x=(group_col+j)*cell_w+(cell_w-bg.width)//2
        y=row*cell_h
        sheet.paste(bg,(x,y))
        draw.text(((group_col+j)*cell_w+6,y+216),hero+" "+action,fill="white")
out=Path(r"C:\Rabota\projects\BrainrotPlanetUnity\Characters\MotionPreviews_059.jpg")
sheet.save(out,quality=88,optimize=True)
print(out, out.stat().st_size)
