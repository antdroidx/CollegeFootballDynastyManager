"""Native depth dragging, arbitrary moves and out-of-position save acceptance."""
from pathlib import Path
import time
exec(compile(Path(__file__).with_name("pro-staff-calendar-smoke.py").read_text(encoding="utf-8").split("\ntry:\n")[0],"shared-management-helpers","exec"),globals())

def saved():return json.loads(s.adb("exec-out","run-as",package,"cat","files/pro-franchises-v1/"+fixture["id"]+".json"))
def node(root,id):return next(n for n in root.iter("node") if n.get("resource-id")==id or n.get("resource-id", "").endswith(":id/"+id) or n.get("content-desc")==id)
def center(n):
    l,t,r,b=map(int,re.findall(r"\d+",n.get("bounds")));return (l+r)//2,(t+b)//2

def editor():
    navigate("Roster");root=s.scroll_to_text("pro-depth-open","Organize depth chart",max_swipes=20,toward_start=True);s.tap(root,"Organize depth chart")
    return ready("depth-editor",lambda r:"Playing position" in s.texts(r) or "QB depth · 3" in s.texts(r))
try:
    fixture=json.loads(gzip.decompress(next(Path("apk").glob("**/pro-staff-depth-ui-fixture.json.gz")).read_bytes()));load(fixture)
    root=editor()
    handles=[n for n in root.iter("node") if "pro-depth-drag-" in (n.get("resource-id","")+n.get("content-desc",""))]
    assert len(handles)>=3,"Three QB drag handles must be visible"
    def handle_id(n):return re.search(r"pro-depth-drag-([0-9a-f-]{36})",n.get("resource-id","")+n.get("content-desc","")).group(1)
    first,last=handle_id(handles[0]),handle_id(handles[2]);x,y=center(handles[0]);tx,ty=center(handles[2])
    # A held native touch followed by gradual moves; no fallback buttons count as drag evidence.
    commands=[f"input touchscreen motionevent DOWN {x} {y}","sleep 1"]
    for step in range(1,13):commands.extend([f"input touchscreen motionevent MOVE {x+(tx-x)*step//12} {y+(ty-y)*step//12}","sleep 0.07"])
    commands.append(f"input touchscreen motionevent UP {tx} {ty}");s.adb("shell","; ".join(commands))
    root=ready("native-drop",lambda r:any(t.startswith("3 · Move") for t in s.texts(r)))
    data=saved();a=next(a for a in data["depthChartAssignments"] if a["playerId"]==first);assert a["order"]==3,"Native drag did not move QB to last slot"
    s.capture("pro-depth-native-drag")
    root=s.scroll_to_text("pro-depth-move-last","3 · Move to…",max_swipes=12);s.tap(root,"3 · Move to…");click("move-first","Depth 1")
    ready("move-first",lambda r:any("1 · Move" in t for t in s.texts(r)));assert next(a for a in saved()["depthChartAssignments"] if a["playerId"]==first)["order"]==1
    root=s.capture("pro-depth-picker");s.tap_id(root,"pro-depth-position");click("choose-te","TE")
    root=s.scroll_to_text("pro-depth-search","Find any active player",max_swipes=15);entry=next(n for n in root.iter("node") if n.get("text")=="Find any active player");sx,sy=center(entry);s.adb("shell","input","tap",str(sx),str(sy))
    wr=next(p for p in fixture["players"] if p["football"]["teamName"]==fixture["userTeam"] and p["football"]["position"]==2 and not p.get("practiceSquad") and not p.get("reserve") and not p.get("retired"))
    s.adb("shell","input","text",wr["football"]["fullName"].replace(" ","%s"));s.adb("shell","input","keyevent","111");click("assign-wr","Assign")
    data=saved();assert next(a for a in data["depthChartAssignments"] if a["playerId"]==wr["football"]["playerId"])["positionCode"]==4
    assert data["players"]==fixture["players"] and data["contracts"]==fixture["contracts"]
    s.capture("pro-depth-out-of-position");s.adb("shell","input","keyevent","4");load(data);root=editor();s.tap_id(root,"pro-depth-position");click("reload-te","TE");root=s.scroll_to_text("pro-depth-reloaded",wr["football"]["fullName"],max_swipes=20);s.capture("pro-depth-reloaded")
    Path("pro-depth-editor-check.json").write_text(json.dumps({"syntheticFixture":True,"nativeDragToBottom":True,"arbitraryRankMove":True,"outOfPositionAssignment":True,"naturalPlayersAndContractsUnchanged":True,"reloadPreservesAssignments":True},indent=2),encoding="utf-8")
finally:Path("pro-depth-editor-logcat.txt").write_bytes(s.adb("logcat","-d","-t","1000"))
