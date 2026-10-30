"""Completed production-game fixture: detailed blocking, receiving and playing-time tables."""
from pathlib import Path
exec(compile(Path(__file__).with_name("pro-staff-calendar-smoke.py").read_text(encoding="utf-8").split("\ntry:\n")[0],"pro-statistics-base","exec"),globals())
def choose(label):
    root=s.scroll_to_text("pro-stat-picker", "Player statistics",max_swipes=20,toward_start=True)
    s.tap_id(root,"pro-stat-category");click("choose-"+label,label)
    return ready(label,lambda r:any(label in (t or "") and "players" in (t or "") for t in s.texts(r)))
try:
    fixture=json.loads(gzip.decompress(next(Path("apk").glob("**/pro-staff-statistics-ui-fixture.json.gz")).read_bytes()))
    load(fixture);rotate(1);navigate("Statistics")
    root=choose("Blocking");assert "PB snaps" in s.texts(root),s.texts(root);s.capture("pro-statistics-blocking")
    # Identity columns remain links; wide statistical tables scroll horizontally.
    scroll=next(n for n in root.iter("node") if "HorizontalScrollView" in n.get("class","") and n.get("scrollable")=="true")
    l,t,r,b=map(int,re.findall(r"\d+",scroll.get("bounds")))
    s.adb("shell","input","swipe",str(l+(r-l)*4//5),str((t+b)//2),str(l+(r-l)//5),str((t+b)//2),"400")
    s.capture("pro-statistics-blocking-details")
    root=choose("Playing time");assert "Off snaps" in s.texts(root);s.capture("pro-statistics-playing-time")
    root=choose("Receiving");s.capture("pro-statistics-receiving")
    root=s.scroll_to_text("pro-stat-period","Player statistics",max_swipes=20,toward_start=True);s.tap_id(root,"pro-stat-period");click("career","Career")
    ready("career",lambda r:any("Receiving" in (t or "") and "Career" in (t or "") for t in s.texts(r)));s.capture("pro-statistics-career")
    Path("pro-statistics-check.json").write_text(json.dumps({"productionGameFixture":True,"blockingTable":True,"horizontalScroll":True,"playingTime":True,"receiving":True,"career":True},indent=2),encoding="utf-8")
finally:Path("pro-statistics-logcat.txt").write_bytes(s.adb("logcat","-d","-t","1000"))
