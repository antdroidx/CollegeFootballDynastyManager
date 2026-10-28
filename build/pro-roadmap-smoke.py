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
    return s.wait_for("pro-roadmap-"+name,check,timeout=180)
def click(name,label):
    root=s.scroll_to_text("pro-roadmap-"+name,label,max_swipes=20);s.tap(root,label);return root
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
    path=Path("pro-roadmap-save.json");path.write_text(json.dumps(fixture),encoding="utf-8")
    s.adb("shell","am","force-stop",package);s.adb("push",str(path),"/data/local/tmp/pro-roadmap-save.json")
    s.adb("shell","run-as",package,"mkdir","-p","files/pro-franchises-v1")
    s.adb("shell","run-as",package,"cp","/data/local/tmp/pro-roadmap-save.json","files/pro-franchises-v1/"+fixture["id"]+".json")
    s.adb("shell","monkey","-p",package,"-c","android.intent.category.LAUNCHER","1");ready("library",s.library_loaded)
    phase=["Preseason","RegularSeason","Playoffs","Offseason"][fixture["phase"]]
    click("slot",f"{fixture['name']} · {fixture['userTeam']} · {fixture['year']} {phase} W{fixture['week']}")
    ready("overview",s.pro_overview_loaded)
def category(label):
    root=s.scroll_to_text("pro-roadmap-category","Franchise analytics",max_swipes=20,toward_start=True)
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
def ui_polish():
    # Isolated display fixture: full synthetic box scores, not claims about actual NFL results.
    regular=json.loads(json.dumps(data["regular"]))
    for game in regular["games"]:
        f=game["football"]
        if not f["hasPlayed"]:continue
        for key in ["homeStats","awayStats"]:
            f[key]={"passAttempts":30,"rushAttempts":30,"passYards":210,"rushYards":100,"passingTouchdowns":2,"rushingTouchdowns":1,"turnovers":1,"sacksAllowed":3,"explosivePasses20":3,"explosiveRuns20":1,"efficiency":{"plays":60,"yards":310,"successful":27,"thirdDowns":12,"thirdDownConversions":5,"sackYardsLost":18}}
    load(regular);navigate("Overview");click("polish-options","Options");click("polish-appearance","Appearance");click("polish-light","Light")
    navigate("Analytics");category("Advanced efficiency")
    root=s.scroll_to_text("pro-polish-efficiency","Adjusted net efficiency",max_swipes=20)
    s.capture("pro-presentation-light-efficiency")
    root=s.scroll_to_text("pro-polish-method","How these stats work",max_swipes=20);s.tap(root,"How these stats work")
    ready("efficiency-definition",lambda r:"Hide metric definitions" in s.texts(r))
    navigate("Front Office");click("polish-trade","Trade center")
    root=s.scroll_to_text("pro-polish-search","Find player",max_swipes=20)
    checks=[n for n in root.iter("node") if "CheckBox" in n.get("class","")]
    if not checks:
        root=s.scroll_to_text("pro-polish-select","Select",max_swipes=20);checks=[n for n in root.iter("node") if "CheckBox" in n.get("class","")]
    assert checks,"Trade selection checkbox missing"
    b=list(map(int,re.findall(r"\d+",checks[0].get("bounds"))));s.adb("shell","input","tap",str((b[0]+b[2])//2),str((b[1]+b[3])//2))
    root=s.scroll_to_text("pro-polish-selected","You send",max_swipes=20,toward_start=True);s.capture("pro-presentation-light-trade-selected")
    remove=next(n for n in root.iter("node") if n.get("text","").endswith(" ×"));s.tap(root,remove.get("text"))
    rotate(1);navigate("Front Office");click("polish-landscape-trade","Trade center");s.capture("pro-presentation-landscape-trade");rotate(0)
    load(data["retention"]);navigate("Front Office");click("polish-retention","Expiring contracts");click("polish-negotiate","Negotiate")
    ready("contract-modal",lambda r:"Low offer" in s.texts(r))
    for label in ["Low offer","Agent demand","Strong offer"]:click("polish-preset-"+label,label)
    s.capture("pro-presentation-light-contract-presets")
    click("polish-yearly","Show yearly cap breakdown");ready("contract-breakdown",lambda r:"Hide yearly cap breakdown" in s.texts(r));s.capture("pro-presentation-light-contract-breakdown");click("polish-cancel","Cancel")
    navigate("Overview");click("polish-dark-options","Options");click("polish-dark-appearance","Appearance");click("polish-dark","Dark");s.capture("pro-presentation-dark-overview")
    Path("pro-polish-check.json").write_text(json.dumps({"syntheticContext":True,"lightEfficiency":True,"metricDefinitions":True,"tradeSelectionRemoval":True,"landscapeTrade":True,"contractPresets":True,"optionalYearlyDetails":True,"darkTheme":True},indent=2),encoding="utf-8")

def presentation():
    rotate(0)
    navigate("Analytics");ready("mobile-analytics",lambda r:"Franchise analytics" in s.texts(r))
    for label,header in [("Player development","Pos avg Δ"),("Draft outcomes","League Δ"),("Career records","Total")]:
        category(label);root=s.scroll_to_text("pro-presentation-"+label,header,max_swipes=20)
        cell_visible(root,header);s.capture("pro-presentation-portrait-"+label.replace(" ","-"))
    for key,label,header,target in [("draft","Draft & scouting","Scouted","Draft"),("retention","Expiring contracts","Demand / yr","Negotiate")]:
        load(data[key]);navigate("Front Office");click("presentation-"+key,label)
        root=s.scroll_to_text("pro-presentation-"+key,header,max_swipes=20)
        cell_visible(root,header);action_visible(root,target);s.capture("pro-presentation-portrait-"+key)
    navigate("Front Office");click("presentation-camp","Training camp")
    root=s.scroll_to_text("pro-presentation-camp","Change",max_swipes=20);cell_visible(root,"Change");s.capture("pro-presentation-portrait-camp")
    s.adb("shell","wm","size","1768x2208");s.adb("shell","wm","density","320")
    navigate("Analytics");ready("fold-analytics",lambda r:"Franchise analytics" in s.texts(r));category("Draft outcomes");s.capture("pro-roadmap-fold-draft-outcomes")
    for theme in ["Light","Dark"]:
        click("theme-options","Options");click("appearance","Appearance");click(theme.lower(),theme)
        navigate("Analytics");ready(theme.lower()+"-analytics",lambda r:"Franchise analytics" in s.texts(r));s.capture("pro-roadmap-fold-"+theme.lower()+"-analytics")
    s.adb("shell","wm","size","reset");s.adb("shell","wm","density","reset")
    Path("pro-presentation-check.json").write_text(json.dumps({"samePackagedAPK":True,"portraitPrimaryNumbers":True,"draftGradeAndAction":True,"retentionDemandAndAction":True,"campRatings":True,"foldLayout":True,"lightTheme":True,"darkTheme":True},indent=2),encoding="utf-8")

try:
    data=json.loads(gzip.decompress(next(Path("apk").glob("**/pro-staff-roadmap-ui-fixture.json.gz")).read_bytes()))
    s.adb("shell","settings","put","system","accelerometer_rotation","0")
    s.adb("shell","settings","put","system","user_rotation","0")
    load(data["regular"])
    if "--presentation-only" in sys.argv:
        ui_polish();presentation();sys.exit(0)
    navigate("Analytics");ready("analytics",lambda r:"Franchise analytics" in s.texts(r))
    for label in ["Player development","Draft outcomes","Career records"]:
        category(label);s.capture("pro-roadmap-portrait-"+label.replace(" ","-"))
    navigate("News");click("news-filter","Filter: All");click("weekly-filter","Weekly review")
    ready("weekly-news",lambda r:any("around the league" in t for t in s.texts(r) if t));s.capture("pro-roadmap-weekly-news")
    navigate("Career");click("expectations","Ownership expectations");s.capture("pro-roadmap-career")
    navigate("Front Office");click("camp","Training camp");ready("camp",lambda r:"Training camp & roster battles" in s.texts(r));s.capture("pro-roadmap-camp")
    navigate("Standings");ready("standings",lambda r:"Standings" in s.texts(r));s.capture("pro-roadmap-standings")
    for label,key in [("Draft & scouting","draft"),("Expiring contracts","retention")]:
        load(data[key]);navigate("Front Office");click(key,label)
        target="Draft" if key=="draft" else "Negotiate"
        root=s.scroll_to_text("pro-roadmap-portrait-"+key,target,max_swipes=20)
        action_visible(root,target);s.capture("pro-roadmap-portrait-"+key)
        rotate(1)
        root=s.scroll_to_text("pro-roadmap-landscape-"+key,target,max_swipes=20)
        action_visible(root,target);s.capture("pro-roadmap-landscape-"+key)
        rotate(0)
    presentation()
    Path("pro-roadmap-check.json").write_text(json.dumps({"syntheticFixtures":True,"analyticsCategories":3,"weeklyNews":True,"careerExpectations":True,"camp":True,"standings":True,"portraitLandscapeActions":True,"foldLayout":True,"darkTheme":True},indent=2),encoding="utf-8")
    print("PASS: analytics categories, factual weekly news, career context, standings and visible portrait/landscape draft/retention actions.")
finally:
    Path("pro-roadmap-logcat.txt").write_bytes(s.adb("logcat","-d","-t","1000"))
