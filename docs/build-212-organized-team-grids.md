# Builds 212–214: organized grids and school colors

Rankings, conference standings, postseason fields/games, schedules, user rosters, team-browser rosters and recorded player statistics now use shared native MAUI grids. Aligned column headers, alternating rows, rating badges and explicit result labels replace long bullet-separated lists. Tables scroll horizontally on narrower screens and remain inside the screen's vertical scroll. On wider screens, summaries and filters sit side by side to leave more room for data rows.

All 138 current league schools have offline primary/secondary palettes, including legacy save names. School badges use both colors; roster/schedule/career headers use the school primary with a secondary stripe. Black/white badge text is selected from background luminance. Body text stays readable on consistent table surfaces. W/L labels and scores supplement result colors; ratings retain numeric values. Unknown/custom names use a neutral fallback. No save migration or network access is required.

The roster has name search, position and depth/rating/potential/name sorting. Open a player to adjust depth or use existing offseason redshirt/cut controls, inspect attributes/development and view career statistics. Player details refresh after changes. Other schools' rosters are read-only grids with career-stat actions.

Rankings has National, Conference and Postseason panels, plus Top 25/all-school selection. Schedules have home/away filtering, colored opponents, explicit scores, game type, yards and turnovers. Player statistics is accessible from Roster and Manage. It supports recorded seasons, career totals, team/all-school scope, sorting and 50-player pages. The player's career page shows season/school records and career sums. Existing data only records simplified passing/rushing allocations; no new receiving/defensive statistics are invented. School-scoped career totals count only seasons at that school; the all-school view combines records by player ID and displays the latest school in the selected records.

## Data provenance

School colors were retrieved from [ESPN team metadata](https://site.api.espn.com/apis/site/v2/sports/football/college-football/teams?limit=1000) on October 16, 2026, mapped to stable game school names, and bundled in `TeamBrandCatalog`. No ESPN artwork or remote assets are loaded by the app.

## Validation

- 204 focused Release tests passed, including complete palette coverage for the legacy and modern leagues, valid color values, name lookup and neutral fallback.
- Signed Android packaging passed; five existing obsolete MAUI alert warnings remain.
- Emulator checks verified roster details/depth changes, name filtering, career navigation, school-change career sums, schedule W/L colors and venue filtering, rankings/conference/postseason panels, and access to the last page of all-school statistics. Final portrait/landscape layouts and the exact downloaded APK were verified. Build 214 passed GitHub focused regressions, APK packaging and emulator Home/setup/cancel checks. The installed APK reports 2.0.214 / version code 614, and its crash buffer stayed empty.
- Populated-statistics checks use an explicitly named disposable emulator fixture with two sample seasons, a school change, more than 50 players, and W/L examples. This fixture is not shipped and does not touch real user saves.

## Actual Build 214 screenshots

[Rankings](ui/build-214-rankings.png) · [Schedule](ui/build-214-schedule.png) · [Roster](ui/build-214-roster.png) · [Landscape statistics](ui/build-214-statistics.png)

These show the actual emulator UI with the disposable sample-statistics fixture described above. Sample results/rankings are UI test data, not recruiting or game-engine balance evidence.
