using DynastyManager.Core.Models;
using DynastyManager.Core.Seasons;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Import;
using DynastyManager.Data.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace DynastyManager.Core.Tests;

public class DynastyHealthTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(0, 12, 5, 1)]
    [InlineData(6, 12, 5, 2)]
    [InlineData(11, 12, 5, 3)]
    [InlineData(0, 130, 10, 4)]
    [InlineData(6, 130, 10, 5)]
    [InlineData(11, 130, 10, 6)]
    public async Task CareersPreserveDynastyHealth(int userIndex, int teamCount, int years, int seed)
    {
        var teams = Enumerable.Range(0, teamCount).Select(index => new Team
        {
            Name = $"Program {index:D2}", Abbreviation = $"P{index}", ConferenceName = teamCount <= 12 ? "Test" : $"Conference {index / 10}",
            Prestige = 35 + index % 12 * 5, LegacyRegionId = index % 5
        }).ToDictionary(team => team.Name);
        var user = teams[$"Program {userIndex:D2}"];
        var state = new DynastyState
        {
            DynastyId = new Guid(seed, 2222, 3333, 4, 4, 5, 5, 5, 5, 5, 5),
            DynastyName = "Five year health", UserTeamName = user.Name, SeasonYear = 2026,
            Phase = SeasonPhase.RegularSeason, RecruitingAssistanceEnabled = true,
            RecruitingAssistanceWorkloadPercent = 100,
            ActiveRoster = teams.Values.SelectMany((team, teamIndex) => InitialRoster(team, teamIndex)).ToArray()
        };
        state = StaffManagementService.EnsureLeagueStaff(state, teams.Values);
        var path = Path.Combine(Path.GetTempPath(), $"dynasty-health-{userIndex}-{Guid.NewGuid():N}.db");
        try
        {
            await using var repository = new SqliteDynastySaveRepository(path);
            for (var cycle = 0; cycle < years; cycle++)
            {
                state = state with
                {
                    Phase = SeasonPhase.RegularSeason,
                    Schedule = CareerSchedule(teams.Keys.ToArray(), state.SeasonYear)
                };
                for (var gameWeek = 0; gameWeek <= 13; gameWeek++)
                {
                    state = state with { Week = gameWeek };
                    var profiles = DynastyRosterSimulationProfileBuilder.Build(state, teams.Values);
                    state = WeekSimulation.SimulateCurrentRegularSeasonWeek(state, teams, profiles);
                    state = TransferPortalMarketService.AdvanceRegularSeasonWeek(state);
                    state = InteractiveRecruitingService.EnsurePhaseInitialized(state, user);
                    state = CpuRecruitingService.ApplyWeeklyUserAssistance(state, user);
                    state = InteractiveRecruitingService.ResolveCurrentPhase(state, user, teams);
                    if (gameWeek is 3 or 6 or 9 or 12) state = PlayerDevelopmentService.ApplyMidseasonDevelopment(state);
                }
                Assert.All(state.Schedule, game => Assert.True(game.HasPlayed));
                Assert.Equal(state.PendingTransferCommitments.Count,
                    state.PendingTransferCommitments.Select(item => item.Player.PlayerId).Distinct().Count());
                state = DynastyHistoryService.FinalizeSeason(state, teams);
                state = StaffManagementService.AdvanceSeason(state);
                state = ProgramPrestigeService.ApplySeasonResults(state, teams);
                state = state with { Phase = SeasonPhase.TransferPortal, Week = 19 };
                state = OffseasonPlayerLifecycleService.EnterTransferPortal(state);
                state = TransferPortalMarketService.MaterializePendingCommitments(state);
                state = InteractiveRecruitingService.EnsurePhaseInitialized(state, user);
                state = CpuRecruitingService.ApplyWeeklyUserAssistance(state, user);
                state = InteractiveRecruitingService.ResolveCurrentPhase(state, user, teams);
                state = CpuRecruitingService.ApplyPhaseAssistance(state, teams);
                state = SeasonProgression.Advance(state);
                state = state with { HighSchoolRecruitingPool = HighSchoolRecruitingPoolFactory.Create(
                    state.DynastyId, state.SeasonYear, teams.Count, []) };
                var weekly = new List<int>();
                for (var week = 0; week < SeasonProgression.RecruitingWeekCount; week++)
                {
                    state = InteractiveRecruitingService.EnsurePhaseInitialized(state, user);
                    state = CpuRecruitingService.ApplyWeeklyUserAssistance(state, user);
                    var before = state.RecruitingCommitments.Count(item => item.SeasonYear == state.SeasonYear &&
                        item.TeamName == user.Name && item.Source == RecruitingSource.HighSchool);
                    state = InteractiveRecruitingService.ResolveCurrentPhase(state, user, teams);
                    state = CpuRecruitingService.AdvanceCpuRecruitingWeek(state, teams);
                    var after = state.RecruitingCommitments.Count(item => item.SeasonYear == state.SeasonYear &&
                        item.TeamName == user.Name && item.Source == RecruitingSource.HighSchool);
                    weekly.Add(after - before);
                    state = SeasonProgression.Advance(state);
                }
                Assert.Equal(SeasonPhase.RosterManagement, state.Phase);
                Assert.All(state.RecruitingCommitments.Where(item => item.SeasonYear == state.SeasonYear), commitment =>
                    Assert.Contains(state.RecruitingOffers, offer => offer.SeasonYear == commitment.SeasonYear &&
                        offer.Source == commitment.Source && offer.ProspectId == commitment.ProspectId &&
                        offer.TeamName == commitment.TeamName && offer.Status == RecruitingOfferStatus.Signed));
                var rankings = RecruitingRankingService.BuildTeamRankings(state, teams.Keys);
                var rank = rankings.Single(item => item.TeamName == user.Name);
                state = WalkOnRosterService.FillAllTeams(state, teams);
                state = RosterManagementService.NormalizeAllDepthCharts(state);
                AssertHealthy(state, teams);
                Assert.InRange(state.ActiveRoster.Average(p => p.OverallRating), 55, 88);
                Assert.True(state.ActiveRoster.Count(p => p.OverallRating >= 90) < state.ActiveRoster.Count * .20, "Elite ratings became too common");
                var roster = state.ActiveRoster.Where(player => player.TeamName == user.Name).ToArray();
                var cpuAverage = state.ActiveRoster.Where(player => player.TeamName != user.Name)
                    .Average(player => player.OverallRating);
                var peers = teams.Values.Where(team => team.Name != user.Name &&
                    Math.Abs(ProgramPrestigeService.GetCurrentPrestige(state, team) -
                        ProgramPrestigeService.GetCurrentPrestige(state, user)) <= 10).Select(team => team.Name).ToHashSet();
                var peerAverage = state.ActiveRoster.Where(player => peers.Contains(player.TeamName))
                    .Average(player => player.OverallRating);
                Assert.True(roster.Average(player => player.OverallRating) >= peerAverage - 10,
                    "Assistance fell more than ten OVR behind similar-prestige CPU programs");
                Assert.True(roster.Count(player => !player.IsWalkOn) >= 50,
                    "Assistance left too much of the roster dependent on walk-ons");
                output.WriteLine($"seed={seed} teams={teamCount} prestige={ProgramPrestigeService.GetCurrentPrestige(state, user)} year={state.SeasonYear} HS={rank.HighSchoolCount} " +
                    $"portal={rank.TransferCount} rank={rank.Rank}/{teamCount} walkons={roster.Count(player => player.IsWalkOn)} " +
                    $"userOVR={roster.Average(player => player.OverallRating):F1} CPUOVR={cpuAverage:F1} elite90={state.ActiveRoster.Count(p => p.OverallRating >= 90) / (double)state.ActiveRoster.Count:P1} peerOVR={peerAverage:F1} " +
                    $"weeklyHS={string.Join(',', weekly)}");

                var save = await repository.SaveAsync(state, SaveKind.Manual);
                var loaded = await repository.LoadAsync(save);
                Assert.NotNull(loaded);
                Assert.Equal(state.ActiveRoster.Select(player => player.PlayerId), loaded.ActiveRoster.Select(player => player.PlayerId));
                Assert.Equal(state.RecruitingCommitments.Count, loaded.RecruitingCommitments.Count);
                Assert.Equal(state.ProgramPrestigeHistory.Count, loaded.ProgramPrestigeHistory.Count);
                Assert.Equal(state.TeamSeasonHistory.Count, loaded.TeamSeasonHistory.Count);
                Assert.Equal(state.RecruitingOffers.Count, loaded.RecruitingOffers.Count);
                state = loaded;
                state = SeasonProgression.Advance(state);
                state = PlayerDevelopmentService.ApplyOffseasonDevelopment(state);
                AssertHealthy(state, teams);
                Assert.All(state.ActiveRoster, player => Assert.InRange(player.OverallRating, 40, 99));
                state = SeasonProgression.Advance(state);
                state = StaffManagementService.AdvanceSeason(state);
                state = state with { HighSchoolRecruitingPool = HighSchoolRecruitingPoolFactory.Create(
                    state.DynastyId, state.SeasonYear, teams.Count, []) };
                state = ScoutingDepartmentService.EnsureDepartment(state, user);
                state = SeasonProgression.Advance(state);
                Assert.Equal(SeasonPhase.RegularSeason, state.Phase);

            }
        }
        finally
        {
            foreach (var suffix in new[] { "", "-wal", "-shm" })
                if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }
    }

    // Fixed round-robin fixtures keep the career stress test focused on season and roster
    // evolution; randomized calendar construction has its own scheduling tests.
    private static IReadOnlyList<ScheduledGame> CareerSchedule(string[] teams, int year)
    {
        var rotation = teams.ToList();
        var games = new List<ScheduledGame>();
        for (var round = 0; round < Math.Min(12, teams.Length - 1); round++)
        {
            for (var pair = 0; pair < teams.Length / 2; pair++)
                games.Add(new ScheduledGame
                {
                    GameId = $"{year}-{round}-{pair}", SeasonYear = year, Week = round + 1,
                    HomeTeamName = rotation[pair], AwayTeamName = rotation[^(pair + 1)]
                });
            var last = rotation[^1];
            rotation.RemoveAt(rotation.Count - 1);
            rotation.Insert(1, last);
        }
        return games;
    }

    private static void AssertHealthy(DynastyState state, IReadOnlyDictionary<string, Team> teams)
    {
        Assert.Equal(state.ActiveRoster.Count, state.ActiveRoster.Select(player => player.PlayerId).Distinct().Count());
        Assert.Empty(state.ActiveRoster.Select(player => player.PlayerId)
            .Intersect(state.TransferPortalEntries.Select(entry => entry.Player.PlayerId)));
        Assert.Equal(state.RecruitingCommitments.Count, state.RecruitingCommitments
            .Select(item => (item.SeasonYear, item.Source, item.ProspectId)).Distinct().Count());
        foreach (var team in teams.Values)
        {
            var roster = state.ActiveRoster.Where(player => player.TeamName == team.Name).ToArray();
            Assert.Equal(85, roster.Length);
            Assert.All(roster, player => Assert.InRange(player.ClassYear, 1, 4));
            foreach (var need in DynastyRosterRules.StarterPositionCounts)
                Assert.True(roster.Count(player => player.Position == need.Key) >= need.Value,
                    $"{team.Name} lacks starting depth at {need.Key}");
        }
    }

    private static IEnumerable<DynastyPlayer> InitialRoster(Team team, int teamIndex)
    {
        var index = 0;
        foreach (var position in DynastyRosterRules.TargetPositionCounts)
        {
            for (var depth = 0; depth < position.Value; depth++)
            {
                var bytes = new byte[16];
                BitConverter.GetBytes(1000 * teamIndex + ++index).CopyTo(bytes, 0);
                yield return new DynastyPlayer
                {
                    PlayerId = new Guid(bytes), FullName = $"{team.Name} Player {index}", TeamName = team.Name,
                    Position = position.Key, ClassYear = 1 + depth % 4, TalentLevel = 6,
                    OverallRating = 58 + team.Prestige / 4 + depth % 5,
                    PotentialRating = 70 + team.Prestige / 5
                };
            }
        }
    }
}
