"""Prepare observed opening debt, retaining provenance and unresolved identities; never infer cash."""
import argparse, hashlib, json
from pathlib import Path

def prepare(source, league):
    if source["version"] != 1:
        raise ValueError("Unsupported opening ledger version")
    expected = {t["Abbreviation"] for t in league["Teams"]}
    teams = source["teams"]
    if len(teams) != 32 or {t["team"] for t in teams} != expected:
        raise ValueError("Exactly32 distinct league teams required")
    keys = set()
    output = []
    for t in teams:
        if t["year"] != 2026 or t["observedOn"] != "2026-10-27" or len(t["sourceSha256"]) != 64:
            raise ValueError("Dated2026 evidence required")
        if sum(e["amount"] for e in t["entries"]) != t["reportedTotal"]:
            raise ValueError("Debt aggregate does not reconcile: " + t["team"])
        for e in t["entries"]:
            if type(e["amount"]) is not int or e["amount"] < 0 or e["key"] in keys:
                raise ValueError("Invalid/duplicate source assessment")
            keys.add(e["key"])
            output.append({"Team":t["team"],"Year":2026,"PlayerId":e["playerId"],"Amount":e["amount"],
                           "SourceKey":e["key"],"SourceName":e["name"],"SourceKind":e["kind"],
                           "SourceUrl":t["sourceUrl"],"SourceSha256":t["sourceSha256"],"ObservedOn":t["observedOn"]})
    return {"Version":1,"ObservedOn":"2026-10-27","Entries":output}

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument("output",type=Path)
    a=p.parse_args()
    root=Path(__file__).resolve().parents[1]
    source=json.loads((root/"docs/data/pro-opening-dead-cap-2026-10-27.json").read_text(encoding="utf-8"))
    league=json.loads((root/"src/ProFootball.Core/league.json").read_text(encoding="utf-8"))
    result=prepare(source,league)
    a.output.write_text(json.dumps(result,indent=2)+"\n",encoding="utf-8")
    print("PASS32 teams;",len(result["Entries"]),"assessments;",sum(e["Amount"] for e in result["Entries"]),"dollars; no cash or future debt inferred; SHA",hashlib.sha256(a.output.read_bytes()).hexdigest())
if __name__=="__main__":main()
