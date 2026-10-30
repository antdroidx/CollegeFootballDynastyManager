# Expanded individual statistics

Both games share the same dense player-stat columns, season/career aggregation and box-score categories. Blocking and Playing time are new categories. Player profiles include the applicable categories.

- Blocking: pass/run blocking opportunities, wins and win percentages, pressures allowed, sacks allowed, penalties and penalty yards. OL, TE and FB contribute when actually assigned to block.
- Passing: meaningful dropbacks, pressures faced, sacks and sack yards lost, longest completion.
- Rushing: longest run and recorded broken tackles.
- Receiving: targets, catch percentage and longest reception.
- Defense: snaps, pressures, forced fumbles and coverage targets/receptions/yards allowed.
- Kicking: longest made field goal and under-40 / 40–49 / 50+ distance splits.
- Punting/returns: net punting, touchbacks and fair catches.
- Playing time: offense, defense and special-teams snaps; attributed penalties.

## Definitions and data coverage

The engine resolves individual blocker/rusher matchups at the modeled release time. Block skill, rusher strength/agility/awareness and help affect the result. A pressure is a rusher reaching the passer before release; sacks require an actual pressure. Unblocked sacks do not invent a blocker liability. A double team can include an individual lost matchup without a team pressure when the helper succeeds. Run-block wins modestly affect rushing advantage. These are simulation definitions, not licensed PFF grades or a claim to ESPN's fixed-time win-rate method.

Coverage statistics use the read's covering defender, independently of the eventual tackler. Broken tackles count the engine's actual broken-tackle events; candidate pursuit IDs are not counted as missed tackles. Forced fumbles use existing contact-fumble attribution. Current individual penalties cover the false-start/offside events the engine actually models, not unimplemented holding/DPI. No pancake, tackle-assist or invented historical stats are added.

Tracked GP states how many games have detailed event tracking. Old saved totals remain intact and new untracked fields display an em dash. Mixed old/new career catch rates use tracked receptions and targets; net punting uses tracked punts. Longest values aggregate by maximum; counts sum and percentages recompute from denominators. Resuming an old live game can provide partial coverage; old events are not retroactively reconstructed. Legacy synthetic fallback games without detailed events do not fabricate blocking data.

## Verification

726 combined Core/College cases and 354 existing fast Pro cases passed locally. A new completed-production-game Pro fixture passes save/load and snap/sack/target reconciliation. Five focused detail cases cover event-to-player sums, quality-sensitive protection, unblocked sacks, weighted career aggregation, old JSON compatibility, mixed coverage, spike/kneel classification, field-goal distance, net punting and individual penalties. Sixteen Android harness helpers pass. APK builds and scoped native statistics checks are pending; no new APK is accepted yet.

Four fixed-seed 128-game samples (512 games total) cover real/generated Pro and College rosters; raw reports are in data/expanded-statistics-2026-10-29. All sampled scrimmage units have 11 unique players per side. This is a broad gameplay guard after matchup changes, not exact historical replication or a new multi-season calibration. Existing three-season generated-player/development evidence is reused where unchanged.
