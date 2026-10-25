# Build 221: playbook foundation and UI polish

Player profiles now use durability as the single availability trait. Character, physical and skill groups use clean label/rating rows without the full data-table row bands. The depth-chart organizer shows each player's class year, and the active-dynasty header describes its save-and-return action as **Exit Dynasty**.

Recruit cards separate identity from four glanceable metrics: rank/rating, scouted projection, origin and scouting progress. User interest and scholarship state are displayed separately from the competing-offer market, followed by a dedicated action row. The card retains the existing factual recruiting data and multi-school offer behavior while reducing dense inline text.

The Coaching Hub now stores a primary offensive and defensive formation with each team game plan. The initial catalog contains twelve offensive plays across Shotgun Spread, Pistol, Singleback, I-Formation and Empty, plus eight defensive calls across 4-3, 3-4, Nickel, Dime and Goal Line. Each definition records formation, personnel, concept or approach, and a plain-language assignment summary. Players can browse the selected formation and star up to twenty calls for their working playbook; formation and favorite selections persist with the dynasty.

This is the data and navigation foundation for the visual play editor. The catalog does not yet model draggable alignment points, routes, blocking assignments, coverage zones or snap outcomes. Those enter through the assignment editor and snap-event engine rather than receiving hidden play bonuses in the existing drive simulator.

## Validation

- The full Release suite covers formation/favorite persistence, duplicate normalization, invalid play rejection, catalog coverage and all prior dynasty simulations.
- The .NET 10 MAUI Android application compiles with the new play-library UI.
- GitHub packages the Build 221 APK and validates launch on the API 36 emulator.
