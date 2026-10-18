# Recruit loading screen and roadmap diagnostics

## Recruit screen

A separate recruiting overlay shows an animated indicator before list preparation starts. Filtering, sorting, national rank, scout estimates and school-interest calculations use a captured dynasty/filter snapshot on a worker thread. Only MAUI view creation runs on the UI thread, in batches of eight rows. The bottom navigation remains available. Leaving Recruit or starting a replacement render cancels the previous request; version/state checks prevent stale results from replacing the current view. The overlay is cleared on success, cancellation and failure, with a retry message if loading fails. Weekly advance uses its existing separate overlay.

## Next roadmap items

Started recruiting-balance and long-term-health diagnostics: three program strengths, five annual cycles each. Includes annual portal and six-week recruiting, roster replenishment/depth, progression/regression, staff aging, scouting, portal attrition and annual SQLite round trips. Results and limitations are recorded in recruiting-balance-baseline.md; current roadmap is in ROADMAP.md.

This found a real double-signing path: user assistance could acquire a prospect already signed by a CPU team. Assistance now excludes unavailable IDs, interaction reads respect authoritative commitment records, manual actions honor existing commitments, and phase resolution ignores rostered/pending/already-committed prospects. Commitment lookup indexes are tied to immutable collection snapshots and can be garbage collected.

## Validation

164 tests pass, including 15 annual cycles and a targeted already-signed-prospect regression. Android compilation passes with existing deprecated-alert warnings. Device loading-screen behavior and full-league/multi-seed long-term balance still need verification. No save schema change or new signing quotas.
