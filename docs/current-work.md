# Current work: COMPLETE — Build 291

The comprehensive OVR recalibration is complete on game-day-2-test. Final application source: 2837732. Main remains unmerged; PR19 is open. No automation is active and no further work is requested.

APK: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/39886044927/artifacts/12473218579
Successful workflow: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/39886044927
Full report and retained JSON: docs/validation/overall-calibration-291/README.md

Validation passed: 608 core tests, nine Python smoke helpers, packaged resources, Android APK build and complete emulator smoke. The core suite includes two 20-season FBS lifecycle seeds, two complete 20-season 12-team game/stats/save careers and three five-season career cases. Android smoke covers live play/watch/replay/box scores, week advancement, recruiting, roster/departures/Hall of Fame, theme/state preservation and both orientations. Light landscape player-profile screenshot reviewed. Six existing Android compile warnings, zero errors.

Starting NEXT26 roster: mean 72.73, starters 79.16, 90+ 1.34%, 95+ one player, no 99s. Season 20 across seeds 1/17: means 73.04/72.82, starters 80.77/80.59, 90+ 1.62%/1.68%, 95+ 0.14%/0.15%, no 99s at season-start checkpoints. Mean drift from season 1 was +0.47/+0.24. Elite versus weaker program differences remain. All 21 positional comparisons and annual class/star distributions are in the report/evidence.

Implementation: fixed monotonic NEXT26 curves retain corrected POCI weights. Proposed curves were refined with a two-point body lift tapering toward 99, explicit endpoints and a near-perfect weighted threshold for 99. Prior import-only global attribute drops remain. Generated recruits/fillers/walk-ons fit attributes on the shared scale; no final target-OVR overwrite. HS attribute profiles and source roles persist through signing. Stars influence overlapping potential ranges; very rare projected 99 potential allows generational athletes without guaranteeing 99 current ability. Walk-on/replacement targets rose two points; filler prestige scaling was moderated. CPU depth is reassessed after offseason training while manual user ordering remains. A one-time save-scale migration preserves established attributes, potential headroom and current display baselines, including portal/pending players. Historical snapshots retain recorded values. No annual percentile normalization is used.

Limits: the 138-school FBS tests isolate actual roster/portal/recruiting/development services without full national game schedules. Complete smaller-league careers cover games/stats/injuries and save/load. Positional cohorts and walk-on counts are disclosed rather than forced to match quotas. This does not establish realism for every custom league or scholarship/transfer volume. Sources/ was untouched.
