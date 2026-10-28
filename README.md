# College Football Dynasty Manager & Pro Football Franchise Manager

Two independently installable Android football management games, built with C# and .NET MAUI in one repository. They reuse the football engine and presentation components while keeping separate application identities and saves.

## Applications

- **College Football Dynasty Manager** — college recruiting, roster and coaching management, seasons, statistics, and live GameCast. The current college release line is pre-release **0.20**; APKs also display their build number. Its Android package remains `antdroid.cfbcoach`.
- **Pro Football Franchise Manager** — a development preview with 32 NFL teams, roster and coaching data, a professional season structure, franchise management foundations, and the shared GameCast engine. Its Android package is `antdroid.profootball.franchise`. Contracts, free agency, trades, scouting, draft, and CPU management are still being refined; this is not a completed feature release.

Pro management includes automatic staff/combine scouting, annual staff development, camp depth battles, NFL division/wild-card tiebreakers, development and draft-outcome comparisons, career records, weekly league reviews and late-season playoff projections. Analytics is split into focused category views. College-to-Pro draft-class export/import screens are deferred. See [current work and exact accepted APKs](docs/current-work.md) for verified scope; historical parity notes are not a current completion checklist.

Both applications use responsive layouts, light/dark themes, team colors, and separate save storage. Pro development is isolated on `feature/pro-franchise-foundation`; unfinished pro features are not merged into stable `main`.

## Repository layout

| Location | Purpose |
| --- | --- |
| `src/Football.Engine` | Shared simulation and football models; retains the `DynastyManager.Core` assembly identity for college compatibility |
| `src/Football.Presentation` | Shared field rendering, sprites, themes, and UI components |
| `src/DynastyManager.App` | College Android application |
| `src/DynastyManager.Data` | College data import, persistence, branding, and baseline CSV resources |
| `src/ProFootball.Core` | Pro league configuration, franchise rules, data, and persistence |
| `src/ProFootball.App` | Pro Android application |
| `tests` | College and pro regression suites |
| `tools/SimAudit` | Simulation audit utility |
| `build` | Current build, data import, and Android validation tools |
| `docs` | Architecture, data sources, validation evidence, and historical documentation |

The original Java Android project and Gradle files remain in the repository pending archival. They do not build the current C# applications. The historical `Universe/` collection is distinct from the current baseline data. See [original documentation and attribution](docs/legacy-college-football-coach.md).

## Baseline data

The college game's retained baseline files now live in [src/DynastyManager.Data/Resources/Legacy](src/DynastyManager.Data/Resources/Legacy/README.md): `universe.csv`, `roster.csv`, and `coach.csv`. The college application packages these under their existing logical names, preserving runtime loading. Tests and the simulation audit read the same source files. Modern roster packs remain alongside their respective applications and league projects.

## Build and validation

Use the .NET SDK pinned in `global.json` and install its compatible MAUI Android workload, Android SDK, and JDK. From the repository root:

```sh
python build/check-packaged-resources.py
dotnet test tests/ProFootball.Tests/ProFootball.Tests.csproj -c Release
dotnet test tests/DynastyManager.Core.Tests/DynastyManager.Core.Tests.csproj -c Release
dotnet build src/DynastyManager.App/DynastyManager.App.csproj -c Debug
dotnet build src/ProFootball.App/ProFootball.App.csproj -c Debug
```

The college suite includes long-running dynasty and calibration tests. CI workflows define the current toolchain and device validation commands.

## Android downloads and workflows

- [College CI and APK builds](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/workflows/csharp-ci.yml)
- [Pro Android CI and APK builds](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/workflows/pro-android.yml)
- [Published releases](https://github.com/antdroidx/CollegeFootballDynastyManager/releases)

Open a successful run and download its APK artifact. Each application has independent Android version codes and uses consistent signing for update installations; college and pro can coexist. Artifact downloads may require GitHub sign-in and expire according to retention settings.

## Compatibility and status

Preserving existing college behavior, package identity, and save compatibility is a priority. Shared engine changes require college and pro regression validation. Pro saves have their own versioned format and migrations. Data snapshot provenance and import tooling are documented under `docs`; imported real-world data is a dated snapshot, not a live roster feed.

See [LICENSE](LICENSE) for repository licensing and the historical documentation for the original game's acknowledgments.

## Pro parity and contract-data status

See [the current parity audit](docs/pro-parity-audit.md) for implemented screens and remaining college UI/GameCast gaps. New Pro franchises default to real-roster contracts, with reasonable game estimates for missing details and an optional generated-contract alternative. Starting2026 budgets include adjusted team caps, rollover and opening dead money. The cap panel shows the main numbers first, with an optional detailed breakdown. Existing saves preserve negotiated contracts and booked obligations. [Practical finance policy](docs/pro-practical-finance-policy.md) documents the modeling choices.

## Shared player ratings and GameCast

Both games use the same 19 player attributes, position-weighted overall calculation, and responsive attribute tiles. Pro imports supply attribute data; external overall ratings are not the in-game overall. Existing pro saves adopt the shared calculation once while retaining player IDs, contracts, and historical results. College retains its potential-star scale. Pro separates current ability, an age/position-aware projected ceiling, and remaining-growth stars; annual rating deltas preserve the last offseason change. Athletic decline begins before technical and mental decline, with different prime ages by position.

The college GameCast animation, playbook cards/diagrams, camera framing, broadcast scoreboard layout, and detailed live box-score page live in `Football.Presentation` and are called by both apps. Professional rules and aging remain league-specific. See the [parity audit](docs/pro-parity-audit.md) for remaining work.
