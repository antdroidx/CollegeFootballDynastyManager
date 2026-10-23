# Current work: COMPLETE — Build 298

Latest requested deep-coverage/kickoff/on-field presentation update is complete on game-day-2-test. Application source: 59d1cae; Android smoke correction: 49f9b74. Main remains unmerged; PR19 is open. No scoped work or automation is pending.

APK: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/39913553559/artifacts/12483159389
Successful workflow: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/39913553559
Report: docs/validation/coverage-kickoffs-298/README.md

Fixed DBs stopping on zero-yard incomplete deep passes: non-rushing coverage follows receiver routes with man/zone lateral behavior and catch-to-pursuit transitions. Outside-WR coverage chooses nearby corners instead of cycling through linebackers. Explicit Man/Zone assignments override names, and Cover 0/1 or Cover 2 Man are not mislabeled zone. Breakup defenders track the actual intended target. Hail Mary keeps deep difficulty but reduces its extra blanket penalty on open targets; inaccurate passes remain.

Kickoffs use spread receiving/coverage formations with unit movement. Seeded kick-power/wind touchbacks retain the ball at the end-zone landing location during notification, announce TOUCHBACK and the receiving team's next spot (kickoff 25 / punt 20), and exclude return attempts/yards and clock runoff. Box Score is larger, bold and accented. Coin toss is over the field with captains/referee in both layouts and an aligned landscape midfield camera. Decorative pixel referee/sideline officials appear live; no new official AI or penalty rules were added.

Validation: 630 regular core tests, three five-season 12-team dynasty careers, nine Python helpers, resources, Android compile and APK build pass; six existing Android warnings. Full Build 298 emulator smoke passes, including on-field toss/rotation and existing live/watch/replay/box-score/week/recruiting/depth/Auto Set/career/theme flows. Screenshots reviewed for both coin layouts, live officials and Box Score. Final smoke exercised a returned kick; Build 297 captured a correct touchback/empty return category but failed its old return-required assertion, corrected in 298. Core tests verify touchbacks. Deep-route geometry tests cover both directions and man/zone, not every play's visual appearance. Viewer remains result-driven animation, not collision simulation.

Builds 295/296 were superseded during final source review. Prior Build 294 returns/weather/contrast/coin logic, Build 293 depth chart and Build 291 OVR calibration remain. Generation/development curves unchanged. Sources/ untouched.
