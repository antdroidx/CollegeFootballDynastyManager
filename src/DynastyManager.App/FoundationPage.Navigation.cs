using Microsoft.Maui.Layouts;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private readonly Button _settingsButton = new() { Text = "Settings", FontSize = 11, BackgroundColor = Colors.Transparent, Padding = new Thickness(7, 5) };
    private readonly ContentView _overviewBanner = new();
    private readonly ContentView _overviewGameActions = new();
    private readonly VerticalStackLayout _seasonHub = new() { Spacing = 12 };
    private readonly VerticalStackLayout _matchupHub = new() { Spacing = 12 };
    private readonly VerticalStackLayout _newsRows = new() { Spacing = 10 };
    private readonly VerticalStackLayout _injuryRows = new() { Spacing = 10 };
    private readonly Picker _injuryScope = new() { ItemsSource = new[] { "Your team", "Entire league" }, SelectedIndex = 0 };
    private Action<int>? _selectRankingsPanel;
    private bool _openingSchool;
    private readonly Stack<AppSection> _sectionTrail = new();
    private bool _restoringSection;

    private static string PhaseLabel(SeasonPhase phase) => phase switch
    {
        SeasonPhase.RegularSeason => "Regular season", SeasonPhase.ConferenceChampionship => "Conference championships",
        SeasonPhase.TransferPortal => "Transfer portal", SeasonPhase.RosterManagement => "Roster management", _ => phase.ToString()
    };

    private void RememberSection(AppSection next)
    {
        if (next == AppSection.Home) { _sectionTrail.Clear(); return; }
        if (_restoringSection || next == _activeSection) return;
        if (_activeSection == AppSection.NewDynasty && next == AppSection.Dynasty)
        {
            _sectionTrail.Clear(); _sectionTrail.Push(AppSection.Home); return;
        }
        _sectionTrail.Push(_activeSection);
    }

    private bool RestorePreviousSection()
    {
        if (_sectionTrail.Count == 0 || _sectionTrail.Peek() == AppSection.Home) return false;
        var previous = _sectionTrail.Pop();
        _restoringSection = true;
        try { ShowSection(previous); }
        finally { _restoringSection = false; }
        return true;
    }

    private static AppSection? PrimarySection(AppSection section) => section switch
    {
        AppSection.Dynasty or AppSection.Roster or AppSection.Playbook or AppSection.Recruiting or AppSection.Season => section,
        AppSection.Program => AppSection.Playbook,
        AppSection.Schedule or AppSection.Teams or AppSection.Rankings or AppSection.Stats or AppSection.Injuries or AppSection.News or AppSection.GameDay => AppSection.Season,
        _ => null
    };

    private View BuildOverviewSection()
    {
        _advanceWeekButton.BackgroundColor = DynastyTheme.Accent;
        _advanceWeekButton.TextColor = DynastyTheme.Background;
        _manualSaveButton.Text = "Save";
        return new VerticalStackLayout { Spacing = 12, Children =
        {
            _overviewBanner, new FlexLayout { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, Children = { _advanceWeekButton, _manualSaveButton } },
            _overviewGameActions, _homeHighlights
        } };
    }

    private static View NavigationCard(string title, string value, string hint, string color, Action open)
    {
        var card = new Border { BackgroundColor = DynastyTheme.Surface, Stroke = DynastyTheme.Palette("#497088"), Padding = 8,
            Content = new VerticalStackLayout { Spacing = 2, Children =
            {
                new Label { Text = title, FontSize = 11, TextColor = DynastyTheme.Muted },
                new Label { Text = value, FontSize = 17, FontAttributes = FontAttributes.Bold, TextColor = DynastyTheme.Text },
                new Label { Text = hint, FontSize = 11, TextColor = DynastyTheme.Accent }
            } } };
        var tap = new TapGestureRecognizer(); tap.Tapped += (_, _) => open(); card.GestureRecognizers.Add(tap);
        SemanticProperties.SetDescription(card, $"{title}. {value}. {hint}");
        return card;
    }

    private void OpenRankings(int panel)
    {
        ShowSection(AppSection.Rankings);
        _selectRankingsPanel?.Invoke(panel);
    }

    private async void OpenSchool(string name)
    {
        if (_openingSchool || _currentDynasty is null || !_teamsByName.ContainsKey(name)) return;
        _openingSchool = true;
        try
        {
            while (Navigation.ModalStack.Count > 0) await Navigation.PopModalAsync(false);
            _teamBrowserPicker.SelectedItem = name;
            ShowSection(AppSection.Teams);
        }
        finally { _openingSchool = false; }
    }

    private void RenderOverviewDashboard()
    {
        _homeHighlights.Children.Clear();
        if (_currentDynasty is null) return;
        var state = _currentDynasty;
        var games = state.Schedule.Where(g => g.SeasonYear == state.SeasonYear && g.HasPlayed && g.InvolvesTeam(state.UserTeamName)).ToArray();
        var wins = games.Count(g => g.WinnerTeamName == state.UserTeamName);
        var conference = games.Where(g => g.GameType == ScheduledGameType.Conference).ToArray();
        var conferenceWins = conference.Count(g => g.WinnerTeamName == state.UserTeamName);
        var ranking = BuildCurrentNationalRankings().FirstOrDefault(r => r.TeamName == state.UserTeamName);
        var next = state.Schedule.Where(g => g.SeasonYear == state.SeasonYear && !g.HasPlayed && g.InvolvesTeam(state.UserTeamName)).OrderBy(g => g.Week).FirstOrDefault();
        var opponent = next is null ? "No upcoming game" : next.HomeTeamName == state.UserTeamName ? next.AwayTeamName : next.HomeTeamName;
        var injuries = state.ActiveRoster.Count(p => p.TeamName == state.UserTeamName && p.CurrentInjury is not null);
        _overviewBanner.Content = SchoolBanner(state.UserTeamName, $"{state.DynastyName} · {state.SeasonYear} · {PhaseLabel(state.Phase)}");
        RenderOverviewGameActions(next);
        var cards = new Grid { ColumnSpacing = 10, RowSpacing = 10,
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) },
            RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Auto) } };
        var independent = _teamsByName.TryGetValue(state.UserTeamName, out var school) && school.ConferenceName == "Independent";
        cards.Add(NavigationCard("RECORD", $"{wins}–{games.Length - wins}", independent ? "Open season standings ›" : $"Conf {conferenceWins}–{conference.Length - conferenceWins} · Open standings ›",
            "#183E50", () => OpenRankings(independent ? 0 : 1)), 0, 0);
        cards.Add(NavigationCard("TEAM RANK", ranking is null ? "Unranked" : ranking.Rank <= 25 ? $"#{ranking.Rank}" : $"NR (#{ranking.Rank})",
            "Open rankings ›", "#293F65", () => OpenRankings(0)), 1, 0);
        cards.Add(NavigationCard("NEXT GAME", opponent, next is null ? "Open schedule ›" : $"Week {next.Week} · Matchup & prep ›", "#234A43",
            () => ShowSection(next is null ? AppSection.Schedule : AppSection.GameDay)), 0, 1);
        cards.Add(NavigationCard("INJURIES", $"{injuries} active", "Team & league report ›", "#443657", () => ShowSection(AppSection.Injuries)), 1, 1);
        var tasks = new Grid { ColumnSpacing=6,RowSpacing=6,ColumnDefinitions=new() {new(GridLength.Star),new(GridLength.Star)},RowDefinitions=new() {new(GridLength.Auto),new(GridLength.Auto)} };
        var taskIndex=0;
        foreach(var task in new[] { ("Recruiting & scouting", AppSection.Recruiting), ("Roster & depth",AppSection.Roster),
            ("Staff & game plan",AppSection.Playbook), ("Injury report",AppSection.Injuries) })
        {
            var button=new Button { Text=task.Item1+" ›", FontSize=11, HorizontalOptions=LayoutOptions.Fill, BackgroundColor=DynastyTheme.Panel, TextColor=DynastyTheme.Text };
            button.Clicked+=(_,_)=>ShowSection(task.Item2); tasks.Add(button,taskIndex%2,taskIndex/2);taskIndex++;
        }
        var standings=new VerticalStackLayout { Spacing=5 };
        var conferenceName=school?.ConferenceName;
        foreach(var entry in BuildCurrentNationalRankings().Where(r=>_teamsByName.TryGetValue(r.TeamName,out var t)&&t.ConferenceName==conferenceName).Take(5))
        {
            var row=new Grid { ColumnDefinitions=new() {new(GridLength.Star),new(GridLength.Auto)} };
            row.Add(SchoolCell(entry.TeamName),0,0); row.Add(TableText("#"+entry.Rank,true),1,0); standings.Add(row);
        }
        var pollPanel=ManagementPanel(conferenceName+" · Poll positions",standings,"Conference teams in the national poll");
        pollPanel.IsVisible=false;
        var dashboard=ResponsivePanels(ManagementPanel("Season overview", cards),ManagementPanel("Weekly tasks",tasks),pollPanel);
        dashboard.SizeChanged+=(_,_)=>pollPanel.IsVisible=dashboard.Width>=600;
        _homeHighlights.Children.Add(dashboard);
        if (state.UserCoachIsFired || state.UserCoachJobOffers.Any(o => o.Status == UserCoachJobOfferStatus.Pending))
            _homeHighlights.Children.Add(NavigationCard(state.UserCoachIsFired ? "ACTION REQUIRED" : "COACHING CAROUSEL", "Review coaching offers", "Open Program & staff ›", "#643238", () => ShowSection(AppSection.Program)));
        else if (state.Phase is SeasonPhase.TransferPortal or SeasonPhase.Recruiting or SeasonPhase.RosterManagement)
            _homeHighlights.Children.Add(NavigationCard("OFFSEASON", PhaseLabel(state.Phase), "Review your roster and recruiting ›", "#4C432A",
                () => ShowSection(state.Phase == SeasonPhase.RosterManagement ? AppSection.Roster : AppSection.Recruiting)));
        var all = new Button { Text = "News & headlines ›", TextColor = DynastyTheme.Palette("#E8C979"), BackgroundColor = Colors.Transparent };
        all.Clicked += (_, _) => ShowSection(AppSection.News);
        _homeHighlights.Children.Add(all);
        if (state.Schedule.Any(g => g.SeasonYear == state.SeasonYear && g.HasPlayed))
        {
            var recap = new Button { Text = "Weekly recap & highlights ›", FontSize = 12, TextColor = DynastyTheme.Accent, BackgroundColor = Colors.Transparent };
            recap.Clicked += async (_, _) => await ShowCollegeCenterAsync(3);
            _homeHighlights.Children.Add(recap);
        }
        var stories = DynastyNewsService.BuildOverview(state);
        if(stories.Count>0) _homeHighlights.Children.Add(ResponsivePanels(stories.Select(NewsCard).ToArray()));
        if (stories.Count == 0) _homeHighlights.Children.Add(new Label { Text = "Results, recruiting and injury updates will appear as your dynasty develops.", TextColor = DynastyTheme.Muted });
    }

    private View BuildSeasonHub() => _seasonHub;

    private void RenderSeasonHub()
    {
        _seasonHub.Children.Clear();
        if (_currentDynasty is null) return;
        _seasonHub.Children.Add(SchoolBanner(_currentDynasty.UserTeamName, $"{_currentDynasty.SeasonYear} season · {_currentDynasty.LeagueName}"));
        _seasonHub.Children.Add(SectionTitle("Season center", "Statistics, results and people across the entire league"));
        var links = new[]
        {
            ("Statistics", "Season & career totals ›", AppSection.Stats, "#234B46"),
            ("Rankings", "National & conference ›", AppSection.Rankings, "#293F65"),
            ("Schedule", "Results & upcoming games ›", AppSection.Schedule, "#183E50"),
            ("Teams & staff", "Every school's roster ›", AppSection.Teams, "#443657"),
            ("Injury report", "Your team & the league ›", AppSection.Injuries, "#56353E"),
            ("Headlines", "Stories from your dynasty ›", AppSection.News, "#4C432A")
        };
        var grid = new Grid { ColumnSpacing = 10, RowSpacing = 10,
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) } };
        for (var index = 0; index < links.Length; index++)
        {
            if (index % 2 == 0) grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var link = links[index];
            grid.Add(NavigationCard("SEASON", link.Item1, link.Item2, link.Item4, () =>
            {
                if (link.Item3 == AppSection.Rankings) OpenRankings(0); else ShowSection(link.Item3);
            }), index % 2, index / 2);
        }
        _seasonHub.Children.Add(grid);
        var college = new Button { Text = "Awards, polls & program center ›", BackgroundColor = DynastyTheme.Palette("#645025") };
        college.Clicked += async (_, _) => await ShowCollegeCenterAsync();
        _seasonHub.Children.Add(college);
        var careers = new FlexLayout { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap };
        foreach (var item in new[] { ("Player departures", 5), ("Hall of Fame", 6) })
        {
            var open = new Button { Text = item.Item1 + " ›", FontSize = 12, Margin = new Thickness(3) };
            open.Clicked += async (_, _) => await ShowCollegeCenterAsync(item.Item2); careers.Children.Add(open);
        }
        _seasonHub.Children.Add(careers);
    }

    private View BuildMatchupHub() => _matchupHub;

    private void RenderOverviewGameActions(ScheduledGame? game)
    {
        if (_currentDynasty is null || game is null || !GameDayEngine.IsAvailable(game, _currentDynasty.SeasonYear, _currentDynasty.Week, _currentDynasty.Phase))
        {
            _overviewGameActions.Content = null;
            _overviewGameActions.IsVisible = false;
            return;
        }
        var title = new Label { Text = $"WEEK {game.Week} GAME DAY · {game.AwayTeamName} at {game.HomeTeamName}",
            FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = DynastyTheme.Palette("#E8C979") };
        var simulate = GameModeButton("Sim game", DynastyTheme.Accent, async () => await OpenGameDayAsync(game, GameDayMode.Simulate));
        simulate.TextColor = DynastyTheme.Background;
        var watch = GameModeButton("Watch game", DynastyTheme.Palette("#315D59"), async () => await OpenGameDayAsync(game, GameDayMode.Watch));
        var play = GameModeButton("Play game", DynastyTheme.Palette("#8A6234"), async () => await OpenGameDayAsync(game, GameDayMode.Play));
        _overviewGameActions.Content = new Border { StrokeThickness = 0, Padding = 10,
            BackgroundColor = DynastyTheme.Palette("#172733"), Content = new VerticalStackLayout { Spacing = 8, Children =
            {
                title, new Label { Text = GameOutlookLabel(game), FontSize = 11, TextColor = DynastyTheme.Muted }, new FlexLayout { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, Children = { simulate, watch, play } }
            } } };
        _overviewGameActions.IsVisible = true;
    }

    private void RenderMatchupHub()
    {
        _matchupHub.Children.Clear();
        if (_currentDynasty is null) return;
        var unplayed = _currentDynasty.Schedule.Where(g => g.SeasonYear == _currentDynasty.SeasonYear && !g.HasPlayed)
            .OrderBy(g => g.Week).ThenBy(g => g.AwayTeamName).ToArray();
        if (unplayed.Length == 0) { _matchupHub.Children.Add(new Label { Text = "No upcoming game is scheduled." }); return; }
        var week = _currentDynasty.Week;
        var games = unplayed.Where(g => GameDayEngine.IsAvailable(g, _currentDynasty.SeasonYear, week, _currentDynasty.Phase))
            .OrderByDescending(g => g.InvolvesTeam(_currentDynasty.UserTeamName)).ThenBy(g => g.AwayTeamName).ToArray();
        var userGame = games.FirstOrDefault(g => g.InvolvesTeam(_currentDynasty.UserTeamName));
        if (games.Length == 0)
        {
            _matchupHub.Children.Add(new Label { Text = $"No games to play in Week {week}. Advance the week to reach the next matchup.", TextColor = DynastyTheme.Muted });
            return;
        }
        _matchupHub.Children.Add(SectionTitle($"Week {week} Game Day", "Sim, watch, or take control of any league matchup"));
        if (userGame is not null)
        {
            _matchupHub.Children.Add(SchoolBanner(userGame.AwayTeamName, "Your matchup · away"));
            _matchupHub.Children.Add(SchoolBanner(userGame.HomeTeamName, "Your matchup · home"));
        }
        var schedule = new Button { Text = "View schedule & results" };
        schedule.Clicked += (_, _) => ShowSection(AppSection.Schedule);
        var prep = new Button { Text = "Prepare game plan", BackgroundColor = DynastyTheme.Palette("#315D59") };
        prep.Clicked += (_, _) => ShowSection(AppSection.Playbook);
        if (userGame is not null) _matchupHub.Children.Add(prep);
        _matchupHub.Children.Add(schedule);
        _matchupHub.Children.Add(new Label { Text = "Completed games are saved immediately. Advance the week afterward to process any matchups you did not open.", FontSize = 12, TextColor = DynastyTheme.Muted });
        foreach (var game in games) _matchupHub.Children.Add(MatchupGameCard(game));
    }

    private View MatchupGameCard(ScheduledGame game)
    {
        var userGame = _currentDynasty?.UserTeamName is { } user && game.InvolvesTeam(user);
        var matchup = new Label { Text = $"{game.AwayTeamName}  at  {game.HomeTeamName}",
            FontSize = 15, FontAttributes = FontAttributes.Bold };
        var note = new Label { Text = userGame ? "YOUR GAME" : "CPU vs CPU · choose either school in Play mode",
            FontSize = 10, TextColor = userGame ? DynastyTheme.Palette("#E8C979") : DynastyTheme.Muted };
        var simulate = GameModeButton("Sim", DynastyTheme.Accent, async () => await OpenGameDayAsync(game, GameDayMode.Simulate));
        simulate.TextColor = DynastyTheme.Background;
        var watch = GameModeButton("Watch", DynastyTheme.Palette("#315D59"), async () => await OpenGameDayAsync(game, GameDayMode.Watch));
        var play = GameModeButton("Play", DynastyTheme.Palette("#8A6234"), async () => await OpenPlayModeAsync(game));
        return new Border { StrokeThickness = 0, Padding = 11, BackgroundColor = DynastyTheme.Palette("#1B2B37"),
            Content = new VerticalStackLayout { Spacing = 7, Children =
            {
                matchup, note, new Label { Text = GameOutlookLabel(game), FontSize = 11, TextColor = DynastyTheme.Muted }, new FlexLayout { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, Children = { simulate, watch, play } }
            } } };
    }

    private async Task OpenPlayModeAsync(ScheduledGame game)
    {
        if (_currentDynasty is null) return;
        var controlled = game.InvolvesTeam(_currentDynasty.UserTeamName) ? _currentDynasty.UserTeamName :
            await DisplayActionSheetAsync("Choose team to control", "Cancel", null, game.AwayTeamName, game.HomeTeamName);
        if (string.IsNullOrWhiteSpace(controlled) || controlled == "Cancel") return;
        await OpenGameDayAsync(game, GameDayMode.Play, controlled);
    }

    private static Button GameModeButton(string text, Color color, Func<Task> open)
    {
        var button = new Button { Text = text, BackgroundColor = color, Padding = new Thickness(12, 8) };
        button.Clicked += async (_, _) =>
        {
            button.IsEnabled = false;
            try { await open(); }
            finally { button.IsEnabled = true; }
        };
        return button;
    }

    private View BuildNewsSection() => _newsRows;

    private View NewsCard(DynastyNewsStory story)
    {
        var card = new Border { BackgroundColor = DynastyTheme.Palette("#253642"), Stroke = DynastyTheme.Palette("#527185"), Content =
            new VerticalStackLayout { Spacing = 5, Children =
            {
                new Label { Text = $"{story.Category.ToUpperInvariant()} · {story.SeasonYear} · Week {story.Week}", FontSize = 10, TextColor = DynastyTheme.Palette("#E8C979") },
                new Label { Text = story.Headline, FontSize = 14, FontAttributes = FontAttributes.Bold },
                new Label { Text = "Read story ›", FontSize = 11, TextColor = DynastyTheme.Accent }
            } } };
        var open = new TapGestureRecognizer(); open.Tapped += async (_, _) =>
        {
            var close = new Button { Text = "‹ Back to headlines" };
            var page = new ContentPage(); close.Clicked += async (_, _) => await Navigation.PopModalAsync();
            var destination = new Button { Text = story.Category == "Injuries" ? "Open injury report" : story.Category == "Recruiting" ? "Open recruiting" : "Open schedule" };
            destination.Clicked += async (_, _) =>
            {
                await Navigation.PopModalAsync();
                ShowSection(story.Category == "Injuries" ? AppSection.Injuries : story.Category == "Recruiting" ? AppSection.Recruiting : AppSection.Schedule);
            };
            page.Content = new ScrollView { Content = new VerticalStackLayout { Padding = 14, Spacing = 14, Children =
            {
                close, SchoolBanner(story.TeamName, $"{story.Category} · {story.SeasonYear} · Week {story.Week}"),
                new Label { Text = story.Headline, FontSize = 18, FontAttributes = FontAttributes.Bold },
                new Label { Text = story.Body, FontSize = 14 }, destination
            } } };
            await Navigation.PushModalAsync(page);
        };
        card.GestureRecognizers.Add(open);
        return card;
    }

    private void RenderNews()
    {
        _newsRows.Children.Clear();
        if (_currentDynasty is null) return;
        _newsRows.Children.Add(SectionTitle("News & headlines", "Generated from recorded results, commitments and current injury reports"));
        var stories = DynastyNewsService.Build(_currentDynasty);
        foreach (var story in stories) _newsRows.Children.Add(NewsCard(story));
        if (stories.Count == 0) _newsRows.Children.Add(new Label { Text = "No stories yet. Check back as your season develops." });
    }

    private View BuildInjurySection()
    {
        _injuryScope.SelectedIndexChanged += (_, _) => RenderInjuryReport();
        return new VerticalStackLayout { Spacing = 10, Children =
        {
            SectionTitle("Injury report", "Open a player to inspect their profile and current status"), _injuryScope, _injuryRows
        } };
    }

    private void RenderInjuryReport()
    {
        _injuryRows.Children.Clear();
        if (_currentDynasty is null) return;
        var injuries = _currentDynasty.ActiveRoster.Where(p => p.CurrentInjury is not null &&
            (_injuryScope.SelectedIndex == 1 || p.TeamName == _currentDynasty.UserTeamName)).OrderBy(p => p.TeamName).ThenBy(p => p.FullName).ToArray();
        _injuryRows.Children.Add(new Label { Text = $"{injuries.Length} active injuries", TextColor = DynastyTheme.Palette("#E2ABBC") });
        if (injuries.Length == 0) { _injuryRows.Children.Add(new Label { Text = "No current injuries in this selection." }); return; }
        _injuryRows.Children.Add(DataTable([("Player", 175), ("School", 195), ("Pos", 45), ("Severity", 85), ("Area", 90), ("Weeks", 60)],
            injuries.Select(player => new View[] { PlayerLink(player), SchoolCell(player.TeamName), TableText(player.Position.ToString()),
                TableText(player.CurrentInjury!.Severity.ToString(), color: DynastyTheme.Palette("#E5A1B7")), TableText(player.CurrentInjury.BodyArea.ToString()), TableText(player.CurrentInjury.WeeksRemaining.ToString()) })));
    }
}
