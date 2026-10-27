"""Read-only cross-check of normalized 2026 budgets against the observed public NFLPA report."""
import csv
import hashlib
import json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
data=root/"docs/data"
meta=json.loads((data/"nflpa-cap-observation-2026-10-26.json").read_text(encoding="utf-8"))
observed=list(csv.DictReader((data/"nflpa-cap-observation-2026-10-26.csv").open(encoding="utf-8")))
normalized=list(csv.DictReader((root/"src/ProFootball.Core/nfl_2026_salary_caps.csv").open(encoding="utf-8")))
legacy=root/"src/ProFootball.Core/nfl_2026_salary_caps.user-original.csv"
assert hashlib.sha256(legacy.read_bytes()).hexdigest()==meta["original_user_csv_sha256"],"Original supplied data changed"
assert meta["base_cap"]==301_200_000 and meta["report_year"]==2026
assert len(observed)==len(normalized)==32
source={r["team_abbr"]:r for r in observed};current={r["team_abbr"]:r for r in normalized}
assert len(source)==len(current)==32 and source.keys()==current.keys()
for abbreviation,r in source.items():
    n=current[abbreviation];carry=int(r["carryover"])
    a=int(r["series_a_team_cap"])+int(r["series_a_room"])
    b=int(r["series_b_team_cap"])+int(r["series_b_room"])
    assert a==b==int(n["adjusted_cap_2026"]),abbreviation+" budget/room confused or report pair differs"
    assert n["team_name"]==r["team_name"] and int(n["base_cap_2026"])==301_200_000
    assert int(n["rollover_from_2025"])==carry
    assert int(n["other_adjustments"])==a-301_200_000-carry
row_totals={"carryover":sum(int(r["carryover"])*2 for r in observed),"team_cap":sum(int(r["series_a_team_cap"])+int(r["series_b_team_cap"]) for r in observed),"room":sum(int(r["series_a_room"])+int(r["series_b_room"]) for r in observed)}
assert row_totals==meta["row_totals"]
assert row_totals["carryover"]==meta["report_totals"]["carryover"]
assert row_totals["team_cap"]+row_totals["room"]==meta["report_totals"]["team_cap"]+meta["report_totals"]["room"]
print("PASS: 32 distinct NFLPA report pairs reconcile to normalized budgets; original preserved; room is evidence, not an opening-balance input.")
