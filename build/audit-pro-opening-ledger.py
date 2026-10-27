"""Offline row-level source-pack comparison; never writes app data or inferred balancing entries."""
import argparse,collections,csv,hashlib,json
from pathlib import Path

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("identities",type=Path);parser.add_argument("obligations",type=Path);parser.add_argument("output",type=Path);args=parser.parse_args()
    root=Path(__file__).resolve().parents[1]
    identity=json.loads(args.identities.read_text(encoding="utf-8"));observation=json.loads(args.obligations.read_text(encoding="utf-8"))
    teams={t["team"]:t for t in observation["teams"]}
    if len(teams)!=32 or len(identity["teams"])!=32 or observation["errors"]:raise ValueError("Require complete reviewed32-team sources")
    pack_path=root/"src/ProFootball.Core/nfl-contracts.json";pack=json.loads(pack_path.read_text(encoding="utf-8"));by_player=collections.defaultdict(list)
    for c in pack:by_player[c["PlayerId"]].append(c)
    league=json.loads((root/"src/ProFootball.Core/league.json").read_text(encoding="utf-8"))["Teams"]
    names={t["Abbreviation"]:t["City"]+" "+t["Nickname"] for t in league}
    def charge(c):
        row=next((r for r in c["Years"] if r["Year"]==2026),None)
        return 0 if row is None or not c.get("Active",True) else row["Salary"]+(row.get("ProratedBonus") or 0)+row.get("OtherCompensation",0)
    results=[]
    for group in identity["teams"]:
        abbr=group["team"];observed=teams[abbr]
        if group["source_sha256"]!=observed["sha256"]:raise ValueError("Mixed source team snapshots")
        rows=[]
        for row in group["rows"]:
            if row["section"].casefold()=="dead money":continue
            contracts=[c for c in by_player.get(row.get("player_id"),[]) if c["Team"]==names[abbr] and c.get("Active",True)]
            amount=sum(charge(c) for c in contracts) if contracts else None
            classification="identity_or_status_review" if row["status"]!="verified_identifier" else "missing_source_contract" if amount is None else "cap_match" if amount==row["source_cap"] else "cap_difference"
            rows.append({"name":row["source_name"],"section":row["section"],"playerId":row.get("player_id"),"otcId":row["otc_id"],"classification":classification,"reportedCap":row["source_cap"],"packCap":amount,"difference":None if amount is None else amount-row["source_cap"]})
        dead=next((s for s in observed["sections"] if s["section"].casefold()=="dead money"),None)
        # Report the observed named/aggregate gap as an unresolved source amount; never allocate it to players.
        results.append({"team":abbr,"sourceUrl":observed["url"],"sourceSha256":observed["sha256"],"reportedLiabilities":observed["liabilities"],"reportedRoom":observed["reported_room"],"packCurrentFullRosterCharges":sum(charge(c) for c in pack if c["Team"]==names[abbr]),"rowCounts":dict(collections.Counter(r["classification"] for r in rows)),"deadMoneySource":dead,"currentRows":rows})
    summary=collections.Counter()
    for t in results:summary.update(t["rowCounts"])
    if sum(summary.values())!=identity["summary"]["current_rows"]:raise ValueError("Current row decomposition does not reconcile to strict identity audit")
    if any(t["deadMoneySource"] is None for t in results):raise ValueError("Missing reported dead-money section")
    output={"observedOn":identity["observed"],"year":2026,"packSha256":hashlib.sha256(pack_path.read_bytes()).hexdigest(),"summary":{"teams":len(results),"currentRowClassifications":dict(summary)},"scope":"Developer-only comparison of current source rows against packaged sourced contracts. Generated fallbacks are not invented or included. Row comparisons are not summed as a game balance: source rows can represent prior-team spending, credits or repeated financial events. Complete dead-money sections are retained, not automatically booked against current players. Reported room is not an opening balance or balancing target.","teams":results}
    args.output.write_text(json.dumps(output,indent=2)+"\n",encoding="utf-8");print(json.dumps(output["summary"]))
if __name__=="__main__":main()
