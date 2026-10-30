"""Dense retention rows with visible actions on both alternating stripe colors."""
from pathlib import Path
exec(compile(Path(__file__).with_name("pro-staff-calendar-smoke.py").read_text(encoding="utf-8").split("\ntry:\n")[0],"shared-management-helpers","exec"),globals())
def theme(value):
    navigate("Overview");root=s.scroll_to_text("pro-retention-theme","Settings",max_swipes=20,toward_start=True);s.tap(root,"Settings");click("appearance","Appearance");click("theme",value)
try:
    fixture=json.loads(gzip.decompress(next(Path("apk").glob("**/pro-staff-inseason-ui-fixture.json.gz")).read_bytes()));load(fixture)
    for mode in ["Light","Dark"]:
        theme(mode);navigate("Front Office");click("retention","Expiring contracts")
        root=s.scroll_to_text("pro-retention-table-"+mode.lower(),"Negotiate",max_swipes=15)
        labels=s.texts(root);assert "Player" in labels and "Actions" in labels
        buttons=[n for n in root.iter("node") if n.get("text")=="Negotiate" and n.get("enabled")=="true"]
        assert len(buttons)>=3,"Dense table should show at least three actionable rows"
        heights=[int(re.findall(r"\d+",n.get("bounds"))[3])-int(re.findall(r"\d+",n.get("bounds"))[1]) for n in buttons];assert max(heights)<180,heights
        assert len([n for n in root.iter("node") if n.get("text")=="Let expire"])>=3
        s.capture("pro-retention-table-"+mode.lower());s.tap(root,"Negotiate");ready("negotiation",lambda r:"Contract negotiation" in s.texts(r));click("cancel","Cancel")
    Path("pro-retention-table-check.json").write_text(json.dumps({"syntheticFixture":True,"denseTable":True,"multipleRowsVisible":True,"compactActionHeights":True,"lightDarkStripes":True,"negotiationAccessible":True},indent=2),encoding="utf-8")
finally:Path("pro-retention-table-logcat.txt").write_bytes(s.adb("logcat","-d","-t","1000"))
