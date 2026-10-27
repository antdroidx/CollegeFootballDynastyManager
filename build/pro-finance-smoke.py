"""Targeted Pro financial acceptance; no unchanged GameCast/College walkthrough."""

import copy
import uuid
import importlib.util,json,re,time

from pathlib import Path

spec=importlib.util.spec_from_file_location("shared_smoke",Path(__file__).with_name("android-ui-smoke.py"));s=importlib.util.module_from_spec(spec);spec.loader.exec_module(s)

def ready(name,condition):

    def check(root):

        assert "Unable to continue" not in s.texts(root),"App error at "+name

        return condition(root)

    return s.wait_for("pro-finance-"+name,check,timeout=180)

def click(name,label,toward_start=False):

    root=s.scroll_to_text("pro-finance-"+name,label,max_swipes=20,toward_start=toward_start);s.tap(root,label)

def contains(root,value):return any(value in (t or "") for t in s.texts(root))

def cap_view(name):

    root=ready(name,lambda r:contains(r,"Adjusted team cap $302,100,000"))

    assert contains(root,"Rollover $15,030,000")

    assert contains(root,"Other adjustments $-14,130,000")

    assert contains(root,"Base league cap $301,200,000")

    return root

franchise_name="Cap import QA "+str(int(time.time()))

