# College Football Dynasty Manager — C# Rewrite

## Baseline

The original Java/Android game remains intact and is the behavioral reference for the rewrite.

Legacy baseline commit:

`9bf44cd7ec16cc5efabdef0fd8eae95d14f0914e`

The rewrite is being developed on `csharp-maui-foundation` before any merge to `main`.

## Platform direction

- C# and .NET 10 LTS
- .NET MAUI application layer
- Android first
- iOS target after the Android version matures
- Visual Studio-friendly solution
- GitHub Actions required for build/test validation
- Existing Android application ID retained: `antdroid.cfbcoach`

## Architecture

- `DynastyManager.Core`: platform-independent football simulation/domain code.
- `DynastyManager.Data`: CSV import/export and future SQLite dynasty persistence.
- `DynastyManager.App`: MAUI presentation and platform integration.
- `DynastyManager.Core.Tests`: deterministic model/simulation tests.

The core project must not depend on Android, iOS, MAUI, or UI concepts.

## Confirmed product decisions

- CSV remains a first-class editable import/export format for Excel workflows.
- Active dynasty saves will move to structured persistence rather than the legacy text snapshot.
- Defensive roster positions are DE, DT, OLB, MLB, CB, FS, and SS rather than generic DL/LB/S.
- On-field roles and secondary positions will allow scheme flexibility.
- Recruiting will use a shared national pool and scale with modeled team count.
- The transfer portal will scale with modeled team count, with a modern target range around 1,500–2,500 entrants for an FBS-sized universe.
- Not every recruit or transfer must sign with a modeled team.
- Game-day modes will eventually include Coach, Watch, and Simulate.
- Play calling and the live viewer will consume the same underlying play-by-play simulation events.

## First milestones

1. Establish C#/.NET MAUI build and CI.
2. Port clean core models.
3. Build validated CSV import/export.
4. Add dynasty persistence.
5. Port the existing simulation and season systems before changing their behavior.
6. Add redesigned recruiting, portal, schemes/playbooks, and live game presentation.
