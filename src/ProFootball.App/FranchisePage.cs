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
    readonly Border loading = new() { IsVisible = false, InputTransparent = true, Padding = 12, VerticalOptions = LayoutOptions.Start, HorizontalOptions = LayoutOptions.End, BackgroundColor = DynastyTheme.Surface,
        Content = new HorizontalStackLayout { Spacing = 8, Children = { new ActivityIndicator { IsRunning = true, Color = DynastyTheme.Accent, WidthRequest = 24 }, new Label { Text = "Working…", TextColor = DynastyTheme.Text, VerticalTextAlignment = TextAlignment.Center } } } };
    ProFranchise? state; bool initialized, busy; string section = "Overview";
    static readonly string[] Sections = ["Overview", "Roster", "Staff", "Front Office", "Schedule", "Standings", "Playoffs", "Statistics", "Analytics", "Inbox", "Transactions", "News", "Honors", "Career", "History"];
    public FranchisePage()
    {
        Title = "Pro Football Franchise Manager"; NavigationPage.SetHasNavigationBar(this, false); BackgroundColor = DynastyTheme.Background;
        scroll = new() { Content = body }; shell.Add(new ScrollView { Content = sidebar }, 0, 0); Grid.SetRowSpan((View)shell.Children[0], 2); shell.Add(scroll, 1, 0); shell.Add(bottom, 1, 1); var root = new Grid(); root.Add(shell); root.Add(loading); Content = root;
        SizeChanged += (_, _) => LayoutShell(); Loaded += async (_, _) => { if (initialized) return; initialized = true; await Library(); };
    }
    static Label Text(string text, int size = 14, bool bold = false) => new() { Text = text, FontSize = size, FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None, TextColor = DynastyTheme.Text };
    Button Action(string text, Func<Task> action, string? id = null)
    {
        var b = new Button { Text = text, AutomationId = id, BackgroundColor = DynastyTheme.Panel, TextColor = DynastyTheme.Text };
        b.Clicked += async (_, _) => { if (busy) return; busy = true; b.IsEnabled = false; loading.IsVisible = true; try { await Task.Delay(16); await action(); } catch (Exception e) { await GameDialogs.AlertAsync(this, "Unable to continue", e.Message, "OK"); } finally { busy = false; loading.IsVisible = false; b.IsEnabled = true; } }; return b;
    }
    void LayoutShell() { sidebar.BackgroundColor = bottom.BackgroundColor = DynastyTheme.Surface; var wide = state is not null && Width >= 650; ((View)shell.Children[0]).IsVisible = wide; sidebar.IsVisible = wide; bottom.IsVisible = state is not null && !wide; shell.ColumnDefinitions[0].Width = wide ? 180 : 0; }
    Task Open(string target) { section = target; Render(); scroll.ScrollToAsync(0, 0, false); return Task.CompletedTask; }
    void RenderNavigation()
    {
        sidebar.Clear(); bottom.Clear(); var club = state!.League.Teams.Single(t => t.Name == state.UserTeam);
        sidebar.Add(new Border { BackgroundColor = Color.FromArgb(club.Primary), Padding = 12, Content = new VerticalStackLayout { Spacing = 6, Children = { new TeamBadgeView(club, 58), new Label { Text = club.Abbreviation + " · " + state.Year, TextColor = TeamBadgeView.Ink(club), FontAttributes = FontAttributes.Bold } } } });
        foreach (var name in Sections) { var target = name; var b = Action(name, () => Open(target)); b.HorizontalOptions = LayoutOptions.Fill; b.BackgroundColor = section == name ? DynastyTheme.Selected : Colors.Transparent; sidebar.Add(b); }
        foreach (var (name, index) in new[] { ("Overview", 0), ("Roster", 1), ("Staff", 2), ("Front Office", 3), ("League", 4) })
        { bottom.ColumnDefinitions.Add(new(GridLength.Star)); var target = name; var b = Action(name, () => target == "League" ? LeagueMenu() : Open(target)); b.FontSize = 10; b.Padding = 4; b.HorizontalOptions = LayoutOptions.Fill; b.BackgroundColor = section == name ? DynastyTheme.Selected : Colors.Transparent; bottom.Add(b, index, 0); }
        LayoutShell();
    }
    async Task LeagueMenu() { var pick = await GameDialogs.ActionSheetAsync(this, "League", "Cancel", null, "Schedule", "Standings", "Playoffs", "Statistics", "Analytics", "Inbox", "Transactions", "News", "Honors", "Career", "History"); if (pick is "Schedule" or "Standings" or "Playoffs" or "Statistics" or "Analytics" or "Inbox" or "Transactions" or "News" or "Honors" or "Career" or "History") await Open(pick); }
    View Panel(string title, params View[] views)
    { var content = new VerticalStackLayout { Spacing = 10 }; if(!string.IsNullOrEmpty(title)) content.Add(Text(title, 20, true)); foreach (var view in views) content.Add(view); return new Border { Padding = 14, BackgroundColor = DynastyTheme.Surface, StrokeShape = new RoundRectangle { CornerRadius = 12 }, Content = content }; }
    View Cards(params View[] views)
    { var grid = new Grid { ColumnSpacing = 12, RowSpacing = 12 }; var previousCount = 0; void Layout() { var count = Width >= 900 ? 2 : 1; if (count == previousCount) return; previousCount = count; grid.ColumnDefinitions.Clear(); grid.RowDefinitions.Clear(); for (var c = 0; c < count; c++) grid.ColumnDefinitions.Add(new(GridLength.Star)); for (var i = 0; i < views.Length; i++) { if (i % count == 0) grid.RowDefinitions.Add(new(GridLength.Auto)); Grid.SetColumn(views[i], i % count); Grid.SetRow(views[i], i / count); } } foreach (var v in views) grid.Add(v); Layout(); grid.SizeChanged += (_, _) => Layout(); return grid; }
    async Task Library()
    {
        var previousBusy=busy;busy=true;
        state=null;LayoutShell();body.Clear();body.Add(new Image{Source="pro_brand.png",HeightRequest=145,HorizontalOptions=LayoutOptions.Center});
        body.Add(Text("PRO FOOTBALL",28,true));body.Add(Text("Franchise Manager",24,true));body.Add(Text("Development preview · "+AppInfo.VersionString+" · Build "+AppInfo.BuildString));
        var create=Action("New franchise",Create,"pro-new");var options=Action("Options",Options);create.IsEnabled=options.IsEnabled=false;body.Add(create);body.Add(options);body.Add(Text("Your franchises",20,true));
        var progress=new HorizontalStackLayout{Spacing=8,Children={new ActivityIndicator{IsRunning=true,WidthRequest=24,Color=DynastyTheme.Accent},Text("Loading franchises…")}};body.Add(progress);
        try
        {
            foreach(var id in store.Slots().ToArray())
            {
                var slot=id;var row=new ActionRow();string label;
                try
                {
                    var saved=await Task.Run(()=>store.Load(slot));label=$"{saved.Name} · {saved.UserTeam} · {saved.Year} {saved.Phase} W{saved.Week}";
                    row.Add(Action(label,async()=>{state=await Task.Run(()=>store.Load(slot));section="Overview";Render();}));
                }
                catch(Exception){label="Unavailable save · "+slot;row.Add(Text(label));}
                row.Add(Action("Delete save",async()=>{if(!await GameDialogs.AlertAsync(this,"Delete franchise?",label+"\nThis removes this franchise and its rolling backups. Other saves are unaffected.","Delete","Cancel"))return;await store.Delete(slot);await Library();}));body.Add(row);
            }

        }
        finally{body.Remove(progress);create.IsEnabled=options.IsEnabled=true;busy=previousBusy;}
    }
    async Task Options()
    {
        var pick = await GameDialogs.ActionSheetAsync(this, "Options", "Cancel", null, "Appearance", "Career options", "Data sources");
        if (pick == "Career options") { if (state is not null) await Open("Career"); else await GameDialogs.AlertAsync(this, "Career options", "Choose your coaching role and firing preference when creating a franchise.", "Close"); }
        if (pick == "Appearance") { var choice = await GameDialogs.ActionSheetAsync(this, "Appearance", "Cancel", null, "Dark", "Light"); if (choice is "Dark" or "Light") { Preferences.Set("app-theme", choice); Application.Current!.Resources = DynastyTheme.CreateResources(); Application.Current.UserAppTheme = DynastyTheme.RequestedTheme; BackgroundColor = DynastyTheme.Background; DynastyTheme.ApplyNativeAppearance(); if (state is null) await Library(); else Render(); } }
        if (pick == "Data sources") await GameDialogs.AlertAsync(this, "Data sources", "Rosters: nflverse (CC BY 4.0), 2026 Weeks 6–7. Staff: official team sites. Colors: nflverse. Player overall is calculated from shared football attributes using the college game's rating system. ", "Close");
    }
    Task Create()
    {
        body.Clear(); body.Add(Text("Start a franchise", 25, true)); var name = new Entry { Text = "My franchise", Placeholder = "Franchise name" }; body.Add(name);
        var league = ProLeagueService.Load(); var picker = new Picker { Title = "Choose your team", ItemsSource = league.Teams.Select(t => t.Name).ToList(), SelectedIndex = league.Teams.FindIndex(t => t.Id == "SEA") }; body.Add(picker);
        var role = new Picker { Title = "Coaching role", ItemsSource = new[] { "Head coach", "Offensive coordinator", "Defensive coordinator" }, SelectedIndex = 0 }; var firing = new Switch { IsToggled = true }; var coachName = new Entry { Placeholder = "Coach name (optional)" }; body.Add(role); body.Add(coachName); body.Add(new HorizontalStackLayout { Spacing = 8, Children = { firing, Text("Coach can be fired") } });
        body.Add(Text("Coordinators manage their unit; the GM approves shared cap and draft-pick decisions.", 12));
        var preview = new ContentView(); void Preview() { var t = league.Teams[picker.SelectedIndex]; preview.Content = new HorizontalStackLayout { Spacing = 12, Children = { new TeamBadgeView(t, 70), Text(t.Name, 21, true) } }; }
        picker.SelectedIndexChanged += (_, _) => Preview(); Preview(); body.Add(preview);
        var finances = new Picker { Title = "Starting contract data", ItemsSource = new[] { "Game contracts", "Imported contracts" }, SelectedIndex = 0 }; body.Add(finances);
        body.Add(Text("Build your 2026 franchise. Regular-season rosters allow 53 active players and a 16-player practice squad."));
        body.Add(Action("Start franchise", async () => { state = await Task.Run(() => ProLeagueService.Create((string)picker.SelectedItem, name.Text, finances.SelectedIndex == 1, role.SelectedIndex == 1 ? StaffRole.OffensiveCoordinator : role.SelectedIndex == 2 ? StaffRole.DefensiveCoordinator : StaffRole.HeadCoach, firing.IsToggled, coachName.Text)); await Save(); section = "Overview"; Render(); }, "pro-start")); body.Add(Action("Cancel", Library)); return Task.CompletedTask;
    }
    async Task Save() { if (state is not null) { state = ProNews.Refresh(state); await store.Save(state); } }
    void Render()
    {
        if (state is null) return; body.Clear(); bottom.ColumnDefinitions.Clear(); RenderNavigation();
        var header = new Grid { ColumnSpacing = 10, ColumnDefinitions = new() { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
        header.Add(Action("‹ Exit", async () => { await Save(); await Library(); }), 0, 0); header.Add(new VerticalStackLayout { Children = { Text(section == "Staff" ? "Coaching Hub" : section, 25, true), Text($"{state.UserTeam} · {state.Year} · {state.Phase} · Week {state.Week}", 12) } }, 1, 0); header.Add(Action("Options", Options), 2, 0); body.Add(header);
        var team = state.League.Teams.Single(t => t.Name == state.UserTeam); body.Add(new Border { Padding = 14, BackgroundColor = Color.FromArgb(team.Primary), Content = new HorizontalStackLayout { Spacing = 12, Children = { new TeamBadgeView(team, 56), new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Children = { new Label { Text = team.Name, FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = TeamBadgeView.Ink(team) }, new Label { Text = state.Name + " · " + state.Year, TextColor = TeamBadgeView.Ink(team) } } } } } });
        switch (section) { case "Overview": Overview(); break; case "Roster": Roster(); break; case "Staff": Staff(); break; case "Front Office": FrontOffice(); break; case "Schedule": Schedule(); break; case "Standings": Standings(); break; case "Playoffs": PlayoffTree(); break; case "History": History(); break; case "Career": Career(); break; case "Transactions": Transactions(); break; case "Inbox": Inbox(); break; case "News": News(); break; case "Statistics": Statistics(); break; case "Analytics": Analytics(); break; case "Honors": Honors(); break; }
        var actions = new ActionRow(); actions.Add(Action("Save franchise", async () => { await Save(); await GameDialogs.AlertAsync(this, "Saved", "Franchise and rolling backups saved.", "OK"); })); actions.Add(Action("Save & exit", async () => { await Save(); await Library(); })); body.Add(actions);
    }
    void Overview()
    {
        var s = state!; var r = ProStandings.Build(s).Single(r => r.Team.Name == s.UserTeam);
        var actions = new ActionRow(); actions.Add(Action(s.Phase == ProPhase.Offseason ? "Advance offseason" : "Advance week", Advance, "pro-advance")); actions.Add(Action("This week's league games", () => OpenLeagueGames(), "pro-week-games")); body.Add(actions);
        body.Add(Action("My team’s season schedule", () => OpenTeamSchedule()));
        if(s.Phase==ProPhase.Playoffs||s.Phase==ProPhase.RegularSeason&&s.Week>=14||s.Phase==ProPhase.Offseason&&s.OffseasonStep==OffseasonStep.Retention)body.Add(Action("Playoff bracket ›",()=>Open("Playoffs")));
        body.Add(WeeklyTalkButton());
        var notices=ProInbox.Build(s);var unread=notices.Count(n=>!s.ReadNotificationIds.Contains(n.Id));
        body.Add(Panel($"Franchise inbox · {unread} unread",Text(notices.Count==0?"No current action items.":string.Join("\n",notices.Take(3).Select(n=>n.Title)),12),Action("Open inbox",()=>Open("Inbox"))));
        if(s.UnsignedRookies.Any(r=>r.Team==s.UserTeam))
            body.Add(Panel("Unsigned draft picks",Text("Selected rookies remain unavailable until their four-year contracts fit the cap and roster."),Action("Sign rookie contracts",async()=>{state=ProDraft.SignRookies(state!,state!.UserTeam);await Save();Render();})));
        if(ProEconomy.Room(s,s.UserTeam)<0)
            body.Add(Panel("Salary cap action required",Text($"{Money(-ProEconomy.Room(s,s.UserTeam))} over the cap · Regular-season entry and play are blocked until compliant."),Action("Manage contracts & cap",()=>Open("Front Office"))));
        if (s.Phase == ProPhase.Preseason && s.Week == 3 && s.Games.Where(g => g.Phase == ProPhase.Preseason && g.Football.Week == 3).All(g => g.Football.HasPlayed) && ProRoster.Active(s, s.UserTeam) > 53)
            body.Add(Panel("Final roster cutdown", Text($"{ProRoster.Active(s, s.UserTeam)} active players · reduce to 53, then advance to the regular season."), Action("Manage roster", () => Open("Roster"))));
        if (s.Phase == ProPhase.Offseason) { body.Add(Panel("Offseason · " + s.OffseasonStep, Text(s.Champion is null ? "Build next season's roster." : "Champions · " + s.Champion), Action("Open front office", () => Open("Front Office")))); }
        else { var game = s.Games.FirstOrDefault(g => g.Phase == s.Phase && g.Football.Week == s.Week && g.Football.InvolvesTeam(s.UserTeam)); if (game is not null) body.Add(GameCard(game)); else body.Add(Text("Your team has a bye this week.")); }
        body.Add(Panel("Your coaching career", Text($"{s.Career?.Name} · {RoleName(ProCareer.Role(s))}", 17, true), Text(s.Career?.Unemployed == true ? "Seeking employment · Open your job offers" : $"Job security {s.Career?.Security ?? 65}/100 · Reputation {s.Career?.Reputation ?? 50}/100"), Action("Career & job offers ›", () => Open("Career"))));
        var owner = s.Owners.FirstOrDefault(o => o.Team == s.UserTeam); body.Add(Cards(Panel("Season overview", Text($"{r.Wins}–{r.Losses}–{r.Ties}", 28, true), Text(r.Team.Conference + " · " + r.Team.Division), Action("Open standings ›", () => Open("Standings"))), Panel("Front Office", Text($"{Money(ProEconomy.Room(s, s.UserTeam))} cap room", 24, true), Text($"Roster {ProRoster.CountForLimit(s, s.UserTeam)}/{ProRoster.Limit(s)} · Owner security {owner?.Security ?? 65}/100"), Action("Contracts & salary cap ›", () => Open("Front Office")))));
        body.Add(Panel("League news", Action("Headlines & results ›", () => Open("News")), Action("Statistics & leaders ›", () => Open("Statistics"))));
        body.Add(Panel("Latest transactions", Text(string.Join("\n", s.Transactions.TakeLast(4).Reverse()), 12),Action("View all transactions ›",()=>Open("Transactions"))));
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

    void Standings()
    {
        body.Add(Action("Playoff bracket ›",()=>Open("Playoffs")));
        foreach (var group in ProStandings.Build(state!).GroupBy(r => r.Team.Conference + " · " + r.Team.Division))
        { body.Add(Text(group.Key, 19, true)); body.Add(Table([("Team", 210), ("W", 40), ("L", 40), ("T", 40), ("Pct", 65), ("Diff", 65)], group.Select(r => new View[] { TeamCell(r.Team), Text(r.Wins.ToString()), Text(r.Losses.ToString()), Text(r.Ties.ToString()), Text(r.Percentage.ToString(".000")), Text(r.Differential.ToString()) }))); }
        body.Add(Text("Initial tiebreaks: percentage, division, conference, opponent strength, differential. Full official multi-team chains remain in development.", 12));
    }
    View TeamCell(ProTeam team) => new HorizontalStackLayout { Spacing = 8, Children = { new TeamBadgeView(team, 28), Text(team.Name, 12) } };
    void Roster()
    {
        var s = state!; var teams = s.League.Teams.OrderBy(t => t.Name).ToArray();
        var team = new Picker { Title = "Team", ItemsSource = teams.Select(t => t.Name).ToArray(), SelectedIndex = Array.FindIndex(teams, t => t.Name == s.UserTeam) };
        var search = new SearchBar { Placeholder = "Find a player" }; var positions = new Picker { Title = "Position", ItemsSource = new[] { "All positions" }.Concat(Enum.GetNames<Position>()).ToArray(), SelectedIndex = 0 };
        var list = new VerticalStackLayout { Spacing = 12 };
        var showRatings = false;
        var tools = new ActionRow(); tools.Add(Action("Organize depth chart", DepthChart)); var columnsButton = new Button { Text = "Columns: Essential" }; columnsButton.Clicked += (_, _) => { showRatings = !showRatings; columnsButton.Text = showRatings ? "Columns: Ratings" : "Columns: Essential"; Draw(); }; tools.Add(columnsButton); body.Add(tools); body.Add(team); body.Add(search); body.Add(positions);
        body.Add(Text("Δ since last offseason (season start for new saves) · Growth is remaining upside · Swipe for more columns", 12));
        body.Add(list);
        void Draw()
        {
            list.Clear(); if (team.SelectedIndex < 0) return;
            var selectedTeam = teams[team.SelectedIndex].Name;
            var players = s.Players.Where(p => p.Football.TeamName == selectedTeam && !p.Retired && (string.IsNullOrWhiteSpace(search.Text) || p.Football.FullName.Contains(search.Text.Trim(), StringComparison.OrdinalIgnoreCase)) && (positions.SelectedIndex <= 0 || p.Football.Position.ToString() == (string)positions.SelectedItem)).ToArray();
            foreach (var group in players.GroupBy(p => p.Reserve ? "Reserve" : p.PracticeSquad ? "Practice squad" : "Active roster"))
            {
                list.Add(Text(group.Key + " · " + group.Count(), 21, true));
                var columns = new List<(string, int)> { ("Pos", 48), ("Player", 220), ("Age", 45), ("OVR", 50), ("Δ", 45), ("Growth", 100), ("Morale", 65), ("Cap hit", 90) };
                if (showRatings) columns.AddRange(PlayerAttributeCatalog.Groups(players[0].Football).SelectMany(g => g.Attributes).Select(a => (a.Name, 95)));
                list.Add(Table(columns.ToArray(), group.OrderBy(p => p.Football.Position).ThenBy(p => p.Football.DepthChartOrder).Select(p =>
                {
                    var cells = new List<View> { Text(OffensiveLineService.Label(p.Football), 12), PlayerCell(p), Text(p.Age.ToString()), Text(p.Football.OverallRating.ToString(), 15, true), RatingDelta(p), Text(ProDevelopment.Growth(p)), Text(p.Morale.ToString()), Text(Money(s.Contracts.Where(c => c.PlayerId == p.Football.PlayerId).Sum(c => ProEconomy.CapHit(c, s.Year))), 12) };
                    if (showRatings) cells.AddRange(PlayerAttributeCatalog.Groups(p.Football).SelectMany(g => g.Attributes).Select(a => Text(a.Rating.ToString())));
                    return cells.ToArray();
                })));

            }
            if (players.Length == 0) list.Add(Text("No players match these filters."));
        }
        team.SelectedIndexChanged += (_, _) => Draw(); positions.SelectedIndexChanged += (_, _) => Draw(); search.TextChanged += (_, _) => Draw(); Draw();
    }
    Task DepthChart()
    {
        var s = state!; var picker = new Picker { Title = "Position", ItemsSource = s.Players.Where(p => p.Football.TeamName == s.UserTeam && !p.PracticeSquad && !p.Reserve && !p.Retired).Select(p => OffensiveLineService.Label(p.Football)).Distinct().Order().ToArray() };
        var list = new VerticalStackLayout { Spacing = 10 };
        void Draw()
        {
            list.Clear(); if (picker.SelectedItem is not string label) return;
            var players = state!.Players.Where(p => p.Football.TeamName == state.UserTeam && !p.PracticeSquad && !p.Reserve && !p.Retired && OffensiveLineService.Label(p.Football) == label).OrderBy(p => p.Football.DepthChartOrder).ThenByDescending(p => p.Football.OverallRating).ToArray();
            for (var index = 0; index < players.Length; index++)
            {
                var p = players[index]; var starter = index == 0; var button = new Button { Text = starter ? "First choice" : "Move to first", IsEnabled = !starter && ProCareer.Controls(state!, p.Football.Position) && p.Football.CurrentInjury is not { WeeksRemaining: > 0 } };
                button.Clicked += async (_, _) => { ProCareer.RequirePlayer(state!, p.Football.PlayerId); state = ProDepthChart.MakeStarter(state!, state!.UserTeam, p.Football.PlayerId); await Save(); Draw(); Render(); };
                list.Add(Panel($"{index + 1} · {p.Football.FullName}", PlayerCell(p), Text($"OVR {p.Football.OverallRating} · Morale {p.Morale}"), button));
            }
        }
        picker.SelectedIndexChanged += (_, _) => Draw(); picker.SelectedIndex = 0;
        return ShowPage("Depth chart", Text("Select a position to reorder your active roster. Changes apply to the next game; use the live depth chart for substitutions during a game."), picker, list);
    }
    void History()
    { body.Add(Text("Franchise history", 22, true)); foreach (var season in state!.History) body.Add(Panel(season.Year + " champions", Text(season.Champion, 21, true))); body.Add(Text("Draft history", 21, true)); foreach (var pick in state.DraftHistory.Where(p => p.Team == state.UserTeam).TakeLast(21).Reverse()) body.Add(Text($"{pick.Year} R{pick.Round} #{pick.Slot} · {pick.Player.Football.FullName}", 13)); }
    Task BoxScore(ScheduledGame game) => ShowBoxScore(game);
    async Task Live(ProGame game, GameDayMode mode)
    {
        var controlled = game.Football.InvolvesTeam(state!.UserTeam) ? state.UserTeam : game.Football.HomeTeamName;
        if (mode == GameDayMode.Play && !game.Football.InvolvesTeam(state.UserTeam)) { controlled = await GameDialogs.ActionSheetAsync(this, "Choose team to control", "Cancel", null, game.Football.AwayTeamName, game.Football.HomeTeamName); if (controlled is null or "Cancel") return; }
        var session = ProLeagueService.Start(state, game, mode, controlled);
        await GameCastCoinToss.ShowAsync(Navigation, session, controlled, mode == GameDayMode.Play, name =>
        {
            var team = state!.League.Teams.Single(t => t.Name == name);
            return new(team.Primary, team.Secondary, team.Abbreviation);
        });
        await Navigation.PushModalAsync(new ProLivePage(session, state.League, async () => { state = ProLeagueService.Finish(state!, session); await Save(); Render(); }, chooseTeamOnTakeover: !game.Football.InvolvesTeam(state.UserTeam)));
    }
}
