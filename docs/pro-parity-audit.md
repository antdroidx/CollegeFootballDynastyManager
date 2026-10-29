# Pro application parity audit — October 25, 2026

The pro application is still a development preview, not a completed adaptation of the college application. This checklist distinguishes implemented changes from remaining work; a successful build alone does not demonstrate UI or animation correctness.

| Area | Current implementation / remaining gap |
| --- | --- |
| Portraits | College `PersonPortrait` renderer moved into shared presentation unchanged except public visibility; pro roster, player profiles and staff profiles consume it. Illustrated likenesses are generated, not photographs. |
| Player cards | Navigable profile page with identity, physical attributes, rating bars, availability, morale, contract years and regular-season totals. All 19 college attributes and the same responsive attribute tiles are now shared; overall is recalculated by the college rating service. Portrait editing and full career pages remain to port. |
| Staff | Four roles, illustrated profiles and offseason hire/dismiss actions. Saved offense/defense/tempo game plans feed the shared simulation. Development controls and user job market still need full college parity. |
| News | Actual simulated game result reports and transaction feed, accessible through League and Overview. Full college editorial/news taxonomy still pending. |
| Statistics | Regular-season passing/rushing/receiving/tackle/sack leaders, player links and game report pages. Career record books and advanced sorting still pending. |
| GameCast | Both hosts now call the extracted college animation routine, including blocking, coverage, tackles, kick/return paths, pass timing and turnover returns. Camera transforms, broadcast scoreboard, illustrated playbook cards and detailed box-score page are shared. Pro supports watch viewing options, automatic/skip replay and return to watching; visual validation is required after extraction. Remaining presentation work includes full college tray/quick-call parity and highlight shows. |
| Finances | Optional OTC/nflverse snapshot matches GSIS ID, team and 2026 season; 1,981 contract matches. Exact yearly base pay, cap charge, bonus and guarantees imported in dollars. Unmatched contracts remain estimates. Team rollover, adjustments and historical dead money are not imported, so displayed cap room is provisional. |
| Saves | New optional fields have defaults; existing saves are not automatically replaced with sourced contracts. Independent package/save storage retained. |
| Roster | Team selection, player search, position filters, portraits and a position-based persistent depth chart are present. Live substitutions remain separate. College split profiles and advanced roster actions need fuller reuse. |

## Contract provenance

See `nfl-contract-data.json`. Source is the current Parquet asset, not the CSV asset last updated in 2022. Provider monetary values are millions of dollars; the import converts to integer dollars. A sourced yearly cap charge is not interchangeable with base salary or annual contract value. Snapshot application is an explicit new-franchise choice; the default core API retains estimated finances for deterministic existing simulations/tests.

The 2026 base league cap is $301,200,000 ([NFL Football Operations](https://operations.nfl.com/calendar-events/nfl-free-agency/nfl-salary-cap)). Team-specific adjusted caps and obligations must be validated before claiming financial realism. The snapshot does not implement the full CBA or all release/trade timing exceptions.

## Shared rating model

The pro application no longer displays vendor rating labels. Import provenance remains in the data manifest; imported attributes provide inputs to the game's own rating system. `PlayerRatingService.CalculateOverall` is the single overall calculation used by college, pro rosters, generated prospects, imported classes and pro offseason progression. All 19 ratings use the canonical Character/Physical/Skill attribute catalog and the college responsive tile component. Pro-specific aging and college-to-pro attribute translation remain distinct lifecycle rules.

Pro saves add `RatingSystemVersion` independently of their existing schema version. Version-zero saves are recalculated once from their existing attributes; subsequent loads are idempotent. Contracts and historical game results are not rewritten. Overall numbers can change when an old pro save first adopts the shared formula. The college formula and college save schema are unchanged.
