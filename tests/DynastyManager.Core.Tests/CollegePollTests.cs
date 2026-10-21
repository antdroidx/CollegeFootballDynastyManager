using System.Text.Json;
using DynastyManager.Core.Models;
using DynastyManager.Core.Seasons;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public sealed class CollegePollTests
{
    private static Team School(string name, int prestige = 70, string conference = "League") => new()
        { Name = name, Abbreviation = name, ConferenceName = conference, Prestige = prestige };
    private static DynastyState State(params Team[] teams) => new()
    {
        DynastyId = Guid.Parse("57b858f6-a08c-4a81-9a69-3432aaa61d5a"), DynastyName = "Polls", UserTeamName = teams[0].Name,
        SeasonYear = 2026, Week = 0, Phase = SeasonPhase.Preseason, LeagueTeams = teams
    };
    private static ScheduledGame Result(string winner, string loser, int week) => new()
    {
        GameId = $"{week}-{winner}-{loser}", SeasonYear = 2026, Week = week, HomeTeamName = winner, AwayTeamName = loser,
        HasPlayed = true, HomeScore = 31, AwayScore = 14
    };
    private static TeamSimulationProfile Profile(string name, double rating) => new()
    {
        TeamName = name, PassOffenseRating = rating, RushOffenseRating = rating,
        PassDefenseRating = rating, RushDefenseRating = rating
    };

    [Theory]
    [InlineData(PollKind.Media)]
    [InlineData(PollKind.Coaches)]
    [InlineData(PollKind.Computer)]
    public void IndividualGamesAndReloadDoNotChangePublishedPollButWeekAdvanceDoes(PollKind kind)
    {
        var state = State(School("A"), School("B")); var teams = state.LeagueTeams.ToDictionary(t => t.Name);
        state = SeasonProgression.Advance(CollegePollService.Record(state, teams));
        var entering = CollegePollService.GetPublished(state, teams, kind);
        state = state with { Schedule = [Result("B", "A", state.Week)] };
        Assert.Equal(entering, CollegePollService.GetPublished(state, teams, kind));
        var loaded = JsonSerializer.Deserialize<DynastyState>(JsonSerializer.Serialize(state))!;
        Assert.Equal(entering, CollegePollService.GetPublished(loaded, teams, kind));
        state = SeasonProgression.Advance(CollegePollService.Record(loaded, teams));
        var released = CollegePollService.GetPublished(state, teams, kind);
        Assert.Equal("B", released[0].TeamName);
        Assert.Equal(1, released[0].Wins);
        Assert.Equal(1, released[1].Losses);
        // Even fresh profiles and another current-week game cannot move the published poll.
        state = state with { Schedule = state.Schedule.Append(Result("A", "B", state.Week)).ToArray() };
        Assert.Equal(released, CollegePollService.GetPublished(state, teams, kind,
            new Dictionary<string, TeamSimulationProfile> { ["A"] = Profile("A", 99), ["B"] = Profile("B", 20) }));
    }

    [Fact]
    public void MissingSnapshotsExcludeCurrentWeekResultsAndAreAnchoredOnce()
    {
        var state = State(School("A"), School("B")) with
            { Week = 2, Phase = SeasonPhase.RegularSeason, Schedule = [Result("A", "B", 1), Result("B", "A", 2)] };
        var teams = state.LeagueTeams.ToDictionary(t => t.Name);
        state = CollegePollService.EnsurePublished(state, teams);
        Assert.Equal(3, state.PollHistory.Count);
        Assert.All(state.PollHistory, p => { Assert.Equal(1, p.Week); Assert.Equal(1, p.Rankings.Single(r => r.TeamName == "A").Wins); });
        Assert.Same(state, CollegePollService.EnsurePublished(state, teams));
    }

    [Fact]
    public void NewSeasonDoesNotReuseLastSeasonsPollAndWeekZeroPublishesSeparately()
    {
        var state = State(School("A"), School("B")); var teams = state.LeagueTeams.ToDictionary(t => t.Name);
        state = CollegePollService.Record(state, teams);
        state = state with { SeasonYear = 2027 };
        state = CollegePollService.Record(state, teams);
        Assert.Equal(6, state.PollHistory.Count);
        Assert.All(CollegePollService.GetPublished(state, teams), r => Assert.Equal(0, r.Wins));
        state = state with { Phase = SeasonPhase.RegularSeason, Schedule = [Result("B", "A", 0) with { SeasonYear = 2027 }] };
        state = CollegePollService.Record(state, teams);
        Assert.Equal(9, state.PollHistory.Count);
        Assert.Equal(1, CollegePollService.GetPublished(state, teams).Single(r => r.TeamName == "B").Wins);
    }

    [Theory]
    [InlineData(PollKind.Media)]
    [InlineData(PollKind.Coaches)]
    public void EarlyWinOverFcsDoesNotLeapfrogStrongTeamOnBye(PollKind kind)
    {
        var state = State(School("Favorite", 95, "Power"), School("Underdog", 40, "Small"));
        var teams = state.LeagueTeams.ToDictionary(t => t.Name);
        state = CollegePollService.Record(state, teams) with
            { Week = 1, Phase = SeasonPhase.RegularSeason, Schedule = [Result("Underdog", "Montana", 1) with { IsFcsGame = true }] };
        Assert.Equal("Favorite", CollegePollService.Build(state, teams, kind)[0].TeamName);
        Assert.Equal("Underdog", CollegePollService.Build(state, teams, PollKind.Computer)[0].TeamName);
    }

    [Theory]
    [InlineData(PollKind.Media)]
    [InlineData(PollKind.Coaches)]
    public void SeasonResultsEventuallyOvercomePreseasonReputation(PollKind kind)
    {
        var state = State(School("Favorite", 95, "Power"), School("Underdog", 40, "Small"));
        var teams = state.LeagueTeams.ToDictionary(t => t.Name);
        state = CollegePollService.Record(state, teams) with { Week = 10, Phase = SeasonPhase.RegularSeason,
            Schedule = Enumerable.Range(1, 9).Select(w => Result("Underdog", "Favorite", w)).ToArray() };
        Assert.Equal("Underdog", CollegePollService.Build(state, teams, kind)[0].TeamName);
    }

    [Fact]
    public void HumanPreseasonPollReflectsRosterConferenceAndRecentHistory()
    {
        var state = State(School("Strong", 70, "Power"), School("Weak", 70, "Small"),
            School("PowerPeer", 99, "Power"), School("SmallPeer", 35, "Small"));
        var teams = state.LeagueTeams.ToDictionary(t => t.Name);
        var profiles = new Dictionary<string, TeamSimulationProfile> { ["Strong"] = Profile("Strong", 95), ["Weak"] = Profile("Weak", 60) };
        Assert.True(CollegePollService.Build(state, teams, profiles: profiles).Single(r => r.TeamName == "Strong").Rank <
            CollegePollService.Build(state, teams, profiles: profiles).Single(r => r.TeamName == "Weak").Rank);
        // Equal programs and rosters: actual prior seasons distinguish them.
        state = State(School("Champion"), School("Struggler")) with { TeamSeasonHistory = [
            new() { TeamName = "Champion", SeasonYear = 2025, Wins = 14, Losses = 1, FinalRanking = 1 },
            new() { TeamName = "Struggler", SeasonYear = 2025, Wins = 2, Losses = 10, FinalRanking = 90 }] };
        teams = state.LeagueTeams.ToDictionary(t => t.Name);
        Assert.Equal("Champion", CollegePollService.Build(state, teams)[0].TeamName);
        Assert.All(CollegePollService.Build(state, teams, PollKind.Computer), r => Assert.Equal(0, r.Score));
    }
    [Fact]
    public void EqualProgramsReceiveDifferentPreseasonExpectationsFromConferenceReputation()
    {
        var state = State(School("PowerMember", 70, "Power"), School("SmallMember", 70, "Small"),
            School("PowerPeer", 99, "Power"), School("SmallPeer", 35, "Small"));
        var ranks = CollegePollService.Build(state, state.LeagueTeams.ToDictionary(t => t.Name));
        Assert.True(ranks.Single(r => r.TeamName == "PowerMember").Rank < ranks.Single(r => r.TeamName == "SmallMember").Rank);
    }

    [Fact]
    public void EqualBrandsReceiveDifferentPreseasonExpectationsFromRosterTalent()
    {
        var state = State(School("Talented"), School("Untalented"));
        var profiles = new Dictionary<string, TeamSimulationProfile>
            { ["Talented"] = Profile("Talented", 95), ["Untalented"] = Profile("Untalented", 55) };
        Assert.Equal("Talented", CollegePollService.Build(state, state.LeagueTeams.ToDictionary(t => t.Name), profiles: profiles)[0].TeamName);
    }

    [Fact]
    public void EarlyQualityWinCountsMoreThanWinOverWeakOpponent()
    {
        var state = State(School("QualityWin"), School("EasyWin"), School("Power", 99), School("Weak", 35)) with
            { Week = 1, Phase = SeasonPhase.RegularSeason, Schedule = [Result("QualityWin", "Power", 1), Result("EasyWin", "Weak", 1)] };
        var ranks = CollegePollService.Build(state, state.LeagueTeams.ToDictionary(t => t.Name));
        Assert.True(ranks.Single(r => r.TeamName == "QualityWin").Rank < ranks.Single(r => r.TeamName == "EasyWin").Rank);
    }

}
