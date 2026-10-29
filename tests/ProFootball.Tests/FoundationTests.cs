using Xunit;
using ProFootball.Core;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
namespace ProFootball.Tests;

public class FoundationTests
{
    static ProFranchise New() => ProLeagueService.Create(ProLeagueService.Load().Teams[0].Name);
    [Theory]
    [InlineData(2026)]
    [InlineData(2027)]
    [InlineData(2028)]
    [InlineData(2029)]
    public void ScheduleHasCorrectOpponentsGamesHomeSplitsAndByes(int year)
    {
        var league = ProLeagueService.Load(); var games = ProSchedule.Build(league, year).Where(g => g.Phase == ProPhase.RegularSeason).ToArray(); Assert.Equal(272, games.Length);
        foreach (var team in league.Teams)
        {
            var own = games.Where(g => g.Football.InvolvesTeam(team.Name)).ToArray(); Assert.Equal(17, own.Length); Assert.Equal(17, own.Select(g => g.Football.Week).Distinct().Count()); Assert.InRange(own.Count(g => g.Football.HomeTeamName == team.Name), 8, 9);
            foreach (var rival in league.Teams.Where(t => t.Id != team.Id && t.Conference == team.Conference && t.Division == team.Division))
            { var pair = own.Where(g => g.Football.InvolvesTeam(rival.Name)).ToArray(); Assert.Equal(2, pair.Length); Assert.Single(pair.Where(g => g.Football.HomeTeamName == team.Name)); }
        }
    }
    [Fact]
    public void RosterLimitsAndIdentitiesAreIndependent()
    { var s = New(); Assert.Equal(32, s.League.Teams.Count); Assert.Equal(2593, s.Players.Count); Assert.Equal(128,s.Coaches.Count); Assert.Equal(2255,s.Players.Count(p=>p.RatingSource.StartsWith("Madden"))); Assert.Equal(s.Players.Count, s.Players.Select(p => p.Football.PlayerId).Distinct().Count()); foreach (var t in s.League.Teams) { Assert.InRange(ProRoster.Active(s,t.Name),45,ProRoster.Limit(s)); Assert.InRange(s.Players.Count(p => p.Football.TeamName == t.Name && p.PracticeSquad),0,16); } }
    [Fact]
    public async Task MultipleSlotsAndCorruptPrimaryRecoverFromRollingBackup()
    { var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()); try { var store = new ProSaveStore(dir); var s = New(); await store.Save(s); await store.Save(s with { Week = 2 }); var other = New(); await store.Save(other); Assert.Equal(2, store.Slots().Count()); Assert.Equal(2, (await store.Load(s.Id)).Week); await File.WriteAllTextAsync(Path.Combine(dir, s.Id + ".json"), "broken"); Assert.Equal(1, (await store.Load(s.Id)).Week); } finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); } }
    [Fact]
    public void ProAndCollegeSessionsUseSameEngineWithDifferentRules()
    { var s = New(); var g = s.Games[0]; var pro = ProLeagueService.Start(s, g, GameDayMode.Play); Assert.True(pro.Rules.TimedOvertime); var college = GameDayEngine.Start(g.Football, pro.HomeProfile, pro.AwayProfile, pro.Roster, GameDayMode.Play); Assert.False(college.Rules.TimedOvertime); Assert.Equal(20, college.Rules.ExtraPointDistance); Assert.Equal(33, pro.Rules.ExtraPointDistance); }
    [Fact]
    public void CollegeDefaultRemainsDeterministicAfterExtraction()
    { var s = New(); var g = s.Games[0]; var a = ProLeagueService.Start(s, g, GameDayMode.Simulate); var x = GameDayEngine.Start(g.Football, a.HomeProfile, a.AwayProfile, a.Roster, GameDayMode.Simulate); var y = GameDayEngine.Start(g.Football, a.HomeProfile, a.AwayProfile, a.Roster, GameDayMode.Simulate, rules: FootballRules.College); GameDayEngine.SimulateToEnd(x); GameDayEngine.SimulateToEnd(y); Assert.Equal(x.HomeScore, y.HomeScore); Assert.Equal(x.AwayScore, y.AwayScore); Assert.Equal(x.Events.Count, y.Events.Count); }
    [Fact]
    public void ProOvertimeEndsAndPlayoffsCannotTie()
    { var s = New(); for (var seed = 0; seed < 20; seed++) { var game = s.Games[0] with { Football = s.Games[0].Football with { SimulationSeed = seed } }; var session = ProLeagueService.Start(s, game, GameDayMode.Simulate); session.Quarter = 4; session.ClockSeconds = 1; session.KickoffPending = false; session.HomeScore = session.AwayScore = 7; var steps = 0; while (!session.IsComplete && steps++ < 600) GameDayEngine.Step(session); Assert.True(session.IsComplete, $"seed {seed} stalled"); } }
    [Fact]
    public void CompleteSeasonProducesSeededPlayoffsAndChampion()
    { var s = New(); for (var week = 0; week < 25 && s.Phase != ProPhase.Offseason; week++) s = ProLeagueService.Advance(s); Assert.Equal(ProPhase.Offseason, s.Phase); Assert.NotNull(s.Champion); Assert.Single(s.History); var postseason = s.Games.Where(g => g.Phase == ProPhase.Playoffs).ToArray(); Assert.Equal(13, postseason.Length); Assert.All(postseason, g => Assert.NotEqual(g.Football.HomeScore, g.Football.AwayScore)); Assert.All(s.League.Teams, t => Assert.InRange(ProRoster.Active(s,t.Name),0,53)); }
    [Fact]
    public void ImportTranslatesRatingsAndRejectsFutureVersions()
    { var p = New().Players[0].Football; var file = new PlayerExport(1, "College Football Dynasty Manager", 2026, [p]); var imported = ProPlayerImport.Convert(file, 1); Assert.NotEqual(p.OverallRating, imported[0].Football.OverallRating); Assert.Throws<InvalidDataException>(() => ProPlayerImport.Convert(file with { FormatVersion = 2 }, 1)); }
    [Fact]
    public void ProKickoffTouchbacksUseConfiguredSpotAndOvertimeOffersAReply()
    {
        var s = New(); var found = false;
        for (var seed = 0; seed < 30; seed++)
        {
            var game = s.Games[0] with { Football = s.Games[0].Football with { SimulationSeed = seed } };
            var session = ProLeagueService.Start(s, game, GameDayMode.Play); var kickoff = GameDayEngine.Step(session);
            if (kickoff.IsTouchback) { Assert.Equal(35, session.YardLine); found = true; }
            session.KickoffPending = false; session.OvertimeRound = 1; session.Quarter = 5; session.ClockSeconds = 600;
            session.PossessionTeamName = session.Game.HomeTeamName; session.HomeScore = 7; session.AwayScore = 0;
            session.OvertimePossessions = 0; session.TryPending = true;
            GameDayEngine.Step(session, "special-extra-point");
            Assert.False(session.IsComplete); Assert.Equal(1, session.OvertimePossessions);
            Assert.Equal(session.Game.AwayTeamName, session.PossessionTeamName);
        }
        Assert.True(found);
    }
    [Fact]
    public void NonUserGamesUseAValidControlledTeamAndRejectForeignTeams()
    {
        var state = New();
        var game = state.Games.First(g => !g.Football.InvolvesTeam(state.UserTeam));
        var watch = ProLeagueService.Start(state, game, GameDayMode.Watch);
        Assert.Equal(game.Football.HomeTeamName, watch.ControlledTeamName);
        Assert.Throws<ArgumentException>(() => ProLeagueService.Start(state, game, GameDayMode.Play, state.UserTeam));
    }
}
