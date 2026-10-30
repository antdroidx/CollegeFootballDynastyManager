"""Scoped retention contrast/negotiation and all three fast-forward phase boundaries."""
from pathlib import Path
exec(compile(Path(__file__).with_name("pro-staff-calendar-smoke.py").read_text(encoding="utf-8").split("\ntry:\n")[0],"shared-management-helpers","exec"),globals())
def read(f):return json.loads(s.adb("exec-out","run-as",package,"cat","files/pro-franchises-v1/"+f["id"]+".json"))
def theme(value):
    navigate("Overview");root=s.scroll_to_text("pro-phase-options","Settings",max_swipes=20,toward_start=True);s.tap(root,"Settings");click("appearance","Appearance");click("theme",value)
try:
    fixture=json.loads(gzip.decompress(next(Path("apk").glob("**/pro-staff-inseason-ui-fixture.json.gz")).read_bytes()));load(fixture)
    theme("Light");navigate("Front Office");click("expiring","Expiring contracts")
    root=ready("retention-layout",lambda r:"Expiring contracts & retention" in s.texts(r));assert not any("Retention decisions open when" in t for t in s.texts(root))
    root=s.scroll_to_text("pro-inseason-actions","Negotiate",max_swipes=15);buttons=[n for n in root.iter("node") if n.get("text")=="Negotiate" and n.get("enabled")=="true"];assert buttons
    s.capture("pro-inseason-light-actions");s.tap(root,"Negotiate")
    root=ready("inseason-negotiation",lambda r:"Contract negotiation" in s.texts(r));s.capture("pro-inseason-negotiation");click("cancel","Cancel")
    theme("Dark");navigate("Front Office");click("expiring-dark","Expiring contracts");s.scroll_to_text("pro-inseason-dark-actions","Negotiate",max_swipes=15);s.capture("pro-inseason-dark-actions")
    for phase,label,target in [("Preseason","Simulate preseason",1),("RegularSeason","Simulate regular season",2),("Playoffs","Simulate playoffs",3)]:
        f=json.loads(gzip.decompress(next(Path("apk").glob("**/pro-staff-phase-"+phase+"-ui-fixture.json.gz")).read_bytes()));load(f)
        root=s.scroll_to_text("pro-phase-start-"+phase,label,max_swipes=20,toward_start=True);s.tap(root,label);click("confirm-phase","Simulate")
        root=ready("phase-boundary-"+phase,lambda r:"Advance week" in s.texts(r) or "Advance offseason" in s.texts(r));result=read(f);assert result["phase"]==target and result["year"]==f["year"],(phase,result["phase"]);s.capture("pro-phase-boundary-"+phase)
    Path("pro-inseason-phase-check.json").write_text(json.dumps({"syntheticFixtures":True,"inSeasonNegotiationOpens":True,"lightDarkContractActions":True,"preseasonStopsAtRegularSeason":True,"regularStopsAtPlayoffs":True,"playoffsStopAtOffseason":True,"savedPhaseBoundaries":True},indent=2),encoding="utf-8")
finally:Path("pro-inseason-phase-logcat.txt").write_bytes(s.adb("logcat","-d","-t","1000"))
