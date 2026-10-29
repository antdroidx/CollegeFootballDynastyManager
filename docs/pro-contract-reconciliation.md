# Current scope update

October27 user direction supersedes the source-perfect promotion gate in the historical plan below. Finish a practical finance system using best available real contracts and reasonable estimates, default that mode in the app, and keep detailed accounting optional. Preserve saves and coherent cap/transaction rules. See pro-practical-finance-policy.md. Do not keep auditing private guarantees or per-team website room before closing this phase.

# Real-contract reconciliation phase

Authorized October 26, 2026. Implementation is in progress; The first accounting/UI batch is verified in Pro Build 36; broader source reconciliation, option rules and the sourced default remain open.

## Product decision

Real sourced contracts should be the default for new Pro franchises once their accounting is corrected. Keep an explicit generated-contract alternative. Do not simply switch the default while known void-year and cash errors remain. College is unaffected.

Generated fallback terms must use known signing, rookie and draft metadata rather than random remaining years. A drafted rookie starts with four years; a first-round fifth year is a separate option decision, not an automatic fifth season. Undrafted initial deals and genuinely released young free agents need separate handling. Age alone must not prevent legitimate free agency. Never overwrite a real extension with inferred rookie terms. Missing facts remain estimates, not invented sourced commitments.

## Confirmed defects to fix

- The factory defaults to generated contracts; the new-franchise picker selects Game contracts.
- Fixed in `6faa77f`: veteran generated remaining terms previously used a GUID-based random one-to-four-year rule. New terms use source playing dates where matched and explicit estimates otherwise.
- Corrected initial metadata/cash separation in `6faa77f`: original signing year, inferred cap-only rows, bonus cash, reported APY and source guarantee totals are retained; guaranteed base is capped at salary. Source option origin, vesting and complete remaining guaranteed cash still require reconciliation.
- Fixed initial void/employment defect in `6faa77f`: phantom salaries no longer inflate cash averages or delay expiration; proration still retains accounting years. Natural expiry, net extension replacement and void acceleration have targeted regressions.
- Seattle's displayed allowance reconciles to the supplied CSV, but mixed sourced/generated obligations and accounting basis do not reconcile to the quoted real-world cap space. Do not force a balancing entry to match that number.

## Implementation order

1. Preserve source provenance/date and explicit employment end, void/accounting year, cash and guarantee semantics. Add safe, versioned save migration that preserves negotiated changes and current obligations.
2. Correct annual cash/AAV, expiration, renewal, release, bonus acceleration and offer calculations to distinguish employment from accounting years. Keep estimated source void structure identifiable in developer documentation.
3. Repair fallback remaining terms using available metadata. Add first-round option eligibility, timing, election and salary handling with primary-source rule checks.
4. Reconcile 32 team ledgers using one dated snapshot and explicit seasonal counting rules, player obligations, departed-player dead money and other charges. Keep uncovered data visible to developers; do not fabricate commitments or opening balances.
5. Promote sourced contracts as the new-franchise default after the above checks. Existing saves retain their selected mode and negotiated contracts.
6. Simplify the main contract view: remaining playing years, annual pay, annual cap hit, guarantees and release impact. Put bonus proration, void years and detailed accounting in an expandable advanced view. Offer controls retain $250K increments; imported accounting retains full dollars.

## Acceptance and efficient verification

- Regressions for void rows excluded from cash/employment but retained for cap acceleration; correct guarantee classification; preservation of negotiated saves; drafted rookie remaining terms; undrafted/released exceptions; first-round option decision; accurate default selection.
- Record exactly what is and is not reconciled for Seattle and every other team. A headline cap-space figure is not a substitute for matching dates, roster statuses and counting rules.
- Run affected Pro finance tests and targeted contract/device checks. Repeat long simulations when annual expiration/options or CPU finances change. Reuse unchanged College and GameCast evidence per testing-scope.md.
- Report a new APK only after its applicable checks pass; keep Build 33 clearly separate from this pending phase.

## Source references already inspected

- https://overthecap.com/player/jaxon-smith-njigba/10844
- https://overthecap.com/salary-cap/seattle-seahawks
- https://overthecap.com/salary-cap-space
- https://www.seahawks.com/news/seahawks-sign-jaxon-smith-njigba-to-multi-year-extension
- NFL/NFLPA 2020 CBA: verify applicable rookie, option and cap-accounting provisions during implementation.

## Current accounting implementation and limits

