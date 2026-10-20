namespace DynastyManager.Core.Models;

public sealed record LeagueRules
{
    public int GamesPerTeam { get; init; } = 12;
    public int PlayoffTeams { get; init; } = 12;
    public bool ConferenceChampionships { get; init; } = true;
    public bool AllowAutomaticConferenceFill { get; init; }
}

public sealed record StartingLeagueSetup
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public IReadOnlyList<Team> Teams { get; init; } = [];
    public LeagueRules Rules { get; init; } = new();
    public bool IsBuiltIn { get; init; }
}

public sealed record StartingRosterPack
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public IReadOnlyList<DynastyPlayer> Players { get; init; } = [];
}

public sealed record StartingLibrary
{
    public IReadOnlyList<StartingLeagueSetup> Setups { get; init; } = [];
    public IReadOnlyList<StartingRosterPack> Rosters { get; init; } = [];
    public IReadOnlyList<Guid> HiddenBuiltIns { get; init; } = [];
    public IReadOnlyDictionary<Guid, Guid> DefaultRosterBySetup { get; init; } = new Dictionary<Guid, Guid>();
}
