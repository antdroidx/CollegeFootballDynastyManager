# Dynasty Manager roadmap

Working continuation of the roadmap discussed in the "Dynasty Manager Roadmap" chat. Build 202's geography and Week 19 fixes were confirmed working by the user. Subsequent work is published through the GitHub development branch and APK workflow.

| Priority | Area | Current status | Next exit criteria |
|---|---|---|---|
| 1 | Recruiting fixes | Shared multi-school HS/portal offer engine implemented; interest, effort, withdrawals and capacity drive decisions | Keep current save compatibility and single ownership across recruiting paths |
| 2 | Recruiting balance | Expanded: six seeded careers, including 130-team leagues, games and prestige movement; see build-205-career-validation.md | Full-league, multiple-seed comparisons with changing results/coach strength and CPU peers |
| 3 | Long-term dynasty health | Expanded: five/ten-year careers with weekly portal decisions, games, age/depth checks and annual save/load | Longer careers with games, deferred transfers, position age distributions and save recovery |
| 4 | Interface polish | Build 223 flattens remaining Android control/scroll dividers, restructures recruit detail and allows every conference to be viewed | Continue refining presentation using device feedback |
| 5 | Game-day control | Build 225 introduces the shared snap engine, Sim/Watch/Play modes, live field markers, play selection, fatigue rotation and recorded player game logs | Continue visual polish, situational playcalling, special-teams depth and richer animation |
| 6 | Broader modernization | Future | Remaining position detail, CSV workflows, faces and presentation |
| 7 | NIL and coaching budgets | Later; design backlog only | Separate NIL resources and coaching/staff budgets, sustainable multi-year commitments and recruiting/carousel integration |

## Current delivery

- Show an animated loading screen while recruit list calculations run off the UI thread; build visible rows in small batches.
- Cancel outdated loads on navigation or a replacement request and prevent stale results from overwriting the new view.
- Recognize CPU-signed recruits in visible interactions, assistance and manual actions, and prevent double-signing during resolution.
- Add repeatable five-cycle diagnostics rather than assuming that a single working season proves balance.

## Next work

1. Expand balance/health diagnostics to FBS scale and additional seeds, including simulated wins/losses and prestige movement.
2. Investigate low-prestige walk-on dependence and age/position bottlenecks from those measurements; preserve organic commitment timing and roster-based portal decisions.
3. Continue interface clarity once the recruiting and dynasty-health checks are dependable.

Competing offers are now a core engine feature (Build 204). CPU and user schools share persisted offers and one decision resolver. CPU direct signings and protected user targets have been removed. The next balance baseline must use this shared market; the original Build 203 report is historical.

Build 206 adds a selectable 138-team 2026 FBS structure with saved league configuration, modern conference game counts, original HS recruiting stars and a roster career-stats view. See build-206-modern-fbs-careers.md for limitations and validation.

Build 207 starts UI modernization and coaching controls. Drive-level playbooks and opening call sheets now affect the shared simulation. Full live per-play calling follows the snap-event layer; see build-207-coaching-ui.md.

## Later economy additions

NIL and coaching budgets are deferred while the UI, playcalling and career-history layers develop. Plan for separate NIL resources and school coaching/staff budgets, coach salaries/contracts/buyouts, multi-year affordability forecasts, and recruiting/transfer offer integration. Coaching preferences should influence spending. Avoid hard transfer/recruit signing caps or a money-only recruiting winner; preserve competition, player preferences and long-term roster health. This is a game-design backlog, not an implemented economy or a model of current NIL regulations.

Build 208 adds coach career histories and an archive: user/CPU head coaches and other staff retain season results by identity through moves and departures. Head-coaching records are separate from team results earned in other staff roles.

Builds 209–211 revise the initial UI after feedback: Home starts/loads careers, active dynasty navigation is separate, saves are grouped by dynasty, and short landscape windows keep recruiting controls beside the list. This remains an iterative UI, not a final visual design. See build-209-save-library-ui.md for scope and device validation.

Builds 212–214 add school-color palettes for all 138 schools and organized grids for rankings, standings, postseason, schedules, rosters and recorded player statistics. Roster filters and player-detail controls, season/career statistics with paging, and responsive wide-screen headers continue the UI revision. See build-212-organized-team-grids.md.

Builds 215–216 reorganize the active-career tabs as Overview / Roster / Coach / Recruit / Season, separate Settings, connect Overview cards to their reports, add factual generated headlines, sortable grid headings and reusable league-wide player/coach profiles. Initials avatars are placeholders; real portraits and live snap-by-snap viewing remain future work. See build-215-navigation-news-profiles.md.

Build 217 adds persistent position-sensitive football skills, player-card ratings and aggregate passing/rushing simulation integration, plus upset/streak/shutout/blowout/postseason news from recorded outcomes. Detailed individual fatigue, penalties, catch/fumble events and snap-level effects remain work for the play-by-play engine. See build-217-skills-notable-news.md.

Builds 218–219 establish the fresh-game boundary (schema 23): independent detailed skills drive position overall and passing/rushing performance directly; player creation and practice use the new model. Prior save formats require a new dynasty. See build-218-fresh-game-ratings.md.

Build 220 organizes player profiles into character, physical and skill traits; adds starter/role roster filters and a drag-and-drop depth-chart organizer with button controls; links the Coaching Hub to that organizer; improves recruit-card hierarchy; and curates varied Overview headlines so injury reports do not crowd out notable results. See build-220-roster-recruit-news-polish.md.

Build 221 removes the redundant injury-proneness display, adds class year to the depth organizer, clarifies the Exit Dynasty action, simplifies trait layouts and rebuilds recruit cards as scannable metric/status blocks. It also begins the next game-day milestone with persistent offensive/defensive formations, personnel-aware play definitions and a saved working playbook. See build-221-playbook-foundation-ui-polish.md.

Build 222 expands every offensive formation to at least eight calls and every defensive formation to at least six calls, for 40 offensive and 30 defensive definitions. It removes generic border/table separators, applies the clean trait layout to coach profiles, and standardizes player class labels including permanent redshirt-history suffixes. See build-222-expanded-playbooks-class-labels.md.

Build 223 follows device feedback: standings can switch among every conference, table scrollbars and Android picker/search underlines no longer appear as heavy dividers, recruit detail uses a metric-and-market layout, and every playbook call has readable field art. See build-223-recruit-standings-play-art.md.

Build 225 moves regular-season, conference-title, bowl, playoff and interactive user games onto one snap engine. Sim Game, Watch Game and Play Game share playbooks, coaching, personnel packages, fatigue rotation and player event recording. See build-225-game-day-snap-engine.md.
