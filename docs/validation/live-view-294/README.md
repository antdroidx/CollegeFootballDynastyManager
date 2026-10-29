# Live viewer clarity and game flow — Build 294

Source: 8f5c1a5 on game-day-2-test. Main stays unmerged. Sources/ untouched.

## Changes

- Live action controls use a dedicated row below the field. Between-play cards remain accessible; More expands utilities without laying a bar over players or yard lines. The call picker is still a deliberate between-play overlay.
- Play-picker surfaces and card text follow the selected theme. Replay banners use an opaque dark surface with gold text; team highlight text chooses black/white based on the team background. Live box-score tab labels also follow the theme.
- Outdoor rain/snow is drawn on the field and animated during plays using recorded game weather. Indoor games stay clear. Existing wind/wet-weather catching, fumbling and kicking modifiers remain; this is fictional seeded weather, not a forecast.
- A retro animated coin toss precedes Play/Watch. Away calls heads/tails when controlled; CPU calls heads and defers when it wins. A user-controlled toss winner may receive or defer. Simulate applies the same deterministic default toss without a dialog. A separate RNG keeps toss presentation from consuming play randomness. The other team receives the second-half kickoff.
- Lost fumbles and interceptions can be downed or returned, including defensive touchdowns. Attributes and opposing pursuit influence return yardage. Interception pickup depth follows quick/deep passing concepts; it is an abstract result, not a geometric collision simulation. Return ball/owner animation follows the recovering defender, with the recorded tackler pursuing. Return yards consume live clock. End-zone interceptions can become touchbacks.
- Defensive scores, PAT/kickoff rights, overtime finishes, period totals, commentary and morale use the defensive team. Defensive return yards/TDs are retained and shown in defense statistics, including career aggregation. Old replay events without turnover metadata retain their old presentation.

## Validation

Local: 619 regular core tests, three complete five-season 12-team dynasty careers, nine Python helpers, resource checks and Android compile passed (six existing warnings). New core coverage includes repeatable tosses, random-stream isolation, halftime receivers, variable returns, defensive TD/PAT scoring, touchbacks, overtime, return ability, serialization and ball ownership/commentary. Existing assertions that assumed Home always received first now select the appropriate team.

GitHub run: https://github.com/antdroidx/Football-Sim-Engine/actions/runs/39904787868
GitHub APK packaging and the complete Android emulator run passed. Smoke handles coin-toss choices and asserts that controls are below the field in landscape; existing live play/watch/replay/box scores, week advancement, recruiting, depth chart/Auto Set, careers and light/dark theme flows also pass.

[Download Build 294 APK](https://github.com/antdroidx/Football-Sim-Engine/actions/runs/39904787868/artifacts/12480970226). Evidence artifact: android-launch-ui-294. Portrait/landscape viewer, coin-result and expanded play-picker screenshots were reviewed. The recorded live game was clear weather; rain/snow graphics were implemented but not demonstrated by that emulator game. Light-theme smoke covers general theme/profile flows; live popup contrast fixes are explicit surfaces/text choices, not a separate captured light-game run.

The long-term rating calibration parameters are unchanged; this update does not claim a new full-FBS statistical calibration of defensive return frequency. Sprite return choreography remains an abstract visual presentation of the engine result.
