"""Check the launch/create/cancel flow on an already running disposable emulator."""
import re
import subprocess
import time
import xml.etree.ElementTree as ET
from pathlib import Path


def adb(*args):
    return subprocess.check_output(["adb", *args], stderr=subprocess.STDOUT, timeout=30)


def capture(name, timeout=90):
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        try:
            adb("shell", "rm", "-f", "/sdcard/dynasty-ui.xml")
            dumped = adb("shell", "uiautomator", "dump", "/sdcard/dynasty-ui.xml")
            if b"dumped to:" not in dumped:
                time.sleep(1)
                continue
            xml = adb("exec-out", "cat", "/sdcard/dynasty-ui.xml")
        except (subprocess.CalledProcessError, subprocess.TimeoutExpired):
            time.sleep(1)
            continue
        Path(f"android-{name}.xml").write_bytes(xml)
        try:
            root = ET.fromstring(xml)
        except ET.ParseError:
            # ADB can briefly return transport/startup text during a fresh boot.
            time.sleep(1)
            continue
        Path(f"android-{name}.png").write_bytes(adb("exec-out", "screencap", "-p"))
        visible = texts(root)
        if "Quickstep isn't responding" in visible:
            tap(root, "Close app")
        elif "System UI isn't responding" in visible:
            tap(root, "Wait")
        else:
            # Never dismiss a game ANR or another application's error dialog.
            return root
    raise AssertionError(f"Could not capture a fresh, unobstructed UI for {name}")


def texts(root):
    return {node.get("text") for node in root.iter("node")}


