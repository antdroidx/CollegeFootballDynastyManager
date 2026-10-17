# Builds 218–219: fresh-game ratings foundation

This build starts a new save format (schema 23). Other save formats show an explicit start-new-dynasty message. They are not deleted. The app no longer rebuilds schedules/rosters, reruns recruiting assistance or replays offseason actions while loading a save. Unsupported saves show a handled dialog. The save loader no longer migrates geography, recruiting interactions, player ratings or coach history, and current-format saves deserialize without repair passes.

Initial rosters, signees and walk-ons receive complete detailed profiles at creation. Input talent/quality seeds independent skill draws rather than an overall to preserve. Relevant position skills have higher baselines and non-specialist skills lower baselines; individual skill spread is independent of the old broad Technique rating. Generated identities retain their recorded attributes through saves and transfers.

Passing and rushing performance are calculated directly from relevant football skills (65%), position-specific physical traits (20%), awareness (10%) and stamina/durability/leadership/discipline (5%). Stored overall does not feed performance back into itself. Overall combines passing/rushing ability with position weights: QB/WR emphasize passing, RB emphasizes rushing, defensive backs emphasize coverage, and linemen balance the relevant blocking/front roles. Kicking uses kick power/accuracy.

Practice produces deterministic but independent changes by skill, year and midseason/offseason stage. Position skills receive the full practice change; non-position skills progress more slowly. Leadership and discipline change slowly in offseason and remain stable at midseason. Physical development and injury effects still affect the relevant athletic attributes/skills; potential ceilings apply to the resulting complete profile. The prior duplicate development coupling and six-rating overall correction have been removed.

This remains a drive-level engine. Individual snap events for fatigue, penalties, catching and fumbles are still future work. Technique remains a broad training/injury attribute displayed on the profile; it is no longer the substitute for position skills in overall or team performance.

Validation: all 231 Release tests passed, including 225 focused regressions and six multi-year career simulations. Android Debug APK 2.0.219 (code 619) and packaged-resource checks passed. On the isolated emulator, a new 138-team Modern FBS dynasty generated and persisted all 11,730 complete player profiles. The entire JSON dynasty snapshot was identical after load, player-card viewing and re-save. The corrected unsupported-save dialog displayed and dismissed normally, with the app alive and the final crash buffer empty. GitHub Build 219 is running independent tests, packaging and emulator smoke validation.

The initial Build 218 manual load-boundary check caught an uncaught unsupported-save exception in the UI; Build 219 handles that exception and removes load-time reconstruction/replay. Its corrected path was explicitly retested. The test dynasty/database was disposable and the emulator's original database was restored afterward.

## Screenshots

- [Fresh player profile](ui/build-219-fresh-player-card.png)
- [Retired format message](ui/build-219-final-retired-save-dialog.png)