try:

    library=ready("library",lambda r:any(n.get("text")=="New franchise" and n.get("enabled")=="true" for n in r.iter("node")))

    s.tap(library,"New franchise");create=ready("create",lambda r:"Start franchise" in s.texts(r));s.tap(create,"My franchise")

    s.adb("shell","input","keyevent","123");s.adb("shell","input","keyevent",*(["67"]*32));s.adb("shell","input","text",franchise_name.replace(" ","%s"));s.adb("shell","input","keyevent","4")

    click("contract-mode","Game contracts")
    modes=ready("contract-mode-options",lambda r:"Imported contracts" in s.texts(r));s.tap(modes,"Imported contracts")

    click("start","Start franchise");ready("overview",lambda r:"Advance week" in s.texts(r));click("office","Front Office");cap_view("opening-budget")

    click("hub","Financial overview");cap_view("financial-overview");click("back","‹ Front Office")

    click("jsn-profile","Jaxon Smith-Njigba")
    root=s.scroll_to_text("pro-finance-jsn-term","Content · Playing contract through 2031",max_swipes=20)
    value=s.scroll_to_text("pro-finance-jsn-value","Contract value / year $42,150,000",max_swipes=10)
    assert contains(value,"Contract value / year $42,150,000")
    compact=s.scroll_to_text("pro-finance-compact-pay","Show accounting details",max_swipes=10)
    assert not contains(compact,"2032 · Void")
    s.capture("pro-finance-compact-contract")
    s.tap(compact,"Show accounting details")
    void=s.scroll_to_text("pro-finance-void-year","2032 · Void",max_swipes=12)
    assert contains(void,"2032 · Void")
    click("collapse-accounting","Hide accounting details",True)
    click("close-profile","‹ Back",True)
    ready("profile-closed",lambda r:"Overview" in s.texts(r))
    click("overview-return","Overview");click("save","\u2039 Exit",True)

    library=ready("saved-library",lambda r:any((t or "").startswith(franchise_name+" · Seattle Seahawks") for t in s.texts(r)))

    # Inspect only the disposable emulator save produced by the UI; never manufacture opening obligations.

    names=s.adb("shell","run-as","antdroid.profootball.franchise","ls","files/pro-franchises-v1").decode().splitlines();saved=None

    for name in names:

        if not re.fullmatch(r"[0-9a-fA-F-]+\.json",name):continue

        data=json.loads(s.adb("exec-out","run-as","antdroid.profootball.franchise","cat","files/pro-franchises-v1/"+name))

        if data.get("name")==franchise_name:saved=data;break

    assert saved is not None,"UI franchise was not persisted"

    jsn=next(p for p in saved["players"] if p["football"]["fullName"]=="Jaxon Smith-Njigba")
    contract=next(c for c in saved["contracts"] if c["active"] and c["playerId"]==jsn["football"]["playerId"])
    assert contract["accountingVersion"]==1 and contract["reportedAnnualValue"]==42_150_000
    assert max(y["year"] for y in contract["years"] if not y["isVoid"])==2031
    assert all(y["salary"]==0 for y in contract["years"] if y["isVoid"])
    assert contract["bonusCash"]["2026"]==35_000_000

    assert saved["capRulesVersion"]==2,"New franchises must persist seasonal cap counting"

    caps=saved["teamCapAllocations"];assert len(caps)==32 and len({r["teamAbbreviation"] for r in caps})==32

    assert all(r["baseCap"]+r["rollover"]+r["otherAdjustments"]==r["adjustedCap"] for r in caps)

    assert sum(r["otherAdjustments"]<0 for r in caps)==28

    Path("pro-finance-ledger-check.json").write_text(json.dumps({"teams":32,"negative_adjustments":28,"all_reconcile":True,"franchise_id":saved["id"]},indent=2))

    apk=next(Path("apk").glob("*-Signed.apk"));s.adb("install","-r",str(apk));s.adb("shell","monkey","-p","antdroid.profootball.franchise","-c","android.intent.category.LAUNCHER","1")

    library=ready("updated-library",lambda r:any((t or "").startswith(franchise_name+" · Seattle Seahawks") for t in s.texts(r)));s.tap(library,next(t for t in s.texts(library) if (t or "").startswith(franchise_name+" · Seattle Seahawks")))

    ready("updated-overview",lambda r:"Advance week" in s.texts(r));click("updated-office","Front Office");cap_view("updated-cap")

    click("options","Options",True);options=ready("options",lambda r:"Appearance" in s.texts(r));s.tap(options,"Appearance");themes=ready("theme",lambda r:"Light" in s.texts(r));s.tap(themes,"Light");cap_view("light-budget")

    click("light-hub","Financial overview");cap_view("light-finance");s.capture("pro-finance-light")

    # Clearly synthetic, isolated option fixtures; never alter the imported cap-audit franchise above.
    for exercise in (True, False):
        fixture=copy.deepcopy(saved);fixture_id=str(uuid.uuid4());player_id=str(uuid.uuid4())
        fixture_name="Synthetic option QA "+("exercise" if exercise else "decline")
        player=copy.deepcopy(next(p for p in saved["players"] if p["football"]["position"] in (0,"QB") and p["football"]["teamName"]==saved["userTeam"]))
        player.update(age=24,experience=3,creditedSeasons=3,rookieYear=2023,draftOverall=1,practiceSquad=False,reserve=False,retired=False,externalId="",contractConcernWeeks=0)
        player["football"].update(playerId=player_id,fullName="Option QA Rookie",teamName=saved["userTeam"])
        contract=dict(playerId=player_id,team=saved["userTeam"],signedYear=2023,originalRookieYear=2023,bonusYears=4,signingBonus=0,accountingVersion=1,active=True,estimated=False,lastChangedYear=0,
            years=[dict(year=y,salary=1_000_000,guaranteed=0,proratedBonus=0,otherCompensation=0,otherCash=0,isVoid=False) for y in range(2023,2027)])
        fixture.update(id=fixture_id,name=fixture_name,year=2026,week=1,phase=3,offseasonStep=1,players=[player],contracts=[contract],games=[],honors=[],deadCap=[],bids=[],rookieOptionDecisions=[],rookieParticipation=[],salarySamples=[],autoManageRookieOptions=False,
            optionTenders=[dict(decisionYear=2026,positionGroup="QB",tier=0,salary=3_000_000,estimatedHistory=True)])
        s.adb("shell","am","force-stop","antdroid.profootball.franchise")
        fixture_path=Path("pro-option-fixture.json");fixture_path.write_text(json.dumps(fixture),encoding="utf-8")
        s.adb("push",str(fixture_path),"/data/local/tmp/pro-option-fixture.json")
        s.adb("shell","run-as","antdroid.profootball.franchise","cp","/data/local/tmp/pro-option-fixture.json","files/pro-franchises-v1/"+fixture_id+".json")
        s.adb("shell","monkey","-p","antdroid.profootball.franchise","-c","android.intent.category.LAUNCHER","1")
        library=ready("option-library",lambda r:any((t or "").startswith(fixture_name+" ·") for t in s.texts(r)))
        s.tap(library,next(t for t in s.texts(library) if (t or "").startswith(fixture_name+" ·")))
        ready("option-overview",lambda r:"Advance offseason" in s.texts(r))
        click("option-office","Front Office");click("option-desk","Rookie options")
        ready("option-due",lambda r:"1 pending decisions" in s.texts(r))
        click("option-choice","Exercise option" if exercise else "Decline option")
        confirm=ready("option-confirm",lambda r:("Exercise fifth-year option?" if exercise else "Decline fifth-year option?") in s.texts(r))
        s.tap(confirm,"Exercise" if exercise else "Decline")
        ready("option-complete",lambda r:"0 pending decisions" in s.texts(r))
        s.scroll_to_text("pro-option-history","Exercised" if exercise else "Declined",max_swipes=12)
        s.capture("pro-option-"+("exercised" if exercise else "declined"))
        checked=json.loads(s.adb("exec-out","run-as","antdroid.profootball.franchise","cat","files/pro-franchises-v1/"+fixture_id+".json"))
        decision=checked["rookieOptionDecisions"];assert len(decision)==1 and decision[0]["exercised"]==exercise
        deal=checked["contracts"][0];assert deal["lastChangedYear"]==0
        if exercise:
            fourth=next(y for y in deal["years"] if y["year"]==2026);fifth=next(y for y in deal["years"] if y["year"]==2027)
            assert fourth["guaranteed"]==1_000_000 and fifth["salary"]==fifth["guaranteed"]==3_000_000
        else:assert max(y["year"] for y in deal["years"])==2026 and all(y["guaranteed"]==0 for y in deal["years"])

    print("PASS: new 2026 adjusted budget, 32 persisted allocations, signed adjustments, exact reconciliation, reinstall/save and financial UI themes, sourced cash, compact contract and expandable void accounting; synthetic rookie-option exercise/decline and saved guarantees.")

finally:

    Path("pro-finance-logcat.txt").write_bytes(s.adb("logcat","-d","-t","1000"))
