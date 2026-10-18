# Build 223: recruit clarity, league standings and play art

Recruit detail now separates identity, evaluation, interest and the active market. Four compact metrics show scouted overall, potential, user interest and the best offered rival; scholarship offers and revealed priorities sit in dedicated panels; favorites and recruiting actions remain available without forming one dense paragraph. Native Android picker/search underlines, explicit commitment separators and always-visible table scrollbars have been removed.

Conference standings include a conference selector populated from the current league, so the user can inspect every conference race, championship matchup and champion rather than only the user team's conference.

The play library now pairs each call with field art. Offensive diagrams distinguish run and pass concepts and defensive diagrams distinguish pressure and coverage approaches, while the play name, personnel, concept and description remain visible beside the art.

## Game Day sequence

- Build 224: create one shared snap-event model with clock, down, distance, field position, possession, selected concepts and results; expose it through a simulated Game Day broadcast with pause and speed controls.
- Build 225: add live offensive and defensive play selection on top of those same snap events, including situational suggestions, timeouts and fourth-down decisions.
- Later iterations: richer assignments in the play editor, substitutions, fatigue, penalties and presentation polish.

## Validation

- Build the Android application and run the full core test suite.
- Launch the packaged APK on the API 36 emulator.
- Confirm conference switching, recruit detail wrapping and play-art rendering in the Android UI.
