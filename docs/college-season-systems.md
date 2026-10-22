# College season systems

The Season tab now links to **Awards, polls & program center**. This contains honors, poll history, program conduct, the weekly recap, and conference strength. Schedule results also open a game center with the full saved box score and highlight replays.

## Honors

Preseason and final first/second teams cover a starting offense, defense, and kicker nationally and within each conference. Five offensive linemen and the appropriate multiple receiver/defensive slots are selected, rather than a single player at every position. Preseason selection combines returning talent and the previous season's production. Final selection uses recorded season production and actual appearances; individual blocking statistics are unavailable, so linemen use appearances and overall rating. Redshirts and unused bench players are excluded from final selections.

The Heisman, Maxwell, Davey O’Brien, Doak Walker, Biletnikoff, Mackey, Outland, Bednarik, Thorpe, Groza, and Ray Guy awards have position/role criteria. The Heisman uses position-normalized excellence, production, and team success so an exceptional receiver or defender can contend. Current-season selections are a provisional race until season finalization after the national championship. Final honors are stored in `PlayerAwardHistory`, survive graduation/transfers, and appear on player profiles. Preseason honors are labeled separately. Original pixel-style trophy drawings are rendered offline; the Heisman has a football-player silhouette.

Award labels and roles were checked against the [National College Football Awards Association](https://ncfaa.org/2025-ncfaa-award-winners). Selection scores are this game's simulation model, not real award voting results.

## Polls and projections

**Media** and **Coaches** use separate simulated panels of 65 and 60 voters. Each ballot awards 25 points to first place down to one point for 25th. Voter preferences are stable and seeded. Early expectations combine roster talent (45%), current program prestige (25%), the last three completed seasons (15%, weighted toward the most recent), and conference reputation (15%). A program without recorded season history uses prestige as its history baseline. Expectations fade across eight games; results reach full weight after six, so a single opening win does not overwhelm preseason expectations or penalize a team on a bye. Previous rankings supply inertia, greater in the Coaches poll. Early opponent program strength distinguishes quality wins before opponent records become meaningful; later, wins, losses, opponent results, and capped scoring margins dominate. These weights model fictional human preferences, not an official AP/Coaches formula.

The ballot points follow the [AP's published method](https://www.ap.org/media-center/press-releases/2019/ap-top-25-football-poll-to-kick-off-84th-year-with-regions-bank-sponsorship/). The in-game panels are fictional; no real voters or live poll feeds are used.

**Computer** excludes prestige, roster talent, conference reputation, and prior poll position. It ranks resumes using record, opponents' win percentages, number of wins/losses, and scoring margin capped at 21 per game. Before games are played all computer resumes are equal; alphabetical order resolves ties. The playoff uses this resume-based ranking, subject to automatic conference bids and sanction eligibility. Conference power is the mean current program prestige and updates when membership changes.

All three published polls stay fixed throughout a week, including after manually played or simulated games and save/reload. Advancing the week resolves remaining games and releases the next poll; tables retain the records and statistics at publication. New seasons receive their own preseason poll. Saves missing snapshots establish a saved baseline using completed prior weeks only. Weekly poll snapshots are retained for rank-over-time charts (a numbered snapshot is the poll released after that week). Top-25 media ranks appear on school cells, team banners, and the live scoreboard. Poll tables show their own ranking without a second conflicting rank next to the school.

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

Realignment checks occur once per offseason with a 28% chance, exchanging members of conferences with at least four teams to preserve scheduling capacity. FCS promotion has a separate 16% chance, requires FCS opponents enabled, excludes already-playable schools, favors successful opponents and stronger programs, and adds a generated scholarship roster spanning all four class years to the playable league. These are gameplay rates rather than forecasts of real-world moves. Future schedules, conference strength, polls, and recruiting use the resulting league snapshot. A fast fan-recoloring calendar assignment handles normal schedules; the compact-calendar solver remains available when every week must be occupied.

## Recorded highlights and weekly recap

Every newly completed Sim, Watch, or Play game retains up to five notable snap events, slim participant snapshots, period scores, and its game plans. Highlights favor touchdowns, turnovers, late field goals, sacks, and explosive gains. They preserve actual play IDs, routes, ball ownership, personnel, yards, and outcomes; viewing them never rolls a new result or awards statistics again.

**Saturday rewind** orders completed games by notable plays and upset value. It offers individual game centers and an automatic weekly show of up to five games, with a stop control. Game centers have the full recorded box score, a highlight reel, and individual clips. Old saved results without a tape remain viewable but cannot show newly recorded highlights retroactively.

## Sprite feasibility (assessment only)

Sprites can replace the current marker bodies while keeping the existing field positions, routes, pursuit, ball ownership, and replay timing. A first version needs eight-direction idle/run frames and short tackle, throw, kick, and celebration sequences, plus team-color tinting. Jersey numbers, the active-carrier outline, and a marker-mode fallback should remain available. The main work is creating consistent art and animation transitions, then checking frame time on lower-end Android devices. This would improve presentation; it would not turn the statistical engine into collision-based football physics. No sprite assets or sprite renderer are included in this build.

## Pass-rush playback

Live games and recorded highlights pair defensive linemen and assigned rushers with available offensive linemen. They approach and remain on a moving blocker's defensive shoulder, with contact motion instead of a fixed spot behind the OL. Pass blockers retreat into protection; run blockers push forward. Only recorded sack/tackle pursuers release into a breakthrough, work around a shoulder, and reach the recorded contact; completion pursuit releases after the catch. Playback timing includes that escape path to avoid warp-speed pursuit. Explicit coverage assignments remain respected. These paths visualize the shared engine's outcomes; they do not add a separate collision resolver or reroll statistics.

## Experimental football sprites

The live viewer and recorded highlights default to original procedural pixel players. Helmets and home jerseys use team colors; away jerseys are white. Strides, passing and blocking poses follow the existing movement/possession timeline, with a gold ball-carrier ring and optional jersey-number/position labels. The ball is attached to the sprite using actual layout dimensions in portrait and landscape. No game results, ratings, substitutions or replay events are changed by choosing a renderer.

Use **Players: Sprites / Players: Circles** in the viewer to switch immediately; the choice persists on the device. **Markers: Numbers / Positions** controls the labels in either style. Players now use shaded 64-unit three-quarter pixel figures: upright torsos, modeled helmets/facemasks, bent arms, knees, cleats and direction-dependent front/back/profile poses. Eight movement headings change the helmet and limb pose rather than rotating a person sideways; route cuts still use current screen displacement and numbers remain readable. This is original procedural artwork, not the exact illustration or a finished hand-authored sprite sheet. Richer tackle, diving and celebration frames remain art follow-ups.

## Penalty attribution

Penalty events retain the offending team, foul name and enforced yards. The banner uses three lines, e.g. **PENALTY / 5 yards Offside / Washington**, and commentary repeats the same attribution. Defensive offside advances the offense; a false start moves the offense backward. No-play penalties do not become rushing/passing attempts. Snapshot/replay serialization retains the details; earlier events infer the team from enforcement direction when metadata is absent.

The current engine generates pre-snap false starts and defensive offside. Whole-yard half-distance enforcement is capped before either goal line; at the one-yard line the integer field model retains the spot rather than manufacturing a goal-line crossing. Fractional-yard spots and a complete live-ball foul catalog remain follow-ups.


## Hybrid landscape game-day layout

The landscape field spans the available width, with a compact score/situation header and commentary below it. Between user decisions a translucent bottom tray shows the selected/recommended call and two alternatives with play art. Selecting an alternative changes the selected call; **Call play / Call defense** still explicitly starts the snap.

**Choose play / Choose defense** expands a field overlay with a scrollable three-column library and formation filter. It uses only the controlled coach's available plays. Selecting a play closes the overlay without consuming game time. **Close playbook** or Android Back dismisses it. Portrait retains the larger scrolling cards and existing formation-grouped call sheets.

The tray's play cards disappear during a snap and while watching CPU play. **More** reveals special teams, kneel/spike, offense/defense delegation, onside preference, jersey labels and renderer options. The scoreboard's box-score control and compact watch/timeout/end-game controls remain accessible. This is an in-game layout change; the concept image's stadium surroundings and illustrated sprite assets are not bundled artwork.


## Live presentation, timeouts and instant replay

Play-art diagrams render in a fixed aspect ratio, centered within their cards. Landscape quick calls use art beside the labels; expanded calls retain larger art. The procedural field includes mowing bands, subtle turf grain, five-yard lines, college hash marks, team-colored named end zones and a bordered midfield initial. Sprites include collar/pad highlights and seams. This remains original procedural art rather than the earlier concept's illustrated stadium assets.

The commentary panel shows a neutral live-play caption while action unfolds and named play-by-play afterward. The active ball owner's full name appears above the sprite, transfers at the handoff/catch, and disappears during flight. Carrier names can be disabled in More. At user decision points, the prior active units gather in separate huddles without consuming simulation randomness. The next snap renders the called formation.

Both timeout banks appear in the scoreboard. User and CPU team timeouts record distinct clock-stoppage events containing the calling school and both remaining counts; a field banner announces the timeout. Official two-minute timeouts remain separate, without charging a team timeout. Standard coach playbooks reserve one Hail Mary within their existing play count; final-heave advice covers suitable final seconds of either half, after considering a feasible field goal. Custom playbooks must include it explicitly.

Tightly contested non-drop incompletions can be recorded as pass breakups. The credited defender converges at the target's catch point, the ball tips away without possession, commentary names the defender, and the same event credits a pass defended without adding a tackle or interception. Other misses and receiver drops retain separate animation/commentary.

Automatic instant replays show touchdowns, turnovers, two-point conversions, gains of at least 25 yards, and returns of at least 30 yards. More offers Auto replay On/Off. Replay last is available between user calls and after the final play; pause Watch first to replay manually. Replays use recorded events at slightly slower playback and never step the engine, consume clock/randomness, alter the pending call or add statistics. Recorded replay playback remains a visualization of the shared probabilistic engine, not collision physics.

## Live conditions, morale and portrait calls (after Build 280)

Carrier labels are now optional and off by default; enabling them displays an unobtrusive surname without an opaque box. Portrait uses the same three-call field overlay as landscape, with secondary controls under More. Replays announce themselves with a large banner. Kickoff auto replays require a returned ball ending strictly beyond midfield; onside, fair-catch and touchback events are excluded.

Weather is seeded fictional game-day weather, shared by Sim/Watch/Play, with wind, rain/snow and cold affecting passing, ball security and kicking. Indoor sites suppress weather effects. Morale responds to recent results, coaching leadership and major game events; execution effects are bounded and training receives a smaller modifier. These are an initial model, not real forecasts or a full player personality system.

PBU (passes defended) is visible in defensive statistics, including players whose only contribution is a breakup. Player of the Game is selected from recorded production across positions, saved with the game and archived for profiles. Rare long kickoff returns can now cross midfield and score with the normal conversion sequence. Final-seconds Hail Mary/field goal preparation preserves time for a snap; Hail Mary eligible receivers run deep routes. Huddles use the next unit's actual personnel.

## Carrier presentation, career growth and formation editing (Build 282 update)

The simulation already used speed, agility and break tackle versus pursuit/tackling to create explosive rushing and YAC gains. Those escape results are now recorded as BrokeTackle. Playback adds smooth cuts for carriers traveling at least four yards, with stronger cuts on recorded escapes. The ball shares the same path; this is an outcome-driven visualization, not a real-time collision/joystick engine. Subtle sprite labels default on (a new preference adopts this requested default; users can switch it off). A single label can move from the carrier to the nearest defender within five yards late in the play, without revealing the final tackler at the snap. All scrimmage gains of 20+ yards qualify for auto replay when replays are enabled; kickoff replay remains strictly beyond midfield.

Recruit cards offer Scout weekly to toggle staff priority during the season. Out-of-phase extra-review/offer/pitch transactions are hidden, with remaining unavailable actions visibly disabled. Shared overview/coach/roster/stats actions wrap in narrow layouts, and sortable tables expand into spare landscape width while preserving their minimum readable columns and horizontal scrolling.

Player morale combines recent team form, individual production/turnovers, leadership and injury; it is displayed with start-of-season overall change on profiles and rosters. OVR baselines are stored per season and survive saves. Automatic training at weeks 3, 6, 9 and 12 uses staff, class, role, potential, injury and morale. Actual attribute-derived NCAA OVR is restrained to a total -2/+2 in season and -2/+4 during offseason development, still constrained by potential. This is a player growth budget, not a roster/recruiting/transfer cap. A 750-player generated cohort improved from mean 69.1 to 71.8 by senior season; 505/750 improved and 3.7% finished at 90+. This deterministic sample is a regression guard, not a claim that every dynasty distribution is identical. Filler creation now also accounts for class year; imported real roster ratings remain untouched.

Elite juniors already had early declarations (93+ OVR, seeded 66% chance). Departures now persist across years and can be filtered by school/season, including graduations, early pro declarations and portal entries. The Hall of Fame inducts completed careers on graduation/pro entry, with original Java Team.checkHallofFame honors weights (conference 10, national AA 25, Heisman 60; other major awards 30) and career score thresholds. Position production adapts original metrics to recorded modern stats: no duplicated current/archived yards, no invented imported career seasons, no double-counted return yards, and at least two recorded seasons for the average-score criterion. OL relies on honors because blocking production is not recorded. Specialist weights use efficiency and conservative punt production. Entries and player links remain available after departure. Team/national filters are under Season.

Play editors now expose assignments rather than free alignment sliders. Offense and defense align with their selected formation/package. Offense uses five ineligible linemen plus two uncovered eligible ends on the line, with other receivers off the line; play art, read model and live view share this alignment. Formation changes update I-form / empty / flexbone personnel, and validation rejects nonexistent slots and OL routes.