The October 26 source Parquet SHA-256 is `3f6f8906a20d48c688f5727e76856997bc01ad2d9e1ac0ed83b41559f4a12a9f`. Importer now requires an explicit `--as-of` date. 2,012 active roster contracts match; 519 roster commitments still use generated fallbacks. The original signed year, reported deal APY, separate cash bonuses, total source guarantees and capped guaranteed-base component are retained. Bonus allocation totals are no longer labeled signing cash. Trailing future rows with zero cash after the last paid year are treated as inferred cap-only years; zero-cash current exempt/unknown rows are not automatically voided. The source does not preserve all trigger/vesting/option semantics, so those still need deeper reconciliation.

Unchanged legacy source contracts migrate only if every annual cap charge and non-void salary still matches. Negotiated, restructured, traded, terminated or financially different deals remain untouched. New fallback terms use known playing end dates before original rookie metadata, preserve legitimate later short re-signings, and otherwise use explicit age-based estimates rather than GUID randomness.

Employment expiration, extension and retention use playing years. Bonus proration still uses accounting years. Natural expiration records remaining bonus debt in the next season; extensions and restructures update void acceleration once. Traded future cash follows the remaining contract; departed contracts cannot continue paying future bonuses. Compact contract details show playing seasons/pay/cap/guaranteed salary; accounting is optional.

The repeated 2029 sourced test exposed an existing CPU planning weakness: small cap-saving cuts could cost more to replace, and a no-offer round stopped even after a budget repair. Replacement funding/retry and retention depth reserves are included in this batch. Final multi-year results are recorded in current-work.md when verified. Do not promote the sourced default or call the 32 opening balances reconciled yet. Fifth-year option decisions remain a separate outstanding item.

Run `dotnet run --project tools/ProContractAudit -- OUTPUT.json` for the developer ledger audit. Its full-roster model values are not an assertion of official Top-51 cap space. See pro-contract-source-audit.md.

## Next rule audit checkpoint

Before implementing offseason Top-51 counting, use CBA Article 13 section 6(a)(i), PDF page 125: contracts outside the highest 51 can still carry Paragraph 5 amounts above the applicable minimum thresholds (different for undrafted rookies). Do not implement this by dropping every charge outside a simple 51-player slice. Bonus proration/acceleration and other charges remain separate. Sections 6(b)(ii) and 6(b)(iii) also require distinguishing paid/guaranteed option bonuses and release timing. Current simplified release behavior is not a complete June-1 designation model; preserve its explicit preview until a versioned rule upgrade is tested.

Primary reference: https://nflpaweb.blob.core.windows.net/website/PDFs/CBA/March-15-2020-NFL-NFLPA-Collective-Bargaining-Agreement-Final-Executed-Copy.pdf

Landscape work preparation: ProLivePage currently stacks separate score/situation/possession rows, a field capped at half the page, quick cards, and a wrapping action row inside a ScrollView. Compare College's compact broadcast strip and quick-call collapse before changing field-height math. Keep field geometry/simulation independent of available display height. Test selected-call confirmation, reopening choices and next-snap reachability in a landscape viewport without scrolling.

## Fifth-year implementation audit notes

GameSnapEvent already records OffensePersonnel/DefensePersonnel and GameDaySession.Events; ProLeagueService.Finish rejects duplicate finals before applying state. Track first-round rookies' regular-season snaps/eligible team snaps in the Pro layer at that guarded point, excluding special teams/non-scrimmage events. Preserve missing pre-franchise history as unknown; never assert an imported player met past playing-time thresholds without records. Existing source contracts already extending through a fifth year must remain untouched.

Model an explicit original-rookie-contract origin and a once-only exercise/decline decision. Window: after third regular season (retention) through the following free-agency and draft stages, before roster cuts; original four playing years remain when declined. First-round only; no automatic fifth season for everyone. Exercise adds a fully guaranteed fifth-year salary and guarantees the fourth-year base under the applicable CBA rule. Keep a statutory option decision separate from negotiated-change lock semantics; preserve source/negotiated extensions. Values for future simulated league years are league-generated positional option prices, not announced real NFL figures. Use recorded game Pro Bowl selections and usage tiers where available; document incomplete legacy history conservatively. Full future cap/role/GM checks and CPU same-rule decisions are required.

