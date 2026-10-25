using DynastyManager.App;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Microsoft.Maui.Controls.Shapes;
using ProFootball.Core;
namespace ProFootball.App;

public sealed partial class FranchisePage : ContentPage
{
    readonly ProSaveStore store = new(System.IO.Path.Combine(FileSystem.AppDataDirectory, "pro-franchises-v1"));
    readonly VerticalStackLayout body = new() { Padding = 16, Spacing = 12 };
    readonly Grid shell = new() { RowDefinitions = new() { new(GridLength.Star), new(GridLength.Auto) }, ColumnDefinitions = new() { new(0), new(GridLength.Star) } };
    readonly VerticalStackLayout sidebar = new() { Padding = 12, Spacing = 6, BackgroundColor = DynastyTheme.Surface };
    readonly Grid bottom = new() { ColumnSpacing = 3, Padding = 6, BackgroundColor = DynastyTheme.Surface };
    readonly ScrollView scroll;
    ProFranchise? state; bool initialized, busy; string section = "Overview";
    static readonly string[] Sections = ["Overview", "Roster", "Staff", "Front office", "Schedule", "Standings", "History"];
    public FranchisePage()
    {
        Title = "Pro Football Franchise Manager"; NavigationPage.SetHasNavigationBar(this, false); BackgroundColor = DynastyTheme.Background;
        scroll = new() { Content = body }; shell.Add(sidebar, 0, 0); Grid.SetRowSpan(sidebar, 2); shell.Add(scroll, 1, 0); shell.Add(bottom, 1, 1); Content = shell;
        SizeChanged += (_, _) => LayoutShell(); Loaded += async (_, _) => { if (initialized) return; initialized = true; await Library(); };
    }
    static Label Text(string text, int size = 14, bool bold = false) => new() { Text = text, FontSize = size, FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None, TextColor = DynastyTheme.Text };
    Button Action(string text, Func<Task> action, string? id = null)
    {
        var b = new Button { Text = text, AutomationId = id, BackgroundColor = DynastyTheme.Panel, TextColor = DynastyTheme.Text };
        b.Clicked += async (_, _) => { if (busy) return; busy = true; b.IsEnabled = false; try { await action(); } catch (Exception e) { await GameDialogs.AlertAsync(this, "Unable to continue", e.Message, "OK"); } finally { busy = false; b.IsEnabled = true; } }; return b;
    }
    void LayoutShell() { sidebar.BackgroundColor = bottom.BackgroundColor = DynastyTheme.Surface; var wide = state is not null && Width >= 900; sidebar.IsVisible = wide; bottom.IsVisible = state is not null && !wide; shell.ColumnDefinitions[0].Width = wide ? 210 : 0; }
    Task Open(string target) { section = target; Render(); scroll.ScrollToAsync(0, 0, false); return Task.CompletedTask; }
    void RenderNavigation()
    {
        sidebar.Clear(); bottom.Clear(); sidebar.Add(Text("FRANCHISE", 22, true)); sidebar.Add(Text(state!.UserTeam, 15, true));
        foreach (var name in Sections) { var target = name; var b = Action(name, () => Open(target)); b.HorizontalOptions = LayoutOptions.Fill; b.BackgroundColor = section == name ? DynastyTheme.Selected : Colors.Transparent; sidebar.Add(b); }
        foreach (var (name, index) in new[] { ("Overview", 0), ("Roster", 1), ("Staff", 2), ("Front office", 3), ("League", 4) })
        { bottom.ColumnDefinitions.Add(new(GridLength.Star)); var target = name; var b = Action(name, () => target == "League" ? LeagueMenu() : Open(target)); b.FontSize = 10; b.Padding = 4; b.HorizontalOptions = LayoutOptions.Fill; b.BackgroundColor = section == name ? DynastyTheme.Selected : Colors.Transparent; bottom.Add(b, index, 0); }
        LayoutShell();
    }
    async Task LeagueMenu() { var pick = await GameDialogs.ActionSheetAsync(this, "League", "Cancel", null, "Schedule", "Standings", "History"); if (pick is "Schedule" or "Standings" or "History") await Open(pick); }
    View Panel(string title, params View[] views)
    { var content = new VerticalStackLayout { Spacing = 10 }; content.Add(Text(title, 20, true)); foreach (var view in views) content.Add(view); return new Border { Padding = 14, BackgroundColor = DynastyTheme.Surface, StrokeShape = new RoundRectangle { CornerRadius = 12 }, Content = content }; }
    View Cards(params View[] views)
    { var grid = new Grid { ColumnSpacing = 12, RowSpacing = 12 }; var previousCount = 0; void Layout() { var count = Width >= 900 ? 2 : 1; if (count == previousCount) return; previousCount = count; grid.ColumnDefinitions.Clear(); grid.RowDefinitions.Clear(); for (var c = 0; c < count; c++) grid.ColumnDefinitions.Add(new(GridLength.Star)); for (var i = 0; i < views.Length; i++) { if (i % count == 0) grid.RowDefinitions.Add(new(GridLength.Auto)); Grid.SetColumn(views[i], i % count); Grid.SetRow(views[i], i / count); } } foreach (var v in views) grid.Add(v); Layout(); grid.SizeChanged += (_, _) => Layout(); return grid; }
    async Task Library()
    {
        state = null; LayoutShell(); body.Clear(); body.Add(Text("PRO FOOTBALL", 28, true)); body.Add(Text("Franchise Manager", 24, true)); body.Add(Text("Development preview · " + AppInfo.VersionString));
        body.Add(Text("Real NFL teams and staff · Madden-based ratings · Build your own football legacy."));
        body.Add(Action("New franchise", Create, "pro-new")); body.Add(Action("Options", Options)); body.Add(Text("Your franchises", 20, true));
        foreach (var id in store.Slots().ToArray()) try { var save = await store.Load(id); body.Add(Action($"{save.Name} · {save.UserTeam} · {save.Year} {save.Phase} W{save.Week}", async () => { state = await store.Load(id); section = "Overview"; Render(); })); } catch (Exception) { body.Add(Text("Unavailable save · " + id)); }
        body.Add(Text("NFL roster snapshot: 2026 Weeks 6–7 (latest available per team). Madden 27 Week 6 plus launch fallback; unmatched ratings and all initial contracts/staff ratings are game estimates.", 12));
    }
    async Task Options()
    {
        var pick = await GameDialogs.ActionSheetAsync(this, "Options", "Cancel", null, "Appearance", "Data sources");
        if (pick == "Appearance") { var choice = await GameDialogs.ActionSheetAsync(this, "Appearance", "Cancel", null, "Dark", "Light"); if (choice is "Dark" or "Light") { Preferences.Set("app-theme", choice); Application.Current!.Resources = DynastyTheme.CreateResources(); Application.Current.UserAppTheme = DynastyTheme.RequestedTheme; BackgroundColor = DynastyTheme.Background; DynastyTheme.ApplyNativeAppearance(); if (state is null) await Library(); else Render(); } }
        if (pick == "Data sources") await GameDialogs.AlertAsync(this, "Data sources", (state?.DataSnapshot ?? "NFL 2026 Week 7 · Madden 27 Week 6") + "\nRosters: nflverse (CC BY 4.0). Ratings: EA public Madden ratings, launch archive fallback; unmatched players estimated. Staff: official team sites. Colors: nflverse. Badges: team abbreviations and NFL club colors. Initial finances and coaching ratings are estimates. Transactions and future draft classes are simulated. Existing fictional franchises remain intact.\nSources and file hashes are recorded in docs/nfl-data-manifest.json.", "Close");
    }
    Task Create()
    {
        body.Clear(); body.Add(Text("Start a franchise", 25, true)); var name = new Entry { Text = "My franchise", Placeholder = "Franchise name" }; body.Add(name);
        var league = ProLeagueService.Load(); var picker = new Picker { Title = "Choose your team", ItemsSource = league.Teams.Select(t => t.Name).ToList(), SelectedIndex = league.Teams.FindIndex(t => t.Id == "SEA") }; body.Add(picker);
        var preview = new ContentView(); void Preview() { var t = league.Teams[picker.SelectedIndex]; preview.Content = new HorizontalStackLayout { Spacing = 12, Children = { new TeamBadgeView(t, 70), Text(t.Name, 21, true) } }; }
        picker.SelectedIndexChanged += (_, _) => Preview(); Preview(); body.Add(preview);
        body.Add(Text("Start a new 2026 franchise using real roster identities. Actual source vacancies and reserve players are preserved; active roster limit is 53 and practice squad limit is 16."));
        body.Add(Action("Start franchise", async () => { state = await Task.Run(() => ProLeagueService.Create((string)picker.SelectedItem, name.Text)); await Save(); section = "Overview"; Render(); }, "pro-start")); body.Add(Action("Cancel", Library)); return Task.CompletedTask;
    }
    async Task Save() { if (state is not null) await store.Save(state); }
    void Render()
    {
        if (state is null) return; body.Clear(); bottom.ColumnDefinitions.Clear(); RenderNavigation();
        var header = new Grid { ColumnSpacing = 10, ColumnDefinitions = new() { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
        header.Add(Action("‹ Exit", async () => { await Save(); await Library(); }), 0, 0); header.Add(new VerticalStackLayout { Children = { Text(section == "Staff" ? "Coaching Hub" : section, 25, true), Text($"{state.UserTeam} · {state.Year} · {state.Phase} · Week {state.Week}", 12) } }, 1, 0); header.Add(Action("Options", Options), 2, 0); body.Add(header);
        var team = state.League.Teams.Single(t => t.Name == state.UserTeam); body.Add(new Border { Padding = 14, BackgroundColor = Color.FromArgb(team.Primary), Content = new HorizontalStackLayout { Spacing = 12, Children = { new TeamBadgeView(team, 56), new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Children = { new Label { Text = team.Name, FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = TeamBadgeView.Ink(team) }, new Label { Text = state.Name + " · " + state.Year, TextColor = TeamBadgeView.Ink(team) } } } } } });
        switch (section) { case "Overview": Overview(); break; case "Roster": Roster(); break; case "Staff": Staff(); break; case "Front office": FrontOffice(); break; case "Schedule": Schedule(); break; case "Standings": Standings(); break; case "History": History(); break; }
        var actions = new ActionRow(); actions.Add(Action("Save franchise", async () => { await Save(); await GameDialogs.AlertAsync(this, "Saved", "Franchise and rolling backups saved.", "OK"); })); actions.Add(Action("Save & exit", async () => { await Save(); await Library(); })); body.Add(actions);
    }
    void Overview()
    {
        var s = state!; var r = ProStandings.Build(s).Single(r => r.Team.Name == s.UserTeam);
        var actions = new ActionRow(); actions.Add(Action(s.Phase == ProPhase.Offseason ? "Advance offseason" : "Advance week", Advance, "pro-advance")); actions.Add(Action("This week's league games", () => Open("Schedule"), "pro-week-games")); body.Add(actions);
        if (s.Phase == ProPhase.Offseason) { body.Add(Panel("Offseason · " + s.OffseasonStep, Text(s.Champion is null ? "Build next season's roster." : "Champions · " + s.Champion), Action("Open front office", () => Open("Front office")))); }
        else { var game = s.Games.FirstOrDefault(g => g.Phase == s.Phase && g.Football.Week == s.Week && g.Football.InvolvesTeam(s.UserTeam)); if (game is not null) body.Add(GameCard(game)); else body.Add(Text("Your team has a bye this week.")); }
        var owner = s.Owners.FirstOrDefault(o => o.Team == s.UserTeam); body.Add(Cards(Panel("Season overview", Text($"{r.Wins}–{r.Losses}–{r.Ties}", 28, true), Text(r.Team.Conference + " · " + r.Team.Division), Action("Open standings ›", () => Open("Standings"))), Panel("Front office", Text($"{Money(ProEconomy.Room(s, s.UserTeam))} cap room", 24, true), Text($"Active {ProRoster.Active(s, s.UserTeam)}/53 · Owner security {owner?.Security ?? 65}/100"), Action("Contracts & salary cap ›", () => Open("Front office")))));
        body.Add(Panel("Latest transactions", Text(string.Join("\n", s.Transactions.TakeLast(4).Reverse()), 12)));
    }
    async Task Advance()
    {
        var label = state!.Phase == ProPhase.Offseason ? "Advance offseason?" : "Advance week?";
        if (!await GameDialogs.AlertAsync(this, label, state.Phase == ProPhase.Offseason ? "Process this offseason stage and CPU decisions?" : "Simulate remaining games, process bids/waivers and save?", "Advance", "Cancel")) return;
        state = await Task.Run(() => ProLeagueService.Advance(state)); await Save(); Render();
    }
    View GameCard(ProGame game)
    {
        var g = game.Football; var stack = new VerticalStackLayout { Spacing = 8 }; stack.Add(Text(g.AwayTeamName + " at " + g.HomeTeamName, 17, true));
        if (g.HasPlayed) { stack.Add(Text($"Final · {g.AwayScore}–{g.HomeScore}")); stack.Add(Action("Box score", () => BoxScore(g))); }
        else if (game.Phase == state!.Phase && g.Week == state.Week)
        { var row = new ActionRow(); row.Add(Action("Watch", () => Live(game, GameDayMode.Watch), "pro-watch")); row.Add(Action("Play", () => Live(game, GameDayMode.Play), "pro-play")); row.Add(Action("Simulate", async () => { var session = ProLeagueService.Start(state!, game, GameDayMode.Simulate); await Task.Run(() => GameDayEngine.SimulateToEnd(session)); state = ProLeagueService.Finish(state!, session); await Save(); Render(); }, "pro-sim")); stack.Add(row); }
        else stack.Add(Text(game.Phase + " · Week " + g.Week));
        return new Border { Padding = 12, StrokeThickness = 0, BackgroundColor = DynastyTheme.Surface, StrokeShape = new RoundRectangle { CornerRadius = 12 }, Content = stack };
    }

    readonly VerticalStackLayout schedule = new() { Spacing = 10 };
    void Schedule()
    {
        var row = new ActionRow(); foreach (var phase in new[] { ProPhase.Preseason, ProPhase.RegularSeason, ProPhase.Playoffs }) { var target = phase; row.Add(Action(phase.ToString(), () => { RenderSchedule(target); return Task.CompletedTask; })); }
        body.Add(row); RenderSchedule(state!.Phase == ProPhase.Offseason ? ProPhase.RegularSeason : state.Phase);
    }
    void RenderSchedule(ProPhase phase)
    {
        if (schedule.Parent is null) body.Add(schedule); schedule.Clear(); var weeks = state!.Games.Where(g => g.Phase == phase).Select(g => g.Football.Week).Distinct().Order().ToArray();
        var picker = new Picker { Title = "Week", ItemsSource = weeks.Select(w => $"Week {w}" + (phase == state.Phase && w == state.Week ? " · Current" : "")).ToList() }; var own = new Switch(); var list = new VerticalStackLayout { Spacing = 10 }; schedule.Add(new HorizontalStackLayout { Spacing = 8, Children = { own, Text("Your games only") } }); schedule.Add(picker); schedule.Add(list);
        void Draw() { list.Clear(); if (picker.SelectedIndex < 0 || picker.SelectedIndex >= weeks.Length) return; foreach (var game in state.Games.Where(g => g.Phase == phase && g.Football.Week == weeks[picker.SelectedIndex] && (!own.IsToggled || g.Football.InvolvesTeam(state.UserTeam)))) list.Add(GameCard(game)); }
        picker.SelectedIndexChanged += (_, _) => Draw(); own.Toggled += (_, _) => Draw(); picker.SelectedIndex = weeks.Length == 0 ? -1 : Math.Max(0, Array.IndexOf(weeks, state.Week)); Draw();
    }
    void Standings()
    {
        foreach (var group in ProStandings.Build(state!).GroupBy(r => r.Team.Conference + " · " + r.Team.Division))
        { body.Add(Text(group.Key, 19, true)); body.Add(Table([("Team", 210), ("W", 40), ("L", 40), ("T", 40), ("Pct", 65), ("Diff", 65)], group.Select(r => new View[] { TeamCell(r.Team), Text(r.Wins.ToString()), Text(r.Losses.ToString()), Text(r.Ties.ToString()), Text(r.Percentage.ToString(".000")), Text(r.Differential.ToString()) }))); }
        body.Add(Text("Initial tiebreaks: percentage, division, conference, opponent strength, differential. Full official multi-team chains remain in development.", 12));
    }
    View TeamCell(ProTeam team) => new HorizontalStackLayout { Spacing = 8, Children = { new TeamBadgeView(team, 28), Text(team.Name, 12) } };
    void Roster()
    {
        var players = state!.Players.Where(p => p.Football.TeamName == state.UserTeam && !p.Retired).ToArray();
        foreach (var group in players.GroupBy(p => p.Reserve ? "Reserve" : p.PracticeSquad ? "Practice squad" : "Active roster"))
        { body.Add(Text(group.Key + " · " + group.Count(), 21, true)); body.Add(Table([("Pos", 48), ("Player", 165), ("Age", 45), ("OVR", 50), ("Morale", 65), ("Cap hit", 90)], group.OrderBy(p => p.Football.Position).ThenBy(p => p.Football.DepthChartOrder).Select(p => new View[] { Text(OffensiveLineService.Label(p.Football), 12), Link(p.Football.FullName, () => Player(p)), Text(p.Age.ToString()), Text(p.Football.OverallRating.ToString(), 15, true), Text(p.Morale.ToString()), Text(Money(state.Contracts.Where(c => c.PlayerId == p.Football.PlayerId).Sum(c => ProEconomy.CapHit(c, state.Year))), 12) }))); }
    }
    void History()
    { body.Add(Text("Franchise history", 22, true)); foreach (var season in state!.History) body.Add(Panel(season.Year + " champions", Text(season.Champion, 21, true))); body.Add(Text("Draft history", 21, true)); foreach (var pick in state.DraftHistory.Where(p => p.Team == state.UserTeam).TakeLast(21).Reverse()) body.Add(Text($"{pick.Year} R{pick.Round} #{pick.Slot} · {pick.Player.Football.FullName}", 13)); }
    Task BoxScore(ScheduledGame game) => GameDialogs.AlertAsync(this, "Box score", $"{game.AwayTeamName} {game.AwayScore} · {game.HomeTeamName} {game.HomeScore}\nTotal yards {game.AwayStats?.TotalYards} / {game.HomeStats?.TotalYards}\nTurnovers {game.AwayStats?.Turnovers} / {game.HomeStats?.Turnovers}", "Close");
    async Task Live(ProGame game, GameDayMode mode)
    {
        var controlled = game.Football.InvolvesTeam(state!.UserTeam) ? state.UserTeam : game.Football.HomeTeamName;
        if (mode == GameDayMode.Play && !game.Football.InvolvesTeam(state.UserTeam)) { controlled = await GameDialogs.ActionSheetAsync(this, "Choose team to control", "Cancel", null, game.Football.AwayTeamName, game.Football.HomeTeamName); if (controlled is null or "Cancel") return; }
        var session = ProLeagueService.Start(state, game, mode, controlled);
        await Navigation.PushModalAsync(new ProLivePage(session, state.League, async () => { state = ProLeagueService.Finish(state!, session); await Save(); Render(); }));
    }
}
