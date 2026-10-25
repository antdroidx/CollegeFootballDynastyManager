# Pro application parity audit — October 25, 2026

The pro application is still a development preview, not a completed adaptation of the college application. This checklist distinguishes implemented changes from remaining work; a successful build alone does not demonstrate UI or animation correctness.

| Area | Current implementation / remaining gap |
| --- | --- |
| Portraits | College `PersonPortrait` renderer moved into shared presentation unchanged except public visibility; pro roster, player profiles and staff profiles consume it. Illustrated likenesses are generated, not photographs. |
| Player cards | Navigable profile page with identity, physical attributes, rating bars, availability, morale, contract years and regular-season totals. College portrait editing and full career pages remain to port. |
| Staff | Four roles, illustrated profiles and offseason hire/dismiss actions. Saved offense/defense/tempo game plans feed the shared simulation. Development controls and user job market still need full college parity. |
| News | Actual simulated game result reports and transaction feed, accessible through League and Overview. Full college editorial/news taxonomy still pending. |
| Statistics | Regular-season passing/rushing/receiving/tackle/sack leaders, player links and game report pages. Career record books and advanced sorting still pending. |
| GameCast | Shared sprites/field/planner; corrected ball attachment to sampled player path; passing flight has explicit release/catch timing. Pro host still needs extraction of the complete college live presentation and camera/replay controls. Special teams, turnovers and contact sequences require visual validation. |
| Finances | Optional OTC/nflverse snapshot matches GSIS ID, team and 2026 season; 1,981 contract matches. Exact yearly base pay, cap charge, bonus and guarantees imported in dollars. Unmatched contracts remain estimates. Team rollover, adjustments and historical dead money are not imported, so displayed cap room is provisional. |
| Saves | New optional fields have defaults; existing saves are not automatically replaced with sourced contracts. Independent package/save storage retained. |
| Roster | Team selection, player search, position filters, portraits and a position-based persistent depth chart are present. Live substitutions remain separate. College split profiles and advanced roster actions need fuller reuse. |

## Contract provenance

See `nfl-contract-data.json`. Source is the current Parquet asset, not the CSV asset last updated in 2022. Provider monetary values are millions of dollars; the import converts to integer dollars. A sourced yearly cap charge is not interchangeable with base salary or annual contract value. Snapshot application is an explicit new-franchise choice; the default core API retains estimated finances for deterministic existing simulations/tests.

The 2026 base league cap is $301,200,000 ([NFL Football Operations](https://operations.nfl.com/calendar-events/nfl-free-agency/nfl-salary-cap)). Team-specific adjusted caps and obligations must be validated before claiming financial realism. The snapshot does not implement the full CBA or all release/trade timing exceptions.
