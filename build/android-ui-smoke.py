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
        left, top, right, bottom = map(int, re.findall(r"\d+", scrollables[-1].get("bounds")))
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
    tap(overview, "Play game")
    viewer = wait_for("game-day-portrait", lambda r: "Call play" in texts(r) or "Call defense" in texts(r))
    assert {"DOWN", "YARDS TO GO", "BALL ON"} <= texts(viewer)
    # Swap the renderer on an existing field and restore it without advancing the snap.
    sprites = scroll_to_text("sprites-ready", "Players: Sprites")
    tap(sprites, "Players: Sprites")
    circles = capture("circle-renderer")
    assert "Players: Circles" in texts(circles)
    tap(circles, "Players: Circles")
    assert "Players: Sprites" in texts(capture("sprite-renderer"))
    adb("shell", "settings", "put", "system", "accelerometer_rotation", "0")
    adb("shell", "settings", "put", "system", "user_rotation", "1")
    time.sleep(3)
    landscape = capture("game-day-landscape")
    assert {"DOWN", "YARDS TO GO", "BALL ON"} <= texts(landscape)
    assert "Call play" in texts(landscape) or "Call defense" in texts(landscape)
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
    # Close the box, then leave the incomplete watched game. Advance the actual league
    # calendar as well, so a repeated numeric zero cannot silently pass this smoke test.
    adb("shell", "input", "keyevent", "4")
    # The box closes asynchronously through MAUI modal navigation. A second Back
    # during that transition can be discarded by Android, leaving Watch running.
    # Match normal touch sequencing instead of issuing overlapping navigation.
    time.sleep(1.5)
    adb("shell", "input", "keyevent", "4")
    overview = wait_for("after-live-view", lambda r: overview_ready(r), timeout=300)
    previous = calendar(overview)
    tap(overview, advance_action(overview))
    advanced = wait_for("calendar-advanced", lambda r: overview_ready(r, previous), timeout=300)
    assert calendar(advanced) != previous
    assert "Advance to Week 1" not in texts(advanced), "The calendar remained at Week 0"
    print("Actual week advancement after live-view exit passed.")


if __name__ == "__main__":
    main()