def tap(root, text):
    node = next(node for node in root.iter("node") if node.get("text") == text)
    left, top, right, bottom = map(int, re.findall(r"\d+", node.get("bounds")))
    assert right > left and bottom > top, f"Target is not visible: {text}"
    adb("shell", "input", "tap", str((left + right) // 2), str((top + bottom) // 2))
    time.sleep(1)


def scroll_to_text(name, text, max_swipes=6):
    for attempt in range(max_swipes + 1):
        root = capture(name)
        for node in root.iter("node"):
            if node.get("text") == text:
                left, top, right, bottom = map(int, re.findall(r"\d+", node.get("bounds")))
                if right > left and bottom > top:
                    return root
        scrollables = [node for node in root.iter("node") if node.get("scrollable") == "true"]
        if not scrollables or attempt == max_swipes:
            raise AssertionError(f"Could not find visible {text}: {sorted(texts(root))}")
        def area(node):
            x1,y1,x2,y2=map(int,re.findall(r"\d+",node.get("bounds")))
            return (x2-x1)*(y2-y1)
        left, top, right, bottom = map(int, re.findall(r"\d+", max(scrollables,key=area).get("bounds")))
        x = str((left + right) // 2)
        adb("shell", "input", "swipe", x, str(top + (bottom-top)*3//4), x, str(top + (bottom-top)//4), "350")


def wait_for(name, predicate, timeout=120):
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        root = capture(name)
        if predicate(root):
            return root
        time.sleep(2)
    raise AssertionError(f"Timed out waiting for {name}: {sorted(texts(root))}")


def enabled(root, text):
    return any(n.get("text") == text and n.get("enabled") == "true" for n in root.iter("node"))


def calendar(root):
    # The shell header precedes dated news cards in document order.
    values = [node.get("text") for node in root.iter("node")]
    header = next((value for value in values if value and re.search(r" · \d{4} · (?:Week \d+(?: · Opening week)?|Preseason)$", value)), None)
    phase = next((value for value in values if value and value.endswith((" · Preseason", " · Regular season"))), None)
    return (header, phase)


def advance_action(root):
    return next((label for label in ("Start season", "Advance to Week 1", "Advance Week") if enabled(root, label)), None)


def overview_ready(root, previous=None):
    state = calendar(root)
    busy = any(node.get("class") == "android.widget.ProgressBar" for node in root.iter("node"))
    return (advance_action(root) is not None and state[0] is not None and state[1] is not None
            and not busy and (previous is None or state != previous))


def check_college_center():
    tap(capture("season-navigation"), "Season")
    tap(scroll_to_text("season-center", "Awards, polls & program center ›"), "Awards, polls & program center ›")
    honors = wait_for("preseason-honors", lambda r: "Preseason honors" in texts(r))
    assert "Trophy room" in texts(honors)
    tap(honors, "Awards & honors")
    tap(capture("college-center-sections"), "Polls & trends")
    polls = wait_for("poll-trends", lambda r: "Poll tracker" in texts(r))
    assert "Media" in texts(polls)
    tap(polls, "Polls & trends")
    tap(capture("college-center-sections-2"), "Program conduct")
    conduct = wait_for("program-conduct", lambda r: "Program standards" in texts(r))
    assert any("Conduct 80/100" in (value or "") for value in texts(conduct))
    tap(conduct, "Return to season")
    tap(capture("season-return"), "Overview")
    return wait_for("overview-after-college-center", overview_ready)


def main():
    home = wait_for("launch", lambda r: {"Your dynasties", "New dynasty"} <= texts(r) and
        not any(t.startswith("Loading ") for t in texts(r)))
    assert {"Your dynasties", "New dynasty"} <= texts(home), "Launch must open the save library"
    assert not {"Advance Week", "Start season", "Advance to Week 1", "Overview", "Roster", "Coach", "Recruit", "Season", "Settings"} & texts(home), "Career controls leaked onto Home"
    tap(home, "New dynasty")
    create = capture("new-dynasty")
    assert {"Start a dynasty", "Dynasty name", "League structure", "Your school", "Start dynasty"} <= texts(create)
    tap(create, "‹ Cancel")
    assert "Your dynasties" in texts(capture("cancel-create")), "Cancel must return to the library"
    assert adb("shell", "pidof", "antdroid.cfbcoach.dev").strip(), "App exited during navigation"
    print("Save library, new-dynasty form, and cancel flow passed.")


    # Exercise a real dynasty rather than a viewer-only fixture, including the week gate.
    tap(capture("library"), "New dynasty")
    tap(capture("create-form"), "Choose your team")
    tap(capture("school-picker"), "Air Force")
    tap(capture("create-ready"), "Start dynasty")
    overview = wait_for("overview-preseason", lambda r: overview_ready(r), timeout=300)
    overview = check_college_center()
    for week in range(4):
        if "Play game" in texts(overview):
            break
        previous = calendar(overview)
        tap(overview, advance_action(overview))
        overview = wait_for(f"overview-week-{week}", lambda r: overview_ready(r, previous), timeout=300)
    assert "Play game" in texts(overview), "Expected a current-week user matchup"
    tap(overview,"Settings")
    wait_for("management-before-layouts",lambda r:"Appearance" in texts(r))
    overview=check_management_layouts()
    tap(overview, "Play game")
    toss = wait_for("coin-toss", lambda r: "Heads" in texts(r) or "Receive" in texts(r) or "Start game" in texts(r))
    assert any((n.get("resource-id") or "").endswith("coin-toss-field") for n in toss.iter("node")), "Coin toss must be on the field"
    if "Heads" in texts(toss):
        tap(toss, "Heads")
        toss = wait_for("coin-result", lambda r: "Receive" in texts(r) or "Start game" in texts(r))
    if "Receive" in texts(toss):
        tap(toss, "Receive")
        toss = wait_for("coin-ready", lambda r: "Start game" in texts(r))
    adb("shell", "settings", "put", "system", "accelerometer_rotation", "0")
    adb("shell", "settings", "put", "system", "user_rotation", "1")
    time.sleep(2)
    toss = capture("coin-toss-landscape")
    assert "Start game" in texts(toss)
    adb("shell", "settings", "put", "system", "user_rotation", "0")
    time.sleep(2)
    toss = capture("coin-toss-portrait-ready")
    tap(toss, "Start game")
    def has_field(root):
        return any((n.get("resource-id") or "").endswith("game-day-field") for n in root.iter("node"))

    def game_clock(root):
        return next(t for t in texts(root) if t and re.match(r"^Q\d+\s+\d+:\d+$", t))

    def options_choice(item, name):
        tap(capture(name + "-ready"), "Options")
        menu = wait_for(name + "-options", lambda r: "GameCast options" in texts(r))
        assert any((n.get("resource-id") or "").endswith("game-dialog-surface") for n in menu.iter("node")), "Live menu must use themed dialog surface"
        tap(menu, item)
        return capture(name)

    def open_box(name):
        options_choice("Box score", name)
        return wait_for(name, lambda r: "Live box score" in texts(r) or "Final statistics" in texts(r))

    viewer = wait_for("game-day-portrait", lambda r: "Call play" in texts(r) or "Call defense" in texts(r))
    badges = [n for n in viewer.iter("node") if n.get("content-desc", "").endswith(" team badge")]
    assert len(badges) == 2, "Broadcast scoreboard must expose both team badges"
    assert any((n.get("resource-id") or "").endswith("game-day-field-frame") for n in viewer.iter("node"))
    before_clock = game_clock(viewer)
    for name in ("Full field", "Broadcast", "Follow ball"):
        choices = options_choice("Camera", "camera-" + name.replace(" ", "-"))
        tap(choices, name)
        assert before_clock == game_clock(capture("camera-returned")), "Camera changes advanced the game clock"
    for name in ("Game speed", "Replay speed"):
        choices = options_choice(name, name.replace(" ", "-"))
        assert {"0.5×", "1×", "1.5×", "2×"} <= texts(choices)
        tap(choices, "1.5×")
        assert before_clock == game_clock(capture("speed-returned")), "Viewing speed changed the game clock"
    tap(capture("team-ready"), "Team")
    team = wait_for("team-depth", lambda r: "Coach talk" in texts(r))
    assert any("Energy" in (t or "") and "Morale" in (t or "") for t in texts(team))
    assert not enabled(team, "Coach talk"), "Coach talk must be disabled outside an eligible break"
    assert "Available at timeouts and quarter breaks" in texts(team)
    assert "Talk to player" not in texts(team), "Individual talks must not be offered"
    assert "Starter" in texts(team) and not enabled(team, "Starter")
    assert len([n for n in team.iter("node") if n.get("text") == "Sub in"]) <= 2, "Default QB list must stay compact"
    tap(team, "Use second unit")
    second = wait_for("second-unit-confirm", lambda r: "Use second unit?" in texts(r))
    tap(second, "Use second unit")
    second = capture("second-unit-active")
    assert "Return to starters" in texts(second)
    assert "Starter" in texts(second)
    tap(second, "Return to starters")
    team = capture("first-unit-restored")
    assert "Use second unit" in texts(team)
    if "Sub in" in texts(team):
        tap(team, "Sub in")
        scope = wait_for("live-substitution-scope", lambda r: "This game only" in texts(r))
        tap(scope, "This game only")
        switched = capture("live-substitution-applied")
        assert "Starter" in texts(switched) and not enabled(switched, "Starter")
        assert "Automatic rotation" in texts(switched)
        tap(switched, "Automatic rotation")
    tap(capture("team-return-ready"), "Return to game")
    assert before_clock == game_clock(capture("team-returned")), "Team management advanced the game clock"
    for rotation, name in ((1, "landscape"), (0, "portrait-restored")):
        adb("shell", "settings", "put", "system", "accelerometer_rotation", "0")
        adb("shell", "settings", "put", "system", "user_rotation", str(rotation))
        time.sleep(2)
        rotated = capture("game-day-" + name)
        assert has_field(rotated) and "Options" in texts(rotated)
        field_node = next(n for n in rotated.iter("node") if (n.get("resource-id") or "").endswith("game-day-field"))
        dock_node = next(n for n in rotated.iter("node") if (n.get("resource-id") or "").endswith("game-day-controls"))
        field_bounds = list(map(int, re.findall(r"\d+", field_node.get("bounds"))))
        dock_bounds = list(map(int, re.findall(r"\d+", dock_node.get("bounds"))))
        assert field_bounds[3] <= dock_bounds[1], "Controls overlap framed field"
        assert before_clock == game_clock(rotated)
    # Fold-size resize while retaining the same game and score panel.
    adb("shell", "wm", "size", "1768x2208")
    time.sleep(3)
    folded = capture("game-day-fold")
    assert has_field(folded) and "Options" in texts(folded)
    assert before_clock == game_clock(folded)
    adb("shell", "wm", "size", "reset")
    time.sleep(2)
    box = open_box("live-box-score")
    assert any("since kickoff" in (t or "") for t in texts(box))
    baseline = texts(box)
    tap(box, "Return to game")
    tap(capture("manual-replay-more"), "More")
    replay = wait_for("manual-replay-ready", lambda r: "Replay last" in texts(r))
    tap(replay, "Replay last")
    returned = wait_for("manual-replay-returned", lambda r: "Options" in texts(r) and ("Call play" in texts(r) or "Call defense" in texts(r)))
    assert before_clock == game_clock(returned), "Replay consumed game time"
    after = open_box("replay-stats-after")
    assert texts(after) == baseline, "Replay changed scores, morale or statistics"
    tap(after, "Return to game")
    tap(capture("watch-ready"), "Watch CPU")
    # Viewing options must be reachable while Watch is advancing.
    watch_box = open_box("watch-box-score")
    snapshot = texts(watch_box)
    time.sleep(2)
    assert texts(capture("watch-box-still-paused")) == snapshot
    tap(watch_box, "Return to game")
    ready = capture("takeover-ready")
    tap(ready, "Take over")
    controlled = wait_for("takeover-controlled", lambda r: "Call play" in texts(r) or "Call defense" in texts(r))
    assert "Watch CPU" in texts(controlled), "Take over did not leave Watch mode"
    tap(controlled, "More")
    tap(scroll_to_text("skip-ready", "Skip to end"), "Skip to end")
    confirmation = wait_for("skip-confirm-cancel", lambda r: "Skip to end?" in texts(r))
    cancel_label = next(t for t in texts(confirmation) if (t or "").casefold() == "keep playing")
    tap(confirmation, cancel_label)
    cancelled = capture("skip-cancelled")
    assert "Close" not in texts(cancelled)
    if "Skip to end" not in texts(cancelled):
        tap(cancelled, "More")
    tap(scroll_to_text("skip-ready-again", "Skip to end"), "Skip to end")
    confirmation = wait_for("skip-confirm-finish", lambda r: "Skip to end?" in texts(r))
    accept_label = next(t for t in texts(confirmation) if (t or "").casefold() == "simulate remainder")
    tap(confirmation, accept_label)
    final = wait_for("skip-final-saved", lambda r: enabled(r, "Close"), timeout=300)
    final_box = open_box("skip-final-box")
    assert "Final statistics" in texts(final_box)
    tap(final_box, "Return to game")
    tap(capture("skip-final-close"), "Close")
    overview = wait_for("after-live-view", overview_ready, timeout=300)
    assert "Play game" not in texts(overview), "Saved game was not marked completed"
    print("Broadcast scoreboard, inset frame, cameras, independent speeds, live morale, coach talks, replay invariants, rotation/Fold, Watch options, Take over and saved final passed.")
    previous = calendar(overview)
    tap(overview, advance_action(overview))
    advanced = wait_for("calendar-advanced", lambda r: overview_ready(r, previous), timeout=300)
    assert calendar(advanced) != previous
    assert "Advance to Week 1" not in texts(advanced), "The calendar remained at Week 0"
    print("Actual week advancement after live-view exit passed.")
    tap(advanced, "Roster")
    roster = wait_for("roster-morale", lambda r: any("Δ = change from season start" in (t or "") for t in texts(r)))
    assert any("Morale responds" in (t or "") for t in texts(roster))
    adb("shell", "settings", "put", "system", "user_rotation", "0")
    time.sleep(2)
    organizer = scroll_to_text("depth-open-button", "Organize depth chart")
    tap(organizer, "Organize depth chart")
    chart = wait_for("depth-chart-portrait", lambda r: "PPOS 0 · QB" in texts(r))
    tap(chart, "✦ Auto Set")
    menu = wait_for("depth-auto-menu", lambda r: "Whole team · roster, scheme & staff" in texts(r))
    tap(menu, "Whole team · roster, scheme & staff")
    chart = wait_for("depth-chart-auto", lambda r: "PPOS 0 · QB" in texts(r))
    tap(chart, "Defense")
    chart = wait_for("depth-chart-defense", lambda r: "PPOS 10 · LE" in texts(r))
    adb("shell", "settings", "put", "system", "user_rotation", "1")
    time.sleep(2)
    chart = capture("depth-chart-landscape")
    assert "PPOS 10 · LE" in texts(chart)
    adb("shell", "settings", "put", "system", "user_rotation", "0")
    time.sleep(2)
    chart = scroll_to_text("depth-close-button", "Close depth chart")
    tap(chart, "Close depth chart")
    roster = wait_for("depth-chart-closed", lambda r: "Organize depth chart" in texts(r))
    tap(roster, "Recruit")
    recruit_hub = wait_for("recruit-hub", lambda r: "High School" in texts(r), timeout=120)
    tap(recruit_hub, "High School")
    recruits = wait_for("season-recruits", lambda r: any((n.get("content-desc") or "").startswith("View recruit ") for n in r.iter("node")), timeout=120)
    assert "Offer Scholarship" not in texts(recruits) and "Choose pitch" not in texts(recruits)
    tap(recruits, "Season")
    season = scroll_to_text("season-careers", "Player departures ›")
    tap(season, "Player departures ›")
    departures = wait_for("departures-view", lambda r: "Player departures" in texts(r) and "Return to season" in texts(r))
    assert any("departures" in (t or "").lower() for t in texts(departures))
    tap(departures, "Return to season")
    season = scroll_to_text("season-hall", "Hall of Fame ›")
    tap(season, "Hall of Fame ›")
    hall = wait_for("hall-of-fame", lambda r: "Hall of Fame" in texts(r) and "Return to season" in texts(r))
    assert any("No inductees" in (t or "") for t in texts(hall))
    adb("shell", "settings", "put", "system", "user_rotation", "0")
    time.sleep(2)
    assert "Hall of Fame" in texts(capture("hall-portrait"))
    tap(capture("hall-close"), "Return to season")
    print("Season-baseline roster, phase-aware recruiting, departures and Hall of Fame navigation passed.")
    # Theme changes rebuild view controls around the live unsaved state.
    tap(capture("before-theme"), "Settings")
    settings = wait_for("appearance-settings", lambda r: "Appearance" in texts(r) and "Dark" in texts(r))
    before_theme = calendar(settings)
    tap(settings, "Dark")
    choices = wait_for("theme-choices", lambda r: "Light" in texts(r))
    tap(choices, "Light")
    light = wait_for("light-settings-portrait", lambda r: "Light" in texts(r) and "Appearance" in texts(r))
    assert calendar(light) == before_theme, "Theme switch changed unsaved career/calendar"
    tap(light, "Overview")
    light_overview = wait_for("light-overview", lambda r: overview_ready(r))
    assert enabled(light_overview, advance_action(light_overview)), "Theme rebuild disabled dynasty controls"
    tap(light_overview, "Roster")
    roster = wait_for("light-roster-portrait", lambda r: "Morale" in texts(r))
    player_button = next(n for n in roster.iter("node") if (n.get("content-desc") or "").startswith("View player "))
    tap(roster, player_button.get("text"))
    profile = wait_for("light-player-profile-portrait", lambda r: any((t or "").startswith("Morale · ") for t in texts(r)))
    assert any("Season OVR" in (t or "") for t in texts(profile))
    adb("shell", "settings", "put", "system", "user_rotation", "1")
    time.sleep(2)
    profile = capture("light-player-profile-landscape")
    assert any((t or "").startswith("Morale · ") for t in texts(profile))
    adb("shell", "input", "keyevent", "4")
    time.sleep(1.5)
    tap(capture("light-after-profile"), "Settings")
    light_settings = wait_for("light-settings-landscape", lambda r: "Light" in texts(r))
    tap(light_settings, "Light")
    dark_choice = wait_for("theme-dark-choice", lambda r: "Dark" in texts(r))
    tap(dark_choice, "Dark")
    dark = wait_for("dark-restored", lambda r: "Dark" in texts(r) and "Appearance" in texts(r))
    assert calendar(dark) == before_theme
    print("Light/dark theme, live-state preservation, enabled controls and player-profile morale passed in both layouts.")
    # A system appearance change during processing must not clone the pre-advance state.
    tap(dark,"Dark")
    choices=wait_for("busy-theme-system-choice",lambda r:"System" in texts(r))
    tap(choices,"System")
    system=wait_for("busy-theme-system",lambda r:"System" in texts(r) and "Appearance" in texts(r))
    tap(system,"Overview")
    ready=wait_for("busy-theme-before-advance",overview_ready)
    previous=calendar(ready)
    action=advance_action(ready)
    node=next(n for n in ready.iter("node") if n.get("text")==action)
    left,top,right,bottom=map(int,re.findall(r"\d+",node.get("bounds")))
    adb("shell","input","tap",str((left+right)//2),str((top+bottom)//2))
    adb("shell","cmd","uimode","night","yes")
    advanced=wait_for("busy-theme-advanced",lambda r:overview_ready(r,previous),timeout=300)
    after=calendar(advanced)
    adb("shell","cmd","uimode","night","no")
    time.sleep(2)
    stable=wait_for("busy-theme-light-stable",lambda r:overview_ready(r) and calendar(r)==after)
    assert adb("shell","pidof","antdroid.cfbcoach.dev").strip()
    print("System appearance refresh preserved the completed week while processing was active.")


def check_remaining_management():
    tap(capture("remaining-before-program"), "Program & staff")
    program=wait_for("management-program-career",lambda r:"Staff & hiring" in texts(r))
    tap(program,"Staff & hiring")
    staff=wait_for("management-program-staff",lambda r:any((n.get("content-desc") or "").startswith("View coach ") for n in r.iter("node")))
    coach=next(n for n in staff.iter("node") if (n.get("content-desc") or "").startswith("View coach "))
    tap(staff,coach.get("text"))
    profile=wait_for("management-coach-profile",lambda r:"Career history" in texts(r))
    tap(profile,"Attributes")
    wait_for("management-coach-attributes",lambda r:"Coaching traits" in texts(r))
    adb("shell","input","keyevent","4")
    staff=scroll_to_text("management-staff-candidates-button","Candidates")
    tap(staff,"Candidates")
    market=wait_for("management-staff-market",lambda r:"Find a coach" in texts(r))
    assert "Fit" in texts(market) and "Review hire" in texts(market)
    tap(market,"Cancel")
    program=wait_for("management-staff-cancel",lambda r:"History & titles" in texts(r))
    tap(program,"History & titles")
    wait_for("management-program-history",lambda r:any(t and "National titles:" in t for t in texts(r)))
    assert "Development" not in texts(capture("program-without-development")), "Development must move out of Program & staff"
    tap(capture("management-before-coach"), "Coach")
    coach_hub = wait_for("coach-hub", lambda r: "Game plan" in texts(r) and "Development" in texts(r))
    tap(coach_hub, "Development")
    wait_for("management-coach-development",lambda r:"Availability" in texts(r) and "Morale" in texts(r))
    tap(capture("management-before-news"),"News")
    news=wait_for("management-news-cards",lambda r:"Read story ›" in texts(r))
    tap(news,"Read story ›")
    story=wait_for("management-news-story",lambda r:"‹ Back to headlines" in texts(r))
    assert any("Dynasty Wire" in (t or "") for t in texts(story))
    tap(story,"‹ Back to headlines")
    wait_for("management-news-return",lambda r:"News & headlines" in texts(r))
    tap(capture("management-before-schedule"),"Schedule")
    schedule=wait_for("management-schedule",lambda r:"Season schedule" in texts(r))
    tap(schedule,"Your team")
    options=wait_for("management-schedule-scope",lambda r:"All teams" in texts(r))
    assert any((n.get("resource-id") or "").endswith("game-dialog-surface") for n in options.iter("node")), "Picker must use themed choices"
    scroll = next(n for n in options.iter("node") if n.get("scrollable") == "true")
    left, top, right, bottom = map(int, re.findall(r"\d+", scroll.get("bounds")))
    first_choice = next(n for n in options.iter("node") if n.get("text") == "Your team")
    first_bounds = list(map(int, re.findall(r"\d+", first_choice.get("bounds"))))
    first_height = first_bounds[3] - first_bounds[1]
    for label in ("Your team", "All teams"):
        choice = next(n for n in options.iter("node") if n.get("text") == label)
        x1, y1, x2, y2 = map(int, re.findall(r"\d+", choice.get("bounds")))
        assert top <= y1 < y2 <= bottom and y2-y1 >= first_height - 2, "Short menu clipped a choice"
    tap(options,"All teams")
    schedule=wait_for("management-all-team-schedule",lambda r:"All teams" in texts(r) and "All weeks" not in texts(r))
    tap(schedule,"Standings & polls")
    rankings=wait_for("management-rankings",lambda r:"National rankings" in texts(r))
    tap(rankings,"Conference")
    conference=wait_for("management-conference",lambda r:"Conference race" in texts(r))
    tap(conference,"Postseason")
    wait_for("management-postseason",lambda r:"Playoff field, bracket results and bowl games" in texts(r))
    tap(capture("management-before-statistics"),"Statistics")
    statistics=wait_for("management-league-statistics",lambda r:"Player statistics" in texts(r))
    tap(statistics,"Passing")
    options=wait_for("management-stat-category",lambda r:"Defense" in texts(r))
    tap(options,"Defense")
    wait_for("management-defense-statistics",lambda r:any("PBU =" in (t or "") for t in texts(r)))



def check_management_layouts():
    # Use the same saved dynasty while resizing the Android window in dp.
    adb("shell", "settings", "put", "system", "user_rotation", "0")
    # MAUI caches its initial pixel conversion. Change window dimensions at the
    # startup density, as an actual resize/fold transition does; don't alter DPI.
    density=int(re.search(r"Physical density: (\d+)",adb("shell","wm","density").decode()).group(1))/160
    def resize(width,height):
        adb("shell","wm","size",f"{round(width*density)}x{round(height*density)}")
    resize(360,800)
    time.sleep(3)
    tap(capture("management-compact-settings"), "Overview")
    compact=wait_for("management-compact-dashboard", overview_ready)
    assert any((n.get("resource-id") or "").endswith("management-menu") for n in compact.iter("node"))
    menu=next(n for n in compact.iter("node") if (n.get("resource-id") or "").endswith("management-menu"))
    tap(compact,menu.get("text"))
    expanded=wait_for("management-compact-sidebar",lambda r:any((n.get("resource-id") or "").endswith("sidebar-identity") for n in r.iter("node")))
    context=calendar(compact)[0]
    before=next(n for n in compact.iter("node") if n.get("text")==context)
    after=next(n for n in expanded.iter("node") if n.get("text")==context)
    assert before.get("bounds")==after.get("bounds"), "Compact drawer squeezed the page header"
    badge=next(n for n in expanded.iter("node") if (n.get("resource-id") or "").endswith("sidebar-identity"))
    assert "Air Force" not in texts(badge), "Full team name returned to the narrow sidebar"
    tap(expanded,"Close menu")
    closed=wait_for("management-compact-drawer-closed",lambda r:"Close menu" not in texts(r))
    menu=next(n for n in closed.iter("node") if (n.get("resource-id") or "").endswith("management-menu"))
    tap(closed,menu.get("text"))
    expanded=wait_for("management-compact-drawer-reopened",lambda r:"Close menu" in texts(r))
    tap(expanded,"Roster")
    roster=wait_for("management-compact-roster",lambda r:"Columns: Essential" in texts(r) and "Morale" in texts(r))
    tap(roster,"Columns: Essential")
    all_columns=wait_for("management-roster-all-columns",lambda r:"Columns: All" in texts(r))
    tap(all_columns,"OVR")
    sorted_roster=wait_for("management-roster-sort-up",lambda r:"OVR ↑" in texts(r))
    tap(sorted_roster,"OVR ↑")
    sorted_roster=wait_for("management-roster-sort-down",lambda r:"OVR ↓" in texts(r))
    tap(sorted_roster,"Columns: All")
    roster=wait_for("management-roster-essential-restored",lambda r:"Columns: Essential" in texts(r))
    person=next(n for n in roster.iter("node") if (n.get("content-desc") or "").startswith("View player "))
    tap(roster,person.get("text"))
    profile=wait_for("management-compact-profile",lambda r:"Stats & history" in texts(r))
    tap(profile,"Attributes")
    attrs=wait_for("management-compact-attributes",lambda r:"Character traits" in texts(r))
    assert "Awareness" in texts(attrs)
    adb("shell","input","keyevent","4")
    tap(capture("management-after-profile"),"Recruit")
    recruits=wait_for("management-compact-recruiting",lambda r:"High School" in texts(r))
    tap(recruits,"High School")
    recruits=wait_for("management-recruit-table",lambda r:any((n.get("content-desc") or "").startswith("View recruit ") for n in r.iter("node")))
    recruit=next(n for n in recruits.iter("node") if (n.get("content-desc") or "").startswith("View recruit "))
    tap(recruits,recruit.get("text"))
    detail=wait_for("management-compact-recruit-profile",lambda r:"Back to prospects" in texts(r))
    tap(detail,"Recruitment")
    detail=wait_for("management-recruit-actions",lambda r:"Priority Scout" in texts(r) or "Priority On" in texts(r))
    assert "Offer Scholarship" not in texts(detail), "In-season actions leaked into recruit profile"
    tap(detail,"Back to prospects")
    resize(700,900)
    time.sleep(3)
    fold=capture("management-foldable-recruiting")
    assert any((n.get("resource-id") or "").endswith("nav-roster") for n in fold.iter("node")), "Foldable sidebar missing"
    tap(fold,"Roster")
    capture("management-foldable-roster")
    resize(1200,800)
    time.sleep(3)
    tablet=capture("management-tablet-roster")
    person=next(n for n in tablet.iter("node") if (n.get("content-desc") or "").startswith("View player "))
    tap(tablet,person.get("text"))
    split=wait_for("management-tablet-split-profile",lambda r:"Stats & history" in texts(r))
    assert any((n.get("resource-id") or "").endswith("nav-roster") for n in split.iter("node")), "Tablet profile became a full-screen modal"
    tap(split,"‹ Back")
    tap(capture("management-tablet-profile-closed"),"Recruit")
    recruits=wait_for("management-tablet-recruiting",lambda r:any((n.get("content-desc") or "").startswith("View recruit ") for n in r.iter("node")))
    recruit=next(n for n in recruits.iter("node") if (n.get("content-desc") or "").startswith("View recruit "))
    tap(recruits,recruit.get("text"))
    detail=wait_for("management-tablet-split-recruit",lambda r:"Back to prospects" in texts(r))
    assert any((n.get("resource-id") or "").endswith("nav-recruiting") for n in detail.iter("node"))
    tap(detail,"Back to prospects")
    check_remaining_management()
    tap(capture("management-tablet-before-settings"),"Settings")
    settings=wait_for("management-system-settings",lambda r:"Appearance" in texts(r))
    tap(settings,"Dark")
    options=wait_for("management-system-choice",lambda r:"System" in texts(r))
    tap(options,"System")
    system=wait_for("management-system-selected",lambda r:"System" in texts(r) and "Appearance" in texts(r))
    before=calendar(system)
    adb("shell","cmd","uimode","night","yes")
    time.sleep(3)
    dark=capture("management-system-dark")
    assert calendar(dark)==before
    adb("shell","cmd","uimode","night","no")
    time.sleep(3)
    light=capture("management-system-light")
    assert calendar(light)==before
    assert "System" in texts(light)
    tap(light,"News")
    light_news=wait_for("management-light-news",lambda r:"Read story ›" in texts(r))
    tap(light_news,"Program & staff")
    light_program=wait_for("management-light-program",lambda r:"Staff & hiring" in texts(r))
    tap(light_program,"Staff & hiring")
    wait_for("management-light-staff",lambda r:any((n.get("content-desc") or "").startswith("View coach ") for n in r.iter("node")))
    tap(capture("management-light-staff-before-settings"),"Settings")
    light=wait_for("management-light-settings-return",lambda r:"Appearance" in texts(r))
    tap(light,"System")
    choices=wait_for("management-restore-dark-choice",lambda r:"Dark" in texts(r))
    tap(choices,"Dark")
    dark=wait_for("management-restore-dark",lambda r:"Dark" in texts(r) and "Appearance" in texts(r))
    adb("shell","wm","size","reset")
    adb("shell","wm","density","reset")
    time.sleep(2)
    tap(capture("management-layout-reset"),"Overview")
    print("Management compact/foldable/tablet, profile tabs, recruit actions, split panes and System theme passed.")
    return wait_for("management-returned-overview",overview_ready)



if __name__ == "__main__":
    try:
        main()
    finally:
        Path("android-logcat.txt").write_bytes(adb("logcat", "-d"))
