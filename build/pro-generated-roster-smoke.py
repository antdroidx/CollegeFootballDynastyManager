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
    Path("pro-generated-roster-check.json").write_text(json.dumps({"twoRealNewFranchises":True,"freshIdentitiesNamesAndRatings":True,"fictionalMode":True,"noOpeningRealTrades":True,"reloadPreservesPlayersAndContracts":True,"activeRoster53":True},indent=2),encoding="utf-8")
finally:Path("pro-generated-roster-logcat.txt").write_bytes(s.adb("logcat","-d","-t","1000"))
