"""Stage only observed 2026 departed-contract cap charges; no cash, future debt, or balancing entries."""
import argparse,hashlib,json
from pathlib import Path
p=argparse.ArgumentParser(description=__doc__);p.add_argument('obligations',type=Path);p.add_argument('identities',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
source=json.loads(a.obligations.read_text());identities=json.loads(a.identities.read_text());by={t['team']:t for t in identities['teams']}
if source['year']!=2026 or source['errors'] or len({t['team'] for t in source['teams']})!=32:raise ValueError('Require complete32-team2026 source')
teams=[]
for team in source['teams']:
 identity=by[team['team']]
 if team['sha256']!=identity['source_sha256']:raise ValueError('Mismatched dated source pages')
 sections=[s for s in team['sections'] if s['section']=='Dead Money']
 if len(sections)!=1:raise ValueError('Require exactly one dead-money section')
 dead=sections[0];matches=[r for r in identity['rows'] if r['section']=='Dead Money']
 if len(matches)!=len(dead['entries']):raise ValueError('Identity row count differs')
 entries=[]
 for index,(row,match) in enumerate(zip(dead['entries'],matches)):
  if row['name']!=match['source_name'] or row['cap']!=match['source_cap'] or row['otc_id']!=match['otc_id']:raise ValueError('Identity row order differs')
  if row['cap']<0:raise ValueError('Negative debt needs a separately reviewed credit')
  entries.append({'key':f"OTC/{source['observed']}/{team['team']}/dead/{index}",'name':row['name'],'playerId':match['player_id'] if match['status']=='verified_identifier' else None,'otcId':row['otc_id'],'amount':row['cap'],'kind':'named_departed_contract'})
 residual=dead['reported_total']-sum(r['amount'] for r in entries)
 if residual<0:raise ValueError('Visible debt exceeds aggregate')
 if residual:entries.append({'key':f"OTC/{source['observed']}/{team['team']}/dead/aggregate",'name':'Other prior-contract cap charges','playerId':None,'otcId':None,'amount':residual,'kind':'reported_aggregate_remainder'})
 if sum(r['amount'] for r in entries)!=dead['reported_total']:raise ValueError('Debt does not reconcile')
 teams.append({'team':team['team'],'year':2026,'sourceUrl':team['url'],'sourceSha256':team['sha256'],'observedOn':source['observed'],'reportedTotal':dead['reported_total'],'entries':entries})
result={'version':1,'scope':'Observed2026 cap assessments only. Null player IDs remain unresolved financial identities. Aggregate remainders are source-reported debt, not invented individual liabilities. No historical cash, future-year debt, player ownership or available room is inferred. Staged only; no game resource or saved ledger is changed.','sourceSha256':hashlib.sha256(a.obligations.read_bytes()).hexdigest(),'teams':teams}
a.output.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print('PASS:',len(teams),'teams;',sum(t['reportedTotal'] for t in teams),'exact known cap charges;',sum(len(t['entries']) for t in teams),'financial events; no cash or future liabilities inferred.')
