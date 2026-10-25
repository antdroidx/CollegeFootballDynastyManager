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

## Foundation snapshot and follow-on work (not represented as complete)
The first preview established the foundation above. The next branch increment replaces the new-franchise pack with real NFL identities and starts Phase 3/4 economics, draft, roster and staff AI. See `pro-data-formats.md` for precise supported behavior and remaining CBA/career/conformance work. Full official tiebreak procedures and multi-decade calibration remain pending. New functionality is confined to ProFootball.Core/App except opt-in palette/logo hooks and behavior-preserving shared defensive choreography helpers. College defaults and storage remain unchanged.

## Regression controls
Preserve college package IDs, signing key, serialized type names, assembly identity, database paths and schema. Run college tests and compile both hosts after extraction; add pro schedule/save/playoff/rules tests; separate APK CI and emulator smoke. Preserve college release and workflow. Feature branch only; no merge into stable main.

## Building and validation
- College: `dotnet build src/DynastyManager.App/DynastyManager.App.csproj -c Debug`.
- Pro: `dotnet build src/ProFootball.App/ProFootball.App.csproj -c Debug`.
- Pro tests: `dotnet test tests/ProFootball.Tests/ProFootball.Tests.csproj -c Release`.
- College regression: the existing Core.Tests project path stays unchanged.
- Solution contains both hosts; install the MAUI Android workload before restoring the solution.
- College workflow remains `.github/workflows/csharp-ci.yml`; separate pro artifacts and device evidence are produced by `pro-android.yml`. Pro version codes are the pro workflow run number, independently increasing; the package and stable test signing key allow in-place updates. These are testing APKs, not production Play Store releases.

## First device evidence
Initial pro candidate passed launch/create, CPU watch, replay, rotation, save/reload, advancing a week and coach-mode play selection on Android API 36. Screenshot review prompted smaller landscape field framing and use of shared offensive alignment (to prevent overlapping linemen). The final candidate additionally checks update-install preservation and light/Fold display. Refer to the final Actions run for actual final status rather than treating this planned checklist as proof.
