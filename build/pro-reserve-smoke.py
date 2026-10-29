"""Scoped reserve activation on a synthetic, core-tested 32-team save."""
import gzip,importlib.util,json
from pathlib import Path

def run(s):
    package="antdroid.profootball.franchise"
    def ready(name,predicate):
        def check(root):
            assert "Unable to continue" not in s.texts(root),"App error at "+name
            return predicate(root)
        return s.wait_for("pro-reserve-"+name,check,timeout=180)
    def click(name,label,toward_start=False):
        root=s.scroll_to_text("pro-reserve-"+name,label,max_swipes=20,toward_start=toward_start);s.tap(root,label)
    data=json.loads(gzip.decompress(next(Path("apk").glob("**/pro-staff-reserve-ui-fixture.json.gz")).read_bytes()))
    fixture=data["franchise"];slot=fixture["id"]
    path=Path("pro-reserve-fixture.json");path.write_text(json.dumps(fixture),encoding="utf-8")
    s.adb("shell","am","force-stop",package)
    s.adb("push",str(path),"/data/local/tmp/pro-reserve-fixture.json")
    s.adb("shell","run-as",package,"mkdir","-p","files/pro-franchises-v1")
    s.adb("shell","run-as",package,"cp","/data/local/tmp/pro-reserve-fixture.json","files/pro-franchises-v1/"+slot+".json")
    def open_slot():
        s.adb("shell","monkey","-p",package,"-c","android.intent.category.LAUNCHER","1")
        ready("library",s.library_loaded)
        click("slot",f"{fixture['name']} · {fixture['userTeam']} · {fixture['year']} RegularSeason W{fixture['week']}")
        ready("overview",s.pro_overview_loaded)
    def profile(name):
        click("roster","Roster")
        root=s.scroll_to_text("pro-reserve-find","Find a player",max_swipes=20,toward_start=True)
        s.tap_id(root,"pro-roster-search");s.adb("shell","input","text",name.replace(" ","%s"));s.adb("shell","input","keyevent","4")
        click("player",name)
        return ready("profile",lambda r:"Activate to roster" in s.texts(r))
    open_slot()
    root=profile("Reserve Four")
    assert not s.enabled(root,"Activate to roster") and any("4 team games" in (t or "") for t in s.texts(root))
    s.capture("pro-reserve-four-game-minimum")
    s.adb("shell","am","force-stop",package);open_slot()
    root=profile("Reserve Season")
    assert not s.enabled(root,"Activate to roster") and any("Season-ending" in (t or "") for t in s.texts(root))
    s.capture("pro-reserve-season-ending")
    s.adb("shell","am","force-stop",package);open_slot()
    root=profile("Reserve Injured")
    assert not s.enabled(root,"Activate to roster")
    assert any("2 weeks" in (t or "") for t in s.texts(root))
    s.capture("pro-reserve-blocked")
    s.adb("shell","input","keyevent","4")
    # Restart clears the roster search and proves slot navigation independently.
    s.adb("shell","am","force-stop",package);open_slot()
    root=profile("Reserve Ready");assert s.enabled(root,"Activate to roster")
    s.capture("pro-reserve-ready");s.tap(root,"Activate to roster")
    ready("activated",lambda r:"Player profile" in s.texts(r) and "Reserve activation" not in s.texts(r))
    checked=json.loads(s.adb("exec-out","run-as",package,"cat","files/pro-franchises-v1/"+slot+".json"))
    player=next(p for p in checked["players"] if p["football"]["playerId"]==data["readyPlayer"])
    assert not player["reserve"] and not player["practiceSquad"] and player["rosterStatus"]=="ACT"
    assert checked["contracts"]==fixture["contracts"],"Activation changed negotiated contracts"
    assert next(p for p in checked["players"] if p["football"]["playerId"]==data["blockedPlayer"])["reserve"]
    s.adb("shell","am","force-stop",package);open_slot();root=profile_after_activation(s,ready,click)
    Path("pro-reserve-check.json").write_text(json.dumps({"activated":data["readyPlayer"],"fourGameMinimumBlocked":True,"seasonEndingBlocked":True,"injuredBlocked":True,"contractsPreserved":True,"reloadVerified":True},indent=2),encoding="utf-8")
    print("PASS: available reserve activation, disabled injured action, persisted active status, unchanged contracts and reload.")

def profile_after_activation(s,ready,click):
    click("reloaded-roster","Roster")
    root=s.scroll_to_text("pro-reserve-reloaded-find","Find a player",max_swipes=20,toward_start=True)
    s.tap_id(root,"pro-roster-search");s.adb("shell","input","text","Reserve%sReady");s.adb("shell","input","keyevent","4")
    click("reloaded-player","Reserve Ready")
    root=ready("reloaded-profile",lambda r:"Player profile" in s.texts(r))
    assert "Activate to roster" not in s.texts(root)
    s.capture("pro-reserve-reloaded")
    return root

if __name__=="__main__":
    spec=importlib.util.spec_from_file_location("shared_smoke",Path(__file__).with_name("android-ui-smoke.py"));s=importlib.util.module_from_spec(spec);spec.loader.exec_module(s)
    try:run(s)
    finally:Path("pro-reserve-logcat.txt").write_bytes(s.adb("logcat","-d","-t","1000"))
