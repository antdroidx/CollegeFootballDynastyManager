using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class GameDayRecommendationTests
{
    private static readonly TeamGamePlan Plan = new() { TeamName = "Home", Offense = OffensivePlaybook.Balanced };
    private static bool Run(OffensivePlayDefinition play) => play.Concept is OffensiveConcept.InsideRun or OffensiveConcept.OutsideRun;

    [Fact]
    public void NormalDownsMixCallsInsteadOfAlwaysTakingTheFirstSortedRun()
    {
        var history = new List<string>();
        var plays = Enumerable.Range(0, 200).Select(snap =>
        {
            var play = GamePlanService.RecommendOffensivePlay(Plan, 1, 10, 35, snap, history.TakeLast(4).ToArray(), 4242);
            history.Add(play.Id); return play;
        }).ToArray();
        Assert.True(plays.Select(p => p.Id).Distinct().Count() >= 7);
        Assert.InRange(plays.Count(Run), 50, 150);
        Assert.True(plays.GroupBy(p => p.Id).Max(g => g.Count()) < 70);
    }

    [Fact]
    public void SituationAndCoachTendencyChangeTheMix()
    {
        var shortRuns = Enumerable.Range(0, 1000).Count(i => Run(GamePlanService.RecommendOffensivePlay(Plan, 3, 1, 40, i)));
        var longRuns = Enumerable.Range(0, 1000).Count(i => Run(GamePlanService.RecommendOffensivePlay(Plan, 3, 10, 40, i)));
        Assert.True(shortRuns > 600);
        Assert.True(longRuns < 100);
        var powerRuns = Enumerable.Range(0, 1000).Count(i => Run(GamePlanService.RecommendOffensivePlay(Plan with { Offense = OffensivePlaybook.PowerRun }, 1, 10, 40, i)));
        var airRuns = Enumerable.Range(0, 1000).Count(i => Run(GamePlanService.RecommendOffensivePlay(Plan with { Offense = OffensivePlaybook.AirRaid }, 1, 10, 40, i)));
        Assert.True(powerRuns > airRuns + 150);
    }

    [Fact]
    public void RecentCallPenaltyReducesRepetitionsWithoutBanningThem()
    {
        var repeated = GamePlanService.RecommendOffensivePlay(Plan, 1, 10, 40).Id;
        var baseline = 0; var penalized = 0;
        for (var i = 0; i < 1000; i++)
        {
            if (GamePlanService.RecommendOffensivePlay(Plan, 1, 10, 40, i).Id == repeated) baseline++;
            if (GamePlanService.RecommendOffensivePlay(Plan, 1, 10, 40, i, [repeated]).Id == repeated) penalized++;
        }
        Assert.True(baseline > 0);
        Assert.True(penalized < baseline / 2);
    }

    [Fact]
    public void RefreshIsStableAndDoesNotAdvanceTheSimulationRandomStream()
    {
        var session = Session(); var untouched = Session();
        var first = GameDayRecommendationService.Recommend(session, Plan);
        for (var i = 0; i < 10; i++) Assert.Equal(first, GameDayRecommendationService.Recommend(session, Plan));
        Assert.Equal(untouched.Random.Next(), session.Random.Next());
        Assert.Empty(session.Events);
    }

    [Fact]
    public void CustomBookRecommendationsStayInsideTheSelectedMembership()
    {
        var custom = Plan with { Offense = OffensivePlaybook.Custom, CustomOffensivePlayIds = ["off-spread-mesh"] };
        for (var i = 0; i < 20; i++)
            Assert.Equal("off-spread-mesh", GamePlanService.RecommendOffensivePlay(custom, 1, 10, 35, i).Id);
    }

    [Theory]
    [InlineData(30, 8, RecommendedCallType.Punt)]
    [InlineData(75, 8, RecommendedCallType.FieldGoal)]
    [InlineData(50, 1, RecommendedCallType.Scrimmage)]
    public void FourthDownAdviceIncludesKicksAndReasonableShortYardageAttempts(int yardLine, int distance, RecommendedCallType expected)
    {
        var session = Session(); session.Down = 4; session.YardLine = yardLine; session.Distance = distance;
        var advice = GameDayRecommendationService.Recommend(session, Plan);
        Assert.Equal(expected, advice.Type);
        Assert.NotEmpty(advice.Reason);
        if (expected == RecommendedCallType.FieldGoal)
        {
            Assert.Equal(42, advice.KickDistance);
            Assert.Equal(GameDayEngine.FieldGoalProbability(session), advice.KickProbability);
        }
        if (expected != RecommendedCallType.Scrimmage)
        {
            var snap = GameDayEngine.Step(session, advice.PlayId);
            Assert.True(expected == RecommendedCallType.Punt ? snap.Outcome == SnapOutcome.Punt
                : snap.Outcome is SnapOutcome.FieldGoal or SnapOutcome.MissedFieldGoal);
        }
    }

    [Fact]
    public void TrailingLateDoesNotRecommendPuntingOrAKickWhenATouchdownIsNeeded()
    {
        var session = Session(); session.Down = 4; session.Quarter = 4; session.ClockSeconds = 60;
        session.HomeScore = 10; session.AwayScore = 17;
        foreach (var yard in new[] { 25, 85 })
        {
            session.YardLine = yard;
            Assert.Equal(RecommendedCallType.Scrimmage, GameDayRecommendationService.Recommend(session, Plan).Type);
        }
        session.AwayScore = 12; session.YardLine = 75;
        Assert.Equal(RecommendedCallType.FieldGoal, GameDayRecommendationService.Recommend(session, Plan).Type);
    }

    [Fact]
    public void CanRecommendAnEarlyDownFieldGoalBeforeHalftimeExpires()
    {
        var session = Session(); session.Down = 2; session.Quarter = 2; session.ClockSeconds = 3; session.YardLine = 80;
        Assert.Equal(RecommendedCallType.FieldGoal, GameDayRecommendationService.Recommend(session, Plan).Type);
    }

    [Fact]
    public void KickerAttributesAffectRecommendedRange()
    {
        var strong = Session(95); var weak = Session(40);
        strong.Down = weak.Down = 4; strong.YardLine = weak.YardLine = 62;
        Assert.Equal(RecommendedCallType.FieldGoal, GameDayRecommendationService.Recommend(strong, Plan).Type);
        Assert.Equal(RecommendedCallType.Punt, GameDayRecommendationService.Recommend(weak, Plan).Type);
    }

    private static GameDaySession Session(int? kicking = null)
    {
        var home = new TeamSimulationProfile { TeamName = "Home", SpecialTeamsRating = 75, GamePlan = Plan };
        var away = home with { TeamName = "Away", GamePlan = Plan with { TeamName = "Away" } };
        var roster = kicking is int skill ? new[] { new DynastyPlayer { PlayerId = Guid.NewGuid(), FullName = "Kicker", TeamName = "Home",
            Position = Position.K, Skills = new PlayerSkillRatings { KickAccuracy = skill, KickPower = skill } } } : Array.Empty<DynastyPlayer>();
        var session = GameDayEngine.Start(new ScheduledGame { GameId = "advice", SeasonYear = 2026, Week = 1,
            HomeTeamName = "Home", AwayTeamName = "Away", SimulationSeed = 4242 }, home, away, roster, GameDayMode.Play);
        session.KickoffPending = false;
        return session;
    }
}
