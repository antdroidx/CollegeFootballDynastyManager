# Current work — shared Core, College and Pro

Updated October27,2026. This file records the current state; Git history retains superseded build narratives. Continue all four authorized tasks through completion during active work. The user deleted automatic continuation: do not recreate it.

## Workspace and safeguards

- Work only in D:/AppProjects/Codex/Football Simulation Engine on feature/pro-franchise-foundation.
- Stable main remains fdd3d4898b5bd07eeec8dba346024d61f5289474 (verify exact ref before any future merge; no merge is currently authorized for unfinished Pro).
- Preserve College package/save compatibility, separate Pro identity and Android update signing.
- Follow docs/pro-contract-reconciliation.md and docs/testing-scope.md. Do not duplicate active builds or completed long simulations. No subagents authorized.
- Raw source evidence belongs in data/pro/source-snapshots, not disposable .artifacts. Huge serialized franchise JSON can be136MB on one line: parse/select fields, never dump the raw file.
- Archive obsolete Initial Base Upload Java files and rename the repository Football-Sim-Engine only after current application work.

## Current four tasks

1. Reserve activation: accepted Pro63 in the available imported reserve scope. Healthy signed generic RES/INA/DEV players activate with cap/roster/role checks and saved status. Explicit IR/PUP/SUSP/EXE unknown clearance remains blocked; no invented medical dates.
2. Real-contract accounting: framework and JSN reviewed payments/guarantees/vesting are implemented; source-wide review remains open. Preserve signed contracts and historical debt. No inferred guarantees from a balancing difference.
3.32-team financial reconciliation/default: corrected NFLPA allowances/counting are accepted.447 recovered contracts are packaged in64; dated current financial rows, departed debt, identity/status and guarantee discrepancies remain. Real contracts remain opt-in until corrected. Do not force reported room or invent liabilities/cash.
4. Original-contract waivers and legal termination timing: packaged64/device accepted in its recorded scope; the assignment accrual follow-up below still requires its own APK before claiming the whole task complete.

## Pro64 — accepted finance/management/termination

App/core8b06fa7 packaged in run40547793852, APK12754790693:
https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40547793852/artifacts/12754790693

All239 full local Pro cases passed in5m22s, including20 generated/five sourced seasons.237 fast CI cases,16 helpers, source validator and packaging passed. Exact-APK verification40573852505 at harness00944a7 passed finance/budgets/source-cash/options, trade, six coordinator scenarios, June1 held release and original-contract waiver claim/award/reload. Evidence12766593020 is in .artifacts/pro64-accepted-ui; June1 and claim screenshots reviewed. The original run's broad fixture-glob failure is superseded, not an app failure. No app rebuild was required for acceptance.

64 includes original-contract snapshots/tagged assessment reversal, remaining salary/rookie/lock preservation and claim/award cap/roster/role rechecks. Dated June1 split, two early designations/held savings,2030 final-year rules, cash/cap preview and a near-header waiver claim action are verified in this scope. Reserve63/full GameCast44/College390 evidence is reused where unchanged. Source default and the whole four-task request remain unfinished.

## Current candidate after64 — salary assignment/contract recovery

Implemented salary assignment start year/fraction. Release, repeated trades, waiver preview and minimum-salary checks now use only elapsed time since assignment; historical debt is not recalculated. Six AssignmentAccrualTests cover immediate/later claims, repeated trades, original signing year, guaranteed unpaid option transfer and real-loader debt preservation.31 combined contract/waiver/termination/accrual cases pass. Earlier three reproduced failures are corrected; .artifacts/assignment-accrual-before.txt is historical.

Trades now transfer future unpaid guaranteed option cash to the buyer, preserving paid seller bonus charges and original signing/negotiation terms. Waiver Paragraph5/non-salary retention is separate. Primary support: CBA Article13.6(d)(v), source trade figures and cash-conservation regressions. A current guaranteed unpaid option is no longer paid twice. Legacy booked debt remains unchanged.

23 additional strict-identity/exact-cap contracts added after annotated future years and published cash were reviewed. Total optional source records2482; pack SHA2cff3ae5b107c1e58ecda7ec6c4fd6f3dd3a53b705c40c1f1187954a86048895. JSN raw record equality checked before updating hash guard. No existing source record or saved contract is replaced. Extension new-money charts, unsupported cash differences and partial-year discrepancies remain rejected. Latest current-row audit:2467 exact,19 missing,32 identity/status cases,21 differences across2539 events.

All245 current Pro cases passed in10m6s, including20 generated/five sourced seasons (.artifacts/assignment-full-final.txt). Earlier240 fast cases passed before the final three new accrual tests;31 targeted cases include all six. No local test remains active.16 helpers/five parser cases pass. New core-generated device fixture uses a midseason claim, then release after one week, requiring333333 buyer-earned cash. `termination` CI scope verifies only the changed release/waiver/accrual UI, reusing accepted64 finance/coordinator/trade UI and unchanged College/GameCast. Next APK will use termination scope; no duplicate long simulations or College/GameCast build.

