# Pro Football Franchise Manager architecture audit

Baseline: main fdd3d4898b5bd07eeec8dba346024d61f5289474; consolidated main CI 40147357019 passed build, Android launch, and coordinator validation. College 0.20 Build 356 was accepted by the user and released before this branch.

## Existing architecture
- DynastyManager.Core: platform-independent records, game simulation, playbooks, ratings, personnel, fatigue, weather, coaching, statistics, animation planning, plus college-specific recruiting/career/season services. No MAUI dependencies.
- DynastyManager.Data: college imports, branding and SQLite save store. Keep college storage/schema unchanged.
- DynastyManager.App: MAUI Android host; FoundationPage partials contain management and live viewer orchestration. Field drawable is nested; sprites, dialogs, theme and action rows are independent.
- Existing tests: 678 fast core cases, extended career/calibration tests, real Android phone/Fold/landscape and coordinator suites.

## Phase 1 boundary
Move the existing Core implementation into Football.Engine, retaining assembly name DynastyManager.Core and all namespaces/record shapes. This is an incremental extraction, not a rewrite. College-specific adapters remain in this assembly temporarily because they and the calibrated simulation share models; split them only behind tested interfaces later. All old project references point to this one engine. No duplicate simulation.
Extract theme, dialogs, sprites, action layout and field drawable into Football.Presentation. Keep their original namespace to minimize college call-site changes. College retains its mature viewer orchestration; the pro host consumes the same rendering components and animation planner. Further extraction of the whole viewer is deferred rather than copying its 2,000-line college-coupled controller.
ProFootball.Core owns league configuration, pro player metadata, schedules, standings/playoffs, season state and independent versioned saves. ProFootball.App is a separate Android package, launcher icon and workflow; no college assets or college persistence.

## First APK acceptance
32 fictional teams; 2 conferences x 4 divisions x 4 teams; 53 active + 16 practice players; preseason, 17 regular games over 18 weeks, division rivals home/away, rotating cross-division and cross-conference opponents; standings, 14-team seeded playoffs, championship, season archive; multiple save slots/rolling autosaves; watch, call plays and simulate through the single shared engine; responsive themed shell and original shared sprite/field renderer.
Professional rules are explicit configuration. College default must remain unchanged. NFL rule references: https://operations.nfl.com/the-rules/nfl-rulebook and https://operations.nfl.com/calendar-events/nfl-schedule/making-the-schedule .

## Deferred phases (not represented as complete)
Contracts/cap/guarantees/dead money, negotiations, free agency, tags, pick trades, seven-round draft/scouting/UDFA/waivers, owner/coaching AI and professional aging are Phase 3/4. Foundation includes versioned import/export DTOs and pro-specific rating conversion, not real-world licensed data. Full NFL penalty/kickoff nuance and all official multi-team tiebreak procedures require later conformance work. Multi-decade economic realism cannot be claimed before contracts/AI exist.

## Regression controls
Preserve college package IDs, signing key, serialized type names, assembly identity, database paths and schema. Run college tests and compile both hosts after extraction; add pro schedule/save/playoff/rules tests; separate APK CI and emulator smoke. Preserve college release and workflow. Feature branch only; no merge into stable main.
