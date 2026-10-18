# Build 200 recruiting follow-up

Baseline: successful GitHub workflow Build 200, commit `8a264cdab1180ecbda2f844a45e47af5f0c95b97` on `build-196-recruit-interest`.

## Recruit page performance

Interest sorting previously scanned the HS pool repeatedly for each recruit's position, prestige floor and home region, and scanned the league roster for playing-time grades. Weakly held indexes now reuse each immutable pool/roster snapshot, and geography filtering uses a lookup. Replacing either collection refreshes the corresponding index; old snapshots can be collected. No save-format change.

The 10,000-recruit regression test checks one pool enumeration across all interest calculations, bounded interests, and refreshed pool/roster results. Device page-open latency has not been measured.

## Assisted commitment timing

HS commitment timing now uses a stable personal readiness percentile for each prospect. As the six-week window progresses, more recruits become ready; strong interest and a clear favorite can advance the decision. High interest no longer forces an instant commitment. Manual and assisted prospects use identical rules. There are no new team signing quotas or weekly class-size limits. CPU recruiting retains its Build 200 implementation.

Readiness is derived from the dynasty, season and prospect ID, so saving/reloading does not reroll it. Existing interest, targeting, scouting, pitch, workload and school-selection behavior remains. The final week resolves outstanding offers as before. Existing early commitments are not undone. No save-format change.

## Validation

- 148 core/data tests pass in Release, including new six-week progression, same-week repeat, manual/assisted timing parity, large-pool and cache-refresh regressions.
- Packaged resource check passes.
- Debug Android build passes after the organic timing revision: zero errors, five existing obsolete-API warnings. No emulator/device validation was performed.

## Transfer roster planning

Removed the eight-transfer maximum for CPU teams and user assistance. Each automated transfer decision now considers positional depth, immediate starter improvement, remaining eligibility, promising young competition, pending incoming transfers, and graduation concentration. A head coach's recruiting versus player-development strengths shift willingness to use the portal. This is an inferred preference from existing attributes, not a new editable coaching trait.

CPU teams reassess against the roster after each signing. Assisted offers account for other outstanding offers, and staff stop spending on targets that no longer improve the plan. Manual transfer recruiting remains under user control. The overall 85-player roster maximum remains; there is no annual transfer quota.

150 tests pass, including signing more than eight useful transfers, protecting young players, pending arrivals and coach preference differences. Long-term balance still needs multi-season gameplay observation.
