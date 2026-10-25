"""Convert a dated nflverse OTC Parquet snapshot into the optional game contract pack.

Usage: python build/import-nfl-contracts.py path/to/historical_contracts.parquet
Requires pyarrow. Download source separately; never use the obsolete CSV asset.
"""
import hashlib
import json
import sys
from pathlib import Path
import pyarrow.parquet as pq

root = Path(__file__).resolve().parents[1]
source = Path(sys.argv[1])
rows = pq.read_table(source).to_pylist()
players = json.loads((root / 'src/ProFootball.Core/nfl-snapshot.json').read_text(encoding='utf-8'))['Players']
teams = json.loads((root / 'src/ProFootball.Core/league.json').read_text(encoding='utf-8'))['Teams']
nicknames = {t['City'] + ' ' + t['Nickname']: t['Nickname'] for t in teams}
by_id = {}
for row in rows:
    if row.get('gsis_id') and row.get('is_active'):
        by_id.setdefault(row['gsis_id'], []).append(row)
contracts = []
for player in players:
    team = player['Football']['TeamName']
    if team not in nicknames:
        continue
    for row in sorted(by_id.get(player.get('ExternalId'), []), key=lambda x: x.get('year_signed') or 0, reverse=True):
        history = [h for h in row.get('season_history') or [] if str(h.get('year', '')).isdigit() and int(h['year']) >= 2026 and h.get('team') == nicknames[team] and h.get('cap_number') is not None]
        if not any(int(h['year']) == 2026 for h in history):
            continue
        years = []
        for h in sorted(history, key=lambda h: int(h['year'])):
            def dollars(key): return round((h.get(key) or 0) * 1_000_000)
            salary = dollars('base_salary')
            bonus = dollars('prorated_bonus') + dollars('option_bonus')
            years.append(dict(Year=int(h['year']), Salary=salary, Guaranteed=dollars('guaranteed_salary'), ProratedBonus=bonus, OtherCompensation=dollars('cap_number') - salary - bonus))
        contracts.append(dict(PlayerId=player['Football']['PlayerId'], Team=team, SignedYear=2026, BonusYears=len(years), SigningBonus=sum(y['ProratedBonus'] for y in years), Years=years, Estimated=False, Source='OverTheCap via nflverse · 2026-10-24', Active=True))
        break
(root / 'src/ProFootball.Core/nfl-contracts.json').write_text(json.dumps(contracts, ensure_ascii=False, separators=(',', ':')), encoding='utf-8')
print(f'{len(contracts)} matched contracts; source SHA-256 {hashlib.sha256(source.read_bytes()).hexdigest()}')
