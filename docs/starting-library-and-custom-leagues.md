# Starting rosters and league setups

The main save-library screen now has **Starting rosters & league setups** and **Import starting roster**. Imports are reusable starting data, separate from `dynasties.db3`, in `starting-library.json`. Existing careers retain their team/rule/player snapshots when library entries are edited or removed.

## Workflow

1. Keep a built-in Modern FBS or Legacy FBS setup, copy/edit one, or add a new setup.
2. Add/remove schools; edit their conference/division, prestige, region and optional conference-game target. Original-game universe CSVs can populate the school list. Their team rows are `Name,Abbreviation,Conference,Prestige,Division,Region[,ConferenceGames]` inside `[START_TEAMS]` / `[END_TEAMS]`.
3. Choose 1–12 regular-season games per school, 2/4/8/12 playoff teams, and whether qualifying conferences hold championship games. Validate the actual schedule before saving a setup.
4. Import a NCAA PLAY CSV from the main screen, give it a name, preview the results, and optionally make it the default roster for a setup. Team names are resolved against schools in the available setups; numeric TGIDs still require mappings.
5. New dynasty selects **League structure**, **Starting roster**, then **Your school**. Imported players get new career identities before depth charts and staff are initialized. A pack replaces the entire starting roster of each matching school; schools missing from the pack use default/generated players. Pack schools outside the selected league are ignored, so full-universe rosters can be reused by smaller subsets.
6. Remove packs or setups in the starting library. Removing a default pack reverts that setup to generated/default players. Built-in options can be hidden and restored; packaged data is never overwritten.

## Dynamic configuration: supported and remaining

Team count is collection-based, rather than fixed to 130 or 138. Regression schedules cover 2, 3, 4, 8, 13, 16, 24, 64 and 160 schools, including unique opponents and no double-booked weeks. The UI validates distinct school names, opponent availability, parity, playoff size and schedule feasibility. These tests are not a claim of unlimited size or that every combination is schedulable.

The current season calendar remains 13 regular-season weeks, with the existing conference championship and postseason weeks. Supported playoffs are 2, 4, 8 and 12, with smaller brackets starting in their appropriate round and the final still in Week 18. The field/tree UI follows the selected size. Played finals are recorded without re-simulating them. Conference championship games still require 8+ members; bowl eligibility still uses six wins and the existing bowl catalog. Automatic conference-bid policy remains based on the existing FBS model.

Automatic custom scheduling can fill unused opponents within a conference when targets are unspecified. Explicit conference-game counts remain constraints; infeasible combinations are rejected. The built-in FBS scheduling path retains its previous behavior. Independents do not acquire conference games merely by sharing the Independent label.

Still pending: arbitrary season lengths, unrestricted playoff formats/byes/automatic bids, fully configurable conference-title/bowl rules, custom school branding and large-league performance characterization. NIL and budgets remain separate later work.

## Validation

Core tests cover variable-size scheduling, complete 2/4/8/12 playoff progression, manual-final recording, roster reuse across league subsets, separate persistence/defaults, library deletion, fresh imported player identities and independence of already-created careers. Device QA additionally checks the main-screen import/setup/new-dynasty path and save reload.
