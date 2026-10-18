# Build 204: competing scholarship offers

## Shared engine

Recruits and transfers now have persisted offers from individual schools. Each offer records the school, prospect/source/season, interest, invested recruiting points, offer week, last contact and status (active, withdrawn, signed or lost). School preferences exist before offers, but preferences alone cannot win a commitment.

CPU teams build pursuit boards from positional needs, public quality, geography and school fit. They contact prospects each week using their existing prestige/shortage-based budget, shared across HS and portal pursuits, with a scouting allowance. Recruiting staff affect contact gains. Transfer planning still weighs immediate improvement, eligibility, promising younger players, pending arrivals and coach strengths. Pursuit board size is a planning choice; there is no weekly signing quota or annual transfer maximum.

All schools' offers are generated before any decisions. Recruits compare only active offers from eligible schools with space. Interest and personal readiness determine when they decide; a small stable personal-choice variation prevents equal-interest ties from always favoring the same school order. CPU teams can pursue and win user targets. Withdrawn and nonexistent offers cannot win, and full schools cannot redirect a prospect to an unrelated unoffered school. Unsigned prospects can remain unsigned.

Manual actions and assistance update the same offer book. Direct CPU signing and percentage-based weekly CPU allocation paths were removed. All prior recruiting entry points delegate to one idempotent market resolver. Regular-season transfers reserve next-season space, account for graduating seniors/pending arrivals, and do not join immediately. Walk-ons remain the separate roster safety net.

## Screen and saves

The Recruit screen initializes offers in the background without resolving commitments. Rows show actual offer counts and the leading offered school. Player details list offering schools, interest, status and recent contact; pre-recruit favorites remain separately labeled. Best rival interest now comes only from actual active rival offers. School changes preserve inherited outstanding offers in the user's board/actions.

Save schema 18 preserves offers, effort, contact dates and processed-turn markers. Existing saves load normally: current manual scholarships are imported at the upgrade week, prior commitments stay intact, and historical CPU offers are not invented. Active CPU offers start from remaining needs. Current and previous season offer books are retained. Repeated calls and save/reload cannot spend CPU effort twice or reroll a resolved week.

## Validation

- 185 tests pass; local Debug Android build succeeds (five existing obsolete-alert API warnings).
- Dedicated checks cover competing user/CPU bids, actual-offer-only decisions, withdrawals, full rosters, no-offer unsigned prospects, origin-school exclusion, mixed HS/portal budgets, dictionary-order independence, duplicate pools, inherited offers, old-save upgrade and save/reload before/after decisions.
- Full HS workload: 130 teams and 3,250 recruits generated 6,966 offers and 49 first-week decisions in approximately 0.48 seconds locally.
- Portal workload: 130 teams, 8,450 roster players, 2,000 transfers and active user offers completed in approximately 0.59 seconds locally.
- Five annual cycles at three program strengths verify unique players/commitments, full rosters, positional depth, progression and saves. Every new commitment must have a matching signed offer.

These are desktop synthetic timings. Device behavior and broader career balance still need playtesting; this is a substantial recruiting balance change. The five-cycle scenarios use a 12-team league and one seed with fixed prestige, not complete game simulations.

## New five-cycle measurements

| Prestige | Year | HS | Portal | Class rank / 12 | Walk-ons | User OVR | Peer OVR | HS commitments by week |
|---|---|---|---|---|---|---|---|---|
| 35 | 2026 | 26 | 34 | 12/12 | 0 | 67.2 | 68.4 | 0,2,8,12,4,0 |
| 35 | 2027 | 29 | 30 | 10/12 | 5 | 69.0 | 69.4 | 0,2,5,10,8,4 |
| 35 | 2028 | 32 | 28 | 7/12 | 5 | 70.4 | 69.5 | 0,3,6,9,14,0 |
| 35 | 2029 | 34 | 27 | 9/12 | 13 | 68.8 | 67.8 | 1,5,7,9,10,2 |
| 35 | 2030 | 34 | 26 | 12/12 | 11 | 67.5 | 68.4 | 0,3,4,10,17,0 |
| 65 | 2026 | 31 | 29 | 6/12 | 0 | 71.1 | 72.1 | 5,5,7,8,6,0 |
| 65 | 2027 | 33 | 28 | 4/12 | 2 | 74.2 | 73.8 | 3,2,7,11,7,3 |
| 65 | 2028 | 36 | 24 | 6/12 | 1 | 75.5 | 74.4 | 3,3,4,9,15,2 |
| 65 | 2029 | 31 | 29 | 6/12 | 7 | 73.5 | 74.1 | 2,1,6,15,7,0 |
| 65 | 2030 | 31 | 29 | 5/12 | 7 | 73.0 | 72.5 | 6,5,8,6,5,1 |
| 90 | 2026 | 46 | 14 | 4/12 | 0 | 72.7 | 78.1 | 5,7,8,17,9,0 |
| 90 | 2027 | 36 | 25 | 2/12 | 1 | 77.6 | 78.4 | 6,5,8,13,4,0 |
| 90 | 2028 | 42 | 18 | 3/12 | 0 | 77.4 | 80.3 | 6,4,7,11,14,0 |
| 90 | 2029 | 34 | 26 | 2/12 | 0 | 77.4 | 79.6 | 0,5,5,9,15,0 |
| 90 | 2030 | 27 | 33 | 3/12 | 1 | 78.1 | 79.8 | 7,5,7,4,4,0 |