UI: a compact Rookie options desk under Front Office, clear inactive/available/deadline text, price/current deal/future cap impact together, exercise/decline and optional staff delegation; inbox deep link; prevent silently skipping a required user decision at the stage boundary. Existing v0/v1 saves gain only additive fields and remain financially unchanged until a valid explicit decision.


### Option deadline / pricing audit correction

CBA Article 7 section 7(a) allows the decision after the third season's final regular-season game until before May 3 of year four. The earlier proposed pre-draft deadline was too early. In this game's coarse calendar, retain the window through the draft and require explicit user exercise/decline before moving from Draft to RosterCuts. CPU choices use the same window; no automatic fifth season. Eligibility must refer to the original four-year rookie deal, not any later contract with a matching remaining length.

Pricing is **not** simply a current-year average of salaries: section 7(g) refers to the transition/franchise tender methodology and positional salary categories. Build versioned historical positional salary/cap samples and recorded usage/original-ballot Pro Bowl eligibility; label incomplete historical model inputs in developer evidence. A two-season 75% threshold can be proven from two recorded seasons even if an older season is unknown; cumulative-three-year and every-season 50% thresholds require all three denominators. Exclude special teams and non-plays. A first-round pick with source fifth year already exercised must not receive a second decision or duplicate liability.

Rule reference: https://overthecap.com/collective-bargaining-agreement/article/7/section/7 ; verify against the NFLPA-hosted executed CBA before acceptance. The model-generated future tender remains distinct from announced real NFL option salaries. This audit adds no financial obligations to saves.

The option-deadline audit was cross-checked against the executed NFLPA CBA, Article 7 section 7(a), PDF page 56 (printed page 40), confirming the May 3 deadline. No option liabilities have been introduced yet.


Observed-usage foundation: `ProRookieParticipation.Record` now runs after duplicate-final rejection and before saving the completed regular-season game. Additive rows track both unit counts/denominators, game IDs and missing-personnel flags for each first-round player's first three years. A season qualifies as fully observed only with at least 17 distinct recorded games and known personnel for that unit; missing earlier franchise history stays unknown. Injured/reserve teammates still accrue denominators, not invented participation. `MeetsPlayingTimeTier` applies two proven 75% seasons, weighted three-year 75%, or all-three-years 50%. Exercise/decline, guarantees, origin metadata, tender-price history and UI remain pending. Do not claim options are available to users yet.


Option implementation now exists in ProRookieOptions and FranchisePage.RookieOptions: original-deal identity, frozen positional game tender samples, recorded usage/Pro Bowl tiers, guarantee/cap checks, once-only decisions, CPU/GM/staff behavior and draft-stage deadline. Testing/Android acceptance is recorded in current-work.md. New annual salary samples are observed game obligations; early missing history is marked estimated rather than fabricated. Continue deeper source guarantee/option-bonus vesting and authoritative team/departed-debt reconciliation separately; do not promote real-source defaults from this feature alone.


## Latest acceptance — Pro 46 rookie options

Pro 46 application source `26cb625` passed 152 scoped Pro tests and packaging. The exact APK is https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40445569937/artifacts/12708396236 . Harness-only retry `acac104` corrected the synthetic offseason label; run https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40446386891 passed source-equality guard, helper tests and exact-APK financial plus exercise/decline device checks. Evidence: https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40446386891/artifacts/12707954405 . Persisted fourth/fifth-year guarantees and unchanged negotiation lock were checked. Reuse full Pro 44 GameCast and College 390 evidence. Earlier pending-option paragraphs below are historical and superseded by this acceptance.

## Active correction — 2026 allowance source interpretation

The browser-rendered NFLPA public report observed October 26 shows two spending/room series per club. Both independently produce the same allowance from reported Team Cap + Cap Room. The supplied CSV's adjusted-cap column matches rounded spending, not that allowance. Preserve the supplied bytes as `nfl_2026_salary_caps.user-original.csv`; corrected exact-dollar allowances and separate carryover/derived adjustments are in the normalized CSV. Raw observed pairs, observation metadata and original hash are in docs/data. All 32 pairs reconcile; four derived adjustments are negative. Displayed report subtotals have opposite $2 rounding differences, with identical combined totals; no balancing liabilities are added. Seattle corrected allowance is $315,053,262, rollover $15,029,706, other adjustment -$1,176,444. This supersedes earlier claims that the original rounded allowances were validated.

