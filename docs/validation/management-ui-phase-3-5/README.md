# Management UI follow-up — Build 307 candidate

Application source `e65c03b`, branch `feature/management-ui-overhaul`, PR20 targeting `game-day-2-test`. No merge performed.

[Actions run](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40022397222)

## Changes

- Sidebar initial/year badge replaces wrapping full team names; full team identity remains in the accessibility description and page banners.
- Roster, recruiting and other dense person tables use text-only start-aligned links. Portraits remain on detailed profiles and editorial news cards.
- News cards include a team initial or player portrait, recorded-event preview and offline stadium illustration. Articles add clearly labeled fictional editorial context without fabricated quotes, statistics, injuries or outcomes. Cards form one/two/three columns as width permits.
- Program tabs separate career, staff/hiring/carousel, history/titles and development. Development lists actual morale, season deltas and availability. Career/coach information and actions remain connected to the same services.
- Staff candidates use a searchable, sortable comparison table with coach profiles and the original hire confirmation. Browsing/canceling does not hire anyone.
- Coach profiles have overview/attributes/career tabs and responsive identity/details panels. Team browser separates roster and staff; commitments/class rankings/national commits use sortable tabbed tables.
- Schedule/poll filters wrap, program season history uses a table, and preferences use responsive display/delegation/save/league panels. Existing depth chart, statistics, conference selection, postseason, save library and phase-aware portal actions retain shared theme/table styling.

## Validation status

Local:647 core/data tests passed;9 UI synchronization helpers passed; packaged resources and diff checks passed. Android compile passed with five existing warnings. The expensive long-term rating suites are excluded for this presentation-only change.

GitHub: signed APK built and build/test job passed. Full emulator verification and screenshot review are pending; do not treat this candidate report as a completed delivery yet.

Production Core formulas/generation, save/persistence models, package/signing identity and Game Day visualization/playcalling are unchanged from UI base `1dd2b72`. No synced reference files under sources/ were edited.

The emulator tests use Android36, startup density with360/700/1200dp window resizing, native UI-tree-derived coordinates, screenshots and runtime logs. Real fold hinges/cutouts, a broad physical device matrix and every possible staff-hire outcome are outside this device pass; existing core tests cover hiring behavior.
