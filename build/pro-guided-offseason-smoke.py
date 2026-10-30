"""Guided management flow: no live GameCast or long-season replay."""
from pathlib import Path
exec(compile(Path(__file__).with_name("pro-staff-calendar-smoke.py").read_text(encoding="utf-8").split("\ntry:\n")[0],"guided-base","exec"),globals())
old_overview=s.pro_overview_loaded
s.pro_overview_loaded=lambda r:old_overview(r) or any((n.get("resource-id") or "").endswith("pro-offseason-guide") for n in r.iter("node"))
def guided(label):return ready("stage-"+label,lambda r:any(label in (t or "") for t in s.texts(r)) and any((n.get("resource-id") or "").endswith("pro-offseason-guide") for n in r.iter("node")))
def theme(value):
    root=s.scroll_to_text("guided-settings","Settings",max_swipes=20,toward_start=True);s.tap(root,"Settings");click("appearance","Appearance");click("theme-"+value,value)
def end_table(name):
    root=s.capture(name)
    nodes=[n for n in root.iter("node") if "HorizontalScrollView" in n.get("class","") and n.get("scrollable")=="true"]
    assert nodes,"No horizontally scrollable table"
    node=max(nodes,key=lambda n:tuple(map(int,re.findall(r"\d+",n.get("bounds"))))[3])
    l,t,r,b=map(int,re.findall(r"\d+",node.get("bounds")));s.adb("shell","input","swipe",str(l+(r-l)*4//5),str((t+b)//2),str(l+(r-l)//5),str((t+b)//2),"400")
    return s.capture(name+"-right")
def continue_stage(label):
    root=s.scroll_to_text("guided-next-"+label,ProContinue[label],max_swipes=25);s.tap_id(root,"pro-offseason-next");click("confirm-"+label,"Advance")
ProContinue={"retention":"Finish retention & open free agency","draft":"Continue to training camp"}
try:
    data=json.loads(gzip.decompress(next(Path("apk").glob("**/pro-staff-guided-ui-fixture.json.gz")).read_bytes()))
    load(data["roster"]);rotate(1);root=ready("compact-overview",old_overview);assert "‹ Exit" not in s.texts(root);s.capture("pro-guided-overview")
    # With the position order fixed, an FB appears in the first roster page without selecting the FB filter.
    navigate("Roster");click("russell","Brady Russell");root=ready("russell-fb",lambda r:any("FB" in (t or "") and "Seattle Seahawks" in (t or "") for t in s.texts(r)));s.capture("pro-guided-fullback-profile");click("back-profile","‹ Back")
    navigate("Overview");s.exit_via_settings("pro")
    load(data["retention"]);guided("Contract retention");root=s.scroll_to_text("guided-veteran",data["extensionPlayerName"],max_swipes=20);s.tap_id(root,"pro-retention-negotiate-"+data["extensionPlayerId"])
    root=ready("negotiation",lambda r:"Contract negotiation" in s.texts(r));root=s.scroll_to_text("guided-cap-preview","Room after",max_swipes=25);s.capture("pro-guided-contract-cap-years");assert "Team cap" in s.texts(root)
    click("cancel-contract","Cancel");continue_stage("retention");guided("Free agency")
    root=s.scroll_to_text("guided-assistance","GM assistance · propose offers on advancement",max_swipes=20,toward_start=True);s.tap_id(root,"pro-auto-free-agency")
    root=s.scroll_to_text("guided-fa-table","Market / yr",max_swipes=20);end_table("pro-guided-free-agency-dark")
    theme("Light");guided("Free agency");s.scroll_to_text("guided-light-table","Market / yr",max_swipes=20);end_table("pro-guided-free-agency-light")
    theme("Dark");s.exit_via_settings("pro")
    load(data["draft"]);root=guided("Draft room");assert "Start draft / advance to your pick" not in s.texts(root);s.capture("pro-guided-draft-room")
    click("draft-results","Draft results");root=guided("Draft room");s.capture("pro-guided-draft-results")
    click("draft-order","Order");root=guided("Draft room");s.capture("pro-guided-draft-order")
    click("draft-prospects","Prospects");root=s.capture("draft-first-player")
    # Read the actual first prospect link from the displayed table.
    names={p["football"]["fullName"] for p in data["draft"]["prospects"]}
    for _ in range(12):
        visible=[t for t in s.texts(root) if t in names]
        if visible:break
        scrolls=[n for n in root.iter("node") if n.get("scrollable")=="true" and "Horizontal" not in n.get("class","")]
        node=max(scrolls,key=lambda n:tuple(map(int,re.findall(r"\d+",n.get("bounds"))))[0]);l,t,r,b=map(int,re.findall(r"\d+",node.get("bounds")));s.adb("shell","input","swipe",str((l+r)//2),str(t+(b-t)*3//4),str((l+r)//2),str(t+(b-t)//4),"350");root=s.capture("draft-prospect-scroll")
    assert visible;s.tap(root,visible[0]);ready("prospect-profile",lambda r:"Prospect profile" in s.texts(r));s.capture("pro-guided-prospect-profile");click("back-prospect","‹ Back")
    click("auto-draft","Delegate remaining picks");guided("Draft room");continue_stage("draft");guided("Training camp");s.capture("pro-guided-training-camp")
    root=s.scroll_to_text("camp-player-table","Growth",max_swipes=20);assert "Player" in s.texts(root);s.capture("pro-guided-camp-dense-table")
    Path("pro-guided-offseason-check.json").write_text(json.dumps({"compactOverview":True,"fullbackIdentityAndDefaultRoster":True,"settingsExit":True,"retentionAutoOpened":True,"contractYearlyTeamRoom":True,"freeAgencyAutoOpened":True,"assistanceToggle":True,"offerContrastBothThemes":True,"draftAutoStarts":True,"draftResultsAndOrder":True,"prospectProfile":True,"trainingCampAutoOpened":True,"denseCamp":True},indent=2),encoding="utf-8")
finally:Path("pro-guided-offseason-logcat.txt").write_bytes(s.adb("logcat","-d","-t","1200"))
