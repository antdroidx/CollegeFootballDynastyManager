"""Parse dated cached contract tables into integer-dollar evidence. Does not update game contracts."""
import argparse,json,re
from pathlib import Path
from bs4 import BeautifulSoup

def dollars(text):
    text=text.strip()
    if text in ["","Void","N/A","--","-"]:return None
    if not re.fullmatch(r"\(?-?\$[0-9,]+\)?",text):raise ValueError("Unrecognized money value "+text)
    return int(re.sub(r"[^0-9]","",text))*(-1 if "(" in text or "-" in text else 1)

def table_rows(table):
    if table is None:return []
    head=table.select_one("thead tr");headers=head.find_all(["th","td"],recursive=False)
    indices={}
    names={"year":"year","base-salary":"salary","prorated-bonus":"signingProration","option-bonus":"optionProration","regular-roster-bonus":"rosterCash","per-game-roster-bonus":"perGameCash","workout-bonus":"workoutCash","other-bonus":"otherCash","salary-guarantees":"reportedGuaranteedCash","cap-number":"cap"}
    offset=0
    for h in headers:
        for css,key in names.items():
            if css in h.get("class",[]):indices[key]=offset
        if h.get_text(" ",strip=True)=="Team":indices["team"]=offset
        if h.get_text(" ",strip=True)=="Cash Paid":indices["cashPaid"]=offset
        offset+=int(h.get("colspan",1))
    # Multi-row current-contract headers have signing/option and regular/per-game subcolumns.
    if table.select_one("thead tr:nth-of-type(2)"):
        for h in headers:
            if "prorated-bonus" in h.get("class",[]) and int(h.get("colspan",1))==2:indices["optionProration"]=indices["signingProration"]+1
            if "regular-roster-bonus" in h.get("class",[]) and int(h.get("colspan",1))==2:indices["perGameCash"]=indices["rosterCash"]+1
    rows=[]
    for tr in table.select("tbody tr"):
        cells=tr.find_all(["td","th"],recursive=False)
        if not cells:continue
        year=cells[indices.get("year",0)].get_text(" ",strip=True)
        if not re.fullmatch(r"\d{4}",year):continue
        row={"year":int(year)}
        for key,index in indices.items():
            if key=="year":continue
            value=cells[index].get_text(" ",strip=True)
            row[key]=value if key=="team" else dollars(value)
        row["void"]=any(c.get_text(" ",strip=True)=="Void" for c in cells)
        rows.append(row)
    return rows

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument("cache",type=Path);p.add_argument("output",type=Path);p.add_argument("--ledger",type=Path);a=p.parse_args()
    result=[];failures=[]
    for path in sorted(a.cache.glob("*.html")):
        try:
            soup=BeautifulSoup(path.read_bytes(),"html.parser");meta=json.loads(path.with_suffix(".observation.json").read_text(encoding="utf-8"))
            import hashlib
            if hashlib.sha256(path.read_bytes()).hexdigest()!=meta["sha256"]:raise ValueError("Raw page hash mismatch")
            current=table_rows(soup.select_one("table.current-contract"));history=table_rows(soup.select_one("table.salary-cap-history"))
            if not current and not history:raise ValueError("No annual contract rows")
            result.append({"otcId":int(path.stem),**meta,"current":current,"history":history})
        except Exception as e:failures.append({"file":path.name,"error":str(e)})
    comparisons=[]
    if a.ledger:
        by_id={r["otcId"]:r for r in result};ledger=json.loads(a.ledger.read_text(encoding="utf-8"))
        for team in ledger["teams"]:
            for row in team["currentRows"]:
                if row["classification"] not in ["missing_source_contract","cap_difference"]:continue
                page=by_id.get(row["otcId"]);annual=[] if page is None else [r for r in page["current"] if r["year"]==2026]
                comparisons.append({"team":team["team"],"playerId":row["playerId"],"otcId":row["otcId"],"name":row["name"],"recordedTeamCap":row["reportedCap"],"currentPageCap":annual[0].get("cap") if len(annual)==1 else None,"sameCap":len(annual)==1 and annual[0].get("cap")==row["reportedCap"]})
    output={"formatVersion":1,"scope":"Offline annual source evidence only; absent cash/guarantee values remain null. No game data, roster ownership or opening room is changed.","pages":result,"errors":failures,"currentComparisons":comparisons,"summary":{"pages":len(result),"errors":len(failures),"sameCap":sum(r["sameCap"] for r in comparisons),"differentOrMissingCap":sum(not r["sameCap"] for r in comparisons)}}
    a.output.write_text(json.dumps(output,indent=2)+"\n",encoding="utf-8");print(json.dumps(output["summary"]))
if __name__=="__main__":main()
