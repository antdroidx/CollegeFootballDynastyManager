using DynastyManager.App;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using ProFootball.Core;
namespace ProFootball.App;

public sealed partial class FranchisePage
{
    View PlayerCell(ProPlayer p) => new HorizontalStackLayout { Spacing = 8, Children = { Portrait(p, 38), Link(p.Football.FullName, () => Player(p)) } };
    View Portrait(ProPlayer p, double size)
    {
        var team = state!.League.Teams.FirstOrDefault(t => t.Name == p.Football.TeamName) ?? state.League.Teams.Single(t => t.Name == state.UserTeam);
        return PersonPortrait.Create(p.Football.PlayerId, p.Football.FullName, Color.FromArgb(team.Primary), Color.FromArgb(team.Secondary), size, age: p.Age, appearance: p.Football.Appearance);
    }
    static Guid CoachId(ProCoach c) => new(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(c.Name))[..16]);
    View CoachIdentity(ProCoach c, ProTeam team) => new HorizontalStackLayout { Spacing = 12, Children = { PersonPortrait.Create(CoachId(c), c.Name, Color.FromArgb(team.Primary), Color.FromArgb(team.Secondary), 70, staff: true, age: 48), Text(c.Name, 22, true) } };
    async Task ShowPage(string title, params View[] views)
    {
        var page = new ContentPage { Title = title, BackgroundColor = DynastyTheme.Background, AutomationId = "pro-detail" };
        var stack = new VerticalStackLayout { Padding = 16, Spacing = 12, MaximumWidthRequest = 1000, HorizontalOptions = LayoutOptions.Fill };
        var close = new Button { Text = "‹ Back" }; close.Clicked += async (_, _) => await Navigation.PopModalAsync(); stack.Add(close); stack.Add(Text(title, 26, true));
        foreach (var view in views) stack.Add(view);
        page.Content = new ScrollView { Content = stack }; await Navigation.PushModalAsync(page);
    }
    async Task Player(ProPlayer original)
    {
        var s = state!; var p = s.Players.FirstOrDefault(x => x.Football.PlayerId == original.Football.PlayerId) ?? original; var f = p.Football;
        var contract = s.Contracts.LastOrDefault(c => c.Active && c.PlayerId == f.PlayerId);
        var identity = new HorizontalStackLayout { Spacing = 14, Children = { Portrait(p, 108), new VerticalStackLayout { Children = { Text(f.FullName, 23, true), Text($"#{f.JerseyNumber} · {OffensiveLineService.Label(f)} · {f.TeamName}"), Text($"Age {p.Age} · Experience {p.Experience} · {f.HeightInches / 12}′{f.HeightInches % 12}″ · {f.WeightPounds} lb") } } } };
        var attributes = new List<View> { Text($"OVR {f.OverallRating} · Potential {f.PotentialRating} · Morale {p.Morale}", 20, true) };
        foreach (var (name, rating) in new[] { ("Speed", f.SpeedRating), ("Strength", f.StrengthRating), ("Agility", f.AgilityRating), ("Awareness", f.AwarenessRating), ("Durability", f.InjuryRating), ("Passing power", f.Skills?.PassingPower ?? 0), ("Passing accuracy", f.Skills?.PassingAccuracy ?? 0), ("Catching", f.Skills?.Catching ?? 0), ("Tackling", f.Skills?.Tackling ?? 0), ("Run blocking", f.Skills?.RunBlocking ?? 0), ("Pass blocking", f.Skills?.PassBlocking ?? 0) })
        { attributes.Add(Text(name + " · " + rating)); attributes.Add(new ProgressBar { Progress = rating / 100d, ProgressColor = DynastyTheme.Accent }); }
        var actions = new ActionRow(); if (f.TeamName == s.UserTeam) actions.Add(Action("Contract & roster actions", () => ManagePlayer(p)));
        var lines = s.Games.Where(g => g.Phase == ProPhase.RegularSeason && g.Football.HasPlayed).SelectMany(g => g.Football.PlayerStats).Where(x => x.PlayerId == f.PlayerId).ToArray();
        await ShowPage("Player profile", identity, Panel("Status", Text(p.Retired ? "Retired" : p.Reserve ? "Reserve" : p.PracticeSquad ? "Practice squad" : "Active roster"), Text(f.CurrentInjury is { WeeksRemaining: > 0 } injury ? $"{injury.BodyArea} · {injury.WeeksRemaining} weeks remaining" : "Available"), Text("Ratings · " + p.RatingSource, 12)), Panel("Attributes", attributes.ToArray()),
            Panel("Contract", Text(contract is null ? "No active contract" : $"Current cap hit {Money(ProEconomy.CapHit(contract, s.Year))} · Signing bonus {Money(contract.SigningBonus)}"), Text(contract is null ? "" : string.Join("\n", contract.Years.Select(y => $"{y.Year} · Salary {Money(y.Salary)} · Guaranteed {Money(y.Guaranteed)}"))), Text(contract?.Estimated == true ? "Estimated game contract · real contract data not yet imported" : contract?.Source ?? "Franchise contract", 12)),
            Panel("Regular season", Text($"Passing {lines.Sum(x => x.PassYards)} yd · {lines.Sum(x => x.PassingTouchdowns)} TD\nRushing {lines.Sum(x => x.RushYards)} yd · Receiving {lines.Sum(x => x.ReceivingYards)} yd\nTackles {lines.Sum(x => x.Tackles)} · Sacks {lines.Sum(x => x.Sacks)}")), actions);
    }
    Task CoachProfile(ProCoach coach)
    {
        var team = state!.League.Teams.Single(t => t.Id == coach.TeamId);
        return ShowPage("Coach profile", CoachIdentity(coach, team), Panel(RoleName(coach.Role), Text(coach.Title), Text($"Coaching {coach.Rating} · Development {coach.Development}"), Text(coach.Vacant ? "Position vacant" : "Employed by " + team.Name), Text("Coaching ratings and illustrated portraits are generated game representations.", 12)));
    }
    Task GamePlan()
    {
        var plan = state!.GamePlans.FirstOrDefault(p => p.TeamName == state.UserTeam) ?? new TeamGamePlan { TeamName = state.UserTeam };
        Picker Choices<T>(T selected) where T : struct, Enum => new() { ItemsSource = Enum.GetNames<T>(), SelectedIndex = Array.IndexOf(Enum.GetValues<T>(), selected) };
        var offense = Choices(plan.Offense); var defense = Choices(plan.Defense); var approach = Choices(plan.Approach); var tempo = Choices(plan.Tempo);
        var save = new Button { Text = "Save game plan" }; var saved = Text("");
        save.Clicked += async (_, _) =>
        {
            var next = plan with { Offense = Enum.Parse<OffensivePlaybook>((string)offense.SelectedItem), Defense = Enum.Parse<DefensivePlaybook>((string)defense.SelectedItem), Approach = Enum.Parse<DefensiveApproach>((string)approach.SelectedItem), Tempo = Enum.Parse<OffensiveTempo>((string)tempo.SelectedItem) };
            state = state! with { GamePlans = state.GamePlans.Where(p => p.TeamName != state.UserTeam).Append(next).ToList() }; await Save(); saved.Text = "Saved · applies to future games";
        };
        return ShowPage("Game plan", Text("Offensive playbook"), offense, Text("Defensive playbook"), defense, Text("Defensive approach"), approach, Text("Tempo"), tempo, save, saved);
    }
    Task InjuryReport()
    {
        var players = state!.Players.Where(p => p.Football.TeamName == state.UserTeam && !p.Retired && (p.Reserve || p.Football.CurrentInjury is { WeeksRemaining: > 0 })).ToArray();
        return ShowPage("Injuries & availability", players.Length == 0 ? Text("No reported injuries or reserve players.") : new VerticalStackLayout { Spacing = 12, Children = { } },
            Panel("Unavailable players", players.Select(p => Panel(p.Football.FullName, PlayerCell(p), Text(p.Football.CurrentInjury is { } injury ? $"{injury.BodyArea} · {injury.WeeksRemaining} weeks remaining" : "Reserve list · source roster status " + p.RosterStatus))).ToArray()));
    }
    void News()
    {
        body.Add(Text("League news", 23, true));
        foreach (var game in state!.Games.Where(g => g.Football.HasPlayed).OrderByDescending(g => g.Phase).ThenByDescending(g => g.Football.Week).Take(32))
        { var g = game.Football; body.Add(Panel(g.HomeScore == g.AwayScore ? "Teams finish level" : g.WinnerTeamName + " wins", Text($"{game.Phase} · Week {g.Week} · {g.AwayTeamName} {g.AwayScore} – {g.HomeScore} {g.HomeTeamName}"), Action("Game report ›", () => BoxScore(g)))); }
        foreach (var transaction in state.Transactions.AsEnumerable().Reverse().Take(40)) body.Add(Panel("League transaction", Text(transaction)));
        if (!state.Games.Any(g => g.Football.HasPlayed) && state.Transactions.Count == 0) body.Add(Panel("Season kickoff", Text("Play or simulate games and make roster moves to build your league's news feed.")));
    }
    void Statistics()
    {
        var games = state!.Games.Where(g => g.Phase == ProPhase.RegularSeason && g.Football.HasPlayed).Select(g => g.Football).ToArray();
        body.Add(Text("Regular-season league leaders", 23, true));
        foreach (var (name, value) in new (string, Func<PlayerSeasonStatLine, int>)[] { ("Passing yards", x => x.PassYards), ("Rushing yards", x => x.RushYards), ("Receiving yards", x => x.ReceivingYards), ("Tackles", x => x.Tackles), ("Sacks", x => x.Sacks) })
        {
            var leaders = games.SelectMany(g => g.PlayerStats).GroupBy(x => x.PlayerId).Select(g => (Id: g.Key, Name: g.First().PlayerName, Value: g.Sum(value))).Where(x => x.Value > 0).OrderByDescending(x => x.Value).Take(12);
            body.Add(Panel(name, leaders.Select(x => { var p = state.Players.FirstOrDefault(p => p.Football.PlayerId == x.Id); return p is null ? Text($"{x.Name} · {x.Value}") : Link($"{x.Name} · {x.Value}", () => Player(p)); }).ToArray()));
        }
        if (games.Length == 0) body.Add(Text("Regular-season statistics appear after the first completed regular-season game."));
    }
    Task ShowBoxScore(ScheduledGame g)
    {
        var home = g.HomeStats ?? new(); var away = g.AwayStats ?? new();
        var stats = Table([("Statistic", 140), ("Away", 100), ("Home", 100)], new[] { new View[] { Text("Total yards"), Text(away.TotalYards.ToString()), Text(home.TotalYards.ToString()) }, new View[] { Text("Passing"), Text(away.PassYards.ToString()), Text(home.PassYards.ToString()) }, new View[] { Text("Rushing"), Text(away.RushYards.ToString()), Text(home.RushYards.ToString()) }, new View[] { Text("Turnovers"), Text(away.Turnovers.ToString()), Text(home.Turnovers.ToString()) }, new View[] { Text("Sacks allowed"), Text(away.SacksAllowed.ToString()), Text(home.SacksAllowed.ToString()) } });
        var players = Table([("Player", 190), ("Pass", 60), ("Rush", 60), ("Rec", 60), ("Tkl", 50)], g.PlayerStats.OrderByDescending(x => x.ScrimmageYards).ThenByDescending(x => x.Tackles).Select(x => { var p = state!.Players.FirstOrDefault(p => p.Football.PlayerId == x.PlayerId); return new View[] { p is null ? Text(x.PlayerName) : Link(x.PlayerName, () => Player(p)), Text(x.PassYards.ToString()), Text(x.RushYards.ToString()), Text(x.ReceivingYards.ToString()), Text(x.Tackles.ToString()) }; }));
        return ShowPage("Game report", Text($"{g.AwayTeamName} {g.AwayScore} – {g.HomeScore} {g.HomeTeamName}", 22, true), Text($"{g.SeasonYear} · Week {g.Week}"), Panel("Team statistics", stats), Panel("Player statistics", players));
    }
}
