# Management UI architecture

## Audit

The Android client is .NET MAUI/C# with a single `FoundationPage` split into section files, a management section host, modal profiles and separate Game Day modals. Native MAUI controls are used throughout. Existing `DataTable` provides sort state/numeric ordering; recruiting already prepares a bounded 80-row snapshot off-thread with cancellation. `DynastyTheme` supplies implicit styles and legacy explicit-color translation. `PersonPortrait` draws deterministic fictional cartoons keyed by character identity and saved NCAA appearance. No new framework, imagery service or external assets are needed.

## Foundation

`ManagementLayout` documents 600/840dp breakpoints. `FoundationPage.Management` provides a persistent sidebar from 600dp, a compact expand/collapse menu plus familiar bottom navigation below 600dp, responsive panels, tabbed profiles and roster/recruit workspaces. A selected tablet player remains beside the roster; compact profiles open as full-screen modals. Resizing moves the same views; it does not mutate dynasty state.

The existing palette now uses navy panels and off-white light surfaces. `System` follows Android appearance; explicit-color management controls rebuild around the same live dynasty and repository. OS theme changes while a modal is open are deferred until it closes. Active dynasty updates also finish before the page rebuilds, so the current continuation commits to its original model owner. Native system-bar icons follow the selected appearance. Brand accents are blended for readable navigation while school badges preserve original colors and initials. Existing cartoon portraits, including imported appearance, remain deterministic.

Shared tables retain keyboard/touch sorting and horizontal scrolling. Roster and recruit tables add saved Essential/All column presets. Important columns lead; extra physical, origin and competition details remain reachable. Recruiting retains the existing worker preparation, cancellation and 80-row bound and yields while constructing rows; no full-database native view inflation.

## Functional screens

Dashboard uses existing record, poll, next-game/injury entry points, real news and task shortcuts. Roster keeps search, position/role/sort filters, existing depth-chart editor and automatic assignments. Profiles expose existing ratings, physical data, recruiting stars, morale, progression, injuries, honors and career/statistics navigation. User-team management actions retain their existing phase rules.

Recruiting uses the same existing source/view filters and mechanics. Prospect rows open an overview/recruitment workspace, with scouted ranges and revealed priorities only. Board and priority shortcuts call the existing services. Offers/pitches remain phase-aware. No invented traits, salaries, visits or fabricated data were added to match mockups.

## Boundaries and remaining phases

This phase changes presentation only. Simulation, POCI/calibration, recruitment logic, generation, persistence models and package/signing identity are unchanged. Game Day field visualization and playcalling source files are unchanged by the overhaul; existing Skip/Close fixes are inherited from the branch base. Shared branding/styles can also appear in existing secondary screens; those screens are not claimed as individually redesigned. Staff, league information and final polish continue in Phases 3–5 when requested.

## Validation

Compile Android after integration; run existing core/data tests, resource and synchronization helper checks. GitHub Actions installs the signed test APK into Android 36 and exercises career navigation, Game Day skip/close, depth chart, recruiting and theme retention, then 360dp compact, 700dp foldable and 1200dp tablet window sizes. Inspect screenshots and runtime failures before delivering the artifact. Window resizing validates responsive policy; physical fold hinge/cutout behavior still requires device testing.

Final Phase1–2 evidence: [Build306 validation and screenshots](validation/management-ui-phase-1-2/README.md).


## Remaining management phases (Build 307 candidate)

The Oct23 follow-up extends the existing components rather than introducing a second UI framework. Sidebar identity uses only the team initial/year, with the full name retained in accessibility text and normal page banners. Dense person links use text-only buttons aligned to the start of their cells; deterministic portraits remain in profiles and news. This removes repeated avatar measurement and appearance lookups from recruit table construction.

| Area | Presentation and existing actions |
| --- | --- |
| Portal / commitments | Portal shares the recruiting workspace and phase-aware actions. Commitments, national commitments and class rankings now have separate sortable table tabs. |
| Staff / hiring / carousel | Program tabs separate career, staff/hiring, history/titles and development. Carousel records remain in staff. Candidate comparison is a searchable, sortable table with profile links and the existing confirmed hiring path; dismissing it makes no hire. |
| Coach profile | Responsive identity/details layout with overview, attributes and career-history tabs; real staff labels, shared roles and generated estimates remain disclosed. |
| Team browser | Team ratings and banner precede roster/staff tabs, preserving league-wide profile links and roster morale/deltas. |
| Depth / injuries / development | Existing legal PPOS organizer and Auto Set stay intact. Program development adds actual season deltas, morale, availability and injury-report navigation in a sortable table. |
| Schedule / standings / postseason | Existing organized tables, conference picker and postseason panels reuse shared styling. Schedule/poll filters wrap within narrow content widths. All-team schedule still defaults to the current week. |
| Statistics / history | Existing paged, searchable category statistics retain their fields and sorting. Program season history now uses a sortable record/honors table. |
| News / weekly coverage | Team or player icons, recorded-event previews and native vector stadium illustrations. Responsive card columns lead to detailed articles with recorded facts plus explicitly fictional editorial context. No quotations, results or stats are invented. |
| Preferences / saves | Responsive display, recruiting-delegation, save-protection and league-configuration panels. Existing System/Light/Dark rebuilding, autosave controls, save/start library and package identity remain intact. |

Management tabs remember selection across model refreshes and explicit theme rebuilding. Existing bounded recruiting preparation, table presets, horizontal scrolling, rating indicators and accessible button identities are shared across these screens. Graphics are offline code-native illustrations and deterministic cartoon portraits; no licensed sports-game assets, photographs or real team logos are added.

The Game Day field and play-calling UI remain excluded. No simulation, POCI, development, recruiting, generation, persistence schema or signing changes are part of this follow-up. Validation records distinguish emulator coverage from physical-device/hinge testing; the latter still needs real hardware.
