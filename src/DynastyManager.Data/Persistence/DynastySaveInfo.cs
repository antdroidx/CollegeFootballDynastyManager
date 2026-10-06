using DynastyManager.Core.Models;

namespace DynastyManager.Data.Persistence;

public sealed record DynastySaveInfo(
    Guid SaveId,
    Guid DynastyId,
    string DynastyName,
    string UserTeamName,
    int SeasonYear,
    int Week,
    SeasonPhase Phase,
    SaveKind Kind,
    DateTime UpdatedUtc,
    int SchemaVersion);
