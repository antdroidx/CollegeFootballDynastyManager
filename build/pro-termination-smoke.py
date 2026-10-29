"""Exact-APK waiver assignment and June1 designation acceptance on core-generated saves."""
import gzip,importlib.util,json,time
from pathlib import Path

def run(s):
    package="antdroid.profootball.franchise"
    data=json.loads(gzip.decompress(next(Path("apk").glob("**/pro-staff-termination-ui-fixture.json.gz")).read_bytes()))
    def ready(name,predicate):
        def check(root):
            assert "Unable to continue" not in s.texts(root),"App error at "+name
            return predicate(root)
        return s.wait_for("pro-termination-"+name,check,timeout=180)
    def click(name,label,start=False):
        root=s.scroll_to_text("pro-termination-"+name,label,max_swipes=25,toward_start=start);s.tap(root,label)
    def read(slot):
        return json.loads(s.adb("exec-out","run-as",package,"cat","files/pro-franchises-v1/"+slot+".json"))
    def saved(slot,predicate):
        end=time.monotonic()+90
        while time.monotonic()<end:
            checked=read(slot)
            if predicate(checked):return checked
            time.sleep(1)
        raise AssertionError("Transaction was not persisted")
    def open_slot(fixture,week=None):
        s.adb("shell","am","force-stop",package)
        s.adb("shell","monkey","-p",package,"-c","android.intent.category.LAUNCHER","1")
        ready("library",s.library_loaded)
        phase="Offseason" if fixture["name"].endswith("June1 QA") else "RegularSeason"
        click("slot",f"{fixture['name']} · {fixture['userTeam']} · {fixture['year']} {phase} W{week or fixture['week']}")
        ready("overview",s.pro_overview_loaded)
    def install(fixture):
        s.adb("shell","am","force-stop",package)
        path=Path("pro-termination-fixture.json");path.write_text(json.dumps(fixture),encoding="utf-8")
        s.adb("push",str(path),"/data/local/tmp/pro-termination-fixture.json")
        s.adb("shell","run-as",package,"mkdir","-p","files/pro-franchises-v1")
        s.adb("shell","run-as",package,"cp","/data/local/tmp/pro-termination-fixture.json","files/pro-franchises-v1/"+fixture["id"]+".json")
        open_slot(fixture)
    def roster_profile(name):
        click("roster","Roster")
        root=s.scroll_to_text("pro-termination-search","Find a player",max_swipes=20,toward_start=True)
        s.tap_id(root,"pro-roster-search");s.adb("shell","input","text",name.replace(" ","%s"));s.adb("shell","input","keyevent","4")
        click("player",name);ready("profile",lambda r:"Player profile" in s.texts(r))
    fixture=data["release"];install(fixture);roster_profile("June1 Release")
    click("roster-actions","Contract & roster actions");click("release-action","Release player")
    ready("release-preview",lambda r:"Cap impact of release" in s.texts(r))
    toggle=s.scroll_to_text("pro-termination-june1","Use a June 1 designation · maximum two per team",max_swipes=20)
    s.tap_id(toggle,"pro-june1-release")
    ready("designation-preview",lambda r:"Cap impact of release" in s.texts(r));s.capture("pro-termination-june1-preview")
    click("release-confirm","Release player")
    checked=saved(fixture["id"],lambda x:len(x.get("june1Designations",[]))==1)
    target=data["releasePlayer"]
    assert not any(c["active"] and c["playerId"]==target for c in checked["contracts"])
    def debt_key(d):return tuple(d.get(k,False if k in ["june1Hold","retainedContract","retainedBonus"] else 0) for k in ["team","year","playerId","amount","cashAmount","june1Hold","retainedContract","retainedBonus"])
    assert sorted(map(debt_key,checked["deadCap"]))==sorted(map(debt_key,data["releaseDebt"]))
    assert any(d["june1Hold"] for d in checked["deadCap"])
    open_slot(fixture);assert len(read(fixture["id"])["june1Designations"])==1
    fixture=data["waiver"];install(fixture);click("office","Front Office");click("free-agents","Free agents")
    search=s.scroll_to_text("pro-termination-free-agent-search","Find a free agent",max_swipes=20,toward_start=True)
    s.tap(search,"Find a free agent");s.adb("shell","input","text","Waiver%sRookie");s.adb("shell","input","keyevent","4")
    click("waiver-player","Waiver Rookie")
    claim=ready("waiver-profile",lambda r:"Claim from waivers" in s.texts(r));s.tap(claim,"Claim from waivers")
    confirm=ready("claim-confirm",lambda r:"Claim Waiver Rookie?" in s.texts(r))
    assert any("$3,000,000" in (t or "") and "2028" in (t or "") for t in s.texts(confirm))
    s.capture("pro-termination-waiver-confirm");s.tap(confirm,"Claim")
    submitted=ready("claim-submitted",lambda r:"Claim submitted" in s.texts(r));assert not s.enabled(submitted,"Claim submitted")
    pending=read(fixture["id"]);assert pending["contracts"]==fixture["contracts"]
    assert pending["userTeam"] in pending["waivers"][0]["claims"]
    click("close-profile","‹ Back",True);click("overview-return","Overview")
    click("advance","Advance week",True)
    confirm=ready("advance-confirm",lambda r:"Advance week?" in s.texts(r));s.tap(confirm,"Advance")
    checked=saved(fixture["id"],lambda x:x["week"]==11)
    acquired=[c for c in checked["contracts"] if c["playerId"]==data["waiverPlayer"] and c["active"]]
    assert acquired==[data["assumed"]],"Waiver award repriced or shortened the existing contract"
    assert next(p for p in checked["players"] if p["football"]["playerId"]==data["waiverPlayer"])["football"]["teamName"]==fixture["userTeam"]
    open_slot(fixture,week=11);roster_profile("Waiver Rookie")
    assert "Claim from waivers" not in s.texts(s.capture("pro-termination-waiver-awarded"))
    click("assumed-contract-actions","Contract & roster actions")
    click("assumed-release-action","Release player")
    ready("assumed-release-preview",lambda r:"Cap impact of release" in s.texts(r))
    click("assumed-release-confirm","Release player")
    cut=saved(fixture["id"],lambda x:not any(c["active"] and c["playerId"]==data["waiverPlayer"] for c in x["contracts"]))
    current=next(c for c in cut["contracts"] if c["playerId"]==data["waiverPlayer"] and c["team"]==fixture["userTeam"])
    assert current["earnedCash"]==data["postClaimReleaseCash"]==333333,"Buyer earned salary from before the claim"
    profile=ready("released-profile-updated",lambda r:"Released by your team" in s.texts(r))
    assert not s.enabled(profile,"Released by your team")
    assert any("Free agent" in (text or "") for text in s.texts(profile))
    assert "Contract & roster actions" not in s.texts(profile)
    s.capture("pro-termination-assigned-release")
    Path("pro-termination-check.json").write_text(json.dumps({"june1HoldPreserved":True,"savedReleaseMatchesCore":True,"waiverSalaryAndTermReviewed":True,"claimDoesNotTransferEarly":True,"awardedContractEqualsCore":True,"reloadVerified":True,"midseasonAccrualVerified":True,"releasedProfileRefreshed":True,"ownClaimDisabled":True},indent=2),encoding="utf-8")
    print("PASS: June1 preview/held savings/persisted designation and original-contract waiver claim/award/reload.")

if __name__=="__main__":
    spec=importlib.util.spec_from_file_location("shared_smoke",Path(__file__).with_name("android-ui-smoke.py"));s=importlib.util.module_from_spec(spec);spec.loader.exec_module(s)
    try:run(s)
    finally:Path("pro-termination-logcat.txt").write_bytes(s.adb("logcat","-d","-t","1000"))
