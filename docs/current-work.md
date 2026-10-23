# Current work: COMPLETE — Build 294

The requested live-view clarity and game-flow update is complete on game-day-2-test. Application source: 8f5c1a5. Main remains unmerged; PR19 is open. No scoped work or automation is pending.

APK: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/39904787868/artifacts/12480970226
Successful workflow: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/39904787868
Report: docs/validation/live-view-294/README.md

Controls sit below the live field, with More utilities and between-play cards. Play-card/picker surfaces respect light/dark themes; replay banners use an opaque dark surface and team highlights choose readable ink. Outdoor rain/snow is drawn and animated during plays from existing seeded weather.

Retro coin toss precedes Play/Watch; away calls, CPU defers and controlled winners can receive/defer. Simulate applies the same seeded default without UI, using an independent random stream. Opposite team receives at halftime.

Lost fumbles/interceptions can be downed or returned, including defensive TDs. Return carrier and ball move together; a recorded offensive tackler pursues. Attributes influence returns; interception depth is concept-based and animation remains result-driven rather than a collision simulation. Corrected possession coordinates, touchbacks, defensive scoring, PAT/kickoff rights, overtime endings, period totals, retained defensive return stats/career aggregation, commentary and morale. Old highlight events without return metadata keep prior presentation.

Validation: 619 regular core tests, three full five-season 12-team careers, nine Python helpers, resources and Android compile passed; six existing warnings. GitHub APK build and full emulator flow passed, including coin toss and a non-overlap assertion for field/controls. Viewer portrait/landscape, coin-result and expanded-picker screenshots reviewed. The emulator game had clear weather; no captured rain/snow game or separate light live-popup flow. General light/dark profile/theme flows passed. No new statistical calibration of return frequency is claimed.

Build 293 depth chart and Build 291 OVR calibration remain intact. Ratings/generation/development parameters unchanged. Sources/ untouched.
