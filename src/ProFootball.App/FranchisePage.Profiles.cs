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
        var attributes = new List<View> { Text($"OVR {f.OverallRating} · Potential {PlayerAttributeCatalog.PotentialStars(f.PotentialRating)}", 20, true) };
        foreach (var group in PlayerAttributeCatalog.Groups(f))
            attributes.Add(PlayerAttributeViews.Group(group.Title, group.Subtitle, group.Attributes,
                (title, subtitle) => new VerticalStackLayout { Spacing = 3, Children = { Text(title, 15, true), Text(subtitle, 11) } }));
        attributes.Add(Text("Higher ratings are better. Injury represents resistance to injury and wear.", 11));
        var actions = new ActionRow(); if (f.TeamName == s.UserTeam) actions.Add(Action("Contract & roster actions", () => ManagePlayer(p)));
        var lines = s.Games.Where(g => g.Phase == ProPhase.RegularSeason && g.Football.HasPlayed).SelectMany(g => g.Football.PlayerStats).Where(x => x.PlayerId == f.PlayerId).ToArray();
        await ShowPage("Player profile", identity, Panel("Player attributes", attributes.ToArray()), Panel("Status", Text(p.Retired ? "Retired" : p.Reserve ? "Reserve" : p.PracticeSquad ? "Practice squad" : "Active roster"), Text(f.CurrentInjury is { WeeksRemaining: > 0 } injury ? $"{injury.BodyArea} · {injury.WeeksRemaining} weeks remaining" : "Available"), Text($"Morale {p.Morale} · Depth {f.DepthChartOrder}", 12)),
            Panel("Contract", Text(contract is null ? "No active contract" : $"Current cap hit {Money(ProEconomy.CapHit(contract, s.Year))} · Bonus balance {Money(contract.SigningBonus)}"), Text(contract is null ? "" : string.Join("\n", contract.Years.Select(y => $"{y.Year} · Salary {Money(y.Salary)} · Guaranteed {Money(y.Guaranteed)}"))), Text(contract?.Estimated == true ? "Estimated game contract · real contract data not yet imported" : contract?.Source ?? "Franchise contract", 12)),
            PlayerAnalytics(p), Panel("Regular season", Text($"Passing {lines.Sum(x => x.PassYards)} yd · {lines.Sum(x => x.PassingTouchdowns)} TD\nRushing {lines.Sum(x => x.RushYards)} yd · Receiving {lines.Sum(x => x.ReceivingYards)} yd\nTackles {lines.Sum(x => x.Tackles)} · Sacks {lines.Sum(x => x.Sacks)}")), actions);
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
        offense.IsEnabled = tempo.IsEnabled = ProCareer.Role(state!) != StaffRole.DefensiveCoordinator; defense.IsEnabled = approach.IsEnabled = ProCareer.Role(state!) != StaffRole.OffensiveCoordinator;
        var save = new Button { Text = "Save game plan" }; var saved = Text("");
        save.Clicked += async (_, _) =>
        {
            var next = plan with { Offense = Enum.Parse<OffensivePlaybook>((string)offense.SelectedItem), Defense = Enum.Parse<DefensivePlaybook>((string)defense.SelectedItem), Approach = Enum.Parse<DefensiveApproach>((string)approach.SelectedItem), Tempo = Enum.Parse<OffensiveTempo>((string)tempo.SelectedItem) };
            next = ProCareer.LimitPlan(state!, next); state = state! with { GamePlans = state.GamePlans.Where(p => p.TeamName != state.UserTeam).Append(next).ToList() }; await Save(); saved.Text = "Saved · applies to future games";
        };
        return ShowPage("Game plan", Text("Offensive playbook"), offense, Text("Defensive playbook"), defense, Text("Defensive approach"), approach, Text("Tempo"), tempo, save, saved);
    }
    Task InjuryReport()
    {
        var players = state!.Players.Where(p => p.Football.TeamName == state.UserTeam && !p.Retired && (p.Reserve || p.Football.CurrentInjury is { WeeksRemaining: > 0 })).ToArray();
        return ShowPage("Injuries & availability", players.Length == 0 ? Text("No reported injuries or reserve players.") : new VerticalStackLayout { Spacing = 12, Children = { } },
            Panel("Unavailable players", players.Select(p => Panel(p.Football.FullName, PlayerCell(p), Text(p.Football.CurrentInjury is { } injury ? $"{injury.BodyArea} · {injury.WeeksRemaining} weeks remaining" : "Reserve list · source roster status " + p.RosterStatus))).ToArray()));
    }
    string newsFilter = "All";
    void News()
    {
        state = ProNews.Refresh(state!); var s = state!;
        body.Add(Text("League news", 23, true));
        body.Add(Action("Filter: " + newsFilter, async () => { var filter = await GameDialogs.ActionSheetAsync(this, "News", "Cancel", null, new[] { "All", "My team", "Unread" }.Concat(s.News.Select(n => n.Category).Distinct()).ToArray()); if (filter is null or "Cancel") return; newsFilter = filter; Render(); }));
        var stories = s.News.Where(n => newsFilter == "All" || newsFilter == "My team" && (n.Team == s.UserTeam || n.Headline.Contains(s.UserTeam)) || newsFilter == "Unread" && !s.ReadNewsIds.Contains(n.Id) || n.Category == newsFilter).Take(60).ToArray();
        if (stories.Length == 0) body.Add(Text("No stories in this view yet. Completed games and league activity will appear here."));
        foreach (var story in stories)
        {
            var n = story; body.Add(Panel((s.ReadNewsIds.Contains(n.Id) ? "" : "● ") + n.Headline, NewsThumbnail(n), Text($"{n.Year} · Week {n.Week} · {n.Category}", 12), Text(n.Body), Action(n.GameId is null ? "Read story" : "Game report ›", async () =>
            {
                state = state! with { ReadNewsIds = state.ReadNewsIds.Append(n.Id).Distinct().ToList() }; await Save();
                var game = state.Games.Concat(state.History.SelectMany(h => h.Games)).FirstOrDefault(g => g.Football.GameId == n.GameId);
                if (game is not null) await BoxScore(game.Football); else await GameDialogs.AlertAsync(this, n.Headline, n.Body, "Close");
            })));
        }
    }
    View NewsThumbnail(ProNewsStory story)
    {
        var team = state!.League.Teams.FirstOrDefault(t => t.Name == story.Team);
        var player = state.Players.FirstOrDefault(p => p.Football.TeamName == story.Team && story.Headline.Contains(p.Football.FullName, StringComparison.OrdinalIgnoreCase));
        if (player is not null) return Portrait(player, 42);
        return team is not null ? new TeamBadgeView(team, 42) : Text("LEAGUE", 12, true);
    }
    Task ShowBoxScore(ScheduledGame g) => GameCastBoxScore.ShowAsync(Navigation, new()
    {
        HomeTeamName = g.HomeTeamName, AwayTeamName = g.AwayTeamName, HomeScore = g.HomeScore ?? 0, AwayScore = g.AwayScore ?? 0,
        HomeStats = g.HomeStats ?? new(), AwayStats = g.AwayStats ?? new(), Periods = g.PeriodScores, Players = g.PlayerStats,
        Quarter = Math.Max(4, g.PeriodScores.Select(p => p.Period).DefaultIfEmpty(4).Max()), ClockSeconds = 0, IsComplete = g.HasPlayed,
        HomeMorale = g.HomeEndingMorale, AwayMorale = g.AwayEndingMorale, Weather = g.Weather, PlayerOfGame = g.PlayerOfGame
    }, ProGameCastPresentation.BoxStyle(state!.League));
}
