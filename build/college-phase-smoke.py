"""Real College creation, preseason phase boundary and interruptible regular-season skip."""
import importlib.util,json,sqlite3,time
from pathlib import Path
spec=importlib.util.spec_from_file_location("shared",Path(__file__).with_name("android-ui-smoke.py"));s=importlib.util.module_from_spec(spec);spec.loader.exec_module(s)
package="antdroid.cfbcoach.dev"
def click(name,label):
    root=s.scroll_to_text("college-phase-"+name,label,max_swipes=20,toward_start=True);s.tap(root,label)
def ready(name,predicate,timeout=300):return s.wait_for("college-phase-"+name,predicate,timeout=timeout)
try:
    ready("library",lambda r:"New dynasty" in s.texts(r) and "Your dynasties" in s.texts(r))
    click("new","New dynasty");click("picker","Choose your team");click("school","Air Force");click("start","Start dynasty")
    root=ready("overview",s.overview_ready);assert "Simulate preseason" in s.texts(root)
    s.tap(root,"Simulate preseason");click("confirm","Simulate")
    root=ready("preseason-boundary",lambda r:s.overview_ready(r) and "Simulate regular season" in s.texts(r));s.capture("college-phase-preseason-boundary")
    s.tap(root,"Simulate regular season");click("confirm-regular","Simulate")
    root=ready("stop-control",lambda r:"Stop after this week" in s.texts(r));s.tap(root,"Stop after this week")
    root=ready("stopped",s.overview_ready,timeout=600);assert "Simulate regular season" in s.texts(root);s.capture("college-phase-stopped-regular")
    s.adb("shell","am","force-stop",package);db=Path("college-phase.db3");db.write_bytes(s.adb("exec-out","run-as",package,"cat","files/dynasties.db3"))
    conn=sqlite3.connect(db);rows=conn.execute("select snapshot_json from dynasty_saves order by updated_utc desc").fetchall();assert rows
    state=json.loads(rows[0][0]);assert state["phase"]==1 and state["week"]<18
    s.adb("shell","monkey","-p",package,"-c","android.intent.category.LAUNCHER","1");ready("saved-library",lambda r:"Load" in s.texts(r));click("load","Load");ready("reloaded",s.overview_ready);s.capture("college-phase-reloaded")
    Path("college-phase-check.json").write_text(json.dumps({"realCreatedDynasty":True,"preseasonStopsAtRegularSeason":True,"stopAfterWeek":True,"weeklyAutosave":True,"reload":True,"savedWeek":state["week"]},indent=2),encoding="utf-8")
finally:Path("college-phase-logcat.txt").write_bytes(s.adb("logcat","-d","-t","1000"))
