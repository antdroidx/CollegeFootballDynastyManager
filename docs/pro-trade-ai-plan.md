# Autonomous Pro trade work — next implementation batch

Existing services remain the authority: ProTrade.Propose transfers player/pick ownership and validates current cap/roster rules; ProTradeMarket supplies block candidates, package values and smallest-sufficient pick suggestions; ProManagement wraps coordinator recommendations and protects shared picks/future unit budgets. Do not build a second trade engine or silently change user assets.

## Offers to the user

Persist additive offers with buyer, user player, offered future pick IDs, created/expiry tick, decision/status and limited counter attempts. Generate only for listed/requesting players in the user's controlled unit; user-owned players are never automatically traded. Show a readable offer panel linked from Inbox/Front Office, including player, each pick/year/round and the actual accept/decline/counter decision. Accepted/countered offers must revalidate current ownership, deadline, cap and roster room. Stale/expired offers resolve without moving assets. Declines should not be regenerated immediately; cap counter attempts and offer volume to avoid spam.

For CPU-initiated buying, call the existing transaction with CPU buyer as `from` and user seller as `to`; its 5% valuation threshold then represents the initiating CPU paying for the player. Counter with a stronger legal pick package within a capped CPU willingness-to-pay; retain the original if rejected. Do not reverse direction accidentally and require the user to sell for less than an unsolicited offer. Head coaches control their own decisions; coordinator acceptance still goes through ProManagement.

## CPU-to-CPU execution

Run once per market tick and at limited points in camp/free agency/pre-deadline regular season, with a small candidate/buyer budget and at most one or two completed trades per cycle. Prefer surplus/older/requesting block players, healthy buyers' position/role upgrades and real contract fit. Require seller depth/position coverage and at least 45 active players after departure. Never sell the only player at an OL or kicking role merely because the broad position count has a spare.

Regular-season CPU rosters are usually full. A buyer may need one atomic, explicitly legal surplus-player release before acquisition. It must not cut its only role player, an essential starter or arbitrary cheap kicker. Build the whole candidate immutably and discard releases, dead money, logs and pick movements if any trade check fails. Use real release charges; never forgive cap. Revalidate full opening-day obligations in camp and current/future years against existing limits/commitments, including pending bids. Do not let temporary Top-51 exclusions justify an illegal season-start transaction.

Use existing position/age/potential/contract/need and projected pick values, require a defensible package range and avoid spending a first-round pick for a cheap reserve solely because it is the next sufficient asset. Available picks remain restricted to valid current/future inventory; automatic offers can initially use the next three classes to avoid disturbing a live draft cursor. Prevent recently traded players being flipped again immediately. Promote a newly acquired CPU upgrade into the appropriate depth-chart role with ProDepthChart.MakeStarter; user depth choices stay untouched. Existing trade history/transaction news should describe only committed events.

## Acceptance

Cover no silent user trade, offer expiry/ownership changes, no duplicate market processing, final counter limits, deadline, pick ownership over three years, same-unit coordinator restrictions, future cap/bonus/dead-money conservation, atomic buyer cut rollback, minimum seller depth/role coverage, realistic overpay rejection and actual CPU upgrades starting. Run one multi-season calibration for market frequency, roster sizes, cap legality and pick concentration. Reuse College/shared and unchanged GameCast evidence. Package a separate Pro management batch after the option APK is accepted; do not invalidate an in-flight test-only APK retry with application edits.
