"""Two independently built apps; synthetic class snapshot, real College export and Android document picker."""
import gzip,importlib.util,json,re,sqlite3
from pathlib import Path
spec=importlib.util.spec_from_file_location("shared",Path(__file__).with_name("android-ui-smoke.py"));s=importlib.util.module_from_spec(spec);spec.loader.exec_module(s)
college="antdroid.cfbcoach.dev";pro="antdroid.profootball.franchise"
def ready(name,predicate):
    def check(root):
        assert not {"Unable to continue","Could not load dynasty"}&s.texts(root),"App error at "+name
        return predicate(root)
    return s.wait_for("exchange-"+name,check,timeout=240)
def click(name,label,toward_start=False):
    root=s.scroll_to_text("exchange-"+name,label,max_swipes=25,toward_start=toward_start);s.tap(root,label);return root
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

def pick_document(filename):
    root=ready("file-picker",lambda r:any("documentsui" in n.get("package","") for n in r.iter("node")))
    if filename not in s.texts(root):
        drawer=next((n for n in root.iter("node") if n.get("content-desc") in {"Show roots","Open navigation drawer"}),None)
        if drawer is not None:
            b=list(map(int,re.findall(r"\d+",drawer.get("bounds"))));s.adb("shell","input","tap",str((b[0]+b[2])//2),str((b[1]+b[3])//2))
        click("downloads","Downloads")
    click("document",filename)
try:
    s.adb("shell","settings","put","system","accelerometer_rotation","0");s.adb("shell","wm","user-rotation","lock","0")
    s.adb("shell","monkey","-p",college,"-c","android.intent.category.LAUNCHER","1")
    ready("college-library",lambda r:"Your dynasties" in s.texts(r) and "New dynasty" in s.texts(r))
    click("new","New dynasty");click("start-college","Start dynasty")
    ready("college-overview",lambda r:"Overview" in s.texts(r) and "Season" in s.texts(r))
    s.adb("shell","am","force-stop",college)
    db=Path("exchange-college.db3");db.write_bytes(s.adb("exec-out","run-as",college,"cat","files/dynasties.db3"))
    conn=sqlite3.connect(db);row=conn.execute("select save_id,snapshot_json from dynasty_saves order by updated_utc desc limit 1").fetchone();assert row
    snapshot=json.loads(row[1]);players=[p for p in snapshot["activeRoster"] if p["classYear"]>=4 and not p["isRedshirted"]][:5];assert len(players)==5
    draft_year=snapshot["seasonYear"]+1
    file={"formatVersion":1,"source":"College Football Dynasty Manager","year":draft_year,"players":players}
    # A deliberate isolated snapshot represents the newly captured completed class; engine departure rules have separate regressions.
    snapshot["latestProDraftClass"]=file
    conn.execute("update dynasty_saves set snapshot_json=? where save_id=?",(json.dumps(snapshot),row[0]));conn.commit();conn.close()
    s.adb("push",str(db),"/data/local/tmp/exchange-college.db3");s.adb("shell","run-as",college,"cp","/data/local/tmp/exchange-college.db3","files/dynasties.db3")
    s.adb("shell","monkey","-p",college,"-c","android.intent.category.LAUNCHER","1");ready("college-reload",lambda r:"Load" in s.texts(r));click("load-college","Load")
    ready("college-loaded",lambda r:"Season" in s.texts(r));click("season","Season");click("export","Export College draft class")
    ready("export-preview",lambda r:"Prepare export file" in s.texts(r));s.capture("exchange-college-preview");click("prepare-export","Prepare export file")
    filename=f"college-class-{draft_year}.json";ready("prepared",lambda r:"Ready: "+filename in s.texts(r))
    content=s.adb("exec-out","run-as",college,"cat","cache/draft-classes/"+filename);exported=json.loads(content);assert exported==file
    click("save-json","Save JSON file")
    save_root=ready("native-save-picker",lambda r:any("documentsui" in n.get("package","") for n in r.iter("node")))
    drawer=next((n for n in save_root.iter("node") if n.get("content-desc") in {"Show roots","Open navigation drawer"}),None)
    if drawer is not None:
        b=list(map(int,re.findall(r"\d+",drawer.get("bounds"))));s.adb("shell","input","tap",str((b[0]+b[2])//2),str((b[1]+b[3])//2));click("save-downloads","Downloads")
    save_root=s.capture("exchange-native-save-location");save_label=next(t for t in s.texts(save_root) if t in {"Save","SAVE"});s.tap(save_root,save_label)
    ready("native-saved",lambda r:"Saved using Android file picker." in s.texts(r));s.capture("exchange-college-saved")
    assert json.loads(s.adb("exec-out","cat","/sdcard/Download/"+filename))==exported
    click("share","Share / save draft class");ready("share-sheet",lambda r:r.find("node").get("package")!=college);s.capture("exchange-share-sheet");s.adb("shell","input","keyevent","4")
    path=Path(filename);path.write_bytes(content)
    fixture=json.loads(gzip.decompress(next(Path("pro-apk").glob("**/pro-staff-exchange-ui-fixture.json.gz")).read_bytes()));assert fixture["year"]==draft_year
    fp=Path("exchange-pro.json");fp.write_text(json.dumps(fixture),encoding="utf-8")
    s.adb("shell","monkey","-p",pro,"-c","android.intent.category.LAUNCHER","1");ready("pro-initial",s.library_loaded);s.adb("shell","am","force-stop",pro)
    s.adb("push",str(fp),"/data/local/tmp/exchange-pro.json");s.adb("shell","run-as",pro,"mkdir","-p","files/pro-franchises-v1");s.adb("shell","run-as",pro,"cp","/data/local/tmp/exchange-pro.json","files/pro-franchises-v1/"+fixture["id"]+".json")
    def load_pro():
        s.adb("shell","monkey","-p",pro,"-c","android.intent.category.LAUNCHER","1");ready("pro-library",s.library_loaded)
        click("pro-slot",f"{fixture['name']} · {fixture['userTeam']} · {draft_year} Offseason W1");ready("pro-overview",s.pro_overview_loaded)
    load_pro();navigate("Front Office");click("import","Import College draft class");pick_document(filename)
    root=ready("pro-preview",lambda r:"Import class for "+str(draft_year) in s.texts(r));s.capture("exchange-pro-preview")
    click("apply","Import class for "+str(draft_year));ready("class-loaded",lambda r:any("5 imported + 315 generated" in t for t in s.texts(r) if t));s.capture("exchange-pro-imported")
    def saved():return json.loads(s.adb("exec-out","run-as",pro,"cat","files/pro-franchises-v1/"+fixture["id"]+".json"))
    checked=saved();ids={p["playerId"] for p in players};imported=[p for p in checked["prospects"] if p["football"]["playerId"] in ids];assert len(imported)==5 and len(checked["prospects"])==320
    for field in ["players","contracts","picks","deadCap","career"]:assert checked[field]==fixture[field],"Import altered "+field
    for p in imported:
        original=next(x for x in players if x["playerId"]==p["football"]["playerId"]);assert p["football"]["fullName"]==original["fullName"] and p["football"]["appearance"]==original["appearance"]
    s.adb("shell","am","force-stop",pro);load_pro();assert saved()["importedDraftClass"]==checked["importedDraftClass"]
    # A future-format file must fail without touching the accepted saved class.
    invalid=dict(exported,formatVersion=999);bad=Path("unsupported-class.json");bad.write_text(json.dumps(invalid),encoding="utf-8");s.adb("push",str(bad),"/sdcard/Download/unsupported-class.json")
    navigate("Front Office");click("invalid-import","Import College draft class");pick_document("unsupported-class.json")
    s.wait_for("exchange-rejected",lambda r:"Unable to continue" in s.texts(r));s.capture("exchange-unsupported-rejected");assert saved()["importedDraftClass"]==checked["importedDraftClass"]
    Path("player-exchange-check.json").write_text(json.dumps({"syntheticCollegeSnapshot":True,"realCollegeExport":True,"nativeShareSheet":True,"nativeDocumentSave":True,"androidDocumentPicker":True,"fiveImported":True,"generatedFiller":315,"identityAppearance":True,"financialRosterCareerPreserved":True,"reload":True,"unsupportedRejected":True},indent=2),encoding="utf-8")
    print("PASS: College export/share, Android picker preview/import, translated prospects, preserved franchise and reload/invalid rejection.")
finally:Path("player-exchange-logcat.txt").write_bytes(s.adb("logcat","-d","-t","1200"))