New franchises use cap-rules version 3 (same seasonal counting). Version-2 saves retain their existing budgets unless the user explicitly selects Update 2026 cap budgets. Eligibility requires all original 32 allocations unchanged, base unchanged, 2026 not closed; custom, legacy and later-year budgets are excluded. Correction changes allowances only and preserves all contracts, transactions, debt and elections. Reported room is never imported as opening game cash/room. Source roster/guarantee/departed-debt reconciliation remains unfinished and real-source default remains off.

Current verification: 32-pair source validator and 13 UI helpers passed. One stale rollover expected value was corrected; all 157 Pro cases passed in 4m20s, including twenty generated seasons, five sourced seasons and imported-cap lifecycle. No corrected-budget APK is accepted yet. This batch also makes option history compact and inactive staff decisions clearly unavailable. Main/College/shared engine unchanged.


## 32-team obligation observation — next reconciliation inputs

Developer-only tool `build/audit-otc-opening-obligations.py` retrieved all 32 public OTC team pages for 2026, observed October 26. Raw pages and full parsed audit are cached in `.artifacts/otc-2026-10-26`; versioned aggregate observations/source URLs/hashes are in `docs/data/otc-obligations-observation-2026-10-26.csv`. Offline replay passed for 32 teams with no errors. Each team's active visible cap plus reported non-active section totals exactly matches its reported liabilities. No game or save was changed.

There are 157 distinct same-team roster/dead-name candidates (172 entries including separate repeated charges). These are review candidates, not verified mismatches: a released player can re-sign and legitimately have both an old dead charge and a new active/practice deal. Do not delete active deals by name or deduplicate repeated charges. Montorie Foster's roster status still requires a dated identity/status check rather than a guessed removal. Every reported Dead Money total exceeds its displayed named-row sum; keep the unallocated source residual explicit, never assign it to invented players. Seattle reported dead total $4,844,656, displayed named rows $3,510,818, residual $1,333,838. Practice/IR categories reconcile to their visible sums. Importing accurate aggregate reported debt may be valid, but only after current status/source credits and legacy-debt migration are designed to avoid double counting or rewriting negotiated saves. Cap-room differences are not debt.

Next implement a versioned source opening-obligations pack with provenance/identity/status verification and explicit aggregate debt, test current/prior-deal coexistence and safe unchanged-save migration. Preserve the different observation bases of NFLPA allowances and OTC obligations; do not force either reported room value as the game's opening balance. Source default promotion remains pending.


## Accepted corrected-budget batch — Pro 50

Application/data source `61022d9` (core change `61ec66e`). Exact [APK 50](https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40449248836/artifacts/12710150676). Build run 40449248836 passed 155 scoped Pro cases, source-data verification, helpers and packaging. Harness-only correction `9f4938c` waits for completed save-library layout. [Verification run 40451229268](https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40451229268) passed application-input equality guard, 14 helpers and exact-APK financial device flow: new budgets/all 32 equations, signed adjustments/source cash, themes, reinstall/save survival, explicit old-budget correction with contracts retained, rookie-option exercise/decline and persisted guarantees. [Evidence](https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40451229268/artifacts/12709764334), downloaded `.artifacts/pro51-ui`. Corrected old-save budget and compact/disabled option-history screens were reviewed. All 157 full Pro cases passed locally in 4m20s, including annual simulations. No College/shared application changes; reuse full Pro 44 and College 390 evidence. Source obligations/default promotion and remaining roadmap remain unfinished.

## Strict identifier crosswalk observation

The public obligation audit now retains each row's OTC player URL/ID. `build/audit-nfl-contract-identities.py` joins OTC ID to the dated Parquet GSIS identity, then to the game roster's ExternalId; name-only candidates are never accepted automatically. On all 32 cached pages it identifies 2,507 of 2,539 current source rows with the same game-team identity, finds 10 dated team conflicts and 22 unresolved rows, with no ambiguous identifiers. Of 703 displayed dead-money rows, 476 have a game-player identifier; former/unrostered players and source residuals remain valid separate debt inputs rather than invented roster entries. Summary URLs/hashes/conflicts are in docs/data/nfl-contract-identity-audit-2026-10-26.json; full audit is .artifacts/otc-2026-10-26/identity-audit.json. No franchise data/status/contract/debt changes occur. Identity match does not prove contract cash, guarantee vesting or period alignment; retain those independent reconciliation requirements. This provides a strict basis for the next versioned source-obligations pack while isolating the remaining manual identity/date review.


## Additional market-pricing gate

