# GameCast visual polish — implementation plan

Base: feature/management-ui-overhaul (not legacy Java main). Primary UI: src/DynastyManager.App/FoundationPage.GameDay.cs (MAUI), with src/DynastyManager.App/FoundationPage.LiveBoxScore.cs, src/DynastyManager.Core/Simulation/GameDayLivePresentation.cs and LiveFootballChoreography.cs.

## Scope
- Preserve existing cartoon sprites, field orientation, team-initial logos, game modes, snap events, replay and saved dynasty behavior.
- Replace oversized top header metric cards with compact scoreboard showing ranked teams, score, possession, quarter/clock, down-distance, field position, and timeouts. Move weather/morale into secondary details. Keep box score accessible.
- Make controls responsive: portrait bottom control row or sheet; landscape compact overlay outside the playable field. Never cover players or the field during live play.
- Improve label collision: show only ball carrier/passer/target/selected player, with background or outline for contrast; keep optional player-label preference.
- Add follow-ball camera as opt-in while retaining full-field default, ensure 22 players remain visible in tactical view and never crop active ball trajectory.
- Replay overlay with outcome, slow motion, replay/skip controls, preserving existing instant replay toggle.
- Follow existing DynastyTheme light/dark settings and accessible touch target sizes.
- Verify blue LOS/yellow line-to-gain positions and correct possession direction; no invented game-state calculations.
- No changes to game engine physics, ratings, recruitment, or save schemas.

## Sequence
1. Extract compact scoreboard component and add it to GameDay viewer, preserving existing functionality.
2. Responsive game field and controls, compact/expanded layouts.
3. Player-label legibility and replay overlay.
4. Camera modes after motion correctness tests.
5. Build Android APK via existing GitHub Actions; run GameDayLivePolishTests and GameDayLiveBoxAndPursuitTests.

## Acceptance
- Correct scoreboard across Q1–OT, possession changes, 1st–4th down, goal-to-go, penalties, timeouts, replays, and final.
- No field obstruction on compact phones, Fold unfolded, and landscape.
- Watch CPU, Take over, Timeout, Skip to end, Box score continue working.
- Existing installed app updates without uninstall; saved dynasties load.
- Tests and APK build pass before merging.
