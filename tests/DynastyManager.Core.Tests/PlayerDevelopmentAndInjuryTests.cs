using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class PlayerDevelopmentAndInjuryTests
{
    [Fact]
    public void HighPotentialFreshmanDevelopsInOffseason()
    {
        var player = RatedPlayer(
            "Young QB",
            Position.QB,
            overall: 70,
            potential: 95,
            classYear: 1);

        var state = State(
            SeasonPhase.Offseason,
            new[] { player });

        state = RosterManagementService.NormalizeAllDepthCharts(state);

        var updated =
            PlayerDevelopmentService.ApplyOffseasonDevelopment(state);

        var developed = Assert.Single(updated.ActiveRoster);

        Assert.True(developed.OverallRating > player.OverallRating);
        Assert.True(developed.OverallRating <= developed.PotentialRating + 1);

        var record = Assert.Single(
            updated.PlayerDevelopmentHistory);

        Assert.Equal(player.OverallRating, record.BeforeOverall);
        Assert.Equal(developed.OverallRating, record.AfterOverall);
        Assert.True(record.OverallChange > 0);
    }

    [Fact]
    public void MidseasonDevelopmentRunsOnceAndRecordsProgressionOrRegression()
    {
        var players = new[]
        {
            RatedPlayer("Young QB", Position.QB, 70, 95, 1),
            RatedPlayer("Senior QB", Position.QB, 88, 84, 4)
        };
        var state = RosterManagementService.NormalizeAllDepthCharts(
            State(SeasonPhase.RegularSeason, players) with { Week = 6 });

        var updated = PlayerDevelopmentService.ApplyMidseasonDevelopment(state);
        var repeated = PlayerDevelopmentService.ApplyMidseasonDevelopment(updated);

        Assert.Equal(2, updated.PlayerDevelopmentHistory.Count(record =>
            record.Stage == PlayerDevelopmentStage.Midseason));
        Assert.Equal(updated.PlayerDevelopmentHistory.Count,
            repeated.PlayerDevelopmentHistory.Count);
        Assert.Contains(updated.PlayerDevelopmentHistory,
            record => record.OverallChange != 0);
    }

    [Fact]
    public void LatestDevelopmentRecordsPreferOffseasonAndContainOnePerPlayer()
    {
        var playerId = Guid.NewGuid();
        var history = new[]
        {
            DevelopmentRecord(playerId, PlayerDevelopmentStage.Midseason, 70, 72),
            DevelopmentRecord(playerId, PlayerDevelopmentStage.Offseason, 72, 75)
        };

        var latest = PlayerDevelopmentService.GetLatestRecords(history, "Test");

        var record = Assert.Single(latest);
        Assert.Equal(PlayerDevelopmentStage.Offseason, record.Stage);
        Assert.Equal(75, record.AfterOverall);
        Assert.Single(latest.Select(item => item.PlayerId).Distinct());
    }

    [Fact]
    public void SevereKneeInjuryCausesPermanentRelatedRegression()
    {
        var player = RatedPlayer(
            "Star RB",
            Position.RB,
            overall: 90,
            potential: 96,
            classYear: 2);

        var state = State(
            SeasonPhase.RegularSeason,
            new[] { player }) with
        {
            Week = 4
        };

        var injured = InjuryService.ApplyInjury(
            state,
            player.PlayerId,
            InjuryBodyArea.Knee,
            InjurySeverity.Severe,
            12);

        var result = Assert.Single(injured.ActiveRoster);

        Assert.NotNull(result.CurrentInjury);
        Assert.True(result.SpeedRating < player.SpeedRating);
        Assert.True(result.AgilityRating < player.AgilityRating);
        Assert.True(result.DurabilityRating < player.DurabilityRating);
        Assert.True(result.PotentialRating < player.PotentialRating);
        Assert.True(result.OverallRating < player.OverallRating);
        Assert.True(result.IsRedshirted);
        Assert.True(result.CurrentInjury!.IsMedicalRedshirt);

        var history = Assert.Single(injured.InjuryHistory);
        Assert.Equal(InjurySeverity.Severe, history.Severity);
        Assert.True(history.SpeedLoss > 0);
        Assert.True(history.AgilityLoss > 0);
        Assert.True(history.PotentialLoss > 0);
    }

    [Fact]
    public void InjuryRecoveryCountsDownAndClears()
    {
        var player = RatedPlayer(
            "Injured WR",
            Position.WR,
            overall: 82,
            potential: 90,
            classYear: 2) with
        {
            CurrentInjury = new PlayerInjury
            {
                SeasonYear = 2026,
                StartWeek = 3,
                BodyArea = InjuryBodyArea.Ankle,
                Severity = InjurySeverity.Minor,
                InitialWeeks = 1,
                WeeksRemaining = 1
            }
        };

        var state = State(
            SeasonPhase.RegularSeason,
            new[] { player }) with
        {
            Week = 4,
            Schedule = Array.Empty<ScheduledGame>()
        };

        var updated =
            InjuryService.AdvanceAndGenerateForCurrentWeek(state);

        Assert.Null(
            Assert.Single(updated.ActiveRoster).CurrentInjury);
    }

    [Fact]
    public void InjuredStarterLowersCurrentTeamSimulationProfile()
    {
        var starter = RatedPlayer(
            "Starter QB",
            Position.QB,
            overall: 94,
            potential: 96,
            classYear: 3) with
        {
            DepthChartOrder = 1
        };

        var backup = RatedPlayer(
            "Backup QB",
            Position.QB,
            overall: 68,
            potential: 82,
            classYear: 2) with
        {
            DepthChartOrder = 2
        };

        var supporting = Enumerable.Range(1, 10)
            .Select(index => RatedPlayer(
                $"WR {index}",
                Position.WR,
                80,
                86,
                2) with
            {
                DepthChartOrder = index
            })
            .ToArray();

        var team = new Team
        {
            Name = "Test",
            Abbreviation = "TST",
            ConferenceName = "Test",
            Prestige = 80
        };

        var healthyState = State(
            SeasonPhase.RegularSeason,
            new[] { starter, backup }
                .Concat(supporting)
                .ToArray());

        var healthy = DynastyRosterSimulationProfileBuilder
            .Build(healthyState, new[] { team })[team.Name];

        var injuredState = healthyState with
        {
            ActiveRoster = healthyState.ActiveRoster
                .Select(player =>
                    player.PlayerId == starter.PlayerId
                        ? player with
                        {
                            CurrentInjury = new PlayerInjury
                            {
                                SeasonYear = 2026,
                                StartWeek = 2,
                                BodyArea = InjuryBodyArea.Shoulder,
                                Severity = InjurySeverity.Major,
                                InitialWeeks = 6,
                                WeeksRemaining = 6
                            }
                        }
                        : player)
                .ToArray()
        };

        var injured = DynastyRosterSimulationProfileBuilder
            .Build(injuredState, new[] { team })[team.Name];

        Assert.True(
            injured.PassOffenseRating <
            healthy.PassOffenseRating);
    }

    private static DynastyState State(
        SeasonPhase phase,
        IReadOnlyList<DynastyPlayer> roster) =>
        new()
        {
            DynastyId =
                Guid.Parse("77777777-8888-9999-aaaa-bbbbbbbbbbbb"),
            DynastyName = "Development Test",
            UserTeamName = "Test",
            SeasonYear = 2026,
            Week = 14,
            Phase = phase,
            ActiveRoster = roster
        };

    private static DynastyPlayer RatedPlayer(
        string name,
        Position position,
        int overall,
        int potential,
        int classYear) =>
        new()
        {
            PlayerId = Guid.NewGuid(),
            FullName = name,
            TeamName = "Test",
            Position = position,
            ClassYear = classYear,
            TalentLevel = 7,
            OverallRating = overall,
            PotentialRating = potential,
            SpeedRating = overall,
            StrengthRating = overall,
            AgilityRating = overall,
            AwarenessRating = overall,
            TechniqueRating = overall,
            DurabilityRating = overall
        };

    private static PlayerDevelopmentRecord DevelopmentRecord(
        Guid playerId,
        PlayerDevelopmentStage stage,
        int before,
        int after) =>
        new()
        {
            SeasonYear = 2026,
            PlayerId = playerId,
            PlayerName = "Test Player",
            TeamName = "Test",
            Position = Position.QB,
            BeforeOverall = before,
            AfterOverall = after,
            BeforePotential = 90,
            AfterPotential = 90,
            Stage = stage
        };
}
