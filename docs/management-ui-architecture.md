# Management UI architecture and Phase 1–2 scope

## Audit

The Android client is .NET MAUI/C# with a single `FoundationPage` split into section files, a management section host, modal profiles and separate Game Day modals. Native MAUI controls are used throughout. Existing `DataTable` provides sort state/numeric ordering; recruiting already prepares a bounded 80-row snapshot off-thread with cancellation. `DynastyTheme` supplies implicit styles and legacy explicit-color translation. `PersonPortrait` draws deterministic fictional cartoons keyed by character identity and saved NCAA appearance. No new framework, imagery service or external assets are needed.

## Foundation

`ManagementLayout` documents 600/840dp breakpoints. `FoundationPage.Management` provides a persistent sidebar from 600dp, a compact expand/collapse menu plus familiar bottom navigation below 600dp, responsive panels, tabbed profiles and roster/recruit workspaces. A selected tablet player remains beside the roster; compact profiles open as full-screen modals. Resizing moves the same views; it does not mutate dynasty state.

The existing palette now uses navy panels and off-white light surfaces. `System` follows Android appearance; explicit-color management controls rebuild around the same live dynasty and repository. OS theme changes while a modal is open are deferred until it closes. Brand accents are blended for readable navigation while school badges preserve original colors and initials. Existing cartoon portraits, including imported appearance, remain deterministic.

Shared tables retain keyboard/touch sorting and horizontal scrolling. Roster and recruit tables add saved Essential/All column presets. Important columns lead; extra physical, origin and competition details remain reachable. Recruiting retains the existing worker preparation, cancellation and 80-row bound and yields while constructing rows; no full-database native view inflation.

## Functional screens

Dashboard uses existing record, poll, next-game/injury entry points, real news and task shortcuts. Roster keeps search, position/role/sort filters, existing depth-chart editor and automatic assignments. Profiles expose existing ratings, physical data, recruiting stars, morale, progression, injuries, honors and career/statistics navigation. User-team management actions retain their existing phase rules.

Recruiting uses the same existing source/view filters and mechanics. Prospect rows open an overview/recruitment workspace, with scouted ranges and revealed priorities only. Board and priority shortcuts call the existing services. Offers/pitches remain phase-aware. No invented traits, salaries, visits or fabricated data were added to match mockups.

## Boundaries and remaining phases

This phase changes presentation only. Simulation, POCI/calibration, recruitment logic, generation, persistence models and package/signing identity are unchanged. Game Day field visualization and playcalling source files are unchanged by the overhaul; existing Skip/Close fixes are inherited from the branch base. Shared branding/styles can also appear in existing secondary screens; those screens are not claimed as individually redesigned. Staff, league information and final polish continue in Phases 3–5 when requested.

## Validation

Compile Android after integration; run existing core/data tests, resource and synchronization helper checks. GitHub Actions installs the signed test APK into Android 36 and exercises career navigation, Game Day skip/close, depth chart, recruiting and theme retention, then 360dp compact, 700dp foldable and 1200dp tablet window sizes. Inspect screenshots and runtime failures before delivering the artifact. Window resizing validates responsive policy; physical fold hinge/cutout behavior still requires device testing.
