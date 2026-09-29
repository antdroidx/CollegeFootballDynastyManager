using DynastyManager.Core.Models;

namespace DynastyManager.Data.Import;

public sealed record LegacyUniverseImportResult(
    IReadOnlyList<Conference> Conferences,
    IReadOnlyList<Team> Teams);
