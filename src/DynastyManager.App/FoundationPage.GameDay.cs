using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Persistence;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private async Task OpenGameDayAsync(ScheduledGame game, GameDayMode mode)
    {
        if (_currentDynasty is null || game.HasPlayed) return;
        var profiles = BuildCurrentSimulationProfiles();
        if (!profiles.TryGetValue(game.HomeTeamName, out var home) || !profiles.TryGetValue(game.AwayTeamName, out var away)) return;
        home = GamePlanService.Attach(_currentDynasty, _teamsByName[game.HomeTeamName], home);
        away = GamePlanService.Attach(_currentDynasty, _teamsByName[game.AwayTeamName], away);
        var session = GameDayEngine.Start(game, home, away, _currentDynasty.ActiveRoster, mode);

        if (mode == GameDayMode.Simulate)
        {
            GameDayEngine.SimulateToEnd(session);
            await CompleteGameDayAsync(session);
            return;
        }

        var page = new ContentPage { BackgroundColor = DynastyTheme.Background };
        var score = new Label { FontSize = 22, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center };
        var situation = new Label { HorizontalTextAlignment = TextAlignment.Center, TextColor = DynastyTheme.Muted };
        var result = new Label { HorizontalTextAlignment = TextAlignment.Center, FontSize = 14 };
        var field = new Grid { HeightRequest = 260, BackgroundColor = Color.FromArgb("#245B38"), Padding = 12 };
        var offenseMarkers = new FlexLayout { Direction = FlexDirection.Row, JustifyContent = FlexJustify.SpaceAround, AlignItems = FlexAlignItems.Center };
        var defenseMarkers = new FlexLayout { Direction = FlexDirection.Row, JustifyContent = FlexJustify.SpaceAround, AlignItems = FlexAlignItems.Center };
        field.RowDefinitions.Add(new RowDefinition(GridLength.Star)); field.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        field.Add(defenseMarkers, 0, 0); field.Add(offenseMarkers, 0, 1);
        var playPicker = new Picker { Title = "Call offensive play", ItemsSource = PlaybookCatalog.OffensivePlays.Select(play =>
            $"{play.Name} · {GamePlanService.Label(play.Formation)} · {GamePlanService.Label(play.Concept)}").ToList(), SelectedIndex = 0 };
        var call = new Button { Text = "Call play", BackgroundColor = DynastyTheme.Accent, TextColor = DynastyTheme.Background };
        var watch = new Button { Text = "Switch to Watch" };
        var skip = new Button { Text = "Skip to end" };
        var close = new Button { Text = "Close", IsVisible = false };
        var controls = new HorizontalStackLayout { Spacing = 8, Children = { call, watch, skip, close } };
        page.Content = new ScrollView { Content = new VerticalStackLayout { Padding = 14, Spacing = 12, Children =
        {
            score, situation, field, result, playPicker, controls
        } } };

        void Markers(FlexLayout host, IReadOnlyList<Guid> ids, Color color)
        {
            host.Children.Clear();
            foreach (var id in ids.Take(11))
            {
                var player = session.Roster.FirstOrDefault(item => item.PlayerId == id);
                host.Children.Add(new Border { WidthRequest = 25, HeightRequest = 25, StrokeShape = new RoundRectangle { CornerRadius = 13 },
                    BackgroundColor = color, Stroke = Colors.White, Content = new Label { Text = player?.Position.ToString() ?? "•", FontSize = 8,
                        HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center } });
            }
        }
        void Refresh(GameSnapEvent? snap = null)
        {
            score.Text = $"{game.AwayTeamName} {session.AwayScore}  ·  {session.HomeScore} {game.HomeTeamName}";
            situation.Text = $"Q{session.Quarter} {TimeSpan.FromSeconds(session.ClockSeconds):m\\:ss} · {session.PossessionTeamName} · {Ordinal(session.Down)} & {session.Distance} · ball on {session.YardLine}";
            if (snap is not null)
            {
                var play = PlaybookCatalog.OffensivePlays.First(item => item.Id == snap.OffensivePlayId);
                result.Text = $"{play.Name}: {snap.Outcome} · {(snap.Yards >= 0 ? "+" : string.Empty)}{snap.Yards} yards";
                Markers(offenseMarkers, snap.OffensePersonnel, Color.FromArgb("#B3263B"));
                Markers(defenseMarkers, snap.DefensePersonnel, Color.FromArgb("#2457A7"));
            }
            var userOnOffense = session.PossessionTeamName.Equals(_currentDynasty.UserTeamName, StringComparison.OrdinalIgnoreCase);
            call.IsVisible = session.Mode == GameDayMode.Play && userOnOffense && !session.IsComplete;
            playPicker.IsVisible = call.IsVisible;
            watch.IsVisible = session.Mode == GameDayMode.Play && !session.IsComplete;
            skip.IsVisible = !session.IsComplete;
            close.IsVisible = session.IsComplete;
        }
        async Task FinishIfNeeded()
        {
            if (!session.IsComplete) return;
            await CompleteGameDayAsync(session);
            result.Text = "Final · result saved";
            Refresh();
        }
        async Task WatchLoop()
        {
            while (!session.IsComplete && session.Mode == GameDayMode.Watch && page.Parent is not null)
            {
                var snap = GameDayEngine.Step(session); Refresh(snap);
                await Task.Delay(350);
            }
            await FinishIfNeeded();
        }
        async Task RunOpponentPossession()
        {
            while (!session.IsComplete && session.Mode == GameDayMode.Play &&
                   !session.PossessionTeamName.Equals(_currentDynasty.UserTeamName, StringComparison.OrdinalIgnoreCase))
            {
                var snap = GameDayEngine.Step(session); Refresh(snap); await Task.Delay(180);
            }
            await FinishIfNeeded();
        }
        call.Clicked += async (_, _) =>
        {
            var selected = PlaybookCatalog.OffensivePlays[Math.Max(0, playPicker.SelectedIndex)];
            Refresh(GameDayEngine.Step(session, selected.Id)); await RunOpponentPossession();
        };
        watch.Clicked += async (_, _) => { session.Mode = GameDayMode.Watch; Refresh(); await WatchLoop(); };
        skip.Clicked += async (_, _) => { GameDayEngine.SimulateToEnd(session); await FinishIfNeeded(); };
        close.Clicked += async (_, _) => await Navigation.PopModalAsync();
        Refresh();
        await Navigation.PushModalAsync(page);
        if (mode == GameDayMode.Watch) await WatchLoop(); else await RunOpponentPossession();
    }

    private async Task CompleteGameDayAsync(GameDaySession session)
    {
        if (_currentDynasty is null) return;
        var completed = GameDayEngine.Finish(session);
        _currentDynasty = _currentDynasty with { Schedule = _currentDynasty.Schedule.Select(game =>
            game.GameId == completed.GameId ? completed : game).ToArray() };
        _cachedCurrentProfiles = null;
        await SaveCurrentAsync(SaveKind.AutosaveCurrent);
        RenderCurrentDynasty();
    }

    private static string Ordinal(int down) => down switch { 1 => "1st", 2 => "2nd", 3 => "3rd", _ => "4th" };
}
