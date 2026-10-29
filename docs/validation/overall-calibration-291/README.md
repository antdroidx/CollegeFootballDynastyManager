# NEXT26 overall calibration validation

[Validated build and evidence](https://github.com/antdroidx/Football-Sim-Engine/actions/runs/39886044927).

## Starting roster

The same 11,560 NEXT26 players are compared, including the previously applied global attribute corrections. Before uses Build 289's POCI normalization; after uses fixed position curves with unchanged POCI attribute weights. No EA overall overrides or annual percentile normalization are used.

| Measure | Before | After |
|---|---:|---:|
| League average | 71.96 | 72.73 |
| Median | 72.00 | 72.00 |
| Starter average | 78.95 | 79.16 |
| 80+ (%) | 20.87 | 16.25 |
| 85+ (%) | 7.66 | 5.70 |
| 90+ (%) | 2.16 | 1.34 |
| 95+ (%) | 0.42 | 0.01 |
| 99 (%) | 0.04 | 0.00 |
| Freshman average | 67.17 | 68.80 |
| Senior average | 75.91 | 76.30 |

## All 21 positions

| Position (PPOS) | Players | Mean before | Mean after | Starter before | Starter after | 90+ before (%) | 90+ after (%) |
|---|---:|---:|---:|---:|---:|---:|---:|
| QB (0) | 559 | 69.57 | 72.87 | 79.49 | 79.60 | 2.15 | 0.54 |
| HB (1) | 742 | 77.90 | 79.59 | 85.04 | 86.29 | 2.43 | 3.10 |
| FB (2) | 155 | 69.30 | 66.57 | 68.98 | 66.33 | 2.58 | 0.00 |
| WR (3) | 1207 | 78.35 | 73.75 | 83.97 | 80.25 | 4.56 | 1.91 |
| TE (4) | 795 | 67.77 | 71.26 | 77.46 | 80.24 | 0.00 | 1.01 |
| LT (5) | 474 | 62.11 | 72.98 | 69.38 | 79.41 | 0.00 | 2.11 |
| LG (6) | 464 | 62.35 | 73.06 | 70.24 | 80.01 | 0.00 | 1.72 |
| C (7) | 410 | 62.54 | 72.81 | 69.76 | 79.34 | 0.00 | 1.95 |
| RG (8) | 441 | 62.31 | 73.11 | 69.14 | 79.09 | 0.00 | 1.13 |
| RT (9) | 460 | 61.70 | 72.97 | 68.78 | 78.69 | 0.22 | 2.39 |
| LE (10) | 567 | 71.16 | 71.81 | 77.28 | 78.11 | 0.71 | 1.06 |
| RE (11) | 544 | 71.12 | 72.20 | 77.33 | 78.47 | 0.37 | 1.29 |
| DT (12) | 816 | 73.47 | 72.99 | 78.39 | 77.79 | 1.10 | 0.98 |
| LOLB (13) | 337 | 74.47 | 68.98 | 77.94 | 72.34 | 2.37 | 0.89 |
| MLB (14) | 552 | 68.96 | 70.48 | 76.88 | 78.13 | 0.36 | 0.72 |
| ROLB (15) | 467 | 76.61 | 71.16 | 83.04 | 76.96 | 5.57 | 1.07 |
| CB (16) | 1059 | 77.41 | 73.26 | 81.99 | 78.25 | 1.98 | 1.23 |
| FS (17) | 560 | 77.41 | 71.12 | 83.72 | 78.29 | 3.57 | 1.07 |
| SS (18) | 505 | 78.17 | 72.28 | 84.48 | 78.49 | 5.74 | 0.79 |
| K (19) | 251 | 73.85 | 71.35 | 79.54 | 74.87 | 7.97 | 0.00 |
| P (20) | 195 | 75.76 | 72.33 | 78.81 | 74.09 | 9.74 | 0.00 |

Position starters are the top player per school and NCAA position code, except WR/CB use three and DT two. This includes FB and punter specialists. League starter averages use the game's depth-chart starter rules, so the two summaries have different denominators.

## Twenty-season FBS lifecycle

| Seed | Season | Mean | Median | Starters | 80+ % | 85+ % | 90+ % | 95+ % | 99 % | FR mean | SR mean | Elite programs | Weaker programs |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | 1 | 72.57 | 72.00 | 79.16 | 16.01 | 5.62 | 1.32 | 0.01 | 0.00 | 68.44 | 76.30 | 75.83 | 69.67 |
| 1 | 5 | 72.91 | 71.00 | 80.62 | 26.85 | 8.66 | 1.66 | 0.10 | 0.00 | 70.96 | 73.64 | 77.57 | 71.02 |
| 1 | 10 | 72.91 | 71.00 | 80.54 | 26.63 | 8.54 | 1.65 | 0.17 | 0.00 | 70.09 | 75.54 | 77.88 | 70.91 |
| 1 | 20 | 73.04 | 72.00 | 80.77 | 26.92 | 8.69 | 1.62 | 0.14 | 0.00 | 70.37 | 74.63 | 77.56 | 71.12 |
| 17 | 1 | 72.58 | 72.00 | 79.16 | 16.01 | 5.62 | 1.32 | 0.01 | 0.00 | 68.47 | 76.30 | 75.83 | 69.68 |
| 17 | 5 | 73.02 | 72.00 | 80.70 | 26.97 | 9.14 | 1.66 | 0.10 | 0.00 | 71.44 | 73.84 | 77.67 | 71.34 |
| 17 | 10 | 72.79 | 70.00 | 80.55 | 26.53 | 8.56 | 1.59 | 0.14 | 0.00 | 70.13 | 74.60 | 77.21 | 71.18 |
| 17 | 20 | 72.82 | 70.00 | 80.59 | 26.20 | 8.47 | 1.68 | 0.15 | 0.00 | 70.04 | 74.95 | 77.32 | 71.02 |

Machine-readable evidence: [starting roster](starting-roster.json), [seed 1](dynasty-seed-1.json), [seed 17](dynasty-seed-17.json).

## Method and limits

- Fixed curves interpolate the supplied nine anchors per position. Validation required a two-point body lift, tapering to zero toward 99, and explicit low/high endpoints. A 99 requires a near-perfect weighted score of at least 98.5; there is no required number of elite players.
- Generated athletes fit underlying attributes to the shared scale. HS profiles persist through signing; transfers retain attributes. Stars influence overlapping potential ranges, with no fixed current OVR. Walk-on/replacement targets rose two points, and initial filler prestige scaling was moderated.
- Projected generational potential is rare: 0.5% of five-star, 0.02% of four-star and 0.005% of three-star prospects may receive 99 potential. It is not 99 current OVR and does not guarantee reaching 99; eligibility, development, injuries and early departures still apply.
- Development remains attribute-based and staff/morale/injury aware. CPU depth is reassessed each offseason; human manual depth remains intact. Save-scale migration preserves attributes and remaining potential headroom and restarts the current display baseline once. Historical snapshots retain their recorded values.
- The FBS lifecycle tests use 138 schools, the bundled 136-school roster plus two generated schools, actual portal/recruitment/development/walk-on services, and two deterministic seeds. They isolate roster balance without playing full national game schedules. Prestige strata remain fixed in this test to make elite-versus-weaker comparisons interpretable.
- Separate 12-team career tests play complete schedules, record stats/injuries, evolve prestige/staff, and save/reload through 20 seasons. These complement rather than replace the FBS lifecycle measurement. Long-term distributions are test tolerances, not production quotas.
- JSON evidence includes every annual checkpoint, all positional threshold distributions, freshman/senior distributions, recruiting-star populations and recent development stages for retained players. Development averages have survivor bias; this is not a causal estimate of star rating or staff quality.
- Existing recruiting-market roster filling is retained. JSON exposes walk-on counts; rating balance does not establish realistic scholarship/transfer volumes. Initial positional cohorts can differ from later generated cohorts, so per-position annual distributions are included rather than forcing fixed positional averages.
- Two seeds provide a reproducible balance check, not a guarantee for every custom league, import or user strategy. Optional realignment and extreme custom rosters can change league distributions.

Validation: 608 core tests and nine Android smoke-helper tests passed. Android APK packaging succeeded. Emulator validation is recorded in the linked build. Final application source: 2837732.

Final result: Android emulator validation also passed. [Download Build 291 APK](https://github.com/antdroidx/Football-Sim-Engine/actions/runs/39886044927/artifacts/12473218579). The light landscape player-profile screenshot was visually reviewed. Main remains unmerged.
