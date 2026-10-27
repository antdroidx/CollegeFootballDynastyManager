"""Cache contract-note evidence for the reviewed future-bonus cohort, then work offline.
Requires beautifulsoup4. At most two public requests concurrently; no automatic financial inference.
"""
import argparse,concurrent.futures,gzip,hashlib,json,re,threading
from datetime import date,datetime,timezone
from pathlib import Path
from urllib.request import Request,urlopen
from bs4 import BeautifulSoup

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("bundle",type=Path);parser.add_argument("output",type=Path)
    parser.add_argument("--observed",required=True,type=date.fromisoformat);parser.add_argument("--report",required=True,type=Path)
    parser.add_argument("--offline",action="store_true");parser.add_argument("--max-fetches",type=int,default=8,help="Bound online observations per invocation; missing pages remain explicit")
    args=parser.parse_args()
    root=Path(__file__).resolve().parents[1]
    cohort=json.loads((root/"docs/data/pro-bonus-event-audit-2026-10-27.json").read_text(encoding="utf-8"))["contracts"]
    players={p["Football"]["PlayerId"]:p for p in json.loads((root/"src/ProFootball.Core/nfl-snapshot.json").read_text(encoding="utf-8"))["Players"]}
    source=json.loads(gzip.decompress((args.bundle/"current-contracts.json.gz").read_bytes()))["contracts"]
    args.output.mkdir(parents=True,exist_ok=True)
    fetch_count=0;fetch_gate=threading.Lock()
    def observe(item):
        nonlocal fetch_count
        try:
            gsis=players[item["playerId"]]["ExternalId"]
            candidates=[r for r in source if r["gsisId"]==gsis]
            ids={r["otcId"] for r in candidates}
            if len(ids)!=1:raise ValueError("No unique strict GSIS/OTC source identity")
            otc=next(iter(ids));urls={r["sourceUrl"] for r in candidates}
            if len(urls)!=1:raise ValueError("Conflicting source URLs")
            url=next(iter(urls))
            if not re.fullmatch(r"https://overthecap.com/player/[a-z0-9-]+/\d+/?",url):raise ValueError("Unexpected source URL")
            path=args.output/(str(otc)+".html");meta=args.output/(str(otc)+".observation.json")
            if not path.exists():
                if args.offline:raise ValueError("No cached note page")
                with fetch_gate:
                    if fetch_count>=args.max_fetches:raise ValueError("Online capture budget reached; source remains unresolved")
                    fetch_count+=1
                raw=urlopen(Request(url,headers={"User-Agent":"FootballSimulation-ContractResearch/1.0"}),timeout=12).read()
                soup=BeautifulSoup(raw,"html.parser")
                heading=soup.find(lambda t:t.name in ["h2","h3","h4","h5"] and t.get_text(" ",strip=True)=="Contract Notes")
                if heading is None:raise ValueError("No Contract Notes heading; response not cached as valid evidence")
                path.write_bytes(raw)
                meta.write_text(json.dumps({"sourceUrl":url,"retrievedAt":datetime.now(timezone.utc).isoformat(),"observedOn":str(args.observed),"sha256":hashlib.sha256(raw).hexdigest()},indent=2)+"\n",encoding="utf-8")
            raw=path.read_bytes();metadata=json.loads(meta.read_text(encoding="utf-8"))
            if metadata["sourceUrl"]!=url or metadata["observedOn"]!=str(args.observed) or metadata["sha256"]!=hashlib.sha256(raw).hexdigest():raise ValueError("Cached evidence provenance mismatch")
            soup=BeautifulSoup(raw,"html.parser");heading=soup.find(lambda t:t.name in ["h2","h3","h4","h5"] and t.get_text(" ",strip=True)=="Contract Notes")
            if heading is None:raise ValueError("Cached notes heading missing")
            parts=[]
            for sibling in heading.next_siblings:
                if getattr(sibling,"name",None) in ["h2","h3","h4","h5"]:break
                text=sibling.get_text(" ",strip=True) if hasattr(sibling,"get_text") else str(sibling).strip()
                if text:parts.append(text)
            notes="\n".join(parts)
            return {"playerId":item["playerId"],"player":item["name"],"otcId":otc,**metadata,"rawFile":path.name,"notes":notes,"reviewStatus":"unreviewed; not financial instructions"}
        except Exception as e:return {"playerId":item["playerId"],"player":item["name"],"error":str(e)}
    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as executor:results=list(executor.map(observe,cohort))
    results.sort(key=lambda r:r["playerId"])
    summary={"cohort":len(results),"captured":sum("notes" in r for r in results),"failures":sum("error" in r for r in results)}
    full={"observedOn":str(args.observed),"scope":"Offline reported contract notes for review only. No guarantee, vesting or source-data update is inferred.","summary":summary,"contracts":results}
    (args.output/"contract-notes.json").write_text(json.dumps(full,indent=2)+"\n",encoding="utf-8")
    report={**full,"contracts":[{k:v for k,v in r.items() if k!="notes"} for r in results]}
    args.report.write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(summary))
if __name__=="__main__":main()
