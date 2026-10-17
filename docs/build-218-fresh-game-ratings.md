# Build 218: fresh-game ratings foundation

This build starts a new save format (schema 23). Other save formats show an explicit start-new-dynasty message. They are not deleted. The save loader no longer migrates geography, recruiting interactions, player ratings or coach history, and current-format saves deserialize without repair passes.

Initial rosters, signees and walk-ons receive complete detailed profiles at creation. Input talent/quality seeds independent skill draws rather than an overall to preserve. Relevant position skills have higher baselines and non-specialist skills lower baselines; individual skill spread is independent of the old broad Technique rating. Generated identities retain their recorded attributes through saves and transfers.

Passing and rushing performance are calculated directly from relevant football skills (65%), position-specific physical traits (20%), awareness (10%) and stamina/durability/leadership/discipline (5%). Stored overall does not feed performance back into itself. Overall combines passing/rushing ability with position weights: QB/WR emphasize passing, RB emphasizes rushing, defensive backs emphasize coverage, and linemen balance the relevant blocking/front roles. Kicking uses kick power/accuracy.

Practice produces deterministic but independent changes by skill, year and midseason/offseason stage. Position skills receive the full practice change; non-position skills progress more slowly. Leadership and discipline change slowly in offseason and remain stable at midseason. Physical development and injury effects still affect the relevant athletic attributes/skills; potential ceilings apply to the resulting complete profile. The prior duplicate development coupling and six-rating overall correction have been removed.

This remains a drive-level engine. Individual snap events for fatigue, penalties, catching and fumbles are still future work. Technique remains a broad training/injury attribute displayed on the profile; it is no longer the substitute for position skills in overall or team performance.

Validation is in progress: focused regressions pass; full career health, Android packaging and fresh-dynasty emulator checks are running.
