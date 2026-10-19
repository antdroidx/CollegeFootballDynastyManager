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
        if "Quickstep isn't responding" not in texts(root):
            return root
        # Only dismiss the disposable emulator's launcher ANR; never mask an app ANR.
        tap(root, "Close app")
    raise AssertionError(f"Could not capture a fresh, unobstructed UI for {name}")


def texts(root):
    return {node.get("text") for node in root.iter("node")}


def tap(root, text):
    node = next(node for node in root.iter("node") if node.get("text") == text)
    left, top, right, bottom = map(int, re.findall(r"\d+", node.get("bounds")))
    assert right > left and bottom > top, f"Target is not visible: {text}"
    adb("shell", "input", "tap", str((left + right) // 2), str((top + bottom) // 2))
    time.sleep(1)


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
    values = texts(root)
    header = next((value for value in values if value and re.search(r" · \d{4} · Week \d+$", value)), None)
    phase = next((value for value in values if value and value.endswith((" · Preseason", " · Regular season"))), None)
    return (header, phase)


def overview_ready(root, previous=None):
    state = calendar(root)
    busy = any(node.get("class") == "android.widget.ProgressBar" for node in root.iter("node"))
    return (enabled(root, "Advance Week") and state[0] is not None and state[1] is not None
            and not busy and (previous is None or state != previous))


def main():
    home = capture("launch")
    assert {"Your dynasties", "New dynasty"} <= texts(home), "Launch must open the save library"
    assert not {"Advance Week", "Overview", "Roster", "Coach", "Recruit", "Season", "Settings"} & texts(home), "Career controls leaked onto Home"
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
    for week in range(4):
        if "Play game" in texts(overview):
            break
        previous = calendar(overview)
        tap(overview, "Advance Week")
        overview = wait_for(f"overview-week-{week}", lambda r: overview_ready(r, previous), timeout=300)
    assert "Play game" in texts(overview), "Expected a current-week user matchup"
    tap(overview, "Play game")
    viewer = wait_for("game-day-portrait", lambda r: "Call play" in texts(r) or "Call defense" in texts(r))
    assert {"DOWN", "YARDS TO GO", "BALL ON"} <= texts(viewer)
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
    # Rotate back as well: the same field/cards must safely reparent in both directions.
    adb("shell", "settings", "put", "system", "user_rotation", "0")
    time.sleep(2)
    assert "BALL ON" in texts(capture("game-day-portrait-restored"))
    assert adb("shell", "pidof", "antdroid.cfbcoach.dev").strip()
    print("Current-week game entry and portrait/landscape rotation passed.")


if __name__ == "__main__":
    main()
