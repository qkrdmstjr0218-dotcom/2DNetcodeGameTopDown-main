from pathlib import Path
from PIL import Image
import numpy as np
root=Path(__file__).resolve().parents[2]
sheet=np.array(Image.open(root/'Assets/RightWalkPrototype/Art/RightWalkSheet-Redrawn-v5.png').convert('RGBA'))
cells=[sheet[i//5*512:(i//5+1)*512,i%5*512:(i%5+1)*512] for i in range(10)]
assert all(np.array_equal(c[:220],cells[0][:220]) for c in cells)
assert all(not np.any(np.concatenate([c[:16,:,3].ravel(),c[-16:,:,3].ravel(),c[:,:16,3].ravel(),c[:,-16:,3].ravel()])) for c in cells)
assert len({c[245:].tobytes() for c in cells})==10
frames=[Image.open(root/f'LocalTools/Captures/SwingPlateWalk/{i:02}.png').convert('RGB') for i in range(10)]
frames[0].save(root/'LocalTools/Captures/swing-plate-right-walk-v5.gif',save_all=True,append_images=frames[1:],duration=[150,150,160,70,80,150,160,150,150,150],loop=0)
contact=Image.new('RGB',(1200,480))
for i,f in enumerate(frames):contact.paste(f.resize((240,240)),(i%5*240,i//5*240))
contact.save(root/'LocalTools/Captures/swing-plate-unity-v5.jpg')
print('PASS: fixed upper body, transparent 16px gutters, 10 distinct lower poses; GIF saved.')
