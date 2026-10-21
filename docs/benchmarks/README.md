# Game-day calibration against 2025 FBS

Run from the repository root:

```powershell
dotnet run --project tools/SimAudit -c Release -- 272 2025 .artifacts/calibration.json
dotnet run --project tools/SimAudit -c Release -- 544 70931 .artifacts/validation.json
```

The runner uses the bundled August 26, 2025 NEXT26 roster and the same `GameDayEngine` used by Sim, Watch, Play and normal weekly/postseason games. It selects the 136 schools present in that pack, derives ratings and game plans from actual roster attributes, rotates home schools evenly and samples opponents. It does not force outcomes or a play-count maximum. Team statistics include overtime; regulation play count excludes overtime, kicks, tries, penalties and clock-only events. National offense tables include overtime, so the play-count comparison has that small methodological difference. Published FBS totals also include games against FCS opponents; this audit samples FBS versus FBS.

The table below records the Build 266 calibration. The first 272 games were used for tuning. The 544-game sample uses a separate seed and was not used to choose parameters. These are synthetic schedules, not a replay of the actual 2025 schedule. Imported August ratings, injuries, depth charts and coaching choices do not reconstruct the season's real development or availability.

| Metric | Before | Validation (544 games) | 2025 FBS |
|---|---:|---:|---:|
| Combined regulation plays/game | 181.2 | 134.5 | 133.9* |
| Points/team/game | 35.1 | 26.7 | 27.5 |
| Yards/team/game | 543.5 | 389.4 | 383.5 |
| Passing yards/team/game | 413.3 | 235.6 | 224.6 |
| Rushing yards/team/game | 130.2 | 153.8 | 159.0 |
| Yards/play | 5.97 | 5.76 | 5.73 |
| Completion percentage | 68.1% | 62.5% | 62.3% |
| Yards/pass attempt | 7.74 | 7.16 | 7.31 |
| Yards/rush (including sacks) | 3.79** | 4.43 | 4.39 |
| Sacks/team/game | 3.33 | 1.97 | 2.07 |
| 20+ yard pass plays/team/game | 4.33 | 2.92 | 3.11 |
| 20+ yard rush plays/team/game | 0.00 | 1.42 | 1.31 |
| Recorded YAC/completion | 0.00 | 5.40 | Not available in these public tables |

*National combined plays = twice total offensive plays / total team-games (1,742), not an unweighted average of the displayed rounded averages. The national play total includes overtime. **Before the change, sacks were omitted from rushing attempts/yards; the baseline is the original reported output, not a retrospectively corrected number.

The old model skipped pre-snap runoff on an incomplete pass and again on the following snap. The replacement charges pre-snap and live-ball time separately, before the result is resolved. Running clock, incomplete-pass/timeout/possession stoppages, late first-down ready-for-play restarts, two-minute timeouts and period expiry are distinct. Overtime remains untimed. Automatic tempo uses team identity and playbook, with situational hurry-up and clock protection. Coach can save Normal, Up tempo or Chew clock. User play selection pauses the presentation; simulated game-clock runoff is applied when the next call is submitted.

Receiving gains now separate air yards from YAC, with space, speed, acceleration, agility, break tackle, defensive tackling and pursuit affecting an explosive tail. Runs also have a rating-sensitive breakaway chance. Both are limited by the actual distance to the goal line, not an arbitrary gain cap. The viewer follows the catch point and then the carrier's YAC segment. Individual receiving, quarterback, team and drive yardage reconcile; sacks count as NCAA rushing losses and TFLs. The national comparison does not establish that the new YAC average itself matches 2025; only overall passing and explosive-play rates have a public benchmark here.

Remaining limitations: probabilistic play resolution, no full collision simulation; live-ball penalties, safeties and complete defensive/return scoring remain follow-ups. Individual schools, leaders, strength of schedule, distributions by conference, turnovers and special-teams rates need a broader season-level audit. No claim is made that matching league means validates every player or team.

## Sources and calculation inputs

- [2025 total offense](https://cfbstats.com/2025/leader/national/team/offense/split01/category10/sort01.html), [passing](https://cfbstats.com/2025/leader/national/team/offense/split01/category02/sort01.html), [rushing](https://cfbstats.com/2025/leader/national/team/offense/split01/category01/sort01.html), [scoring](https://cfbstats.com/2025/leader/national/team/offense/split01/category09/sort01.html), through January 19, 2026.
- [Long passing plays](https://cfbstats.com/2025/leader/national/team/offense/split01/category32/sort01.html), [long rushing plays](https://cfbstats.com/2025/leader/national/team/offense/split01/category31/sort01.html), and [sacks](https://cfbstats.com/2025/leader/national/team/offense/split01/category20/sort01.html). Use the 20+ column for explosive plays.
- [NCAA timing changes](https://www.ncaa.org/news/media-center-football-timing-rules-approved-for-divisions-i-ii/), [two-minute timeout rule](https://www.ncaa.org/news/media-center-technology-rules-approved-in-football/), [NCAA 40/25-second clock explanation](https://ncaanewsarchive.s3.amazonaws.com/2008/association-wide/football-rules-changes-succeed-shorter-games-faster-play-result---10-08-08---ncaa-news.html).
- [NCAA statistics manual](https://fs.ncaa.org/Docs/stats/Stats_Manuals/Football.pdf).

`2025-fbs.json` contains derived national aggregates and source URLs. `before-build266.json` and `build266-validation.json` contain measured simulator output. Original downloaded HTML is retained only in ignored local artifacts.

## Build 267 pursuit check

`build267-validation.json` repeats the separate 544-game sample after the pursuit and live box-score work: 134.3 regulation plays, 388.8 yards/team, 62.5% completions, 2.03 sacks/team, 2.90 explosive passes/team and 1.38 explosive runs/team. This is a regression check of the earlier calibration, not a new fit to the data. The runner now also reports credited tackles by position, including special-teams participants. Defensive pursuit uses shared formation/assignment starting positions, estimated arrival/reaction time, speed/acceleration/agility and tackling ratings. It chooses among close contenders rather than forcing equal tackle shares by position. Receiving/defensive coverage is still an approximation, not a collision simulation.

Live box-score snapshots read the same in-progress event/stat builders used by final results, without consuming randomness or marking a game complete. The viewer pauses after the current animation while a box score is open and resumes on return. Quarter scoring includes tries and overtime; player statistics can be filtered by team/category and sorted. Touchdowns and interceptions no longer invent defensive tackles.

## Build 268 formation correction

`build268-validation.json` repeats the same 544-game sample after correcting default slot-corner alignment and inside/outside linebacker positions. All scoring, play-count, yardage, completion, turnover, sack and explosive-play metrics are exactly unchanged from Build 267; credited tackle shares change with the corrected geometry. Default corners/slot corners have distinct starts, partial custom assignments preserve remaining slots, and explicit rush/drop assignments are matched by position **and role index**. Passing reads record actual rusher counts; an all-coverage call does not generate an invented pass-rush sack.
