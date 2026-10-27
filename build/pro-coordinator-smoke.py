"""Scoped coordinator retention and delegation acceptance; fixtures are disposable and synthetic."""
import gzip
import importlib.util
import json
from pathlib import Path


def run(s):
    def contains(root,value):return any(value in (t or "") for t in s.texts(root))
    def ready(name,predicate):
        def check(root):
            assert "Unable to continue" not in s.texts(root),"App error at "+name
            return predicate(root)
        return s.wait_for("pro-coordinator-"+name,check,timeout=180)
    def click(name,label,toward_start=False):
        root=s.scroll_to_text("pro-coordinator-"+name,label,max_swipes=20,toward_start=toward_start);s.tap(root,label)
    packs=sorted(Path("apk").glob("**/pro-coordinator-ui-*.json.gz"));assert len(packs)==2
    s.adb("shell","run-as","antdroid.profootball.franchise","mkdir","-p","files/pro-franchises-v1")
    for pack in packs:
        data=json.loads(gzip.decompress(pack.read_bytes()));fixture=data["franchise"];fixture_id=fixture["id"]
        role=fixture["career"]["role"];s.adb("shell","am","force-stop","antdroid.profootball.franchise")
        path=Path("pro-coordinator-fixture.json");path.write_text(json.dumps(fixture),encoding="utf-8")
        s.adb("push",str(path),"/data/local/tmp/pro-coordinator-fixture.json")
        s.adb("shell","run-as","antdroid.profootball.franchise","cp","/data/local/tmp/pro-coordinator-fixture.json","files/pro-franchises-v1/"+fixture_id+".json")
        s.adb("shell","monkey","-p","antdroid.profootball.franchise","-c","android.intent.category.LAUNCHER","1")
        ready("library",s.library_loaded)
        click("slot",f"{fixture['name']} · {fixture['userTeam']} · {fixture['year']} Offseason W{fixture['week']}")
        ready("overview",lambda r:"Advance offseason" in s.texts(r))
        click("office","Front Office");click("retention","Expiring contracts")
        ready("delegation",lambda r:contains(r,"Staff handle retention for the other unit automatically."))
        assert "Staff negotiate my unit's expiring contracts" in s.texts(s.capture("pro-coordinator-"+str(role)+"-retention"))
        click("back","‹ Front Office",True);click("overview-return","Overview")
        click("advance","Advance offseason")
        confirm=ready("advance-confirm",lambda r:"Advance offseason?" in s.texts(r));s.tap(confirm,"Advance")
        ready("next-year",lambda r:contains(r,str(fixture["year"]+1)+" · Offseason"))
        checked=json.loads(s.adb("exec-out","run-as","antdroid.profootball.franchise","cat","files/pro-franchises-v1/"+fixture_id+".json"))
        assert checked["year"]==fixture["year"]+1 and checked["offseasonStep"]==1
        assert checked["career"]["role"]==role and checked["career"]["name"]==fixture["career"]["name"]
        assert not checked["autoNegotiateExpiring"]
        own=next(p for p in checked["players"] if p["football"]["playerId"]==data["ownPlayer"])
        other=next(p for p in checked["players"] if p["football"]["playerId"]==data["otherPlayer"])
        assert own["football"]["teamName"]=="Free agent" and other["football"]["teamName"]==fixture["userTeam"]
        assert any(c["active"] and c["playerId"]==data["otherPlayer"] and max(y["year"] for y in c["years"] if not y["isVoid"])>=checked["year"] for c in checked["contracts"])
        assert any(d["approved"] and d["action"].startswith("Staff retention ·") for d in checked["gmDecisions"])
        s.capture("pro-coordinator-"+str(role)+"-advanced")
    staff_packs=sorted([*Path("apk").glob("**/pro-staff-roster-ui-*.json.gz"),*Path("apk").glob("**/pro-staff-draft-ui-*.json.gz")]);assert len(staff_packs)==4
    for pack in staff_packs:
        data=json.loads(gzip.decompress(pack.read_bytes()));fixture=data["franchise"];fixture_id=fixture["id"]
        kind="draft" if "-draft-" in pack.name else "roster";role=fixture["career"]["role"]
        s.adb("shell","am","force-stop","antdroid.profootball.franchise")
        path=Path("pro-coordinator-fixture.json");path.write_text(json.dumps(fixture),encoding="utf-8")
        s.adb("push",str(path),"/data/local/tmp/pro-coordinator-fixture.json")
        s.adb("shell","run-as","antdroid.profootball.franchise","cp","/data/local/tmp/pro-coordinator-fixture.json","files/pro-franchises-v1/"+fixture_id+".json")
        s.adb("shell","monkey","-p","antdroid.profootball.franchise","-c","android.intent.category.LAUNCHER","1")
        ready("staff-library",s.library_loaded)
        phase="Offseason" if kind=="draft" else "RegularSeason"
        click("staff-slot",f"{fixture['name']} · {fixture['userTeam']} · {fixture['year']} {phase} W{fixture['week']}")
        ready("staff-overview",s.pro_overview_loaded)
        s.scroll_to_text("pro-coordinator-staff-overview-top","Advance offseason" if kind=="draft" else "Advance week",max_swipes=20,toward_start=True)
        if kind=="draft":
            click("staff-office","Front Office");click("staff-draft","Draft & scouting")
            s.scroll_to_text("pro-coordinator-staff-draft-top","Draft & scouting",max_swipes=20,toward_start=True)
            ready("staff-draft-ready",lambda r:contains(r,"R1 #2") and contains(r,"The GM selects for the other unit automatically"))
        else:
            click("staff-advance","Advance week")
            confirm=ready("staff-advance-confirm",lambda r:"Advance week?" in s.texts(r));s.tap(confirm,"Advance")
            ready("staff-next-week",lambda r:contains(r,"Week "+str(fixture["week"]+1)))
        checked=json.loads(s.adb("exec-out","run-as","antdroid.profootball.franchise","cat","files/pro-franchises-v1/"+fixture_id+".json"))
        assert checked["career"]==fixture["career"] and checked["userTeam"]==fixture["userTeam"]
        assert next(p for p in checked["players"] if p["football"]["playerId"]==data["player"])["football"]["teamName"]==fixture["userTeam"]
        controlled=set(data["controlledPlayers"])
        assert [c for c in checked["contracts"] if c["playerId"] in controlled]==[c for c in fixture["contracts"] if c["playerId"] in controlled]
        assert all(p["football"]["teamName"]==fixture["userTeam"] for p in checked["players"] if p["football"]["playerId"] in controlled)
        if kind=="draft":
            assert checked["draftIndex"]==1 and len(checked["draftHistory"])==1
            assert any(d["approved"] and d["action"].startswith("GM draft selection") for d in checked["gmDecisions"])
        else:
            assert checked["week"]==fixture["week"]+1
            assert any(d["approved"] and d["action"].startswith("Staff free-agent offer") for d in checked["gmDecisions"])
        s.capture("pro-coordinator-"+str(role)+"-"+kind+"-verified")
    print("PASS: OC/DC retention UI and own-unit delegation, opposite-unit automatic renewal, manual-own expiry, staff free-agent fill, GM other-unit draft selection/own-unit stop, preserved controlled contracts and coach identity.")


if __name__=="__main__":
    spec=importlib.util.spec_from_file_location("shared_smoke",Path(__file__).with_name("android-ui-smoke.py"));s=importlib.util.module_from_spec(spec);spec.loader.exec_module(s)
    try:run(s)
    finally:Path("pro-coordinator-logcat.txt").write_bytes(s.adb("logcat","-d","-t","1000"))
