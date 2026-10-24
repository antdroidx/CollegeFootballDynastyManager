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
    private async Task OpenGameDayAsync(ScheduledGame game, GameDayMode mode, string? controlledTeamName = null, IReadOnlyList<GameSnapEvent>? replay = null, bool autoReplay = false)
    {
        if (_currentDynasty is null || replay is null && !GameDayEngine.IsAvailable(game, _currentDynasty.SeasonYear, _currentDynasty.Week, _currentDynasty.Phase)) return;
        controlledTeamName ??= game.InvolvesTeam(_currentDynasty.UserTeamName)
            ? _currentDynasty.UserTeamName : game.HomeTeamName;
        _currentDynasty = PlayerRatingService.EnsureProfiles(_currentDynasty);
        var roster = replay is null ? _currentDynasty.ActiveRoster : GameHighlightService.ReplayRoster(game);
        var homeTeam = _teamsByName.GetValueOrDefault(game.HomeTeamName) ?? FcsLeagueService.Find(game.HomeTeamName);
        var awayTeam = _teamsByName.GetValueOrDefault(game.AwayTeamName) ?? FcsLeagueService.Find(game.AwayTeamName);
        if (homeTeam is null || awayTeam is null) return;
        if (game.IsFcsGame && replay is null)
        {
            var external = _teamsByName.ContainsKey(homeTeam.Name) ? awayTeam : homeTeam;
            roster = roster.Concat(FcsLeagueService.CreateRoster(_currentDynasty, external)).ToArray();
        }
        var profiles = DynastyRosterSimulationProfileBuilder.Build(_currentDynasty with { ActiveRoster = roster }, new[] { homeTeam, awayTeam });
        var home = GamePlanService.Attach(_currentDynasty, homeTeam, profiles[game.HomeTeamName]);
        var away = GamePlanService.Attach(_currentDynasty, awayTeam, profiles[game.AwayTeamName]);
        if (replay is not null) { home = home with { GamePlan = game.HomeGamePlan ?? home.GamePlan }; away = away with { GamePlan = game.AwayGamePlan ?? away.GamePlan }; }
        var session = GameDayEngine.Start(game, home, away, roster, mode, replay is null ? _currentDynasty.DepthChartAssignments : null);
        session.ControlledTeamName = controlledTeamName;
        var gameRanks = BuildCurrentNationalRankings();
        var animating = false;
        var boxScoreOpen = false;
        var driving = false;
        var watchPaused = false;
        var takingOver = false;
        var gameSpeed = Math.Clamp(Microsoft.Maui.Storage.Preferences.Default.Get("live-game-speed", 1d), .5, 2);
        bool? compactLayout = null;
        var completion = new GameDayCompletionService();
        var skipPending = false;
        var finalizing = false;
        var finalPresented = false;
        var viewerClosed = false;
        var closingViewer = false;
        Task currentSnapFinished = Task.CompletedTask;
        GameSnapEvent? lastPlayedSnap = null;
        var instantReplays = Microsoft.Maui.Storage.Preferences.Default.Get("live-instant-replays", true);
        var showCarrierNames = Microsoft.Maui.Storage.Preferences.Default.Get("live-player-labels", true);

        if (mode == GameDayMode.Simulate)
        {
            GameDayEngine.SimulateToEnd(session);
            await CompleteGameDayAsync(session);
            return;
        }

        var page = new GameDayViewerPage { BackgroundColor = DynastyTheme.Background };
        var score = new Label { FontSize = 17, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center, LineBreakMode = LineBreakMode.TailTruncation, MaxLines = 1, AutomationId = "game-day-score" };
        var quarterClock = new Label { FontSize = 14, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center };
        var downValue = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center };
        var toGoValue = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center };
        var ballValue = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center };
        var situation = new Grid { ColumnSpacing = 12, Padding = new Thickness(10, 6), AutomationId = "game-day-situation",
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) } };
        situation.Add(quarterClock, 0, 0); situation.Add(downValue, 1, 0); situation.Add(ballValue, 2, 0);
        quarterClock.FontSize = downValue.FontSize = ballValue.FontSize = 13;
        var awayScoreLabel = new Label { FontSize = 25, FontAttributes = FontAttributes.Bold, VerticalTextAlignment = TextAlignment.Center, TextColor = DynastyTheme.Text };
        var homeScoreLabel = new Label { FontSize = 25, FontAttributes = FontAttributes.Bold, VerticalTextAlignment = TextAlignment.Center, TextColor = DynastyTheme.Text };
        var awayTimeouts = new Label { FontSize = 11, TextColor = DynastyTheme.Muted };
        var homeTimeouts = new Label { FontSize = 11, TextColor = DynastyTheme.Muted };
        var awayName = new Label { FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = DynastyTheme.Text };
        var homeName = new Label { FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = DynastyTheme.Text };
        var possession = new Label { FontSize = 11, HorizontalTextAlignment = TextAlignment.Center, TextColor = DynastyTheme.Palette("#E8C979") };
        var result = new Label { HorizontalTextAlignment = TextAlignment.Center, FontSize = 14,
            FontAttributes = FontAttributes.Bold, TextColor = DynastyTheme.Text, MaxLines = 3 };
        var commentaryPanel = new Border { StrokeThickness = 0, Padding = new Thickness(10, 6),
            BackgroundColor = DynastyTheme.Palette("#203645"), Content = result };
        var conditionsHud = new Label { FontSize = 9, MaxLines = 2, IsVisible = false, TextColor = DynastyTheme.Palette("#B8D8FA"),
            HorizontalTextAlignment = TextAlignment.Center };
        var timeoutsHud = new Label { FontSize = 10, TextColor = DynastyTheme.Palette("#E8C979"),
            HorizontalTextAlignment = TextAlignment.Center, AutomationId = "game-day-timeouts" };
        var homeBrand = TeamBrandCatalog.Get(game.HomeTeamName);
        var fieldArt = new GameFieldDrawable
        {
            HomeColor = Color.FromArgb(homeBrand.Primary),
            HomeSecondaryColor = Color.FromArgb(homeBrand.Secondary),
            HomeName = game.HomeTeamName.ToUpperInvariant(),
            HomeInitial = TeamBrandCatalog.Monogram(game.HomeTeamName),
            Weather = session.Weather
        };
        var field = new Grid { AutomationId = "game-day-field", HeightRequest = 330, BackgroundColor = DynastyTheme.Palette("#245B38") };
        var fieldFrame = new Border { Padding = 5, StrokeThickness = 1, Stroke = DynastyTheme.Muted,
            BackgroundColor = DynastyTheme.Surface, StrokeShape = new RoundRectangle { CornerRadius = 8 }, Content = field,
            AutomationId = "game-day-field-frame" };
        var fieldLines = new GraphicsView { Drawable = fieldArt };
        var fieldPlayers = new AbsoluteLayout();
        field.IsClippedToBounds = true;
        var cameraMode = Math.Clamp(Microsoft.Maui.Storage.Preferences.Default.Get("live-camera", 0), 0, 2);
        void FrameCamera(double x = .5, double y = .5)
        {
            if (field.Width <= 0 || field.Height <= 0) return;
            var zoom = cameraMode == 0 ? 1 : cameraMode == 1 ? 1.15 : 1.35;
            foreach (var layer in new View[] { fieldLines, fieldPlayers })
            {
                layer.Scale = zoom;
                layer.TranslationX = Math.Clamp((.5 - x) * field.Width * zoom, -field.Width * (zoom - 1) / 2, field.Width * (zoom - 1) / 2);
                layer.TranslationY = Math.Clamp((.5 - y) * field.Height * zoom, -field.Height * (zoom - 1) / 2, field.Height * (zoom - 1) / 2);
            }
        }
        field.SizeChanged += (_, _) => FrameCamera();
        var highlightText = new Label { FontSize = 22, FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center };
        var highlight = new Border { IsVisible = false, StrokeThickness = 2, Stroke = Colors.White,
            Padding = new Thickness(22, 12), HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center, Content = highlightText };
        var replayChip = new Label { Text = "INSTANT REPLAY", IsVisible = false, FontSize = 12,
            FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#FFE194"), BackgroundColor = Color.FromArgb("#101F2C"),
            Padding = new Thickness(12, 4), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Start, Margin = 6 };
        field.Add(fieldLines); field.Add(fieldPlayers); field.Add(highlight); field.Add(replayChip);

        var controlledProfile = controlledTeamName.Equals(game.HomeTeamName, StringComparison.OrdinalIgnoreCase) ? home : away;
        var controlledPlan = GamePlanService.Normalize(controlledProfile.GamePlan ?? new TeamGamePlan { TeamName = controlledTeamName });
        var availablePlays = GamePlanService.AvailableOffensivePlays(controlledPlan).ToArray();
        var availableDefenses = GamePlanService.AvailableDefensivePlays(controlledPlan).ToArray();
        var selectedPlay = GamePlanService.RecommendOffensivePlay(controlledPlan, session.Down, session.Distance, session.YardLine);
        var selectedCallId = selectedPlay.Id;
        var call = new Button { Text = "Call play", BackgroundColor = DynastyTheme.Accent, TextColor = DynastyTheme.Background };
        var selectedDefense = GamePlanService.RecommendDefensivePlay(controlledPlan, session.Down, session.Distance);
        var selectedArt = new GraphicsView { HeightRequest = 112, WidthRequest = 185, BackgroundColor = DynastyTheme.Palette("#102A24") };
        var selectedHeading = new Label { Text = "RECOMMENDED OFFENSIVE CALL", FontSize = 9, TextColor = DynastyTheme.Palette("#E8C979") };
        var selectedName = new Label { FontAttributes = FontAttributes.Bold, FontSize = 15 };
        var selectedDetails = new Label { FontSize = 11, TextColor = DynastyTheme.Palette("#B8D8FA") };
        var selectedDescription = new Label { FontSize = 11, TextColor = DynastyTheme.Muted };
        var choose = new Button { Text = "Choose play", BackgroundColor = DynastyTheme.Palette("#315D59") };
        var playInfo = new VerticalStackLayout { Spacing = 4, Children = { selectedHeading, selectedName, selectedDetails, selectedDescription, choose } };
        var playGrid = new Grid { ColumnSpacing = 12,
            ColumnDefinitions = new ColumnDefinitionCollection { new(new GridLength(185)), new(GridLength.Star) } };
        playGrid.Add(selectedArt, 0, 0); playGrid.Add(playInfo, 1, 0);
        var playCard = new Border { StrokeThickness = 0, Padding = 10,
            BackgroundColor = DynastyTheme.Palette("#1B2B37"), Content = playGrid };
        void SelectPlay(OffensivePlayDefinition play, bool recommended = false)
        {
            selectedPlay = play;
            selectedCallId = play.Id;
            call.Text = "Call play";
            selectedArt.IsVisible = true;
            playGrid.ColumnDefinitions[0].Width = new GridLength(selectedArt.WidthRequest);
            selectedHeading.Text = recommended ? "RECOMMENDED OFFENSIVE CALL" : "SELECTED OFFENSIVE CALL";
            selectedArt.Drawable = new PlayArtDrawable(play.Id, play.Name, true, play.Concept.ToString(), play);
            selectedName.Text = play.Name;
            selectedDetails.Text = $"{GamePlanService.Label(play.Formation)} · {GamePlanService.Label(play.Personnel)} · {GamePlanService.Label(play.Concept)}";
            selectedDescription.Text = play.Description;
        }
        SelectPlay(selectedPlay, true);

        void SelectRecommendation(GameDayRecommendation recommendation)
        {
            if (recommendation.Play is not null)
            {
                SelectPlay(recommendation.Play, true);
                selectedDescription.Text = recommendation.Reason + " " + recommendation.Play.Description;
                return;
            }
            selectedCallId = recommendation.PlayId;
            var fieldGoal = recommendation.Type == RecommendedCallType.FieldGoal;
            selectedHeading.Text = "RECOMMENDED SPECIAL TEAMS CALL";
            selectedName.Text = fieldGoal ? "Field goal attempt" : "Punt";
            selectedDetails.Text = fieldGoal ? $"{recommendation.KickDistance}-yard attempt · {recommendation.KickProbability:P0} estimated"
                : $"4th & {session.Distance} · Protect field position";
            selectedDescription.Text = recommendation.Reason;
            selectedArt.IsVisible = false;
            playGrid.ColumnDefinitions[0].Width = new GridLength(0);
            call.Text = fieldGoal ? "Kick field goal" : "Call punt";
        }

        var defenseArt = new GraphicsView { HeightRequest = 112, WidthRequest = 185, BackgroundColor = DynastyTheme.Palette("#102A24") };
        var defenseHeading = new Label { Text = "RECOMMENDED DEFENSIVE CALL", FontSize = 9, TextColor = DynastyTheme.Palette("#E8C979") };
        var defenseName = new Label { FontAttributes = FontAttributes.Bold, FontSize = 15 };
        var defenseDetails = new Label { FontSize = 11, TextColor = DynastyTheme.Palette("#B8D8FA") };
        var defenseDescription = new Label { FontSize = 11, TextColor = DynastyTheme.Muted };
        var chooseDefense = new Button { Text = "Choose defense", BackgroundColor = DynastyTheme.Palette("#315D59") };
        var defenseInfo = new VerticalStackLayout { Spacing = 4, Children =
            { defenseHeading, defenseName, defenseDetails, defenseDescription, chooseDefense } };
        var defenseGrid = new Grid { ColumnSpacing = 12,
            ColumnDefinitions = new ColumnDefinitionCollection { new(new GridLength(185)), new(GridLength.Star) } };
        defenseGrid.Add(defenseArt, 0, 0); defenseGrid.Add(defenseInfo, 1, 0);
        var defenseCard = new Border { StrokeThickness = 0, Padding = 10,
            BackgroundColor = DynastyTheme.Palette("#1B2B37"), Content = defenseGrid, IsVisible = false };
        void SelectDefense(DefensivePlayDefinition play, bool recommended = false)
        {
            selectedDefense = play;
            defenseHeading.Text = recommended ? "RECOMMENDED DEFENSIVE CALL" : "SELECTED DEFENSIVE CALL";
            defenseArt.Drawable = new PlayArtDrawable(play.Id, play.Name, false, play.Approach.ToString(), defensiveDefinition: play);
            defenseName.Text = play.Name;
            defenseDetails.Text = $"{GamePlanService.Label(play.Formation)} · {GamePlanService.Label(play.Personnel)} · {GamePlanService.Label(play.Approach)}";
            defenseDescription.Text = play.Description;
        }
        SelectDefense(selectedDefense, true);

        var callDefense = new Button { Text = "Call defense", BackgroundColor = DynastyTheme.Accent, TextColor = DynastyTheme.Background };
        var delegateOffense = new Button { Text = "Offense: You", BackgroundColor = DynastyTheme.Palette("#315D59") };
        var delegateDefense = new Button { Text = "Defense: You", BackgroundColor = DynastyTheme.Palette("#315D59") };
        var watch = new Button { Text = "Watch / Pause" };
        var takeover = new Button { Text = "Take over", IsVisible = false };
        var punt = new Button { Text = "Punt", IsVisible = false };
        var kick = new Button { Text = "Field goal", IsVisible = false };
        var extraPoint = new Button { Text = "Extra point", IsVisible = false };
        var twoPoint = new Button { Text = "Go for 2", IsVisible = false };
        var kneel = new Button { Text = "Kneel", IsVisible = false };
        var spike = new Button { Text = "Spike", IsVisible = false };
        var onside = new Button { Text = "Next kickoff: Deep" };
        onside.Clicked += (_, _) => { session.OnsideKickRequested = !session.OnsideKickRequested; onside.Text = session.OnsideKickRequested ? "Next kickoff: Onside" : "Next kickoff: Deep"; };
        var timeout = new Button { Text = "Timeout" };
        var replayLast = new Button { Text = "Replay last", IsVisible = false };
        var replaySpeed = Math.Clamp(Microsoft.Maui.Storage.Preferences.Default.Get("live-replay-speed", 1d), .5, 2);
        var skipReplayRequested = false;
        var replaySpeedButton = new Button { Text = "Replay speed: 1x" };
        replaySpeedButton.Clicked += (_, _) => { replaySpeed = replaySpeed == 1 ? .5 : 1; replaySpeedButton.Text = $"Replay speed: {replaySpeed:0.0}x"; };
        var skipReplay = new Button { Text = "Skip replay", IsVisible = false, AutomationId = "game-day-skip-replay" };
        skipReplay.Clicked += (_, _) => { skipReplayRequested = true; fieldPlayers.AbortAnimation("snap-movement"); };
        string CameraName() => cameraMode == 0 ? "Full field" : cameraMode == 1 ? "Broadcast" : "Follow ball";
        var camera = new Button { Text = "Camera: " + CameraName(), AutomationId = "game-day-camera" };
        camera.Clicked += (_, _) => { cameraMode = (cameraMode + 1) % 3; Microsoft.Maui.Storage.Preferences.Default.Set("live-camera", cameraMode); camera.Text = "Camera: " + CameraName(); FrameCamera(); };
        var replayToggle = new Button { Text = instantReplays ? "Auto replay: On" : "Auto replay: Off" };
        var carrierToggle = new Button { Text = showCarrierNames ? "Player names: On" : "Player names: Off" };
        replayToggle.Clicked += (_, _) => { instantReplays = !instantReplays;
            Microsoft.Maui.Storage.Preferences.Default.Set("live-instant-replays", instantReplays);
            replayToggle.Text = instantReplays ? "Auto replay: On" : "Auto replay: Off"; };
        carrierToggle.Clicked += (_, _) => { showCarrierNames = !showCarrierNames;
            Microsoft.Maui.Storage.Preferences.Default.Set("live-player-labels", showCarrierNames);
            carrierToggle.Text = showCarrierNames ? "Player names: On" : "Player names: Off"; };
        var showNumbers = Microsoft.Maui.Storage.Preferences.Default.Get("live-jersey-numbers", false);
        var markerLabels = new Dictionary<Label, DynastyPlayer?>();
        var spritePlayers = new Dictionary<FootballSpriteView, DynastyPlayer?>();
        var activeBallOwners = new HashSet<Guid>();
        var useSprites = Microsoft.Maui.Storage.Preferences.Default.Get("live-football-sprites", true);
        var spriteMode = new Button { Text = useSprites ? "Players: Sprites" : "Players: Circles" };
        var markerHosts = new Dictionary<Border, (DynastyPlayer? Player, Color Primary, Color Secondary, bool Away)>();
        var numbers = new Button { Text = showNumbers ? "Markers: Numbers" : "Markers: Positions" };
        void UpdateMarkerLabels()
        {
            foreach (var (label, player) in markerLabels)
            {
                label.Text = showNumbers && player?.JerseyNumber is int number ? number.ToString() : player?.Position.ToString() ?? "•";
                label.FontSize = showNumbers ? 8 : 6;
            }
            foreach (var (sprite, player) in spritePlayers)
                sprite.SetLabel(showNumbers && player?.JerseyNumber is int jersey ? jersey.ToString() : player?.Position.ToString() ?? "");
            numbers.Text = showNumbers ? "Markers: Numbers" : "Markers: Positions";
        }
        numbers.Clicked += (_, _) => { showNumbers = !showNumbers; Microsoft.Maui.Storage.Preferences.Default.Set("live-jersey-numbers", showNumbers); UpdateMarkerLabels(); };
        var boxScore = new Button { Text = "Box score", AutomationId = "live-box-score", FontSize = 13, FontAttributes = FontAttributes.Bold, BackgroundColor = DynastyTheme.Accent, TextColor = DynastyTheme.Background, MinimumHeightRequest = 42, Padding = new Thickness(14, 8), VerticalOptions = LayoutOptions.Center };
        var gameInfo = new Button { Text = "Weather", FontSize = 11, MinimumHeightRequest = 40,
            BackgroundColor = DynastyTheme.Palette("#172733"), AutomationId = "game-day-conditions-toggle" };
        gameInfo.Clicked += (_, _) =>
        {
            conditionsHud.IsVisible = !conditionsHud.IsVisible;
            gameInfo.Text = conditionsHud.IsVisible ? "Hide info" : "Weather";
        };
        Border TeamBadge(string team)
        {
            var brand = TeamBrandCatalog.Get(team);
            var primary = Color.FromArgb(brand.Primary);
            var ink = primary.Red * .2126 + primary.Green * .7152 + primary.Blue * .0722 > .5 ? Colors.Black : Colors.White;
            var badge = new Border { WidthRequest = 30, HeightRequest = 30, Padding = 2,
                Stroke = Color.FromArgb(brand.Secondary), StrokeThickness = 2, BackgroundColor = primary,
                Content = new Label { Text = TeamBrandCatalog.Monogram(team), TextColor = ink,
                    FontSize = TeamBrandCatalog.Monogram(team).Length >= 4 ? 8 : 10, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center } };
            SemanticProperties.SetDescription(badge, team + " team badge");
            return badge;
        }
        Grid TeamScore(string team, Label name, Label value, Label timeouts)
        {
            var grid = new Grid { Padding = new Thickness(9, 7), ColumnSpacing = 7,
                ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
            grid.Add(TeamBadge(team), 0, 0);
            grid.Add(new VerticalStackLayout { Spacing = 1, Children = { name, timeouts } }, 1, 0);
            grid.Add(value, 2, 0); return grid;
        }
        var scoreHeader = new Grid { BackgroundColor = DynastyTheme.Surface, ColumnSpacing = 1,
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) }, AutomationId = "game-day-score" };
        scoreHeader.Add(TeamScore(game.AwayTeamName, awayName, awayScoreLabel, awayTimeouts), 0, 0);
        scoreHeader.Add(TeamScore(game.HomeTeamName, homeName, homeScoreLabel, homeTimeouts), 1, 0);
        var options = new Button { Text = "Options", AutomationId = "game-day-options" };
        var teamControl = new Button { Text = "Team", AutomationId = "game-day-team" };
        var skip = new Button { Text = "Skip to end" };
        var close = new Button { Text = "Close", IsVisible = false };
        var controls = new ActionRow { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, JustifyContent = FlexJustify.Start };
        foreach (var button in new[] { call, callDefense, punt, kick, extraPoint, twoPoint, kneel, spike, onside, delegateOffense, delegateDefense, watch, takeover, options, teamControl, gameInfo, boxScore, timeout, replayLast, camera, replaySpeedButton, skipReplay, numbers, spriteMode, replayToggle, carrierToggle, skip, close })
        { button.Margin = new Thickness(0, 0, 8, 8); controls.Children.Add(button); }
        var coachCallsOffense = false;
        var coachCallsDefense = false;
        var quickCalls = new Grid { ColumnSpacing = 6,
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) } };
        var expand = new Button { Text = "Choose play" };
        controls.Children.Insert(1, expand);
        var more = new Button { Text = "More", AutomationId = "game-day-more" };
        controls.Children.Add(more);
        var tray = new VerticalStackLayout { Spacing = 4, Children = { quickCalls, controls } };
        var traySurface = new Border { AutomationId = "game-day-controls", StrokeThickness = 0, Padding = 7,
            BackgroundColor = DynastyTheme.Surface, Content = new ScrollView { Content = tray, MaximumHeightRequest = 190 },
            VerticalOptions = LayoutOptions.End, Margin = new Thickness(8) };
        var pickerLayer = new Grid { IsVisible = false, BackgroundColor = DynastyTheme.Palette("#88000000"), Padding = 10 };
        expand.Clicked += (_, _) => ExpandCalls(!ControlledOnOffense());
        var moreOpen = false;
        void ClosePicker() { pickerLayer.IsVisible = false; pickerLayer.Children.Clear(); }
        page.BackHandler = () =>
        {
            if (skipPending || finalizing || closingViewer) return true;
            if (session.IsComplete) { _ = CloseViewerAsync(); return true; }
            if (pickerLayer.IsVisible) { ClosePicker(); return true; }
            if (moreOpen) { moreOpen = false; UpdateOverlay(); return true; }
            if (replay is null) { _ = CloseViewerAsync(); return true; }
            return false;
        };
        void UpdateOverlay()
        {
            if (viewerClosed) return;
            var landscape = compactLayout == true;
            var deciding = !animating && replay is null && !session.IsComplete && session.Mode == GameDayMode.Play && !session.KickoffPending &&
                (session.PossessionTeamName.Equals(controlledTeamName, StringComparison.OrdinalIgnoreCase) ? !coachCallsOffense : !coachCallsDefense);
            quickCalls.IsVisible = deciding && !moreOpen;
            // In portrait the selected card retains its larger, readable layout.
            playCard.IsVisible = defenseCard.IsVisible = false;
            expand.IsVisible = deciding && !session.TryPending;
            expand.Text = ControlledOnOffense() ? "Choose play" : "Choose defense";
            extraPoint.IsVisible = deciding && ControlledOnOffense() && session.TryPending && session.OvertimeRound < 2;
            twoPoint.IsVisible = deciding && ControlledOnOffense() && session.TryPending;
            call.IsVisible = deciding && ControlledOnOffense();
            callDefense.IsVisible = deciding && !ControlledOnOffense();
            replayLast.IsVisible = replay is null && lastPlayedSnap is not null && !animating;
            more.IsVisible = !animating && !session.IsComplete && replay is null;
            options.IsVisible = replay is null; teamControl.IsVisible = replay is null;
            takeover.IsEnabled = !takingOver && !session.IsComplete;
            gameInfo.IsVisible = boxScore.IsVisible = moreOpen && !animating;
            timeout.IsVisible = skip.IsVisible = replayLast.IsVisible = moreOpen && !animating;
            more.Text = moreOpen ? "Less" : "More";
            foreach (var utility in new[] { punt, kick, kneel, spike, onside, delegateOffense, delegateDefense, numbers, spriteMode, replayToggle, carrierToggle, camera, replaySpeedButton })
            {
                var eligible = utility == punt || utility == kick || utility == kneel || utility == spike
                    ? deciding && ControlledOnOffense() && !session.TryPending && (utility != kneel && utility != spike || session.OvertimeRound == 0)
                    : utility == camera || utility == replaySpeedButton || utility == numbers || utility == spriteMode || utility == replayToggle || utility == carrierToggle || session.Mode == GameDayMode.Play && !session.IsComplete;
                utility.IsVisible = eligible && moreOpen && !animating;
            }
            foreach (var action in new[] { call, callDefense, punt, kick, extraPoint, twoPoint, kneel, spike, delegateOffense, delegateDefense, onside, skip })
                action.IsEnabled = !animating;
            controls.IsEnabled = !skipPending && !finalizing && !closingViewer;
            boxScore.IsEnabled = !skipPending && !finalizing && !closingViewer && !boxScoreOpen;
            // Skip remains reachable during Watch animations; its handler waits for the current play.
            skip.IsEnabled = !skipPending && !finalizing && !session.IsComplete;
            skip.Text = skipPending ? "Please wait…" : "Skip to end";
            close.Text = session.IsComplete && !completion.IsSaved && !finalizing ? "Retry save" : "Close";
            close.IsEnabled = !skipPending && !finalizing && !animating && !closingViewer;
            if (animating) { quickCalls.IsVisible = false; playCard.IsVisible = defenseCard.IsVisible = false; moreOpen = false; ClosePicker(); }
        }
        more.Clicked += (_, _) => { moreOpen = !moreOpen; UpdateOverlay(); };
        void UpdateQuickCalls()
        {
            quickCalls.Children.Clear();
            if (replay is not null) return;
            var offense = ControlledOnOffense();
            var heading = offense ? selectedHeading.Text : defenseHeading.Text;
            var badge = heading.StartsWith("RECOMMENDED", StringComparison.Ordinal) ? "COACH PICK" : "SELECTED";
            var type = offense ? selectedCallId.StartsWith("special-", StringComparison.Ordinal) ? "SPECIAL TEAMS"
                : selectedPlay.Concept is OffensiveConcept.InsideRun or OffensiveConcept.OutsideRun ? "RUN" : "PASS"
                : GamePlanService.Label(selectedDefense.Approach);
            var current = GameDayCallPicker.Card(offense ? selectedName.Text : defenseName.Text,
                offense ? selectedDetails.Text : defenseDetails.Text, $"{badge} · {type}",
                offense ? selectedArt.IsVisible ? selectedArt.Drawable : null : defenseArt.Drawable,
                () => { if (!session.TryPending) ExpandCalls(!offense); }, compact: true, portrait: compactLayout != true);
            current.Stroke = DynastyTheme.Palette("#E8C979"); current.StrokeThickness = 1;
            quickCalls.Add(current, 0, 0);
            if (session.TryPending) { Grid.SetColumnSpan(current, 3); return; }
            if (offense)
            {
                var choices = availablePlays.Where(p => p.Id != selectedCallId)
                    .OrderBy(p => p.Concept == selectedPlay.Concept ? 1 : 0).Take(2).ToArray();
                for (var i = 0; i < choices.Length; i++)
                {
                    var play = choices[i];
                    quickCalls.Add(GameDayCallPicker.Card(play.Name, GamePlanService.Label(play.Formation),
                        play.Concept is OffensiveConcept.InsideRun or OffensiveConcept.OutsideRun ? "RUN" : "PASS",
                        new PlayArtDrawable(play.Id, play.Name, true, play.Concept.ToString(), play),
                        () => { SelectPlay(play); UpdateQuickCalls(); }, compact: true, portrait: compactLayout != true), i + 1, 0);
                }
            }
            else
            {
                var choices = availableDefenses.Where(p => p.Id != selectedDefense.Id)
                    .OrderBy(p => p.Approach == selectedDefense.Approach ? 1 : 0).Take(2).ToArray();
                for (var i = 0; i < choices.Length; i++)
                {
                    var play = choices[i];
                    quickCalls.Add(GameDayCallPicker.Card(play.Name, GamePlanService.Label(play.Formation),
                        GamePlanService.Label(play.Approach),
                        new PlayArtDrawable(play.Id, play.Name, false, play.Approach.ToString(), defensiveDefinition: play),
                        () => { SelectDefense(play); UpdateQuickCalls(); }, compact: true, portrait: compactLayout != true), i + 1, 0);
                }
            }
        }

        void ArrangeViewer(bool compact)
        {
            if (compactLayout == compact) return;
            compactLayout = compact;
            ClosePicker();
            page.Content = null;
            foreach (var view in new View[] { scoreHeader, situation, possession, fieldFrame, commentaryPanel, playCard, defenseCard, controls, quickCalls, traySurface, pickerLayer })
                if (view.Parent is Layout parent) parent.Children.Remove(view);
            foreach (var button in controls.Children.OfType<Button>())
            {
                button.FontSize = compact ? 10 : 11;
                button.Padding = compact ? new Thickness(7, 4) : new Thickness(12, 8);
                button.MinimumHeightRequest = 44;
                button.HeightRequest = 44;
                button.Margin = new Thickness(0, 0, 4, 4);
            }
            score.FontSize = compact ? 15 : 17;
            downValue.FontSize = quarterClock.FontSize = 13;
            ballValue.FontSize = 13;
            result.FontSize = compact ? 13 : 15;
            result.MaxLines = compact ? 2 : 3;
            selectedDescription.IsVisible = defenseDescription.IsVisible = !compact;
            selectedHeading.FontSize = defenseHeading.FontSize = compact ? 8 : 9;
            selectedName.FontSize = defenseName.FontSize = compact ? 13 : 15;
            selectedDetails.FontSize = defenseDetails.FontSize = compact ? 10 : 11;
            selectedDetails.MaxLines = defenseDetails.MaxLines = compact ? 2 : -1;
            playInfo.Spacing = defenseInfo.Spacing = compact ? 2 : 4;
            playGrid.ColumnSpacing = defenseGrid.ColumnSpacing = compact ? 7 : 12;
            foreach (var pickerButton in new[] { choose, chooseDefense })
            {
                pickerButton.FontSize = compact ? 10 : 14;
                pickerButton.Padding = compact ? new Thickness(6, 3) : new Thickness(12, 8);
                pickerButton.HeightRequest = compact ? 26 : -1;
                pickerButton.MinimumHeightRequest = compact ? 26 : 42;
            }
            selectedArt.HeightRequest = defenseArt.HeightRequest = compact ? 54 : 112;
            selectedArt.WidthRequest = defenseArt.WidthRequest = compact ? 72 : 185;
            playGrid.ColumnDefinitions[0].Width = new GridLength(selectedArt.IsVisible ? compact ? 72 : 185 : 0);
            defenseGrid.ColumnDefinitions[0].Width = new GridLength(compact ? 72 : 185);
            playCard.Padding = defenseCard.Padding = compact ? 6 : 10;
            field.HeightRequest = -1;
            var heading = new Grid { ColumnSpacing = 10, ColumnDefinitions =
                new ColumnDefinitionCollection { new(new GridLength(2, GridUnitType.Star)), new(new GridLength(3, GridUnitType.Star)) } };
            if (compact)
            {
                heading.Add(scoreHeader, 0, 0);
                heading.Add(situation, 1, 0);
            }
            else
            {
                heading.ColumnDefinitions.Clear(); heading.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                heading.Add(new VerticalStackLayout { Spacing = 3, Children = { scoreHeader, situation } });
                downValue.FontSize = ballValue.FontSize = quarterClock.FontSize = 13;
            }
            UpdateQuickCalls();
            if (playCard.Parent is Layout oldPlay) oldPlay.Children.Remove(playCard);
            if (defenseCard.Parent is Layout oldDefense) oldDefense.Children.Remove(defenseCard);
            if (controls.Parent is Layout oldControls) oldControls.Children.Remove(controls);
            if (quickCalls.Parent is Layout oldQuick) oldQuick.Children.Remove(quickCalls);
            tray.Children.Clear(); tray.Children.Add(quickCalls); tray.Children.Add(controls);
            var body = new Grid { RowDefinitions = new RowDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) } };
            body.Add(fieldFrame, 0, 0); body.Add(traySurface, 0, 1); body.Add(pickerLayer, 0, 0);
            Grid.SetRowSpan(pickerLayer, 2);
            var root = new Grid { Padding = 4, RowSpacing = 3, RowDefinitions =
                new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
            root.Add(heading, 0, 0); root.Add(body, 0, 1); root.Add(commentaryPanel, 0, 2);
            page.Content = root;
            UpdateOverlay();
        }
        ArrangeViewer(false);
        page.SizeChanged += (_, _) =>
        {
            if (page.Width > 0 && page.Height > 0) ArrangeViewer(page.Width >= 700);
        };

        Color TeamColor(string teamName) => Color.FromArgb(TeamBrandCatalog.Get(teamName).Primary);
        void SetMarkerAppearance(Border marker, DynastyPlayer? player, Color color, Color secondary, bool awayTeam)
        {
            if (marker.Content is Label oldLabel) markerLabels.Remove(oldLabel);
            if (marker.Content is FootballSpriteView oldSprite) spritePlayers.Remove(oldSprite);
            if (useSprites)
            {
                var sprite = new FootballSpriteView(color, secondary, awayTeam, player?.Appearance?.SkinTone ?? 1,
                    showNumbers && player?.JerseyNumber is int jersey ? jersey.ToString() : player?.Position.ToString() ?? "");
                spritePlayers[sprite] = player;
                marker.Stroke = Colors.Transparent; marker.StrokeThickness = 0; marker.BackgroundColor = Colors.Transparent;
                marker.StrokeShape = new RoundRectangle { CornerRadius = 0 };
                sprite.Highlight(player is not null && activeBallOwners.Contains(player.PlayerId));
                marker.Content = sprite;
            }
            else
            {
                var label = new Label { Text = showNumbers && player?.JerseyNumber is int jersey ? jersey.ToString() : player?.Position.ToString() ?? "•",
                    FontSize = showNumbers ? 8 : 6, FontAttributes = FontAttributes.Bold, TextColor = awayTeam ? color : Colors.White,
                    HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
                markerLabels[label] = player;
                marker.StrokeShape = new RoundRectangle { CornerRadius = 14 };
                marker.BackgroundColor = awayTeam ? Colors.White : color;
                var active = player is not null && activeBallOwners.Contains(player.PlayerId);
                marker.Stroke = active ? Colors.Yellow : awayTeam ? color : Colors.White;
                marker.StrokeThickness = active ? 3 : awayTeam ? 2.2 : 1.4;
                marker.Content = label;
            }
        }
        Border Marker(DynastyPlayer? player, Color color, Color secondary, bool awayTeam)
        {
            var marker = new Border { WidthRequest = 28, HeightRequest = 28,
                StrokeShape = new RoundRectangle { CornerRadius = 14 }, Padding = 0, InputTransparent = true };
            markerHosts[marker] = (player, color, secondary, awayTeam);
            SetMarkerAppearance(marker, player, color, secondary, awayTeam);
            if (player is not null) SemanticProperties.SetDescription(marker, $"{player.Position} {player.FullName}");
            return marker;
        }
        spriteMode.Clicked += (_, _) =>
        {
            useSprites = !useSprites;
            Microsoft.Maui.Storage.Preferences.Default.Set("live-football-sprites", useSprites);
            spriteMode.Text = useSprites ? "Players: Sprites" : "Players: Circles";
            foreach (var (marker, info) in markerHosts)
                SetMarkerAppearance(marker, info.Player, info.Primary, info.Secondary, info.Away);
        };
        void RenderHuddles()
        {
            if (viewerClosed || replay is not null || animating || session.IsComplete || !NeedsUserCall()) return;
            fieldPlayers.Children.Clear(); markerLabels.Clear(); spritePlayers.Clear(); markerHosts.Clear(); activeBallOwners.Clear();
            var direction = (session.PossessionTeamName == game.HomeTeamName ? 1 : -1) * (session.Quarter % 2 == 0 ? -1 : 1);
            fieldArt.YardLine = session.YardLine; fieldArt.Distance = session.Distance; fieldArt.Direction = direction; fieldLines.Invalidate();
            foreach (var team in new[] { game.HomeTeamName, game.AwayTeamName })
            {
                var offense = team == session.PossessionTeamName;
                var homeUnit = team == game.HomeTeamName;
                var profile = homeUnit ? home : away;
                var plan = profile.GamePlan ?? controlledPlan;
                var depth = homeUnit ? session.HomeDepthChart : session.AwayDepthChart;
                var fatigue = homeUnit ? session.HomeFatigue : session.AwayFatigue;
                var previewRandom = new Random(unchecked(game.SimulationSeed ^ session.Events.Count * 31 ^ (homeUnit ? 17 : 37)));
                var preview = offense
                    ? PersonnelRotationService.SelectOffense(depth, team,
                        team == controlledTeamName ? selectedPlay.Personnel : GamePlanService.RecommendOffensivePlay(plan, session.Down, session.Distance, session.YardLine).Personnel, fatigue, previewRandom)
                    : PersonnelRotationService.SelectDefense(depth, team,
                        team == controlledTeamName ? selectedDefense.Personnel : GamePlanService.RecommendDefensivePlay(plan, session.Down, session.Distance).Personnel, fatigue, previewRandom);
                var unit = preview.PlayerIds.Select(id => session.PlayersById.GetValueOrDefault(id)).OfType<DynastyPlayer>().ToArray();
                var brand = TeamBrandCatalog.Get(team);
                for (var i = 0; i < unit.Length; i++)
                {
                    var point = GameDayLivePresentation.HuddlePoint(session.YardLine, direction, offense, i);
                    var marker = Marker(unit[i], Color.FromArgb(brand.Primary), Color.FromArgb(brand.Secondary), team == game.AwayTeamName);
                    if (marker.Content is FootballSpriteView sprite) sprite.HuddleFacing(.5 - point.X, .5 - point.Y);
                    AbsoluteLayout.SetLayoutFlags(marker, AbsoluteLayoutFlags.PositionProportional);
                    AbsoluteLayout.SetLayoutBounds(marker, new Rect(point.X, point.Y, 28, 28)); fieldPlayers.Add(marker);
                }
            }
        }
        async Task AnimateSnapAsync(GameSnapEvent snap, bool instantReplay = false)
        {
            if (snap.IsOnsideKick) { await Task.Delay(350); return; }
            if (!GameDayEngine.IsScrimmage(snap) && !GameDayAnimationPlanner.IsPlaceKick(snap) && snap.Outcome is not (SnapOutcome.Kickoff or SnapOutcome.Punt)) { await Task.Delay(350); return; }
            highlight.IsVisible = false;
            result.Text = instantReplay ? "REPLAY · " + GameDayCommentaryService.Describe(snap, session.PlayersById)
                : $"LIVE · {snap.OffenseTeamName} · {PlayEditorService.OffensiveLibrary(controlledPlan).FirstOrDefault(p => p.Id == snap.OffensivePlayId)?.Name ?? (snap.Outcome is SnapOutcome.Punt or SnapOutcome.Kickoff ? "Kick and return" : "Play in progress")}";
            fieldPlayers.AbortAnimation("snap-movement");
            fieldPlayers.Children.Clear(); markerLabels.Clear(); spritePlayers.Clear(); markerHosts.Clear(); activeBallOwners.Clear();
            fieldArt.YardLine = snap.StartYardLine; fieldArt.Distance = snap.Distance;
            fieldArt.HomeColor = TeamColor(game.HomeTeamName);
            var offenseColor = TeamColor(snap.OffenseTeamName);
            var defenseColor = TeamColor(snap.DefenseTeamName);
            var offenseIsAway = snap.OffenseTeamName.Equals(game.AwayTeamName, StringComparison.OrdinalIgnoreCase);
            var defenseIsAway = snap.DefenseTeamName.Equals(game.AwayTeamName, StringComparison.OrdinalIgnoreCase);
            var direction = (snap.OffenseTeamName == game.HomeTeamName ? 1 : -1) * (snap.Quarter % 2 == 0 ? -1 : 1);
            double Project(double yard) => GameDayAnimationPlanner.ProjectFieldYardLine(yard, direction);
            var line = Project(snap.StartYardLine);
            var kickLanding = Math.Clamp(Project(LiveFootballChoreography.KickLandingYardLine(snap)), .02, .98);
            var destination = Project(GameDayAnimationPlanner.CarrierDestinationYardLine(snap));
            fieldArt.Direction = direction; fieldLines.Invalidate();
            var movements = new List<(View View, Rect From, Rect To)>();
            var routeViews = new HashSet<View>();
            var assignedRoutes = new Dictionary<View, GameDayRouteFamily>();
            var markerHighlights = new Dictionary<Guid, Action<bool>>();
            var movementSpeeds = new Dictionary<View, int>();
            var roleCounts = new Dictionary<Position, int>();
            Guid? highlightedOwner = null;
            void RegisterMarker(DynastyPlayer? player, Border view)
            {
                movementSpeeds[view] = player?.SpeedRating ?? 70;
                if (player is null) return;
                markerHighlights[player.PlayerId] = active =>
                {
                    if (active) activeBallOwners.Add(player.PlayerId); else activeBallOwners.Remove(player.PlayerId);
                    if (view.Content is FootballSpriteView sprite) sprite.Highlight(active);
                    else
                    {
                        var info = markerHosts[view];
                        view.Stroke = active ? Colors.Yellow : info.Away ? info.Primary : Colors.White;
                        view.StrokeThickness = active ? 3 : info.Away ? 2.2 : 1.4;
                    }
                };
            }
            var pursuingDefenders = new HashSet<View>();
            View? fairCatchGunner = null;
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
            var kickoffReceiver = snap.ReturnerId ?? (snap.Outcome == SnapOutcome.Kickoff && snap.IsTouchback
                ? offense.FirstOrDefault(p => p?.Position is Position.RB or Position.WR)?.PlayerId : null);
            var lineSlots = new[] { .36, .43, .50, .57, .64 };
            var lineIndex = 0;
            var receiverIndex = 0;
            var tightEndIndex = 0;
            var backIndex = 0;
            var routeEligibleIndex = 0;
            var calledPlay = PlayEditorService.OffensiveLibrary((snap.OffenseTeamName == game.HomeTeamName ? home : away).GamePlan ?? controlledPlan).FirstOrDefault(play => play.Id == snap.OffensivePlayId);
            var routeFamily = calledPlay is null ? GameDayRouteFamily.Run : GameDayAnimationPlanner.RouteFamily(calledPlay);
            var receiverFamily = routeFamily;
            var passRelease = snap.TurnoverYardLine.HasValue ? .30 : snap.PassReleaseProgress > 0 ? snap.PassReleaseProgress : calledPlay is null ? .54 : GameDayAnimationPlanner.PassReleaseProgress(calledPlay);
            var catchProgress = GameDayAnimationPlanner.CatchProgress(passRelease);
            var caught = snap.TargetId.HasValue && snap.Outcome is SnapOutcome.Completion or SnapOutcome.Touchdown;
            var terminalCarrierId = snap.BallCarrierId ?? snap.ReturnerId ??
                (snap.Outcome is SnapOutcome.Completion or SnapOutcome.Touchdown ? snap.TargetId : null) ??
                (snap.Outcome == SnapOutcome.Sack ? snap.QuarterbackId : null);
            var isReturnTackle = (snap.Outcome is SnapOutcome.Kickoff or SnapOutcome.Punt) &&
                snap.ReturnYards > 0 && snap.Points < 6 && !snap.IsFairCatch && !snap.IsTouchback;
            var isTackle = terminalCarrierId.HasValue && (isReturnTackle ||
                snap.Outcome is SnapOutcome.Run or SnapOutcome.Scramble or SnapOutcome.Completion or SnapOutcome.Sack or SnapOutcome.Fumble);
            var isFieldGoalSnap = GameDayAnimationPlanner.IsPlaceKick(snap);
            var specialContact = GameDayAnimationPlanner.ContactPoint(snap, direction, .50);
            Rect? qbFrom = null, qbTo = null, runnerFrom = null, runnerTo = null, receiverFrom = null, receiverTo = null,
                terminalCarrierTo = null, kickerFrom = null, kickerTo = null, returnerFrom = null, returnerTo = null;
            View? terminalCarrierView = null, primaryTacklerView = null, specialReturnerView = null;
            for (var index = 0; index < offense.Length; index++)
            {
                var player = offense[index];
                double x, y;
                if (player?.PlayerId == snap.KickerId && snap.Outcome is
                    SnapOutcome.Kickoff or SnapOutcome.Punt or SnapOutcome.FieldGoal or SnapOutcome.MissedFieldGoal or SnapOutcome.ExtraPoint or SnapOutcome.MissedExtraPoint)
                {
                    x = line - direction * .075;
                    y = .50;
                }
                else if (snap.Outcome == SnapOutcome.Kickoff)
                {
                    x = player?.PlayerId == snap.ReturnerId
                        ? kickLanding : Math.Clamp(Project(10 + index % 5 * 8), .03, .60);
                    y = player?.PlayerId == snap.ReturnerId ? .50 : .10 + index % 6 * .16;
                }
                else if (isFieldGoalSnap)
                {
                    if (player?.Position == Position.QB)
                    {
                        x = line - direction * .065;
                        y = .50;
                    }
                    else if (player?.Position == Position.OL)
                    {
                        x = line - direction * .012;
                        y = lineSlots[Math.Min(lineIndex++, lineSlots.Length - 1)];
                    }
                    else
                    {
                        x = line - direction * .026;
                        y = .20 + (index % 7) * .10;
                    }
                }
                else switch (player?.Position)
                {
                    case Position.OL:
                        x = line - direction * .016;
                        y = lineSlots[Math.Min(lineIndex++, lineSlots.Length - 1)];
                        break;
                    case Position.QB:
                        x = line - direction * .065;
                        y = .50;
                        break;
                    case Position.RB:
                        x = line - direction * (.095 + backIndex * .023);
                        y = GameDayAnimationPlanner.AlignmentLane(Position.RB, backIndex++);
                        break;
                    case Position.WR:
                        x = line - direction * .018;
                        y = GameDayAnimationPlanner.AlignmentLane(Position.WR, receiverIndex++);
                        break;
                    case Position.TE:
                        x = line - direction * .022;
                        y = GameDayAnimationPlanner.AlignmentLane(Position.TE, tightEndIndex++);
                        break;
                    default:
                        x = line - direction * (.04 + (index % 3) * .02);
                        y = .20 + (index % 7) * .10;
                        break;
                }
                PlayAssignment? playerAssignment = null;
                if (player is not null && calledPlay is not null &&
                    snap.Outcome is not (SnapOutcome.Punt or SnapOutcome.Kickoff or SnapOutcome.FieldGoal or SnapOutcome.MissedFieldGoal or SnapOutcome.ExtraPoint or SnapOutcome.MissedExtraPoint))
                {
                    roleCounts.TryGetValue(player.Position, out var roleIndex);
                    roleCounts[player.Position] = roleIndex + 1;
                    playerAssignment = calledPlay.Assignments.FirstOrDefault(a => a.Position == player.Position && a.RoleIndex == roleIndex);
                    if (player.Position is Position.QB or Position.RB or Position.WR or Position.TE)
                    {
                        var custom = calledPlay.Assignments.FirstOrDefault(a => a.Position == player.Position && a.RoleIndex == roleIndex);
                        var alignment = GameDayAnimationPlanner.OffensiveAlignment(calledPlay, player.Position, roleIndex);
                        x = Project(snap.StartYardLine + alignment.X); y = alignment.Y;
                    }
                    else if (player.Position == Position.OL)
                    {
                        var alignment = GameDayAnimationPlanner.OffensiveAlignment(calledPlay, Position.OL, roleIndex);
                        x = Project(snap.StartYardLine + alignment.X); y = alignment.Y;
                    }
                }
                if (snap.Outcome == SnapOutcome.Kickoff)
                {
                    var formation = LiveFootballChoreography.Kickoff(true, player?.PlayerId == kickoffReceiver, index, snap.KickLandingYardLine, snap.IsTouchback && player?.PlayerId == kickoffReceiver ? snap.KickLandingYardLine : snap.BallEndYardLine, direction);
                    x = formation.From.X; y = formation.From.Y;
                }
                x = Math.Clamp(x, .025, .96);
                var isEligible = playerAssignment?.Task != AssignmentTask.Block && player?.Position is Position.WR or Position.TE or Position.RB;
                var assigned = player is null || calledPlay is null ? routeFamily :
                    GameDayAnimationPlanner.AssignedRoute(calledPlay, player.Position, Math.Max(0, roleCounts.GetValueOrDefault(player.Position) - 1));
                var eligibleIndex = isEligible ? routeEligibleIndex++ : index;
                var endY = player is null ? y : GameDayAnimationPlanner.RouteEndLane(assigned,
                    player.Position, eligibleIndex, y, player.PlayerId == snap.TargetId);
                var routeShare = player is null ? .2 : GameDayAnimationPlanner.RouteDepthShare(assigned,
                    player.Position, player.PlayerId == snap.TargetId || player.PlayerId == snap.BallCarrierId);
                var endX = Math.Clamp(x + (destination - line + direction * .045) * routeShare, .025, .975);
                if (isEligible && routeFamily != GameDayRouteFamily.Run && calledPlay is not null)
                {
                    var designDepth = calledPlay.Concept switch
                    {
                        OffensiveConcept.DeepPass => 24,
                        OffensiveConcept.PlayAction => 18,
                        _ => routeFamily == GameDayRouteFamily.Screen ? 5 : 10
                    };
                    if (calledPlay.Name == "Hail Mary") designDepth = Math.Min(65, 100 - snap.StartYardLine);
                    endX = Project(snap.StartYardLine + designDepth * routeShare);
                }
                else if (player?.Position == Position.OL && routeFamily != GameDayRouteFamily.Run)
                    endX = Math.Clamp(x - direction * .008, .025, .975);
                if (player?.Position == Position.QB && !snap.IsQuarterbackScramble &&
                    routeFamily != GameDayRouteFamily.Run)
                    endX = Math.Clamp(x - direction * .025, .025, .975);
                if ((snap.IsBootleg || snap.IsQuarterbackKeep) && player?.Position == Position.QB) endY = .25;
                if (playerAssignment?.Task == AssignmentTask.Block) { endX = x + direction * (routeFamily == GameDayRouteFamily.Run ? .012 : -.008); endY = y; }
                if (isFieldGoalSnap)
                {
                    endX = Math.Clamp(x + (player?.PlayerId == snap.KickerId ? direction * .025 : 0), .025, .96);
                    endY = y;
                }
                else if (snap.Outcome == SnapOutcome.Punt)
                {
                    endX = Math.Clamp(x + (destination - x) * .68, .025, .96);
                    endY = Math.Clamp(y + (.50 - y) * .45, .07, .93);
                }
                if (snap.Outcome == SnapOutcome.Kickoff)
                {
                    var formation = LiveFootballChoreography.Kickoff(true, player?.PlayerId == kickoffReceiver, index, snap.KickLandingYardLine, snap.IsTouchback && player?.PlayerId == kickoffReceiver ? snap.KickLandingYardLine : snap.BallEndYardLine, direction);
                    endX = formation.To.X; endY = formation.To.Y;
                }
                if (player?.PlayerId == terminalCarrierId && snap.CarrierLane.HasValue) endY = snap.CarrierLane.Value;
                if (player?.PlayerId == snap.TargetId && caught) endX = Project(snap.StartYardLine + snap.AirYards);
                else if (player?.PlayerId == terminalCarrierId) endX = destination;
                var view = Marker(player, offenseColor, Color.FromArgb(TeamBrandCatalog.Get(snap.OffenseTeamName).Secondary), offenseIsAway);
                RegisterMarker(player, view);
                assignedRoutes[view] = assigned;
                if (isEligible && calledPlay is not null && routeFamily != GameDayRouteFamily.Run &&
                    snap.Outcome is not (SnapOutcome.Kickoff or SnapOutcome.Punt or SnapOutcome.FieldGoal or SnapOutcome.MissedFieldGoal or SnapOutcome.ExtraPoint or SnapOutcome.MissedExtraPoint))
                    routeViews.Add(view);
                var from = new Rect(x, y, 28, 28);
                var puntCoverageTackler = isReturnTackle && snap.Outcome == SnapOutcome.Punt &&
                    player?.PlayerId == snap.PrimaryDefenderId;
                var to = puntCoverageTackler
                    ? new Rect(specialContact.X, specialContact.Y, 28, 28)
                    : new Rect(endX, endY, 28, 28);
                if (snap.IsFairCatch && player?.PlayerId == snap.PrimaryDefenderId)
                {
                    to = new Rect(kickLanding - direction * snap.NearestCoverageYardsAtCatch / 120d, .52, 28, 28);
                    fairCatchGunner = view;
                }
                if (puntCoverageTackler)
                {
                    pursuingDefenders.Add(view);
                    primaryTacklerView = view;
                }
                if (player?.PlayerId == snap.QuarterbackId) { qbFrom = from; qbTo = to; }
                if (player?.PlayerId == snap.BallCarrierId) { runnerFrom = from; runnerTo = to; }
                if (player?.PlayerId == snap.TargetId) { receiverFrom = from; receiverTo = to; receiverFamily = assigned; }
                if (player?.PlayerId == terminalCarrierId) { terminalCarrierTo = caught ? to with { X = destination } : to; terminalCarrierView = view; }
                if (player?.PlayerId == snap.KickerId) { kickerFrom = from; kickerTo = to; }
                if (player?.PlayerId == snap.ReturnerId)
                {
                    returnerFrom = from; returnerTo = to; specialReturnerView = view;
                }
                AddMovement(view, from, to, movements, fieldPlayers);
            }
            Rect CarrierTrack(Rect from, Rect to, double progress)
            {
                var carrier = session.PlayersById.GetValueOrDefault(snap.BallCarrierId ?? snap.ReturnerId ?? Guid.Empty);
                var point = GameDayAnimationPlanner.CarrierPoint(new(from.X, from.Y), new(to.X, to.Y), progress, carrier?.AgilityRating ?? 65, snap.BrokeTackle);
                return new Rect(point.X, point.Y, from.Width, from.Height);
            }
            Rect ReceiverTrack(double progress)
            {
                if (receiverFrom is not Rect from || receiverTo is not Rect to) return new Rect(line, .50, 28, 28);
                if (!caught) return RouteTrack(from, to, progress, receiverFamily);
                return progress < catchProgress
                    ? RouteTrack(from, to, progress / catchProgress, receiverFamily)
                    : CarrierTrack(to, to with { X = destination }, (progress - catchProgress) / (1 - catchProgress));
            }
            GameFieldPoint? interceptionPoint = (snap.Outcome == SnapOutcome.Interception || snap.IsPassBreakup) && receiverFrom is Rect rf && receiverTo is Rect rt
                ? GameDayAnimationPlanner.RoutePoint(new(rf.X, rf.Y), new(rt.X, rt.Y), receiverFamily, catchProgress) : null;
            if (snap.TurnoverYardLine is int turnoverSpot)
                interceptionPoint = new(GameDayAnimationPlanner.ProjectYardLine(turnoverSpot, direction), snap.CarrierLane ?? .5);
            Rect? interceptorFrom = null, interceptorTo = null;
            View? interceptorView = null;
            var contact = GameDayAnimationPlanner.ContactPoint(snap, direction, isReturnTackle ? .50 : terminalCarrierTo?.Y ?? .50);
            var assistCount = snap.Outcome switch
            {
                SnapOutcome.Sack => 0,
                SnapOutcome.Run when snap.Yards < 0 => 1,
                SnapOutcome.Run or SnapOutcome.Scramble or SnapOutcome.Completion or SnapOutcome.Fumble => snap.Yards >= 8 ? 2 : 1,
                _ => 0
            };
            var assistIds = snap.TackleAttemptIds.Where(id => id != snap.PrimaryDefenderId).Take(assistCount).ToHashSet();
            var defensivePlan = (snap.DefenseTeamName == game.HomeTeamName ? home : away).GamePlan ?? controlledPlan;
            var calledDefensivePlay = PlayEditorService.DefensiveLibrary(defensivePlan).FirstOrDefault(p => p.Id == snap.DefensivePlayId);
            var defensiveStarts = calledDefensivePlay is null ? new Dictionary<Guid, GameFieldPoint>() :
                DefensivePursuitService.Alignments(defense.Where(p => p is not null).Select(p => p!).ToArray(), calledDefensivePlay);
            var blockingTracks = new Dictionary<View, (Rect From, Rect To, double Release)>();
            var blockers = movements.Where(m => m.View is Border b && markerHosts[b].Player?.Position == Position.OL).ToArray();
            var assignedBlockers = new HashSet<View>();
            var coverageTracks = new Dictionary<View, (Rect From, Rect To, GameDayRouteFamily Route, GameFieldPoint Start, bool Zone, bool Pursue)>();
            var defensiveRoles = new Dictionary<Position, int>();
            var defensiveLine = 0;
            var linebackers = 0;
            var secondary = 0;
            for (var index = 0; index < defense.Length; index++)
            {
                var player = defense[index];
                var defensiveRole = player is null ? 0 : defensiveRoles.GetValueOrDefault(player.Position);
                if (player is not null) defensiveRoles[player.Position] = defensiveRole + 1;
                var passRushSnap = snap.TargetId.HasValue || snap.Outcome == SnapOutcome.Sack || snap.IsQuarterbackScramble;
                var passRusher = player is not null && !isFieldGoalSnap && passRushSnap &&
                    DefensivePursuitService.IsPassRusher(calledDefensivePlay, player.Position, defensiveRole);
                double x, y;
                if (snap.Outcome == SnapOutcome.Punt && player?.PlayerId == snap.ReturnerId)
                {
                    x = kickLanding;
                    y = .50;
                }
                else switch (player?.Position)
                {
                    case Position.DE:
                    case Position.DT:
                        x = line + direction * .025;
                        y = .34 + (defensiveLine++ % 5) * .08;
                        break;
                    case Position.OLB:
                    case Position.MLB:
                        x = line + direction * .075;
                        y = .28 + (linebackers++ % 5) * .11;
                        break;
                    case Position.CB:
                    case Position.FS:
                    case Position.SS:
                        x = line + direction * (player.Position is Position.FS or Position.SS ? .19 : .12);
                        y = secondary++ % 2 == 0 ? .15 + (secondary / 2) * .09 : .85 - (secondary / 2) * .09;
                        break;
                    default:
                        x = line + direction * .10;
                        y = .20 + (index % 7) * .10;
                        break;
                }
                x = Math.Clamp(x, .025, .96);
                if (isFieldGoalSnap)
                {
                    var isFront = player?.Position is Position.DE or Position.DT or Position.OLB or Position.MLB;
                    x = Math.Clamp(line + direction * (isFront ? .018 : .065), .025, .96);
                    y = isFront ? .26 + (index % 7) * .08 : .18 + (index % 6) * .13;
                }
                if (player is not null && !isFieldGoalSnap && snap.Outcome is not (SnapOutcome.Punt or SnapOutcome.Kickoff) &&
                    defensiveStarts.TryGetValue(player.PlayerId, out var defensiveStart))
                { x = Project(snap.StartYardLine + defensiveStart.X); y = defensiveStart.Y; }
                var primaryDefender = isTackle && player?.PlayerId == snap.PrimaryDefenderId;
                var assistingDefender = isTackle && player is not null && assistIds.Contains(player.PlayerId);
                var hasPursuitTarget = primaryDefender || assistingDefender;
                var pursuitTarget = GameDayAnimationPlanner.PursuitTarget(contact, index, primaryDefender);
                var endX = hasPursuitTarget ? pursuitTarget.X : Math.Clamp(x + (destination - line) * .72, .025, .96);
                var endY = hasPursuitTarget ? pursuitTarget.Y : Math.Clamp(y + ((index % 3) - 1) * .025, .07, .93);
                (View View, Rect From, Rect To)? blocker = null;
                var engages = player is not null && GameDayEngine.IsScrimmage(snap) && !isFieldGoalSnap &&
                    (passRusher || !passRushSnap && player.Position is Position.DE or Position.DT) &&
                    !(snap.Outcome == SnapOutcome.Interception && player.PlayerId == snap.PrimaryDefenderId);
                if (engages && blockers.Length > 0)
                {
                    var available = blockers.Where(m => !assignedBlockers.Contains(m.View)).ToArray();
                    blocker = (available.Length > 0 ? available : blockers).MinBy(m => Math.Abs(m.From.Y - y));
                    assignedBlockers.Add(blocker.Value.View);
                    if (!hasPursuitTarget)
                    { endX = blocker.Value.To.X + direction * .020; endY = blocker.Value.To.Y; }
                }
                if (isFieldGoalSnap)
                {
                    endX = Math.Clamp(x - direction * .018, .025, .96);
                    endY = y;
                }
                if (snap.Outcome == SnapOutcome.Punt && player?.PlayerId == snap.ReturnerId)
                {
                    endX = destination;
                    endY = .50;
                }
                if (interceptionPoint is GameFieldPoint interception && player?.PlayerId == snap.PrimaryDefenderId)
                { endX = interception.X; endY = interception.Y; }
                var view = Marker(player, defenseColor, Color.FromArgb(TeamBrandCatalog.Get(snap.DefenseTeamName).Secondary), defenseIsAway);
                RegisterMarker(player, view);
                if (blocker is { } block)
                    blockingTracks[view] = (block.From, block.To, !hasPursuitTarget ? 1 :
                        snap.Outcome == SnapOutcome.Sack ? .32 : snap.TargetId.HasValue ? catchProgress : .42);
                if (hasPursuitTarget) pursuingDefenders.Add(view);
                if (primaryDefender)
                {
                    primaryTacklerView = view;
                }
                if (snap.Outcome == SnapOutcome.Kickoff)
                {
                    var formation = LiveFootballChoreography.Kickoff(false, player?.PlayerId == snap.KickerId, index, snap.KickLandingYardLine, snap.BallEndYardLine, direction);
                    x = formation.From.X; y = formation.From.Y; endX = formation.To.X; endY = formation.To.Y;
                    if (player?.PlayerId == snap.PrimaryDefenderId) { endX = destination; endY = .5; }
                    if (snap.IsTouchback) { endX = Project(30); endY = y; }
                }
                var from = new Rect(x, y, 28, 28);
                var to = new Rect(endX, endY, 28, 28);
                if (interceptionPoint.HasValue && player?.PlayerId == snap.PrimaryDefenderId)
                { interceptorView = view; interceptorFrom = from; interceptorTo = to; }
                if (player?.PlayerId == snap.KickerId) { kickerFrom = from; kickerTo = to; }
                if (player?.PlayerId == snap.ReturnerId)
                {
                    returnerFrom = from; returnerTo = to;
                    terminalCarrierTo ??= to; terminalCarrierView ??= view; specialReturnerView = view;
                }
                if (passRushSnap && !passRusher && player is not null && snap.Outcome is not (SnapOutcome.Kickoff or SnapOutcome.Punt) && routeViews.Count > 0 && player.PlayerId != snap.ReturnerId)
                {
                    var match = movements.Where(m => routeViews.Contains(m.View))
                        .OrderBy(m => player.PlayerId == snap.PrimaryDefenderId && snap.TargetId.HasValue && m.View is Border marker && markerHosts[marker].Player?.PlayerId == snap.TargetId ? 0 : 1)
                        .ThenBy(m => Math.Abs(m.From.Y - y)).First();
                    var assignment = calledDefensivePlay?.Assignments.FirstOrDefault(a => a.Position == player.Position && a.RoleIndex == defensiveRole);
                    var zone = calledDefensivePlay is not null && LiveFootballChoreography.Zone(calledDefensivePlay, assignment);
                    coverageTracks[view] = (match.From, match.To, assignedRoutes[match.View], new(x, y), zone, hasPursuitTarget);
                    if (!hasPursuitTarget && interceptionPoint is null)
                    { var point = LiveFootballChoreography.Coverage(new(x,y), new(match.To.X, match.To.Y), 1, zone, direction); to = to with { X = point.X, Y = point.Y }; }
                }
                AddMovement(view, from, to, movements, fieldPlayers);
            }
            var ball = new Border { WidthRequest = 14, HeightRequest = 9, BackgroundColor = DynastyTheme.Palette("#8B4D24"),
                Stroke = DynastyTheme.Palette("#F5D8B5"), StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 5 } };
            // Ball starts on the center, goes to the QB, then follows the handoff
            // or pass target. Its position is computed from the same player movement.
            var center = movements.Where(m => m.From.Y >= .35 && m.From.Y <= .65 &&
                Math.Abs(m.From.X - Project(snap.StartYardLine - .25)) < .01)
                .OrderBy(m => Math.Abs(m.From.Y - .50)).FirstOrDefault();
            var centerPoint = center.View is null ? new Rect(line, .50, 14, 9) : center.From;
            Rect BallAt(Rect player)
            {
                // Proportional layout positions depend on each view's size. Anchor the ball to the
                // sprite's hands in actual pixels so portrait/landscape and larger players stay synced.
                var width = Math.Max(40, fieldPlayers.Width); var height = Math.Max(40, fieldPlayers.Height);
                var handX = player.X * (width - player.Width) + player.Width * (.5 + direction * .23);
                var handY = player.Y * (height - player.Height) + player.Height * .52;
                return new Rect(Math.Clamp((handX - 7) / (width - 14), 0, 1),
                    Math.Clamp((handY - 4.5) / (height - 9), 0, 1), 14, 9);
            }
            var isPass = snap.TargetId.HasValue && !snap.BallCarrierId.HasValue;
            var isSack = snap.Outcome == SnapOutcome.Sack;
            var isKick = snap.Outcome is SnapOutcome.Kickoff or SnapOutcome.Punt or SnapOutcome.FieldGoal or SnapOutcome.MissedFieldGoal or SnapOutcome.ExtraPoint or SnapOutcome.MissedExtraPoint;
            var isFieldGoal = GameDayAnimationPlanner.IsPlaceKick(snap);
            Rect RouteTrack(Rect from, Rect to, double progress, GameDayRouteFamily? assigned = null)
            {
                var point = GameDayAnimationPlanner.RoutePoint(new(from.X, from.Y), new(to.X, to.Y), assigned ?? routeFamily, progress);
                return new Rect(point.X, point.Y, from.Width, from.Height);
            }
            Rect BlockingTrack(Rect from, Rect to, (Rect From, Rect To, double Release) block, double progress)
            {
                var point = GameDayAnimationPlanner.BlockedRusherPoint(new(from.X, from.Y),
                    new(block.From.X, block.From.Y), new(block.To.X, block.To.Y), new(to.X, to.Y), direction, progress, block.Release);
                return new Rect(point.X, point.Y, from.Width, from.Height);
            }
            var carrierLabel = new Label { FontSize = 10, MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation, FontAttributes = FontAttributes.Bold, TextColor = Colors.White,
                BackgroundColor = Colors.Transparent, Shadow = new Shadow { Brush = Brush.Black, Offset = new Point(1, 1), Radius = 2, Opacity = 1 }, Padding = new Thickness(2, 0),
                HorizontalTextAlignment = TextAlignment.Center, IsVisible = false, InputTransparent = true };
            var ballStart = BallAt(centerPoint);
            AbsoluteLayout.SetLayoutFlags(ball, AbsoluteLayoutFlags.PositionProportional);
            AbsoluteLayout.SetLayoutBounds(ball, ballStart);
            fieldPlayers.Add(ball);
            AbsoluteLayout.SetLayoutFlags(carrierLabel, AbsoluteLayoutFlags.PositionProportional); fieldPlayers.Add(carrierLabel);
            var durationSeconds = GameDayAnimationPlanner.PlaybackDurationSeconds(movements.Select(m =>
            {
                var from = new GameFieldPoint(m.From.X, m.From.Y);
                var to = new GameFieldPoint(m.To.X, m.To.Y);
                var distance = routeViews.Contains(m.View) ? GameDayAnimationPlanner.RouteLengthYards(from, to, assignedRoutes.GetValueOrDefault(m.View, routeFamily))
                    : GameDayAnimationPlanner.DistanceYards(from, to);
                if (blockingTracks.TryGetValue(m.View, out var block))
                {
                    var approach = BlockingTrack(m.From, m.To, block, .22);
                    var escapeLength = 0d;
                    var previous = BlockingTrack(m.From, m.To, block, block.Release);
                    if (block.Release < 1)
                        for (var sample = 1; sample <= 20; sample++)
                        {
                            var next = BlockingTrack(m.From, m.To, block, block.Release + (1 - block.Release) * sample / 20d);
                            escapeLength += GameDayAnimationPlanner.DistanceYards(new(previous.X, previous.Y), new(next.X, next.Y));
                            previous = next;
                        }
                    return (Math.Max(GameDayAnimationPlanner.DistanceYards(from, new(approach.X, approach.Y)) / .22,
                        block.Release < 1 ? escapeLength / (1 - block.Release) : 0), movementSpeeds[m.View], 1d);
                }
                if (caught && ReferenceEquals(m.View, terminalCarrierView))
                    return (Math.Max(distance / catchProgress, GameDayAnimationPlanner.DistanceYards(to, new(destination, to.Y)) / (1 - catchProgress)), movementSpeeds[m.View], 1d);
                var active = ReferenceEquals(m.View, fairCatchGunner) ? .65 : ReferenceEquals(m.View, specialReturnerView) &&
                    snap.Outcome is SnapOutcome.Kickoff or SnapOutcome.Punt ? .35 : ReferenceEquals(m.View, interceptorView) ? catchProgress : 1;
                return (distance, movementSpeeds[m.View], active);
            }));
            if (snap.TurnoverYardLine.HasValue) durationSeconds = Math.Max(durationSeconds, snap.ReturnYards / 9d / (1 - catchProgress));
            durationSeconds /= instantReplay ? replaySpeed : gameSpeed;
            Rect TurnoverTrack(double progress)
            {
                var pickup = interceptorTo ?? new Rect(GameDayAnimationPlanner.ProjectYardLine(snap.TurnoverYardLine ?? snap.StartYardLine, direction), snap.CarrierLane ?? .5, 28, 28);
                var end = pickup with { X = GameDayAnimationPlanner.ProjectYardLine(snap.BallEndYardLine, direction) };
                return Interpolate(pickup, end, Math.Clamp((progress - catchProgress) / (1 - catchProgress), 0, 1));
            }
            var turnoverPickupPositions = new Dictionary<View, Rect>();
            var previousPositions = movements.ToDictionary(m => m.View, m => m.From);
            var completion = new TaskCompletionSource();
            var animation = new Animation(progress =>
            {
                foreach (var movement in movements)
                {
                    var movementProgress = ReferenceEquals(movement.View, fairCatchGunner) ? Math.Min(1, progress / .65) : ReferenceEquals(movement.View, interceptorView) ? Math.Min(1, progress / catchProgress) : isKick && (snap.Outcome is SnapOutcome.Kickoff or SnapOutcome.Punt) &&
                        ReferenceEquals(movement.View, specialReturnerView)
                        ? GameDayAnimationPlanner.ReturnProgress(progress)
                        : pursuingDefenders.Contains(movement.View)
                        ? GameDayAnimationPlanner.PursuitProgress(progress, ReferenceEquals(movement.View, primaryTacklerView))
                        : progress;
                    var position = blockingTracks.TryGetValue(movement.View, out var block)
                        ? BlockingTrack(movement.From, movement.To, block, progress)
                        : caught && ReferenceEquals(movement.View, terminalCarrierView) ? ReceiverTrack(progress) : routeViews.Contains(movement.View)
                        ? RouteTrack(movement.From, movement.To, movementProgress, assignedRoutes.GetValueOrDefault(movement.View, routeFamily))
                        : (ReferenceEquals(movement.View, terminalCarrierView) && !isPass || ReferenceEquals(movement.View, specialReturnerView))
                        ? CarrierTrack(movement.From, movement.To, movementProgress) : Interpolate(movement.From, movement.To, movementProgress);
                    if (coverageTracks.TryGetValue(movement.View, out var cover))
                    {
                        Rect CoverAt(double at)
                        {
                            var receiverAt = RouteTrack(cover.From, cover.To, at, cover.Route);
                            var point = LiveFootballChoreography.Coverage(cover.Start, new(receiverAt.X, receiverAt.Y), at, cover.Zone, direction);
                            return new Rect(point.X, point.Y, 28, 28);
                        }
                        position = cover.Pursue && progress > catchProgress
                            ? Interpolate(CoverAt(catchProgress), movement.To, (progress - catchProgress) / (1 - catchProgress)) : CoverAt(progress);
                    }
                    if (snap.TurnoverYardLine.HasValue && progress >= catchProgress)
                    {
                        var athlete = movement.View is Border marker ? markerHosts.GetValueOrDefault(marker).Player : null;
                        if (athlete?.PlayerId == snap.ReturnerId) position = TurnoverTrack(progress);
                        else if (athlete?.PlayerId == snap.ReturnTacklerId)
                        {
                            var carrierAt = TurnoverTrack(progress);
                            var chase = Math.Clamp((progress - catchProgress) / (1 - catchProgress), 0, 1);
                            // Pursuer closes from their actual pickup-time position; no teleport to the carrier.
                            if (!turnoverPickupPositions.TryGetValue(movement.View, out var pickup))
                                turnoverPickupPositions[movement.View] = pickup = previousPositions[movement.View];
                            position = Interpolate(pickup, carrierAt, chase);
                        }
                    }
                    AbsoluteLayout.SetLayoutBounds(movement.View, position);
                    if (movement.View is Border host && host.Content is FootballSpriteView sprite)
                    {
                        var player = spritePlayers.GetValueOrDefault(sprite);
                        var blocking = player?.Position == Position.OL ||
                            blockingTracks.TryGetValue(host, out var engagement) && progress <= engagement.Release;
                        var prior = previousPositions[host];
                        var dx = (position.X - prior.X) * Math.Max(1, fieldPlayers.Width - position.Width);
                        var dy = (position.Y - prior.Y) * Math.Max(1, fieldPlayers.Height - position.Height);
                        var passing = player?.PlayerId == snap.QuarterbackId && isPass && progress < passRelease;
                        if (blocking || passing) { dx = player?.TeamName == snap.OffenseTeamName ? direction : -direction; dy = 0; }
                        sprite.Animate(progress, durationSeconds, dx, dy, blocking, passing);
                        previousPositions[host] = position;
                    }
                }
                var owner = GameDayAnimationPlanner.BallOwner(snap, progress, passRelease);
                if (owner != highlightedOwner)
                {
                    if (highlightedOwner is Guid old && markerHighlights.TryGetValue(old, out var clear)) clear(false);
                    if (owner is Guid current && markerHighlights.TryGetValue(current, out var mark)) mark(true);
                    highlightedOwner = owner;
                }
                var labelOwner = owner;
                if (!isPass && owner is Guid liveOwner && progress > .92)
                {
                    var carrierHost = markerHosts.FirstOrDefault(m => m.Value.Player?.PlayerId == liveOwner).Key;
                    if (carrierHost is not null)
                    {
                        var at = AbsoluteLayout.GetLayoutBounds(carrierHost);
                        var nearby = markerHosts.Where(m => m.Value.Player?.TeamName == snap.DefenseTeamName)
                            .Select(m => (Player: m.Value.Player!, Distance: GameDayAnimationPlanner.DistanceYards(new(at.X, at.Y),
                                new(AbsoluteLayout.GetLayoutBounds(m.Key).X, AbsoluteLayout.GetLayoutBounds(m.Key).Y))))
                            .OrderBy(m => m.Distance).FirstOrDefault();
                        if (nearby.Player is not null && nearby.Distance < 5) labelOwner = nearby.Player.PlayerId;
                    }
                }
                carrierLabel.IsVisible = useSprites && showCarrierNames && labelOwner.HasValue;
                if (carrierLabel.IsVisible && labelOwner is Guid ownerId)
                {
                    var host = markerHosts.FirstOrDefault(m => m.Value.Player?.PlayerId == ownerId).Key;
                    if (host is not null)
                    {
                        var ownerPlayer = session.PlayersById.GetValueOrDefault(ownerId);
                        var fullName = ownerPlayer?.FullName ?? "";
                        var surname = fullName.Contains(' ') ? fullName[(fullName.IndexOf(' ') + 1)..] : fullName;
                        carrierLabel.Text = $"{(ownerPlayer?.JerseyNumber is int n ? $"#{n} " : "")}{surname}";
                        var pos = AbsoluteLayout.GetLayoutBounds(host);
                        var width = Math.Max(180, fieldPlayers.Width); var height = Math.Max(50, fieldPlayers.Height);
                        var labelX = Math.Clamp(pos.X * (width - 28) + 14 - 75, 0, width - 150);
                        // Pick the less crowded side in screen pixels; only one name is shown.
                        var candidates = new[] { Math.Clamp(pos.Y * (height - 28) - 24, 0, height - 20),
                            Math.Clamp(pos.Y * (height - 28) + 32, 0, height - 20) };
                        var labelY = candidates.OrderBy(y => markerHosts.Keys.Count(marker =>
                        {
                            var other = AbsoluteLayout.GetLayoutBounds(marker);
                            var x = other.X * (width - other.Width); var top = other.Y * (height - other.Height);
                            return x < labelX + 150 && x + other.Width > labelX && top < y + 20 && top + other.Height > y;
                        })).First();
                        AbsoluteLayout.SetLayoutBounds(carrierLabel, new Rect(labelX / (width - 150), labelY / (height - 20), 150, 20));
                    }
                    else carrierLabel.IsVisible = false;
                }
                Rect Track(Rect? from, Rect? to, double fraction) =>
                    from is Rect a && to is Rect b ? (from == runnerFrom && !isPass || from == returnerFrom ? CarrierTrack(a, b, fraction) : Interpolate(a, b, fraction)) : centerPoint;
                Rect currentBall;
                if (isKick && snap.Outcome is SnapOutcome.Kickoff or SnapOutcome.Punt)
                {
                    var launch = kickerFrom ?? qbFrom ?? centerPoint;
                    if (progress < .65)
                    {
                        var landing = returnerFrom ?? new Rect(kickLanding, .50, 14, 9);
                        currentBall = Interpolate(BallAt(launch), BallAt(landing), progress / .65);
                        currentBall = currentBall with { Y = Math.Clamp(currentBall.Y - Math.Sin(progress / .65 * Math.PI) * .18, .02, .97) };
                    }
                    else if (returnerFrom.HasValue)
                        currentBall = BallAt(Track(returnerFrom, returnerTo, GameDayAnimationPlanner.ReturnProgress(progress)));
                    else currentBall = new Rect(snap.IsTouchback ? kickLanding : destination, .50, 14, 9);
                }
                else if (isFieldGoal)
                {
                    var launch = kickerFrom ?? qbFrom ?? centerPoint;
                    var flight = Math.Clamp((progress - .20) / .80, 0, 1);
                    var goalX = direction == 1 ? .975 : .025;
                    var goalY = snap.Outcome is SnapOutcome.FieldGoal or SnapOutcome.ExtraPoint ? .50 : snap.Sequence % 2 == 0 ? .28 : .72;
                    currentBall = progress < .20
                        ? Interpolate(ballStart, BallAt(launch), progress / .20)
                        : Interpolate(BallAt(launch), new Rect(goalX, goalY, 14, 9), flight);
                    ball.Scale = 1 + Math.Sin(flight * Math.PI) * .55;
                    ball.Rotation = flight * 540 * direction;
                }
                else if (progress < .13)
                    currentBall = Interpolate(ballStart, BallAt(Track(qbFrom, qbTo, 0)), progress / .13);
                else if (isSack)
                    currentBall = BallAt(Track(qbFrom, qbTo, progress));
                else if (isPass && progress < passRelease)
                    currentBall = BallAt(Track(qbFrom, qbTo, progress));
                else if ((snap.Outcome == SnapOutcome.Interception || snap.IsPassBreakup) && interceptorTo is Rect interceptionSpot)
                {
                    var launch = Track(qbFrom, qbTo, passRelease);
                    var caughtAt = GameDayAnimationPlanner.InterceptionBallPoint(new(launch.X, launch.Y),
                        new(interceptionSpot.X, interceptionSpot.Y), progress, passRelease);
                    if (snap.IsPassBreakup && progress >= catchProgress)
                        caughtAt = GameDayLivePresentation.DeflectedBallPoint(new(interceptionSpot.X, interceptionSpot.Y), direction, progress, catchProgress);
                    currentBall = BallAt(new Rect(caughtAt.X, caughtAt.Y, 28, 28));
                    if (snap.IsPassBreakup && interceptorView is not null && progress >= catchProgress)
                    {
                        interceptorView.Scale = 1 + Math.Sin(Math.Clamp((progress - catchProgress) / .12, 0, 1) * Math.PI) * .18;
                        ball.Rotation = (progress - catchProgress) * 1400;
                    }
                }
                else if (isPass && receiverFrom.HasValue)
                {
                    var receiver = ReceiverTrack(progress);
                    currentBall = caught
                        ? progress < catchProgress
                            ? Interpolate(BallAt(Track(qbFrom, qbTo, passRelease)), BallAt(receiverTo ?? receiver),
                                (progress - passRelease) / (catchProgress - passRelease))
                            : BallAt(receiver)
                        : IncompleteBall();
                    Rect IncompleteBall()
                    {
                        var intended = RouteTrack(receiverFrom.Value, receiverTo ?? receiverFrom.Value, catchProgress, receiverFamily);
                        var miss = GameDayAnimationPlanner.IncompletePassDestination(new(intended.X, intended.Y), direction, snap.IsDrop, snap.Sequence);
                        var flight = Math.Clamp((progress - passRelease) / (catchProgress - passRelease), 0, 1);
                        return Interpolate(BallAt(Track(qbFrom, qbTo, passRelease)), new Rect(miss.X, miss.Y, 14, 9), flight);
                    }
                }
                else if (runnerFrom.HasValue)
                    currentBall = progress < .25
                        ? Interpolate(BallAt(Track(qbFrom, qbTo, 0)), BallAt(Track(runnerFrom, runnerTo, .25)), (progress - .13) / .12)
                        : BallAt(Track(runnerFrom, runnerTo, progress));
                else currentBall = BallAt(Track(qbFrom, qbTo, progress));
                if (snap.TurnoverYardLine.HasValue && progress >= catchProgress) currentBall = BallAt(TurnoverTrack(progress));
                AbsoluteLayout.SetLayoutBounds(ball, currentBall);
                if (cameraMode != 0) FrameCamera(cameraMode == 2 ? currentBall.X : (centerPoint.X + currentBall.X) / 2, cameraMode == 2 ? currentBall.Y : .5);
                fieldArt.WeatherPhase = progress * durationSeconds;
                if (session.Weather.Precipitation != GamePrecipitation.Clear) fieldLines.Invalidate();
                if (isTackle && terminalCarrierView is not null && primaryTacklerView is not null && progress >= .84)
                {
                    var impact = Math.Clamp((progress - .84) / .16, 0, 1);
                    primaryTacklerView.Scale = 1 + Math.Sin(impact * Math.PI) * .22;
                    terminalCarrierView.Rotation = direction * impact * 10;
                    terminalCarrierView.Scale = 1 - impact * .08;
                }
            });
            var duration = (uint)(durationSeconds * 1000);
            animation.Commit(fieldPlayers, "snap-movement", 16, duration,
                Easing.Linear, (_, _) => completion.TrySetResult());
            await completion.Task;
        }
        async Task ShowHighlightAsync(GameSnapEvent snap, string? period = null)
        {
            if (viewerClosed) return;
            var headline = period ?? (snap.IsTouchback ? $"TOUCHBACK\nBall at {(snap.Outcome == SnapOutcome.Kickoff ? 25 : 20)}" : snap.Points >= 6 ? "TOUCHDOWN" : snap.Outcome == SnapOutcome.ClockStoppage
                ? snap.TimeoutTeamName is not null ? "TIMEOUT" : snap.Description?.Contains("Two-minute", StringComparison.OrdinalIgnoreCase) == true ? "TWO-MINUTE TIMEOUT" : null
                : GameDayAnimationPlanner.OutcomeHeadline(snap.Outcome));
            if (headline is null) return;
            var penalty = GameDayCommentaryService.Penalty(snap);
            var team = snap.TimeoutTeamName ?? penalty?.TeamName ?? LiveFootballChoreography.NotificationTeam(snap);
            highlightText.Text = snap.TimeoutTeamName is not null && period is null
                ? $"TIMEOUT\n{snap.TimeoutTeamName}\n{(team == game.HomeTeamName ? snap.HomeTimeoutsRemaining : snap.AwayTimeoutsRemaining)} remaining" : period is null ? penalty?.Banner ?? $"{headline}\n{team}" : $"{headline}\n{game.AwayTeamName} {session.AwayScore} - {session.HomeScore} {game.HomeTeamName}";
            highlight.BackgroundColor = TeamColor(team);
            var color = TeamColor(team);
            highlightText.TextColor = color.Red * .2126 + color.Green * .7152 + color.Blue * .0722 > .55 ? Colors.Black : Colors.White;
            highlight.Opacity = 0;
            highlight.Scale = .72;
            highlight.IsVisible = true;
            var completion = new TaskCompletionSource();
            new Animation(progress =>
            {
                highlight.Opacity = progress < .25 ? progress / .25 : progress > .82 ? (1 - progress) / .18 : 1;
                highlight.Scale = .72 + Math.Min(1, progress / .30) * .28 + Math.Sin(progress * Math.PI * 2) * .025;
            }).Commit(highlight, "game-highlight", 16, penalty is not null && period is null ? 2100u : 1250u, Easing.CubicOut,
                (_, _) => completion.TrySetResult());
            await completion.Task;
            highlight.IsVisible = false;
        }
        bool ControlledOnOffense() => session.PossessionTeamName.Equals(controlledTeamName, StringComparison.OrdinalIgnoreCase);
        bool NeedsUserCall() => session.Mode == GameDayMode.Play && !session.IsComplete && !session.KickoffPending &&
            (ControlledOnOffense() ? !coachCallsOffense : !coachCallsDefense);
        void Refresh(GameSnapEvent? snap = null)
        {
            if (viewerClosed) return;
            var homeMorale = GameMoraleService.InGame(session, game.HomeTeamName);
            var awayMorale = GameMoraleService.InGame(session, game.AwayTeamName);
            conditionsHud.Text = $"{session.Weather.Summary} · Morale {awayTeam.Abbreviation} {awayMorale} / {homeTeam.Abbreviation} {homeMorale}";
            awayScoreLabel.Text = session.AwayScore.ToString(); homeScoreLabel.Text = session.HomeScore.ToString();
            string ShortName(string name, string abbreviation) => name == "Ohio State" ? "OHST" : name == "Oklahoma State" ? "OKST" : name == "Oregon State" ? "ORST" : abbreviation;
            awayName.Text = (session.PossessionTeamName == game.AwayTeamName ? "● " : "") + ShortName(game.AwayTeamName, awayTeam.Abbreviation);
            homeName.Text = (session.PossessionTeamName == game.HomeTeamName ? "● " : "") + ShortName(game.HomeTeamName, homeTeam.Abbreviation);
            string Dots(int count) => new string('●', count) + new string('○', Math.Max(0, 3 - count));
            timeoutsHud.Text = $"TIMEOUTS  {awayTeam.Abbreviation} {Dots(session.AwayTimeouts)} ({session.AwayTimeouts})   ·   {homeTeam.Abbreviation} {Dots(session.HomeTimeouts)} ({session.HomeTimeouts})";
            awayTimeouts.Text = Dots(session.AwayTimeouts); homeTimeouts.Text = Dots(session.HomeTimeouts);
            SemanticProperties.SetDescription(awayTimeouts, $"{game.AwayTeamName}: {session.AwayTimeouts} timeouts");
            SemanticProperties.SetDescription(homeTimeouts, $"{game.HomeTeamName}: {session.HomeTimeouts} timeouts");
            quarterClock.Text = $"Q{session.Quarter}  {TimeSpan.FromSeconds(session.ClockSeconds):m\\:ss}";
            if (session.OvertimeRound > 0) quarterClock.Text = $"OT {session.OvertimeRound} · untimed";
            downValue.Text = session.KickoffPending ? "—" : Ordinal(session.Down);
            toGoValue.Text = session.KickoffPending ? "KICKOFF" : session.Distance.ToString();
            if (session.TryPending) { downValue.Text = "TRY"; toGoValue.Text = "3"; }
            if (!session.KickoffPending && !session.TryPending) downValue.Text = $"{Ordinal(session.Down)} & {session.Distance}";
            ballValue.Text = session.KickoffPending ? "RECEIVE" : GameDayEngine.BallOn(session, name => _teamsByName.TryGetValue(name, out var team) ? team.Abbreviation : name);
            possession.Text = session.Mode == GameDayMode.Watch
                ? $"CPU vs CPU · {session.PossessionTeamName} possession"
                : $"Controlling {controlledTeamName} · {session.PossessionTeamName} possession";
            if (snap is not null)
            {
                result.Text = GameDayCommentaryService.Describe(snap, session.PlayersById);
            }
            if (!session.IsComplete && !session.KickoffPending)
            {
                if (ControlledOnOffense())
                    {
                    if (session.TryPending)
                    {
                        selectedCallId = GameDayEngine.ShouldAttemptTwo(session) ? "special-two-point" : "special-extra-point";
                        selectedName.Text = selectedCallId == "special-two-point" ? "Two-point conversion" : "Extra point";
                        selectedHeading.Text = "CONVERSION ATTEMPT"; selectedDetails.Text = "Try from the 3-yard line";
                        selectedDescription.Text = "Choose one or two points."; selectedArt.IsVisible = false; call.Text = "Attempt conversion";
                    }
                    else SelectRecommendation(GameDayRecommendationService.Recommend(session, controlledPlan));
                }
                else
                    SelectDefense(GamePlanService.RecommendDefensivePlay(controlledPlan, session.Down, session.Distance), true);
            }
            var awaitingOffense = session.Mode == GameDayMode.Play && ControlledOnOffense() && !coachCallsOffense &&
                !session.IsComplete && !session.KickoffPending;
            var awaitingDefense = session.Mode == GameDayMode.Play && !ControlledOnOffense() && !coachCallsDefense &&
                !session.IsComplete && !session.KickoffPending;
            call.IsVisible = awaitingOffense;
            callDefense.IsVisible = awaitingDefense;
            playCard.IsVisible = call.IsVisible;
            choose.IsVisible = !session.TryPending;
            defenseCard.IsVisible = callDefense.IsVisible;
            delegateOffense.IsVisible = delegateDefense.IsVisible = session.Mode == GameDayMode.Play && !session.IsComplete;
            delegateOffense.Text = coachCallsOffense ? "Offense: Coach" : "Offense: You";
            delegateDefense.Text = coachCallsDefense ? "Defense: Coach" : "Defense: You";
            watch.IsVisible = !session.IsComplete;
            watch.Text = session.Mode == GameDayMode.Watch ? watchPaused ? "Resume watch" : "Pause watch" : "Watch CPU";
            takeover.IsVisible = session.Mode == GameDayMode.Watch && !session.IsComplete;
            onside.IsVisible = session.Mode == GameDayMode.Play && !session.IsComplete;
            onside.Text = session.OnsideKickRequested ? "Next kickoff: Onside" : "Next kickoff: Deep";
            punt.IsVisible = kick.IsVisible = awaitingOffense && !session.TryPending;
            extraPoint.IsVisible = awaitingOffense && session.TryPending && session.OvertimeRound < 2;
            twoPoint.IsVisible = awaitingOffense && session.TryPending;
            kneel.IsVisible = spike.IsVisible = awaitingOffense && !session.TryPending && session.OvertimeRound == 0;
            skip.IsVisible = !session.IsComplete;
            close.IsVisible = session.IsComplete;
            var remaining = controlledTeamName == game.HomeTeamName ? session.HomeTimeouts : session.AwayTimeouts;
            timeout.Text = $"Timeout ({remaining})";
            timeout.IsVisible = !session.IsComplete;
            timeout.IsEnabled = !animating && !session.KickoffPending && (!session.ClockStopped || session.ClockRestartsOnReady) && remaining > 0;
            UpdateQuickCalls();
            UpdateOverlay();
            RenderHuddles();
        }
        async Task ShowSnapAsync(GameSnapEvent snap)
        {
            var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            currentSnapFinished = finished.Task;
            try
            {
                if (GameDayEngine.IsScrimmage(snap) || GameDayAnimationPlanner.IsPlaceKick(snap) || snap.Outcome is SnapOutcome.Punt or SnapOutcome.Kickoff)
                    lastPlayedSnap = snap;
                animating = true; timeout.IsEnabled = false; UpdateOverlay();
                await AnimateSnapAsync(snap);
                if (viewerClosed) return;
                await ShowHighlightAsync(snap);
                if (instantReplays && GameDayLivePresentation.InstantReplay(snap)) await ReplaySnapAsync(snap);
                var period = GameDayAnimationPlanner.PeriodHeadline(snap.Quarter, session.Quarter, session.IsComplete);
                if (period is not null) await ShowHighlightAsync(snap, period);
                var halftime = snap.Quarter < session.Quarter || snap.TimeoutTeamName is not null;
                if (halftime && session.Mode == GameDayMode.Watch) watchPaused = true;
                await Task.Delay((int)((session.Mode == GameDayMode.Watch ? 450 : 80) / gameSpeed));
                animating = false;
                Refresh(snap);
                if (halftime) result.Text = "BREAK · Team → Coach talk or substitutions. Resume watch when ready.";
            }
            finally { animating = false; UpdateOverlay(); finished.TrySetResult(); }
        }
        async Task ReplaySnapAsync(GameSnapEvent snap)
        {
            skipReplayRequested = false; skipReplay.IsVisible = true; replayChip.IsVisible = true;
            highlightText.Text = "INSTANT REPLAY";
            highlightText.FontSize = 30;
            highlight.BackgroundColor = Color.FromArgb("#ED102B40"); highlight.Opacity = 1; highlight.Scale = 1;
            highlight.IsVisible = true;
            await Task.Delay(850);
            highlight.IsVisible = false; highlightText.FontSize = 22;
            try { if (!skipReplayRequested && !viewerClosed) await AnimateSnapAsync(snap, true); }
            finally { skipReplay.IsVisible = false; replayChip.IsVisible = false; FrameCamera(); }
        }
        replayLast.Clicked += async (_, _) =>
        {
            if (animating || lastPlayedSnap is null) return;
            var commentary = result.Text;
            var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            currentSnapFinished = finished.Task;
            animating = true; UpdateOverlay();
            try { await ReplaySnapAsync(lastPlayedSnap); }
            finally { animating = false; result.Text = commentary; UpdateOverlay(); RenderHuddles(); finished.TrySetResult(); }
            if (session.Mode == GameDayMode.Watch && !boxScoreOpen) await WatchLoop();
        };
        async Task FinishIfNeeded()
        {
            if (!session.IsComplete || viewerClosed) return;
            finalizing = true; UpdateOverlay();
            try
            {
                await completion.EnsureSavedAsync(() => CompleteGameDayAsync(session));
                if (viewerClosed || finalPresented) return;
                finalPresented = true;
                var award = GameDayEngine.BoxScore(session).PlayerOfGame;
                if (award is not null)
                {
                    animating = true; UpdateOverlay();
                    highlightText.Text = $"PLAYER OF THE GAME\n{award.PlayerName}\n{award.Summary}";
                    highlightText.FontSize = 18; highlight.BackgroundColor = TeamColor(award.TeamName).WithAlpha(.96f);
                    highlight.Opacity = 1; highlight.Scale = 1; highlight.IsVisible = true;
                    await Task.Delay(2200);
                    if (viewerClosed) return;
                    highlight.IsVisible = false; highlightText.FontSize = 22; animating = false;
                }
                result.Text = award is null ? "Final · result saved" : $"PLAYER OF THE GAME · {award.PlayerName} · {award.Summary}";
                Refresh();
            }
            catch (Exception error)
            {
                System.Diagnostics.Trace.WriteLine(error);
                if (!viewerClosed)
                {
                    result.Text = "Final · save failed. Use Retry save before leaving.";
                    await GameDialogs.AlertAsync(page, "Could not save game", "Your result is still in memory. Tap Retry save to try again.", "OK");
                }
            }
            finally { animating = false; finalizing = false; UpdateOverlay(); }
        }
        async Task CloseViewerAsync()
        {
            if (closingViewer || viewerClosed || skipPending || finalizing) return;
            closingViewer = true; UpdateOverlay();
            try
            {
                await currentSnapFinished;
                async Task PopViewer()
                {
                    viewerClosed = true;
                    fieldPlayers.AbortAnimation("snap-movement");
                    if (Navigation.ModalStack.LastOrDefault() == page) await Navigation.PopModalAsync();
                }
                if (session.IsComplete)
                {
                    await FinishIfNeeded();
                    if (!completion.IsSaved) return;
                    await completion.CloseAsync(() => CompleteGameDayAsync(session), PopViewer);
                }
                else await PopViewer();
            }
            catch (Exception error)
            {
                viewerClosed = false;
                System.Diagnostics.Trace.WriteLine(error);
                await GameDialogs.AlertAsync(page, "Could not close game", "The game view could not close. Please try again.", "OK");
            }
            finally { closingViewer = false; UpdateOverlay(); }
        }
        page.Disappearing += (_, _) => Dispatcher.Dispatch(() =>
        {
            // A box score covers this page too; only detach playback after the viewer itself leaves the stack.
            if (!Navigation.ModalStack.Contains(page))
            { viewerClosed = true; fieldPlayers.AbortAnimation("snap-movement"); }
        });
        async Task DriveGameAsync()
        {
            if (driving || animating || skipPending || viewerClosed || finalizing || closingViewer) return;
            driving = true;
            try
            {
                while (!session.IsComplete && !watchPaused && !boxScoreOpen && !skipPending && !viewerClosed && !closingViewer && page.Parent is not null &&
                    (session.Mode == GameDayMode.Watch || !NeedsUserCall()))
                    await ShowSnapAsync(GameDayEngine.Step(session));
                await FinishIfNeeded();
            }
            finally { driving = false; }
        }
        Task WatchLoop() => DriveGameAsync();
        Task RunCpuUntilUserDecision() => DriveGameAsync();
        if (replay is not null)
        {
            var clipIndex = 0;
            var replayBusy = false;
            var replayClosed = false;
            var replayCompleted = false;
            page.Disappearing += (_, _) => Dispatcher.Dispatch(() => { if (!Navigation.ModalStack.Contains(page)) { replayClosed = true; if (autoReplay && !replayCompleted) _highlightShowCanceled = true; } });
            var replayNext = new Button { Text = "Next highlight" };
            var replayAll = new Button { Text = "Play highlights" };
            var replayClose = new Button { Text = autoReplay ? "Stop highlight show" : "Close highlights" };
            replayAll.IsVisible = replayNext.IsVisible = !autoReplay;
            controls.Children.Clear(); controls.Children.Add(replayAll); controls.Children.Add(replayNext); controls.Children.Add(replayClose);
            playCard.IsVisible = defenseCard.IsVisible = false;
            boxScore.IsVisible = false;
            async Task Clip()
            {
                if (replayBusy || replayClosed || replay.Count == 0) return;
                replayBusy = true; replayAll.IsEnabled = replayNext.IsEnabled = false;
                try
                {
                    var snap = replay[clipIndex++ % replay.Count];
                    session.PossessionTeamName = snap.OffenseTeamName;
                    session.Quarter = snap.Quarter; session.ClockSeconds = snap.ClockSeconds;
                    session.Down = snap.Down; session.Distance = snap.Distance; session.YardLine = snap.StartYardLine;
                    awayScoreLabel.Text = game.AwayScore.ToString(); homeScoreLabel.Text = game.HomeScore.ToString(); awayName.Text = awayTeam.Abbreviation; homeName.Text = homeTeam.Abbreviation;
                    quarterClock.Text = $"Q{snap.Quarter} {snap.ClockSeconds / 60}:{snap.ClockSeconds % 60:D2}";
                    downValue.Text = snap.Down.ToString(); toGoValue.Text = snap.Distance.ToString();
                    ballValue.Text = GameDayEngine.BallOn(session);
                    possession.Text = $"HIGHLIGHT {((clipIndex - 1) % replay.Count) + 1}/{replay.Count} · Recorded play · Final score above";
                    await AnimateSnapAsync(snap);
                    result.Text = GameDayCommentaryService.Describe(snap, session.PlayersById);
                    await ShowHighlightAsync(snap);
                }
                finally { replayBusy = false; replayAll.IsEnabled = replayNext.IsEnabled = true; }
            }
            replayNext.Clicked += async (_, _) => await Clip();
            replayAll.Clicked += async (_, _) => { for (var i = 0; i < replay.Count && !replayClosed; i++) { await Clip(); if (!replayClosed) await Task.Delay(500); } };
            replayClose.Clicked += async (_, _) => { _highlightShowCanceled = true; await Navigation.PopModalAsync(); };
            await Navigation.PushModalAsync(page);
            if (autoReplay)
            {
                for (var i = 0; i < replay.Count && !replayClosed; i++) { await Clip(); if (!replayClosed) await Task.Delay(500); }
                if (!replayClosed) { replayCompleted = true; await Navigation.PopModalAsync(); }
            }
            else await Clip();
            return;
        }
        void ExpandCalls(bool defense)
        {
            if (animating || session.IsComplete) return;
            var choices = defense
                ? availableDefenses.Select(p => new GameDayCallChoice(p.Id, p.Name, GamePlanService.Label(p.Formation),
                    GamePlanService.Label(p.Approach), new PlayArtDrawable(p.Id, p.Name, false, p.Approach.ToString(), defensiveDefinition: p))).ToArray()
                : availablePlays.Select(p => new GameDayCallChoice(p.Id, p.Name, GamePlanService.Label(p.Formation),
                    p.Concept is OffensiveConcept.InsideRun or OffensiveConcept.OutsideRun ? "RUN" : "PASS",
                    new PlayArtDrawable(p.Id, p.Name, true, p.Concept.ToString(), p))).ToArray();
            pickerLayer.Children.Clear();
            pickerLayer.Add(new GameDayCallPicker(defense ? "Choose defense" : "Choose play", choices,
                defense ? selectedDefense.Id : selectedCallId, id =>
                {
                    if (defense) SelectDefense(availableDefenses.Single(p => p.Id == id));
                    else SelectPlay(availablePlays.Single(p => p.Id == id));
                    ClosePicker(); UpdateQuickCalls();
                }, ClosePicker));
            pickerLayer.IsVisible = true;
        }
        choose.Clicked += async (_, _) =>
        {
            if (compactLayout.HasValue) { ExpandCalls(false); return; }
            var choice = await ShowPlayCallSheetAsync(availablePlays, selectedPlay);
            if (choice is not null) { SelectPlay(choice); UpdateQuickCalls(); }
        };
        chooseDefense.Clicked += async (_, _) =>
        {
            if (compactLayout.HasValue) { ExpandCalls(true); return; }
            var choice = await ShowDefensiveCallSheetAsync(availableDefenses, selectedDefense);
            if (choice is not null) { SelectDefense(choice); UpdateQuickCalls(); }
        };
        call.Clicked += async (_, _) =>
        {
            call.IsEnabled = false;
            try { await ShowSnapAsync(GameDayEngine.Step(session, selectedCallId)); await RunCpuUntilUserDecision(); }
            finally { call.IsEnabled = true; }
        };
        callDefense.Clicked += async (_, _) =>
        {
            callDefense.IsEnabled = false;
            try { await ShowSnapAsync(GameDayEngine.Step(session, userDefensivePlayId: selectedDefense.Id)); await RunCpuUntilUserDecision(); }
            finally { callDefense.IsEnabled = true; }
        };
        delegateOffense.Clicked += async (_, _) =>
        {
            coachCallsOffense = !coachCallsOffense; Refresh();
            if (coachCallsOffense) await RunCpuUntilUserDecision();
        };
        delegateDefense.Clicked += async (_, _) =>
        {
            coachCallsDefense = !coachCallsDefense; Refresh();
            if (coachCallsDefense) await RunCpuUntilUserDecision();
        };
        watch.Clicked += async (_, _) =>
        {
            if (session.Mode != GameDayMode.Watch) { session.Mode = GameDayMode.Watch; watchPaused = false; }
            else watchPaused = !watchPaused;
            Refresh();
            if (!animating && !watchPaused) await WatchLoop();
        };
        takeover.Clicked += async (_, _) =>
        {
            if (takingOver) return;
            takingOver = true; session.Mode = GameDayMode.Play; watchPaused = false;
            coachCallsOffense = coachCallsDefense = false;
            takeover.Text = "Taking over after this play";
            await currentSnapFinished;
            takingOver = false; takeover.Text = "Take over"; Refresh();
            await RunCpuUntilUserDecision();
        };
        punt.Clicked += async (_, _) => { await ShowSnapAsync(GameDayEngine.Step(session, "special-punt")); await RunCpuUntilUserDecision(); };
        extraPoint.Clicked += async (_, _) => { await ShowSnapAsync(GameDayEngine.Step(session, "special-extra-point")); await RunCpuUntilUserDecision(); };
        twoPoint.Clicked += async (_, _) => { await ShowSnapAsync(GameDayEngine.Step(session, "special-two-point")); await RunCpuUntilUserDecision(); };
        kneel.Clicked += async (_, _) => { await ShowSnapAsync(GameDayEngine.Step(session, "special-kneel")); await RunCpuUntilUserDecision(); };
        spike.Clicked += async (_, _) => { await ShowSnapAsync(GameDayEngine.Step(session, "special-spike")); await RunCpuUntilUserDecision(); };
        kick.Clicked += async (_, _) => { await ShowSnapAsync(GameDayEngine.Step(session, "special-field-goal")); await RunCpuUntilUserDecision(); };
        timeout.Clicked += async (_, _) =>
        {
            if (!animating && GameDayEngine.TryCallTimeout(session, controlledTeamName))
            { await ShowSnapAsync(session.Events[^1]); }
        };
        async Task GiveTalkAsync() => await ShowGameCoachTalkAsync(page, session, controlledTeamName);
        async Task ShowTeamAsync()
        {
            var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var teamPage = new GameDayViewerPage { BackgroundColor = DynastyTheme.Background };
            async Task Back() { await Navigation.PopModalAsync(); closed.TrySetResult(); }
            teamPage.BackHandler = () => { _ = Back(); return true; };
            var back = new Button { Text = "Return to game" }; back.Clicked += async (_, _) => await Back();
            var talkButton = new Button { Text = "Coach talk" };
            var filter = new Picker { Title = "Position", ItemsSource = Enum.GetNames<Position>(), SelectedIndex = 0 };
            var rows = new VerticalStackLayout { Spacing = 3 };
            var availability = new Label { FontSize = 12, TextColor = DynastyTheme.Muted };
            var showAll = false;
            var expandRoster = new Button { Text = "Show all reserves" };
            var automatic = new Button { Text = "Automatic rotation", FontSize = 12 };
            var secondUnit = new Button { AutomationId = "live-second-unit", FontSize = 13, MinimumHeightRequest = 44 };
            void Draw()
            {
                rows.Children.Clear();
                secondUnit.Text = session.SecondUnits.Contains(controlledTeamName) ? "Return to starters" : "Use second unit";
                secondUnit.IsEnabled = !session.IsComplete;
                var depth = controlledTeamName == game.HomeTeamName ? session.HomeDepthChart : session.AwayDepthChart;
                var fatigue = controlledTeamName == game.HomeTeamName ? session.HomeFatigue : session.AwayFatigue;
                var stats = GameDayEngine.BoxScore(session).Players.ToDictionary(p => p.PlayerId);
                var starters = GameDayCoachingService.Starters(session, controlledTeamName);
                var selectedPosition = Enum.Parse<Position>(filter.SelectedItem?.ToString() ?? "QB");
                var pool = session.Roster.Where(p => p.TeamName == controlledTeamName && p.Position == selectedPosition)
                    .OrderByDescending(p => starters.Contains(p.PlayerId)).ThenBy(p => p.DepthChartOrder <= 0 ? int.MaxValue : p.DepthChartOrder).ThenByDescending(p => p.OverallRating).ToArray();
                availability.Text = GameDayCoachingService.TalkAvailability(session, controlledTeamName);
                var canTalk = availability.Text == "Available";
                talkButton.IsEnabled = canTalk;
                talkButton.BackgroundColor = canTalk ? DynastyTheme.Accent : DynastyTheme.Panel;
                talkButton.TextColor = canTalk ? DynastyTheme.Background : DynastyTheme.Muted;
                talkButton.Opacity = canTalk ? 1 : .55;
                expandRoster.IsVisible = pool.Length > 3;
                expandRoster.Text = showAll ? "Show top three" : $"Show all {pool.Length} players";
                automatic.IsVisible = session.ManualStarters.ContainsKey((controlledTeamName, selectedPosition));
                foreach (var player in pool.Take(showAll ? pool.Length : 3))
                {
                    var morale = GameDayCoachingService.LivePlayerMorale(session, player, GameMoraleService.PlayerMorale(_currentDynasty!, player));
                    var starter = starters.Contains(player.PlayerId);
                    var unavailable = player.IsRedshirted || player.CurrentInjury is not null;
                    var name = new Label { Text = $"#{player.JerseyNumber} {player.FullName}", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = DynastyTheme.Text,
                        LineBreakMode = LineBreakMode.TailTruncation, MaxLines = 1 };
                    var info = new Label { Text = $"OVR {player.OverallRating} · Energy {fatigue.Energy(player.PlayerId):0}% · Morale {morale}", FontSize = 12, TextColor = DynastyTheme.Muted };
                    var sub = new Button { Text = unavailable ? "Unavailable" : starter ? "Starter" : "Sub in", FontSize = 12,
                        IsEnabled = !starter && !unavailable && !session.IsComplete, MinimumHeightRequest = 44, Padding = new Thickness(10, 4),
                        BackgroundColor = starter || unavailable ? DynastyTheme.Panel : DynastyTheme.Accent, TextColor = starter || unavailable ? DynastyTheme.Muted : DynastyTheme.Background };
                    sub.Clicked += async (_, _) =>
                    {
                        var scope = await GameDialogs.ActionSheetAsync(teamPage, $"Start {player.FullName} at {player.Position}", "Cancel", null, "This game only", "Also update season depth chart");
                        if (scope is not ("This game only" or "Also update season depth chart")) return;
                        if (!GameDayCoachingService.Substitute(session, controlledTeamName, player.PlayerId)) return;
                        if (scope == "Also update season depth chart")
                        {
                            _currentDynasty = DepthChartService.Assign(_currentDynasty!, controlledTeamName, player.PlayerId, player.PlayingPositionCode ?? Ncaa06RatingService.PositionCode(player), 1);
                            await SaveCurrentAsync(SaveKind.AutosaveCurrent);
                        }
                        Draw();
                    };
                    var detail = new TapGestureRecognizer();
                    detail.Tapped += async (_, _) =>
                    {
                        var stat = stats.GetValueOrDefault(player.PlayerId);
                        var summary = GameDayCoachingService.LiveReason(session, player);
                        if (player.Position == Position.QB && stat is not null) summary += $"\nPassing {stat.PassYards} yd · TD {stat.PassingTouchdowns} · INT {stat.InterceptionsThrown}";
                        await GameDialogs.AlertAsync(teamPage, player.FullName, summary, "Close");
                    };
                    var text = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center, Children = { name, info } };
                    text.GestureRecognizers.Add(detail);
                    var line = new Grid { Padding = 8, ColumnSpacing = 8, ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(new GridLength(92)) } };
                    line.Add(text, 0, 0); line.Add(sub, 1, 0);
                    rows.Children.Add(new Border { StrokeThickness = 0, BackgroundColor = DynastyTheme.Surface, Content = line });
                }
            }
            talkButton.Clicked += async (_, _) => { await GiveTalkAsync(); Draw(); };
            filter.SelectedIndexChanged += (_, _) => { showAll = false; Draw(); };
            secondUnit.Clicked += async (_, _) =>
            {
                var enabled = !session.SecondUnits.Contains(controlledTeamName);
                if (enabled && !await GameDialogs.AlertAsync(teamPage, "Use second unit?", "Use eligible backups across offense and defense from the next snap, plus a backup kicker where available. Starters fill gaps where backups are unavailable. Season depth chart stays unchanged.", "Use second unit", "Cancel")) return;
                GameDayCoachingService.UseSecondUnit(session, controlledTeamName, enabled); Draw();
            };
            expandRoster.Clicked += (_, _) => { showAll = !showAll; Draw(); };
            automatic.Clicked += (_, _) => { var pos = Enum.Parse<Position>(filter.SelectedItem?.ToString() ?? "QB"); session.ManualStarters.Remove((controlledTeamName, pos)); Draw(); };
            teamPage.Content = new Grid { Padding = 12, RowSpacing = 8, RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star) } };
            var layout = (Grid)teamPage.Content;
            layout.Add(new HorizontalStackLayout { Spacing = 8, Children = { back, talkButton } }, 0, 0);
            layout.Add(new VerticalStackLayout { Spacing = 5, Children = { availability, secondUnit, filter, automatic } }, 0, 1);
            layout.Add(new ScrollView { Content = new VerticalStackLayout { Spacing = 6, Children = { rows, expandRoster } } }, 0, 2);
            Draw(); await Navigation.PushModalAsync(teamPage); await closed.Task;
        }
        async Task OpenViewingMenuAsync(bool team)
        {
            if (boxScoreOpen || closingViewer || finalizing) return;
            boxScoreOpen = true;
            try
            {
                await currentSnapFinished;
                if (team) { await ShowTeamAsync(); return; }
                var selected = await GameDialogs.ActionSheetAsync(page, "GameCast options", "Close", null,
                    "Camera", "Game speed", "Replay speed", "Automatic replays", "Player labels", "Box score", "Weather", "Team & substitutions");
                if (selected == "Camera")
                {
                    var choice = await GameDialogs.ActionSheetAsync(page, "Camera", "Cancel", null, "Full field", "Broadcast", "Follow ball");
                    var index = Array.IndexOf(new[] { "Full field", "Broadcast", "Follow ball" }, choice);
                    if (index >= 0) { cameraMode = index; Microsoft.Maui.Storage.Preferences.Default.Set("live-camera", index); camera.Text = "Camera: " + CameraName(); FrameCamera(); }
                }
                else if (selected is "Game speed" or "Replay speed")
                {
                    var choice = await GameDialogs.ActionSheetAsync(page, selected + " · presentation only", "Cancel", null, "0.5×", "1×", "1.5×", "2×");
                    var index = Array.IndexOf(new[] { "0.5×", "1×", "1.5×", "2×" }, choice);
                    if (index >= 0)
                    {
                        var value = new[] { .5, 1, 1.5, 2 }[index];
                        if (selected == "Game speed") { gameSpeed = value; Microsoft.Maui.Storage.Preferences.Default.Set("live-game-speed", value); }
                        else { replaySpeed = value; Microsoft.Maui.Storage.Preferences.Default.Set("live-replay-speed", value); }
                    }
                }
                else if (selected == "Automatic replays") { instantReplays = !instantReplays; Microsoft.Maui.Storage.Preferences.Default.Set("live-instant-replays", instantReplays); }
                else if (selected == "Player labels") { showCarrierNames = !showCarrierNames; Microsoft.Maui.Storage.Preferences.Default.Set("live-player-labels", showCarrierNames); }
                else if (selected == "Box score") await ShowLiveBoxScoreAsync(GameDayEngine.BoxScore(session));
                else if (selected == "Weather") await GameDialogs.AlertAsync(page, "Conditions", session.Weather.Summary, "OK");
                else if (selected == "Team & substitutions") await ShowTeamAsync();
            }
            finally
            {
                boxScoreOpen = false; Refresh();
                if (!watchPaused) await DriveGameAsync();
            }
        }
        options.Clicked += async (_, _) => await OpenViewingMenuAsync(false);
        teamControl.Clicked += async (_, _) => await OpenViewingMenuAsync(true);
        boxScore.Clicked += async (_, _) =>
        {
            if (boxScoreOpen) return;
            boxScoreOpen = true; controls.IsEnabled = false; boxScore.IsEnabled = false;
            try
            {
                await currentSnapFinished;
                await ShowLiveBoxScoreAsync(GameDayEngine.BoxScore(session));
            }
            finally { boxScoreOpen = false; controls.IsEnabled = true; boxScore.IsEnabled = true; }
            if (session.Mode == GameDayMode.Watch) await WatchLoop(); else await RunCpuUntilUserDecision();
        };
        skip.Clicked += async (_, _) =>
        {
            if (skipPending || viewerClosed || session.IsComplete) return;
            skipPending = true; UpdateOverlay();
            try
            {
                await currentSnapFinished;
                if (viewerClosed) return;
                if (!session.IsComplete)
                {
                    var confirmed = await GameDialogs.AlertAsync(page, "Skip to end?", "Simulate the rest of this game and save the final result?", "Simulate remainder", "Keep playing");
                    if (!confirmed) return;
                    result.Text = "Simulating remaining game…";
                    await Task.Run(() => GameDayEngine.SimulateToEnd(session));
                }
                await FinishIfNeeded();
            }
            catch (Exception error)
            {
                System.Diagnostics.Trace.WriteLine(error);
                if (!viewerClosed) await GameDialogs.AlertAsync(page, "Could not skip game", "The game is still open. Please try again.", "OK");
            }
            finally
            {
                skipPending = false; UpdateOverlay();
                if (!viewerClosed && !session.IsComplete) await DriveGameAsync();
            }
        };
        close.Clicked += async (_, _) => await CloseViewerAsync();
        Refresh();
        await ShowCoinTossAsync(session, controlledTeamName, mode == GameDayMode.Play);
        Refresh();
        await Navigation.PushModalAsync(page);
        if (mode == GameDayMode.Watch) await WatchLoop(); else await RunCpuUntilUserDecision();
    }

    private async Task<OffensivePlayDefinition?> ShowPlayCallSheetAsync(
        IReadOnlyList<OffensivePlayDefinition> plays, OffensivePlayDefinition selected)
    {
        var completion = new TaskCompletionSource<OffensivePlayDefinition?>();
        var modal = new ContentPage { Title = "Call offensive play", BackgroundColor = DynastyTheme.Background };
        var sections = new VerticalStackLayout { Spacing = 16 };
        foreach (var formation in plays.GroupBy(play => play.Formation))
        {
            sections.Children.Add(new Label { Text = GamePlanService.Label(formation.Key).ToUpperInvariant(),
                FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = DynastyTheme.Palette("#E8C979") });
            var grid = new Grid { ColumnSpacing = 10, RowSpacing = 10,
                ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) } };
            var formationPlays = formation.ToArray();
            for (var index = 0; index < formationPlays.Length; index++)
            {
                if (index % 2 == 0) grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                var play = formationPlays[index];
                var type = play.Concept is OffensiveConcept.InsideRun or OffensiveConcept.OutsideRun ? "RUN" : "PASS";
                var badge = new Label { Text = type, FontSize = 9, FontAttributes = FontAttributes.Bold,
                    TextColor = type == "RUN" ? DynastyTheme.Palette("#B9F6CA") : DynastyTheme.Palette("#B8D8FA") };
                var art = new GraphicsView { Drawable = new PlayArtDrawable(play.Id, play.Name, true, play.Concept.ToString(), play),
                    HeightRequest = 105, BackgroundColor = DynastyTheme.Palette("#102A24") };
                var card = new Border { StrokeThickness = play.Id == selected.Id ? 2 : 0,
                    Stroke = DynastyTheme.Accent, Padding = 9, BackgroundColor = DynastyTheme.Palette("#1B2B37"), Content =
                    new VerticalStackLayout { Spacing = 4, Children =
                    {
                        art, badge, new Label { Text = play.Name, FontAttributes = FontAttributes.Bold, FontSize = 13 },
                        new Label { Text = GamePlanService.Label(play.Concept), FontSize = 10, TextColor = DynastyTheme.Palette("#B8D8FA") },
                        new Label { Text = play.Description, FontSize = 10, TextColor = DynastyTheme.Muted, MaxLines = 2 }
                    } } };
                var tap = new TapGestureRecognizer();
                tap.Tapped += async (_, _) => { completion.TrySetResult(play); await Navigation.PopModalAsync(); };
                card.GestureRecognizers.Add(tap);
                grid.Add(card, index % 2, index / 2);
            }
            sections.Children.Add(grid);
        }
        var cancel = new Button { Text = "Cancel" };
        cancel.Clicked += async (_, _) => { completion.TrySetResult(null); await Navigation.PopModalAsync(); };
        modal.Disappearing += (_, _) => Dispatcher.Dispatch(() => { if (!Navigation.ModalStack.Contains(modal)) completion.TrySetResult(null); });
        var header = new Grid { Padding = 12,
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) } };
        header.Add(new Label { Text = "Call offensive play", FontSize = 20, FontAttributes = FontAttributes.Bold,
            VerticalTextAlignment = TextAlignment.Center }, 0, 0);
        header.Add(cancel, 1, 0);
        var body = new Grid { RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Star) } };
        body.Add(header, 0, 0); body.Add(new ScrollView { Content = sections, Margin = new Thickness(12, 0, 12, 12) }, 0, 1);
        modal.Content = body;
        await Navigation.PushModalAsync(modal);
        return await completion.Task;
    }

    private async Task<DefensivePlayDefinition?> ShowDefensiveCallSheetAsync(
        IReadOnlyList<DefensivePlayDefinition> plays, DefensivePlayDefinition selected)
    {
        var completion = new TaskCompletionSource<DefensivePlayDefinition?>();
        var modal = new ContentPage { Title = "Call defensive play", BackgroundColor = DynastyTheme.Background };
        var sections = new VerticalStackLayout { Spacing = 16 };
        foreach (var formation in plays.GroupBy(play => play.Formation))
        {
            sections.Children.Add(new Label { Text = GamePlanService.Label(formation.Key).ToUpperInvariant(),
                FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = DynastyTheme.Palette("#E8C979") });
            var grid = new Grid { ColumnSpacing = 10, RowSpacing = 10,
                ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) } };
            var formationPlays = formation.ToArray();
            for (var index = 0; index < formationPlays.Length; index++)
            {
                if (index % 2 == 0) grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                var play = formationPlays[index];
                var badgeText = play.Approach switch { DefensiveApproach.StopRun => "RUN", DefensiveApproach.ProtectPass => "PASS", _ => "BALANCED" };
                var art = new GraphicsView { Drawable = new PlayArtDrawable(play.Id, play.Name, false, play.Approach.ToString(), defensiveDefinition: play),
                    HeightRequest = 105, BackgroundColor = DynastyTheme.Palette("#102A24") };
                var card = new Border { StrokeThickness = play.Id == selected.Id ? 2 : 0,
                    Stroke = DynastyTheme.Accent, Padding = 9, BackgroundColor = DynastyTheme.Palette("#1B2B37"), Content =
                    new VerticalStackLayout { Spacing = 4, Children =
                    {
                        art, new Label { Text = badgeText, FontSize = 9, FontAttributes = FontAttributes.Bold,
                            TextColor = DynastyTheme.Palette("#F4C97A") },
                        new Label { Text = play.Name, FontAttributes = FontAttributes.Bold, FontSize = 13 },
                        new Label { Text = $"{GamePlanService.Label(play.Personnel)} · {GamePlanService.Label(play.Approach)}",
                            FontSize = 10, TextColor = DynastyTheme.Palette("#B8D8FA") },
                        new Label { Text = play.Description, FontSize = 10, TextColor = DynastyTheme.Muted, MaxLines = 2 }
                    } } };
                var tap = new TapGestureRecognizer();
                tap.Tapped += async (_, _) => { completion.TrySetResult(play); await Navigation.PopModalAsync(); };
                card.GestureRecognizers.Add(tap);
                grid.Add(card, index % 2, index / 2);
            }
            sections.Children.Add(grid);
        }
        var cancel = new Button { Text = "Cancel" };
        cancel.Clicked += async (_, _) => { completion.TrySetResult(null); await Navigation.PopModalAsync(); };
        modal.Disappearing += (_, _) => Dispatcher.Dispatch(() => { if (!Navigation.ModalStack.Contains(modal)) completion.TrySetResult(null); });
        var header = new Grid { Padding = 12,
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) } };
        header.Add(new Label { Text = "Call defensive play", FontSize = 20, FontAttributes = FontAttributes.Bold,
            VerticalTextAlignment = TextAlignment.Center }, 0, 0);
        header.Add(cancel, 1, 0);
        var body = new Grid { RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Star) } };
        body.Add(header, 0, 0); body.Add(new ScrollView { Content = sections, Margin = new Thickness(12, 0, 12, 12) }, 0, 1);
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
        if (completed.GameType == ScheduledGameType.CollegeFootballPlayoff && completed.PostseasonRound == PostseasonRound.NationalChampionship)
            _currentDynasty = CollegeFootballPlayoffService.RecordNationalChampion(_currentDynasty);
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

    private static string Ordinal(int down) => down switch { 1 => "1st", 2 => "2nd", 3 => "3rd", _ => "4th" };

    private sealed class GameFieldDrawable : IDrawable
    {
        public int YardLine { get; set; } = 25;
        public int Distance { get; set; } = 10;
        public int Direction { get; set; } = 1;
        public Color HomeColor { get; set; } = Colors.DarkBlue;
        public Color HomeSecondaryColor { get; set; } = Colors.White;
        public string HomeInitial { get; set; } = "H";
        public string HomeName { get; set; } = "HOME";
        public bool CoinCeremony { get; set; }
        public GameWeather Weather { get; set; } = new();
        public double WeatherPhase { get; set; }

        public void Draw(ICanvas canvas, RectF rect)
        {
            canvas.FillColor = DynastyTheme.Palette("#245B38"); canvas.FillRectangle(rect);
            // Stable procedural turf: mowing bands, subtle blades and five-yard lines.
            for (var band = 0; band < 20; band++)
            {
                canvas.FillColor = Color.FromArgb(band % 2 == 0 ? "#2E6944" : "#285D3D");
                canvas.FillRectangle(rect.Width * (10 + band * 5) / 120f, 0, rect.Width * 5 / 120f, rect.Height);
            }
            for (var blade = 0; blade < 900; blade++)
            {
                var x = rect.Width * ((blade * 73 % 997) / 997f);
                var y = rect.Height * ((blade * 139 % 991) / 991f);
                canvas.FillColor = blade % 2 == 0 ? Color.FromRgba(1, 1, 1, .035) : Color.FromRgba(0, 0, 0, .045);
                canvas.FillRectangle(x, y, 1, 2);
            }
            canvas.StrokeColor = Color.FromRgba(1, 1, 1, .25); canvas.StrokeSize = .7f;
            for (var yard = 5; yard < 100; yard += 5)
            {
                var x = rect.Width * (float)GameDayAnimationPlanner.ProjectYardLine(yard, 1);
                canvas.DrawLine(x, 0, x, rect.Height);
            }
            canvas.FillColor = HomeColor;
            var leftGoal = (float)GameDayAnimationPlanner.ProjectYardLine(0, 1);
            var rightGoal = (float)GameDayAnimationPlanner.ProjectYardLine(100, 1);
            canvas.FillRectangle(0, 0, rect.Width * leftGoal, rect.Height);
            canvas.FillRectangle(rect.Width * rightGoal, 0, rect.Width * leftGoal, rect.Height);
            canvas.SaveState(); canvas.FontColor = HomeSecondaryColor; canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
            canvas.FontSize = Math.Clamp(rect.Width * leftGoal * .35f, 10, 23);
            canvas.Translate(rect.Width * leftGoal * .5f, rect.Height * .5f); canvas.Rotate(-90);
            canvas.DrawString(HomeName, -rect.Height / 2, -rect.Width * leftGoal / 2, rect.Height, rect.Width * leftGoal,
                HorizontalAlignment.Center, VerticalAlignment.Center); canvas.RestoreState();
            canvas.SaveState(); canvas.FontColor = HomeSecondaryColor; canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
            canvas.FontSize = Math.Clamp(rect.Width * leftGoal * .35f, 10, 23);
            canvas.Translate(rect.Width * (rightGoal + leftGoal * .5f), rect.Height * .5f); canvas.Rotate(90);
            canvas.DrawString(HomeName, -rect.Height / 2, -rect.Width * leftGoal / 2, rect.Height, rect.Width * leftGoal,
                HorizontalAlignment.Center, VerticalAlignment.Center); canvas.RestoreState();
            canvas.StrokeColor = Colors.White; canvas.StrokeSize = 2;
            canvas.DrawRectangle(1, 1, rect.Width - 2, rect.Height - 2);
            canvas.DrawLine(rect.Width * leftGoal, 0, rect.Width * leftGoal, rect.Height);
            canvas.DrawLine(rect.Width * rightGoal, 0, rect.Width * rightGoal, rect.Height);
            canvas.StrokeColor = DynastyTheme.Palette("#FFD447"); canvas.StrokeSize = 4;
            var leftPost = rect.Width * .026f;
            var rightPost = rect.Width * .974f;
            var postTop = rect.Height * .41f;
            var postBottom = rect.Height * .59f;
            canvas.DrawLine(0, rect.Center.Y, leftPost, rect.Center.Y);
            canvas.DrawLine(leftPost, postTop, leftPost, postBottom);
            canvas.DrawLine(leftPost, postTop, rect.Width * .046f, postTop);
            canvas.DrawLine(leftPost, postBottom, rect.Width * .046f, postBottom);
            canvas.DrawLine(rect.Width, rect.Center.Y, rightPost, rect.Center.Y);
            canvas.DrawLine(rightPost, postTop, rightPost, postBottom);
            canvas.DrawLine(rightPost, postTop, rect.Width * .954f, postTop);
            canvas.DrawLine(rightPost, postBottom, rect.Width * .954f, postBottom);
            canvas.StrokeColor = DynastyTheme.Palette("#A8CBB5"); canvas.StrokeSize = 1;
            for (var yard = 10; yard < 100; yard += 10)
            {
                var x = rect.Width * (float)GameDayAnimationPlanner.ProjectYardLine(yard, 1);
                canvas.DrawLine(x, 0, x, rect.Height);
                canvas.FontColor = DynastyTheme.Palette("#D5E6DB"); canvas.FontSize = 11;
                canvas.DrawString((yard <= 50 ? yard : 100 - yard).ToString(), x - 10, 8, 24, 18,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
                canvas.DrawString((yard <= 50 ? yard : 100 - yard).ToString(), x - 10, rect.Height - 26, 24, 18,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
            }
            canvas.StrokeColor = DynastyTheme.Palette("#D5E6DB"); canvas.StrokeSize = 1;
            for (var yard = 1; yard < 100; yard++)
            {
                var x = rect.Width * (float)GameDayAnimationPlanner.ProjectYardLine(yard, 1);
                canvas.DrawLine(x, rect.Height * .29f, x, rect.Height * .31f);
                canvas.DrawLine(x, rect.Height * .69f, x, rect.Height * .71f);
            }
            canvas.FillColor = HomeColor;
            canvas.FillCircle(rect.Center.X, rect.Center.Y, Math.Min(rect.Width, rect.Height) * .105f);
            canvas.StrokeColor = HomeSecondaryColor; canvas.StrokeSize = 2;
            canvas.DrawCircle(rect.Center.X, rect.Center.Y, Math.Min(rect.Width, rect.Height) * .105f);
            canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
            canvas.FontSize = Math.Min(46, Math.Min(rect.Width, rect.Height) * .16f) / Math.Max(1, HomeInitial.Length * .6f); canvas.FontColor = HomeSecondaryColor;
            canvas.DrawString(HomeInitial, rect.Width * .40f, rect.Height * .36f, rect.Width * .20f, rect.Height * .28f,
                HorizontalAlignment.Center, VerticalAlignment.Center);
            var line = rect.Width * (float)GameDayAnimationPlanner.ProjectYardLine(YardLine, Direction);
            var gain = rect.Width * (float)GameDayAnimationPlanner.ProjectYardLine(Math.Min(100, YardLine + Distance), Direction);
            canvas.StrokeColor = DynastyTheme.Palette("#4FA8FF"); canvas.StrokeSize = 3; canvas.DrawLine(line, 0, line, rect.Height);
            canvas.StrokeColor = DynastyTheme.Palette("#E7C45B"); canvas.StrokeSize = 3; canvas.DrawLine(gain, 0, gain, rect.Height);
            if (!CoinCeremony)
            {
                var officialX = rect.Width * (float)GameDayAnimationPlanner.ProjectFieldYardLine(Math.Clamp(YardLine - 12, 5, 95), Direction);
                FootballOfficialDrawable.Paint(canvas, officialX, rect.Height * .55f, .7f);
                var sidelineX = rect.Width * (float)GameDayAnimationPlanner.ProjectFieldYardLine(YardLine, Direction);
                FootballOfficialDrawable.Paint(canvas, sidelineX, rect.Height * .045f, .65f);
                FootballOfficialDrawable.Paint(canvas, sidelineX, rect.Height * .955f, .65f);
            }
            if (!Weather.Indoors && Weather.Precipitation != GamePrecipitation.Clear)
            {
                var snow = Weather.Precipitation == GamePrecipitation.Snow;
                canvas.FillColor = Color.FromRgba(1, 1, 1, .75);
                canvas.StrokeColor = Color.FromRgba(.75, .88, 1, .65); canvas.StrokeSize = 1;
                for (var i = 0; i < 100; i++)
                {
                    var x = rect.Width * (float)((i * .618033 + WeatherPhase * .07 * Weather.WindFieldDirection + 10) % 1);
                    var y = rect.Height * (float)((i * .41321 + WeatherPhase * (snow ? .18 : .8)) % 1);
                    if (snow) canvas.FillCircle(x, y, 1.7f);
                    else canvas.DrawLine(x, y, x + Weather.WindFieldDirection * 3, y + 9);
                }
            }
        }
    }
}
