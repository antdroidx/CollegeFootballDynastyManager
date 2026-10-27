"""Developer-only public-source observation; never changes a franchise or game data.
Requires beautifulsoup4. Fetches at most four public pages concurrently and caches raw evidence.
Usage: python build/audit-otc-opening-obligations.py --observed 2026-10-26 --year 2026
The observed date is retrieval, not a claim of a published effective date.
"""
import argparse, concurrent.futures, hashlib, json, re, unicodedata
from pathlib import Path
from urllib.request import urlopen
from datetime import date, datetime, timezone
from bs4 import BeautifulSoup
root=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--observed',type=date.fromisoformat,required=True)
parser.add_argument('--year',type=int,required=True)
parser.add_argument('--offline',action='store_true')
args=parser.parse_args()
cache=root/'.artifacts'/('otc-'+str(args.observed));cache.mkdir(parents=True,exist_ok=True)
league=json.loads((root/'src/ProFootball.Core/league.json').read_text(encoding='utf-8'))['Teams']
players=json.loads((root/'src/ProFootball.Core/nfl-snapshot.json').read_text(encoding='utf-8'))['Players']
def normalized(name):
    value=unicodedata.normalize('NFKD',name).encode('ascii','ignore').decode().lower()
    return re.sub(r'[^a-z0-9]','',re.sub(r'\b(jr|sr|ii|iii|iv)\b','',value))
def dollars(value):
    value=value.strip()
    if not re.fullmatch(r'\(?-?\$[0-9,]+\)?',value):raise ValueError('Unrecognized dollar cell: '+value)
    return int(re.sub(r'[^0-9]','',value))*(-1 if '(' in value or '-' in value else 1)
def observe(team):
    name=team['City']+' '+team['Nickname'];abbr=team['Abbreviation']
    url='https://overthecap.com/salary-cap/'+name.lower().replace(' ','-')
    path=cache/(abbr+'.html')
    if not path.exists():
        if args.offline:raise ValueError('Missing cached '+abbr)
        path.write_bytes(urlopen(url,timeout=30).read())
    raw=path.read_bytes();year=BeautifulSoup(raw,'html.parser').find(id='y'+str(args.year))
    if year is None:raise ValueError('Missing requested year '+abbr)
    text=year.get_text(' ',strip=True)
    def headline(label):
        found=re.search(re.escape(label)+r':\s*(\(?-?\$[0-9,]+\)?)',text)
        if not found:raise ValueError('Missing '+label+' '+abbr)
        return dollars(found.group(1))
    sections=[]
    for table in year.find_all('table'):
        active='contracted-players' in table.get('class',[])
        title='Active roster' if active else table.find_previous('h5').get_text(' ',strip=True)
        entries=[];total=None
        for row in table.find_all('tr'):
            cells=row.find_all('td',recursive=False)
            if not cells:continue
            label=cells[0].get_text(' ',strip=True)
            if label=='TOTAL':
                total=dollars(cells[11 if active else 1].get_text(' ',strip=True));continue
            if active:
                if 'player-information' not in cells[0].get('class',[]):continue
                charge=dollars(cells[11].get_text(' ',strip=True))
            else:charge=dollars(cells[1].get_text(' ',strip=True))
            link=cells[0].find('a',href=True);href=link['href'] if link else None
            identity=re.search(r'/player/[^/]+/(\d+)/?',href or '')
            entries.append(dict(name=label,cap=charge,player_url='https://overthecap.com'+href if href and href.startswith('/') else href,otc_id=int(identity.group(1)) if identity else None))
        sections.append(dict(section=title,entries=entries,reported_total=total,visible_sum=sum(e['cap'] for e in entries),unallocated_source_difference=None if total is None else total-sum(e['cap'] for e in entries)))
    section_total=sum(s['reported_total'] if s['reported_total'] is not None else s['visible_sum'] for s in sections)
    if section_total!=headline('Total Cap Liabilities'):
        raise ValueError('Section totals do not match reported liabilities '+abbr)
    dead=next((s for s in sections if s['section']=='Dead Money'),None)
    overlaps=[]
    if dead:
        for e in dead['entries']:
            matches=[p for p in players if p['Football']['TeamName']==name and normalized(p['Football']['FullName'])==normalized(e['name'])]
            for p in matches:overlaps.append(dict(source_name=e['name'],roster_name=p['Football']['FullName'],player_id=p['Football']['PlayerId'],status=p.get('RosterStatus',''),practice=p.get('PracticeSquad',False),reserve=p.get('Reserve',False)))
    return dict(team=abbr,url=url,observed=str(args.observed),year=args.year,sha256=hashlib.sha256(raw).hexdigest(),liabilities=headline('Total Cap Liabilities'),top51=headline('Top 51'),reported_room=headline('Team Cap Space'),sections=sections,dead_roster_name_candidates=overlaps)
results=[];errors=[]
with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
    futures={pool.submit(observe,t):t['Abbreviation'] for t in league}
    for future in concurrent.futures.as_completed(futures):
        try:results.append(future.result())
        except Exception as e:errors.append(dict(team=futures[future],error=str(e)))
output=dict(observed=str(args.observed),generated_utc=datetime.now(timezone.utc).isoformat(),year=args.year,scope='Developer observation only. No game commitments, statuses, debt or room are changed. Name candidates are not verified identity matches. Source section residuals remain unresolved.',teams=sorted(results,key=lambda r:r['team']),errors=errors)
(cache/'obligations-audit.json').write_text(json.dumps(output,indent=2),encoding='utf-8')
print('Observed',len(results),'teams;',len(errors),'errors;',sum(len(r['dead_roster_name_candidates']) for r in results),'roster/dead-name candidates.')
for error in errors:print(error['team'],error['error'])
if errors:raise SystemExit(1)
