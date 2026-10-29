"""Stage source-backed missing annual contracts; never invent absent cash or overwrite app data."""
import argparse,collections,json
from pathlib import Path

def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument("pages",type=Path);p.add_argument("output",type=Path);p.add_argument("--report",type=Path,required=True);a=p.parse_args()
 root=Path(__file__).resolve().parents[1];evidence=json.loads(a.pages.read_text(encoding="utf-8"));pages={p["otcId"]:p for p in evidence["pages"]};ledger=json.loads((root/"docs/data/pro-opening-ledger-comparison-2026-10-27.json").read_text(encoding="utf-8"))
 players={p["Football"]["PlayerId"]:p for p in json.loads((root/"src/ProFootball.Core/nfl-snapshot.json").read_text(encoding="utf-8"))["Players"]};out=[];decisions=[]
 for team in ledger["teams"]:
  for row in team["currentRows"]:
   if row["classification"] not in ["missing_source_contract","cap_difference"]:continue
   try:
    if row["playerId"] not in players:raise ValueError("No verified game identity")
    page=pages.get(row["otcId"])
    if page is None:raise ValueError("No cached player page")
    years=[r for r in page["current"] if r["year"]>=2026];current=[r for r in years if r["year"]==2026]
    if len(current)!=1 or current[0].get("cap")!=row["reportedCap"]:raise ValueError("Current-page/team-snapshot charge differs or missing")
    if any(r.get("salary") is None and not r["void"] or r.get("cap") is None for r in years):raise ValueError("Missing annual salary or cap")
    signed=page.get("signedYear")
    if signed is None:raise ValueError("No reported signing year")
    full=page.get("reportedValue");fully_guaranteed=full is not None and full==page.get("reportedFullyGuaranteed")
    signing=sum((r.get("signingProration") or 0) for r in years)
    annual_other=lambda r:sum(r.get(k) or 0 for k in ["rosterCash","perGameCash","workoutCash","otherCash"])
    face=sum((r.get("salary") or 0)+annual_other(r) for r in years if not r["void"])+signing
    new_cash_proof=signed==2026 and face==full and not any(r.get("optionProration") for r in years)
    rows=[];bonus_cash={}
    for r in years:
     year=r["year"];salary=0 if r["void"] else r["salary"];other=0 if r["void"] else annual_other(r);proration=(r.get("signingProration") or 0)+(r.get("optionProration") or 0)
     if r["void"]:cash=0;bonus=0
     elif new_cash_proof:cash=salary+other+(signing if year==signed else 0);bonus=signing if year==signed else 0
     else:
      published=None if "Extension" in page.get("kind","") or signed!=2026 else page.get("annualCash",{}).get(str(year))
      historic=[h for h in page["history"] if h["year"]==year and h.get("cashPaid") is not None and h.get("cap")==r["cap"]]
      if published is None and len(historic)!=1:raise ValueError("No unambiguous published annual cash")
      cash=historic[0]["cashPaid"] if len(historic)==1 else published;bonus=cash-salary-other
      if len(historic)!=1 and bonus>0 and (year!=signed or bonus!=r.get("preJune1TradeDead")):raise ValueError("Cash-flow extra payment lacks a matching current-club paid-bonus assessment")
      if bonus<0:raise ValueError("Partial-year cash needs explicit salary-rate treatment")
     bonus_cash[str(year)]=bonus
     rows.append({"Year":year,"Salary":salary,"Guaranteed":min(salary,r.get("reportedGuaranteedCash") or 0),"SourceGuaranteed":r.get("reportedGuaranteedCash"),"GuaranteedOtherCash":other if fully_guaranteed else 0,"ProratedBonus":proration,"OtherCompensation":r["cap"]-salary-proration,"OtherCash":other,"IsVoid":r["void"]})
    player=players[row["playerId"]];name=player["Football"]["TeamName"]
    c={"PlayerId":row["playerId"],"Team":name,"SignedYear":signed,"BonusYears":max(r["Year"] for r in rows)-signed+1,"SigningBonus":0,"BonusCash":bonus_cash,"AccountingVersion":1,"ReportedAnnualValue":page.get("reportedApy"),"Years":rows,"Estimated":False,"Source":"OverTheCap page · "+page["observedOn"],"Active":True}
    if player.get("RookieYear")==signed and player.get("DraftOverall"):c["OriginalRookieYear"]=signed
    if new_cash_proof and signing:
     count=sum(1 for r in years if r.get("signingProration"))
     if 1<=count<=5 and max(r["year"] for r in years if r.get("signingProration"))-signed+1==count and all((r.get("signingProration") or 0)==signing//count+(1 if r["year"]-signed<signing%count else 0) for r in years if signed<=r["year"]<signed+count):c["BonusPayments"]=[{"Year":signed,"Amount":signing,"AllocationYears":count,"GuaranteedAtSigning":True,"PaidAtExecution":True}]
    out.append(c);decisions.append({"team":team["team"],"name":row["name"],"playerId":row["playerId"],"otcId":row["otcId"],"status":"source_backed_candidate","sourceSha256":page["sha256"],"sourceUrl":page["sourceUrl"]})
   except ValueError as e:decisions.append({"team":team["team"],"name":row["name"],"playerId":row["playerId"],"otcId":row["otcId"],"status":"unresolved","reason":str(e)})
 a.output.write_text(json.dumps(out,indent=2)+"\n",encoding="utf-8")
 report={"scope":"Staged candidates only; no game pack, historical debt or ownership is changed. Exact published cap and annual cash are required. New signing cash is accepted only when the full published deal reconciles exactly with all published annual cash and signing allocations; no guarantee/vesting is inferred from residuals.","summary":dict(collections.Counter(r["status"] for r in decisions)),"unresolvedReasons":dict(collections.Counter(r.get("reason") for r in decisions if r["status"]=="unresolved")),"decisions":decisions};a.report.write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8");print(json.dumps({k:report[k] for k in ["summary","unresolvedReasons"]}))
if __name__=="__main__":main()
