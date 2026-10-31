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
        b.Clicked += async (_, _) => { if (busy||openingOffseason) return; busy = true; b.IsEnabled = false; loading.IsVisible = true; try { await Task.Delay(16); await action(); } catch (Exception e) { await GameDialogs.AlertAsync(this, "Unable to continue", e.Message, "OK"); } finally { busy = false; loading.IsVisible = openingOffseason; b.IsEnabled = true; } }; return b;
    }
    void LayoutShell() { sidebar.BackgroundColor = bottom.BackgroundColor = DynastyTheme.Surface; var wide = state is not null && Width >= 650; ((View)shell.Children[0]).IsVisible = wide; sidebar.IsVisible = wide; bottom.IsVisible = state is not null && !wide; shell.ColumnDefinitions[0].Width = wide ? 180 : 0; }
    Task Open(string target) { section = target; Render(); scroll.ScrollToAsync(0, 0, false); return Task.CompletedTask; }
    void RenderNavigation()
    {
        sidebar.Clear(); bottom.Clear(); var club = state!.League.Teams.Single(t => t.Name == state.UserTeam);
        sidebar.Add(new Border { BackgroundColor = Color.FromArgb(club.Primary), Padding = 12, Content = new VerticalStackLayout { Spacing = 6, Children = { new TeamBadgeView(club, 58), new Label { Text = CityLabel(club) + " · " + state.Year, TextColor = TeamBadgeView.Ink(club), FontAttributes = FontAttributes.Bold } } } });
        foreach (var name in Sections) { var target = name; var b = Action(name, () => Open(target)); b.HorizontalOptions = LayoutOptions.Fill; b.BorderWidth=0; b.FontAttributes=section==name?FontAttributes.Bold:FontAttributes.None; b.BackgroundColor = section == name ? DynastyTheme.Selected : Colors.Transparent; sidebar.Add(b); }
        foreach (var (name, index) in new[] { ("Overview", 0), ("Roster", 1), ("Staff", 2), ("Front Office", 3), ("League", 4) })
        { bottom.ColumnDefinitions.Add(new(GridLength.Star)); var target = name; var b = Action(name, () => target == "League" ? LeagueMenu() : Open(target)); b.FontSize = 10; b.Padding = 4; b.BorderWidth=0; b.HorizontalOptions = LayoutOptions.Fill; b.BackgroundColor = section == name ? DynastyTheme.Selected : Colors.Transparent; bottom.Add(b, index, 0); }
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
        var create=Action("New franchise",Create,"pro-new");var options=Action("Settings",Options);create.IsEnabled=options.IsEnabled=false;body.Add(create);body.Add(options);body.Add(Text("Your franchises",20,true));
        var progress=new HorizontalStackLayout{Spacing=8,Children={new ActivityIndicator{IsRunning=true,WidthRequest=24,Color=DynastyTheme.Accent},Text("Loading franchises…")}};body.Add(progress);
        try
        {
            foreach(var id in store.Slots().ToArray())
            {
                var slot=id;var row=new ActionRow();string label;
                try
                {
                    var saved=await Task.Run(()=>store.Load(slot));label=$"{saved.Name} · {saved.UserTeam} · {saved.Year} {saved.Phase} W{saved.Week}";
                    row.Add(Action(label,async()=>{state=await Task.Run(()=>store.Load(slot));section="Overview";Render();await scroll.ScrollToAsync(0,0,false);}));
                }
                catch(Exception){label="Unavailable save · "+slot;row.Add(Text(label));}
                row.Add(Action("Delete save",async()=>{if(!await GameDialogs.AlertAsync(this,"Delete franchise?",label+"\nThis removes this franchise and its rolling backups. Other saves are unaffected.","Delete","Cancel"))return;await store.Delete(slot);await Library();}));body.Add(row);
            }

        }
        finally{body.Remove(progress);create.IsEnabled=options.IsEnabled=true;busy=previousBusy;}
    }
    async Task Options()
    {
        var choices=new List<string>{"Appearance","Career options","Data sources"};if(state is not null)choices.Add("Save & exit");
        var pick = await GameDialogs.ActionSheetAsync(this, "Settings", "Cancel", null, choices.ToArray());
        if(pick=="Save & exit"&&await GameDialogs.AlertAsync(this,"Save and exit?","Save this franchise and return to your franchise library?","Save & exit","Cancel")){await Save();await Library();return;}
        if (pick == "Career options") { if (state is not null) await Open("Career"); else await GameDialogs.AlertAsync(this, "Career options", "Choose your coaching role and firing preference when creating a franchise.", "Close"); }
        if (pick == "Appearance") { var choice = await GameDialogs.ActionSheetAsync(this, "Appearance", "Cancel", null, "Dark", "Light"); if (choice is "Dark" or "Light") { Preferences.Set("app-theme", choice); Application.Current!.Resources = DynastyTheme.CreateResources(); Application.Current.UserAppTheme = DynastyTheme.RequestedTheme; BackgroundColor = DynastyTheme.Background; DynastyTheme.ApplyNativeAppearance(); if (state is null) await Library(); else Render(); } }
        if (pick == "Data sources") await GameDialogs.AlertAsync(this, "Data sources", "Rosters: nflverse (CC BY 4.0), 2026 Weeks 6–7. Staff: official team sites. Colors: nflverse. Player overall is calculated from shared football attributes using the college game's rating system. ", "Close");
    }
    Task Create()
    {
        body.Clear(); body.Add(Text("Start a franchise", 25, true)); var name = new Entry { Text = "My franchise", Placeholder = "Franchise name", AutomationId="pro-franchise-name" }; body.Add(name);
        var league = ProLeagueService.Load(); var picker = new Picker { Title = "Choose your team", ItemsSource = league.Teams.Select(t => t.Name).ToList(), SelectedIndex = league.Teams.FindIndex(t => t.Id == "SEA") }; body.Add(picker);
        var role = new Picker { Title = "Coaching role", ItemsSource = new[] { "Head coach", "Offensive coordinator", "Defensive coordinator" }, SelectedIndex = 0 }; var firing = new Switch { IsToggled = true }; var coachName = new Entry { Placeholder = "Coach name (optional)" }; body.Add(role); body.Add(coachName); body.Add(new HorizontalStackLayout { Spacing = 8, Children = { firing, Text("Coach can be fired") } });
        body.Add(Text("Coordinators manage their unit; the GM approves shared cap and draft-pick decisions.", 12));
        var preview = new ContentView(); void Preview() { var t = league.Teams[picker.SelectedIndex]; preview.Content = new HorizontalStackLayout { Spacing = 12, Children = { new TeamBadgeView(t, 70), Text(t.Name, 21, true) } }; }
        picker.SelectedIndexChanged += (_, _) => Preview(); Preview(); body.Add(preview);
        var roster = new Picker {Title="Starting players",AutomationId="pro-starting-players",ItemsSource=new[]{"Real players","New generated players · balanced league"},SelectedIndex=0};body.Add(roster);
        var finances = new Picker { Title = "Starting contract data", ItemsSource = new[] { "Generated contracts", "Real roster contracts" }, SelectedIndex = 1 }; body.Add(finances);
        roster.SelectedIndexChanged+=(_,_)=>{finances.SelectedIndex=roster.SelectedIndex==1?0:1;finances.IsEnabled=roster.SelectedIndex==0;};
        body.Add(Text("Build your 2026 franchise. Regular-season rosters allow 53 active players, with separate injured reserves."));
        body.Add(Action("Start franchise", async () => { state = await Task.Run(() => ProLeagueService.Create((string)picker.SelectedItem, name.Text, finances.SelectedIndex == 1, role.SelectedIndex == 1 ? StaffRole.OffensiveCoordinator : role.SelectedIndex == 2 ? StaffRole.DefensiveCoordinator : StaffRole.HeadCoach, firing.IsToggled, coachName.Text, generatedPlayers:roster.SelectedIndex==1)); await Save(); section = "Overview"; Render(); }, "pro-start")); body.Add(Action("Cancel", Library)); return Task.CompletedTask;
    }
    async Task Save() { if (state is not null) { state = ProNews.Refresh(state); await store.Save(state); } }
    void Render()
    {
        if (state is null) return;
        if(state.Phase==ProPhase.Offseason&&guidedOffseasonKey!=OffseasonKey()){guidedOffseasonKey=OffseasonKey();section="Offseason";}
        body.Clear(); bottom.ColumnDefinitions.Clear(); RenderNavigation();
        var header = new Grid { ColumnSpacing = 10, ColumnDefinitions = new() { new(GridLength.Star), new(GridLength.Auto) } };
        header.Add(new VerticalStackLayout { Children = { Text(section == "Staff" ? "Coaching Hub" : section, 25, true), Text($"{state.UserTeam} · {state.Year} · {state.Phase} · Week {state.Week}", 12) } }, 0, 0); header.Add(Action("Settings", Options,"pro-settings"), 1, 0); body.Add(header);
        var team = state.League.Teams.Single(t => t.Name == state.UserTeam); body.Add(new Border { Padding = 14, BackgroundColor = Color.FromArgb(team.Primary), Content = new HorizontalStackLayout { Spacing = 12, Children = { new TeamBadgeView(team, 56), new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Children = { new Label { Text = team.Name, FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = TeamBadgeView.Ink(team) }, new Label { Text = state.Name + " · " + state.Year, TextColor = TeamBadgeView.Ink(team) } } } } } });
        if(body.Children.LastOrDefault() is View banner){var openTeam=new TapGestureRecognizer();openTeam.Tapped+=async(_,_)=>await TeamPage(team);banner.GestureRecognizers.Add(openTeam);SemanticProperties.SetDescription(banner,"View team "+team.Name);}
        switch (section) { case "Offseason": Dispatcher.Dispatch(async()=>await OpenOffseasonStage()); break; case "Overview": Overview(); break; case "Roster": Roster(); break; case "Staff": Staff(); break; case "Front Office": FrontOffice(); break; case "Schedule": Schedule(); break; case "Standings": Standings(); break; case "Playoffs": PlayoffTree(); break; case "History": History(); break; case "Career": Career(); break; case "Transactions": Transactions(); break; case "Inbox": Inbox(); break; case "News": News(); break; case "Statistics": Statistics(); break; case "Analytics": Analytics(); break; case "Honors": Honors(); break; }
        var actions = new ActionRow(); actions.Add(Action("Save franchise", async () => { await Save(); await GameDialogs.AlertAsync(this, "Saved", "Franchise and rolling backups saved.", "OK"); })); if(section!="Overview")body.Add(actions);
    }
    void Overview()
    {
        var s = state!; var r = ProStandings.Build(s).Single(x => x.Team.Name == s.UserTeam);
        var primary=new HorizontalStackLayout{Spacing=8};
        primary.Add(PrimaryAction(s.Phase==ProPhase.Offseason?"Continue offseason":"Advance week",s.Phase==ProPhase.Offseason?()=>Open("Offseason"):Advance,"pro-advance"));
        if(s.Phase!=ProPhase.Offseason)primary.Add(Action(s.Phase==ProPhase.Preseason?"Simulate preseason":s.Phase==ProPhase.RegularSeason?"Simulate regular season":"Simulate playoffs",SimulatePhase,"pro-simulate-phase"));
        primary.Add(Action("Save",Save));
        body.Add(primary);

        if(s.Phase!=ProPhase.Offseason)
        {
            var game=s.Games.FirstOrDefault(g=>g.Phase==s.Phase&&g.Football.Week==s.Week&&g.Football.InvolvesTeam(s.UserTeam));
            if(game is not null)body.Add(GameCard(game));else body.Add(Text("Your team has a bye this week."));
        }

        // Season overview card
        var owner=s.Owners.FirstOrDefault(o=>o.Team==s.UserTeam);
        var record=new Label{Text=$"{r.Wins}-{r.Losses}-{r.Ties}",TextColor=DynastyTheme.Text,FontSize=38,FontAttributes=FontAttributes.Bold};
        var recordSub=new Label{Text=$"{r.Team.Conference} · {r.Team.Division}",TextColor=DynastyTheme.Muted,FontSize=12};
        var recordColumn=new VerticalStackLayout{Spacing=2};recordColumn.Add(record);recordColumn.Add(recordSub);
        var tiles=new Grid{ColumnSpacing=12,RowSpacing=8,ColumnDefinitions=new(){new(GridLength.Star),new(GridLength.Star),new(GridLength.Star)},RowDefinitions=new(){new(GridLength.Auto)}};
        tiles.Add(StatTile("Cap space",SeasonCapLabel(s,s.UserTeam)),0,0);
        tiles.Add(StatTile("Roster",$"{ProRoster.CountForLimit(s,s.UserTeam)}/{ProRoster.Limit(s)}"),1,0);
        tiles.Add(StatTile("Head coach",owner is null?"You":s.Career?.Name??"You",$"Security {s.Career?.Security??65}/100"),2,0);
        var summaryCard=SectionCard("Season overview",null,recordColumn,tiles);
        var summaryButtons=new HorizontalStackLayout{Spacing=8};
        summaryButtons.Add(Action("Standings",()=>Open("Standings")));summaryButtons.Add(Action("Schedule",()=>OpenTeamSchedule()));summaryButtons.Add(Action("Career",()=>Open("Career")));
        if(s.Phase==ProPhase.Playoffs||s.Phase==ProPhase.RegularSeason&&s.Week>=14||s.Phase==ProPhase.Offseason&&s.OffseasonStep==OffseasonStep.Retention)summaryButtons.Add(Action("Playoff bracket",()=>Open("Playoffs")));
        var summaryCardAll=new VerticalStackLayout{Spacing=10};summaryCardAll.Add(summaryCard);summaryCardAll.Add(summaryButtons);

        // Weekly tasks 2x2
        var notices=ProInbox.Build(s);var unread=notices.Count(n=>!s.ReadNotificationIds.Contains(n.Id));
        var taskGrid=new Grid{ColumnSpacing=10,RowSpacing=10,ColumnDefinitions=new(){new(GridLength.Star),new(GridLength.Star)},RowDefinitions=new(){new(GridLength.Auto),new(GridLength.Auto)}};
        taskGrid.Add(TaskTile("Roster & depth","Lineup, depth chart, injuries",()=>Open("Roster")),0,0);
        taskGrid.Add(TaskTile("Staff & game plan","Coaches, schemes, weekly prep",()=>Open("Staff")),1,0);
        taskGrid.Add(TaskTile("Contracts & cap","Extensions, free agents, cap management",()=>Open("Front Office")),0,1);
        taskGrid.Add(TaskTile($"Inbox · {unread} unread","Messages, league updates",()=>Open("Inbox")),1,1);
        var taskColumn=new VerticalStackLayout{Spacing=8};taskColumn.Add(taskGrid);
        for(var i=0;i<Math.Min(3,notices.Count);i++){var n=notices[i];taskColumn.Add(Link(n.Title,()=>n.Id.StartsWith("retention-")?Open("Offseason"):n.PlayerId is Guid id&&s.Players.FirstOrDefault(p=>p.Football.PlayerId==id) is {} player?Player(player):Open("Inbox")));}

        // Dashboard layout in two columns on wide screens
        var dashboard=new Grid{ColumnSpacing=12,RowSpacing=12};
        int dashLayout=0;
        void LayoutDash(){var w=dashboard.Width>=640;var target=w?2:1;if(target==dashLayout)return;dashLayout=target;dashboard.ColumnDefinitions.Clear();dashboard.RowDefinitions.Clear();if(w){dashboard.ColumnDefinitions.Add(new(GridLength.Star));dashboard.ColumnDefinitions.Add(new(GridLength.Star));dashboard.RowDefinitions.Add(new(GridLength.Auto));dashboard.Children.Clear();dashboard.Add(summaryCardAll,0,0);dashboard.Add(taskColumn,1,0);}else{dashboard.ColumnDefinitions.Add(new(GridLength.Star));dashboard.RowDefinitions.Add(new(GridLength.Auto));dashboard.RowDefinitions.Add(new(GridLength.Auto));dashboard.Children.Clear();dashboard.Add(summaryCardAll,0,0);dashboard.Add(taskColumn,0,1);}}
        dashboard.SizeChanged+=(_,_)=>LayoutDash();
        dashboard.SizeChanged+=(_,_)=>LayoutDash();LayoutDash();
        body.Add(dashboard);

        if(s.UnsignedRookies.Any(rx=>rx.Team==s.UserTeam))body.Add(SectionCard("Unsigned draft picks","Clear cap/roster room to sign the remaining rookie contracts.",Action("Sign rookie contracts",async()=>{state=ProDraft.SignRookies(state!,state!.UserTeam);await Save();Render();})));
        if(s.Phase==ProPhase.Preseason&&s.Week==3&&s.Games.Where(x=>x.Phase==ProPhase.Preseason&&x.Football.Week==3).All(x=>x.Football.HasPlayed)&&ProRoster.Active(s,s.UserTeam)>53)body.Add(SectionCard("Final roster cutdown",$"Reduce {ProRoster.Active(s,s.UserTeam)} active players to 53 before opening day.",Action("Manage roster",()=>Open("Roster"))));
        state=ProNews.Refresh(state!);s=state;
        var newsChildren=new List<View>();
        foreach(var story in s.News.Take(4))
        {
            var n=story;var card=FootballNewsCard.Create(NewsThumbnail(n),$"{n.Category.ToUpperInvariant()} · {n.Year} · Week {n.Week}",n.Headline,n.Body,DynastyTheme.Accent);
            var tap=new TapGestureRecognizer();tap.Tapped+=async(_,_)=>await ShowPage(n.Headline,Text(n.Body),n.Team is null?Text("League news"):TeamCell(s.League.Teams.FirstOrDefault(t=>t.Name==n.Team)??r.Team));card.GestureRecognizers.Add(tap);SemanticProperties.SetDescription(card,n.Headline+". Open story.");
            newsChildren.Add(card);
        }
        if(newsChildren.Count==0)newsChildren.Add(Text("Results, injuries and league activity will appear here as your franchise advances.",12));
        newsChildren.Add(Action("Headlines & results ›",()=>Open("News")));
        newsChildren.Add(Action("Statistics & leaders ›",()=>Open("Statistics")));
        body.Add(SectionCard("League news",null,newsChildren.ToArray()));
        body.Add(SectionCard("Latest transactions",null,Text(string.Join("\n",ProTransactions.Migrate(s).TransactionLog.TakeLast(4).Reverse().Select(t=>$"{t.Year} {ProTransactions.Period(t.Phase,t.Week)} · {t.Activity}")),12),Action("View all transactions ›",()=>Open("Transactions"))));
    }    async Task Advance()
    {
        var label = state!.Phase == ProPhase.Offseason ? "Complete "+ProOffseasonFlow.Title(state.OffseasonStep)+"?" : "Advance week?";
        if (!await GameDialogs.AlertAsync(this, label, state.Phase == ProPhase.Offseason ? "Process this offseason stage and CPU decisions?" : "Simulate remaining games, process bids/waivers and save?", "Advance", "Cancel")) return;
        state = await Task.Run(() => ProLeagueService.Advance(state)); await Save(); Render();
    }
    View GameCard(ProGame game)
    {
        var g = game.Football; var stack = new VerticalStackLayout { Spacing = 6 };
        stack.Add(Text(g.HasPlayed?"Latest result":"Upcoming game",19,true));
        View MatchupTeam(ProTeam t){var cell=new HorizontalStackLayout{Spacing=6,Children={new TeamBadgeView(t,28),Text(CityLabel(t),15,true)}};var open=new TapGestureRecognizer();open.Tapped+=async(_,_)=>await TeamPage(t);cell.GestureRecognizers.Add(open);return cell;}
        var teams=new Grid{ColumnDefinitions=new(){new(GridLength.Star),new(GridLength.Auto),new(GridLength.Star)},ColumnSpacing=8};
        teams.Add(MatchupTeam(state!.League.Teams.Single(t=>t.Name==g.AwayTeamName)),0,0);teams.Add(Text("at",12),1,0);teams.Add(MatchupTeam(state.League.Teams.Single(t=>t.Name==g.HomeTeamName)),2,0);stack.Add(teams);
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
        { body.Add(SectionCard(group.Key,null,Table([("Team", 210), ("W", 40), ("L", 40), ("T", 40), ("Pct", 65), ("Diff", 65)], group.Select(r => new View[] { TeamCell(r.Team), Text(r.Wins.ToString()), Text(r.Losses.ToString()), Text(r.Ties.ToString()), Text(r.Percentage.ToString(".000")), Text(r.Differential.ToString()) })))); }
    }
    View TeamCell(ProTeam team)
    {
        var row=new HorizontalStackLayout{Spacing=8,Children={new TeamBadgeView(team,28),Text(team.Name,12)}};
        var open=new TapGestureRecognizer();open.Tapped+=async(_,_)=>await TeamPage(team);row.GestureRecognizers.Add(open);SemanticProperties.SetDescription(row,"View team "+team.Name);return row;
    }
    Task TeamPage(ProTeam team)
    {
        var s=state!;var standing=ProStandings.Build(s).Single(x=>x.Team.Name==team.Name);
        return ShowPage(team.Name,new TeamBadgeView(team,56),Text(team.Conference+" · "+team.Division),Text($"{standing.Wins}–{standing.Losses}–{standing.Ties} · {SeasonCapLabel(s,team.Name)}"),
            Panel("Players",Table([("Player",190),("Pos",50),("OVR",55)],s.Players.Where(p=>p.Football.TeamName==team.Name&&!p.Retired).OrderBy(p=>p.Football.Position.RosterOrder()).ThenByDescending(p=>p.Football.OverallRating).Select(p=>new View[]{PlayerCell(p),Text(p.Football.Position.DisplayName()),Text(p.Football.OverallRating.ToString())}))),
            Panel("Schedule",Table([("Week",60),("Opponent",210),("Result",90)],s.Games.Where(g=>g.Football.InvolvesTeam(team.Name)).Select(g=>{var f=g.Football;var opponent=s.League.Teams.Single(t=>t.Name==(f.HomeTeamName==team.Name?f.AwayTeamName:f.HomeTeamName));return new View[]{Text(ProTransactions.Period(g.Phase,f.Week)),TeamCell(opponent),f.HasPlayed?Link($"{f.AwayScore}–{f.HomeScore}",()=>BoxScore(f)):Text("Upcoming")};}))));
    }
    void Roster()
    {
        var s = state!; var teams = s.League.Teams.OrderBy(t => t.Name).ToArray();
        var team = new Picker { Title = "Team", AutomationId="pro-roster-team", ItemsSource = teams.Select(t => t.Name).ToArray(), SelectedIndex = Array.FindIndex(teams, t => t.Name == s.UserTeam) };
        var search = new SearchBar { Placeholder = "Find a player", AutomationId = "pro-roster-search" }; var positions = new Picker { Title = "Position", AutomationId="pro-roster-position", ItemsSource = new[] { "All positions" }.Concat(PositionLabels.Names).ToArray(), SelectedIndex = 0 };
        var list = new VerticalStackLayout { Spacing = 12 };
        var showRatings = false;
        var tools = new ActionRow(); var organize=Action("Organize depth chart", DepthChart);tools.Add(organize); var columnsButton = new Button { Text = "Columns: Essential" }; columnsButton.Clicked += (_, _) => { showRatings = !showRatings; columnsButton.Text = showRatings ? "Columns: Ratings" : "Columns: Essential"; Draw(); }; tools.Add(columnsButton); body.Add(CompactFilters.Create((team,210),(search,210),(positions,130)));body.Add(tools);
        body.Add(Text("Delta since last offseason (season start for new saves) · Growth is remaining upside · Swipe for more columns", 12));
        body.Add(list);
        void Draw()
        {
            list.Clear(); if (team.SelectedIndex < 0) return;
            var selectedTeam = teams[team.SelectedIndex].Name;organize.IsVisible=selectedTeam==s.UserTeam;
            var players = s.Players.Where(p => p.Football.TeamName == selectedTeam && !p.Retired && (string.IsNullOrWhiteSpace(search.Text) || p.Football.FullName.Contains(search.Text.Trim(), StringComparison.OrdinalIgnoreCase)) && (positions.SelectedIndex <= 0 || p.Football.Position.DisplayName() == (string)positions.SelectedItem)).ToArray();
            foreach (var group in players.GroupBy(p => p.Reserve ? "Reserve" : "Active roster"))
            {
                list.Add(Text(group.Key + " · " + group.Count(), 20, true));
                // Group by position within each roster section.
                foreach (var pg in group.OrderBy(x => x.Football.Position.RosterOrder()).GroupBy(p => p.Football.Position))
                {
                    var header=new HorizontalStackLayout{Spacing=8,Children={PosPill(pg.Key),Text(PositionGroupTitle(pg.Key),16,true),new Label{Text=$"· {pg.Count()}",FontSize=13,TextColor=DynastyTheme.Muted}}};
                    var innerTable=Table(new[]{("Player",215),("Age",45),("OVR",50),("Delta",50),("Growth",90),("Morale",65),("Cap hit",90)},pg.OrderBy(x=>x.Football.DepthChartOrder).ThenByDescending(x=>x.Football.OverallRating).Select(p=>{
                        var cells=new List<View>{PlayerCell(p),Text(p.Age.ToString()),Text(p.Football.OverallRating.ToString(),15,true),RatingDelta(p),Text(ProDevelopment.Growth(p)),Text(p.Morale.ToString()),Text(Money(s.Contracts.Where(cc=>cc.PlayerId==p.Football.PlayerId).Sum(cc=>ProEconomy.CapHit(cc,s.Year))),12)};
                        return cells.ToArray();
                    }));
                    list.Add(SectionCard(null,null,header,innerTable));
                }
            }
            if(players.Length==0)list.Add(Text("No players match these filters."));
        }
        team.SelectedIndexChanged += (_, _) => Draw(); positions.SelectedIndexChanged += (_, _) => Draw(); search.TextChanged += (_, _) => Draw(); Draw();
    }    void History()
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