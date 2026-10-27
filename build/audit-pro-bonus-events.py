"""Read-only bonus cash/proration compatibility audit; no contract corrections are inferred.
Usage: python build/audit-pro-bonus-events.py OUTPUT.json
"""
import argparse,hashlib,json
from pathlib import Path
root=Path(__file__).resolve().parents[1];p=argparse.ArgumentParser(description=__doc__);p.add_argument('output',type=Path);a=p.parse_args()
path=root/'src/ProFootball.Core/nfl-contracts.json';contracts=json.loads(path.read_text(encoding='utf-8'))
players={p['Football']['PlayerId']:p for p in json.loads((root/'src/ProFootball.Core/nfl-snapshot.json').read_text(encoding='utf-8'))['Players']}
result=[]
for c in contracts:
 cash={int(y):v for y,v in (c.get('BonusCash') or {}).items() if v>0}
 if not any(y>2026 for y in cash):continue
 years=c['Years'];last=max(r['Year'] for r in years);allocated={r['Year']:0 for r in years};events=[]
 for year,amount in sorted(cash.items()):
  count=min(5,last-year+1)
  if count<1:raise ValueError('Cash event after accounting horizon')
  for i in range(count):allocated[year+i]=allocated.get(year+i,0)+amount//count+(i<amount%count)
  row=next(r for r in years if r['Year']==year)
  # This is only an arithmetic coverage candidate; legal guarantees/vesting are not inferred.
  coverage=min(amount,max(0,(row.get('SourceGuaranteed') or 0)-row['Salary']-(row.get('OtherCash') or 0)))
  events.append(dict(year=year,cash=amount,maximumYears=count,sourceGuaranteeResidualCoverage=coverage))
 residuals=[dict(year=r['Year'],sourceProration=r.get('ProratedBonus') or 0,impliedKnownCashProration=allocated.get(r['Year'],0),unexplainedProration=(r.get('ProratedBonus') or 0)-allocated.get(r['Year'],0)) for r in years]
 minimum=min(r['unexplainedProration'] for r in residuals)
 missing=sorted(set(allocated)-{r['Year'] for r in years})
 player=players.get(c['PlayerId'],{})
 result.append(dict(playerId=c['PlayerId'],name=player.get('Football',{}).get('FullName'),team=c['Team'],source=c.get('Source'),futureScheduledBonusCash=sum(v for y,v in cash.items() if y>2026),
   missingAccountingYears=missing,compatibility='does_not_reconcile' if missing else 'nonnegative_residual_candidate' if minimum>=0 else 'rounding_review' if minimum>=-5 else 'does_not_reconcile',events=events,proration=residuals))
summary=dict(contractsWithFutureCash=len(result),futureScheduledBonusCash=sum(r['futureScheduledBonusCash'] for r in result),classifications={label:sum(r['compatibility']==label for r in result) for label in ['nonnegative_residual_candidate','rounding_review','does_not_reconcile']})
output=dict(year=2026,sourcePackSha256=hashlib.sha256(path.read_bytes()).hexdigest(),scope='Developer arithmetic audit only. Future scheduled cash is not proof of prior payment, guarantee or vesting. Nonnegative residuals only show mathematical compatibility with up-to-five-year cash-event allocation; original prior-paid bonus balances and legal timing require verification. Do not use this report to reprice contracts or erase debt.',summary=summary,contracts=result)
a.output.write_text(json.dumps(output,indent=2)+'\n',encoding='utf-8');print(json.dumps(summary))
