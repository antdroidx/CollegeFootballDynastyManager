# Pro data and save contracts

## Application isolation
Pro package: `antdroid.profootball.franchise` (Debug and future release). College package IDs are unchanged. Android private app directories isolate preferences and save files even though both apps share the theme preference key. The pro app does not open the college SQLite database.

Pro saves: `<AppDataDirectory>/pro-franchises-v1/<GUID>.json`, schemaVersion 1. Each save writes a temporary file, rotates two prior versions and atomically replaces the primary. Loading tries primary, auto1, auto2; unknown future schemas fail explicitly. No schema migrations are needed before the first shipped pro format; future changes must add explicit migration handlers and fixtures rather than silently resetting saves.

## League pack v1
`src/ProFootball.Core/league.json` is the initial fictional league. Stable team IDs are independent of displayed city/name. Conference, division, prior ranking, abbreviation, primary/secondary colors, active/practice limits are data. Validate the full 32-team topology before creating a franchise. Future pack importing will accept the same versioned structure with roster and local logo asset manifests. Do not hardcode official teams in simulation, or fetch logos remotely. Pack logos must be app-private assets, with archive size/path validation when the UI importer is introduced. This APK intentionally uses fictional identities only; it has no real-world data downloader.

## Player exchange v1
`DynastyManager.Core.Models.PlayerExport` is owned by Football.Engine and available to both games. Fields: FormatVersion, Source, Year, Players. Existing DynastyPlayer skill/appearance/identity data is retained in this envelope. ProPlayerImport rejects unknown versions and duplicate identities, converts attributes to a professional scale and applies seeded scouting uncertainty. This is a foundation conversion, not a calibrated draft-value model. College draft export UI and pro draft-class import UI are Phase 3. No college save needs to be modified to use this format.

## Current simulation scope
Roster/timetable/save and full-season postseason integration are implemented. Foundational tiebreaks are explicitly labeled in the UI; the complete official common-game/multi-team sequence is deferred. All teams currently share the same valid roster shape and simulation rules. Salary cap, transactions, aging and multi-decade economic/statistical calibration are not implemented and cannot yet be validated. The first preview ends at the championship instead of pretending the unfinished offseason systems exist.
