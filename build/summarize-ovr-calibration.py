"""Render the deterministic test evidence into the release's comparison report."""
import argparse
import json
from pathlib import Path


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("evidence", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--run-url", required=True)
    args = parser.parse_args()
    starting = json.loads((args.evidence / "starting-roster.json").read_text(encoding="utf-8"))
    labels = ["QB", "HB", "FB", "WR", "TE", "LT", "LG", "C", "RG", "RT", "LE", "RE", "DT", "LOLB", "MLB", "ROLB", "CB", "FS", "SS", "K", "P"]
    before, after = starting["before"], starting["after"]
    lines = ["# NEXT26 overall calibration validation", "", f"[Validated build and evidence]({args.run_url}).", "",
             "## Starting roster", "",
             "The same 11,560 NEXT26 players are compared, including the previously applied global attribute corrections. Before uses Build 289's POCI normalization; after uses fixed position curves with unchanged POCI attribute weights. No EA overall overrides or annual percentile normalization are used.", "",
             "| Measure | Before | After |", "|---|---:|---:|"]
    for field, label in [("Average", "League average"), ("Median", "Median"), ("StarterAverage", "Starter average"), ("Percent80", "80+ (%)"), ("Percent85", "85+ (%)"), ("Percent90", "90+ (%)"), ("Percent95", "95+ (%)"), ("Percent99", "99 (%)"), ("FreshmanAverage", "Freshman average"), ("SeniorAverage", "Senior average")]:
        lines.append(f"| {label} | {before[field]:.2f} | {after[field]:.2f} |")
    lines += ["", "## All 21 positions", "", "| Position (PPOS) | Players | Mean before | Mean after | Starter before | Starter after | 90+ before (%) | 90+ after (%) |", "|---|---:|---:|---:|---:|---:|---:|---:|"]
    assert len(before["Positions"]) == len(after["Positions"]) == 21
    for b, a in zip(before["Positions"], after["Positions"]):
        assert b["Code"] == a["Code"]
        p = a["Code"]
        lines.append(f"| {labels[p]} ({p}) | {a['Count']} | {b['Average']:.2f} | {a['Average']:.2f} | {b['StarterAverage']:.2f} | {a['StarterAverage']:.2f} | {b['Percent90']:.2f} | {a['Percent90']:.2f} |")
    lines += ["", "Position starters are the top player per school and NCAA position code, except WR/CB use three and DT two. This includes FB and punter specialists. League starter averages use the game's depth-chart starter rules, so the two summaries have different denominators.", "",
              "## Twenty-season FBS lifecycle", "", "| Seed | Season | Mean | Median | Starters | 80+ % | 85+ % | 90+ % | 95+ % | 99 % | FR mean | SR mean | Elite programs | Weaker programs |", "|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|"]
    for seed in [1, 17]:
        snapshots = json.loads((args.evidence / f"dynasty-seed-{seed}.json").read_text(encoding="utf-8"))
        assert snapshots[-1]["season"] == 20, "Incomplete career evidence"
        for snapshot in snapshots:
            if snapshot["season"] not in [1, 5, 10, 20]:
                continue
            m = snapshot["metrics"]
            fields = ["Average", "Median", "StarterAverage", "Percent80", "Percent85", "Percent90", "Percent95", "Percent99", "FreshmanAverage", "SeniorAverage", "EliteProgramAverage", "WeakProgramAverage"]
            lines.append(f"| {seed} | {snapshot['season']} | " + " | ".join(f"{m[f]:.2f}" for f in fields) + " |")
    lines += ["", "## Method and limits", "",
              "- Fixed curves interpolate the supplied nine anchors per position. Validation required a two-point body lift, tapering to zero toward 99, and explicit low/high endpoints. A 99 requires a near-perfect weighted score of at least 98.5; there is no required number of elite players.",
              "- Generated athletes fit underlying attributes to the shared scale. HS profiles persist through signing; transfers retain attributes. Stars influence overlapping potential ranges, with no fixed current OVR. Walk-on/replacement targets rose two points, and initial filler prestige scaling was moderated.",
              "- Development remains attribute-based and staff/morale/injury aware. CPU depth is reassessed each offseason; human manual depth remains intact. Save-scale migration preserves attributes and remaining potential headroom and restarts the current display baseline once. Historical snapshots retain their recorded values.",
              "- The FBS lifecycle tests use 138 schools, the bundled 136-school roster plus two generated schools, actual portal/recruitment/development/walk-on services, and two deterministic seeds. They isolate roster balance without playing full national game schedules. Prestige strata remain fixed in this test to make elite-versus-weaker comparisons interpretable.",
              "- Separate 12-team career tests play complete schedules, record stats/injuries, evolve prestige/staff, and save/reload through 20 seasons. These complement rather than replace the FBS lifecycle measurement. Long-term distributions are test tolerances, not production quotas.",
              "- JSON evidence includes every annual checkpoint, all positional threshold distributions, freshman/senior distributions, recruiting-star populations and recent development stages for retained players. Development averages have survivor bias; this is not a causal estimate of star rating or staff quality.",
              "- Two seeds provide a reproducible balance check, not a guarantee for every custom league, import or user strategy. Optional realignment and extreme custom rosters can change league distributions.", ""]
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text("\n".join(lines), encoding="utf-8")


if __name__ == "__main__":
    main()
