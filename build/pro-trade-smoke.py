"""Scoped Pro offer UI acceptance on a disposable emulator; can reuse an exact verified APK."""
import gzip
import importlib.util
import json
from pathlib import Path


def run(s):
    def contains(root,value):return any(value in (t or "") for t in s.texts(root))
    def ready(name,predicate):
        def check(root):
            assert "Unable to continue" not in s.texts(root),"App error at "+name
            return predicate(root)
        return s.wait_for("pro-trade-"+name,check,timeout=180)
    def click(name,label):
        root=s.scroll_to_text("pro-trade-"+name,label,max_swipes=20);s.tap(root,label)
    s.adb("shell","run-as","antdroid.profootball.franchise","mkdir","-p","files/pro-franchises-v1")
    # Built by the actual core offer generator inside its regression test, not a hand-priced Python mirror.
    fixture_pack=next(Path("apk").glob("**/pro-trade-ui-fixture.json.gz"))
    trade_fixtures=json.loads(gzip.decompress(fixture_pack.read_bytes()))
    for fixture in trade_fixtures:
        accept=fixture["name"].endswith("accept");fixture_id=fixture["id"];offer=fixture["tradeOffers"][0]
        before_player=next(p for p in fixture["players"] if p["football"]["playerId"]==offer["playerId"])
        s.adb("shell","am","force-stop","antdroid.profootball.franchise")
        path=Path("pro-trade-fixture.json");path.write_text(json.dumps(fixture),encoding="utf-8")
        s.adb("push",str(path),"/data/local/tmp/pro-trade-fixture.json")
        s.adb("shell","run-as","antdroid.profootball.franchise","cp","/data/local/tmp/pro-trade-fixture.json","files/pro-franchises-v1/"+fixture_id+".json")
        s.adb("shell","monkey","-p","antdroid.profootball.franchise","-c","android.intent.category.LAUNCHER","1")
        ready("trade-library",s.library_loaded)
        slot_label=f"{fixture['name']} · {fixture['userTeam']} · {fixture['year']} Preseason W{fixture['week']}"
        click("trade-loaded-slot",slot_label)
        ready("trade-overview",s.pro_overview_loaded)
        s.scroll_to_text("pro-trade-overview-top","Advance week",max_swipes=20,toward_start=True)
        click("trade-office","Front Office");click("trade-offers","Trade offers (1)")
        ready("trade-pending",lambda r:"1 open offers" in s.texts(r));s.capture("pro-trade-offer")
        if accept:
            click("trade-counter","Ask for more")
            ready("trade-countered",lambda r:contains(r,"Counters 1/2"))
            click("trade-accept","Accept offer")
            confirm=ready("trade-confirm",lambda r:"Accept trade offer?" in s.texts(r));s.tap(confirm,"Accept")
        else:click("trade-decline","Decline offer")
        ready("trade-complete",lambda r:"0 open offers" in s.texts(r))
        s.scroll_to_text("pro-trade-decision","Accepted" if accept else "Declined",max_swipes=12)
        s.capture("pro-trade-"+("accepted" if accept else "declined"))
        checked=json.loads(s.adb("exec-out","run-as","antdroid.profootball.franchise","cat","files/pro-franchises-v1/"+fixture_id+".json"))
        decision=checked["tradeOffers"][0];assert decision["status"]==(1 if accept else 2)
        player=next(p for p in checked["players"] if p["football"]["playerId"]==offer["playerId"])
        assert player["football"]["teamName"]==(offer["buyer"] if accept else offer["seller"])
        assert all(next(p for p in checked["picks"] if p["id"]==pick)["owner"]==(offer["seller"] if accept else offer["buyer"]) for pick in decision["pickIds"])
        assert len(checked["tradeHistory"])==(1 if accept else 0)
        if accept:assert decision["counters"]==1 and player["football"]["depthChartOrder"]==1

    print("PASS: isolated CPU offer counter/accept/decline, persisted player/pick ownership, transaction history and acquired starter depth.")


if __name__=="__main__":
    spec=importlib.util.spec_from_file_location("shared_smoke",Path(__file__).with_name("android-ui-smoke.py"));s=importlib.util.module_from_spec(spec);spec.loader.exec_module(s)
    try:run(s)
    finally:Path("pro-trade-logcat.txt").write_bytes(s.adb("logcat","-d","-t","1000"))
