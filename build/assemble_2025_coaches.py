"""Build curated identity seed and coverage audit from locally cached research; no network calls."""
import csv,json,re
from pathlib import Path
from collect_2025_coaches import SCHOOLS
ALIASES={'UConn':'Connecticut','Texas A&M':'Texas AM','Middle Tennessee':'Mid Tenn State','SMU':'Southern Methodist','UCF':'Central Florida','UTSA':'UT San Antonio','UTEP':'UT El Paso','Florida Atlantic':'Florida Atl','FIU':'Florida Intl','Louisiana Tech':'LA Tech','New Mexico State':'New Mexico St','Miami (OH)':'Miami OH','Louisiana':'LA Lafayette','Louisiana–Monroe':'LA Monroe'}
TEAMS=[r.split('|')[0] for r in SCHOOLS]
OFFICIAL=list(csv.DictReader(Path('.artifacts/coaches-2025-review.csv').open(encoding='utf-8')))
result={}
for team in TEAMS:
 for role in ['HC','OC','DC','ST']:
  result[team,role]=[dict(r,CrossCheckUrl='') for r in OFFICIAL if r['Team']==team and r['Role']==role]
for p in Path('.artifacts/season-coaches-2025').glob('*.json'):
 d=json.loads(p.read_text(encoding='utf-8'));team=ALIASES.get(d['team'],d['team'])
 if team not in TEAMS:continue
 groups={}
 for r in d['roles']:
  title=r['title'].lower();role='HC' if title=='head coach' else 'OC' if 'offensive coordinator' in title else 'DC' if 'defensive coordinator' in title else None
  if not role:continue
  names=r['names'] or [re.split(r'\s*\(',r['text'])[0].strip()]
  # A primary-role list may include a midseason replacement. Keep the opening coach; co-coordinator rows retain each person.
  if 'co-' not in title and not title.endswith('coordinators'): names=names[:1]
  for name in names:
   if not name or name.startswith('['):continue
   key=lambda s:re.sub(r'[^a-z]','',s.lower()).replace('michael','mike').replace('donmartindale','winkmartindale')
   match=next((o for o in result[team,role] if key(o['Name'])==key(name)),None)
   item=dict(match) if match else dict(Season='2025',Team=team,Role=role,Name=name,Title=r['title'],SourceUrl=d['source'])
   item['CrossCheckUrl']=d['source'];groups.setdefault(role,[]).append(item)
 for role,items in groups.items():
  # Keep genuinely shared roles visible in the official archive, but not later replacement primaries.
  for old in result[team,role]:
   if re.search(r'co[- ]'+('offensive' if role=='OC' else 'defensive')+r' coordinator',old['Title'],re.I) and not any(x['Name']==old['Name'] for x in items):items.append(old)
  result[team,role]=items
manual=Path('docs/data/coaches-2025-overrides.json')
if manual.exists():
 for item in json.loads(manual.read_text(encoding='utf-8')):
  result[item['Team'],item['Role']]=[dict(Season='2025',Team=item['Team'],Role=item['Role'],Name=n,Title=item.get('Title',{'HC':'Head Coach','OC':'Offensive Coordinator','DC':'Defensive Coordinator','ST':'Special Teams Coordinator'}[item['Role']]),SourceUrl=item['SourceUrl'],CrossCheckUrl=item.get('CrossCheckUrl','')) for n in item['Names']]
fields=['Season','Team','Role','Name','Title','SourceUrl','CrossCheckUrl']
rows=[dict(r, Team='UConn' if r['Team']=='Connecticut' else r['Team']) for key,items in sorted(result.items()) for r in items]
for path in [Path('src/DynastyManager.App/Resources/Raw/coaches-2025.csv'),Path('docs/data/coaches-2025.csv')]:
 path.parent.mkdir(parents=True,exist_ok=True)
 with path.open('w',encoding='utf-8',newline='') as f:
  w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
missing=[(t,r) for (t,r),items in sorted(result.items()) if not items]
print('Records:',len(rows),'Teams:',len(TEAMS),'Missing roles:',len(missing))
for t,r in missing:print(t,r)
