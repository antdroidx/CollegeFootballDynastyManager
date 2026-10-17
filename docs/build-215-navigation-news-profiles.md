# Builds 215–216: actionable Overview, Season center, headlines and profiles

The primary tabs are Overview, Roster, Coach, Recruit and Season. Season holds statistics, national/conference/postseason rankings, schedules, every school's roster/staff, injury reports and headlines. Settings is a separate header destination for recruiting assistance and save preferences. Program/staff management is accessible from Coach. The save library remains separate from an active career. Back navigation restores the previous in-career section before returning to the save library.

Overview uses colored cards linked to their content: Record opens conference standings (national standings for independent schools), Team Rank opens national rankings, Next Game opens a matchup hub with schedule/game-plan/advance-week actions, and Injuries opens a report with user-team/league scope. Live snap-by-snap viewing remains future work; the current game engine continues to simulate drives/calendar weeks.

News stories are generated locally and deterministically from recorded scores, commitments, upcoming games and current injuries. Opening a story shows its body and a link to the related screen. They do not invent interviews, outcomes, recruiting stars or injury estimates. Results/commitments can be regenerated from retained save data; current injury/preview stories are not a separately archived news timeline. No external AI service or network request is required, and reading news does not mutate the save.

Data grids have ascending/descending column-heading controls, visible sort direction, numeric/rank/star ordering and stable ties. Unknown values remain last. Statistics sorts the complete filtered result set before applying its 50-player pages. Action columns are navigation controls rather than sortable values.

Player names open reusable profiles from user/team rosters, statistics and injuries. Profiles include current attributes, status, original HS rating where known, recorded career totals, season statistics and links to the school. Departed players retain available career records without invented current attributes. Coach profiles include attributes and recorded school/role/season history; current team staff is presented as a clickable grid. School badges in tables link to that school's roster/staff. Initials avatars are explicit placeholders; real profile pictures are still future work.

## Validation

- 210 focused Release tests passed, including factual/deterministic news, neutral-site and tied results, preview/commitment/injury reports, recovery, numeric sorting, stable ties and global ordering before paging.
- Build 216 passed GitHub tests, Android APK packaging and emulator smoke validation. The downloaded APK reports version 2.0.216 and its crash buffer remained empty.
- Isolated emulator checks passed for Overview links, conference/national destinations, matchup preparation, Settings, back navigation, league injuries, stories, player/coach profiles, the user head coach in staff listings, numeric roster sorting and ascending/descending global statistics sorting across 73 players. Portrait and landscape layouts were inspected.
- Save schema and simulation rules are unchanged. News is derived from existing records; no monetary mechanics or fabricated historical player attributes were added.

On-device validation uses an explicitly named disposable navigation fixture with sample results, two injuries, a recruiting commitment and 73 recorded player careers. These events are QA data and are not shipped in the APK.

## Screenshots

Screenshots use the disposable sample dynasty described above.

- [Overview](ui/build-216-overview-final.png)
- [Season center](ui/build-216-season-final.png)
- [Coach profile](ui/build-216-coach-profile.png)
- [Sortable roster](ui/build-216-roster-sort-descending.png)
