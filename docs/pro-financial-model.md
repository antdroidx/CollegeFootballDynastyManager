# Pro financial model — October 26 refinement

This is developer documentation. Ordinary gameplay screens explain decisions without development/source disclaimers.

## Implemented accounting

- Annual contract workspace separates salary, allocated bonus, other cap compensation, guaranteed base and total cap hit. Comparables use the same position within five overall points. Offers use $250K controls and start at age/market/competition-based agent demands. Existing deal and market tabs retain context without pushing offer controls below a long table.
- Extensions allocate new bonus over at most five remaining seasons; salary cannot fall below the applicable league minimum. Completed extensions/restructures are limited to one per season. Bonus cash is separately recorded in its paid year, so extending an old deal or converting salary does not rewrite historical cash spending.
- Release previews and execution share the same annual calculation: remaining allocated bonus accelerates into the current season; earned/guaranteed current base and paid other compensation remain; future guaranteed base remains in its scheduled season. Negative savings are shown. June 1 designations, guarantee offsets and detailed vesting dates remain outside this model.
- Trades retain seller earned salary and bonus debt and transfer remaining salary/future base obligations. No bonus cash is duplicated onto the buyer.
- Team adjusted cap includes recorded prior-year carryover. The user elects 0/25/50/75/100%; CPU teams carry all available room. The simplified game books this at retention/year transition, rather than the CBA regular-season notice date. Existing saves start with no invented historical rollover.
- Cash-floor windows use 90% of tracked league-base caps, with settlements recorded separately from cap charges. A franchise beginning in 2026 has only a **partial** 2024–26 window; missing past cash is not authoritative historical data. 2027–30 is the next complete modeled window, followed by modeled four-year cycles.
- 2026 active minimums: $885K rookie, $1.005M one credited season, $1.075M two, $1.145M three, $1.215M four–six, $1.3M seven-plus. Scheduled increases are $45K/year through 2030. Practice minimums use the separate weekly schedule. Beyond 2030, 4% minimum growth is a game assumption. Credited seasons approximate participation in three regular-season games; full benefit/accrual rules are not modeled.
- League cap changes use persistent franchise/year-seeded revenue cycles and shocks, bounded −2% to +10%, rounded to $100K. Contract market expectations scale with cap growth. This is a simulation model, not an announced NFL forecast. CPU year-end roster bonuses invest unused funds toward 90% of adjusted cap without exceeding cap; spending-floor settlements remain cash-only.

## Primary rules references

- [NFL/NFLPA 2020 CBA](https://nflpaweb.blob.core.windows.net/website/PDFs/CBA/March-15-2020-NFL-NFLPA-Collective-Bargaining-Agreement-Final-Executed-Copy.pdf): Articles 12/13 spending, revenue/cap and carryover; salary and practice-squad schedules.
- [NFL free-agency and minimum-salary information](https://edge-operations.nfl.com/calendar-events/nfl-free-agency/types-of-free-agents).

## Limits and validation

Imported source accounting remains experimental: historical cash, rollover/team adjustments, departed-player debts, guarantee vesting and future projections are incomplete. No missing source amounts are asserted as verified zeroes. Twenty-season tests use estimated game contracts; they establish continuity and cap/roster/draft invariants, not authoritative NFL economic/statistical calibration. Regression tests cover annual release debt, restructure cash/cap conservation, legal minimums, rollover/floor separation and deterministic projections. Full sourced-ledger stress and distribution calibration remain pending.
