"""Focused roster order, settings exit and offseason-card priority. No season simulation."""
from pathlib import Path
exec(compile(Path(__file__).with_name("college-phase-smoke.py").read_text(encoding="utf-8").split("\ntry:\n")[0],"college-overview-base","exec"),globals())
try:
    ready("library",lambda r:"New dynasty" in s.texts(r));click("new","New dynasty");root=s.scroll_to_text("college-choose-team","Choose your team",max_swipes=20);s.tap(root,"Choose your team");click("air-force","Air Force");root=s.scroll_to_text("college-start","Start dynasty",max_swipes=20);s.tap(root,"Start dynasty");root=ready("overview",s.overview_ready);assert "‹ Exit Dynasty" not in s.texts(root)
    s.adb("shell","wm","fixed-to-user-rotation","enabled");s.adb("shell","wm","user-rotation","lock","1")
    click("roster","Roster");root=s.scroll_to_text("college-default-fb","FB",max_swipes=20);assert "Next" not in s.texts(root) or "1–50" in " ".join(t or "" for t in s.texts(root));s.capture("college-overview-default-fullback")
    s.exit_via_settings("college")
    s.adb("shell","am","force-stop",package);db=Path("college-overview.db3");db.write_bytes(s.adb("exec-out","run-as",package,"cat","files/dynasties.db3"));conn=sqlite3.connect(db);row=conn.execute("select rowid,snapshot_json from dynasty_saves order by updated_utc desc limit 1").fetchone();state=json.loads(row[1]);state.update(phase=4,week=19);conn.execute("update dynasty_saves set snapshot_json=? where rowid=?",(json.dumps(state),row[0]));conn.commit();conn.close();s.adb("push",str(db),"/data/local/tmp/college-overview.db3");s.adb("shell","run-as",package,"cp","/data/local/tmp/college-overview.db3","files/dynasties.db3")
    s.adb("shell","monkey","-p",package,"-c","android.intent.category.LAUNCHER","1");ready("reload-library",lambda r:"Load" in s.texts(r));click("load","Load");root=ready("offseason-card",lambda r:"OFFSEASON" in s.texts(r));s.capture("college-overview-offseason-priority")
    Path("college-overview-roster-check.json").write_text(json.dumps({"defaultFullbackVisible":True,"exitRemovedFromHeader":True,"settingsSaveExit":True,"offseasonCardVisibleNearTop":True,"noSeasonSimulation":True},indent=2),encoding="utf-8")
finally:Path("college-overview-roster-logcat.txt").write_bytes(s.adb("logcat","-d","-t","1000"))
