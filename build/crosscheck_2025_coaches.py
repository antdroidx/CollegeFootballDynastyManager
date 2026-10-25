import concurrent.futures,json,re,urllib.request
from pathlib import Path
from bs4 import BeautifulSoup
OUT=Path('.artifacts/season-coaches-2025');OUT.mkdir(parents=True,exist_ok=True)
links=json.loads(Path('.artifacts/2025-team-links.json').read_text(encoding='utf-8'))
links.update({'https://en.wikipedia.org/wiki/2025_North_Dakota_State_Bison_football_team':'North Dakota State','https://en.wikipedia.org/wiki/2025_Sacramento_State_Hornets_football_team':'Sacramento State'})
def fetch(item):
 url,team=item
 path=OUT/(url.rsplit('/',1)[-1]+'.json')
 if path.exists():return team,'cached'
 try:
  html=urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'CFDM-CoachResearch/1.0'}),timeout=20).read().decode('utf-8')
  soup=BeautifulSoup(html,'html.parser');box=soup.select_one('.infobox')
  rows=[]
  for row in box.select('tr'):
   th=row.find('th');td=row.find('td')
   if th and td:
    title=th.get_text(' ',strip=True);value=td.get_text(' ',strip=True)
    if 'coach' in title.lower() or 'coordinator' in title.lower():
     rows.append({'title':title,'text':value,'names':[a.get_text(' ',strip=True) for a in td.select('a') if not a.get_text(' ',strip=True).startswith('[')]})
  staff=[]
  for h in soup.find_all(['h2','h3']):
   if 'staff' in h.get_text().lower():
    table=h.find_next('table')
    if table:staff.append(table.get_text(' | ',strip=True))
  path.write_text(json.dumps({'team':team,'source':url,'roles':rows,'staff':staff},ensure_ascii=False,indent=2),encoding='utf-8')
  return team,str(len(rows))
 except Exception as e:return team,str(e)
if __name__=='__main__':
 with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
  for team,result in pool.map(fetch,links.items()):print(team+': '+result,flush=True)
