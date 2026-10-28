"""Focused real-new-franchise roster/compensation plus explicitly synthetic 2027 award acceptance."""
from pathlib import Path
exec(compile(Path(__file__).with_name("pro-staff-calendar-smoke.py").read_text(encoding="utf-8").split("\ntry:\n")[0],"shared-management-helpers","exec"),globals())

def saved_slot(team):
    names=s.adb("shell","run-as",package,"ls","files/pro-franchises-v1").decode().splitlines()
    found=[]
    for name in names:
        if not re.fullmatch(r"[0-9a-fA-F-]+\.json",name):continue
        data=json.loads(s.adb("exec-out","run-as",package,"cat","files/pro-franchises-v1/"+name))
        if data.get("userTeam")==team and data.get("openingTransactionsSnapshot")=="2026-10-28":found.append(data)
    assert found,"New roster snapshot not saved"
    return found[-1]

try:
    ready("fresh-library",s.library_loaded);click("new","New franchise");click("start","Start franchise");ready("real-seattle",s.pro_overview_loaded)
    saved=saved_slot("Seattle Seahawks")
    for name,club in [("Quenton Nelson","Seattle Seahawks"),("DeForest Buckner","San Francisco 49ers")]:
        player=next(p for p in saved["players"] if p["football"]["fullName"]==name);assert player["football"]["teamName"]==club
        contracts=[c for c in saved["contracts"] if c["active"] and c["playerId"]==player["football"]["playerId"]];assert len(contracts)==1 and contracts[0]["team"]==club
    for year,round_,original in [(2027,3,"Seattle Seahawks"),(2028,5,"Seattle Seahawks"),(2027,4,"San Francisco 49ers")]:
        pick=next(p for p in saved["picks"] if p["year"]==year and p["round"]==round_ and p["originalTeam"]==original);assert pick["owner"]=="Indianapolis Colts"
    navigate("Roster");root=s.scroll_to_text("nelson-search","Find a player",max_swipes=12,toward_start=True);s.tap_id(root,"pro-roster-search");s.adb("shell","input","text","Quenton");s.adb("shell","input","keyevent","4");ready("nelson-row",lambda r:"Quenton Nelson" in s.texts(r));s.capture("pro-roster-update-nelson")
    navigate("Front Office");click("picks","Draft picks");ready("projections",lambda r:"Projected compensatory picks" in s.texts(r));s.scroll_to_text("teasley-projection","Nolan Teasley",max_swipes=15);root=s.capture("pro-roster-update-projections");assert "Projected picks cannot be traded." in " ".join(s.texts(root))
    fixture=json.loads(gzip.decompress(next(Path("apk").glob("**/pro-staff-compensatory-ui-fixture.json.gz")).read_bytes()));load(fixture)
    navigate("Front Office");click("awarded-picks","Draft picks");root=ready("pick-year",lambda r:"Draft picks" in s.texts(r));s.tap_id(root,"pro-picks-year");click("2027-year","2027")
    root=s.scroll_to_text("awards-summary","5 compensatory picks awarded. These selections are included in the draft and can be traded.",max_swipes=15);assert "5 compensatory picks awarded" in " ".join(s.texts(root));s.capture("pro-roster-update-awards")
    navigate("Overview");click("save-exit","‹ Exit");ready("reload-library",s.library_loaded)
    Path("pro-roster-update-check.json").write_text(json.dumps({"actualNewSeattleFranchise":True,"nelsonAndBucknerOwnership":True,"buyerContractsAndSellerPickCompensation":True,"nelsonVisibleInRoster":True,"projectionsNotTradable":True,"synthetic2027AwardsVisible":True,"saveExit":True},indent=2),encoding="utf-8")
finally:Path("pro-roster-update-logcat.txt").write_bytes(s.adb("logcat","-d","-t","1200"))