Code audit: the base market formula currently uses one non-QB salary curve for every other position, while recent comparable demands are capped at 125% of that curve. This can understate elite WR/edge markets and overstate specialist pay even when imported APYs are available. Before source-default promotion, calibrate position-specific salary/cap-share distributions from verified deals, separate fixed rookie/discount/practice deals from open-market comparables, and retain cap-growth normalization for future years. Agent demands, CPU bids, extension acceptance and trade contract-surplus valuations must use the same calibrated market, with legal minimums, negotiation personality and competition retained. Validate several future seasons under the revised prices rather than simply raising every generated salary. This is a remaining financial-model task, not a claim of completed source reconciliation.


Pending-offer audit follow-up: ProEconomy.Pending currently floors bonus/years, while current-year offer/sign checks use the first-year remainder charge. GM and trade future planning duplicate annual bid amounts without applying the future service-year minimum salary escalation used by actual signed contracts. Centralize a bid-year obligation quote (legal year-specific minimum plus exact allocated bonus remainder), then share it between current reservations, unit/team future review and trade AI. Add a lowball multi-year rookie/minimum case and odd-dollar bonus case; preserve negotiated existing contracts and avoid turning a projected future cap advisory into a new head-coach legal restriction.

Pending-offer implementation checkpoint: the active coordinator batch now centralizes BidCharge/PendingForYear and uses exact bonus remainder plus future service minimums in current reservations, GM unit/team projections and trade AI. Targeted exact-quote/signed-deal and future-rejection cases pass; final batch verification is recorded in current-work.md. This supersedes the pending-offer code-gap paragraph above without changing any existing negotiated contracts or source salaries.

## Primary roster/event review of the ten identity conflicts

Developer evidence in docs/data/nfl-contract-status-review-2026-10-26.json records direct team roster/transaction/biography links for all ten cases. Wills, Kinnard, Sorsdal and Reed show why historical team financial rows must not overwrite correct current roster ownership. Hill, DeWalt, Diggs and Okoronkwo have official practice-squad evidence despite game free-agent status. Dallas has a release/practice-signing/elevation sequence, and Keys has an official release; neither is safely represented by selecting one financial category as an active contract. This resolves the interpretation of these review flags, not complete contract amounts, future guarantees or the22 unresolved identities. No app data or save is changed; source-default promotion still requires versioned obligations and safe migration.

## Canonical-rating positional market observation — October27

`dotnet run --project tools/ProContractAudit/ProContractAudit.csproj -- --market docs/data/pro-market-calibration-2026-10-27.json` produces a developer-only report using the actual canonical-rating/source-contract initialization path. Both audit modes compile/run. The unchanged ledger mode still produces32 unique teams. The market report accounts for all2,531 contracts:1,113 source veteran samples,578 original rookie/unknown-young terms excluded,316 practice deals excluded,519 generated/unsourced excluded and5 outside the recent playing window excluded. It retains pack SHA256 provenance, individual reported APY versus existing curve estimates, position/specialist-role and rating-band distributions. New veteran extensions after rookie signing year remain eligible; age alone is not a rookie test. These are packaged source headline averages, not verified current cap, guarantees or source-debt reconciliation. No historical signing-year cap normalization has been applied.

Measured medians demonstrate that a flat positional multiplier is insufficient: QB70s have reported APY$2.62m versus the old curve$11.85m, QB80s$44m versus$18.79m and QB90s$55m versus$34.38m. WR90s$29m versus$12.58m; DE90s$28.2m versus$12.58m. Punter80s$2.5m versus$7.08m. Samples within some elite/specialist bands are sparse and individual discounts remain legitimate. Next fit bounded monotone rating/position curves with sparse-sample safeguards, explicit current-cap normalization and one shared market for agent/CPU/extension/trade demand. Preserve all signed contracts and minimum/rookie rules; validate multi-season balance before promoting new pricing or real-source default. This report is evidence for implementation, not a new in-game pricing model or a claim that all source contracts are reconciled.

## Pricing candidate / source-debt gate

The15-curve source/rating quantile candidate is implemented locally, with CPU future-depth reserve, exact-year option pending checks and non-rookie/non-practice agent comparables. It is not accepted: the sourced three-season scenario still exposes PHI2029 expired void debt and insufficient fieldable depth. Smith/Mailata/Barkley are inactive after2028 expiry; their remaining proration must not be removed to balance the team. Resolve forward financial planning and legally dated termination/void accounting rather than applying cap forgiveness.

