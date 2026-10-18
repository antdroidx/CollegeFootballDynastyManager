using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private readonly Button _settingsButton = new() { Text = "Settings", FontSize = 11, BackgroundColor = Colors.Transparent, Padding = new Thickness(7, 5) };
    private readonly ContentView _overviewBanner = new();
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
            _overviewBanner, new HorizontalStackLayout { Spacing = 8, Children = { _advanceWeekButton, _manualSaveButton } }, _homeHighlights
        } };
    }

    private static View NavigationCard(string title, string value, string hint, string color, Action open)
    {
        var card = new Border { BackgroundColor = Color.FromArgb(color), Stroke = Color.FromArgb("#497088"), Padding = 12,
            Content = new VerticalStackLayout { Spacing = 5, Children =
            {
                new Label { Text = title, FontSize = 11, TextColor = Color.FromArgb("#B8D8E6") },
                new Label { Text = value, FontSize = 17, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                new Label { Text = hint, FontSize = 11, TextColor = Color.FromArgb("#BDE7E1") }
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
        _homeHighlights.Children.Add(cards);
        if (state.UserCoachIsFired)
            _homeHighlights.Children.Add(NavigationCard("ACTION REQUIRED", "Review coaching offers", "Open Program & staff ›", "#643238", () => ShowSection(AppSection.Program)));
        else if (state.Phase is SeasonPhase.TransferPortal or SeasonPhase.Recruiting or SeasonPhase.RosterManagement)
            _homeHighlights.Children.Add(NavigationCard("OFFSEASON", PhaseLabel(state.Phase), "Review your roster and recruiting ›", "#4C432A",
                () => ShowSection(state.Phase == SeasonPhase.RosterManagement ? AppSection.Roster : AppSection.Recruiting)));
        var all = new Button { Text = "News & headlines ›", TextColor = Color.FromArgb("#E8C979"), BackgroundColor = Colors.Transparent };
        all.Clicked += (_, _) => ShowSection(AppSection.News);
        _homeHighlights.Children.Add(all);
        var stories = DynastyNewsService.BuildOverview(state);
        foreach (var story in stories) _homeHighlights.Children.Add(NewsCard(story));
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
    }

    private View BuildMatchupHub() => _matchupHub;

    private void RenderMatchupHub()
    {
        _matchupHub.Children.Clear();
        if (_currentDynasty is null) return;
        var game = _currentDynasty.Schedule.Where(g => g.SeasonYear == _currentDynasty.SeasonYear && !g.HasPlayed && g.InvolvesTeam(_currentDynasty.UserTeamName)).OrderBy(g => g.Week).FirstOrDefault();
        if (game is null) { _matchupHub.Children.Add(new Label { Text = "No upcoming game is scheduled." }); return; }
        _matchupHub.Children.Add(SectionTitle($"Week {game.Week} matchup", $"{game.AwayTeamName} at {game.HomeTeamName}"));
        _matchupHub.Children.Add(SchoolBanner(game.AwayTeamName, "Away team"));
        _matchupHub.Children.Add(SchoolBanner(game.HomeTeamName, "Home team"));
        var schedule = new Button { Text = "View schedule & results" };
        schedule.Clicked += (_, _) => ShowSection(AppSection.Schedule);
        var prep = new Button { Text = "Prepare game plan", BackgroundColor = Color.FromArgb("#315D59") };
        prep.Clicked += (_, _) => ShowSection(AppSection.Playbook);
        var simulate = new Button { Text = "Sim game", BackgroundColor = DynastyTheme.Accent, TextColor = DynastyTheme.Background };
        simulate.Clicked += async (_, _) => await OpenGameDayAsync(game, GameDayMode.Simulate);
        var watch = new Button { Text = "Watch game", BackgroundColor = Color.FromArgb("#315D59") };
        watch.Clicked += async (_, _) => await OpenGameDayAsync(game, GameDayMode.Watch);
        var play = new Button { Text = "Play game", BackgroundColor = Color.FromArgb("#8A6234") };
        play.Clicked += async (_, _) => await OpenGameDayAsync(game, GameDayMode.Play);
        _matchupHub.Children.Add(prep); _matchupHub.Children.Add(schedule);
        _matchupHub.Children.Add(new Label { Text = "All three modes use the same play, personnel, fatigue and coaching engine. Advance the week afterward to process the remaining league games.", FontSize = 12, TextColor = DynastyTheme.Muted });
        _matchupHub.Children.Add(new HorizontalStackLayout { Spacing = 8, Children = { simulate, watch, play } });
    }

    private View BuildNewsSection() => _newsRows;

    private View NewsCard(DynastyNewsStory story)
    {
        var card = new Border { BackgroundColor = Color.FromArgb("#253642"), Stroke = Color.FromArgb("#527185"), Content =
            new VerticalStackLayout { Spacing = 5, Children =
            {
                new Label { Text = $"{story.Category.ToUpperInvariant()} · {story.SeasonYear} · Week {story.Week}", FontSize = 10, TextColor = Color.FromArgb("#E8C979") },
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
        _injuryRows.Children.Add(new Label { Text = $"{injuries.Length} active injuries", TextColor = Color.FromArgb("#E2ABBC") });
        if (injuries.Length == 0) { _injuryRows.Children.Add(new Label { Text = "No current injuries in this selection." }); return; }
        _injuryRows.Children.Add(DataTable([("Player", 175), ("School", 195), ("Pos", 45), ("Severity", 85), ("Area", 90), ("Weeks", 60)],
            injuries.Select(player => new View[] { PlayerLink(player), SchoolCell(player.TeamName), TableText(player.Position.ToString()),
                TableText(player.CurrentInjury!.Severity.ToString(), color: Color.FromArgb("#E5A1B7")), TableText(player.CurrentInjury.BodyArea.ToString()), TableText(player.CurrentInjury.WeeksRemaining.ToString()) })));
    }
}
