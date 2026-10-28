# Pro compensatory draft allocation

Implemented October 28, 2026 on feature/pro-franchise-foundation. Pro-only financial/roster module; no College or shared simulation behavior changes.

## Game rules

Natural expired contracts produce a candidate only for a non-practice, non-retired player with at least four credited seasons (including a completed season with at least three game appearances). A qualifying player signing for another club during offseason free agency or the draft creates a loss for the former club and a gain for the signing club. Own-team renewals, street free agents, rookies and waiver assignments do not create credits. Signing after the draft does not qualify. A release before regular week 10 invalidates that signing's eligibility; a later release does not erase earned credit. The game's offseason stages approximate the real Monday-after-draft deadline; it does not simulate wall-clock transaction dates.

Round values use the public APY percentile bands against the previous season's contracted league population: top 5/10/15/25/35 percent yields rounds 3/4/5/6/7. Ten-season non-quarterbacks cannot yield above round 5. This is a transparent game approximation: it does not reproduce private NFL Management Council snap/honor scoring or every CBA exception. Gains cancel equal-round losses first, then less valuable losses, then the highest remaining loss. Up to four net-loss awards per club and 32 across the league. Special, explicitly sourced staff-development entitlements are separate from that quota; the game does not infer a coach's protected characteristics or create automatic staff awards from portraits.

Awards occur before generating that year's draft. A signing in the 2027 market affects the 2028 draft, not 2027. Award years, eligible departures/signings and stable award pick IDs persist in saves. Extra picks follow the normal choices in each round, with special staff picks after the ordinary compensatory third-round choices. Draft overall numbers, values, rookie numbering, automatic completion and inventory maintenance account for the extra selections. Awarded picks can be traded; projections cannot.

## Opening snapshot

New franchises have an October 28, 2026 projection snapshot from [Over the Cap's public cancellation/projection chart](https://overthecap.com/compensatory-picks#y2027), retrieved through its public get_compensatory_picks endpoint. It contains 32 ordinary projected 2027 selections plus Seattle's special third for Nolan Teasley. Seattle's second special third is separately forecast for 2028. These are game opening estimates, not announced final NFL awards. They become game allocations in the corresponding offseason. Later ordinary allocations derive from actual game transactions; a seeded special 2028 entitlement must not suppress simulated 2027-market losses/gains. Existing saves receive no fabricated historical signing records or retroactive opening projections.

The Draft picks screen supports every team/year, shows original ownership and allocated compensation, and lists projections separately. Ordinary rookies still have four-year deals and first-round fifth-year decisions remain separate.

## Real roster update

New franchises apply Nelson IND to SEA for SEA2027R3 and SEA2028R5; Buckner IND to SF for SF2027R4. Existing trade-assignment accounting retains the seller's paid bonus obligations and transfers remaining playing salaries. No trade AI approval is required for a confirmed starting transaction. Existing saved careers and negotiated deals are not overwritten. Buckner retains his calibrated DT position and attributes.

Primary confirmations: [Colts/Nelson](https://www.colts.com/news/colts-trade-g-quenton-nelson-to-seattle-seahawks-for-2-future-nfl-draft-picks), [Colts/Buckner](https://www.colts.com/news/colts-acquire-2027-fourth-round-pick-in-exchange-for-dt-deforest-buckner-in-trade-with-san-francisco-49ers), [Seahawks](https://www.seahawks.com/news/seahawks-acquire-all-pro-guard-quenton-nelson-in-trade-with-colts-pending-physical).

## Outstanding ownership audit

The broader real 2027-2029 pick ownership import is NOT complete. docs/data/pro-future-pick-audit-2026-10-28.json is developer research only and is not embedded or applied to franchises. It records 70 candidate transfer rows, primary references and explicitly unresolved multi-hop/conditional identities. Some team transaction descriptions conflict about years or omit which acquired pick was sent onward. Do not promote candidate rows as verified allocations, silently duplicate a pick, or claim all 32 real inventories are reconciled. Only the three picks in the two confirmed opening trades change ownership in this implementation batch. Remaining work: reconcile original identities/conditions and implement conditional-right reservation/resolution before importing the full ledger.

## Rule references

- [NFL Football Operations draft rules](https://operations.nfl.com/calendar-events/nfl-draft/nfl-draft-rules)
- [NFLPA collective bargaining agreement, Appendix V](https://nflpaweb.blob.core.windows.net/website/PDFs/CBA/March-15-2020-NFL-NFLPA-Collective-Bargaining-Agreement-Final-Executed-Copy.pdf)
- [Public compensatory formula methodology](https://overthecap.com/compensatory-formula)
