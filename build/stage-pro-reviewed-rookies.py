"""Review three fully guaranteed2026 rookie deals against independent annual cash/cap totals.
Does not modify app contracts. Retains source discrepancies instead of generic balancing.
"""
import argparse, json
from pathlib import Path

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('output',type=Path);p.add_argument('--report',type=Path,required=True);a=p.parse_args()
    root=Path(__file__).resolve().parents[1]
    pages={p['otcId']:p for p in json.loads((root/'.artifacts/pro-opening-contract-page-evidence-fixed.json').read_text(encoding='utf-8'))['pages']}
    ledger=json.loads((root/'docs/data/pro-opening-ledger-followup-2026-10-27.json').read_text(encoding='utf-8'))
    players={p['Football']['PlayerId']:p for p in json.loads((root/'src/ProFootball.Core/nfl-snapshot.json').read_text(encoding='utf-8'))['Players']}
    out=[];reports=[]
    for otc in (13160,13174,13159):
        team,identity=next((t,r) for t in ledger['teams'] for r in t['currentRows'] if r['otcId']==otc)
        player=players[identity['playerId']];page=pages[otc]
        if player['RookieYear']!=2026 or not 1<=player['DraftOverall']<=32 or page['signedYear']!=2026 or 'Drafted' not in page['kind']:
            raise ValueError('Require strict identity and original2026 first-round contract')
        years=[y for y in page['current'] if y['year']>=2026]
        if [y['year'] for y in years]!=list(range(2026,2030)) or any(y['void'] for y in years):raise ValueError('Require four playing years')
        annual={int(y):v for y,v in page['annualCash'].items()}
        if set(annual)!=set(range(2026,2030)) or sum(annual.values())!=page['reportedValue'] or page['reportedFullyGuaranteed']!=page['reportedValue']:
            raise ValueError('Full cash/guarantee/value proof absent')
        signing=sum(y.get('signingProration') or 0 for y in years)
        if any(y.get('optionProration') for y in years):raise ValueError('Unreviewed option structure')
        rows=[];proof=[]
        for y in years:
            year=y['year'];pay=annual[year]-(signing if year==2026 else 0);base=y['salary']
            other=sum(y.get(k) or 0 for k in ('rosterCash','perGameCash','workoutCash','otherCash'))
            method='published separate salary/other pay'
            if base+other!=pay:
                if base==pay and other>0:
                    base=pay-other;method='published salary cell includes separately shown roster pay; annual cash and cap confirm single inclusion'
                elif otc==13174 and year==2027 and base==1050000 and other==629628 and pay==1679282:
                    other=629282;method='explicit346-dollar roster-cell discrepancy; annual cash, full guarantee and cap independently agree'
                else:raise ValueError('Unreviewed annual cash inconsistency')
            proration=y.get('signingProration') or 0
            if base+other!=pay or pay+proration!=y['cap'] or min(base,other)<0:raise ValueError('Annual cash/cap does not reconcile')
            rows.append({'Year':year,'Salary':base,'Guaranteed':base,'SourceGuaranteed':y.get('reportedGuaranteedCash'),'GuaranteedOtherCash':other,'ProratedBonus':proration,'OtherCompensation':other,'OtherCash':other,'IsVoid':False})
            proof.append({'year':year,'cash':annual[year],'playingPay':pay,'baseSalary':base,'otherCash':other,'allocatedBonus':proration,'cap':y['cap'],'normalization':method})
        if rows[0]['Salary']+rows[0]['OtherCash']!=885000 or rows[0]['Salary']+rows[0]['OtherCash']+rows[0]['ProratedBonus']!=identity['reportedCap']:
            raise ValueError('Opening team charge does not match')
        c={'PlayerId':identity['playerId'],'Team':player['Football']['TeamName'],'SignedYear':2026,'OriginalRookieYear':2026,'BonusYears':4,'SigningBonus':0,'BonusCash':{str(y):signing if y==2026 else 0 for y in range(2026,2030)},'BonusPayments':[{'Year':2026,'Amount':signing,'AllocationYears':4,'GuaranteedAtSigning':True,'PaidAtExecution':True}],'AccountingVersion':1,'ReportedAnnualValue':page['reportedApy'],'Years':rows,'Estimated':False,'Source':'OverTheCap reviewed rookie cash/cap reconciliation · '+page['observedOn'],'Active':True}
        if any(r['ProratedBonus']!=signing//4+(1 if i<signing%4 else 0) for i,r in enumerate(rows)):raise ValueError('Unreviewed source proration rounding')
        out.append(c);reports.append({'player':player['Football']['FullName'],'playerId':identity['playerId'],'draftOverall':player['DraftOverall'],'team':team['team'],'sourceUrl':page['sourceUrl'],'sourceSha256':page['sha256'],'reportedValue':page['reportedValue'],'reportedFullyGuaranteed':page['reportedFullyGuaranteed'],'signingCash':signing,'annualProof':proof})
    a.output.write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
    a.report.write_text(json.dumps({'scope':'Staged review only. Three strict-identity fully guaranteed original first-round contracts. Annual published cash, cap and full contract value reconcile exactly. Four playing seasons; fifth-year decision is separate. One explicitly identified346-dollar source-cell inconsistency is retained in evidence. No generic rounding/balancing or app/save update.','contracts':reports},indent=2)+'\n',encoding='utf-8')
    print('PASS3 original first-round deals; four playing seasons; all annual cash/cap/guarantee/value proofs exact; no app writes.')
if __name__=='__main__':main()
