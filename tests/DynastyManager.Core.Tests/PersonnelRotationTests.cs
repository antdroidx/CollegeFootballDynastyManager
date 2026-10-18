using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class PersonnelRotationTests
{
    [Fact]
    public void InjuredPlayersNeverEnterAndPackageControlsPersonnel()
    {
        var roster = Offense();
        var injured = roster.Single(player => player.Position == Position.RB && player.DepthChartOrder == 1);
        roster = roster.Select(player => player.PlayerId == injured.PlayerId
            ? player with { CurrentInjury = new PlayerInjury { WeeksRemaining = 2 } } : player).ToArray();

        var personnel = PersonnelRotationService.SelectOffense(roster, "Test", OffensivePersonnel.Twelve,
            new GameFatigueState(), new Random(4));

        Assert.DoesNotContain(injured.PlayerId, personnel.PlayerIds);
        Assert.Equal(2, personnel.PlayerIds.Count(id => roster.Single(player => player.PlayerId == id).Position == Position.TE));
        Assert.Equal(5, personnel.PlayerIds.Count(id => roster.Single(player => player.PlayerId == id).Position == Position.OL));
    }

    [Fact]
    public void ExhaustionRotatesBacksButHealthyOffensiveLineStaysTogether()
    {
        var roster = Offense();
        var fatigue = new GameFatigueState();
        var rb1 = roster.Single(player => player.Position == Position.RB && player.DepthChartOrder == 1);
        var ol1 = roster.Single(player => player.Position == Position.OL && player.DepthChartOrder == 1);
        fatigue.Set(rb1.PlayerId, 10);
        fatigue.Set(ol1.PlayerId, 90);

        var personnel = PersonnelRotationService.SelectOffense(roster, "Test", OffensivePersonnel.Eleven, fatigue, new Random(2));

        Assert.DoesNotContain(rb1.PlayerId, personnel.PlayerIds);
        Assert.Contains(ol1.PlayerId, personnel.PlayerIds);
    }

    private static DynastyPlayer[] Offense()
    {
        var players = new List<DynastyPlayer>();
        Add(Position.QB, 2); Add(Position.RB, 4); Add(Position.WR, 7); Add(Position.TE, 4); Add(Position.OL, 10);
        return players.ToArray();
        void Add(Position position, int count)
        {
            for (var i = 1; i <= count; i++) players.Add(new DynastyPlayer
            {
                PlayerId = Guid.NewGuid(), FullName = $"{position} {i}", TeamName = "Test", Position = position,
                ClassYear = 2, TalentLevel = 70, OverallRating = 90 - i, PotentialRating = 90,
                SpeedRating = 70, StrengthRating = 70, AgilityRating = 70, AwarenessRating = 70,
                TechniqueRating = 70, DurabilityRating = 80, DepthChartOrder = i
            });
        }
    }
}
