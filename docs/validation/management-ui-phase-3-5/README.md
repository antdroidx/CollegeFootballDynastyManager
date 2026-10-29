# Management UI follow-up — Build 309

Application source `6a0540e`, branch `feature/management-ui-overhaul`, PR20 targeting `game-day-2-test`. No merge performed.

[Download test APK](https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40027935772/artifacts/12532894496) · [Passing Actions run](https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40027935772) · [Full screenshot/XML evidence](https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40027935772/artifacts/12534078761) · [PR20](https://github.com/antdroidx/Football-Sim-Engine/pull/20)

## Changes

- Sidebar initial/year badge replaces wrapping full team names; full team identity remains in the accessibility description and page banners.
- Roster, recruiting and other dense person tables use text-only start-aligned links. Portraits remain on detailed profiles and editorial news cards.
- News cards include a team initial or player portrait, recorded-event preview and offline stadium illustration. Articles add clearly labeled fictional editorial context without fabricated quotes, statistics, injuries or outcomes. Cards form one/two/three columns as width permits.
- Program tabs separate career, staff/hiring/carousel, history/titles and development. Development lists actual morale, season deltas and availability. Career/coach information and actions remain connected to the same services.
- Staff candidates use a searchable, sortable comparison table with coach profiles and the original hire confirmation. Browsing/canceling does not hire anyone.
- Coach profiles have overview/attributes/career tabs and responsive identity/details panels. Team browser separates roster and staff; commitments/class rankings/national commits use sortable tabbed tables.
- Schedule/poll filters wrap, program season history uses a table, and preferences use responsive display/delegation/save/league panels. Existing depth chart, statistics, conference selection, postseason, save library and phase-aware portal actions retain shared theme/table styling.

## Verification

Local:647 core/data tests passed;9 UI synchronization helpers passed; packaged resources and diff checks passed. Android compile passed with five existing warnings. The expensive long-term rating suites are excluded for this presentation-only change.

GitHub: the signed APK, build/test job and entire Android36 emulator job passed. Matching final screenshots were reviewed. Verified compact drawer opening preserves the header bounds, close/reopen, table sorting/presets, player/recruit profiles and split panes, in-season action rules, staff-market cancel, coach attributes, program history/development, news articles, current-week all-team schedule, conference/postseason/statistics navigation, and Light/Dark/System retention. Existing Game Day entry/watch/overlays/stats/replays, Skip cancel/confirm, saved final statistics, Close, week advancement, depth Auto Set, recruiting, departures and Hall of Fame all passed. Theme changes during week advancement retain the completed week.

Compact navigation overlays the full-width page, with scrim, close control and Back handling. Persistent foldable/tablet sidebars keep their column. This addresses the narrow-header defect observed during screenshot review. No further scoped work is pending.

Production Core formulas/generation, save/persistence models, package/signing identity and Game Day visualization/playcalling are unchanged from UI base `1dd2b72`. No synced reference files under sources/ were edited.

The emulator tests use Android36, startup density with360/700/1200dp window resizing, native UI-tree-derived coordinates, screenshots and runtime logs. Real fold hinges/cutouts, a broad physical device matrix and every possible staff-hire outcome are outside this device pass; existing core tests cover hiring behavior.


## Screenshots from the final APK

| View | Evidence |
| --- | --- |
| Compact overlay drawer | [Screenshot](compact-drawer.png) |
| Aligned compact roster | [Screenshot](compact-roster.png) |
| Aligned recruit table | [Screenshot](tablet-recruiting.png) |
| Light news cards | [Screenshot](light-news.png) |
| Staff candidate comparison | [Screenshot](staff-market.png) |
| Player development | [Screenshot](development.png) |
| Coach attributes | [Screenshot](coach-attributes.png) |
| Current-week league schedule | [Screenshot](league-schedule.png) |
