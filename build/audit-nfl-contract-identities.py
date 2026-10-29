"""Read-only OTC-ID -> GSIS -> game-player crosswalk audit. No name-only mapping is accepted.
Requires pyarrow. Pass the dated contract Parquet and the cached opening-obligation audit.
Usage: python build/audit-nfl-contract-identities.py SOURCE.parquet AUDIT.json OUTPUT.json
"""
import argparse,hashlib,json,re,unicodedata
from pathlib import Path
import pyarrow.parquet as pq
root=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('contracts',type=Path);parser.add_argument('observation',type=Path);parser.add_argument('output',type=Path)
args=parser.parse_args()
rows=pq.read_table(args.contracts).to_pylist();observation=json.loads(args.observation.read_text(encoding='utf-8'))
if len(observation['teams'])!=32 or observation['errors']:raise ValueError('Require a complete 32-team observation')
players=json.loads((root/'src/ProFootball.Core/nfl-snapshot.json').read_text(encoding='utf-8'))['Players']
league=json.loads((root/'src/ProFootball.Core/league.json').read_text(encoding='utf-8'))['Teams']
team_names={t['Abbreviation']:t['City']+' '+t['Nickname'] for t in league}
identities={};by_gsis={}
for r in rows:
    if r.get('otc_id') is not None and r.get('gsis_id'):identities.setdefault(int(r['otc_id']),set()).add(r['gsis_id'])
for p in players:
    if p.get('ExternalId'):by_gsis.setdefault(p['ExternalId'],[]).append(p)
def normalized(name):return re.sub('[^a-z0-9]','',unicodedata.normalize('NFKD',name).encode('ascii','ignore').decode().lower())
results=[]
for team in observation['teams']:
    mappings=[]
    for section in team['sections']:
        for entry in section['entries']:
            ids=identities.get(entry.get('otc_id'),set());matches=by_gsis.get(next(iter(ids)),[]) if len(ids)==1 else []
            status='verified_identifier' if len(matches)==1 else 'ambiguous_identifier' if len(ids)>1 or len(matches)>1 else 'unresolved_identifier'
            player=matches[0] if len(matches)==1 else None
            current=section['section']!='Dead Money'
            if current and player is not None and player['Football']['TeamName']!=team_names[team['team']]:status='dated_team_conflict'
            candidates=[] if player else [p['Football']['PlayerId'] for p in players if p['Football']['TeamName']==team_names[team['team']] and normalized(p['Football']['FullName'])==normalized(entry['name'])]
            mappings.append(dict(section=section['section'],source_name=entry['name'],source_cap=entry['cap'],otc_id=entry.get('otc_id'),source_player_url=entry.get('player_url'),gsis_ids=sorted(ids),status=status,
                player_id=player['Football']['PlayerId'] if player else None,roster_name=player['Football']['FullName'] if player else None,roster_team=player['Football']['TeamName'] if player else None,
                roster_status=player.get('RosterStatus') if player else None,roster_practice=player.get('PracticeSquad') if player else None,roster_reserve=player.get('Reserve') if player else None,name_only_candidates=candidates))
    results.append(dict(team=team['team'],source_url=team['url'],source_sha256=team['sha256'],rows=mappings))
current=[m for t in results for m in t['rows'] if m['section']!='Dead Money'];dead=[m for t in results for m in t['rows'] if m['section']=='Dead Money']
summary=dict(current_rows=len(current),current_verified=sum(m['status']=='verified_identifier' for m in current),current_team_conflicts=sum(m['status']=='dated_team_conflict' for m in current),current_unresolved=sum(m['status']=='unresolved_identifier' for m in current),current_ambiguous=sum(m['status']=='ambiguous_identifier' for m in current),dead_rows=len(dead),dead_verified=sum(m['status']=='verified_identifier' for m in dead))
output=dict(observed=observation['observed'],year=observation['year'],contract_sha256=hashlib.sha256(args.contracts.read_bytes()).hexdigest(),scope='Developer identity audit only; no statuses/contracts/debt are changed. Name-only candidates require review and are never promoted to verified mappings. Repeated debt rows remain separate.',summary=summary,teams=results)
args.output.write_text(json.dumps(output,indent=2),encoding='utf-8');print(json.dumps(summary))
