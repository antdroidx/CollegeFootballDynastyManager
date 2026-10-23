using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class GameDayMatchupTests
{
    [Fact]
    public void ShortKicksAreLikelyButNotGuaranteedAndRatingsMatter()
    {
        var shortKick = GameDayMatchupService.FieldGoalProbability(25, 75, 75);
        Assert.InRange(shortKick, .95, .99);
        Assert.True(shortKick > GameDayMatchupService.FieldGoalProbability(50, 75, 75));
        Assert.True(GameDayMatchupService.FieldGoalProbability(40, 95, 75) > GameDayMatchupService.FieldGoalProbability(40, 55, 75));
        Assert.True(GameDayMatchupService.FieldGoalProbability(60, 75, 95) > GameDayMatchupService.FieldGoalProbability(60, 75, 55));
        Assert.InRange(GameDayMatchupService.FieldGoalProbability(110, 99, 99), .01, .05);
    }

    [Fact]
    public void DefensivePackagesTradeRunSupportForCoverage()
    {
        var run = PlaybookCatalog.OffensivePlays.First(p => p.Concept == OffensiveConcept.InsideRun);
        var pass = PlaybookCatalog.OffensivePlays.First(p => p.Concept == OffensiveConcept.DeepPass);
        var dime = PlaybookCatalog.DefensivePlays.First(p => p.Personnel == DefensivePersonnel.Dime);
        var heavy = PlaybookCatalog.DefensivePlays.First(p => p.Personnel == DefensivePersonnel.GoalLine);
        Assert.True(GameDayMatchupService.FormationAdvantage(run, dime, false) > GameDayMatchupService.FormationAdvantage(run, heavy, false));
        Assert.True(GameDayMatchupService.FormationAdvantage(pass, dime, true) < GameDayMatchupService.FormationAdvantage(pass, heavy, true));
    }

    [Fact]
    public void ParticipatingPlayerSkillsChangeTheMatchupEvenWithEqualTeamProfiles()
    {
        DynastyPlayer Player(Position position, int rating) => new() { PlayerId = Guid.NewGuid(), FullName = "Test", TeamName = "H", Position = position,
            OverallRating = 75, SpeedRating = rating, AwarenessRating = rating, StrengthRating = rating, AgilityRating = rating,
            Skills = new PlayerSkillRatings { PassingAccuracy = rating, PassingPower = rating, Catching = rating, PassBlocking = rating,
                RunBlocking = rating, BreakTackle = rating, BallCarrying = rating, Tackling = rating, Jumping = rating } };
        var defense = new[] { Player(Position.CB, 75), Player(Position.DT, 75), Player(Position.MLB, 75) };
        var strong = new[] { Player(Position.QB, 90), Player(Position.WR, 90), Player(Position.OL, 90), Player(Position.RB, 90) };
        var weak = new[] { Player(Position.QB, 55), Player(Position.WR, 55), Player(Position.OL, 55), Player(Position.RB, 55) };
        foreach (var pass in new[] { true, false })
            Assert.True(GameDayMatchupService.PersonnelAdvantage(strong, defense, pass) > GameDayMatchupService.PersonnelAdvantage(weak, defense, pass));
    }
}
