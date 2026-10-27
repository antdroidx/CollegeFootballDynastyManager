"""Freeze the existing dated contract/32-team evidence into a self-contained offline bundle.
No network, game-data changes, or guarantee inference. Requires pyarrow.
Raw observations retain their own dates; retrieval does not establish an effective balance date.
"""
import argparse,collections,gzip,hashlib,json,shutil,sys
from decimal import Decimal,ROUND_HALF_UP
from pathlib import Path

def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def dollars(value):return None if value is None else int((Decimal(str(value))*1_000_000).quantize(Decimal(1),rounding=ROUND_HALF_UP))
def stable_json(value):return (json.dumps(value,sort_keys=True,indent=2,allow_nan=False)+"\n").encode()
def preserve(source,target):
    target.parent.mkdir(parents=True,exist_ok=True)
    if target.exists():
        if digest(source)!=digest(target):raise ValueError("Refusing to overwrite dated source: "+str(target))
    else:shutil.copyfile(source,target)
    return {"file":str(target.name),"sha256":digest(target),"bytes":target.stat().st_size}

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("parquet",type=Path);parser.add_argument("team_cache",type=Path);parser.add_argument("output",type=Path)
    parser.add_argument("--report",required=True,type=Path);parser.add_argument("--python-libs",type=Path)
    args=parser.parse_args()
    if args.python_libs:sys.path.insert(0,str(args.python_libs))
    import pyarrow.parquet as pq
    root=Path(__file__).resolve().parents[1]
    identity=json.loads((root/"docs/data/nfl-contract-identity-audit-2026-10-26.json").read_text(encoding="utf-8"))
    if digest(args.parquet)!=identity["contract_sha256"]:raise ValueError("Parquet differs from reviewed source snapshot")
    league=json.loads((root/"src/ProFootball.Core/league.json").read_text(encoding="utf-8"))["Teams"]
    expected={t["Abbreviation"] for t in league};observations={t["team"]:t for t in identity["teams"]}
    if len(expected)!=32 or set(observations)!=expected:raise ValueError("Require exactly32 unique reviewed team observations")
    for abbr,item in observations.items():
        if digest(args.team_cache/(abbr+".html"))!=item["source_sha256"]:raise ValueError("Cached team evidence changed: "+abbr)
    table=pq.read_table(args.parquet);rows=table.to_pylist();active=[r for r in rows if r.get("is_active")]
    normalized=[]
    money=["base_salary","prorated_bonus","option_bonus","roster_bonus","guaranteed_salary","cap_number","cash_paid","workout_bonus","per_game_roster_bonus","other_bonus"]
    for r in active:
        seasons=[]
        for row in r.get("season_history") or []:
            seasons.append({"year":row["year"],"team":row["team"],**{key:dollars(row.get(key)) for key in money},"cap_percent":row.get("cap_percent")})
        normalized.append({"otcId":r["otc_id"],"gsisId":r["gsis_id"],"player":r["player"],"sourceTeam":r["team"],"sourceUrl":r["player_page"],"signedYear":r["year_signed"],"reportedYears":r["years"],"reportedValue":dollars(r["value"]),"reportedApy":dollars(r["apy"]),"reportedGuarantees":dollars(r["guaranteed"]),"seasons":seasons,"guaranteeVesting":"not provided by structured source","optionPaymentConditions":"not provided by structured source"})
    args.output.mkdir(parents=True,exist_ok=True)
    raw=preserve(args.parquet,args.output/"historical_contracts.parquet")
    teams=[]
    for abbr,item in sorted(observations.items()):
        saved=preserve(args.team_cache/(abbr+".html"),args.output/"teams"/(abbr+".html"))
        teams.append({"team":abbr,"url":item["source_url"],"file":"teams/"+saved["file"],"sha256":saved["sha256"],"observedOn":identity["observed"]})
    for name in ["nflpa-cap-observation-2026-10-26.json","nflpa-cap-observation-2026-10-26.csv","nfl-contract-identity-audit-2026-10-26.json","nfl-contract-status-review-2026-10-26.json","otc-obligations-observation-2026-10-26.csv"]:
        preserve(root/"docs/data"/name,args.output/"reviews"/name)
    preserve(root/"src/ProFootball.Core/nfl_2026_salary_caps.csv",args.output/"reviews/nfl_2026_salary_caps.csv")
    for name in ["identity-audit.json","obligations-audit.json"]:
        preserve(args.team_cache/name,args.output/"reviews"/name)
    content=stable_json({"formatVersion":1,"moneyUnits":"integer USD; null means unavailable, never zero by inference","contracts":sorted(normalized,key=lambda r:(r["otcId"] or 0,r["signedYear"] or 0))})
    packed=gzip.compress(content,mtime=0);target=args.output/"current-contracts.json.gz"
    if target.exists() and target.read_bytes()!=packed:raise ValueError("Refusing changed normalized dataset in dated bundle")
    target.write_bytes(packed)
    manifest={"formatVersion":1,"observedOn":identity["observed"],"sourceTimestamp":(table.schema.metadata or {}).get(b"nflverse_timestamp",b"").decode(),"rawContracts":raw,"rawRows":len(rows),"currentSourceContracts":len(normalized),"uniqueCurrentOtcIds":len({r["otcId"] for r in normalized}),"teamCount":len(teams),"teams":teams,"normalizedFile":target.name,"normalizedSha256":digest(target),"strictIdentitySummary":identity["summary"],"scope":"Offline developer source bundle. Raw historical rows and current source status are retained separately; no game ownership, contract, debt or opening room is changed. Annual guaranteed_salary is a reported cash measure, not automatically guaranteed base. Missing vesting/payment conditions remain unresolved. No real-source default promotion."}
    grouped=collections.defaultdict(list)
    for row in normalized:grouped[row["otcId"]].append(row)
    manifest["duplicateCurrentIdentities"]=[{"otcId":key,"sourceRows":[{"player":r["player"],"sourceTeam":r["sourceTeam"],"signedYear":r["signedYear"],"reportedValue":r["reportedValue"]} for r in values]} for key,values in sorted(grouped.items()) if len(values)>1]
    manifest["reviewFiles"]=[{"file":"reviews/"+p.name,"sha256":digest(p)} for p in sorted((args.output/"reviews").iterdir()) if p.is_file()]
    encoded=stable_json(manifest);(args.output/"manifest.json").write_bytes(encoded);args.report.write_bytes(encoded)
    print(json.dumps({key:manifest[key] for key in ["rawRows","currentSourceContracts","uniqueCurrentOtcIds","teamCount","normalizedSha256"]}))
if __name__=="__main__":main()
