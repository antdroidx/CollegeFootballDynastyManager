# Pro financial model — October 26 refinement

This is developer documentation. Ordinary gameplay screens explain decisions without development/source disclaimers.

## Implemented accounting

- Annual contract workspace separates salary, allocated bonus, other cap compensation, guaranteed base and total cap hit. Comparables use the same position within five overall points. Offers use $250K controls and start at age/market/competition-based agent demands. Existing deal and market tabs retain context without pushing offer controls below a long table.
- Extensions allocate new bonus over at most five remaining seasons; salary cannot fall below the applicable league minimum. Completed extensions/restructures are limited to one per season. Bonus cash is separately recorded in its paid year, so extending an old deal or converting salary does not rewrite historical cash spending.
- Release previews and execution share the same annual calculation: remaining allocated bonus accelerates into the current season; earned/guaranteed current base and paid other compensation remain; future guaranteed base remains in its scheduled season. Negative savings are shown. June 1 designations, guarantee offsets and detailed vesting dates remain outside this model.
- Trades retain seller earned salary and bonus debt and transfer remaining salary/future base obligations. No bonus cash is duplicated onto the buyer.
- New 2026 franchises initialize all 32 team allowances from the supplied CSV: $301.2M base + 2025 rollover + signed adjustments = adjusted team cap. WAS/WSH is an explicit alias. Imported integers remain exact; only headline displays round to $250K. Existing saves are not silently re-budgeted.
- New-franchise rollover elections are measured after each club completes its regular-season schedule. The chosen 0/25/50/75/100% is capped by nonnegative unused room, reserved against the closing year and credited once to the next year. Cash-floor spending excludes this reservation. Legacy saves retain their previous end-year convention until replaced by a new franchise.
- Retention-period extensions/restructures start their new bonus allocation and bonus cash in the next league year, so a closing-year rollover reservation cannot be spent twice. The completed-change lock covers that applicable year.
- Cash-floor windows use 90% of tracked league-base caps, with settlements recorded separately from cap charges. A franchise beginning in 2026 has only a **partial** 2024–26 window; missing past cash is not authoritative historical data. 2027–30 is the next complete modeled window, followed by modeled four-year cycles.
- 2026 active minimums: $885K rookie, $1.005M one credited season, $1.075M two, $1.145M three, $1.215M four–six, $1.3M seven-plus. Scheduled increases are $45K/year through 2030. Practice minimums use the separate weekly schedule. Beyond 2030, 4% minimum growth is a game assumption. Credited seasons approximate participation in three regular-season games; full benefit/accrual rules are not modeled.
- League cap changes use persistent franchise/year-seeded revenue cycles and shocks, bounded −2% to +10%, rounded to $100K. Contract market expectations scale with cap growth. This is a simulation model, not an announced NFL forecast. CPU year-end roster bonuses invest unused funds toward 90% of adjusted cap without exceeding cap; spending-floor settlements remain cash-only.

## Primary rules references

- [NFL/NFLPA 2020 CBA](https://nflpaweb.blob.core.windows.net/website/PDFs/CBA/March-15-2020-NFL-NFLPA-Collective-Bargaining-Agreement-Final-Executed-Copy.pdf): Articles 12/13 spending, revenue/cap and carryover; salary and practice-squad schedules.
- [NFL free-agency and minimum-salary information](https://edge-operations.nfl.com/calendar-events/nfl-free-agency/types-of-free-agents).

## Limits and validation

Imported source accounting remains experimental: historical cash, departed-player debts, complete opening contract commitments, guarantee vesting and future projections are incomplete. The provided 2026 allowance components are now imported and reconciled; the CSV does not contain official opening available-cap balances or player/dead-money obligations. No missing source amounts are asserted as verified zeroes. Twenty-season tests use estimated game contracts; they establish continuity and cap/roster/draft invariants, not authoritative NFL economic/statistical calibration. Regression tests cover annual release debt, restructure cash/cap conservation, legal minimums, rollover/floor separation and deterministic projections. Full sourced-ledger stress and distribution calibration remain pending.


## Sourced-mode diagnostic follow-up

A five-season diagnostic first exposed CPU roster starvation/unsigned rookies after source-contract releases. CPU planning now tries legal restructures before releasing core starters, reserves depth money at inflated minimum budgets and retries rookies before free agency. The fixed-seed 2027–31 run then had no over-cap clubs or unsigned rookies and at least 47 active players after the first offseason, 53 thereafter. Draft-class means stayed 63.5–64.8. This is one continuity scenario, not statistical calibration. The imported 2026 opening still lacks authoritative team adjustments and starts eight clubs above modeled base cap; it is not a reconciled real NFL ledger.


## Supplied 2026 allowance file

`src/ProFootball.Core/nfl_2026_salary_caps.csv` preserves all values from the user's `2026-salarycap.csv` (32 rows, six columns). All 32 arithmetic identities reconcile, including 28 negative adjustment rows. Git normalizes text line endings; source attachment SHA-256: `fa83ced04c16e36415ab69c78a666b92bd062c40ff283e9880e16a35aa4e6c52`.

No commitment or opening available-space field is inferred from these allowances. `ProFinance.Budget` separately derives recorded salary, bonus allocation, other contract cap charges, dead money, reserved rollover and available game cap from the saved ledger. Applying the allowance file does not alter contracts, players, dead-money entries or past financial history. A real-world official opening available balance cannot be established from this CSV alone.

Rollover timing follows CBA Article 13 §6(b)(v), printed pages 112–113 (PDF pages 128–129): final regular-game measuring date, current-year reservation and next-year credit. Administrative notice delivery is represented by the saved in-game election, not an external submission. Future incentives/guarantee offsets still require richer source data and rules.

Repository CSV blob SHA-256 (normalized line endings): `d3d67f995d24ecce152509fa6a1ca22f377ba592c37b54956e6162d153d6a3e8`.
