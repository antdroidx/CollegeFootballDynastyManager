# College season systems

The Season tab now links to **Awards, polls & program center**. This contains honors, poll history, program conduct, the weekly recap, and conference strength. Schedule results also open a game center with the full saved box score and highlight replays.

## Honors

Preseason and final first/second teams cover a starting offense, defense, and kicker nationally and within each conference. Five offensive linemen and the appropriate multiple receiver/defensive slots are selected, rather than a single player at every position. Preseason selection combines returning talent and the previous season's production. Final selection uses recorded season production and actual appearances; individual blocking statistics are unavailable, so linemen use appearances and overall rating. Redshirts and unused bench players are excluded from final selections.

The Heisman, Maxwell, Davey O’Brien, Doak Walker, Biletnikoff, Mackey, Outland, Bednarik, Thorpe, Groza, and Ray Guy awards have position/role criteria. The Heisman uses position-normalized excellence, production, and team success so an exceptional receiver or defender can contend. Current-season selections are a provisional race until season finalization after the national championship. Final honors are stored in `PlayerAwardHistory`, survive graduation/transfers, and appear on player profiles. Preseason honors are labeled separately. Original pixel-style trophy drawings are rendered offline; the Heisman has a football-player silhouette.

Award labels and roles were checked against the [National College Football Awards Association](https://ncfaa.org/2025-ncfaa-award-winners). Selection scores are this game's simulation model, not real award voting results.

## Polls and projections

**Media** and **Coaches** use separate simulated panels of 65 and 60 voters. Each ballot awards 25 points to first place down to one point for 25th. Voter preferences are stable and seeded; expectations based on talent, prestige, and conference strength fade across eight games. Previous rankings supply modest inertia, greater in the Coaches poll. Wins, losses, opponent results, and capped scoring margins drive the resume.

The ballot points follow the [AP's published method](https://www.ap.org/media-center/press-releases/2019/ap-top-25-football-poll-to-kick-off-84th-year-with-regions-bank-sponsorship/). The in-game panels are fictional; no real voters or live poll feeds are used.

**Computer** excludes prestige, roster talent, conference reputation, and prior poll position. It ranks resumes using record, opponents' win percentages, number of wins/losses, and scoring margin capped at 21 per game. Before games are played all computer resumes are equal; alphabetical order resolves ties. The playoff uses this resume-based ranking, subject to automatic conference bids and sanction eligibility. Conference power is the mean current program prestige and updates when membership changes.

Weekly poll snapshots are retained for rank-over-time charts. Top-25 media ranks appear on school cells, team banners, and the live scoreboard. Poll tables show their own ranking without a second conflicting rank next to the school.

Upcoming games show a **projected line**, total, favorite, and home win probability. These use current matchup ratings and home-field advantage; neutral sites remove the home bonus. They are game-generated projections, not sportsbook odds or a wagering feature.

## Program conduct

`ProgramDisciplineService.Events` is the versioned event catalog. It includes academic appointments, curfew, practice standards, sideline disagreements, compliance/benefits reviews, community service, and captain-led improvements. Stories describe fictional program situations, not allegations about real people.

Conduct is 0–100, with higher scores healthier. Player Discipline and head-coach Leadership affect incident risk and the program's recovery target. Processing is deterministic and idempotent per week. A user program can strengthen oversight once per season for an eight-point improvement. It does not erase an existing sanction.

Sanctions require conduct below 35, at least three major incidents within three seasons, a further rare check, and a cooldown. The consequence is a one-season postseason ban enforced for conference championships, bowls, and the playoff. Recruiting is not artificially capped. Disabling discipline suspends processing and sanction enforcement.

Coaching offers already existed in `UserCoachCareerService`; the overview now surfaces pending offers even when the coach has not been fired. Offers and accept/decline controls remain in the program/staff area.

## League options and FCS

Settings has separate switches for program discipline, periodic realignment, FCS opponents, and FCS promotion. Realignment and promotion default off. Built-in starting setups enable FCS opponents; copied/custom setups can choose the FCS option while validating their schedule. Changing scheduling options during a season takes effect with the next season's schedule.

The FCS library is a **2025-era historical snapshot** aligned to the bundled roster, including transitional programs and independents. It is not a live membership feed. Examples were cross-checked with the [Southland's 2025 schedule](https://www.southland.org/news/2025/2/4/southland-conference-announces-2025-football-schedule-uiw-nicholls-to-be-played-week-0.aspx), [NEC's 2025 report](https://s3.amazonaws.com/sidearm.sites/northeastconference.org/documents/2025/9/15/25_FB_Week_3_Notebook.pdf), and [Sacred Heart's 2025 notes](https://sacredheartpioneers.com/documents/download/2025/8/28/FB_Game_1_-_Stonehill.pdf).

Only schools absent from the playable league can be scheduled externally. Case/punctuation and supported name aliases are normalized for deduplication. Existing nonconference pairings can be replaced by two hosted FCS games while preserving each FBS school's season game count and conference opponents. No FBS team receives more than one FCS opponent, and neither FBS nor FCS teams play twice in a week. Odd league/odd game counts can use an external opponent to complete the pairing parity. FCS games use generated athletes and the same game-day engine, but external rosters do not enter FBS recruiting/staff markets. FCS results count toward records and bowl eligibility.

Realignment checks occur once per offseason with a 28% chance, exchanging members of conferences with at least four teams to preserve scheduling capacity. FCS promotion has a separate 16% chance, requires FCS opponents enabled, excludes already-playable schools, favors successful opponents and stronger programs, and adds a generated roster to the playable league. These are gameplay rates rather than forecasts of real-world moves. Future schedules, conference strength, polls, and recruiting use the resulting league snapshot. A fast fan-recoloring calendar assignment handles normal schedules; the compact-calendar solver remains available when every week must be occupied.

## Recorded highlights and weekly recap

Every newly completed Sim, Watch, or Play game retains up to five notable snap events, slim participant snapshots, period scores, and its game plans. Highlights favor touchdowns, turnovers, late field goals, sacks, and explosive gains. They preserve actual play IDs, routes, ball ownership, personnel, yards, and outcomes; viewing them never rolls a new result or awards statistics again.

**Saturday rewind** orders completed games by notable plays and upset value. It offers individual game centers and an automatic weekly show of up to five games, with a stop control. Game centers have the full recorded box score, a highlight reel, and individual clips. Old saved results without a tape remain viewable but cannot show newly recorded highlights retroactively.

## Sprite feasibility (assessment only)

Sprites can replace the current marker bodies while keeping the existing field positions, routes, pursuit, ball ownership, and replay timing. A first version needs eight-direction idle/run frames and short tackle, throw, kick, and celebration sequences, plus team-color tinting. Jersey numbers, the active-carrier outline, and a marker-mode fallback should remain available. The main work is creating consistent art and animation transitions, then checking frame time on lower-end Android devices. This would improve presentation; it would not turn the statistical engine into collision-based football physics. No sprite assets or sprite renderer are included in this build.
