# Football rotation and generated-roster balance audit — October 29, 2026

## Scope and method

Three completed regular seasons (2026–2028) each in Pro real/sourced-contract and fictional-player worlds, plus College real and fictional-player worlds: 6,600 regular-season games total. Pro runs normal 32-team seasons, playoffs and offseason/draft/roster/cap management with the user team delegated to CPU for unattended auditing. College uses all138 programs with synthetic balanced12-game round-robin schedules, normal weekly development/recruiting/portal processing and offseason roster lifecycle. It does not claim actual conference/FCS matchups, College bowl/CFP coverage, or a device-played multi-season run.

54 Pro and36 College complete GameDay sessions per world are independently sampled for personnel diagnostics (180 samples total); all remaining season games contribute statistics. College diagnostic sessions are separate deterministic replays before normal weekly simulation. Completed season reports include team-game scoring/yardage/completion/sack/turnover averages, leader totals, roster means and actual generated newcomers who played. Do not infer all-season personnel census from the sampled diagnostics.

| World | Regular games | Points/team/game range | Yards/team/game range | Completion range | Generated newcomers playing in2028 |
|---|---:|---:|---:|---:|---:|
| college-generated | 2484 | 25.5–25.8 | 377–387 | 61.1–62.3% | 4634 |
| college-real | 2484 | 23.8–24.6 | 363–376 | 59.5–60.4% | 5072 |
| pro-generated | 816 | 22.4–23.6 | 348–353 | 61.6–62.4% | 185 |
| pro-real | 816 | 23.8–25.0 | 360–363 | 62.4–63.6% | 309 |

## Findings and changes

- The old rotation only selected the next backup, effectively excluding RB3+ in ordinary one-back packages. The revised weighted reserve group considers quality, depth and rest; sampled third-and-later-back carry shares are about13–19%, with leading backs retaining most work. A larger quality gap reduces routine starter rest. DL reserve share is about37–38%.
- Healthy QB and offensive-line starters no longer get arbitrary rotation chances. All samples have100% primary-QB snap share. Manual coaching substitutions/second-unit choices and injuries remain supported.
- Thin personnel packages now fill with healthy same-unit alternatives, rather than fielding fewer than11. Kneel/spike events also record full personnel. Every diagnostic sample has0 short/duplicate lineups.
- Pro previously used College sack accounting. New Pro games keep sacks out of rushing attempts/yards and subtract them through a separate sack-loss field in net team offense. College retains sacks in rushing. Gross passing yards remain available for player comparisons; net team total yardage reconciles. Old saved box scores retain their data/default-zero new fields.
- Pro AI previously ran too frequently. An NFL-specific base pass bias and modest base-run calibration retain contextual play selection, matchup quality and explosive-run tails. College pacing/run rules remain unchanged.
- Initial uniformly generated attributes depressed offense despite similar OVR. Position-specific athletic/skill archetypes, newly varied ratings and canonical OVR fitting now preserve plausible performance. Initial Pro team strength is deliberately close; College generation varies the selected roster around existing program strength. New names/IDs/classes/ages/potentials vary each new creation.

## Reference and limits

Developer reference sources: [NFL2025 passing](https://www.nfl.com/stats/team-stats/offense/passing/2025/reg/all), [NFL2025 rushing](https://www.nfl.com/stats/team-stats/offense/rushing/2025/reg/all), [NFL2025 scoring](https://www.nfl.com/stats/team-stats/offense/scoring/2025/reg/all), [official NCAA FBS statistics](https://www.ncaa.com/stats/football/fbs) and the [NCAA FBS records national-trends series](https://fs.ncaa.org.s3.amazonaws.com/Docs/stats/football_records/FBS.pdf). The records PDF could not be fetched during this run; do not claim a freshly imported exact NCAA aggregate. Audit bands are deliberately broad engineering checks, not official league limits or a fitted probability forecast:18–32 points,280–450 yards,54–72% completions,3.5–5.8 yards/rush for these league means. Actual per-team and leader variation is preserved, not clipped to bands. College's balanced all-FBS schedule differs from real schedules with FCS opponents; current-source ratings and future league development also differ from historical league averages. The evidence supports plausible distributions and no obvious early-career collapse, not perfect reproduction of a real season or every rotation/coaching archetype.

Run with `dotnet run --project tools/FootballBalanceAudit -c Release -- pro-generated-seasons 3 .artifacts/pro-generated.json` (also pro-real-seasons, college-generated-seasons, college-real-seasons). Generated worlds deliberately use fresh creation; exact roster/attribute identity is verified through save/load, not regeneration. No extra roster seed is persisted.
