# Game-day follow-up after Build 246

- Overview, matchup hub and game entry enforce the current week/year and playable season phase. Real Week 0 games remain valid after preseason.
- Interceptions choose a coverage player from the actual package. Ball flight, defender movement and the possession outline share one catch point/time; there is no unrecorded interception return.
- Users can punt or kick on any offensive down. Field-goal probability uses distance and the kicker's accuracy/power; a typical 25-yard attempt is 98.5%, not guaranteed. Misses receive an explicit banner.
- Quarter endings, halftime, overtime and final scores receive banners after the snap/outcome animation.
- Landscape uses a compact scoreboard, field on the left and play/controls on the right. The field stays visible while secondary controls can scroll. Portrait retains the stacked layout; rotation reparents the same views safely.
- Receivers receive complementary assignments by concept and role. Art and live motion share these assignments; all-go plays deliberately retain vertical routes.

## Simulation behavior

`GameDayEngine` powers Sim, Watch, Play, weekly advancement, conference championships, bowls and playoff games. The same event-to-stat builders record all modes. With the same seed, roster, CPU decisions and timeout delegation, the modes produce identical results/stat lines; manual calls can change them.

Team profiles include roster and staff quality. Snap resolution additionally uses the selected formation/personnel matchup and participating QB, blocking, receiving, running and defensive attributes. Light defensive packages improve pass coverage while weakening run support. Fractional yard advantages use stochastic rounding so small rating/home-field differences aren't erased by integer rounding.

These are probabilistic modifiers. The viewer does not calculate physical blocking collisions, receiver separation or real-time QB progression reads. Route variety is a shared presentation assignment, not a spatial physics engine.

Save schema remains 25; Build 246 dynasties can continue.
