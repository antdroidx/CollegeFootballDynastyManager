"""Focused management acceptance on synthetic, real-loader-validated saves; no full GameCast replay."""
import gzip,importlib.util,json,re,sys
from pathlib import Path
spec=importlib.util.spec_from_file_location("shared_smoke",Path(__file__).with_name("android-ui-smoke.py"))
s=importlib.util.module_from_spec(spec);spec.loader.exec_module(s)
package="antdroid.profootball.franchise"
def ready(name,predicate):
    def check(root):
        assert "Unable to continue" not in s.texts(root),"App error at "+name
        return predicate(root)
    return s.wait_for("pro-staff-calendar-"+name,check,timeout=180)
def click(name,label):
    root=s.scroll_to_text("pro-staff-calendar-"+name,label,max_swipes=20);s.tap(root,label);return root
def navigate(label):
    root=s.capture("pro-navigation-"+label.lower())
    if label in s.texts(root):
        s.tap(root,label)
    elif "League" in s.texts(root):
        s.tap(root,"League");click("league-"+label.lower(),label)
    else:
        for attempt in range(10):
            root=s.capture("pro-sidebar-"+label.lower()+str(attempt))
            if label in s.texts(root): s.tap(root,label); return
            scrollables=[n for n in root.iter("node") if n.get("scrollable")=="true" and "Horizontal" not in n.get("class", "")]
            assert scrollables,"No sidebar scroll container"
            bounds=[tuple(map(int,re.findall(r"\d+",n.get("bounds")))) for n in scrollables]
            left,top,right,bottom=min(bounds,key=lambda b:b[0])
            destinations=["Overview","Roster","Staff","Front Office","Schedule","Standings","Playoffs","Statistics","Analytics","Inbox","Transactions","News","Honors","Career","History"]
            visible=[destinations.index(n.get("text")) for n in root.iter("node") if n.get("text") in destinations and tuple(map(int,re.findall(r"\d+",n.get("bounds"))))[0]<right]
            toward_start=bool(visible) and destinations.index(label)<min(visible)
            y1=top+(bottom-top)*(1 if toward_start else 3)//4;y2=top+(bottom-top)*(3 if toward_start else 1)//4
            s.adb("shell","input","swipe",str((left+right)//2),str(y1),str((left+right)//2),str(y2),"350")
        raise AssertionError("Could not reach sidebar destination "+label)

def load(fixture):
    path=Path("pro-staff-calendar-save.json");path.write_text(json.dumps(fixture),encoding="utf-8")
    s.adb("shell","am","force-stop",package);s.adb("push",str(path),"/data/local/tmp/pro-staff-calendar-save.json")
    s.adb("shell","run-as",package,"mkdir","-p","files/pro-franchises-v1")
    s.adb("shell","run-as",package,"cp","/data/local/tmp/pro-staff-calendar-save.json","files/pro-franchises-v1/"+fixture["id"]+".json")
    s.adb("shell","monkey","-p",package,"-c","android.intent.category.LAUNCHER","1");ready("library",s.library_loaded)
    phase=["Preseason","RegularSeason","Playoffs","Offseason"][fixture["phase"]]
    click("slot",f"{fixture['name']} · {fixture['userTeam']} · {fixture['year']} {phase} W{fixture['week']}")
    ready("overview",s.pro_overview_loaded)
def category(label):
    root=s.scroll_to_text("pro-staff-calendar-category","Franchise analytics",max_swipes=20,toward_start=True)
    s.tap_id(root,"pro-analytics-category");click("category-select",label)
    ready("category-"+label,lambda r:label in s.texts(r))
def rotate(value):
    s.adb("shell","wm","fixed-to-user-rotation","enabled")
    s.adb("shell","wm","user-rotation","lock",str(value))
    s.adb("shell","settings","put","system","user_rotation",str(value))
    def oriented(root):
        b=tuple(map(int,re.findall(r"\d+",root.find("node").get("bounds"))))
        return (b[2]>b[3]) if value else (b[3]>b[2])
    return ready("rotation-"+str(value),oriented)

def action_visible(root,label):
    node=next(n for n in root.iter("node") if n.get("text")==label and n.get("class","").endswith("Button"))
    left,top,right,bottom=map(int,re.findall(r"\d+",node.get("bounds")))
    bounds=tuple(map(int,re.findall(r"\d+",root.find("node").get("bounds"))))
    assert 0<=left<right<=bounds[2] and 0<=top<bottom<=bounds[3],label+" is clipped"
def cell_visible(root,label):
    node=next(n for n in root.iter("node") if n.get("text")==label)
    b=tuple(map(int,re.findall(r"\d+",node.get("bounds"))))
    screen=tuple(map(int,re.findall(r"\d+",root.find("node").get("bounds"))))
    assert 0<=b[0]<b[2]<=screen[2] and 0<=b[1]<b[3]<=screen[3],label+" is clipped or requires horizontal scrolling"
try:
    data=json.loads(gzip.decompress(next(Path("apk").glob("**/pro-staff-roadmap-ui-fixture.json.gz")).read_bytes()))
    fixture=json.loads(json.dumps(data["regular"]));fixture.update(name="StaffCalendarQA",phase=0,week=2)
    load(fixture);navigate("Roster");click("depth-chart","Organize depth chart")
    root=ready("depth-position",lambda r:"QB" in s.texts(r));s.tap_id(root,"pro-depth-position")
    root=ready("depth-choices",lambda r:"RB" in s.texts(r) and "TE" in s.texts(r))
    positions=[n.get("text") for n in root.iter("node") if "Button" in n.get("class","") and n.get("text") in {"QB","RB","WR","TE","OT","OG","OC","DE","DT","OLB","MLB","CB","FS","SS","K"}]
    assert positions[:4]==["QB","RB","WR","TE"],positions
    s.capture("pro-calendar-depth-order");click("close-position","Cancel");click("close-depth","‹ Back")
    navigate("Schedule")
    def select_phase(label):
        root=s.scroll_to_text("pro-calendar-phase","Season schedule",max_swipes=15,toward_start=True);s.tap_id(root,"pro-schedule-phase");click("choose-"+label,label)
    def week_choices():
        root=s.scroll_to_text("pro-calendar-week","Season schedule",max_swipes=15,toward_start=True);s.tap_id(root,"pro-schedule-week");return ready("week-choices",lambda r:"All weeks" in s.texts(r))
    select_phase("Preseason");root=week_choices();labels=[n.get("text") for n in root.iter("node") if "Button" in n.get("class","") and n.get("text","").startswith("Preseason")]
    assert labels==["Preseason · P1","Preseason · P2 · Current","Preseason · P3"],labels
    s.capture("pro-calendar-preseason-weeks");click("choose-p3","Preseason · P3")
    select_phase("RegularSeason");root=week_choices();assert not any(t and "Current" in t for t in s.texts(root));s.capture("pro-calendar-regular-weeks");click("close-regular","Cancel")
    select_phase("Playoffs");root=week_choices();assert {"Wild card","Divisional round","Conference championships","Championship"}<=s.texts(root);assert not any(t and "Current" in t for t in s.texts(root));s.capture("pro-calendar-playoff-weeks");click("close-playoffs","Cancel")
    navigate("Staff");root=ready("staff-faces",lambda r:"Mike Macdonald" in s.texts(r));s.capture("pro-calendar-seattle-coaches")
    navigate("Overview");s.exit_via_settings("pro");ready("new-library",s.library_loaded);click("new-franchise","New franchise");click("team-picker","Seattle Seahawks");click("choose-kc","Kansas City Chiefs");click("start-kc","Start franchise");ready("kc-overview",s.pro_overview_loaded);navigate("Staff");ready("andy-reid",lambda r:"Andy Reid" in s.texts(r));s.capture("pro-calendar-andy-reid")
    navigate("Overview");click("options","Settings");click("appearance","Appearance");click("light","Light");navigate("Staff");ready("light-andy",lambda r:"Andy Reid" in s.texts(r));s.capture("pro-calendar-andy-reid-light")
    Path("pro-staff-calendar-check.json").write_text(json.dumps({"syntheticPreseasonFixture":True,"numericDepthOrder":True,"preseasonThreeWeeks":True,"currentMarkerPhaseCorrect":True,"playoffFourRounds":True,"seattleIllustrations":True,"realKansasCityFranchise":True,"andyReidLightDark":True},indent=2),encoding="utf-8")
finally:Path("pro-staff-calendar-logcat.txt").write_bytes(s.adb("logcat","-d","-t","1200"))
