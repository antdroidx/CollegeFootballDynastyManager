import json,re,csv
from pathlib import Path
ROWS=[]
for p in sorted(Path('.artifacts/coaches-2025').glob('*.json')):
 d=json.loads(p.read_text(encoding='utf-8')); roles={'HC':[],'OC':[],'DC':[],'ST':[]}
 names=[c['name'] for c in d['candidates'] if c['name'] not in ('View Bio','Full Bio')]
 for c in d['candidates']:
  n=' '.join(c['name'].split());text=c['context']
  if n in ('View Bio','Full Bio'):continue
  if text.startswith(c['name']):t=text[len(c['name']):].strip()
  elif 0 < text.find(c['name']) < 160:
   t=text[:text.find(c['name'])].strip()
   if any(other in t for other in names if other!=c['name']):continue
  else:continue
  ends=[t.find(other) for other in names if other!=c['name'] and t.find(other)>=0]
  if ends:t=t[:min(ends)].strip()
  t=re.split(r'Full Bio|View Bio|/Football|Full bio|Phone',t)[0].strip()
  for role,pattern in [('HC',r'^(?:Interim )?Head (?:Football )?Coach'),('OC',r'offensive coordinator'),('DC',r'defensive coordinator'),('ST',r'special teams coordinator')]:
   if re.search(pattern,t,re.I) and n not in roles[role] and not (role=='HC' and re.search(r'strength|conditioning',t,re.I)):
    roles[role].append(n);ROWS.append({'Season':2025,'Team':d['team'],'Role':role,'Name':n,'Title':t,'SourceUrl':d['source']})
 print(d['team']+' | '+' | '.join(k+': '+','.join(v) for k,v in roles.items()))
with Path('.artifacts/coaches-2025-review.csv').open('w',newline='',encoding='utf-8') as f:
 w=csv.DictWriter(f,fieldnames=['Season','Team','Role','Name','Title','SourceUrl']);w.writeheader();w.writerows(ROWS)
