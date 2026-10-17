"""Check the launch/create/cancel flow on an already running disposable emulator."""
import re
import subprocess
import time
import xml.etree.ElementTree as ET
from pathlib import Path


def adb(*args):
    return subprocess.check_output(["adb", *args], stderr=subprocess.STDOUT)


def capture(name):
    adb("shell", "uiautomator", "dump", "/sdcard/dynasty-ui.xml")
    xml = adb("exec-out", "cat", "/sdcard/dynasty-ui.xml")
    Path(f"android-{name}.xml").write_bytes(xml)
    Path(f"android-{name}.png").write_bytes(adb("exec-out", "screencap", "-p"))
    return ET.fromstring(xml)


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
