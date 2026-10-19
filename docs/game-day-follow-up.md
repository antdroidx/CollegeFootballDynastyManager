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

The first game-day expansion adds `PassingReadService`: selected package blockers and rushers establish protection; assigned routes, receiver and defender attributes, leverage, QB awareness and staff game management select a read and determine completion/interception risk. The recorded release timing feeds the viewer. Drops are distinct from ordinary incompletions. This remains a probabilistic matchup model: there are no physical collisions, time-stepped separation calculations or true sequential progression simulation yet.

Save schema remains 25; Build 246 dynasties can continue.

## Recommendations

Offensive advice uses weighted coach tendencies, down/distance, field position and the last four actual calls. Individual recently used plays receive less weight; concept weights are normalized so the size of the passing catalog does not erase run preferences. A separate deterministic stream keeps advice stable during repeated UI refreshes and never advances simulation randomness. Only the active playbook's scrimmage plays are suggested.

Fourth-down advice can recommend a punt, a field goal, or a short-yardage attempt. Score/time context avoids punting or settling for a field goal when a touchdown is needed late. Expiring-half situations can suggest a field goal before fourth down. Kicking advice uses the same selected kicker, distance, accuracy and power calculation as the kick resolver. The recommendation card's main button executes the displayed action; manual plays and kicking shortcuts remain available.

CPU play selection now uses situational advice outside the opening script. The shared snap probabilities now incorporate the passing-read layer.

## Rules and scoring expansion

Touchdowns record six points and queue a separate conversion. Users can choose an XP or two-point try, except when overtime requires two. XP attempts/makes come from actual conversion events. Drives sum actual recorded scoring; penalties, kickoffs and conversions do not inflate scrimmage attempts.

Tied regulation starts untimed possessions at the opponent 25. Both teams receive a possession, first possession alternates each round, second overtime requires two-point attempts after touchdowns, and third onward alternates two-point tries. Timeouts follow the 2025 NCAA third-OT shared allowance. The old 180-event cutoff and fabricated tie-breaking field goal are removed.

Onside kicks resolve recovery and possession; a user can request their next kickoff be onside. CPU teams consider onside kicks when trailing late. This first onside implementation has commentary but no detailed recovery animation. Pre-snap false start/offside enforcement is automatic, and the down is replayed unless the defensive penalty gains a first down. Live-ball penalties, acceptance choices, safeties, conversion returns and full clock-rule coverage remain future work.

## Editor and import

Coach → Playbook Editor → Edit / copy creates persisted play definitions. Save to the library, select the new call with the star, then save the Custom books. Existing 17 offense / 14 defense active-book limits apply to created and catalog calls together. Editing uses slot alignment sliders and route families / block or rush / man / zone assignments; freehand route drawing is still pending. Custom assignments are used in diagrams, live alignments, route animation and passing-read matchups.

Settings → Import NCAA 06 NEXT roster (CSV) imports before the first season starts. See `NCAA06-NEXT-ratings.md` for the explicit input contract. No direct memory-card or raw database loader is included.
