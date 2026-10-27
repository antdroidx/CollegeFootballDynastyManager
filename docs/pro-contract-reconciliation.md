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
