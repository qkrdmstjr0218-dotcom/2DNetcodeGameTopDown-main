"""Composite only darkened seam-gap pixels from the local edit."""
from pathlib import Path
import numpy as np
from PIL import Image

out = Path(__file__).resolve().parent
base = Image.open(out / 'downshot-boots-local-outline-preview.png').convert('RGBA')
edit = Image.open('C:/Users/PC/.codex/generated_images/01a12d87-d09d-7f20-a556-97e2f4b8fa44/exec-b949e1b4-1e97-402c-a6a8-cee25b7ac192.png').convert('RGBA')
rgb = np.asarray(edit)[:, :, :3]
rows = np.flatnonzero(np.any(rgb > 12, axis=(1, 2)))
edit = edit.crop((0, int(rows[0]), edit.width, int(rows[-1])+1)).resize((425,130), Image.Resampling.LANCZOS)
pixels = np.array(base)
patch = np.asarray(edit)
zone = pixels[800:930,900:1325]
y,x = np.indices((130,425))
seam = (((x>=48)&(x<=92)) | ((x>=343)&(x<=388))) & (y>=55)&(y<=61)
selection = seam & (np.min(zone[:,:,:3],axis=2)>100) & (np.max(patch[:,:,:3],axis=2)<90)
zone[selection,:3] = patch[selection,:3]
result = Image.fromarray(pixels)
result.save(out / 'downshot-boot-gap-filled-preview.png')
result.crop((900,800,1325,930)).resize((1275,390), Image.Resampling.NEAREST).save(out / 'downshot-boot-gap-filled-detail.png')
print(f'Changed only {np.count_nonzero(selection)} light gap pixels; no geometry moved.')
