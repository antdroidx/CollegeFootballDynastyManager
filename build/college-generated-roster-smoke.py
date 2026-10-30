"""Two new College worlds with fictional generation enabled, persisted players on reload."""
import importlib.util,json,re,sqlite3
from pathlib import Path
spec=importlib.util.spec_from_file_location("shared",Path(__file__).with_name("android-ui-smoke.py"));s=importlib.util.module_from_spec(spec);spec.loader.exec_module(s)
package="antdroid.cfbcoach.dev"
def ready(name,predicate):return s.wait_for("college-generated-"+name,predicate,timeout=360)
def click(name,label):
    root=s.scroll_to_text("college-generated-"+name,label,max_swipes=20,toward_start=True);s.tap(root,label)
def create():
    click("new","New dynasty");root=s.scroll_to_text("college-generated-switch","Generate new players · keep program strengths",max_swipes=20);s.tap_id(root,"college-generate-players");s.capture("college-generated-enabled");click("picker","Choose your team");click("school","Air Force");click("start","Start dynasty");ready("overview",s.overview_ready);click("save","Save");s.exit_via_settings("college");ready("library",lambda r:"Load" in s.texts(r))
def read():
    s.adb("shell","am","force-stop",package);db=Path("college-generated.db3");db.write_bytes(s.adb("exec-out","run-as",package,"cat","files/dynasties.db3"));conn=sqlite3.connect(db);rows=conn.execute("select snapshot_json from dynasty_saves order by updated_utc desc").fetchall();conn.close();return [json.loads(r[0]) for r in rows]
try:
    ready("library",lambda r:"Your dynasties" in s.texts(r) and "New dynasty" in s.texts(r));create();a=read()[0]
    s.adb("shell","monkey","-p",package,"-c","android.intent.category.LAUNCHER","1");ready("library-two",lambda r:"New dynasty" in s.texts(r));create();b=next(p for p in read() if p["dynastyId"]!=a["dynastyId"])
    assert a["startingRosterName"].startswith("Generated players") and b["startingRosterName"].startswith("Generated players")
    assert not {p["playerId"] for p in a["activeRoster"]}&{p["playerId"] for p in b["activeRoster"]};assert [p["fullName"] for p in a["activeRoster"]]!=[p["fullName"] for p in b["activeRoster"]]
    s.adb("shell","monkey","-p",package,"-c","android.intent.category.LAUNCHER","1");ready("reload-library",lambda r:"Load" in s.texts(r));click("load","Load");ready("reloaded",s.overview_ready);s.capture("college-generated-reloaded");click("save-reloaded","Save");saved=read()[0];assert saved["activeRoster"]==b["activeRoster"]
    assert any(p["position"]==13 for p in b["activeRoster"]),"Imported/generated FB identities must be distinct"
    s.adb("shell","monkey","-p",package,"-c","android.intent.category.LAUNCHER","1");ready("backfield-library",lambda r:"Load" in s.texts(r));click("backfield-load","Load");ready("backfield-overview",s.overview_ready);click("backfield-roster","Roster")
    root=s.scroll_to_text("college-backfield-filter","All positions",max_swipes=20,toward_start=True);s.tap_id(root,"college-roster-position");click("backfield-fb","FB");s.capture("college-backfield-fullbacks")
    Path("college-generated-roster-check.json").write_text(json.dumps({"twoRealNewDynasties":True,"freshIdentitiesAndNames":True,"generatedOption":True,"distinctFullbacks":True,"reloadPreservesPlayers":True,"rosterSize":len(b["activeRoster"])},indent=2),encoding="utf-8")
finally:Path("college-generated-roster-logcat.txt").write_bytes(s.adb("logcat","-d","-t","1000"))
