using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Branding;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private bool _highlightShowCanceled;
    private readonly Picker _pollKind = new() { Title = "Poll", ItemsSource = new[] { "Media poll", "Coaches poll", "Computer poll" }, SelectedIndex = 0 };

    private string GameOutlookLabel(ScheduledGame game)
    {
        var profiles = BuildCurrentSimulationProfiles();
        profiles.TryGetValue(game.HomeTeamName, out var home); profiles.TryGetValue(game.AwayTeamName, out var away);
        if (game.IsFcsGame)
        {
            TeamSimulationProfile External(string name) => new() { TeamName = name, PassOffenseRating = 60, RushOffenseRating = 60, PassDefenseRating = 60, RushDefenseRating = 60, SpecialTeamsRating = 60 };
            home ??= External(game.HomeTeamName); away ??= External(game.AwayTeamName);
        }
        var outlook = GameOutlookService.Build(game, home, away);
        return $"Projected line · {outlook.Label} · Home {outlook.HomeWinProbability:P0}";
    }

    private View GameDetailCell(ScheduledGame game)
    {
        if (!game.HasPlayed)
        {
            var available = _currentDynasty is not null && GameDayEngine.IsAvailable(game,
                _currentDynasty.SeasonYear, _currentDynasty.Week, _currentDynasty.Phase);
            var open = new Button { Text = available ? "Watch / Play ›" : "Preview ›", FontSize = 11,
                BackgroundColor = Colors.Transparent, TextColor = DynastyTheme.Accent,
                AutomationId = game.InvolvesTeam(_currentDynasty!.UserTeamName) ? "schedule-user-game" : "schedule-league-game" };
            open.Clicked += async (_, _) => await ShowScheduledMatchupAsync(game);
            return open;
        }
        var button = new Button { Text = "Box score & highlights", FontSize = 11, BackgroundColor = Colors.Transparent, TextColor = DynastyTheme.Accent };
        button.Clicked += async (_, _) => await ShowRecordedGameAsync(game);
        return button;
    }

    private async Task ShowScheduledMatchupAsync(ScheduledGame game)
    {
        if (_currentDynasty is null) return;
        var page = new ContentPage { Title = "Game center", BackgroundColor = DynastyTheme.Background };
        var body = new VerticalStackLayout { Padding = 16, Spacing = 12 };
        var close = new Button { Text = "‹ Back to schedule" };
        close.Clicked += async (_, _) => await Navigation.PopModalAsync();
        body.Children.Add(close);
        body.Children.Add(SectionTitle("Game center", FormatScheduleWeek(game.Week)));
        body.Children.Add(SchoolBanner(game.AwayTeamName, "Away"));
        body.Children.Add(SchoolBanner(game.HomeTeamName, "Home"));
        body.Children.Add(new Label { Text = GameOutlookLabel(game), TextColor = DynastyTheme.Muted });
        if (GameDayEngine.IsAvailable(game, _currentDynasty.SeasonYear, _currentDynasty.Week, _currentDynasty.Phase))
        {
            async Task Open(GameDayMode mode)
            {
                await Navigation.PopModalAsync();
                if (mode == GameDayMode.Play) await OpenPlayModeAsync(game);
                else await OpenGameDayAsync(game, mode);
            }
            body.Children.Add(new ActionRow { Direction = Microsoft.Maui.Layouts.FlexDirection.Row,
                Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, Children = {
                    GameModeButton("Watch game", DynastyTheme.Palette("#315D59"), () => Open(GameDayMode.Watch)),
                    GameModeButton("Play game", DynastyTheme.Palette("#8A6234"), () => Open(GameDayMode.Play)) } });
        }
        else body.Children.Add(new Label { Text = "Watch and Play become available when this week begins.", TextColor = DynastyTheme.Muted });
        page.Content = new ScrollView { Content = body };
        await Navigation.PushModalAsync(page);
    }

    private View BuildLeagueFeatureSettings()
    {
        var panel = new VerticalStackLayout { Spacing = 8 };
        var updating = false;
        var switches = new List<(Switch Toggle, Func<LeagueRules,bool> Read)>();
        void RefreshOptions()
        {
            updating = true;
            foreach (var item in switches) item.Toggle.IsToggled = _currentDynasty is not null && item.Read(_currentDynasty.LeagueRules);
            updating = false;
        }
        panel.Children.Add(SectionTitle("Dynasty options", "League changes take effect before the next season's schedule is generated."));
        void Option(string title, Func<LeagueRules, bool> read, Func<LeagueRules, bool, LeagueRules> write)
        {
            var toggle = new Switch();
            switches.Add((toggle,read));
            toggle.Loaded += (_, _) => RefreshOptions();
            toggle.Toggled += (_, e) =>
            {
                if (!updating && _currentDynasty is not null)
                {
                    _currentDynasty = _currentDynasty with { LeagueRules = write(_currentDynasty.LeagueRules, e.Value) };
                    RefreshOptions();
                }
            };
            panel.Children.Add(new HorizontalStackLayout { Spacing = 8, Children = { toggle,
                new Label { Text = title, FontSize = 12, VerticalTextAlignment = TextAlignment.Center } } });
        }
        Option("Program discipline & rare sanctions", r => r.ProgramDiscipline, (r,v) => r with { ProgramDiscipline = v });
        Option("Periodic conference realignment", r => r.ConferenceRealignment, (r,v) => r with { ConferenceRealignment = v });
        Option("FCS nonconference opponents (maximum one per team)", r => r.FcsOpponents, (r,v) => r with { FcsOpponents = v, FcsPromotion = v && r.FcsPromotion });
        Option("Occasional FCS promotion (requires FCS opponents)", r => r.FcsPromotion, (r,v) => r with { FcsPromotion = v, FcsOpponents = v || r.FcsOpponents });
        return panel;
    }

    private async Task ShowCollegeCenterAsync(int initialSection = 0)
    {
        if (_currentDynasty is null) return;
        var page = new ContentPage { BackgroundColor = DynastyTheme.Background, Title = "College football center" };
        var close = new Button { Text = "Return to season", HorizontalOptions = LayoutOptions.Start };
        close.Clicked += async (_, _) => await Navigation.PopModalAsync();
        var section = new Picker { Title = "Section", ItemsSource = new[] { "Awards & honors", "Polls & trends", "Program conduct", "Weekly recap", "Conference landscape", "Player departures", "Hall of Fame" }, SelectedIndex = initialSection };
        var body = new VerticalStackLayout { Spacing = 12 };
        void Render()
        {
            body.Children.Clear();
            if (_currentDynasty is null) return;
            switch (section.SelectedIndex)
            {
                case 0: RenderHonors(body); break;
                case 1: RenderPollTrends(body); break;
                case 2: RenderConduct(body); break;
                case 3: RenderWeeklyRecap(body); break;
                case 4: RenderConferenceLandscape(body); break;
                case 5: RenderPlayerDepartures(body); break;
                case 6: RenderHallOfFame(body); break;
            }
        }
        section.SelectedIndexChanged += (_, _) => Render();
        Render();
        close.FontSize = section.FontSize = 12;
        var heading = new Grid { ColumnSpacing = 10, ColumnDefinitions = new() { new(GridLength.Auto), new(GridLength.Star) } };
        heading.Add(close,0,0); heading.Add(section,1,0);
        var layout = new Grid { Padding = 12, RowDefinitions = new() { new(GridLength.Auto), new(GridLength.Star) } };
        layout.Add(heading,0,0); layout.Add(new ScrollView { Content = body },0,1);
        page.Content = layout;
        await Navigation.PushModalAsync(page);
    }

    private void RenderPlayerDepartures(VerticalStackLayout body)
    {
        var state = _currentDynasty!;
        body.Children.Add(SectionTitle("Player departures", "Graduations, early pro declarations and portal entries · retained across seasons"));
        body.Children.Add(DraftClassExportButton());
        var records = state.PlayerDepartureHistory.Concat(state.RecentPlayerDepartures).DistinctBy(p => (p.SeasonYear, p.PlayerId, p.Reason)).ToArray();
        var teams = new[] { "All teams" }.Concat(state.LeagueTeams.Select(t => t.Name).Order()).ToArray();
        var team = new Picker { Title = "Team", ItemsSource = teams, SelectedIndex = Math.Max(0, Array.IndexOf(teams, state.UserTeamName)) };
        var years = records.Select(p => p.SeasonYear).Append(state.SeasonYear).Distinct().OrderDescending().ToArray();
        var year = new Picker { Title = "Season", ItemsSource = years, SelectedIndex = 0 };
        var filters = new Grid { ColumnDefinitions = new() { new(GridLength.Star), new(90) }, ColumnSpacing = 8 };
        filters.Add(team, 0, 0); filters.Add(year, 1, 0); body.Children.Add(filters);
        var list = new VerticalStackLayout { Spacing = 6 }; body.Children.Add(list);
        void Render()
        {
            list.Children.Clear();
            var selected = records.Where(p => p.SeasonYear == (int)year.SelectedItem && (team.SelectedIndex == 0 || p.TeamName == (string)team.SelectedItem)).OrderBy(p => p.Reason).ThenByDescending(p => p.OverallRating).ToArray();
            list.Children.Add(new Label { Text = $"{selected.Length} departures · elite juniors may declare early" });
            foreach (var p in selected)
            {
                list.Children.Add(PlayerLink(p.PlayerId, p.FullName));
                list.Children.Add(new Label { Text = $"{p.TeamName} · {p.Position.DisplayName()} · OVR {p.OverallRating} · {p.Reason switch { PlayerDepartureReason.EarlyProDeclaration => "Declared for pro draft", PlayerDepartureReason.TransferPortal => "Entered transfer portal", PlayerDepartureReason.Released => "Released from roster", _ => "Graduated" }}", FontSize = 11, TextColor = DynastyTheme.Muted });
            }
        }
        team.SelectedIndexChanged += (_, _) => Render(); year.SelectedIndexChanged += (_, _) => Render(); Render();
    }

    private void RenderHallOfFame(VerticalStackLayout body)
    {
        var state = _currentDynasty!;
        body.Children.Add(SectionTitle("Hall of Fame", "Inducted on graduation or early pro departure · original career / honors scoring, with recorded stats only"));
        var teams = new[] { "National" }.Concat(state.HallOfFame.Select(p => p.TeamName).Distinct().Order()).ToArray();
        var scope = new Picker { Title = "Scope", ItemsSource = teams, SelectedIndex = 0 }; body.Children.Add(scope);
        var list = new VerticalStackLayout { Spacing = 8 }; body.Children.Add(list);
        void Render()
        {
            list.Children.Clear();
            var entries = state.HallOfFame.Where(p => scope.SelectedIndex == 0 || p.TeamName == (string)scope.SelectedItem).OrderByDescending(p => p.CareerScore + p.HonorsScore * 10).ToArray();
            if (entries.Length == 0) list.Children.Add(new Label { Text = "No inductees yet. Exceptional completed careers and final honors earn induction." });
            foreach (var p in entries)
                list.Children.Add(new Border { StrokeThickness = 0, Padding = 10, BackgroundColor = DynastyTheme.Palette("#203342"), Content =
                    new VerticalStackLayout { Spacing = 4, Children = { PlayerLink(p.PlayerId, p.PlayerName),
                        new Label { Text = $"{p.InductionYear} · {p.TeamName} · {p.Position.DisplayName()}", TextColor = DynastyTheme.Accent },
                        new Label { Text = p.Citation, FontSize = 11, TextColor = DynastyTheme.Muted } } } });
        }
        scope.SelectedIndexChanged += (_, _) => Render(); Render();
    }

    private void RenderHonors(VerticalStackLayout body)
    {
        var state = _currentDynasty!;
        body.Children.Add(SectionTitle("Trophy room", "National awards and two teams of national / conference selections. Final honors remain in player profiles."));
        var year = new Picker { Title = "Season", ItemsSource = state.PlayerAwardHistory.Select(a => a.SeasonYear).Append(state.SeasonYear).Distinct().OrderDescending().ToArray(), SelectedIndex = 0 };
        var stage = new Picker { Title = "Selections", ItemsSource = new[] { "Season awards / current race", "Preseason honors" }, SelectedIndex = state.Phase == SeasonPhase.Preseason ? 1 : 0 };
        var conferences = new[] { "National" }.Concat(state.LeagueTeams.Select(t => t.ConferenceName).Distinct().Order()).ToArray();
        var scope = new Picker { Title = "Scope", ItemsSource = conferences, SelectedIndex = 0 };
        var filters = new Grid { ColumnSpacing = 8, ColumnDefinitions = new() { new(70), new(GridLength.Star), new(GridLength.Star) } };
        year.FontSize = stage.FontSize = scope.FontSize = 12;
        filters.Add(year,0,0); filters.Add(stage,1,0); filters.Add(scope,2,0); body.Children.Add(filters);
        var list = new VerticalStackLayout { Spacing = 10 }; body.Children.Add(list);
        void Render()
        {
            list.Children.Clear();
            var selectedYear = year.SelectedItem is int y ? y : state.SeasonYear;
            var preseason = stage.SelectedIndex == 1;
            var records = state.PlayerAwardHistory.Where(a => a.SeasonYear == selectedYear && a.IsPreseason == preseason).ToArray();
            if (records.Length == 0 && selectedYear == state.SeasonYear)
            {
                records = SeasonAwardsService.Build(state, preseason).ToArray();
                list.Children.Add(new Label { Text = preseason ? "Preseason projection" : "Provisional race · Final awards are recorded after the national championship.", FontSize = 11, TextColor = DynastyTheme.Accent });
            }
            var conference = scope.SelectedIndex <= 0 ? null : (string?)scope.SelectedItem;
            foreach (var a in records.Where(a => a.ConferenceName == conference).OrderBy(a => a.IsAllAmerican).ThenBy(a => a.SelectionTeam).ThenBy(a => a.Position))
            {
                var name = new Button { Text = $"{a.Position.DisplayName()} {a.PlayerName} · {a.TeamName}", BackgroundColor = Colors.Transparent, FontSize = 12, HorizontalOptions = LayoutOptions.Start };
                name.Clicked += async (_, _) => await ShowPlayerProfileAsync(a.PlayerId);
                var details = new VerticalStackLayout { Spacing = 3, Children =
                {
                    new Label { Text = a.AwardName, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = DynastyTheme.Palette("#E8C979") },
                    name, new Label { Text = a.Summary, FontSize = 11, TextColor = DynastyTheme.Muted }
                } };
                if (!a.IsAllAmerican && a.ConferenceName is null)
                {
                    var row = new Grid { ColumnSpacing = 12, ColumnDefinitions = new() { new(64), new(GridLength.Star) } };
                    row.Add(new GraphicsView { Drawable = new RetroTrophyDrawable(a.AwardName == "Heisman Trophy"), HeightRequest = 80, WidthRequest = 64 },0,0); row.Add(details,1,0);
                    list.Children.Add(new Border { Padding = 10, StrokeThickness = 0, BackgroundColor = DynastyTheme.Palette("#302C24"), Content = row });
                }
                else list.Children.Add(new Border { Padding = 10, StrokeThickness = 0, BackgroundColor = DynastyTheme.Palette("#1B2B37"), Content = details });
            }
            if (list.Children.Count == 0) list.Children.Add(new Label { Text = "No selections recorded for this season and scope." });
        }
        year.SelectedIndexChanged += (_, _) => Render(); stage.SelectedIndexChanged += (_, _) => Render(); scope.SelectedIndexChanged += (_, _) => Render(); Render();
    }

    private void RenderPollTrends(VerticalStackLayout body)
    {
        var state = _currentDynasty!;
        body.Children.Add(SectionTitle("Poll tracker", "Polls update after week advancement. Media / Coaches blend roster, history and conference expectations with growing results. Computer uses game results."));
        var selection = new Picker { ItemsSource = new[] { "Media", "Coaches", "Computer" }, SelectedIndex = 0 };
        var school = new Picker { Title = "Team", ItemsSource = state.LeagueTeams.Select(t => t.Name).Order().ToArray(), SelectedItem = state.UserTeamName };
        body.Children.Add(selection); body.Children.Add(school);
        var graph = new ContentView(); var table = new ContentView(); body.Children.Add(graph); body.Children.Add(table);
        void Render()
        {
            var kind = (PollKind)Math.Max(0, selection.SelectedIndex);
            var name = school.SelectedItem as string ?? state.UserTeamName;
            var history = state.PollHistory.Where(p => p.SeasonYear == state.SeasonYear && p.Kind == kind)
                .Select(p => (Label: p.Phase == SeasonPhase.Preseason ? "Pre" : p.Week.ToString(), Value: (double)(p.Rankings.FirstOrDefault(r => r.TeamName == name)?.Rank ?? state.LeagueTeams.Count))).ToArray();
            graph.Content = TrendChart(history, $"{name} · rank by week (lower is better)", true);
            var ranks = CollegePollService.GetPublished(state, _teamsByName, kind, BuildCurrentSimulationProfiles());
            table.Content = DataTable([("Rank",45),("School",195),("W",35),("L",35),(kind == PollKind.Computer ? "Index" : "Votes",70)],
                ranks.Take(25).Select(r => new View[] { TableText(r.Rank.ToString(),true), SchoolCell(r.TeamName, false), TableText(r.Wins.ToString()), TableText(r.Losses.ToString()), TableText(r.Score.ToString("F1")) }));
        }
        selection.SelectedIndexChanged += (_, _) => Render(); school.SelectedIndexChanged += (_, _) => Render(); Render();
    }

    private void RenderConduct(VerticalStackLayout body)
    {
        var state = _currentDynasty!;
        body.Children.Add(SectionTitle("Program standards", "High conduct is healthy. Player discipline and staff leadership affect incidents and recovery. Sanctions require repeated major failures and are rare."));
        if (!state.LeagueRules.ProgramDiscipline) body.Children.Add(new Label { Text = "Program discipline is disabled in Settings.", TextColor = DynastyTheme.Accent });
        var school = new Picker { ItemsSource = state.LeagueTeams.Select(t => t.Name).Order().ToArray(), SelectedItem = state.UserTeamName };
        body.Children.Add(school); var list = new VerticalStackLayout { Spacing = 10 }; body.Children.Add(list);
        void Render()
        {
            list.Children.Clear(); var team = (string?)school.SelectedItem ?? state.UserTeamName;
            var record = state.ProgramConduct.FirstOrDefault(p => p.TeamName == team);
            var score = record?.Score ?? 80;
            list.Children.Add(new Label { Text = $"{team} · Conduct {score}/100 · {(score >= 70 ? "Healthy" : score >= 40 ? "Needs attention" : "Under scrutiny")}", FontSize = 17, FontAttributes = FontAttributes.Bold });
            list.Children.Add(new ProgressBar { Progress = score / 100.0, ProgressColor = score >= 70 ? DynastyTheme.Palette("#72D4B2") : DynastyTheme.Palette("#F4BC68"), HeightRequest = 12 });
            if (!ProgramDisciplineService.PostseasonEligible(state, team)) list.Children.Add(new Label { Text = "Postseason ban this season", TextColor = DynastyTheme.Palette("#F4A6A6") });
            if (team == state.UserTeamName && state.LeagueRules.ProgramDiscipline)
            {
                var improve = new Button { Text = "Strengthen oversight (+8, once per season)", IsEnabled = !state.DisciplineHistory.Any(i => i.TeamName == team && i.SeasonYear == state.SeasonYear && i.EventId == "oversight") };
                improve.Clicked += (_, _) => { _currentDynasty = ProgramDisciplineService.ImproveOversight(_currentDynasty!, team); state = _currentDynasty; Render(); }; list.Children.Add(improve);
            }
            foreach (var incident in state.DisciplineHistory.Where(i => i.TeamName == team).Reverse().Take(30))
                list.Children.Add(new Border { StrokeThickness = 0, Padding = 10, BackgroundColor = DynastyTheme.Palette("#1B2B37"), Content = new VerticalStackLayout { Spacing = 4, Children =
                {
                    new Label { Text = $"{incident.SeasonYear} · W{incident.Week} · {incident.Title}", FontAttributes = FontAttributes.Bold, FontSize = 13 },
                    new Label { Text = incident.Description, FontSize = 12, TextColor = DynastyTheme.Muted }
                } } });
            list.Children.Add(new Label { Text = "Program events and decisions", FontSize = 10, TextColor = DynastyTheme.Muted });
        }
        school.SelectedIndexChanged += (_, _) => Render(); Render();
    }

    private void RenderWeeklyRecap(VerticalStackLayout body)
    {
        var state = _currentDynasty!;
        body.Children.Add(SectionTitle("Saturday rewind", "Upsets, games of the week and recorded big plays. Replays use the live viewer and never alter results."));
        var weeks = state.Schedule.Where(g => g.SeasonYear == state.SeasonYear && g.HasPlayed).Select(g => g.Week).Distinct().OrderDescending().ToArray();
        if (weeks.Length == 0) { body.Children.Add(new Label { Text = "Play or simulate a week to unlock the recap." }); return; }
        var week = new Picker { Title = "Week", ItemsSource = weeks, SelectedIndex = 0 }; body.Children.Add(week);
        var list = new VerticalStackLayout { Spacing = 10 }; body.Children.Add(list);
        void Render()
        {
            list.Children.Clear(); var games = GameHighlightService.WeeklyGames(state, (int)week.SelectedItem);
            var show = new Button { Text = "Watch weekly highlight show", BackgroundColor = DynastyTheme.Palette("#6A4B2B") };
            show.Clicked += async (_, _) =>
            {
                show.IsEnabled = false;
                try { await ShowWeeklyHighlightShowAsync(games.Where(g => g.Highlights.Count > 0).Take(5).ToArray()); }
                finally { show.IsEnabled = true; }
            }; list.Children.Add(show);
            list.Children.Add(TrendChart(games.Take(8).Select(g => ((g.WinnerTeamName ?? g.HomeTeamName), (double)(g.HomeScore + g.AwayScore ?? 0))).ToArray(), "Featured games · combined points"));
            foreach (var g in games)
            {
                var open = new Button { Text = $"{(GameHighlightService.IsUpset(g) ? "UPSET · " : "")}{g.AwayTeamName} {g.AwayScore}–{g.HomeScore} {g.HomeTeamName} ›", FontSize = 12, BackgroundColor = DynastyTheme.Palette("#1D3446") };
                open.Clicked += async (_, _) => await ShowRecordedGameAsync(g); list.Children.Add(open);
            }
        }
        week.SelectedIndexChanged += (_, _) => Render(); Render();
    }

    private void RenderConferenceLandscape(VerticalStackLayout body)
    {
        var state = _currentDynasty!;
        body.Children.Add(SectionTitle("Conference landscape", $"Realignment {(state.LeagueRules.ConferenceRealignment ? "on" : "off")} · FCS promotion {(state.LeagueRules.FcsPromotion ? "on" : "off")} · Change options in Settings"));
        body.Children.Add(TrendChart(LeagueEvolutionService.ConferencePower(state).OrderByDescending(p => p.Value).Select(p => (p.Key, p.Value)).ToArray(), "Conference power · average current program prestige"));
        foreach (var move in state.LeagueMovementHistory.Reverse().Take(30)) body.Children.Add(new Label { Text = $"{move.SeasonYear} · {move.TeamName}: {move.FromConference} → {move.ToConference}{(move.Promotion ? " · Promoted to FBS" : "")}", FontSize = 12 });
        body.Children.Add(SectionTitle("FCS opponent library", $"{FcsLeagueService.Available(state.LeagueTeams).Count} external schools · schools already in this league are excluded"));
        body.Children.Add(new Label { Text = string.Join(" · ", FcsLeagueService.Available(state.LeagueTeams).Select(t => t.Name)), FontSize = 11, TextColor = DynastyTheme.Muted });
    }

    private async Task ShowRecordedGameAsync(ScheduledGame game)
    {
        var body = new VerticalStackLayout { Spacing = 10, Padding = 14 };
        var page = new ContentPage { BackgroundColor = DynastyTheme.Background, Content = new ScrollView { Content = body } };
        var close = new Button { Text = "Back" }; close.Clicked += async (_, _) => await Navigation.PopModalAsync(); body.Children.Add(close);
        body.Children.Add(SectionTitle($"{game.AwayTeamName} {game.AwayScore}–{game.HomeScore} {game.HomeTeamName}", $"{game.SeasonYear} · Week {game.Week} · Final"));
        var box = new Button { Text = "Full box score" }; box.Clicked += async (_, _) => await ShowLiveBoxScoreAsync(new GameDayBoxScore
        {
            HomeTeamName = game.HomeTeamName, AwayTeamName = game.AwayTeamName, HomeScore = game.HomeScore ?? 0, AwayScore = game.AwayScore ?? 0,
            IsComplete = true, Quarter = Math.Max(4, game.PeriodScores.Select(p => p.Period).DefaultIfEmpty(4).Max()), HomeStats = game.HomeStats ?? new(), AwayStats = game.AwayStats ?? new(),
            Weather = game.Weather, HomeMorale = game.HomeEndingMorale, AwayMorale = game.AwayEndingMorale,
            PlayerOfGame = game.PlayerOfGame ?? PlayerOfGameService.Select(game, game.PlayerStats),
            Players = game.PlayerStats, Periods = game.PeriodScores
        }); body.Children.Add(box);
        var replay = new Button { Text = "Watch game highlights", IsEnabled = game.Highlights.Count > 0 };
        replay.Clicked += async (_, _) => await OpenGameDayAsync(game, GameDayMode.Watch, replay: game.Highlights); body.Children.Add(replay);
        if (game.Highlights.Count == 0) body.Children.Add(new Label { Text = "This result predates recorded highlights." });
        foreach (var snap in game.Highlights)
        {
            var clip = new Button { Text = $"Q{snap.Quarter} {snap.ClockSeconds/60}:{snap.ClockSeconds%60:D2} · {snap.Description ?? snap.Outcome.ToString()}", FontSize = 12 };
            clip.Clicked += async (_, _) => await OpenGameDayAsync(game, GameDayMode.Watch, replay: new[] { snap }); body.Children.Add(clip);
        }
        await Navigation.PushModalAsync(page);
    }

    private async Task ShowWeeklyHighlightShowAsync(IReadOnlyList<ScheduledGame> games)
    {
        _highlightShowCanceled = false;
        foreach (var game in games)
        {
            if (_highlightShowCanceled) break;
            await OpenGameDayAsync(game, GameDayMode.Watch, replay: game.Highlights, autoReplay: true);
        }

    }

    private static View TrendChart((string Label, double Value)[] points, string caption, bool ranks = false) => new VerticalStackLayout { Spacing = 6, Children =
    {
        new Label { Text = caption, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = DynastyTheme.Accent },
        new GraphicsView { Drawable = new SeasonChartDrawable(points, ranks), HeightRequest = 185, BackgroundColor = DynastyTheme.Palette("#142530") },
        new Label { Text = points.Length == 0 ? "Data appears as the season progresses." : string.Join(" · ", points.Select(p => $"{p.Label}: {p.Value:F0}")), FontSize = 10, TextColor = DynastyTheme.Muted }
    } };

    private sealed class SeasonChartDrawable((string Label, double Value)[] points, bool ranks) : IDrawable
    {
        public void Draw(ICanvas c, RectF rect)
        {
            if (points.Length == 0) return;
            var max = Math.Max(1, points.Max(p => p.Value)); var width = Math.Max(1, rect.Width - 32); var height = rect.Height - 35;
            c.StrokeColor = DynastyTheme.Palette("#385365"); c.StrokeSize = 1;
            for (var i = 0; i < 4; i++) c.DrawLine(16,15 + height*i/3,rect.Width-16,15+height*i/3);
            c.FontColor = Colors.White; c.FontSize = 9;
            for (var i = 0; i < points.Length; i++)
            {
                var x = 16 + width * (i + .5f) / points.Length; var normalized = (float)(points[i].Value / max);
                var y = 15 + height * (ranks ? normalized : 1-normalized);
                c.FillColor = Color.FromArgb(i % 2 == 0 ? "#6CD2BC" : "#E6BF72");
                if (ranks)
                {
                    c.FillCircle(x,y,4);
                    if (i > 0) { c.StrokeColor = DynastyTheme.Palette("#6CD2BC"); c.StrokeSize = 2; c.DrawLine(16+width*(i-.5f)/points.Length,15+height*(float)(points[i-1].Value/max),x,y); }
                }
                else c.FillRoundedRectangle(x-width/points.Length*.3f,y,width/points.Length*.6f,Math.Max(2,height*(1-(y-15)/height)),3);
                if (points.Length <= 15) c.DrawString(points[i].Value.ToString("F0"),x-15,Math.Max(0,y-15),30,15,HorizontalAlignment.Center,VerticalAlignment.Center);
            }
        }
    }

    private sealed class RetroTrophyDrawable(bool football) : IDrawable
    {
        public void Draw(ICanvas c, RectF r)
        {
            // Original pixel-style trophy; no external images or fonts required.
            var unit = Math.Min(r.Width/16,r.Height/20);
            void Pixel(int x,int y,int w,int h,string color) { c.FillColor = Color.FromArgb(color); c.FillRectangle(x*unit,y*unit,w*unit,h*unit); }
            if (football)
            {
                Pixel(7,1,3,3,"#FFE6A3"); Pixel(6,4,4,6,"#E7B34F"); Pixel(3,4,3,2,"#E7B34F"); Pixel(1,3,2,2,"#FFE6A3");
                Pixel(10,5,3,2,"#E7B34F"); Pixel(12,6,3,2,"#BA7932"); Pixel(5,10,3,2,"#E7B34F"); Pixel(3,12,3,3,"#E7B34F");
                Pixel(9,10,2,5,"#E7B34F"); Pixel(9,15,4,2,"#FFE6A3"); Pixel(2,17,12,3,"#794F35"); Pixel(3,18,10,1,"#D5A657");
                return;
            }
            Pixel(3,2,10,2,"#FFE6A3"); Pixel(4,4,8,5,"#E7B34F"); Pixel(5,9,6,2,"#BA7932"); Pixel(7,11,2,4,"#E7B34F");
            Pixel(4,15,8,2,"#FFE6A3"); Pixel(2,17,12,3,"#794F35"); Pixel(3,18,10,1,"#D5A657");
            Pixel(1,4,3,1,"#E7B34F"); Pixel(1,5,1,4,"#E7B34F"); Pixel(2,8,3,1,"#E7B34F");
            Pixel(12,4,3,1,"#E7B34F"); Pixel(14,5,1,4,"#E7B34F"); Pixel(11,8,3,1,"#E7B34F"); Pixel(5,4,1,4,"#FFE6A3");
        }
    }
}
