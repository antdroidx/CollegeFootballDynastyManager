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
        if (controlledTeamName is null && mode == GameDayMode.Play && replay is null && !game.InvolvesTeam(_currentDynasty.UserTeamName))
        {
            controlledTeamName = await GameDialogs.ActionSheetAsync(this, "Choose team to control", "Cancel", null, game.AwayTeamName, game.HomeTeamName);
            if (controlledTeamName is null or "Cancel") return;
        }
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
        var careerTeam = controlledTeamName == _currentDynasty.UserTeamName;
        var controlsOffense = !careerTeam || UserCoachControlService.ControlsOffense(_currentDynasty);
        var controlsDefense = !careerTeam || UserCoachControlService.ControlsDefense(_currentDynasty);
        session.UserCoachRole = careerTeam ? _currentDynasty.UserCoachRole : StaffRole.HeadCoach;
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
        void FrameCamera(double x = .5, double y = .5) => GameCastCamera.Apply(
            [fieldLines, fieldPlayers], field.Width, field.Height, cameraMode, x, y);
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
                label.Text = showNumbers && player?.JerseyNumber is int number ? number.ToString() : player?.Position.DisplayName() ?? "•";
                label.FontSize = showNumbers ? 8 : 6;
            }
            foreach (var (sprite, player) in spritePlayers)
                sprite.SetLabel(showNumbers && player?.JerseyNumber is int jersey ? jersey.ToString() : player?.Position.DisplayName() ?? "");
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
        Grid TeamScore(string team, Label name, Label value, Label timeouts) => GameCastScoreboard.Team(TeamBadge(team), name, value, timeouts);
        var scoreHeader = GameCastScoreboard.Create(
            TeamScore(game.AwayTeamName, awayName, awayScoreLabel, awayTimeouts),
            TeamScore(game.HomeTeamName, homeName, homeScoreLabel, homeTimeouts));
        var options = new Button { Text = "Options", AutomationId = "game-day-options" };
        var teamControl = new Button { Text = "Team", AutomationId = "game-day-team" };
        var skip = new Button { Text = "Skip to end" };
        var close = new Button { Text = "Close", IsVisible = false };
        var controls = new ActionRow { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, JustifyContent = FlexJustify.Start };
        foreach (var button in new[] { call, callDefense, punt, kick, extraPoint, twoPoint, kneel, spike, onside, delegateOffense, delegateDefense, watch, takeover, options, teamControl, gameInfo, boxScore, timeout, replayLast, camera, replaySpeedButton, skipReplay, numbers, spriteMode, replayToggle, carrierToggle, skip, close })
        { button.Margin = new Thickness(0, 0, 8, 8); controls.Children.Add(button); }
        var coachCallsOffense = !controlsOffense;
        var coachCallsDefense = !controlsDefense;
        var quickCalls = new Grid { ColumnSpacing = 6,
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) } };
        var expand = new Button { Text = "Choose play" };
        controls.Children.Insert(1, expand);
        var more = new Button { Text = "More", AutomationId = "game-day-more" };
        controls.Children.Add(more);
        var tray = new VerticalStackLayout { Spacing = 8, Children = { quickCalls, controls } };
        var trayScroll = new ScrollView { Content = tray, MaximumHeightRequest = 260 };
        var traySurface = new Border { AutomationId = "game-day-controls", StrokeThickness = 0, Padding = 7,
            BackgroundColor = DynastyTheme.Surface, Content = trayScroll,
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
            delegateOffense.IsEnabled = controlsOffense; delegateDefense.IsEnabled = controlsDefense;
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
            // Leave room for the play cards and two complete action rows in portrait.
            trayScroll.MaximumHeightRequest = compact ? 190 : 260;
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
                var sprite = new FootballSpriteView(color, secondary, awayTeam, PortraitAppearance.ForPlayer(player?.PlayerId ?? Guid.Empty, player?.Appearance).Skin,
                    showNumbers && player?.JerseyNumber is int jersey ? jersey.ToString() : player?.Position.DisplayName() ?? "",PortraitAppearance.ForPlayer(player?.PlayerId??Guid.Empty,player?.Appearance).SkinColorHex);
                spritePlayers[sprite] = player;
                marker.Stroke = Colors.Transparent; marker.StrokeThickness = 0; marker.BackgroundColor = Colors.Transparent;
                marker.StrokeShape = new RoundRectangle { CornerRadius = 0 };
                sprite.Highlight(player is not null && activeBallOwners.Contains(player.PlayerId));
                marker.Content = sprite;
            }
            else
            {
                var label = new Label { Text = showNumbers && player?.JerseyNumber is int jersey ? jersey.ToString() : player?.Position.DisplayName() ?? "•",
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
            if (player is not null) SemanticProperties.SetDescription(marker, $"{player.Position.DisplayName()} {player.FullName}");
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
            fieldArt.SpottingBall = !session.KickoffPending;
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
        Task AnimateSnapAsync(GameSnapEvent snap, bool instantReplay = false) => GameCastAnimation.PlayAsync(new()
        {
            Session = session, ControlledPlan = controlledPlan, Players = fieldPlayers, Artwork = fieldArt,
            Turf = fieldLines, Commentary = result, Highlight = highlight, SpritePlayers = spritePlayers,
            MarkerHosts = markerHosts, ActiveBallOwners = activeBallOwners, CreateMarker = Marker,
            ClearMarkers = () => { markerLabels.Clear(); spritePlayers.Clear(); markerHosts.Clear(); activeBallOwners.Clear(); },
            TeamPrimary = TeamColor, TeamSecondary = team => Color.FromArgb(TeamBrandCatalog.Get(team).Secondary),
            UseSprites = () => useSprites, ShowCarrierNames = () => showCarrierNames, CameraMode = () => cameraMode,
            FrameCamera = FrameCamera, GameSpeed = gameSpeed, ReplaySpeed = replaySpeed
        }, snap, instantReplay);
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
            if (!controlsOffense) return;
            coachCallsOffense = !coachCallsOffense; Refresh();
            if (coachCallsOffense) await RunCpuUntilUserDecision();
        };
        delegateDefense.Clicked += async (_, _) =>
        {
            if (!controlsDefense) return;
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
            coachCallsOffense = !controlsOffense; coachCallsDefense = !controlsDefense;
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
            var filter = new Picker { Title = "Position", ItemsSource = Enum.GetValues<Position>().Where(p => UserCoachControlService.ControlsPosition(session.UserCoachRole, p)).Select(p => p.DisplayName()).ToArray(), SelectedIndex = 0 };
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
                var selectedPosition = PositionLabels.Parse(filter.SelectedItem?.ToString() ?? "QB");
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
                        IsEnabled = !starter && !unavailable && !session.IsComplete && UserCoachControlService.ControlsPosition(session.UserCoachRole, player.Position), MinimumHeightRequest = 44, Padding = new Thickness(10, 4),
                        BackgroundColor = starter || unavailable ? DynastyTheme.Panel : DynastyTheme.Accent, TextColor = starter || unavailable ? DynastyTheme.Muted : DynastyTheme.Background };
                    sub.Clicked += async (_, _) =>
                    {
                        var scope = await GameDialogs.ActionSheetAsync(teamPage, $"Start {player.FullName} at {player.Position.DisplayName()}", "Cancel", null, "This game only", "Also update season depth chart");
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
                if (enabled && !await GameDialogs.AlertAsync(teamPage, "Use second unit?", session.UserCoachRole == StaffRole.HeadCoach ? "Use eligible backups across offense and defense from the next snap, plus a backup kicker where available. Starters fill gaps where backups are unavailable. Season depth chart stays unchanged." : "Use eligible backups on your side from the next snap. The other side and season depth chart stay unchanged.", "Use second unit", "Cancel")) return;
                GameDayCoachingService.UseSecondUnit(session, controlledTeamName, enabled); Draw();
            };
            expandRoster.Clicked += (_, _) => { showAll = !showAll; Draw(); };
            automatic.Clicked += (_, _) => { var pos = PositionLabels.Parse(filter.SelectedItem?.ToString() ?? "QB"); session.ManualStarters.Remove((controlledTeamName, pos)); Draw(); };
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

    private static string Ordinal(int down) => down switch { 1 => "1st", 2 => "2nd", 3 => "3rd", _ => "4th" };

}
