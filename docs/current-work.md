# Current work: morale, line roles, light theme and 2025 coaches

Status: IMPLEMENTED, validation/release IN PROGRESS. Branch game-day-2-test. Do not repeat Build 285's completed work.

Requested by user: morale happiness icons in both detailed profiles and league rosters; league development deltas; OT/OG/OC mapping 5/9,6/8,7; light theme; graduates becoming coaches, coach retirement; real 2025 HC/OC/DC/ST data matching NEXT26. Scouts/support generated. Retention unchanged; save-size question answered in commentary (small departure records; season histories and multiple snapshots dominate).

Implemented this task (uncommitted as of checkpoint):
- Morale face drawable and accessible numeric/mood label on both profiles, icons in user/league tables; league OVR deltas.
- OL source codes preserved; generated role assignment; 2 OT/2 OG/1 OC line selection and role-group depth arrows/drag. Internal OL enum retained to preserve engine/recruiting counts.
- Dark/light preference, shared surfaces/contrast and state-preserving root rebuild. Needs cloud screenshot review.
- Annual AI/support retirement from age 62, increasing chance, by 80. Human head coach exempt from automatic retirement. Persistent retirement and staff movement history. Leadership/awareness/discipline snapshots on departures. Eligible graduates (75/75/60, 25% deterministic career interest, 6-20 years apprenticeship) may replace retiring coordinators in positional phase. Linked former-player identity, modest initial ratings, no instant graduate HC/medical jobs.
- 2025 identity seed CSV covers 138 actual bundled team names and four game role slots. Real coordinator/assistant titles and shared-role names retained; six HC-run OC slots labeled game assignments, not invented OC titles. Analysts filling ST slots retain analyst title. Human HC remains YOU. Ages/ratings/portraits are generated game estimates, not asserted real data. Seed only applied during new dynasty creation; loads/evolved dynasties untouched.
- Research: official archives often silently contain 2026 hires or 2025 interims; cross-checked season-specific team pages and manually curated overrides (docs/data). Primary sources and secondary 2025 season URLs retained in CSV; don't claim every entry independently verified against a frozen primary source. Research caches under .artifacts are ignored; build/*2025_coaches.py research helpers require bs4 and cached discovery data.

Validation so far: 592 regular core tests passed before final UI-control/movement tweaks; Android Compile succeeded before those tweaks, pending final rerun. Nine Python smoke helper tests had Windows sandbox temp-directory permissions errors; rerun escalated. Expanded cloud smoke added light/dark state preservation, enabled controls, player morale in portrait/landscape. Need run GitHub Actions APK + emulator, inspect screenshots, fix errors, provide artifact link and update PR19/handoff. No new build published yet. sources/ untouched.

Prior completed release reference follows:

# Current work: completed after Build 281

Status: COMPLETE. All items requested for this update have been implemented and validated. No pending code work or emulator sessions.

Branch: game-day-2-test. Final source commit d69ed41; later handoff commit only updates this file. PR19 remains open; do not merge without a user request.
Final APK: Build 285, https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/39778190764/artifacts/12425548152
Run: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/39778190764

Implemented:
- Recorded rushing/YAC broken tackles and smooth outcome-driven carrier cuts sharing the ball path. This is playback of resolved outcomes, not a new real-time collision simulator.
- Subtle player names default on; one label can identify a nearby defender late in the play. Users can disable labels. Scrimmage gains of 20+ yards replay; kickoff replays still require crossing midfield.
- Phase-aware recruiting transactions and Scout weekly priority shortcuts placed above card metrics.
- Player morale and persisted start-of-season OVR deltas on profiles and rosters. Staff-led automatic training at weeks 3/6/9/12, using role, age, potential, injury and morale, with restrained actual NCAA-derived rating movement. Filler generation also accounts for class year; real imported attributes are unchanged.
- Existing elite-junior early pro declarations verified. Persistent team/season departures include graduation, pro declarations, portal entries, manual releases and automatic roster rebalancing. Departed player profiles remain accessible.
- Persistent team/national Hall of Fame based on original Java career/honors criteria, adapted to recorded stats. No invented imported career production; at least two recorded seasons for average-score qualification; OL depends on honors; specialists use conservative efficiency weights.
- Both play editors lock starting alignments to formation/package while allowing tasks/routes. Seven on-line players with uncovered eligible ends and off-line receivers, shared by engine reads, art and viewer. Formation changes update appropriate personnel.
- Shared toolbar wrapping, wider landscape tables, compact roster filters, accessible morale placement and corrected roster sort reset/latest-development display ordering.

Validation:
- Final Build 285: 582 regular core tests, nine smoke helper checks, APK packaging and complete expanded cloud Android smoke passed.
- Three five-year 12-team career cases rerun successfully after final history changes. Build 282 passed all 587 then-current tests, including all six five/ten-year cases (three 130-team ten-year cases). Subsequent edits changed UI/history only, not simulation/development outcomes.
- Generated 750-player cohort: mean 69.1 at entry -> 71.8 by senior season; 505/750 improved; 3.7% finished 90+. Generation/filler age/prestige, replay thresholds, formation legality, morale, saved baselines, Hall of Fame and departures have regression coverage.
- Cloud UI covered creation/cancel, current-week game entry, three portrait calls, overlay selection/Back, names default-on, replay clock/stat/selection stability, timeout banks, live box scores, rotation, Watch pause/resume, actual week advancement, roster deltas/morale, seasonal scouting, departures and Hall of Fame. Final roster/recruit screenshots visually reviewed.
- No local emulator run for this update; cloud emulator was used. sources/ remains read-only and untouched.

Automation: resume-unfinished-cfdm-update was enabled hourly for this update and removed on completion. Never redeem reset credits automatically. Create a fresh scoped continuation only if the user asks for another update/restart.
