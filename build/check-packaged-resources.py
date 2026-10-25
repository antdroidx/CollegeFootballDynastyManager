"""Fail before workload installation when an explicit MAUI asset is missing."""
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
project = root / "src/DynastyManager.App/DynastyManager.App.csproj"
missing = []
for item in ET.parse(project).getroot().iter():
    if item.tag not in {"MauiIcon", "MauiSplashScreen", "MauiAsset"}:
        continue
    for attribute in ("Include", "ForegroundFile"):
        value = item.get(attribute)
        if not value or any(marker in value for marker in ("$(", "*", "?")):
            continue
        path = project.parent / value.replace("\\", "/")
        if not path.is_file():
            missing.append(f"{item.tag} {attribute}: {value}")
if missing:
    print("Missing packaged resources:\n" + "\n".join(missing), file=sys.stderr)
    sys.exit(1)
print("All explicit MAUI icon, splash, and asset files exist.")
