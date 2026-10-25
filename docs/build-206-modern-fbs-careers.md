# Build 206: modern FBS and player career history

## League setup

The new-dynasty screen defaults to Modern FBS (2026), with Legacy FBS still selectable. The modern preset has 138 programs: ACC 17, Big Ten 18, Big 12 16, SEC 16, American 14, CUSA 10, MAC 13, Mountain West 10, Pac-12 8, Sun Belt 14 and two independents.

The eight added schools are James Madison, Jacksonville State, Kennesaw State, Sam Houston, Delaware, Missouri State, North Dakota State and Sacramento State. Existing school identifiers/names and imported ratings are retained. New schools receive generated rosters; this is not an updated real-player roster database.

Each new save includes its complete team/conference configuration. Loading a save without this metadata selects the legacy universe. Changing the new-dynasty picker does not change the active career's league.

Modern scheduling specifies nine conference games for the Big Ten, Big 12 and SEC; seven for the Pac-12; eight for the other leagues; and zero for independents. The ACC uses its 2026 transition: Boston College, Clemson, Florida State, Georgia Tech and North Carolina play eight; the other twelve play nine. Conference pairing supports odd-sized groups and mixed counts, instead of losing games to round-robin byes. Sun Belt championship participants come from its two divisions; the other modern leagues use their top two teams.

The preset retains these game-count rules in future years. Fixtures remain generated: official dates, permanent rivals, future ACC rotation changes, FCS opponents and the Pac-12 flex rematch are not modeled here.

### Membership/rule sources

- [Pac-12 membership](https://pac-12.com/news/2026/6/30/general-the-new-pac-12-conference-officially-launches-with-the-addition-of-seven-full-time-members.aspx)
- [Mountain West membership](https://themw.com/sports/2026/5/28/this-is-mountain-west.aspx)
- [CUSA's revised ten-team alignment](https://admin.conferenceusa.com/news/2026/4/29/fb-cusa-releases-revised-2026-football-schedule.aspx)
- [Sacramento State joins the MAC](https://www.csus.edu/president/presidential-communications/)
- [Sun Belt welcomes Louisiana Tech](https://sunbeltsports.org/news/2026/7/1/general-sun-belt-officially-welcomes-louisiana-tech.aspx)
- [SEC nine-game schedule](https://www.secsports.com/news/2025/08/sec-to-implement-nine-game-conference-football-schedule-in-2026)
- [ACC transition](https://admin.theacc.com/news/2026/7/15/acc-announces-new-football-championship-tiebreaker-policy.aspx)

## Player history

Existing completed-season stat rows already persist by player ID, including the team represented each year. They remain available after transfer or graduation. Current individual stats are simplified allocations of team passing/rushing totals to QBs and runners. Receiving, defensive and kicking player stat lines are not implemented.

New HS signees now retain OriginalRecruitingStars independently of talent, OVR and potential. The value travels with the player into the portal and subsequent rosters and is copied into season stat history. A career identity archive records every rostered player at season finalization, including positions without recorded stat lines, preserving their identity and original stars after departure. Older/imported players lacking a known original rating remain unknown rather than receiving an invented rating.

Roster rows show the original HS rating and a Career stats action with completed seasons, school names and career totals. Save schema advances to 19.

## Validation

188 focused Release tests, Android Debug build and packaged-resource checks passed. The build has five existing obsolete MAUI alert API warnings. The six long synthetic-career tests are left to the complete GitHub suite for this build. Added checks cover all conference memberships/counts, 85-player rosters for every modern school, three seeded full-league schedules with exact conference game counts and no weekly collisions, ten championship games with separate Sun Belt division representatives, HS signing star preservation, and career stats/stars surviving transfer, graduation and SQLite save/load.
