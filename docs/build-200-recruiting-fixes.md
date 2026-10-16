# Build 200 recruiting follow-up

Baseline: successful GitHub workflow Build 200, commit `8a264cdab1180ecbda2f844a45e47af5f0c95b97` on `build-196-recruit-interest`.

## Recruit page performance

Interest sorting previously scanned the HS pool repeatedly for each recruit's position, prestige floor and home region, and scanned the league roster for playing-time grades. Weakly held indexes now reuse each immutable pool/roster snapshot, and geography filtering uses a lookup. Replacing either collection refreshes the corresponding index; old snapshots can be collected. No save-format change.

The 10,000-recruit regression test checks one pool enumeration across all interest calculations, bounded interests, and refreshed pool/roster results. Device page-open latency has not been measured.

## Assisted commitment timing

CPU teams and assisted user HS signings now share the same weekly maximum: 16%, 20%, 25%, 33%, 45%, then 100% of remaining roster capacity (rounded up). With 38 openings and enough successful recruits this gives 7, 7, 6, 6, 6, 6 signings. Existing interest, competition, targeting, scouting, pitch and workload rules still determine who signs; the allowance is a ceiling, not guaranteed signings. Manual recruits and transfers keep their existing behavior.

Commitment records reconstruct the current week's used allowance, including after repeated resolution or save/reload. No new state fields are required. Existing early commitments are not undone.

## Validation

- 148 core/data tests pass in Release, including new six-week cadence, same-week repeat, manual recruiting, large-pool and cache-refresh regressions.
- Packaged resource check passes.
- Debug Android build succeeds with zero errors and five existing obsolete-API warnings. No emulator/device validation was performed.
