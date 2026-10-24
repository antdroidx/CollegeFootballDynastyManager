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
    viewer = wait_for("game-day-portrait", lambda r: "Call play" in texts(r) or "Call defense" in texts(r))
    badges = [n for n in viewer.iter("node") if n.get("content-desc", "").endswith(" team badge")]
    assert len(badges) == 2, "Compact scoreboard must expose both team badges"
    settings_clock = next(t for t in texts(viewer) if t and re.match(r"^Q\d+\s+\d+:\d+$", t))
    for current, expected in (("Full field", "Broadcast"), ("Broadcast", "Follow ball"), ("Follow ball", "Full field")):
        tap(scroll_to_text("camera-ready", "Camera: " + current), "Camera: " + current)
        camera_view = capture("camera-" + expected.replace(" ", "-"))
        assert "Camera: " + expected in texts(camera_view)
        assert settings_clock in texts(camera_view), "Camera changes advanced the game clock"
    tap(scroll_to_text("replay-speed-ready", "Replay speed: 1x"), "Replay speed: 1x")
    slow = capture("replay-slow-speed")
    assert "Replay speed: 0.5x" in texts(slow)
    assert settings_clock in texts(slow), "Replay speed changes advanced the game clock"
    tap(slow, "Replay speed: 0.5x")
    viewer = capture("presentation-settings-restored")
    opening_touchback = any("TOUCHBACK" in (value or "") for value in texts(viewer))
    assert {"DOWN", "YARDS TO GO", "BALL ON"} <= texts(viewer)
    assert len([n for n in viewer.iter("node") if n.get("content-desc", "").startswith("Select ")]) == 3, "Portrait must show three play choices"
    tap(viewer, "More")
    assert "Player names: On" in texts(capture("names-default-on")), "Subtle names must default on"
    # Swap the renderer on an existing field and restore it without advancing the snap.
    sprites = scroll_to_text("sprites-ready", "Players: Sprites")
    tap(sprites, "Players: Sprites")
    circles = capture("circle-renderer")
    assert "Players: Circles" in texts(circles)
    tap(circles, "Players: Circles")
    assert "Players: Sprites" in texts(capture("sprite-renderer"))
    tap(capture("portrait-utilities"), "Less")
    adb("shell", "settings", "put", "system", "accelerometer_rotation", "0")
    adb("shell", "settings", "put", "system", "user_rotation", "1")
    time.sleep(3)
    landscape = capture("game-day-landscape")
    assert {"DOWN", "YARDS TO GO", "BALL ON"} <= texts(landscape)
    assert "Call play" in texts(landscape) or "Call defense" in texts(landscape)
    field_node = next(n for n in landscape.iter("node") if (n.get("resource-id") or "").endswith("game-day-field"))
    dock_node = next(n for n in landscape.iter("node") if (n.get("resource-id") or "").endswith("game-day-controls"))
    field_bounds = list(map(int, re.findall(r"\d+", field_node.get("bounds"))))
    dock_bounds = list(map(int, re.findall(r"\d+", dock_node.get("bounds"))))
    assert field_bounds[3] <= dock_bounds[1], "Controls overlap the live field"
    screen = next(landscape.iter("node"))
    screen_left, screen_top, screen_right, screen_bottom = map(int, re.findall(r"\d+", screen.get("bounds")))
    assert screen_right - screen_left > screen_bottom - screen_top, "Viewer did not rotate to landscape"
    for label in ("Call play", "Call defense", "Punt", "Field goal", "Skip to end"):
        for node in landscape.iter("node"):
            if node.get("text") == label:
                left, top, right, bottom = map(int, re.findall(r"\d+", node.get("bounds")))
                assert screen_top <= top < bottom <= screen_bottom, f"Landscape control is off-screen: {label}"
    # The hybrid tray exposes the full call sheet over the existing field. Merely
    # choosing/canceling a play or filtering formations must not advance the game.
    before_picker_clock = next(t for t in texts(landscape) if t and re.match(r"^Q\d+\s+\d+:\d+$", t))
    choose_label = "Choose play" if "Choose play" in texts(landscape) else "Choose defense"
    tap(landscape, choose_label)
    picker = wait_for("expanded-play-picker", lambda r: "Close playbook" in texts(r))
    assert "All formations" in texts(picker)
    assert any(n.get("scrollable") == "true" for n in picker.iter("node")), "Play library must scroll"
    adb("shell", "input", "keyevent", "4")
    collapsed = wait_for("play-picker-back", lambda r: "Close playbook" not in texts(r) and "BALL ON" in texts(r))
    assert before_picker_clock in texts(collapsed), "Browsing the playbook advanced the clock"
    # Select a real card in the expanded library and verify it only changes the
    # pending call, rather than running a snap or consuming time.
    tap(collapsed, choose_label)
    selection = capture("play-picker-selection")
    card_node = next(n for n in selection.iter("node") if n.get("content-desc", "").startswith("Select "))
    left, top, right, bottom = map(int, re.findall(r"\d+", card_node.get("bounds")))
    adb("shell", "input", "tap", str((left + right)//2), str((top + bottom)//2))
    collapsed = wait_for("play-selected", lambda r: "Close playbook" not in texts(r) and choose_label in texts(r))
    assert before_picker_clock in texts(collapsed), "Selecting a play advanced the game clock"
    assert any("SELECTED" in (value or "") for value in texts(collapsed))
    tap(collapsed, "More")
    utilities = capture("game-day-more")
    assert {"Players: Sprites", "Offense: You", "Defense: You"} <= texts(utilities)
    assert {"Markers: Positions", "Markers: Numbers"} & texts(utilities)
    tap(utilities, "Less")
    landscape = capture("game-day-tray-restored")
    assert "Players: Sprites" not in texts(landscape), "Utility controls should collapse during play selection"
    assert choose_label in texts(landscape)
    print("Hybrid play overlay, Android Back, compact utility menu and paused-clock checks passed.")
    assert any((value or "").startswith("TIMEOUTS  ") for value in texts(landscape)), "Both timeout banks must be visible"
    tap(landscape, "Box score")
    replay_before = wait_for("replay-stats-before", lambda r: "Live box score" in texts(r))
    replay_stats = texts(replay_before)
    tap(replay_before, "Return to game")
    replay_ready = wait_for("manual-replay-ready", lambda r: "Replay last" in texts(r))
    replay_clock = next(t for t in texts(replay_ready) if t and re.match(r"^Q\d+\s+\d+:\d+$", t))
    tap(replay_ready, "Replay last")
    Path("android-instant-replay.png").write_bytes(adb("exec-out", "screencap", "-p"))
    replay_returned = wait_for("manual-replay-returned", lambda r: "Replay last" in texts(r))
    assert replay_clock in texts(replay_returned), "Replay consumed game time"
    assert any("SELECTED" in (value or "") for value in texts(replay_returned)), "Replay reset the pending call"
    tap(replay_returned, "Box score")
    replay_after = wait_for("replay-stats-after", lambda r: "Live box score" in texts(r))
    assert texts(replay_after) == replay_stats, "Replay changed scores or statistics"
    tap(replay_after, "Return to game")
    landscape = wait_for("replay-complete", lambda r: "Replay last" in texts(r))
    print("Both timeout banks and manual replay without clock/stat/call changes passed.")
    # Inspect in-progress totals without finishing the match or advancing the play clock.
    before_clock = next(t for t in texts(landscape) if t and re.match(r"^Q\d+\s+\d+:\d+$", t))
    tap(landscape, "Box score")
    box = wait_for("live-box-score", lambda r: "Live box score" in texts(r) and "Return to game" in texts(r))
    assert {"Team totals", "Player statistics", "Total yards"} <= texts(box)
    tap(box, "Player statistics")
    players = capture("live-box-players")
    assert "Passing" in texts(players)
    tap(players, "Passing")
    tap(scroll_to_text("live-box-category", "Returns"), "Returns")
    returns = capture("live-box-returns")
    if opening_touchback:
        assert "No recorded statistics in this category yet." in texts(returns), "A touchback must not create a kick-return statistic"
    else:
        assert {"KR", "KR yds"} <= texts(returns), "The opening kick return must appear in the live box score"
    tap(returns, "Return to game")
    returned = wait_for("live-box-returned", lambda r: "BALL ON" in texts(r))
    assert before_clock in texts(returned), "Reading the box score advanced a paused game"
    if "Call play" in texts(returned):
        tap(returned, "More")
        special = capture("timeout-clock-play")
        tap(special, "Kneel")
        running = wait_for("timeout-available", lambda r: enabled(r, "Timeout (3)"))
        timeout_clock = next(t for t in texts(running) if t and re.match(r"^Q\d+\s+\d+:\d+$", t))
        tap(running, "Timeout (3)")
        Path("android-timeout-banner.png").write_bytes(adb("exec-out", "screencap", "-p"))
        stopped = wait_for("timeout-called", lambda r: "Timeout (2)" in texts(r))
        assert timeout_clock in texts(stopped), "A timeout consumed game time"
        assert any("(2)" in (value or "") and (value or "").startswith("TIMEOUTS  ") for value in texts(stopped))
        print("User timeout notification and both-team remaining counters passed.")
    # Rotate back as well: the same field/cards must safely reparent in both directions.
    adb("shell", "settings", "put", "system", "user_rotation", "0")
    time.sleep(2)
    assert "BALL ON" in texts(capture("game-day-portrait-restored"))
    assert adb("shell", "pidof", "antdroid.cfbcoach.dev").strip()
    print("Current-week game entry, live box-score filters and portrait/landscape rotation passed.")

    # The header button stays in the same place when switching to Watch; wait for the
    # active snap to finish before showing stats, then ensure Watch resumes on return.
    adb("shell", "settings", "put", "system", "user_rotation", "1")
    time.sleep(2)
    ready = capture("watch-ready")
    tap(ready, "Watch CPU")
    tap(ready, "Box score")
    paused = wait_for("watch-box-paused", lambda r: "Live box score" in texts(r))
    snapshot = texts(paused)
    time.sleep(2)
    assert texts(capture("watch-box-still-paused")) == snapshot
    tap(paused, "Return to game")
    tap(ready, "Box score")
    resumed = wait_for("watch-box-resumed", lambda r: "Live box score" in texts(r))
    assert texts(resumed) != snapshot, "Watch did not advance after returning from the box score"
    assert adb("shell", "pidof", "antdroid.cfbcoach.dev").strip()
    print("Watch mode box-score pause/resume passed.")
    # Finish the Watch pause/resume check, then reopen the uncommitted game in Play mode.
    # UIAutomator waits for idle, so a continuously animated Watch game is unsuitable
    # for capturing confirmation controls. Never reuse Play-mode bounds after mode changes.
    adb("shell", "input", "keyevent", "4")
    time.sleep(1)
    adb("shell", "input", "keyevent", "4")
    overview=wait_for("skip-reopen-overview",overview_ready,timeout=300)
    tap(overview,"Play game")
    toss=wait_for("skip-reopen-toss",lambda r:"Heads" in texts(r) or "Receive" in texts(r) or "Start game" in texts(r))
    if "Heads" in texts(toss):
        tap(toss,"Heads")
        toss=wait_for("skip-reopen-result",lambda r:"Receive" in texts(r) or "Start game" in texts(r))
    if "Receive" in texts(toss):
        tap(toss,"Receive")
        toss=wait_for("skip-reopen-ready",lambda r:"Start game" in texts(r))
    tap(toss,"Start game")
    watch_view = wait_for("skip-play-ready", lambda r: enabled(r, "Skip to end"))
    tap(watch_view, "Skip to end")
    confirmation = wait_for("skip-confirm-cancel", lambda r: "Skip to end?" in texts(r), timeout=300)
    cancel_label = next(t for t in texts(confirmation) if (t or "").casefold() == "keep playing")
    tap(confirmation, cancel_label)
    cancelled = wait_for("skip-cancelled", lambda r: enabled(r, "Skip to end") and "BALL ON" in texts(r))
    assert "Close" not in texts(cancelled), "Cancelling Skip unexpectedly finished the game"
    tap(cancelled, "Skip to end")
    confirmation = wait_for("skip-confirm-finish", lambda r: "Skip to end?" in texts(r), timeout=300)
    accept_label = next(t for t in texts(confirmation) if (t or "").casefold() == "simulate remainder")
    tap(confirmation, accept_label)
    final = wait_for("skip-final-saved", lambda r: enabled(r, "Close") and "BALL ON" in texts(r), timeout=300)
    assert not enabled(final, "Skip to end")
    tap(final, "Box score")
    final_box = wait_for("skip-final-box", lambda r: "Final statistics" in texts(r))
    tap(final_box, "Return to game")
    final = wait_for("skip-final-close", lambda r: enabled(r, "Close"))
    tap(final, "Close")
    overview = wait_for("after-live-view", lambda r: overview_ready(r), timeout=300)
    assert adb("shell", "pidof", "antdroid.cfbcoach.dev").strip(), "Closing the saved final crashed the app"
    assert "Play game" not in texts(overview), "Saved game was not marked completed"
    print("Skip cancellation, confirmed simulation, saved final box score and Close without crash passed.")
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
    tap(capture("management-before-development"),"Development")
    wait_for("management-program-development",lambda r:"Availability" in texts(r) and "Morale" in texts(r))
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
