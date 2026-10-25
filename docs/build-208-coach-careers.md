# Build 208: coach career history

Coach careers now retain completed-season records by StaffId, including the user head coach, CPU head coaches and other staff roles. Names are not identity keys. A season snapshot is taken during season finalization, before staff growth and the offseason carousel. A saved year marker makes the snapshot idempotent and prevents incoming coaches from inheriting the outgoing staff's results.

Career details include head-coaching wins/losses, winning percentage, conference titles, CFP appearances, national titles, bowl record and a school/role/season resume with conference record and final ranking when known. Assistant/support-staff seasons retain the team's results and honors, while head-coaching totals include only head-coach records. A current in-progress team record is shown separately and is not added twice to completed-season totals.

The Coach tab links to the user's career and a searchable, virtualized coach career archive. The archive includes departed coaches and filters between head coaches and all staff. Program staff rows and the Teams browser also provide individual career actions.

Save schema advances to 21. Existing user career records are recovered on load using the user's persistent coach ID and matching team history for additional detail. Historical CPU/assistant identities cannot be reliably reconstructed from the current staff, so their tracking begins with the next captured season. Pre-dynasty career years on generated coaches do not create invented win/loss histories. This first version captures season-end roles; it does not split a season into midseason coaching stints.

## Deferred economy work

NIL and coaching budgets are recorded in ROADMAP.md as later work. The design backlog covers separate NIL resources and coaching/staff budgets, salaries and buyouts, sustainable multi-year spending, coach preferences and integration with competing recruiting offers. No monetary mechanics are activated in this release.

## Validation

200 focused Release tests passed, including new cases for same-name/different-ID coaches, season finalization and carousel ownership, assistant-to-head-coach promotion, school changes, departure/archive retention, current-record separation and schema-20 save migration/reload. The six longer synthetic-career tests will also run in GitHub's complete suite. Android Debug APK packaging and packaged-resource checks passed, with five existing obsolete MAUI alert API warnings. Local interactive emulator verification remains unavailable because of the adb authorization issue recorded for Build 207.