Primary rules checked October27: [NFL Operations contract language](https://operations.nfl.com/calendar-events/nfl-free-agency/contract-language) and [executed NFLPA CBA](https://nflpaweb.blob.core.windows.net/website/PDFs/CBA/March-15-2020-NFL-NFLPA-Collective-Bargaining-Agreement-Final-Executed-Copy.pdf), Article13 Section6(b)(ii). In the applicable non-final league years, post-June1 releases/trades retain current proration in-year and accelerate remaining future bonus into the following year; pre-June1 terminations accelerate into the current year, subject to tightly limited designation rules. Existing code accelerates every release/trade bonus into the current year and has no calendar/designation model. Do not pretend already-expired source debt qualifies for a new designation. Any implementation needs explicit phase/date mapping, cash/debt conservation, consistent trade/release previews and unchanged historical saves. This rules audit is a prerequisite, not an implemented feature or complete guarantee reconciliation.

## Cash/rollover lifecycle correction

[NFLPA minimum cash explanation](https://nflpa.com/faq/how-is-the-salary-cap-adjusted) confirms that the90% requirement applies across2024–2026 and2027–2030 windows; it is not a requirement to consume90% of each annual adjusted cap. The old CPU bonus target could unnecessarily burn carryover ahead of known void bills. Current repair pays optional floor-funding bonuses only at the end of a fully tracked window and only against an actual cash shortfall. Missing pre-franchise years are unknown, not zero; no floor penalty is synthesized for the incomplete2024–2026 starting window. Existing booked history and debt are not rewritten. A separate conservative future-deficit buffer limits discretionary CPU FA spending while keeping funds for current roster holes; it is not an invented liability/rollover balance. The precise previously failing three-season imported-cap/PHI lifecycle now passes with unchanged debt/roster assertions; full long/model/device acceptance is still pending. This resolves the specific spending/planning defect, not all source guarantees/departed-debt gaps.

## Future bonus-cash/proration separation audit

Developer-only build/audit-pro-bonus-events.py and docs/data/pro-bonus-event-audit-2026-10-27.json identify95 packaged source contracts with$2,859,652,573 of scheduled bonus cash after2026. This is scheduled cash, not proof of prior payment or unconditional guarantees. A candidate up-to-five-year allocation of known cash events leaves nonnegative source-proration residuals for79 contracts, a small rounding residual for1, and does not reconcile for15. Missing accounting-year checks are included. No franchise/debt/contract is changed; the report is not a migration or legal guarantee decision.

Example: JSN records35m cash bonus in2026,30m in2027 and10m in2029. His2027 SourceGuaranteed32,630,996 equals base1,530,996 plus other cash1.1m plus scheduled bonus30m; the2029 SourceGuaranteed field is zero. That illustrates why base guarantees, future bonus guarantees/vesting and already-earned bonus proration must be separate, rather than summing all future proration as already paid or assuming every scheduled option is owed. The zero field alone does not establish legal termination rights. Next verify cash-event type/vesting against primary contract terms, account for prior-paid proration residuals and isolate the15 incompatible cases before safe source-pack/accounting migration and default promotion. Preserve every negotiated deal and historical debt; do not convert the audit's arithmetic coverage candidate into a guarantee automatically.

## Event/vesting review after accepted Pro62

Pro62 financial/core calibration is accepted; source-default promotion is still separate. Developer review in data/pro-contract-event-review-2026-10-27.json records a concrete conflict between scheduled cash, paid proration and later guarantee triggers. The club confirms the extension but does not publish full accounting terms. OTC reports a guaranteed future option and later vesting, with estimated void treatment. Therefore neither summing every future allocation as already paid nor deleting all unpaid-option exposure is safe. Next introduce explicit paid-bonus balance, guaranteed future cash and dated vesting, requiring corroborated event data and strict migration guards. Incompatible source-event splits remain unresolved; keep signed saves and historical debt intact. No app accounting/source data changes in this review.

## Offline source bundle and row-level ledger review (October27)

`build/prepare-pro-contract-sources.py` consolidates reviewed raw evidence under `data/pro/source-snapshots/2026-10-26/`, outside disposable build artifacts. It verifies the original Parquet hash and all32 team-page hashes, preserves NFLPA/identity/status observations, retains the complete53,507 raw historical rows and produces2,540 integer-dollar current records (2,532 unique OTC IDs). Eight duplicate current identities are retained explicitly, not resolved by row order. The tracked manifest is docs/data/pro-offline-source-manifest-2026-10-26.json. Byte-identical offline regeneration passed. Raw sources/notes are ignored local developer evidence, not app resources; preserve them during artifact cleanup.

`build/audit-pro-opening-ledger.py` accounts for all2,539 current source-table rows:1,997 cap matches,490 missing packaged source contracts,32 identity/status reviews and20 cap differences. It retains every team's named dead-money rows and unallocated reported residual separately. These counts describe source rows, not unique game contracts or new booked debt. The full comparison is docs/data/pro-opening-ledger-comparison-2026-10-27.json. Seattle:62 matching current rows,15 missing packaged contracts,2 identity reviews; reported dead4,844,656 with1,333,838 not assigned to named rows. No opening room or balancing liabilities are imported.

The20 differences require dated review rather than blind overwrite: current-table observations and the morning Parquet can reflect later transactions, practice-squad payments, elevation charges and salary credits. For example, Dalton has a zero2026 annual Parquet row while the team table shows a nonzero current charge; Vera-Tucker differs by1.5m; several recently elevated players differ by310k. These are facts to reconcile, not permissions to manufacture cash or replace signed saves. Future bonus notes are being captured in a separately dated October27 bundle, with missing/failed observations explicit. No source-default or app contract changes occur in this developer preparation batch.

## Waiver-assumption dependency

The current engine signs a new market one-year contract after a successful claim, which does not preserve the waived contract. CBA Article13 Section6 states that signing-bonus allocation stays outside the claiming team's salary, while assigned salary obligations remain relevant. References: https://nflpaweb.blob.core.windows.net/website/PDFs/CBA/March-15-2020-NFL-NFLPA-Collective-Bargaining-Agreement-Final-Executed-Copy.pdf and https://overthecap.com/collective-bargaining-agreement/article/13/section/6 . The broader bonus classes must be verified before treating waiver assignment as identical to a trade. Existing ProTrade.Move transfers future BonusCash/OtherCash, and is not automatically a correct waiver implementation.

Next transaction design must preserve original remaining terms/rookie origin, snapshot the waived contract, identify its release-generated debt separately from unrelated historical debt, transfer only supported obligations, release the former club's transferred guarantees without deleting retained paid-bonus debt, and recheck actual current/future cap/roster/role limits at both claim and award. Old waiver saves without sufficient snapshots must not have historical charges guessed away. Finish paid/future/guaranteed bonus semantics before publishing a general assignment migration. This review changes no runtime waiver or termination behavior; those tasks remain open.


## Active contract and waiver batch — acceptance in progress

Pro63 remains the latest accepted APK. Current candidate adds original-contract waiver assignment; dated June1 release/trade splits, two early designations with held savings and final2030 treatment; paid/unpaid/conditional bonus and published vesting metadata; release cash/cap previews; and a near-header waiver claim action. Trade and waiver follow-up cases prevent paid bonuses or already-earned salary from becoming duplicate buyer guarantees. Legacy saves retain booked debt and negotiated contracts; metadata migration requires exact unchanged annual terms.

447 previously absent strict-identity, exact-cap, published-cash contracts have been added to the optional sourced pack (2459 total), preserving every existing source record. Published rounding allocations are retained; an explicit payment event is attached only when its complete annual allocation matches exactly. New first-round/rookie terms use published rows, with unreported terms left to the existing explicitly estimated fallback. Source pack SHA is c1a2ad199b9689637ad2f1eafa2237b92e028c81beaa764351f7834ecb41628b. Reviewed JSN record equality was checked before updating its hash guard. No saved contracts are replaced by these additions.

Fresh October27 offline evidence now contains all32 internally reconciled team pages,507 opening-contract pages and95 future-bonus pages. Dated raw pages/manifests remain in data/pro/source-snapshots. The new current ledger comparison has2444 exact cap matches,42 missing source contracts,32 identity/status cases and21 differences across2539 financial rows. These are source financial events, not2539 unique roster contracts. Reported room is never a balancing target. Real contracts remain opt-in: guarantees/vesting review, departed-debt application and identity/date discrepancies are still open; all four requested tasks are not yet complete.

Verification:234 fast Pro cases passed after the447 additions;17 targeted source/waiver/termination cases passed, then16 targeted cases passed after the vested-assignment fix and new real-loader device fixtures.16 UI helpers pass. The full current Pro suite including20 generated/five sourced seasons is active (session92669, .artifacts/termination-full-final.txt); do not compile or duplicate it. Earlier local Android build passed before the final source/UI additions and is not final acceptance. Finance device flow now includes core-generated32-team June1 and waiver fixtures; final APK/device acceptance is pending. Reuse unchanged College390 and Pro44 GameCast evidence. No main/College/package identity changes. User removed automatic continuation; do not recreate it. Continue during active work through remaining tasks, without treating this checkpoint as completion.


## Accepted64 and salary-assignment follow-up

Pro64 app8b06fa7 is accepted: APK12754790693 in run40547793852, exact-APK verification40573852505 at harness00944a7, evidence12766593020. Finance/options/trade/coordinator and new June1/waiver/reload flows passed.239 full local cases passed5m22;237 scoped CI cases/16 helpers/source validator/packaging passed. Earlier fixture-glob failure is superseded.

Follow-up records salary assignment start year/fraction, preventing retroactive buyer earnings. Repeated assignments preserve original signing/lock terms; real-loader round trip preserves existing debt. Trades transfer unpaid guaranteed options to the buyer, with paid bonus charges retained by seller; waiver non-salary treatment remains separate. All245 full local cases pass10m6, including20 generated/five sourced seasons, plus16 helpers/five parser cases. Source pack now2482 records after23 additional exact-cap/cash-proof recoveries; old records and saved contracts are unchanged. Extension new-money charts and unsupported cash differences are rejected. Source default remains off while broader guarantees, status and debt are unresolved.

Developer-only staging contains735 actual2026 debt events totaling1,591,660,715 across32 reconciled teams, with source aggregate remainders kept unnamed. No historical cash, future debt or opening room is inferred; this staging is not yet applied to games. Current audit2467 exact financial rows,19 missing source records,32 identity/status cases and21 differences.37 payment/guarantee candidates are staged for review;34 current candidate cuts match both source pre/postJune1 figures (35 total including an already-matching noncandidate). Matching quotes alone does not verify future vesting dates.


### Assigned source contracts and remaining Seattle reconciliation

Avery Smith's source note names the Chargers as the original signing club, while the strict current financial row is Seattle. The source current annual table has no paid bonus allocation; player-wide cash-flow charts retain original signing cash. Do not charge that old club's bonus to Seattle. A reviewed assignment record can preserve the published remaining salary/guarantee years while excluding original-club signing cash, provided current ownership/status and paid-bonus source figures are verified. The same issue affects other previously waived/assigned rookies; it is not a license to drop an unexplained cash difference.

Primary Seattle event review identifies Foster's October24 waiver and Maranges' designated international practice slot. Article33 excludes designated international weekly pay from Team Salary, not every foreign player's pay. A future explicit exemption flag must end its cap/slot effect on active promotion; do not manufacture an offsetting charge or mark someone exempt from a name or nationality. Trevon Diggs' primary practice-squad and temporary elevation events are separate from his old Cowboys extension. His captured current practice table reports220200 cap, with330300 annual salary rate; preserve the partial-year accounting rather than reactivating the old extension or forcing all-year minimum cash. None of these staged status/contract reviews has changed a saved franchise.


## October27 opening-assessment correction (pending APK acceptance)

New sourced franchises now initialize735 separately published prior-contract cap events across32 teams. Keys/names/source hashes/dates and unresolved identities are retained; current-year cap assessments do not invent historical cash or future debt. Existing saved franchises are never automatically backfilled. Separate reviewed Seattle roster/current-club terms and an explicit international practice exemption produce actual runtime liabilities300971343, matching the dated table without reported-room input. NFLPA allowance315053262 yields14081919; the other source allowance is different. Further current-contract/guarantee reconciliation remains open and default stays generated. See current-work.md for tested source and acceptance scope.


## Guarded roster-pay allocation follow-up (pending next APK)

Roquan reviewed metadata moves the2026 published4m roster guarantee out of P5 base guarantees and classifies the2027 unexplained1m annual cash remainder as non-prorated other cash. The published option-proration column does not increase, so this cannot be staged as a fresh option allocation merely because existing option proration is nonzero. Annual cash/cap totals and source pre/post cut observations are conserved. Raw source pack remains unchanged and reviewed corrections are hash guarded. Migration requires exact original/corrected financial basis, untouched source origin and no negotiation lock; negotiated/assigned/terminated deals and booked assessments are preserved. Existing pending original-contract waiver snapshots can receive the same guarded classification correction. See current-work.md for actual test/APK acceptance.