Primary CBA: https://overthecap.com/collective-bargaining-agreement/article/13/section/6 ; official copy https://nflpaweb.blob.core.windows.net/website/PDFs/CBA/March-15-2020-NFL-NFLPA-Collective-Bargaining-Agreement-Final-Executed-Copy.pdf . Keep payment-date/vesting uncertainty in developer evidence, not user-facing disclaimers.

## Source evidence and staged work

Source pack64 has2459 contracts:447 previously absent strict-identity, exact-cap, published-cash records added, preserving every existing source record. Pack SHA c1a2ad199b9689637ad2f1eafa2237b92e028c81beaa764351f7834ecb41628b. JSN source-record equality was checked before updating its metadata hash guard. Source pack is -text in .gitattributes for byte preservation.

Fresh October27 source cache contains32 complete team pages,507 opening-contract pages and95 future-bonus pages. Current financial audit:2444 exact cap rows,42 missing source records,32 identity/status cases and21 differences across2539 financial events. There are repeated player financial events; never collapse them by player ID or use financial spending to overwrite current roster ownership. Strict identity audit still has10 dated team conflicts and22 identifier gaps. Primary status reviews are docs/data/nfl-contract-status-review-2026-10-26.json.

Parser now uses semantic headers because OTC reuses bonus CSS, retains future years annotated with date tooltips, preserves exact dollars and extracts source cut/trade figures plus annual cash flow. Five parser regressions pass. None of these corrections changes the447 records in64.42 additional candidates are staged outside the app. Extension cash-flow charts describe new money, so they are excluded as full annual-pay evidence. Remaining partial-year/duplicate financial rows need review. Two opening player pages are still absent after bounded network retries.

Developer tools:
- build/stage-pro-reviewed-events.py:37 source-column-derived review candidates; no app writes. Matching current cut quotes validates amounts, not future vesting dates.34 staged current cases match both source pre/post-June1 cuts; overall35 of95 match,60 require further review. Original unmodified-source audit had3 exact and92 review cases. Candidate file .artifacts/pro-payment-event-candidates.json; comparison .artifacts/pro-termination-candidate-comparison.json. Do not promote candidates automatically.
- tools/ProContractAudit --termination-evidence PARSED_PAGES NOTE_IDENTITIES OUTPUT [REVIEW_CANDIDATES] executes actual core predictions with optional in-memory review candidates. No app pack or saved ledger is changed.
- build/stage-pro-source-debt.py has staged735 reported2026 events across32 teams, total1,591,660,715, including each source aggregate remainder. All32 sums reconcile; no negative residuals. Null identities stay null, no historical cash/future debt/player ownership is invented. Output docs/data/pro-opening-dead-cap-2026-10-27.json is developer data only, not yet applied to a game.
- Frozen positional market curves and prior1365d52 audit remain unchanged. Do not regenerate the old comparative report against the new curves.

## Accepted/reusable evidence

Reserve-specific accepted Pro63 app/core a8ad14b, run40530334315, APK12745318137, device evidence12745348663.21 focused reserve/camp/depth,220 scoped CI cases and16 helpers passed. Available activation, disabled injured reason, unchanged contracts, saved ACT status and reload were checked. https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40530334315/artifacts/12745318137

Pro62 finance/management app2ac5992/core94d77bc, run40523114687, APK12741879655/evidence12742934656.207 fast CI cases,16 helpers, full finance/budgets/source cash/void UI/themes/reinstall/old-budget correction/options/trades and six OC/DC retention/roster/draft flows. Earlier209 full local cases passed9m55. https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40523114687/artifacts/12741879655

Reuse unchanged Pro44 full GameCast/aging/field source4f6bb2c, run40438801865, APK12705098072/evidence12705962978. College390 source2acfcc1, run40438515279, APK12705660863, evidence12705718044 and12705752478:690 legacy combined Core/College tests and both Android flows. Do not relabel690 as exclusively Core or College until audited. No College/shared rendering changes in current financial work.

Broader authorized roadmap remains in docs/pro-completion-checklist.md; do not lose the four current priorities in that wider list. No main merge, repository rename, Java archival or continuation automation has occurred.


Primary Seattle roster review is in docs/data/pro-roster-status-review-2026-10-27.json: Foster was waived October24; Maranges is the designated international practice player. These source-only reviews do not yet change game data or infer historical payments. Ten dated status-conflict player pages are now cached (all10 successful) in data/pro/source-snapshots/2026-10-27/status-contracts; do not refetch completed pages.
