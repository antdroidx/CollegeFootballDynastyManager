using System.Diagnostics;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;
using Xunit.Abstractions;

namespace DynastyManager.Core.Tests;

public class PortalProcessingTests(ITestOutputHelper output)
{
    [Fact]
    public void Week19PortalProcessingCompletesForFullLeagueAndReportsProgress()
    {
        var teams = Enumerable.Range(0, 130).Select(index => new Team
        {
            Name = $"Team {index:D3}", Abbreviation = $"T{index}", ConferenceName = "Test", Prestige = 80
        }).ToDictionary(team => team.Name);
        var state = new DynastyState
        {
            DynastyName = "League portal workload", UserTeamName = "Team 000", SeasonYear = 2027,
            Phase = SeasonPhase.TransferPortal, Week = 19,
            ActiveRoster = teams.Values.SelectMany((team, teamIndex) => Enumerable.Range(0, 65)
                .Select(index => Player(teamIndex * 100 + index + 1, team.Name, index, 72))).ToArray(),
            Staff = teams.Values.SelectMany(team => Enum.GetValues<StaffRole>().Select(role =>
                new StaffMember { FullName = $"{team.Name} {role}", TeamName = team.Name,
                    Role = role, Recruiting = 75, PlayerDevelopment = 70 })).ToArray(),
            TransferPortalEntries = Enumerable.Range(1, 2000).Select(index => new TransferPortalEntry
            {
                OriginTeamName = "Departed School", SeasonYear = 2027,
                Player = Player(100000 + index, "Departed School", index, 75 + index % 15)
            }).ToArray()
        };
        state = state with
        {
            RecruitingInteractions = state.TransferPortalEntries.Take(30).Select(entry =>
                new RecruitingInteraction { ProspectId = entry.Player.PlayerId,
                    Source = RecruitingSource.TransferPortal, SeasonYear = state.SeasonYear,
                    ScholarshipOffered = true, WasCpuAssisted = true, UserInterest = 100 }).ToArray()
        };
        var progress = new RecordingProgress();
        var timer = Stopwatch.StartNew();
        var resolved = InteractiveRecruitingService.ResolveCurrentPhase(state, teams[state.UserTeamName], teams, progress);
        var result = CpuRecruitingService.ApplyPhaseAssistance(resolved, teams, progress: progress);
        var advanced = DynastyManager.Core.Seasons.SeasonProgression.Advance(result);
        Assert.Equal(SeasonPhase.Recruiting, advanced.Phase);
        Assert.Equal(20, advanced.Week);
        output.WriteLine($"130 teams, 8,450 roster players, 2,000 transfers: {timer.Elapsed.TotalSeconds:F2}s");
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(30));
        Assert.Equal(130, progress.Items.Count);
        Assert.Equal(Enumerable.Range(1, 130), progress.Items.Select(item => item.Completed));
        Assert.All(progress.Items, item => Assert.Equal(130, item.Total));
        Assert.True(result.RecruitingCommitments.Count > 100);
        Assert.Equal(result.ActiveRoster.Count, result.ActiveRoster.Select(player => player.PlayerId).Distinct().Count());
        Assert.All(result.ActiveRoster.GroupBy(player => player.TeamName), group => Assert.True(group.Count() <= 85));
        Assert.Equal(result.TransferPortalEntries.Count, result.TransferPortalEntries
            .Select(item => item.Player.PlayerId).Except(result.ActiveRoster.Select(item => item.PlayerId)).Count());
    }

    [Fact]
    public void FullLeagueHighSchoolMarketUsesActualCompetingOffers()
    {
        var teams = Enumerable.Range(0, 130).Select(index => new Team
        {
            Name = $"Team {index:D3}", Abbreviation = $"T{index}", ConferenceName = "Test",
            Prestige = 40 + index % 50, LegacyRegionId = index % 5
        }).ToDictionary(team => team.Name);
        var state = new DynastyState
        {
            DynastyId = Guid.Parse("99999999-2222-3333-4444-555555555555"),
            DynastyName = "FBS offer workload", UserTeamName = "Team 000", SeasonYear = 2027,
            Phase = SeasonPhase.Recruiting, Week = 20,
            ActiveRoster = teams.Values.SelectMany((team, teamIndex) => Enumerable.Range(0, 55)
                .Select(index => Player(teamIndex * 100 + index + 1, team.Name, index, 72))).ToArray(),
            HighSchoolRecruitingPool = DynastyManager.Data.Import.HighSchoolRecruitingPoolFactory.Create(
                Guid.Parse("99999999-2222-3333-4444-555555555555"), 2027, 130, [])
        };
        var timer = Stopwatch.StartNew();
        var offered = CompetingRecruitingService.AdvanceOffers(state, teams);
        var result = CompetingRecruitingService.ResolveWeek(offered, teams);
        output.WriteLine($"130 teams, 3,250 HS recruits: {timer.Elapsed.TotalSeconds:F2}s; " +
            $"{offered.RecruitingOffers.Count} offers, {result.RecruitingCommitments.Count} decisions");
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(30));
        Assert.True(offered.RecruitingOffers.Count > 1000);
        Assert.True(offered.RecruitingOffers.GroupBy(item => item.ProspectId).Count(group => group.Count() > 1) > 100);
        Assert.All(result.RecruitingCommitments, commitment => Assert.Contains(result.RecruitingOffers,
            offer => offer.ProspectId == commitment.ProspectId && offer.TeamName == commitment.TeamName &&
                offer.Status == RecruitingOfferStatus.Signed));
        Assert.Equal(result.ActiveRoster.Count, result.ActiveRoster.Select(item => item.PlayerId).Distinct().Count());
        Assert.All(result.ActiveRoster.GroupBy(item => item.TeamName), group => Assert.True(group.Count() <= 85));
    }

    [Fact]
    public void PlanningSnapshotEnumeratesRosterOnceForManyCandidates()
    {
        var team = new Team { Name = "Team", Abbreviation = "T", ConferenceName = "Test" };
        var state = new DynastyState { DynastyName = "Snapshot", UserTeamName = "Team" };
        var reads = 0;
        IEnumerable<DynastyPlayer> Roster()
        {
            reads++;
            for (var index = 0; index < 60; index++) yield return Player(index + 1, team.Name, index, 75);
        }
        var plan = TransferRosterPlanningService.CreatePlan(state, team, Roster());
        for (var index = 0; index < 1000; index++)
            plan.GetValue(Player(index + 10000, "Other", index, 80));
        Assert.Equal(1, reads);
    }

    private static DynastyPlayer Player(int id, string team, int index, int overall)
    {
        var bytes = new byte[16];
        BitConverter.GetBytes(id).CopyTo(bytes, 0);
        return new DynastyPlayer { PlayerId = new Guid(bytes), FullName = $"Player {id}", TeamName = team,
            Position = Enum.GetValues<Position>()[index % Enum.GetValues<Position>().Length],
            ClassYear = 2 + index % 3, OverallRating = overall, PotentialRating = overall + 5 };
    }

    private sealed class RecordingProgress : IProgress<(int Completed, int Total, string TeamName)>
    {
        public List<(int Completed, int Total, string TeamName)> Items { get; } = [];
        public void Report((int Completed, int Total, string TeamName) value) => Items.Add(value);
    }
}
