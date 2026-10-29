"""Build offline payment/guarantee REVIEW candidates from published annual columns.
No app writes. Requires exact raw contract rows and nonnegative option allocation residuals.
Matching release quotes is validation, never a source for inventing guarantee amounts.
"""
import argparse,json,collections
from pathlib import Path
p=argparse.ArgumentParser(description=__doc__);p.add_argument('pages',type=Path);p.add_argument('identities',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
root=Path(__file__).resolve().parents[1];pack={c['PlayerId']:c for c in json.loads((root/'src/ProFootball.Core/nfl-contracts.json').read_text(encoding="utf-8"))};ids={x['otcId']:x for x in json.loads(a.identities.read_text(encoding="utf-8"))['contracts'] if 'otcId' in x};out=[];unresolved=[]
for page in json.loads(a.pages.read_text(encoding="utf-8"))['pages']:
 ident=ids[page['otcId']];c=pack.get(ident['playerId'])
 if c is None:continue
 try:
  rows={y['year']:y for y in page['current']};events=[];other={}
  for y in c['Years']:
   if y['Year']<2026 or y.get('IsVoid'):continue
   row=rows[y['Year']];raw=row.get('reportedGuaranteedCash')
   if raw is None:raise ValueError('Annual guarantee not published')
   if min(y['Salary'],raw)!=y['Guaranteed']:raise ValueError('Published base guarantee differs')
   nonbase=max(0,raw-y['Salary']);extra=min(y.get('OtherCash',0) or 0,nonbase);other[str(y['Year'])]=extra
   amount=c.get('BonusCash',{}).get(str(y['Year']),0)
   if y['Year']>2026 and amount:
    guaranteed=nonbase-extra
    if guaranteed not in [0,amount]:raise ValueError('Partially or ambiguously guaranteed option needs explicit event review')
    years=min(5,c['SignedYear']+c['BonusYears']-y['Year'])
    previous=rows.get(y['Year']-1)
    allocation=amount//years+(1 if amount%years else 0)
    if previous is None or (row.get('optionProration') or 0)-(previous.get('optionProration') or 0)<allocation:
     raise ValueError('Future cash has no sufficient new option allocation; classify non-prorated pay or review expiring prior allocations')
    events.append({'Year':y['Year'],'Amount':amount,'AllocationYears':years,'GuaranteedAtSigning':guaranteed==amount,'GuaranteeObservedOn':page['observedOn'],'GuaranteeTimingUnverified':True})
   elif nonbase!=extra:raise ValueError('Unallocated guaranteed cash needs explicit event review')
  for year,row in rows.items():
   if year<2026:continue
   allocated=sum(e['Amount']//e['AllocationYears']+(1 if year-e['Year']<e['Amount']%e['AllocationYears'] else 0) for e in events if 0<=year-e['Year']<e['AllocationYears'])
   if allocated>(row.get('optionProration') or 0):raise ValueError('Future payment allocation exceeds published option column')
  out.append({'PlayerId':c['PlayerId'],'Player':ident['player'],'SignedYear':c['SignedYear'],'Team':c['Team'],'BonusPayments':events,'GuaranteedOtherCash':other,'SourceUrl':page['sourceUrl'],'SourceSha256':page['sha256'],'Status':'source-column-derived review candidate; guarantee flag is an as-of assumption for comparison only, not proof of guarantee at original signing; published notes and dated payment/vesting semantics still require review'})
 except (ValueError,KeyError) as e:unresolved.append({'player':ident['player'],'reason':str(e)})
a.output.write_text(json.dumps({'candidates':out,'unresolved':unresolved},indent=2)+'\n',encoding='utf-8');print('Candidates',len(out),'unresolved',len(unresolved),dict(collections.Counter(x['reason'] for x in unresolved)))
