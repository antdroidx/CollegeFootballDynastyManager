"""Two genuinely new fictional franchises, distinct rosters and unchanged reload."""
from pathlib import Path
exec(compile(Path(__file__).with_name("pro-staff-calendar-smoke.py").read_text(encoding="utf-8").split("\ntry:\n")[0],"shared-management-helpers","exec"),globals())
def saves():
    s.adb("shell","run-as",package,"mkdir","-p","files/pro-franchises-v1")
    listing=s.adb("shell","run-as",package,"ls","files/pro-franchises-v1").decode();ids=re.findall(r"([0-9a-f-]{36})\.json",listing)
    return {id:json.loads(s.adb("exec-out","run-as",package,"cat","files/pro-franchises-v1/"+id+".json")) for id in ids}
def create(index):
    click("new","New franchise");root=ready("create",lambda r:"Start a franchise" in s.texts(r));root=s.scroll_to_text("pro-generated-name","My franchise",max_swipes=15,toward_start=True);s.tap_id(root,"pro-franchise-name");s.adb("shell","input","text","QA"+str(index));s.adb("shell","input","keyevent","111")
    root=s.scroll_to_text("pro-generated-roster-picker","Real players",max_swipes=15);s.tap_id(root,"pro-starting-players");click("generated-choice","New generated players · balanced league");s.capture("pro-generated-creation")
    before=saves();click("start","Start franchise");ready("overview",s.pro_overview_loaded);after=saves();id=next(id for id in after if id not in before);return after[id]
try:
    ready("library",s.library_loaded);a=create(1)
    root=s.scroll_to_text("pro-generated-exit","‹ Exit",max_swipes=20,toward_start=True);s.tap(root,"‹ Exit");ready("library-return",s.library_loaded);b=create(2)
    assert a["fictionalPlayers"] and b["fictionalPlayers"]
    assert not {p["football"]["playerId"] for p in a["players"]}&{p["football"]["playerId"] for p in b["players"]}
    assert [p["football"]["fullName"] for p in a["players"]]!=[p["football"]["fullName"] for p in b["players"]]
    assert [p["football"]["overallRating"] for p in a["players"]]!=[p["football"]["overallRating"] for p in b["players"]]
    assert a.get("openingTransactionsSnapshot") is None and b.get("openingTransactionsSnapshot") is None
    load(b);restored=saves()[b["id"]];assert restored["players"]==b["players"] and restored["contracts"]==b["contracts"]
    navigate("Roster");root=s.scroll_to_text("pro-generated-roster","Active roster · 53",max_swipes=20,toward_start=True);s.capture("pro-generated-roster")
    assert a["practiceSquadRemoved"] and b["practiceSquadRemoved"]
    assert not any(p.get("practiceSquad") for p in b["players"])
    assert b["league"]["practiceSquadLimit"]==0
    assert not any("Practice squad" in t for t in s.texts(root))
    s.tap_id(root,"pro-roster-position");click("hb-filter","HB");s.capture("pro-backfield-hb-filter")
    root=s.scroll_to_text("pro-backfield-editor","Organize depth chart",max_swipes=20,toward_start=True);s.tap(root,"Organize depth chart")
    root=ready("backfield-editor",lambda r:"Playing position" in s.texts(r) or any("depth" in t for t in s.texts(r)))
    s.tap_id(root,"pro-depth-position");click("fb-role","FB")
    root=s.scroll_to_text("pro-backfield-search","Find any active player",max_swipes=20,toward_start=True);s.tap_id(root,"pro-depth-search")
    te=next(p for p in b["players"] if p["football"]["teamName"]==b["userTeam"] and p["football"]["position"]==3)
    s.adb("shell","input","text",te["football"]["fullName"].replace(" ","%s"));s.adb("shell","input","keyevent","111");click("te-at-fb","Assign")
    changed=saves()[b["id"]]
    assert next(a for a in changed["depthChartAssignments"] if a["playerId"]==te["football"]["playerId"])["positionCode"]==2
    assert changed["players"]==b["players"] and changed["contracts"]==b["contracts"]
    s.capture("pro-backfield-te-assigned-fb")
    # Exercise the real app loader with one legacy PS flag, preserving the other contracts.
    legacy=json.loads(json.dumps(changed));cut=next(p for p in legacy["players"] if p["football"]["teamName"]==legacy["userTeam"] and p["football"]["position"]==1)
    cut["practiceSquad"]=True;cut["rosterStatus"]="PS";load(legacy)
    migrated=saves()[b["id"]];assert not any(p.get("practiceSquad") for p in migrated["players"])
    assert next(p for p in migrated["players"] if p["football"]["playerId"]==cut["football"]["playerId"])["football"]["teamName"]=="Free agent"
    unaffected=[c for c in changed["contracts"] if c["playerId"]!=cut["football"]["playerId"]]
    assert all(c in migrated["contracts"] for c in unaffected)
    navigate("Roster");s.capture("pro-backfield-migrated-no-practice")
    Path("pro-generated-roster-check.json").write_text(json.dumps({"twoRealNewFranchises":True,"freshIdentitiesNamesAndRatings":True,"fictionalMode":True,"noOpeningRealTrades":True,"reloadPreservesPlayersAndContracts":True,"activeRoster53":True,"noPracticeSquad":True,"hbFilter":True,"teAssignedAsFullback":True,"naturalPositionPreserved":True,"legacyPracticeMigration":True,"otherContractsPreserved":True},indent=2),encoding="utf-8")
finally:Path("pro-generated-roster-logcat.txt").write_bytes(s.adb("logcat","-d","-t","1000"))
