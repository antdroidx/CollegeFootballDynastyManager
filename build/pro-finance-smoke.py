"""Targeted Pro financial acceptance; no unchanged GameCast/College walkthrough."""

import os
import csv
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

    root=ready(name+"-summary",lambda r:"Show cap breakdown" in s.texts(r) or contains(r,"Adjusted team cap $315,053,262"))
    if "Show cap breakdown" in s.texts(root):
        if name=="opening-budget":s.capture("pro-finance-compact-cap")
        s.tap(root,"Show cap breakdown")
    root=ready(name,lambda r:contains(r,"Adjusted team cap $315,053,262"))

    assert contains(root,"Rollover $15,029,706")

    assert contains(root,"Other adjustments $-1,176,444")

    assert contains(root,"Base league cap $301,200,000")

    return root

franchise_name="Cap import QA "+str(int(time.time()))

try:

    library=ready("library",lambda r:any(n.get("text")=="New franchise" and n.get("enabled")=="true" for n in r.iter("node")))

    s.tap(library,"New franchise");create=ready("create",lambda r:"Start franchise" in s.texts(r));s.tap(create,"My franchise")

    s.adb("shell","input","keyevent","123");s.adb("shell","input","keyevent",*(["67"]*32));s.adb("shell","input","text",franchise_name.replace(" ","%s"));s.adb("shell","input","keyevent","4")

    # The real-roster option is the normal new-franchise default.
    s.scroll_to_text("pro-finance-contract-default","Real roster contracts",max_swipes=10)

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

    library=ready("saved-library",s.library_loaded)

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

    assert saved["capRulesVersion"]==3,"New franchises must persist seasonal cap counting"

    assert saved["openingFinancialSnapshot"]=="2026-10-27"
    for rookie_name,value in (("Kenyon Sadiq",22_344_304),("Omar Cooper Jr.",17_474_196),("Rueben Bain Jr.",23_760_242)):
        rookie=next(p for p in saved["players"] if p["football"]["fullName"]==rookie_name)
        deal=next(c for c in saved["contracts"] if c["active"] and c["playerId"]==rookie["football"]["playerId"])
        assert not deal["estimated"] and deal["originalRookieYear"]==2026
        assert [y["year"] for y in deal["years"]]==[2026,2027,2028,2029]
        assert sum(y["salary"]+y["otherCash"] for y in deal["years"])+deal["bonusCash"]["2026"]==value

    observed=json.loads(Path("src/ProFootball.Core/pro-opening-ledger.json").read_text(encoding="utf-8"))["Entries"]
    debt=saved["deadCap"]
    assert len(debt)==len(observed)==735 and len({d["team"] for d in debt})==32
    assert sum(d["amount"] for d in debt)==1_591_660_715
    assert all(d["cashAmount"]==0 and d["year"]==2026 and d["sourceKey"] for d in debt)
    assert {d["sourceKey"]:d["amount"] for d in debt}=={d["SourceKey"]:d["Amount"] for d in observed}
    club=saved["userTeam"]
    international={d["playerId"] for d in saved["internationalPracticeDesignations"] if d["team"]==club and d["year"]==2026}
    contracts=[c for c in saved["contracts"] if c["active"] and c["team"]==club]
    charge=sum(y["salary"]+y["otherCompensation"]+(y["proratedBonus"] or 0) for c in contracts for y in c["years"] if y["year"]==2026)
    exempt=sum(y["salary"] for c in contracts if c["playerId"] in international for y in c["years"] if y["year"]==2026)
    assert charge-exempt+sum(d["amount"] for d in debt if d["team"]==club)==300_971_343
    assert sum(d["amount"] for d in debt if d["team"]==club)==4_844_656
    Path("pro-opening-ledger-check.json").write_text(json.dumps({"teams":32,"assessments":735,"noHistoricalCashInvented":True,"seattleLiabilities":300971343,"seattleRegularSeasonRoom":14081919,"sourceKeysPreserved":True},indent=2),encoding="utf-8")


    caps=saved["teamCapAllocations"];assert len(caps)==32 and len({r["teamAbbreviation"] for r in caps})==32

    assert all(r["baseCap"]+r["rollover"]+r["otherAdjustments"]==r["adjustedCap"] for r in caps)

    assert sum(r["otherAdjustments"]<0 for r in caps)==4

    Path("pro-finance-ledger-check.json").write_text(json.dumps({"teams":32,"negative_adjustments":4,"all_reconcile":True,"franchise_id":saved["id"]},indent=2))

    apk=next(Path("apk").glob("*-Signed.apk"));s.adb("install","-r",str(apk));s.adb("shell","monkey","-p","antdroid.profootball.franchise","-c","android.intent.category.LAUNCHER","1")

    ready("updated-library",s.library_loaded);click("updated-slot",franchise_name+" · Seattle Seahawks · 2026 Preseason W1")

    ready("updated-overview",lambda r:"Advance week" in s.texts(r));click("updated-office","Front Office");cap_view("updated-cap")

    click("options","Settings",True);options=ready("options",lambda r:"Appearance" in s.texts(r));s.tap(options,"Appearance");themes=ready("theme",lambda r:"Light" in s.texts(r));s.tap(themes,"Light");cap_view("light-budget")

    click("light-hub","Financial overview");cap_view("light-finance");s.capture("pro-finance-light")

    if os.environ.get("PRO_FINANCE_OPENING_ONLY")=="1":
        review=json.loads(Path("src/ProFootball.Core/pro-reviewed-contract-terms.json").read_text(encoding="utf-8"))
        correction=next(c for c in review["Contracts"] if c.get("ExpectedContract"))
        target=correction["PlayerId"]
        actual=next(c for c in saved["contracts"] if c["playerId"]==target and c["active"])
        assert next(y for y in actual["years"] if y["year"]==2026)["guaranteed"]==0
        assert next(y for y in actual["years"] if y["year"]==2026)["guaranteedOtherCash"]==4_000_000
        assert actual["bonusCash"]["2027"]==0 and next(y for y in actual["years"] if y["year"]==2027)["otherCash"]==5_000_000
        def camel(x):
            if isinstance(x,dict):return {k[:1].lower()+k[1:]:camel(v) for k,v in x.items()}
            if isinstance(x,list):return [camel(v) for v in x]
            return x
        # Two complete32-team disposable saves; only one source contract differs from the UI-created baseline.
        for locked in (False,True):
            fixture=copy.deepcopy(saved);fixture_id=str(uuid.uuid4());fixture_name="Guarantee migration QA "+("locked" if locked else "unchanged")
            original=camel(correction["ExpectedContract"])
            if locked:
                original.update(lastChangedYear=2026,source="Negotiated")
                next(y for y in original["years"] if y["year"]==2027)["salary"]+=250_000
            fixture.update(id=fixture_id,name=fixture_name,contracts=[original if c["playerId"]==target else c for c in fixture["contracts"]])
            fixture_path=Path("pro-guarantee-fixture.json");fixture_path.write_text(json.dumps(fixture),encoding="utf-8")
            s.adb("shell","am","force-stop","antdroid.profootball.franchise")
            s.adb("push",str(fixture_path),"/data/local/tmp/pro-guarantee-fixture.json")
            s.adb("shell","run-as","antdroid.profootball.franchise","cp","/data/local/tmp/pro-guarantee-fixture.json","files/pro-franchises-v1/"+fixture_id+".json")
            s.adb("shell","monkey","-p","antdroid.profootball.franchise","-c","android.intent.category.LAUNCHER","1")
            ready("guarantee-library",s.library_loaded)
            click("guarantee-slot",fixture_name+" · "+fixture["userTeam"]+" · 2026 Preseason W1")
            ready("guarantee-overview",s.pro_overview_loaded)
            click("guarantee-save","\u2039 Exit",True);ready("guarantee-saved",s.library_loaded)
            checked=json.loads(s.adb("exec-out","run-as","antdroid.profootball.franchise","cat","files/pro-franchises-v1/"+fixture_id+".json"))
            after=next(c for c in checked["contracts"] if c["playerId"]==target and c["active"])
            assert checked["deadCap"]==fixture["deadCap"] and checked["career"]==fixture["career"]
            if locked:
                for key in ("bonusCash","lastChangedYear","source"):assert after[key]==original[key],"Negotiated contract overwritten"
                for row in original["years"]:
                    current=next(y for y in after["years"] if y["year"]==row["year"])
                    assert all(current[k]==v for k,v in row.items()),"Negotiated annual terms overwritten"
                assert all(y.get("guaranteedOtherCash",0)==0 for y in after["years"])
            else:
                assert next(y for y in after["years"] if y["year"]==2026)["guaranteed"]==0
                assert next(y for y in after["years"] if y["year"]==2026)["guaranteedOtherCash"]==4_000_000
                assert after["bonusCash"]["2027"]==0 and next(y for y in after["years"] if y["year"]==2027)["otherCash"]==5_000_000
        Path("pro-guarantee-migration-check.json").write_text(json.dumps({"newSourceAllocationVerified":True,"unchangedSourceMigrated":True,"negotiatedDealPreserved":True,"bookedDebtAndCareerPreserved":True},indent=2),encoding="utf-8")
        print("PASS: sourced opening ledger/current-club contracts,32 teams, exact Seattle liabilities, named debt, compact accounting, themes/reinstall and guarded source-guarantee migration with negotiated saves preserved.")
        raise SystemExit(0)

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
        if not exercise:
            old_rows=list(csv.DictReader(Path("src/ProFootball.Core/nfl_2026_salary_caps.user-original.csv").open(encoding="utf-8")))
            fixture["capRulesVersion"]=2
            fixture["teamCapAllocations"]=[dict(year=2026,teamAbbreviation=r["team_abbr"],baseCap=int(r["base_cap_2026"]),rollover=int(r["rollover_from_2025"]),otherAdjustments=int(r["other_adjustments"]),adjustedCap=int(r["adjusted_cap_2026"])) for r in old_rows]
        s.adb("shell","am","force-stop","antdroid.profootball.franchise")
        fixture_path=Path("pro-option-fixture.json");fixture_path.write_text(json.dumps(fixture),encoding="utf-8")
        s.adb("push",str(fixture_path),"/data/local/tmp/pro-option-fixture.json")
        s.adb("shell","run-as","antdroid.profootball.franchise","cp","/data/local/tmp/pro-option-fixture.json","files/pro-franchises-v1/"+fixture_id+".json")
        s.adb("shell","monkey","-p","antdroid.profootball.franchise","-c","android.intent.category.LAUNCHER","1")
        ready("option-library",s.library_loaded)
        click("option-loaded-slot",fixture_name+" · "+fixture["userTeam"]+" · 2026 Offseason W1")
        ready("option-overview",lambda r:"Advance offseason" in s.texts(r))
        click("option-office","Front Office")
        if not exercise:
            click("cap-correction","Update 2026 cap budgets")
            confirm=ready("cap-correction-confirm",lambda r:"Update 2026 cap budgets?" in s.texts(r));s.tap(confirm,"Update")
            cap_view("cap-corrected")
            repaired=json.loads(s.adb("exec-out","run-as","antdroid.profootball.franchise","cat","files/pro-franchises-v1/"+fixture_id+".json"))
            assert repaired["capRulesVersion"]==3 and len(repaired["contracts"])==1 and len(repaired["contracts"][0]["years"])==4
            assert all(y["salary"]==1_000_000 and y["guaranteed"]==0 for y in repaired["contracts"][0]["years"])
        click("option-desk","Rookie options")
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

    trade_spec=importlib.util.spec_from_file_location("trade_smoke",Path(__file__).with_name("pro-trade-smoke.py"))
    trade_module=importlib.util.module_from_spec(trade_spec);trade_spec.loader.exec_module(trade_module);trade_module.run(s)
    coordinator_spec=importlib.util.spec_from_file_location("coordinator_smoke",Path(__file__).with_name("pro-coordinator-smoke.py"))
    coordinator_module=importlib.util.module_from_spec(coordinator_spec);coordinator_spec.loader.exec_module(coordinator_module);coordinator_module.run(s)

    termination_spec=importlib.util.spec_from_file_location("termination_smoke",Path(__file__).with_name("pro-termination-smoke.py"))
    termination_module=importlib.util.module_from_spec(termination_spec);termination_spec.loader.exec_module(termination_module);termination_module.run(s)

    print("PASS: new 2026 adjusted budget, 32 persisted allocations, signed adjustments, exact reconciliation, reinstall/save and financial UI themes, sourced cash, compact contract and expandable void accounting; synthetic rookie-option exercise/decline and saved guarantees; explicit old-budget correction with contracts retained; isolated CPU trade offer counter/accept/decline and persisted ownership.")

finally:

    Path("pro-finance-logcat.txt").write_bytes(s.adb("logcat","-d","-t","1000"))
