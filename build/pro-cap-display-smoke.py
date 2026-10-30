"""Focused single-season cap presentation on an explicitly synthetic real-loader-tested save."""
from pathlib import Path
exec(compile(Path(__file__).with_name("pro-staff-calendar-smoke.py").read_text(encoding="utf-8").split("\ntry:\n")[0],"shared-management-helpers","exec"),globals())

def cap(name,expected):
    try:root=s.scroll_to_text("pro-cap-"+name,expected,max_swipes=15)
    except AssertionError:root=s.scroll_to_text("pro-cap-"+name,expected,max_swipes=15,toward_start=True)
    labels=" ".join(s.texts(root));assert "Season-start room" not in labels and "Offseason salary excluded" not in labels and "$1.00m" not in labels
    s.capture("pro-cap-"+name);return root

def appearance(choice):
    navigate("Overview");root=s.scroll_to_text("cap-options","Settings",max_swipes=20,toward_start=True);s.tap(root,"Settings");click("appearance","Appearance");click("theme",choice)

try:
    fixture=json.loads(gzip.decompress(next(Path("apk").glob("**/pro-staff-cap-display-ui-fixture.json.gz")).read_bytes()));load(fixture)
    navigate("Front Office");cap("deficit-dark","$0.50m over season cap")
    click("details","Show cap breakdown");root=s.scroll_to_text("pro-cap-exact","Season cap space $-500,000 / Pending bids $0",max_swipes=15);assert "Season cap space $-500,000" in " ".join(s.texts(root));s.capture("pro-cap-exact-breakdown")
    click("financial-hub","Financial overview");cap("financial-hub","$0.50m over season cap")
    navigate("Overview");cap("overview","$0.50m over season cap");root=s.scroll_to_text("pro-cap-open-inbox","Open inbox",max_swipes=20,toward_start=True);s.tap(root,"Open inbox")
    root=ready("cap-inbox",lambda r:any("Over season salary cap" in (t or "") for t in s.texts(r)));assert any("Clear $500,000" in (t or "") for t in s.texts(root));assert not any("Opening-season cap" in (t or "") for t in s.texts(root));s.capture("pro-cap-inbox")
    appearance("Light");navigate("Front Office");cap("deficit-light","$0.50m over season cap")
    navigate("Overview");s.exit_via_settings("pro");ready("saved-library",s.library_loaded)
    saved=json.loads(s.adb("exec-out","run-as",package,"cat","files/pro-franchises-v1/"+fixture["id"]+".json"));assert saved["contracts"]==fixture["contracts"] and saved["capRulesVersion"]==2
    positive=dict(saved);positive["salaryCap"]=80_000_000;positive["name"]="Synthetic positive cap QA";load(positive)
    navigate("Front Office");cap("positive","$2.00m season cap space")
    navigate("Overview");root=s.scroll_to_text("pro-cap-positive-inbox","Open inbox",max_swipes=20,toward_start=True);s.tap(root,"Open inbox");root=ready("positive-resolved",lambda r:"Franchise inbox" in s.texts(r));assert not any("Over season salary cap" in (t or "") for t in s.texts(root));s.capture("pro-cap-resolved-inbox")
    Path("pro-cap-display-check.json").write_text(json.dumps({"syntheticFixture":True,"frontOfficeAndHubMatchInbox":True,"overviewSeasonBalance":True,"noTemporaryBalance":True,"lightDark":True,"contractsAndCountingUnchanged":True,"positiveBalanceResolvesAlert":True},indent=2),encoding="utf-8")
finally:Path("pro-cap-display-logcat.txt").write_bytes(s.adb("logcat","-d","-t","1000"))
