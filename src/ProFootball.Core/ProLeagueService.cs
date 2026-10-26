using System.Reflection;
using System.Text.Json;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
namespace ProFootball.Core;

public static class ProLeagueService
{
    static readonly JsonSerializerOptions SnapshotJson = new() { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
    public static NflSnapshot Snapshot()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ProFootball.Core.nfl-snapshot.json")!;
        return JsonSerializer.Deserialize<NflSnapshot>(stream, SnapshotJson)!;
    }
    public static ProLeague Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ProFootball.Core.league.json")!;
        var league = JsonSerializer.Deserialize<ProLeague>(stream, SnapshotJson)!;
        Validate(league); return league;
    }
    public static void Validate(ProLeague league)
    {
        if (league.Version != 1 || league.Teams.Count != 32 || league.Teams.Select(t => t.Id).Distinct().Count() != 32 || league.Teams.Select(t => t.Name).Distinct().Count() != 32 ||
            league.Teams.GroupBy(t => t.Conference).Count() != 2 || league.Teams.GroupBy(t => (t.Conference, t.Division)).Count() != 8 ||
            league.Teams.GroupBy(t => (t.Conference, t.Division)).Any(g => g.Count() != 4 || !g.Select(t => t.SeedRank).Order().SequenceEqual(new[] { 0, 1, 2, 3 })) || league.ActiveRosterLimit != 53 || league.PracticeSquadLimit != 16)
            throw new InvalidDataException("A league needs two conferences of four divisions, four unique teams per division, 53 active players and 16 practice players.");
    }
    public static ProFranchise Create(string userTeam, string name = "My franchise", bool sourcedContracts = false, StaffRole role = StaffRole.HeadCoach, bool firingEnabled = false, string? coachName = null)
    {
        var league = Load();
        if (!league.Teams.Any(t => t.Name == userTeam)) throw new ArgumentException("Unknown team");
        var snapshot = Snapshot();
        var franchise = ProEconomy.Initialize(ProPlayerRatingService.Migrate(new()
        {
            League = league,
            UserTeam = userTeam,
            Name = string.IsNullOrWhiteSpace(name) ? "My franchise" : name.Trim(),
            Players = snapshot.Players.ToList(),
            Coaches = snapshot.Coaches.ToList(),
            CoachCandidates = snapshot.Candidates.ToList(),
            Games = ProSchedule.Build(league, 2026),
            DataSnapshot = $"NFL 2026 Week {snapshot.Week} · Madden 27 Week 6 / launch · {snapshot.MaddenMatched} matched, {snapshot.Estimated} estimated"
        }));
        franchise = ProCareer.Initialize(franchise, role, firingEnabled, coachName);
        return sourcedContracts ? ProContractData.Apply(franchise) : ProContractData.CompleteRookieTerms(franchise);
    }
    public static List<ProPlayer> GeneratePlayers(ProLeague league)
    {
        var random = new Random(2026); var players = new List<ProPlayer>();
        (Position, int)[] positions = [(Position.QB, 3), (Position.RB, 4), (Position.WR, 6), (Position.TE, 3), (Position.OL, 9), (Position.DE, 4), (Position.DT, 4), (Position.MLB, 3), (Position.OLB, 4), (Position.CB, 5), (Position.FS, 3), (Position.SS, 3), (Position.K, 2)];
        string[] first = ["Marcus", "Jordan", "Evan", "Tyler", "Darius", "Noah", "Miles", "Andre", "Caleb", "Devin", "Cameron", "Isaiah"];
        string[] last = ["Carter", "Brooks", "Reed", "Bennett", "Hayes", "Morgan", "Coleman", "Foster", "Warren", "Price", "Marshall", "Grant", "Sullivan"];
        foreach (var team in league.Teams)
        {
            foreach (var (position, count) in positions)
                for (var depth = 1; depth <= count; depth++) Add(position, depth, false);
            for (var i = 0; i < league.PracticeSquadLimit; i++) Add(positions[i % positions.Length].Item1, 20 + i, true);
            void Add(Position position, int depth, bool practice)
            {
                var overall = Math.Clamp(84 - depth * 3 + random.Next(-7, 8) - (practice ? 8 : 0), 40, 94);
                var idBytes = new byte[16]; random.NextBytes(idBytes);
                var player = new DynastyPlayer
                {
                    PlayerId = new Guid(idBytes),
                    FullName = first[random.Next(first.Length)] + " " + last[random.Next(last.Length)],
                    TeamName = team.Name,
                    Position = position,
                    NcaaPositionCode = position == Position.K ? (depth == 2 ? 20 : 19) : position==Position.OL?5+(depth-1)%5:null,
                    ClassYear = 4,
                    OverallRating = overall,
                    SeasonStartOverall = overall,
                    PotentialRating = Math.Min(99, overall + random.Next(2, 14)),
                    SpeedRating = overall,
                    StrengthRating = overall,
                    AgilityRating = overall,
                    AwarenessRating = overall,
                    InjuryRating = 85,
                    DepthChartOrder = depth,
                    HeightInches = position == Position.OL ? 77 : 73,
                    WeightPounds = position is Position.OL or Position.DT ? 305 : position == Position.QB ? 220 : 215,
                    Skills = new() { PassingPower = overall, PassingAccuracy = overall, Catching = overall, BreakTackle = overall, Tackling = overall, RunBlocking = overall, PassBlocking = overall, BallCarrying = overall, Acceleration = overall, Leadership = overall, Discipline = 85, Jumping = overall, KickPower = overall, KickAccuracy = overall }
                };
                var age = random.Next(22, 34); players.Add(new(player, age, Math.Min(age - 22, random.Next(0, 9)), practice));
            }
        }
        return players.Select(p => ProPlayerRatingService.Refresh(p, true, true)).ToList();
    }
    public static TeamSimulationProfile Profile(ProFranchise state, string team)
    {
        var players = state.Players.Where(p => p.Football.TeamName == team && !p.PracticeSquad && !p.Reserve && !p.Retired).Select(p => p.Football).ToArray();
        double Rating(params Position[] positions) => players.Where(p => positions.Contains(p.Position)).OrderBy(p => p.DepthChartOrder).Take(8).Select(p => (double)p.OverallRating).DefaultIfEmpty(45).Average();
        int Coach(StaffRole role) => state.Coaches.FirstOrDefault(c => c.TeamId == state.League.Teams.Single(t => t.Name == team).Id && c.Role == role && !c.Vacant)?.Rating ?? (role == StaffRole.DefensiveCoordinator ? state.Coaches.FirstOrDefault(c => c.TeamId == state.League.Teams.Single(t => t.Name == team).Id && c.Role == StaffRole.HeadCoach && !c.Vacant)?.Rating ?? 55 : 55);
        return new()
        {
            TeamName = team,
            PassOffenseRating = Rating(Position.QB, Position.WR, Position.TE),
            RushOffenseRating = Rating(Position.RB, Position.OL),
            PassDefenseRating = Rating(Position.CB, Position.FS, Position.SS),
            RushDefenseRating = Rating(Position.DE, Position.DT, Position.MLB, Position.OLB),
            SpecialTeamsRating = Rating(Position.K),
            RosterSize = 53,
            GamePlan = state.GamePlans.FirstOrDefault(p => p.TeamName == team) ?? new() { TeamName = team },
            Morale = (int)state.Players.Where(p => p.Football.TeamName == team && !p.Retired).Select(p => (double)p.Morale).DefaultIfEmpty(60).Average(),
            HeadCoachLeadership = Coach(StaffRole.HeadCoach),
            HeadCoachGameManagement = Coach(StaffRole.HeadCoach),
            OffensiveCoordinatorScheme = Coach(StaffRole.OffensiveCoordinator),
            DefensiveCoordinatorScheme = Coach(StaffRole.DefensiveCoordinator)
        };
    }
    public static GameDaySession Start(ProFranchise state, ProGame game, GameDayMode mode, string? controlled = null)
    {
        if (mode == GameDayMode.Play && game.Football.InvolvesTeam(state.UserTeam) && state.Career?.Unemployed == true) throw new InvalidOperationException("Accept a coaching job before controlling this team.");
        if (game.Football.HasPlayed || game.Phase != state.Phase || game.Football.Week != state.Week) throw new InvalidOperationException("Game is not available this week.");
        if(state.Phase is ProPhase.RegularSeason or ProPhase.Playoffs && game.Football.InvolvesTeam(state.UserTeam)) ProSeasonEligibility.Require(state,state.UserTeam);
        var session = GameDayEngine.Start(game.Football, Profile(state, game.Football.HomeTeamName), Profile(state, game.Football.AwayTeamName), state.Players.Where(p => !p.PracticeSquad && !p.Reserve && !p.Retired).Select(p => p.Football).ToArray(), mode, rules: FootballRules.Professional with { Postseason = state.Phase == ProPhase.Playoffs });
        controlled ??= game.Football.InvolvesTeam(state.UserTeam) ? state.UserTeam : game.Football.HomeTeamName;
        if (!game.Football.InvolvesTeam(controlled)) throw new ArgumentException("Controlled team must be in this matchup.");
        session.ControlledTeamName = controlled;
        if (controlled == state.UserTeam) session.UserCoachRole = ProCareer.Role(state);
        return session;
    }
    public static ProFranchise Finish(ProFranchise state, GameDaySession session)
    {
        if (!session.IsComplete) throw new InvalidOperationException("Game has not finished.");
        var game = GameDayEngine.Finish(session);
        var scheduled = state.Games.SingleOrDefault(g => g.Football.GameId == game.GameId)
            ?? throw new InvalidOperationException("This game does not belong to this franchise.");
        if (scheduled.Football.HasPlayed) return state; // Retrying a save must not award morale twice.
        var next = state with { Games = state.Games.Select(g => g.Football.GameId == game.GameId ? g with { Football = game } : g).ToList() };
        return next with { Players = next.Players.Select(p => !game.InvolvesTeam(p.Football.TeamName) ? p : p with { Morale = Math.Clamp(GameDayCoachingService.LivePlayerMorale(session, p.Football, p.Morale) + (game.HomeScore == game.AwayScore ? 0 : game.WinnerTeamName == p.Football.TeamName ? 3 : -2), 10, 95) }).ToList() };
    }
    public static ProFranchise Advance(ProFranchise state)
    {
        state = ProDraft.SignRookies(ProDraft.AutoScout(state));
        if(state.Career?.Unemployed!=true && (state.Phase is ProPhase.RegularSeason or ProPhase.Playoffs || state.Phase==ProPhase.Preseason&&state.Week==3&&state.Games.Where(g=>g.Phase==ProPhase.Preseason&&g.Football.Week==3).All(g=>g.Football.HasPlayed)))
            ProSeasonEligibility.Require(state,state.UserTeam);
        state = ProMorale.Advance(ProRoster.ResolveWaivers(state with { TransactionTick = state.TransactionTick + 1 }));
        if (state.Phase == ProPhase.Offseason) return ProOffseason.Advance(state);
        state = ProCpu.Manage(state);
        foreach (var game in state.Games.Where(g => g.Phase == state.Phase && g.Football.Week == state.Week && !g.Football.HasPlayed).ToArray())
        { var session = Start(state, game, GameDayMode.Simulate); GameDayEngine.SimulateToEnd(session); state = Finish(state, session); }
        if (state.Phase == ProPhase.Preseason)
        {
            if (state.Week < 3) return state with { Week = state.Week + 1 };
            foreach (var team in state.League.Teams.Where(t => t.Name != state.UserTeam || state.Career?.Unemployed == true)) state = ProCpu.MakeRoom(ProCpu.Trim(state, team.Name), team.Name, 0);
            // Keep completed finale results while the user makes final cuts; no second simulation.
            if (ProSeasonEligibility.Issue(state,state.UserTeam) is {} issue)
                return ProEconomy.Log(state, issue);
            return state with { Phase = ProPhase.RegularSeason, Week = 1 };
        }
        if (state.Phase == ProPhase.RegularSeason) return state.Week < 18 ? state with { Week = state.Week + 1 } : ProPlayoffs.Begin(state);
        if (state.Phase == ProPhase.Playoffs) return ProPlayoffs.Advance(state);
        return state;
    }
}
