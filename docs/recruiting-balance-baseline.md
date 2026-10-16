# Recruiting balance and dynasty health: first baseline

This begins roadmap priorities 2 and 3. Neither milestone is complete.

## Method

Three deterministic 12-team leagues, with the user controlling a prestige-35, prestige-65 or prestige-90 program. Each runs five annual recruiting/lifecycle cycles (2026-2030), full user assistance, portal entry and recruitment, six HS recruiting weeks, walk-on replenishment, player progression/regression, staff aging, weekly portal attrition, scouting, and a SQLite save/load round trip every year. Peer CPU programs are within five prestige points of the user.

Games are not simulated, wins and program prestige are held fixed, and this is one dynasty seed. A 12-team portal pulls a larger share of available players than the full FBS league. These counts and ranks are diagnostic results, not target quotas or predictions for a 130-team save.

## Results after duplicate-signing correction

| Prestige | Year | HS | Portal | Class rank / 12 | Roster walk-ons | User OVR | Peer OVR | HS commits by week |
|---|---|---|---|---|---|---|---|---|
| 35 | 2026 | 13 | 32 | 9/12 | 15 | 68.0 | 69.0 | 1,1,2,2,2,5 |
| 35 | 2027 | 17 | 30 | 5/12 | 15 | 71.9 | 69.8 | 0,1,5,3,3,5 |
| 35 | 2028 | 14 | 32 | 4/12 | 17 | 72.4 | 70.0 | 1,0,3,1,6,3 |
| 35 | 2029 | 19 | 30 | 4/12 | 17 | 73.0 | 68.1 | 0,3,2,4,5,5 |
| 35 | 2030 | 16 | 32 | 7/12 | 17 | 71.0 | 68.6 | 0,2,1,4,6,3 |
| 65 | 2026 | 26 | 34 | 7/12 | 0 | 72.9 | 73.0 | 3,6,8,9,0,0 |
| 65 | 2027 | 28 | 32 | 2/12 | 1 | 78.1 | 73.5 | 4,1,6,14,3,0 |
| 65 | 2028 | 28 | 32 | 3/12 | 1 | 78.0 | 73.5 | 3,2,8,12,3,0 |
| 65 | 2029 | 19 | 41 | 3/12 | 2 | 78.2 | 72.3 | 2,2,3,8,4,0 |
| 65 | 2030 | 25 | 35 | 3/12 | 1 | 77.8 | 72.3 | 4,7,5,6,3,0 |
| 90 | 2026 | 43 | 17 | 4/12 | 0 | 72.9 | 77.6 | 5,7,9,21,1,0 |
| 90 | 2027 | 31 | 31 | 2/12 | 1 | 79.6 | 78.8 | 9,6,13,3,0,0 |
| 90 | 2028 | 36 | 24 | 2/12 | 0 | 79.5 | 78.5 | 6,6,14,8,2,0 |
| 90 | 2029 | 31 | 30 | 2/12 | 0 | 78.0 | 79.0 | 2,7,11,10,1,0 |
| 90 | 2030 | 27 | 33 | 2/12 | 0 | 79.9 | 79.3 | 5,13,6,3,0,0 |

## Findings

The first pass caught already-signed CPU recruits being targeted and signed again. Assistance now excludes rostered, pending and committed IDs. Interaction reads/manual actions recognize CPU commitments, and resolution guards stale/duplicate offers. The checks now preserve unique players and commitments, 85-player rosters, starting positional depth, valid ratings, separated portal/roster IDs, and annual save/load state.

Broad regression checks require at least 50 non-walk-ons and user roster OVR no more than ten below similar-prestige CPU programs. Those are test failure thresholds, not recruiting restrictions. Stronger assisted programs are competitive in these samples; low-prestige assistance depends more on walk-ons. No new transfer or weekly commitment limits.

## Next checks

Expand to 130 teams, multiple seeds and longer careers; vary wins, coaching ability, prestige changes and portal reliance. Track scholarship age distribution, walk-on dependency by position, class rank and recruit quality relative to CPU peers. Include full-season game simulation and deferred midseason transfers before claiming complete dynasty health.
