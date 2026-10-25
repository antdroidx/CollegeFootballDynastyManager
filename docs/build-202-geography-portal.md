# Recruit geography and Week 19 portal processing

## Fixes

The HS factory mapped alphabetical state indexes into groups of ten. Display/filter regions trusted those values, and proximity calculations could also read the incorrect stored region. A shared explicit 50-state map now drives generation, display, region filters and school proximity. The five broad regions are West, Plains, Midwest, East and South, preserving the numeric west-to-east league layout. Georgia and Arkansas are South. Old saves retain recruit identities and states; regions are repaired during load, including the pre-schema-9 state-in-region representation.

Week 19's portal finalization evaluated roster needs from scratch for each candidate after each signing, repeatedly scanning rosters and league staff. It also recalculated static school-fit scores on every iteration. Team roster plans now group positional depth once per roster change, and school-fit scores are calculated once per team. Weekly HS CPU recruiting also reuses static fit scores. No transfer quota or new signing limit is introduced.

Interactive recruiting resolution and weekly CPU recruiting now run off the UI thread. Portal/final-class processing reports the team being evaluated and advances the progress indicator instead of leaving the same 22% message throughout.

## Validation

- 160 core/data tests pass, including all 50 states, generated pools, schema-8/schema-17 save repair and roster-plan single-enumeration coverage.
- Full-league Week 19 portal workload: 130 teams, 8,450 roster players, 2,000 transfers, approximately 0.71 seconds for CPU finalization, or 1.33 seconds including 30 active user offers and the transition into Week 20, locally in Release. Checks progress for all teams, roster maximums and duplicate player prevention. This is a synthetic desktop workload, not a phone measurement or the user's actual save.
- Android build and GitHub APK run are checked separately.
