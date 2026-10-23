# Current work: COMPLETE — Build 293

Position-based depth chart update is complete on game-day-2-test. Source: 73185da. Main remains unmerged; PR19 is open. No automation or further scoped work is pending.

APK: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/39892892689/artifacts/12475677405
Successful run: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/39892892689
Report: docs/validation/depth-chart-293/README.md

Implemented: numerical NCAA PPOS position picker; one group at a time; responsive depth/candidate panels, search, arrows and drag/drop; out-of-position calibrated OVR; saved assignments that preserve natural position and attributes. Each player has one assigned playing position. Auto Set can rebuild the team using positional quality, scheme and coordinator expertise, or sort only the selected group. Game-day/weekly/postseason simulation and roster team profiles consume assignments. Injured line backups stay on their assigned side, specialist stats use actual kicker/punter, departed assignments are pruned. Natural roster display starter/depth status respects the projected chart. Historical replay personnel remain historical.

Validation: 610 regular core tests, three complete five-season 12-team dynasty careers, nine Python helpers, resource checks and Android compilation passed. Build 293 APK and full emulator flow passed, including organizer opening, Auto Set, position change and rotation, and existing gameplay/recruiting/career/theme flows. Portrait/landscape screenshots reviewed; wide-screen cards require scrolling below the header. Six existing Android compile warnings. Build 292 exposed an AutomationId redraw crash, fixed in 293 by initializing the ID only once.

Build 291 OVR calibration and long-term evidence remain unchanged in docs/validation/overall-calibration-291/README.md. No generation/development/calibration parameter changes in this update. Sources/ untouched.
