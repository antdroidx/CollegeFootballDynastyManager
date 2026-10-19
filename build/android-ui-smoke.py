"""Check the launch/create/cancel flow on an already running disposable emulator."""
import re
import subprocess
import time
import xml.etree.ElementTree as ET
from pathlib import Path


def adb(*args):
    return subprocess.check_output(["adb", *args], stderr=subprocess.STDOUT)


def capture(name):
    for attempt in range(4):
        adb("shell", "uiautomator", "dump", "/sdcard/dynasty-ui.xml")
        xml = adb("exec-out", "cat", "/sdcard/dynasty-ui.xml")
        root = ET.fromstring(xml)
        Path(f"android-{name}.xml").write_bytes(xml)
        Path(f"android-{name}.png").write_bytes(adb("exec-out", "screencap", "-p"))
        if "Quickstep isn't responding" not in texts(root):
            return root
        # Only dismiss the disposable emulator's launcher ANR; never mask an app ANR.
        tap(root, "Close app")
    raise AssertionError("Emulator launcher repeatedly blocked the app")


def texts(root):
    return {node.get("text") for node in root.iter("node")}


def tap(root, text):
    node = next(node for node in root.iter("node") if node.get("text") == text)
    left, top, right, bottom = map(int, re.findall(r"\d+", node.get("bounds")))
    assert right > left and bottom > top, f"Target is not visible: {text}"
    adb("shell", "input", "tap", str((left + right) // 2), str((top + bottom) // 2))
    time.sleep(1)


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


# Exercise a real dynasty rather than a viewer-only fixture, including the week gate.
tap(capture("library"), "New dynasty")
tap(capture("create-form"), "Choose your team")
tap(capture("school-picker"), "Air Force")
tap(capture("create-ready"), "Start dynasty")
overview = wait_for("overview-preseason", lambda r: enabled(r, "Advance Week"))
for week in range(4):
    if "Play game" in texts(overview):
        break
    tap(overview, "Advance Week")
    overview = wait_for(f"overview-week-{week}", lambda r: enabled(r, "Advance Week"))
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
# Rotate back as well: the same field/cards must safely reparent in both directions.
adb("shell", "settings", "put", "system", "user_rotation", "0")
time.sleep(2)
assert "BALL ON" in texts(capture("game-day-portrait-restored"))
assert adb("shell", "pidof", "antdroid.cfbcoach.dev").strip()
print("Current-week game entry and portrait/landscape rotation passed.")
