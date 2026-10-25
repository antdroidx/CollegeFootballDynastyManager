using DynastyManager.Core.Models;
namespace ProFootball.Core;
public sealed record ProTeam(string Id, string City, string Nickname, string Abbreviation, string Conference, string Division, int SeedRank, string Primary, string Secondary)
{ public string Name => City + " " + Nickname; }
public sealed record ProLeague(int Version, string Name, int ActiveRosterLimit, int PracticeSquadLimit, List<ProTeam> Teams);
public enum ProPhase { Preseason, RegularSeason, Playoffs, Offseason }
public sealed record ProPlayer(DynastyPlayer Football, int Age, int Experience, bool PracticeSquad);
public sealed record ProGame(ScheduledGame Football, ProPhase Phase, int Round = 0);
public sealed record SeasonArchive(int Year, string Champion, List<ProGame> Games);
public sealed record ProFranchise
{
    public int SchemaVersion { get; init; } = 1;
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = "My franchise";
    public required ProLeague League { get; init; }
    public required string UserTeam { get; init; }
    public int Year { get; init; } = 2026;
    public int Week { get; init; } = 1;
    public ProPhase Phase { get; init; } = ProPhase.Preseason;
    public List<ProPlayer> Players { get; init; } = [];
    public List<ProGame> Games { get; init; } = [];
    public List<SeasonArchive> History { get; init; } = [];
    public string? Champion { get; init; }
}
public sealed record ProStanding(ProTeam Team, int Wins, int Losses, int Ties, double Percentage, double DivisionPercentage, double ConferencePercentage, double OpponentPercentage, int Differential);
