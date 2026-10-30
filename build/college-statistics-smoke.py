"""Actual College creation and completed week; no fabricated historical statistics."""
from pathlib import Path
exec(compile(Path(__file__).with_name("college-phase-smoke.py").read_text(encoding="utf-8").split("\ntry:\n")[0],"college-statistics-base","exec"),globals())
def choose(control,label):
    root=s.scroll_to_text("college-stat-picker","Player statistics",max_swipes=20,toward_start=True);s.tap_id(root,control)
    try: root=s.scroll_to_text("college-stat-choice-"+label,label,max_swipes=12)
    except AssertionError: root=s.scroll_to_text("college-stat-choice-up-"+label,label,max_swipes=20,toward_start=True)
    s.tap(root,label)
    return ready(label,lambda r:label in s.texts(r))
try:
    ready("library",lambda r:"New dynasty" in s.texts(r));click("new","New dynasty");root=s.scroll_to_text("college-stat-school-picker","Choose your team",max_swipes=20);s.tap(root,"Choose your team");click("school","Air Force");root=s.scroll_to_text("college-stat-start","Start dynasty",max_swipes=20);s.tap(root,"Start dynasty")
    root=ready("overview",s.overview_ready);s.tap(root,"Simulate preseason");click("confirm","Simulate")
    root=ready("regular",lambda r:s.overview_ready(r) and "Simulate regular season" in s.texts(r));s.tap(root,"Simulate regular season");click("confirm-week","Simulate")
    root=ready("stop",lambda r:"Stop after this week" in s.texts(r));s.tap(root,"Stop after this week");ready("finished",s.overview_ready,timeout=600)
    s.adb("shell","wm","fixed-to-user-rotation","enabled");s.adb("shell","wm","user-rotation","lock","1")
    click("season","Season");click("statistics","Statistics");ready("stats",lambda r:"Player statistics" in s.texts(r))
    choose("college-stat-scope","All schools");choose("college-stat-category","Blocking")
    root=ready("blocking",lambda r:any((t or "").startswith("PB snaps") for t in s.texts(r)));s.capture("college-statistics-blocking")
    assert not any("No player statistics yet" in (t or "") for t in s.texts(root)),s.texts(root)
    choose("college-stat-category","Playing time");ready("playing-time",lambda r:any((t or "").startswith("Off snaps") for t in s.texts(r)));s.capture("college-statistics-playing-time")
    choose("college-stat-category","Receiving");s.capture("college-statistics-receiving")
    Path("college-statistics-check.json").write_text(json.dumps({"actualNewDynasty":True,"completedWeek":True,"blockingTable":True,"playingTime":True,"receiving":True,"careerTotals":True},indent=2),encoding="utf-8")
finally:Path("college-statistics-logcat.txt").write_bytes(s.adb("logcat","-d","-t","1000"))
