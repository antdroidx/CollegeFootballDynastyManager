# College baseline CSV data

These unchanged files were retained from the original Java game and are now owned by the C# data project:

- `universe.csv`: baseline teams and league setup.
- `roster.csv`: legacy player data.
- `coach.csv`: legacy coaching data.

`DynastyManager.App.csproj` packages them as `legacy_universe.csv`, `legacy_roster.csv`, and `legacy_coach.csv`. The college test project links them into `TestData`; `tools/SimAudit` uses the universe file directly. Keep those consumers synchronized if these paths change. The separate historical root `Universe/` folder is not this runtime dataset.

The CSV contents were preserved byte for byte during relocation. Modern roster and coach packs are separate resources; these baseline files remain necessary for college startup and compatibility coverage.
