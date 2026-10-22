# Current work: COMPLETE — Build 287

Status: COMPLETE. User requested stopping to conserve usage; do not start further work. No automation was created. Branch game-day-2-test, final source c3e1f9b. PR19 remains open; do not merge without a user request.

APK: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/39819151583/artifacts/12443442516
Successful run: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/39819151583

Validation: Build 287 passed 593 regular core tests, nine smoke helpers, APK packaging and complete Android smoke including Light/Dark transitions, unchanged unsaved calendar, enabled dynasty controls and morale in portrait/landscape. Light player-profile screenshot visually reviewed. Build 286 passed all 598 then-current tests including all six five/ten-year career cases, but exposed a theme-switch crash. Final fixes defer root replacement until native picker dismissal, preserve window/navigation state and handle null default text colors. The only later simulation change protects new hires from their predecessor's firing evaluation (covered by the additional 593rd regression); career training/retirement logic is unchanged from the full health run. Final coach data corrects Southern Miss and Coastal Carolina season-opening HCs. Sources/ remains untouched.

Implemented this task (uncommitted as of checkpoint):
- Morale face drawable and accessible numeric/mood label on both profiles, icons in user/league tables; league OVR deltas.
- OL source codes preserved; generated role assignment; 2 OT/2 OG/1 OC line selection and role-group depth arrows/drag. Internal OL enum retained to preserve engine/recruiting counts.
- Dark/light preference, shared surfaces/contrast and state-preserving root rebuild. Needs cloud screenshot review.
- Annual AI/support retirement from age 62, increasing chance, by 80. Human head coach exempt from automatic retirement. Persistent retirement and staff movement history. Leadership/awareness/discipline snapshots on departures. Eligible graduates (75/75/60, 25% deterministic career interest, 6-20 years apprenticeship) may replace retiring coordinators in positional phase. Linked former-player identity, modest initial ratings, no instant graduate HC/medical jobs.
- 2025 identity seed CSV covers 138 actual bundled team names and four game role slots. Real coordinator/assistant titles and shared-role names retained; six HC-run OC slots labeled game assignments, not invented OC titles. Analysts filling ST slots retain analyst title. Human HC remains YOU. Ages/ratings/portraits are generated game estimates, not asserted real data. Seed only applied during new dynasty creation; loads/evolved dynasties untouched.
- Research: official archives often silently contain 2026 hires or 2025 interims; cross-checked season-specific team pages and manually curated overrides (docs/data). Primary sources and secondary 2025 season URLs retained in CSV; don't claim every entry independently verified against a frozen primary source. Research caches under .artifacts are ignored; build/*2025_coaches.py research helpers require bs4 and cached discovery data.

