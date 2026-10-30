# Pro pacing adjustment — October 29, 2026

Normal Pro pre-snap pacing now uses 36–39 seconds instead of the shared 31–37. Both fit the existing 40-second clock; 25-second administrative resets still cap the wait at 24. Hurry-up (12–20), chew-clock (37–39), spike/quick-shot and stopped-clock behavior are unchanged. College retains 31–37. Values live in league rules, not app UI or contract/save logic.

256 games per Pro mode/treatment, identical matchup/seed schedule and audit configuration. Baseline uses the same current engine with only the old pacing values restored via the developer audit's optional fourth argument `baseline-pacing`. This is not an app option. Reports are under docs/data/pro-pacing-2026-10-29. These are sampled games, not a new multi-season audit.

| Mode | Baseline plays/team/game | Updated | Updated points | Updated yards |
|---|---:|---:|---:|---:|
| Real | 66.32 | 63.16 | 22.59 | 339.46 |
| Generated | 66.96 | 62.82 | 21.27 | 326.55 |

Offensive plays exclude kicking plays and include sacks; Pro box-score pass attempts + rush attempts + sacks reconcile. All sampled personnel remain complete/unique. No output is clipped to a target. College control: 128 games, 69.27 plays/team/game; unchanged default parameters and College regression tests are the preservation evidence, not identical fresh-world output. Prior three-season College means remain 67.99 real / 67.40 generated.

NFL reference: totals across all 32 teams from NFL.com passing/rushing tables, divided by 544 team-games. 2024: 17,811 pass attempts + 14,687 rush attempts + 1,314 sacks = 62.15 plays/team/game. 2025: 17,439 + 14,601 + 1,287 = 61.26. Sources: https://www.nfl.com/stats/team-stats/offense/passing/2024/reg/all ; https://www.nfl.com/stats/team-stats/offense/rushing/2024/reg/all ; https://www.nfl.com/stats/team-stats/offense/passing/2025/reg/all ; https://www.nfl.com/stats/team-stats/offense/rushing/2025/reg/all . This tunes league means, not every team's style. Out-of-bounds timing remains simplified and is not implemented by this patch.

160 focused combined Core/College clock/GameDay/rules cases pass locally. Both Android packaging/regression runs pending; prior prerelease APKs do not include this change.
