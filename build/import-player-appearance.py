"""Derive illustrated skin-palette matches from public roster headshots, never from names.
Developer-only: pip install opencv-python-headless==4.12.0.88 numpy requests
Usage: python import-player-appearance.py ROSTER_CSV CACHE_DIR OUTPUT_JSON
Only confident frontal-face cheek samples are retained. Review a contact sheet before shipping.
"""
import csv,json,sys,hashlib,threading,concurrent.futures,colorsys
from pathlib import Path
from urllib.parse import urlparse
import cv2,numpy as np,requests
PALETTE=["#F0C9AB","#DCAB84","#C58D64","#AB714C","#8E583B","#70432F","#503125"]
local=threading.local()
def skin_match(image,box):
    x,y,w,h=map(int,box);patches=[]
    for xa,ya,xb,yb in [( .18,.45,.37,.60),(.63,.45,.82,.60)]:
        patch=image[y+int(h*ya):y+int(h*yb),x+int(w*xa):x+int(w*xb),:3]
        if patch.size==0:return None
        hsv=cv2.cvtColor(patch,cv2.COLOR_BGR2HSV);mask=(hsv[:,:,0]<30)&(hsv[:,:,1]>15)&(hsv[:,:,1]<220)&(hsv[:,:,2]>25)
        values=patch[mask]
        if len(values)<20:return None
        patches.append(np.median(values,axis=0))
    labs=cv2.cvtColor(np.array(patches,dtype=np.float32).reshape(1,2,3)/255,cv2.COLOR_BGR2LAB).reshape(2,3)
    spread=abs(float(labs[0,0]-labs[1,0]))
    if spread>10:return None
    observed=np.median(patches,axis=0);lab=np.median(labs,axis=0)
    colors=np.array([[int(h[i:i+2],16) for i in (5,3,1)] for h in PALETTE],dtype=np.float32).reshape(1,7,3)/255
    palette=cv2.cvtColor(colors,cv2.COLOR_BGR2LAB).reshape(7,3)
    distance=np.sqrt(((palette-lab)*np.array([1,.25,.25]))**2).sum(axis=1)
    index=int(np.argmin(distance));rgb=observed[::-1].astype(int)
    return dict(SkinTone=index,ObservedColor="#"+"".join(f"{v:02X}" for v in rgb),Confidence=round(1-spread/30,3))
def main():
    roster,cache,output=map(Path,sys.argv[1:4]);cache.mkdir(parents=True,exist_ok=True)
    rows={}
    for r in csv.DictReader(roster.open(encoding="utf-8-sig")):
        if r.get("gsis_id") and r.get("headshot_url") and (r["gsis_id"] not in rows or int(r.get("week") or 0)>int(rows[r["gsis_id"]].get("week") or 0)):rows[r["gsis_id"]]=r
    def process(r):
        url=r["headshot_url"];host=urlparse(url).hostname or ""
        if urlparse(url).scheme!="https" or not(host.endswith(".nfl.com") or host.endswith(".espncdn.com")):return None
        dest=cache/(hashlib.sha256(url.encode()).hexdigest()+".photo")
        try:
            if not dest.exists():
                response=requests.get(url,timeout=20);response.raise_for_status()
                if len(response.content)>5_000_000:return None
                dest.write_bytes(response.content)
            image=cv2.imdecode(np.frombuffer(dest.read_bytes(),np.uint8),cv2.IMREAD_COLOR)
            if image is None:return None
            factor=min(1,600/max(image.shape[:2]));image=cv2.resize(image,None,fx=factor,fy=factor)
            if not hasattr(local,"face"):local.face=cv2.CascadeClassifier(cv2.data.haarcascades+"haarcascade_frontalface_default.xml")
            faces=local.face.detectMultiScale(cv2.cvtColor(image,cv2.COLOR_BGR2GRAY),1.1,5,minSize=(24,24))
            faces=[f for f in faces if .25<(f[0]+f[2]/2)/image.shape[1]<.75 and .1<(f[1]+f[3]/2)/image.shape[0]<.8]
            if len(faces)!=1:return None
            match=skin_match(image,faces[0])
            if not match:return None
            return dict(ExternalId=r["gsis_id"],Name=r["full_name"],Source=url,**match)
        except (requests.RequestException,ValueError,cv2.error):return None
    results=[]
    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as executor:
        for i,result in enumerate(executor.map(process,rows.values()),1):
            if result:results.append(result)
            if i%200==0:print(f"Reviewed {i}/{len(rows)} headshots; {len(results)} confident matches",flush=True)
    output.write_text(json.dumps(sorted(results,key=lambda x:x["ExternalId"]),separators=(",",":")),encoding="utf-8")
    print(f"Saved {len(results)} photo-derived appearances of {len(rows)} headshots",flush=True)
if __name__=="__main__":main()
