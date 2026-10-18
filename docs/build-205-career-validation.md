# Build 205: career validation

The next roadmap step expands recruiting diagnostics into careers with regular-season games and changing program prestige.

## Coverage

- Six reproducible dynasty seeds: three 12-team, five-year careers and three 130-team, ten-year careers (45 combined seasons).
- Regular-season schedules and games use roster-based simulation profiles. Results feed season history and program prestige before recruiting.
- Weekly regular-season portal activity and shared competing-offer resolution exercise deferred transfer commitments, followed by offseason enrollment, six recruiting weeks, walk-ons, development and staff advancement.
- Annual checks cover roster capacity, starting position depth, legal class years, unique players and commitments, signed-offer provenance, and assisted-team quality relative to current-prestige CPU peers.
- SQLite round trips preserve roster identities, commitments, offers, prestige and team history.

This is synthetic-league coverage, not a calibration against real-world recruiting. It does not yet cover postseason brackets, coaching turnover, manual recruiting strategies or recovery from corrupt saves.

## Fix

Weekly game simulation now filters by season year as well as week. An unplayed historical or future game retained in a schedule must remain untouched. A focused regression test covers both cases.

An older assistance-toggle test assumed a CPU commitment in the first week. It now checks that the CPU makes offers and signs during the six-week window while the unassisted user team remains uninvolved. No signing or transfer caps were added.

## Validation

All 189 Release tests passed, including all six careers (45 combined seasons), in 8 minutes 42 seconds. Android Debug build and packaged-resource checks passed; five existing obsolete MAUI alert API warnings remain.

Final-year diagnostics:

```text
seed=1 teams=12 prestige=42 year=2030 HS=28 portal=32 rank=12/12 walkons=9 userOVR=66.8 CPUOVR=74.1 peerOVR=69.8 weeklyHS=1,6,7,8,6,0
seed=6 teams=130 prestige=73 year=2035 HS=19 portal=13 rank=46/130 walkons=3 userOVR=76.3 CPUOVR=71.5 peerOVR=71.8 weeklyHS=12,4,3,0,0,0
seed=4 teams=130 prestige=40 year=2035 HS=12 portal=20 rank=123/130 walkons=25 userOVR=67.6 CPUOVR=71.5 peerOVR=68.9 weeklyHS=4,6,2,0,0,0
seed=2 teams=12 prestige=71 year=2030 HS=37 portal=23 rank=7/12 walkons=6 userOVR=72.1 CPUOVR=73.7 peerOVR=74.8 weeklyHS=6,6,10,13,2,0
seed=5 teams=130 prestige=58 year=2035 HS=19 portal=27 rank=93/130 walkons=21 userOVR=71.0 CPUOVR=71.5 peerOVR=69.1 weeklyHS=8,1,4,6,0,0
seed=3 teams=12 prestige=87 year=2030 HS=32 portal=29 rank=5/12 walkons=0 userOVR=77.5 CPUOVR=73.7 peerOVR=78.9 weeklyHS=5,10,6,5,6,0
```
