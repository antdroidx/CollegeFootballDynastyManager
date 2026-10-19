import importlib.util
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET

spec = importlib.util.spec_from_file_location("ui_smoke", Path(__file__).with_name("android-ui-smoke.py"))
smoke = importlib.util.module_from_spec(spec)
spec.loader.exec_module(smoke)


def overview(week=0, phase="Regular season", busy=False, enabled=True):
    root = ET.Element("hierarchy")
    for text in (f"Air Force · 2026 · Week {week}", f"My Dynasty · 2026 · {phase}"):
        ET.SubElement(root, "node", text=text)
    ET.SubElement(root, "node", text="Advance Week", enabled=str(enabled).lower())
    if busy:
        ET.SubElement(root, "node", attrib={"class": "android.widget.ProgressBar"})
    return root


class SmokeSynchronizationTests(unittest.TestCase):
    def test_enabled_button_does_not_imply_an_advance_finished(self):
        old = overview()
        self.assertFalse(smoke.overview_ready(overview(), smoke.calendar(old)))

    def test_calendar_change_is_not_ready_while_overlay_is_visible(self):
        self.assertFalse(smoke.overview_ready(overview(week=1, busy=True), smoke.calendar(overview())))
        self.assertFalse(smoke.overview_ready(overview(week=1, enabled=False), smoke.calendar(overview())))
        self.assertTrue(smoke.overview_ready(overview(week=1), smoke.calendar(overview())))

    def test_preseason_to_regular_season_can_keep_week_zero(self):
        self.assertTrue(smoke.overview_ready(overview(), smoke.calendar(overview(phase="Preseason"))))

    def test_calendar_uses_shell_header_before_old_news_dates(self):
        root = overview(week=1)
        ET.SubElement(root, "node", text="INJURIES · 2026 · Week 0")
        self.assertEqual("Air Force · 2026 · Week 1", smoke.calendar(root)[0])

    def test_failed_dump_never_reads_stale_xml(self):
        dumps = iter((b"ERROR: could not get idle state", b"UI hierchary dumped to: /sdcard/dynasty-ui.xml"))
        dump_count = 0

        def fake_adb(*args):
            nonlocal dump_count
            if "uiautomator" in args:
                dump_count += 1
                return next(dumps)
            if "cat" in args:
                self.assertEqual(dump_count, 2, "An unsuccessful dump must never reuse the previous file")
                return ET.tostring(overview(week=1))
            return b""

        original_cwd = Path.cwd()
        with tempfile.TemporaryDirectory() as temp, patch.object(smoke, "adb", side_effect=fake_adb), patch.object(smoke.time, "sleep"):
            try:
                os.chdir(temp)
                self.assertEqual(smoke.calendar(overview(week=1)), smoke.calendar(smoke.capture("fresh")))
            finally:
                os.chdir(original_cwd)


if __name__ == "__main__":
    unittest.main()
