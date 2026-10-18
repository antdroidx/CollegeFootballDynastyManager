using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Branding;
using DynastyManager.Data.Persistence;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private async Task OpenGameDayAsync(ScheduledGame game, GameDayMode mode, string? controlledTeamName = null)
    {
        if (_currentDynasty is null || game.HasPlayed) return;
        controlledTeamName ??= game.InvolvesTeam(_currentDynasty.UserTeamName)
            ? _currentDynasty.UserTeamName : game.HomeTeamName;
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
        var possession = new Label { FontSize = 11, HorizontalTextAlignment = TextAlignment.Center, TextColor = Color.FromArgb("#E8C979") };
        var result = new Label { HorizontalTextAlignment = TextAlignment.Center, FontSize = 14 };
        var fieldArt = new GameFieldDrawable();
        var field = new Grid { HeightRequest = 330, BackgroundColor = Color.FromArgb("#245B38") };
        var fieldLines = new GraphicsView { Drawable = fieldArt };
        var fieldPlayers = new AbsoluteLayout();
        field.Add(fieldLines); field.Add(fieldPlayers);

        var availablePlays = PlaybookCatalog.OffensivePlays.ToArray();
        var selectedPlay = availablePlays.First();
        var selectedArt = new GraphicsView { HeightRequest = 112, WidthRequest = 185, BackgroundColor = Color.FromArgb("#102A24") };
        var selectedName = new Label { FontAttributes = FontAttributes.Bold, FontSize = 15 };
        var selectedDetails = new Label { FontSize = 11, TextColor = Color.FromArgb("#B8D8FA") };
        var selectedDescription = new Label { FontSize = 11, TextColor = DynastyTheme.Muted };
        var choose = new Button { Text = "Choose play", BackgroundColor = Color.FromArgb("#315D59") };
        var playInfo = new VerticalStackLayout { Spacing = 4, Children = { selectedName, selectedDetails, selectedDescription, choose } };
        var playGrid = new Grid { ColumnSpacing = 12,
            ColumnDefinitions = new ColumnDefinitionCollection { new(new GridLength(185)), new(GridLength.Star) } };
        playGrid.Add(selectedArt, 0, 0); playGrid.Add(playInfo, 1, 0);
        var playCard = new Border { StrokeThickness = 0, Padding = 10,
            BackgroundColor = Color.FromArgb("#1B2B37"), Content = playGrid };
        void SelectPlay(OffensivePlayDefinition play)
        {
            selectedPlay = play;
            selectedArt.Drawable = new PlayArtDrawable(play.Id, true, play.Concept.ToString());
            selectedName.Text = play.Name;
            selectedDetails.Text = $"{GamePlanService.Label(play.Formation)} · {GamePlanService.Label(play.Personnel)} · {GamePlanService.Label(play.Concept)}";
            selectedDescription.Text = play.Description;
        }
        SelectPlay(selectedPlay);

        var call = new Button { Text = "Call play", BackgroundColor = DynastyTheme.Accent, TextColor = DynastyTheme.Background };
        var watch = new Button { Text = "Switch to Watch" };
        var skip = new Button { Text = "Skip to end" };
        var close = new Button { Text = "Close", IsVisible = false };
        var controls = new HorizontalStackLayout { Spacing = 8, Children = { call, watch, skip, close } };
        page.Content = new ScrollView { Content = new VerticalStackLayout { Padding = 14, Spacing = 10, Children =
        {
            score, situation, possession, field, result, playCard, controls
        } } };

        Color TeamColor(string teamName) => Color.FromArgb(TeamBrandCatalog.Get(teamName).Primary);
        Border Marker(DynastyPlayer? player, Color color)
        {
            var marker = new Border { WidthRequest = 27, HeightRequest = 27,
                StrokeShape = new RoundRectangle { CornerRadius = 14 }, BackgroundColor = color,
                Stroke = Colors.White, StrokeThickness = 1.4,
                Content = new Label { Text = player?.Position.ToString() ?? "•", FontSize = 8, FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center } };
            if (player is not null) SemanticProperties.SetDescription(marker, $"{player.Position} {player.FullName}");
            return marker;
        }
        async Task AnimateSnapAsync(GameSnapEvent snap)
        {
            fieldPlayers.AbortAnimation("snap-movement");
            fieldPlayers.Children.Clear();
            fieldArt.YardLine = snap.StartYardLine; fieldArt.Distance = snap.Distance; fieldLines.Invalidate();
            var offenseColor = TeamColor(snap.OffenseTeamName);
            var defenseColor = TeamColor(snap.DefenseTeamName);
            var line = Math.Clamp(snap.StartYardLine / 100d, .06, .94);
            var destination = Math.Clamp((snap.StartYardLine + snap.Yards) / 100d, .04, .96);
            var movements = new List<(View View, Rect From, Rect To)>();
            // Use the personnel's football roles, not their arbitrary list order.
            // Coordinates are proportional to the field; the line of scrimmage is X.
            var offense = snap.OffensePersonnel.Select(id =>
            {
                session.PlayersById.TryGetValue(id, out var player);
                return player;
            }).ToArray();
            var defense = snap.DefensePersonnel.Select(id =>
            {
                session.PlayersById.TryGetValue(id, out var player);
                return player;
            }).ToArray();
            var lineSlots = new[] { .36, .43, .50, .57, .64 };
            var receiverSlots = new[] { .10, .23, .77, .90, .84, .16 };
            var lineIndex = 0;
            var receiverIndex = 0;
            var backIndex = 0;
            for (var index = 0; index < offense.Length; index++)
            {
                var player = offense[index];
                double x, y;
                switch (player?.Position)
                {
                    case Position.OL:
                        x = line - .016;
                        y = lineSlots[Math.Min(lineIndex++, lineSlots.Length - 1)];
                        break;
                    case Position.QB:
                        x = line - .065;
                        y = .50;
                        break;
                    case Position.RB:
                        x = line - .095 - backIndex * .023;
                        y = backIndex++ % 2 == 0 ? .59 : .41;
                        break;
                    case Position.WR:
                        x = line - .018;
                        y = receiverSlots[Math.Min(receiverIndex++, receiverSlots.Length - 1)];
                        break;
                    case Position.TE:
                        x = line - .022;
                        y = receiverIndex++ % 2 == 0 ? .69 : .31;
                        break;
                    default:
                        x = line - .04 - (index % 3) * .02;
                        y = .20 + (index % 7) * .10;
                        break;
                }
                x = Math.Clamp(x, .025, .96);
                var endY = Math.Clamp(y + RouteLane(player, index, snap) * .35, .07, .93);
                var endX = Math.Clamp(x + Math.Max(.012, destination - line) * RouteShare(player, snap), .025, .96);
                var view = Marker(player, offenseColor);
                AddMovement(view, new Rect(x, y, 27, 27), new Rect(endX, endY, 27, 27), movements, fieldPlayers);
            }
            var defensiveLine = 0;
            var linebackers = 0;
            var secondary = 0;
            for (var index = 0; index < defense.Length; index++)
            {
                var player = defense[index];
                double x, y;
                switch (player?.Position)
                {
                    case Position.DL:
                        x = line + .025;
                        y = .34 + (defensiveLine++ % 5) * .08;
                        break;
                    case Position.LB:
                        x = line + .075;
                        y = .28 + (linebackers++ % 5) * .11;
                        break;
                    case Position.CB:
                    case Position.S:
                        x = line + (player.Position == Position.S ? .19 : .12);
                        y = secondary++ % 2 == 0 ? .15 + (secondary / 2) * .09 : .85 - (secondary / 2) * .09;
                        break;
                    default:
                        x = line + .10;
                        y = .20 + (index % 7) * .10;
                        break;
                }
                x = Math.Clamp(x, .025, .96);
                var endX = Math.Clamp(x + (destination - line) * .72, .025, .96);
                var endY = Math.Clamp(y + ((index % 3) - 1) * .025, .07, .93);
                var view = Marker(player, defenseColor);
                AddMovement(view, new Rect(x, y, 27, 27), new Rect(endX, endY, 27, 27), movements, fieldPlayers);
            }
            var ball = new Border { WidthRequest = 14, HeightRequest = 9, BackgroundColor = Color.FromArgb("#8B4D24"),
                Stroke = Color.FromArgb("#F5D8B5"), StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 5 } };
            AddMovement(ball, new Rect(Math.Clamp(line - .025, .03, .96), .72, 14, 9),
                new Rect(destination, snap.Outcome == SnapOutcome.Incompletion ? .26 : .48, 14, 9), movements, fieldPlayers);
            var completion = new TaskCompletionSource();
            var animation = new Animation(progress =>
            {
                foreach (var movement in movements)
                    AbsoluteLayout.SetLayoutBounds(movement.View, Interpolate(movement.From, movement.To, progress));
            });
            animation.Commit(fieldPlayers, "snap-movement", 16, session.Mode == GameDayMode.Watch ? 620u : 480u,
                Easing.CubicInOut, (_, _) => completion.TrySetResult());
            await completion.Task;
        }
        void Refresh(GameSnapEvent? snap = null)
        {
            score.Text = $"{game.AwayTeamName} {session.AwayScore}  ·  {session.HomeScore} {game.HomeTeamName}";
            situation.Text = $"Q{session.Quarter} {TimeSpan.FromSeconds(session.ClockSeconds):m\\:ss} · {Ordinal(session.Down)} & {session.Distance} · ball on {session.YardLine}";
            possession.Text = session.Mode == GameDayMode.Watch
                ? $"CPU vs CPU · {session.PossessionTeamName} possession"
                : $"Controlling {controlledTeamName} · {session.PossessionTeamName} possession";
            if (snap is not null)
            {
                var play = PlaybookCatalog.OffensivePlays.First(item => item.Id == snap.OffensivePlayId);
                result.Text = $"{play.Name}: {snap.Outcome} · {(snap.Yards >= 0 ? "+" : string.Empty)}{snap.Yards} yards";
            }
            var controlledOnOffense = session.PossessionTeamName.Equals(controlledTeamName, StringComparison.OrdinalIgnoreCase);
            call.IsVisible = session.Mode == GameDayMode.Play && controlledOnOffense && !session.IsComplete;
            playCard.IsVisible = call.IsVisible;
            watch.IsVisible = session.Mode == GameDayMode.Play && !session.IsComplete;
            skip.IsVisible = !session.IsComplete;
            close.IsVisible = session.IsComplete;
        }
        async Task ShowSnapAsync(GameSnapEvent snap)
        {
            await AnimateSnapAsync(snap); Refresh(snap);
            await Task.Delay(session.Mode == GameDayMode.Watch ? 180 : 80);
        }
        async Task FinishIfNeeded()
        {
            if (!session.IsComplete) return;
            await CompleteGameDayAsync(session); result.Text = "Final · result saved"; Refresh();
        }
        async Task WatchLoop()
        {
            while (!session.IsComplete && session.Mode == GameDayMode.Watch && page.Parent is not null)
                await ShowSnapAsync(GameDayEngine.Step(session));
            await FinishIfNeeded();
        }
        async Task RunOpponentPossession()
        {
            while (!session.IsComplete && session.Mode == GameDayMode.Play &&
                   !session.PossessionTeamName.Equals(controlledTeamName, StringComparison.OrdinalIgnoreCase))
                await ShowSnapAsync(GameDayEngine.Step(session));
            await FinishIfNeeded();
        }
        choose.Clicked += async (_, _) =>
        {
            var choice = await ShowPlayCallSheetAsync(availablePlays, selectedPlay);
            if (choice is not null) SelectPlay(choice);
        };
        call.Clicked += async (_, _) =>
        {
            call.IsEnabled = false;
            try { await ShowSnapAsync(GameDayEngine.Step(session, selectedPlay.Id)); await RunOpponentPossession(); }
            finally { call.IsEnabled = true; }
        };
        watch.Clicked += async (_, _) => { session.Mode = GameDayMode.Watch; Refresh(); await WatchLoop(); };
        skip.Clicked += async (_, _) => { GameDayEngine.SimulateToEnd(session); await FinishIfNeeded(); };
        close.Clicked += async (_, _) => await Navigation.PopModalAsync();
        Refresh();
        await Navigation.PushModalAsync(page);
        if (mode == GameDayMode.Watch) await WatchLoop(); else await RunOpponentPossession();
    }

    private async Task<OffensivePlayDefinition?> ShowPlayCallSheetAsync(
        IReadOnlyList<OffensivePlayDefinition> plays, OffensivePlayDefinition selected)
    {
        var completion = new TaskCompletionSource<OffensivePlayDefinition?>();
        var modal = new ContentPage { Title = "Call offensive play", BackgroundColor = DynastyTheme.Background };
        var grid = new Grid { ColumnSpacing = 10, RowSpacing = 10,
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) } };
        for (var index = 0; index < plays.Count; index++)
        {
            if (index % 2 == 0) grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var play = plays[index];
            var art = new GraphicsView { Drawable = new PlayArtDrawable(play.Id, true, play.Concept.ToString()),
                HeightRequest = 105, BackgroundColor = Color.FromArgb("#102A24") };
            var card = new Border { StrokeThickness = play.Id == selected.Id ? 2 : 0,
                Stroke = DynastyTheme.Accent, Padding = 9, BackgroundColor = Color.FromArgb("#1B2B37"), Content =
                new VerticalStackLayout { Spacing = 4, Children =
                {
                    art, new Label { Text = play.Name, FontAttributes = FontAttributes.Bold, FontSize = 13 },
                    new Label { Text = $"{GamePlanService.Label(play.Formation)} · {GamePlanService.Label(play.Concept)}", FontSize = 10, TextColor = Color.FromArgb("#B8D8FA") },
                    new Label { Text = play.Description, FontSize = 10, TextColor = DynastyTheme.Muted, MaxLines = 2 }
                } } };
            var tap = new TapGestureRecognizer();
            tap.Tapped += async (_, _) => { completion.TrySetResult(play); await Navigation.PopModalAsync(); };
            card.GestureRecognizers.Add(tap);
            grid.Add(card, index % 2, index / 2);
        }
        var cancel = new Button { Text = "Cancel" };
        cancel.Clicked += async (_, _) => { completion.TrySetResult(null); await Navigation.PopModalAsync(); };
        modal.Disappearing += (_, _) => completion.TrySetResult(null);
        var header = new Grid { Padding = 12,
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) } };
        header.Add(new Label { Text = "Call offensive play", FontSize = 20, FontAttributes = FontAttributes.Bold,
            VerticalTextAlignment = TextAlignment.Center }, 0, 0);
        header.Add(cancel, 1, 0);
        var body = new Grid { RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Star) } };
        body.Add(header, 0, 0); body.Add(new ScrollView { Content = grid, Margin = new Thickness(12, 0, 12, 12) }, 0, 1);
        modal.Content = body;
        await Navigation.PushModalAsync(modal);
        return await completion.Task;
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

    private static void AddMovement(View view, Rect from, Rect to, List<(View View, Rect From, Rect To)> movements,
        AbsoluteLayout field)
    {
        AbsoluteLayout.SetLayoutFlags(view, AbsoluteLayoutFlags.PositionProportional);
        AbsoluteLayout.SetLayoutBounds(view, from); field.Add(view); movements.Add((view, from, to));
    }

    private static Rect Interpolate(Rect start, Rect end, double progress) => new(
        start.X + (end.X - start.X) * progress, start.Y + (end.Y - start.Y) * progress,
        start.Width, start.Height);

    private static double RouteLane(DynastyPlayer? player, int index, GameSnapEvent snap)
    {
        if (player is null) return 0;
        if (player.Position is Position.WR or Position.TE)
            return snap.TargetId == player.PlayerId ? -.38 : -.16 - index % 3 * .04;
        if (player.Position == Position.RB) return snap.BallCarrierId == player.PlayerId ? -.22 : -.08;
        if (player.Position == Position.QB) return -.04;
        return -.015;
    }

    private static double RouteShare(DynastyPlayer? player, GameSnapEvent snap)
    {
        if (player is null) return .2;
        if (snap.TargetId == player.PlayerId || snap.BallCarrierId == player.PlayerId) return 1;
        return player.Position is Position.WR or Position.TE ? .75 : player.Position == Position.QB ? .25 : .35;
    }

    private static string Ordinal(int down) => down switch { 1 => "1st", 2 => "2nd", 3 => "3rd", _ => "4th" };

    private sealed class GameFieldDrawable : IDrawable
    {
        public int YardLine { get; set; } = 25;
        public int Distance { get; set; } = 10;

        public void Draw(ICanvas canvas, RectF rect)
        {
            canvas.FillColor = Color.FromArgb("#245B38"); canvas.FillRectangle(rect);
            canvas.FillColor = Color.FromArgb("#1B4930");
            canvas.FillRectangle(0, 0, rect.Width * .05f, rect.Height);
            canvas.FillRectangle(rect.Width * .95f, 0, rect.Width * .05f, rect.Height);
            canvas.StrokeColor = Color.FromArgb("#A8CBB5"); canvas.StrokeSize = 1;
            for (var yard = 10; yard < 100; yard += 10)
            {
                var x = rect.Width * yard / 100f;
                canvas.DrawLine(x, 0, x, rect.Height);
                canvas.FontColor = Color.FromArgb("#D5E6DB"); canvas.FontSize = 11;
                canvas.DrawString((yard <= 50 ? yard : 100 - yard).ToString(), x - 10, 8, 24, 18,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
            }
            canvas.StrokeColor = Color.FromArgb("#D5E6DB"); canvas.StrokeSize = 1;
            for (var yard = 5; yard < 100; yard += 5)
            {
                var x = rect.Width * yard / 100f;
                canvas.DrawLine(x, rect.Height * .25f, x, rect.Height * .29f);
                canvas.DrawLine(x, rect.Height * .71f, x, rect.Height * .75f);
            }
            var line = rect.Width * YardLine / 100f;
            var gain = rect.Width * Math.Min(100, YardLine + Distance) / 100f;
            canvas.StrokeColor = Color.FromArgb("#4FA8FF"); canvas.StrokeSize = 3; canvas.DrawLine(line, 0, line, rect.Height);
            canvas.StrokeColor = Color.FromArgb("#E7C45B"); canvas.StrokeSize = 3; canvas.DrawLine(gain, 0, gain, rect.Height);
        }
    }
}
