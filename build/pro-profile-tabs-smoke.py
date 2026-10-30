"""Focused profile navigation and regular/playoff statistic selection, reusing validated game fixture."""
from pathlib import Path
exec(compile(Path(__file__).with_name("pro-staff-calendar-smoke.py").read_text(encoding="utf-8").split("\ntry:\n")[0],"profile-tabs-base","exec"),globals())
def pick(control,label):
    root=s.capture("pro-profile-picker-"+control);s.tap_id(root,control)
    try:root=s.scroll_to_text("pro-profile-choice-"+label,label,max_swipes=10)
    except AssertionError:root=s.scroll_to_text("pro-profile-choice-up-"+label,label,max_swipes=15,toward_start=True)
    s.tap(root,label)
def tab(name):
    root=s.capture("pro-profile-before-"+name);s.tap_id(root,"pro-profile-tab-"+name.lower());return s.capture("pro-profile-"+name.lower())
try:
    fixture=json.loads(gzip.decompress(next(Path("apk").glob("**/pro-staff-statistics-ui-fixture.json.gz")).read_bytes()))
    regular=next(g for g in fixture["games"] if g["phase"]==1 and g["football"]["hasPlayed"])
    quarterback=next(p for p in regular["football"]["playerStats"] if p["teamName"]==fixture["userTeam"] and p["passAttempts"]>0)
    playoff=next(g for g in fixture["games"] if g["phase"]==2 and g["football"]["hasPlayed"])
    expected=next(p["passYards"] for p in playoff["football"]["playerStats"] if p["playerId"]==quarterback["playerId"])
    load(fixture);rotate(1);navigate("Statistics")
    pick("pro-stat-competition","Playoffs");ready("playoff-league",lambda r:any("players" in (t or "") and "Playoffs" in (t or "") for t in s.texts(r)));s.capture("pro-profile-league-playoffs")
    navigate("Roster");click("quarterback",quarterback["playerName"])
    ready("overview-tab",lambda r:"Overview" in s.texts(r) and "Attributes" in s.texts(r) and "Analytics" in s.texts(r))
    s.capture("pro-profile-overview")
    root=tab("Attributes");assert "Player attributes" in s.texts(root)
    root=tab("Contract");assert any("Contract" in (t or "") for t in s.texts(root))
    root=tab("Statistics");assert "Regular season" in s.texts(root)
    pick("pro-profile-stat-competition","Playoffs");root=ready("playoff-profile",lambda r:str(expected) in s.texts(r));assert "Regular season" not in s.texts(root);s.capture("pro-profile-playoff-totals")
    pick("pro-profile-stat-period","Career");ready("career-profile",lambda r:"Playoffs · Career" in s.texts(r));s.capture("pro-profile-playoff-career")
    tab("Analytics");root=tab("Statistics");assert "Playoffs" in s.texts(root) and "Career" in s.texts(root);root=tab("Overview");assert "Player attributes" not in s.texts(root);s.capture("pro-profile-return-overview")
    Path("pro-profile-tabs-check.json").write_text(json.dumps({"mainPlayoffFilter":True,"allFiveTabs":True,"separatePlayoffYards":True,"playoffCareer":True,"tabStateRetained":True,"returnOverview":True},indent=2),encoding="utf-8")
finally:Path("pro-profile-tabs-logcat.txt").write_bytes(s.adb("logcat","-d","-t","1000"))
