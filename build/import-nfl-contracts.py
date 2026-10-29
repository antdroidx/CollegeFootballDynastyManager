"""Convert a dated nflverse OTC Parquet snapshot into the optional game contract pack.

Usage: python build/import-nfl-contracts.py path/to/historical_contracts.parquet --as-of YYYY-MM-DD
Requires pyarrow. Download source separately; never use the obsolete CSV asset.
"""
import argparse
from datetime import date
import hashlib
import json
from pathlib import Path
import pyarrow.parquet as pq

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('source', type=Path)
parser.add_argument('--as-of', type=date.fromisoformat, required=True, help='Snapshot retrieval date, YYYY-MM-DD')
args = parser.parse_args()
source = args.source
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
        bonus_cash = {}
        last_paid_year = max((int(h['year']) for h in history if (h.get('cash_paid') or 0) > 0), default=None)
        for h in sorted(history, key=lambda h: int(h['year'])):
            def dollars(key): return round((h.get(key) or 0) * 1_000_000)
            salary = dollars('base_salary')
            bonus = dollars('prorated_bonus') + dollars('option_bonus')
            # OTC season history uses placeholder base salaries for void rows.
            # Preserve the published cap number, but never treat those placeholders as pay.
            cash = dollars('cash_paid') if h.get('cash_paid') is not None else None
            void = cash == 0 and last_paid_year is not None and int(h['year']) > max(2026, last_paid_year) and dollars('guaranteed_salary') == 0
            other_cash = sum(dollars(k) for k in ('roster_bonus', 'workout_bonus', 'per_game_roster_bonus', 'other_bonus'))
            if void:
                salary, other_cash = 0, 0
            if cash is not None:
                bonus_cash[int(h['year'])] = max(0, cash - salary - other_cash)
            years.append(dict(Year=int(h['year']), Salary=salary,
                Guaranteed=min(salary, dollars('guaranteed_salary')), SourceGuaranteed=dollars('guaranteed_salary'),
                ProratedBonus=bonus, OtherCompensation=dollars('cap_number') - salary - bonus,
                OtherCash=other_cash, IsVoid=void))
        contracts.append(dict(PlayerId=player['Football']['PlayerId'], Team=team, SignedYear=int(row['year_signed']), BonusYears=max(y['Year'] for y in years)-int(row['year_signed'])+1, SigningBonus=0, BonusCash=bonus_cash, AccountingVersion=1, ReportedAnnualValue=round((row.get('apy') or 0)*1_000_000) or None, Years=years, Estimated=False, Source=f'OverTheCap via nflverse · {args.as_of}', Active=True))
        break
(root / 'src/ProFootball.Core/nfl-contracts.json').write_text(json.dumps(contracts, ensure_ascii=False, separators=(',', ':')), encoding='utf-8')
print(f'{len(contracts)} matched contracts; source SHA-256 {hashlib.sha256(source.read_bytes()).hexdigest()}')
