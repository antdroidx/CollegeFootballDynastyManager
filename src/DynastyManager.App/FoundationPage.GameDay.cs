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
        if (_currentDynasty is null || !GameDayEngine.IsAvailable(game, _currentDynasty.SeasonYear, _currentDynasty.Week, _currentDynasty.Phase)) return;
        controlledTeamName ??= game.InvolvesTeam(_currentDynasty.UserTeamName)
            ? _currentDynasty.UserTeamName : game.HomeTeamName;
        var profiles = BuildCurrentSimulationProfiles();
        if (!profiles.TryGetValue(game.HomeTeamName, out var home) || !profiles.TryGetValue(game.AwayTeamName, out var away)) return;
        home = GamePlanService.Attach(_currentDynasty, _teamsByName[game.HomeTeamName], home);
        away = GamePlanService.Attach(_currentDynasty, _teamsByName[game.AwayTeamName], away);
        var session = GameDayEngine.Start(game, home, away, _currentDynasty.ActiveRoster, mode);
        session.ControlledTeamName = controlledTeamName;
        var animating = false;

        if (mode == GameDayMode.Simulate)
        {
            GameDayEngine.SimulateToEnd(session);
            await CompleteGameDayAsync(session);
            return;
        }

        var page = new ContentPage { BackgroundColor = DynastyTheme.Background };
        var score = new Label { FontSize = 22, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center };
        var quarterClock = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center };
        var downValue = new Label { FontSize = 22, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center };
        var toGoValue = new Label { FontSize = 22, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center };
        var ballValue = new Label { FontSize = 22, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center };
        Border SituationCell(string heading, Label value) => new() { StrokeThickness = 0, Padding = 7,
            BackgroundColor = Color.FromArgb("#172733"), Content = new VerticalStackLayout { Spacing = 1, Children =
            { new Label { Text = heading, FontSize = 9, TextColor = DynastyTheme.Muted, HorizontalTextAlignment = TextAlignment.Center }, value } } };
        var situation = new Grid { ColumnSpacing = 7,
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) } };
        situation.Add(SituationCell("QUARTER / CLOCK", quarterClock), 0, 0);
        situation.Add(SituationCell("DOWN", downValue), 1, 0);
        situation.Add(SituationCell("YARDS TO GO", toGoValue), 2, 0);
        situation.Add(SituationCell("BALL ON", ballValue), 3, 0);
        var possession = new Label { FontSize = 11, HorizontalTextAlignment = TextAlignment.Center, TextColor = Color.FromArgb("#E8C979") };
        var result = new Label { HorizontalTextAlignment = TextAlignment.Center, FontSize = 14 };
        var homeBrand = TeamBrandCatalog.Get(game.HomeTeamName);
        var fieldArt = new GameFieldDrawable
        {
            HomeColor = Color.FromArgb(homeBrand.Primary),
            HomeSecondaryColor = Color.FromArgb(homeBrand.Secondary),
            HomeInitial = TeamBrandCatalog.Initial(game.HomeTeamName)
        };
        var field = new Grid { HeightRequest = 330, BackgroundColor = Color.FromArgb("#245B38") };
        var fieldLines = new GraphicsView { Drawable = fieldArt };
        var fieldPlayers = new AbsoluteLayout();
        var highlightText = new Label { FontSize = 22, FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center };
        var highlight = new Border { IsVisible = false, StrokeThickness = 2, Stroke = Colors.White,
            Padding = new Thickness(22, 12), HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center, Content = highlightText };
        field.Add(fieldLines); field.Add(fieldPlayers); field.Add(highlight);

        var controlledProfile = controlledTeamName.Equals(game.HomeTeamName, StringComparison.OrdinalIgnoreCase) ? home : away;
        var controlledPlan = GamePlanService.Normalize(controlledProfile.GamePlan ?? new TeamGamePlan { TeamName = controlledTeamName });
        var availablePlays = GamePlanService.AvailableOffensivePlays(controlledPlan).ToArray();
        var availableDefenses = GamePlanService.AvailableDefensivePlays(controlledPlan).ToArray();
        var selectedPlay = GamePlanService.RecommendOffensivePlay(controlledPlan, session.Down, session.Distance, session.YardLine);
        var selectedCallId = selectedPlay.Id;
        var call = new Button { Text = "Call play", BackgroundColor = DynastyTheme.Accent, TextColor = DynastyTheme.Background };
        var selectedDefense = GamePlanService.RecommendDefensivePlay(controlledPlan, session.Down, session.Distance);
        var selectedArt = new GraphicsView { HeightRequest = 112, WidthRequest = 185, BackgroundColor = Color.FromArgb("#102A24") };
        var selectedHeading = new Label { Text = "RECOMMENDED OFFENSIVE CALL", FontSize = 9, TextColor = Color.FromArgb("#E8C979") };
        var selectedName = new Label { FontAttributes = FontAttributes.Bold, FontSize = 15 };
        var selectedDetails = new Label { FontSize = 11, TextColor = Color.FromArgb("#B8D8FA") };
        var selectedDescription = new Label { FontSize = 11, TextColor = DynastyTheme.Muted };
        var choose = new Button { Text = "Choose play", BackgroundColor = Color.FromArgb("#315D59") };
        var playInfo = new VerticalStackLayout { Spacing = 4, Children = { selectedHeading, selectedName, selectedDetails, selectedDescription, choose } };
        var playGrid = new Grid { ColumnSpacing = 12,
            ColumnDefinitions = new ColumnDefinitionCollection { new(new GridLength(185)), new(GridLength.Star) } };
        playGrid.Add(selectedArt, 0, 0); playGrid.Add(playInfo, 1, 0);
        var playCard = new Border { StrokeThickness = 0, Padding = 10,
            BackgroundColor = Color.FromArgb("#1B2B37"), Content = playGrid };
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

        var defenseArt = new GraphicsView { HeightRequest = 112, WidthRequest = 185, BackgroundColor = Color.FromArgb("#102A24") };
        var defenseHeading = new Label { Text = "RECOMMENDED DEFENSIVE CALL", FontSize = 9, TextColor = Color.FromArgb("#E8C979") };
        var defenseName = new Label { FontAttributes = FontAttributes.Bold, FontSize = 15 };
        var defenseDetails = new Label { FontSize = 11, TextColor = Color.FromArgb("#B8D8FA") };
        var defenseDescription = new Label { FontSize = 11, TextColor = DynastyTheme.Muted };
        var chooseDefense = new Button { Text = "Choose defense", BackgroundColor = Color.FromArgb("#315D59") };
        var defenseInfo = new VerticalStackLayout { Spacing = 4, Children =
            { defenseHeading, defenseName, defenseDetails, defenseDescription, chooseDefense } };
        var defenseGrid = new Grid { ColumnSpacing = 12,
            ColumnDefinitions = new ColumnDefinitionCollection { new(new GridLength(185)), new(GridLength.Star) } };
        defenseGrid.Add(defenseArt, 0, 0); defenseGrid.Add(defenseInfo, 1, 0);
        var defenseCard = new Border { StrokeThickness = 0, Padding = 10,
            BackgroundColor = Color.FromArgb("#1B2B37"), Content = defenseGrid, IsVisible = false };
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
        var delegateOffense = new Button { Text = "Offense: You", BackgroundColor = Color.FromArgb("#315D59") };
        var delegateDefense = new Button { Text = "Defense: You", BackgroundColor = Color.FromArgb("#315D59") };
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
        var skip = new Button { Text = "Skip to end" };
        var close = new Button { Text = "Close", IsVisible = false };
        var controls = new FlexLayout { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, JustifyContent = FlexJustify.Start };
        foreach (var button in new[] { call, callDefense, punt, kick, extraPoint, twoPoint, kneel, spike, onside, delegateOffense, delegateDefense, watch, takeover, timeout, skip, close })
        { button.Margin = new Thickness(0, 0, 8, 8); controls.Children.Add(button); }
        bool? compactLayout = null;
        void ArrangeViewer(bool compact)
        {
            if (compactLayout == compact) return;
            compactLayout = compact;
            page.Content = null;
            foreach (var view in new View[] { score, situation, possession, field, result, playCard, defenseCard, controls })
                if (view.Parent is Layout parent) parent.Children.Remove(view);
            foreach (var button in controls.Children.OfType<Button>())
            {
                button.FontSize = compact ? 10 : 13;
                button.Padding = compact ? new Thickness(7, 4) : new Thickness(12, 8);
                button.MinimumHeightRequest = compact ? 30 : 42;
                button.Margin = compact ? new Thickness(0, 0, 4, 4) : new Thickness(0, 0, 8, 8);
            }
            score.FontSize = compact ? 16 : 22;
            downValue.FontSize = toGoValue.FontSize = ballValue.FontSize = compact ? 17 : 22;
            result.FontSize = compact ? 11 : 14;
            selectedDescription.IsVisible = defenseDescription.IsVisible = !compact;
            selectedArt.HeightRequest = defenseArt.HeightRequest = compact ? 68 : 112;
            selectedArt.WidthRequest = defenseArt.WidthRequest = compact ? 92 : 185;
            playGrid.ColumnDefinitions[0].Width = new GridLength(selectedArt.IsVisible ? compact ? 92 : 185 : 0);
            defenseGrid.ColumnDefinitions[0].Width = new GridLength(compact ? 92 : 185);
            playCard.Padding = defenseCard.Padding = compact ? 6 : 10;
            if (!compact)
            {
                field.HeightRequest = 330;
                page.Content = new ScrollView { Content = new VerticalStackLayout { Padding = 14, Spacing = 10,
                    Children = { score, situation, possession, field, result, playCard, defenseCard, controls } } };
                return;
            }
            field.HeightRequest = -1;
            var heading = new Grid { ColumnSpacing = 10, ColumnDefinitions =
                new ColumnDefinitionCollection { new(new GridLength(2, GridUnitType.Star)), new(new GridLength(3, GridUnitType.Star)) } };
            heading.Add(new VerticalStackLayout { Spacing = 1, Children = { score, possession } }, 0, 0);
            heading.Add(situation, 1, 0);
            var body = new Grid { ColumnSpacing = 8, ColumnDefinitions =
                new ColumnDefinitionCollection { new(new GridLength(2.2, GridUnitType.Star)), new(GridLength.Star) } };
            body.Add(field, 0, 0);
            body.Add(new ScrollView { Content = new VerticalStackLayout { Spacing = 5,
                Children = { playCard, defenseCard, controls } } }, 1, 0);
            var root = new Grid { Padding = 8, RowSpacing = 5, RowDefinitions =
                new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
            root.Add(heading, 0, 0); root.Add(body, 0, 1); root.Add(result, 0, 2);
            page.Content = root;
        }
        ArrangeViewer(false);
        page.SizeChanged += (_, _) =>
        {
            if (page.Width > 0 && page.Height > 0) ArrangeViewer(page.Width > page.Height);
        };

        Color TeamColor(string teamName) => Color.FromArgb(TeamBrandCatalog.Get(teamName).Primary);
        Border Marker(DynastyPlayer? player, Color color, bool awayTeam)
        {
            var marker = new Border { WidthRequest = 19, HeightRequest = 19,
                StrokeShape = new RoundRectangle { CornerRadius = 10 }, BackgroundColor = awayTeam ? Colors.White : color,
                Stroke = awayTeam ? color : Colors.White, StrokeThickness = awayTeam ? 2.2 : 1.4,
                Content = new Label { Text = player?.Position.ToString() ?? "•", FontSize = 6, FontAttributes = FontAttributes.Bold,
                    TextColor = awayTeam ? color : Colors.White, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center } };
            if (player is not null) SemanticProperties.SetDescription(marker, $"{player.Position} {player.FullName}");
            return marker;
        }
        async Task AnimateSnapAsync(GameSnapEvent snap)
        {
            if (snap.IsOnsideKick) { await Task.Delay(350); return; }
            if (!GameDayEngine.IsScrimmage(snap) && snap.Outcome is not (SnapOutcome.Kickoff or SnapOutcome.Punt or SnapOutcome.FieldGoal or SnapOutcome.MissedFieldGoal)) { await Task.Delay(350); return; }
            highlight.IsVisible = false;
            result.Text = string.Empty;
            fieldPlayers.AbortAnimation("snap-movement");
            fieldPlayers.Children.Clear();
            fieldArt.YardLine = snap.StartYardLine; fieldArt.Distance = snap.Distance;
            fieldArt.HomeColor = TeamColor(game.HomeTeamName);
            var offenseColor = TeamColor(snap.OffenseTeamName);
            var defenseColor = TeamColor(snap.DefenseTeamName);
            var offenseIsAway = snap.OffenseTeamName.Equals(game.AwayTeamName, StringComparison.OrdinalIgnoreCase);
            var defenseIsAway = snap.DefenseTeamName.Equals(game.AwayTeamName, StringComparison.OrdinalIgnoreCase);
            var direction = (snap.OffenseTeamName == game.HomeTeamName ? 1 : -1) * (snap.Quarter % 2 == 0 ? -1 : 1);
            double Project(double yard) => GameDayAnimationPlanner.ProjectYardLine(yard, direction);
            var line = Project(snap.StartYardLine);
            var kickLanding = Math.Clamp(Project(snap.KickLandingYardLine > 0
                ? snap.KickLandingYardLine : snap.BallEndYardLine), .02, .98);
            var ballEndYardLine = snap.BallEndYardLine != 0 || snap.StartYardLine + snap.Yards == 0
                ? snap.BallEndYardLine : Math.Clamp(snap.StartYardLine + snap.Yards, 0, 100);
            var destination = Project(ballEndYardLine);
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
                var normalStroke = view.Stroke;
                var normalThickness = view.StrokeThickness;
                markerHighlights[player.PlayerId] = active =>
                {
                    view.Stroke = active ? Colors.Yellow : normalStroke;
                    view.StrokeThickness = active ? 3 : normalThickness;
                };
            }
            var pursuingDefenders = new HashSet<View>();
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
            var lineIndex = 0;
            var receiverIndex = 0;
            var tightEndIndex = 0;
            var backIndex = 0;
            var routeEligibleIndex = 0;
            var calledPlay = PlayEditorService.OffensiveLibrary((snap.OffenseTeamName == game.HomeTeamName ? home : away).GamePlan ?? controlledPlan).FirstOrDefault(play => play.Id == snap.OffensivePlayId);
            var routeFamily = calledPlay is null ? GameDayRouteFamily.Run : GameDayAnimationPlanner.RouteFamily(calledPlay);
            var receiverFamily = routeFamily;
            var passRelease = snap.PassReleaseProgress > 0 ? snap.PassReleaseProgress : calledPlay is null ? .54 : GameDayAnimationPlanner.PassReleaseProgress(calledPlay);
            var catchProgress = GameDayAnimationPlanner.CatchProgress(passRelease);
            var terminalCarrierId = snap.BallCarrierId ?? snap.ReturnerId ??
                (snap.Outcome is SnapOutcome.Completion or SnapOutcome.Touchdown ? snap.TargetId : null) ??
                (snap.Outcome == SnapOutcome.Sack ? snap.QuarterbackId : null);
            var isReturnTackle = (snap.Outcome is SnapOutcome.Kickoff or SnapOutcome.Punt) &&
                snap.ReturnYards > 0 && !snap.IsFairCatch && !snap.IsTouchback;
            var isTackle = terminalCarrierId.HasValue && (isReturnTackle ||
                snap.Outcome is SnapOutcome.Run or SnapOutcome.Scramble or SnapOutcome.Completion or SnapOutcome.Sack or SnapOutcome.Fumble);
            var isFieldGoalSnap = snap.Outcome is SnapOutcome.FieldGoal or SnapOutcome.MissedFieldGoal;
            var specialContact = GameDayAnimationPlanner.ContactPoint(snap, direction, .50);
            Rect? qbFrom = null, qbTo = null, runnerFrom = null, runnerTo = null, receiverFrom = null, receiverTo = null,
                terminalCarrierTo = null, kickerFrom = null, kickerTo = null, returnerFrom = null, returnerTo = null;
            View? terminalCarrierView = null, primaryTacklerView = null, specialReturnerView = null;
            for (var index = 0; index < offense.Length; index++)
            {
                var player = offense[index];
                double x, y;
                if (player?.PlayerId == snap.KickerId && snap.Outcome is
                    SnapOutcome.Kickoff or SnapOutcome.Punt or SnapOutcome.FieldGoal or SnapOutcome.MissedFieldGoal)
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
                if (player is not null && calledPlay is not null &&
                    snap.Outcome is not (SnapOutcome.Punt or SnapOutcome.Kickoff or SnapOutcome.FieldGoal or SnapOutcome.MissedFieldGoal))
                {
                    roleCounts.TryGetValue(player.Position, out var roleIndex);
                    roleCounts[player.Position] = roleIndex + 1;
                    if (player.Position is Position.QB or Position.RB or Position.WR or Position.TE)
                    {
                        var custom = calledPlay.Assignments.FirstOrDefault(a => a.Position == player.Position && a.RoleIndex == roleIndex);
                        var alignment = custom is null ? GameDayAnimationPlanner.Alignment(calledPlay.Formation, player.Position, roleIndex) : new GameFieldPoint(custom.Depth, custom.Lane);
                        x = Project(snap.StartYardLine + alignment.X); y = alignment.Y;
                    }
                    else if (player.Position == Position.OL)
                        x = Project(snap.StartYardLine + GameDayAnimationPlanner.Alignment(calledPlay.Formation, Position.OL, roleIndex).X);
                }
                x = Math.Clamp(x, .025, .96);
                var isEligible = player?.Position is Position.WR or Position.TE or Position.RB;
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
                if (player?.PlayerId == terminalCarrierId) endX = destination;
                var view = Marker(player, offenseColor, offenseIsAway);
                RegisterMarker(player, view);
                assignedRoutes[view] = assigned;
                if (isEligible && calledPlay is not null && routeFamily != GameDayRouteFamily.Run &&
                    snap.Outcome is not (SnapOutcome.Kickoff or SnapOutcome.Punt or SnapOutcome.FieldGoal or SnapOutcome.MissedFieldGoal))
                    routeViews.Add(view);
                var from = new Rect(x, y, 19, 19);
                var puntCoverageTackler = isReturnTackle && snap.Outcome == SnapOutcome.Punt &&
                    player?.PlayerId == snap.PrimaryDefenderId;
                var to = puntCoverageTackler
                    ? new Rect(specialContact.X, specialContact.Y, 19, 19)
                    : new Rect(endX, endY, 19, 19);
                if (puntCoverageTackler)
                {
                    pursuingDefenders.Add(view);
                    primaryTacklerView = view;
                }
                if (player?.PlayerId == snap.QuarterbackId) { qbFrom = from; qbTo = to; }
                if (player?.PlayerId == snap.BallCarrierId) { runnerFrom = from; runnerTo = to; }
                if (player?.PlayerId == snap.TargetId) { receiverFrom = from; receiverTo = to; receiverFamily = assigned; }
                if (player?.PlayerId == terminalCarrierId) { terminalCarrierTo = to; terminalCarrierView = view; }
                if (player?.PlayerId == snap.KickerId) { kickerFrom = from; kickerTo = to; }
                if (player?.PlayerId == snap.ReturnerId)
                {
                    returnerFrom = from; returnerTo = to; specialReturnerView = view;
                }
                AddMovement(view, from, to, movements, fieldPlayers);
            }
            GameFieldPoint? interceptionPoint = snap.Outcome == SnapOutcome.Interception && receiverFrom is Rect rf && receiverTo is Rect rt
                ? GameDayAnimationPlanner.RoutePoint(new(rf.X, rf.Y), new(rt.X, rt.Y), receiverFamily, catchProgress) : null;
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
            var assistIds = defense.Where(player => player is not null && player.PlayerId != snap.PrimaryDefenderId)
                .OrderBy(player => player!.Position is Position.MLB or Position.OLB or Position.FS or Position.SS ? 0 : 1)
                .ThenBy(player => player!.PlayerId)
                .Take(assistCount).Select(player => player!.PlayerId).ToHashSet();
            var defensivePlan = (snap.DefenseTeamName == game.HomeTeamName ? home : away).GamePlan ?? controlledPlan;
            var calledDefensivePlay = PlayEditorService.DefensiveLibrary(defensivePlan).FirstOrDefault(p => p.Id == snap.DefensivePlayId);
            var defensiveRoles = new Dictionary<Position, int>();
            var defensiveLine = 0;
            var linebackers = 0;
            var secondary = 0;
            for (var index = 0; index < defense.Length; index++)
            {
                var player = defense[index];
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
                if (player is not null && calledDefensivePlay is not null && !isFieldGoalSnap && snap.Outcome != SnapOutcome.Punt)
                {
                    var role = defensiveRoles.GetValueOrDefault(player.Position); defensiveRoles[player.Position] = role + 1;
                    var assignment = calledDefensivePlay.Assignments.FirstOrDefault(a => a.Position == player.Position && a.RoleIndex == role);
                    if (assignment is not null) { x = Project(snap.StartYardLine + assignment.Depth); y = assignment.Lane; }
                }
                var primaryDefender = isTackle && player?.PlayerId == snap.PrimaryDefenderId;
                var assistingDefender = isTackle && player is not null && assistIds.Contains(player.PlayerId);
                var hasPursuitTarget = primaryDefender || assistingDefender;
                var pursuitTarget = GameDayAnimationPlanner.PursuitTarget(contact, index, primaryDefender);
                var endX = hasPursuitTarget ? pursuitTarget.X : Math.Clamp(x + (destination - line) * .72, .025, .96);
                var endY = hasPursuitTarget ? pursuitTarget.Y : Math.Clamp(y + ((index % 3) - 1) * .025, .07, .93);
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
                var view = Marker(player, defenseColor, defenseIsAway);
                RegisterMarker(player, view);
                if (hasPursuitTarget) pursuingDefenders.Add(view);
                if (primaryDefender)
                {
                    primaryTacklerView = view;
                }
                var from = new Rect(x, y, 19, 19);
                var to = new Rect(endX, endY, 19, 19);
                if (interceptionPoint.HasValue && player?.PlayerId == snap.PrimaryDefenderId)
                { interceptorView = view; interceptorFrom = from; interceptorTo = to; }
                if (player?.PlayerId == snap.KickerId) { kickerFrom = from; kickerTo = to; }
                if (player?.PlayerId == snap.ReturnerId)
                {
                    returnerFrom = from; returnerTo = to;
                    terminalCarrierTo ??= to; terminalCarrierView ??= view; specialReturnerView = view;
                }
                AddMovement(view, from, to, movements, fieldPlayers);
            }
            var ball = new Border { WidthRequest = 14, HeightRequest = 9, BackgroundColor = Color.FromArgb("#8B4D24"),
                Stroke = Color.FromArgb("#F5D8B5"), StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 5 } };
            // Ball starts on the center, goes to the QB, then follows the handoff
            // or pass target. Its position is computed from the same player movement.
            var center = movements.Where(m => m.From.Y >= .35 && m.From.Y <= .65 &&
                Math.Abs(m.From.X - Project(snap.StartYardLine - .25)) < .01)
                .OrderBy(m => Math.Abs(m.From.Y - .50)).FirstOrDefault();
            var centerPoint = center.View is null ? new Rect(line, .50, 14, 9) : center.From;
            Rect BallAt(Rect player) => new(Math.Clamp(player.X + .006, .02, .97),
                Math.Clamp(player.Y + .006, .02, .97), 14, 9);
            var isPass = snap.TargetId.HasValue && !snap.BallCarrierId.HasValue;
            var isSack = snap.Outcome == SnapOutcome.Sack;
            var caught = snap.Outcome is SnapOutcome.Completion or SnapOutcome.Touchdown;
            var isKick = snap.Outcome is SnapOutcome.Kickoff or SnapOutcome.Punt or SnapOutcome.FieldGoal or SnapOutcome.MissedFieldGoal;
            var isFieldGoal = snap.Outcome is SnapOutcome.FieldGoal or SnapOutcome.MissedFieldGoal;
            Rect RouteTrack(Rect from, Rect to, double progress, GameDayRouteFamily? assigned = null)
            {
                var point = GameDayAnimationPlanner.RoutePoint(new(from.X, from.Y), new(to.X, to.Y), assigned ?? routeFamily, progress);
                return new Rect(point.X, point.Y, from.Width, from.Height);
            }
            var ballStart = BallAt(centerPoint);
            AbsoluteLayout.SetLayoutFlags(ball, AbsoluteLayoutFlags.PositionProportional);
            AbsoluteLayout.SetLayoutBounds(ball, ballStart);
            fieldPlayers.Add(ball);
            var durationSeconds = GameDayAnimationPlanner.PlaybackDurationSeconds(movements.Select(m =>
            {
                var from = new GameFieldPoint(m.From.X, m.From.Y);
                var to = new GameFieldPoint(m.To.X, m.To.Y);
                var distance = routeViews.Contains(m.View) ? GameDayAnimationPlanner.RouteLengthYards(from, to, assignedRoutes.GetValueOrDefault(m.View, routeFamily))
                    : GameDayAnimationPlanner.DistanceYards(from, to);
                var active = ReferenceEquals(m.View, specialReturnerView) &&
                    snap.Outcome is SnapOutcome.Kickoff or SnapOutcome.Punt ? .35 : ReferenceEquals(m.View, interceptorView) ? catchProgress : 1;
                return (distance, movementSpeeds[m.View], active);
            }));
            var completion = new TaskCompletionSource();
            var animation = new Animation(progress =>
            {
                foreach (var movement in movements)
                {
                    var movementProgress = ReferenceEquals(movement.View, interceptorView) ? Math.Min(1, progress / catchProgress) : isKick && (snap.Outcome is SnapOutcome.Kickoff or SnapOutcome.Punt) &&
                        ReferenceEquals(movement.View, specialReturnerView)
                        ? GameDayAnimationPlanner.ReturnProgress(progress)
                        : pursuingDefenders.Contains(movement.View)
                        ? GameDayAnimationPlanner.PursuitProgress(progress, ReferenceEquals(movement.View, primaryTacklerView))
                        : progress;
                    var position = routeViews.Contains(movement.View)
                        ? RouteTrack(movement.From, movement.To, movementProgress, assignedRoutes.GetValueOrDefault(movement.View, routeFamily))
                        : Interpolate(movement.From, movement.To, movementProgress);
                    AbsoluteLayout.SetLayoutBounds(movement.View, position);
                }
                var owner = GameDayAnimationPlanner.BallOwner(snap, progress, passRelease);
                if (owner != highlightedOwner)
                {
                    if (highlightedOwner is Guid old && markerHighlights.TryGetValue(old, out var clear)) clear(false);
                    if (owner is Guid current && markerHighlights.TryGetValue(current, out var mark)) mark(true);
                    highlightedOwner = owner;
                }
                Rect Track(Rect? from, Rect? to, double fraction) =>
                    from is Rect a && to is Rect b ? Interpolate(a, b, fraction) : centerPoint;
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
                    else currentBall = new Rect(destination, .50, 14, 9);
                }
                else if (isFieldGoal)
                {
                    var launch = kickerFrom ?? qbFrom ?? centerPoint;
                    var flight = Math.Clamp((progress - .20) / .80, 0, 1);
                    var goalX = direction == 1 ? .975 : .025;
                    var goalY = snap.Outcome == SnapOutcome.FieldGoal ? .50 : snap.Sequence % 2 == 0 ? .28 : .72;
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
                else if (snap.Outcome == SnapOutcome.Interception && interceptorTo is Rect interceptionSpot)
                {
                    var launch = Track(qbFrom, qbTo, passRelease);
                    var caughtAt = GameDayAnimationPlanner.InterceptionBallPoint(new(launch.X, launch.Y),
                        new(interceptionSpot.X, interceptionSpot.Y), progress, passRelease);
                    currentBall = BallAt(new Rect(caughtAt.X, caughtAt.Y, 19, 19));
                }
                else if (isPass && receiverFrom.HasValue)
                {
                    var receiver = RouteTrack(receiverFrom.Value, receiverTo ?? receiverFrom.Value, progress, receiverFamily);
                    currentBall = caught
                        ? progress < catchProgress
                            ? Interpolate(BallAt(Track(qbFrom, qbTo, passRelease)), BallAt(receiver),
                                (progress - passRelease) / (catchProgress - passRelease))
                            : BallAt(receiver)
                        : Interpolate(BallAt(Track(qbFrom, qbTo, passRelease)),
                            new Rect(destination, .25, 14, 9), (progress - passRelease) / (1 - passRelease));
                }
                else if (runnerFrom.HasValue)
                    currentBall = progress < .25
                        ? Interpolate(BallAt(Track(qbFrom, qbTo, 0)), BallAt(Track(runnerFrom, runnerTo, .25)), (progress - .13) / .12)
                        : BallAt(Track(runnerFrom, runnerTo, progress));
                else currentBall = BallAt(Track(qbFrom, qbTo, progress));
                AbsoluteLayout.SetLayoutBounds(ball, currentBall);
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
            var headline = period ?? GameDayAnimationPlanner.OutcomeHeadline(snap.Outcome);
            if (headline is null) return;
            var team = snap.Outcome is SnapOutcome.Interception or SnapOutcome.Fumble
                ? snap.DefenseTeamName : snap.OffenseTeamName;
            highlightText.Text = period is null ? $"{headline}\n{team}" : $"{headline}\n{game.AwayTeamName} {session.AwayScore} - {session.HomeScore} {game.HomeTeamName}";
            highlight.BackgroundColor = TeamColor(team).WithAlpha(.94f);
            highlight.Opacity = 0;
            highlight.Scale = .72;
            highlight.IsVisible = true;
            var completion = new TaskCompletionSource();
            new Animation(progress =>
            {
                highlight.Opacity = progress < .25 ? progress / .25 : progress > .82 ? (1 - progress) / .18 : 1;
                highlight.Scale = .72 + Math.Min(1, progress / .30) * .28 + Math.Sin(progress * Math.PI * 2) * .025;
            }).Commit(highlight, "game-highlight", 16, 1250, Easing.CubicOut,
                (_, _) => completion.TrySetResult());
            await completion.Task;
            highlight.IsVisible = false;
        }
        var coachCallsOffense = false;
        var coachCallsDefense = false;
        bool ControlledOnOffense() => session.PossessionTeamName.Equals(controlledTeamName, StringComparison.OrdinalIgnoreCase);
        bool NeedsUserCall() => session.Mode == GameDayMode.Play && !session.IsComplete && !session.KickoffPending &&
            (ControlledOnOffense() ? !coachCallsOffense : !coachCallsDefense);
        void Refresh(GameSnapEvent? snap = null)
        {
            score.Text = $"{game.AwayTeamName} {session.AwayScore}  ·  {session.HomeScore} {game.HomeTeamName}";
            quarterClock.Text = $"Q{session.Quarter}  {TimeSpan.FromSeconds(session.ClockSeconds):m\\:ss}";
            if (session.OvertimeRound > 0) quarterClock.Text = $"OT {session.OvertimeRound} · untimed";
            downValue.Text = session.KickoffPending ? "—" : Ordinal(session.Down);
            toGoValue.Text = session.KickoffPending ? "KICKOFF" : session.Distance.ToString();
            if (session.TryPending) { downValue.Text = "TRY"; toGoValue.Text = "3"; }
            ballValue.Text = session.KickoffPending ? "RECEIVE" : session.YardLine.ToString();
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
            watch.Text = session.Mode == GameDayMode.Watch ? "Pause watch" : "Watch CPU";
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
            timeout.IsEnabled = !animating && !session.KickoffPending && !session.ClockStopped && remaining > 0;
        }
        async Task ShowSnapAsync(GameSnapEvent snap)
        {
            animating = true; timeout.IsEnabled = false;
            await AnimateSnapAsync(snap);
            await ShowHighlightAsync(snap);
            var period = GameDayAnimationPlanner.PeriodHeadline(snap.Quarter, session.Quarter, session.IsComplete);
            if (period is not null) await ShowHighlightAsync(snap, period);
            animating = false;
            Refresh(snap);
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
        async Task RunCpuUntilUserDecision()
        {
            while (!session.IsComplete && session.Mode == GameDayMode.Play && !NeedsUserCall())
                await ShowSnapAsync(GameDayEngine.Step(session));
            Refresh();
            await FinishIfNeeded();
        }
        choose.Clicked += async (_, _) =>
        {
            var choice = await ShowPlayCallSheetAsync(availablePlays, selectedPlay);
            if (choice is not null) SelectPlay(choice);
        };
        chooseDefense.Clicked += async (_, _) =>
        {
            var choice = await ShowDefensiveCallSheetAsync(availableDefenses, selectedDefense);
            if (choice is not null) SelectDefense(choice);
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
            session.Mode = session.Mode == GameDayMode.Watch ? GameDayMode.Play : GameDayMode.Watch;
            Refresh();
            if (session.Mode == GameDayMode.Watch) await WatchLoop();
            else await RunCpuUntilUserDecision();
        };
        takeover.Clicked += async (_, _) => { session.Mode = GameDayMode.Play; Refresh(); await RunCpuUntilUserDecision(); };
        punt.Clicked += async (_, _) => { await ShowSnapAsync(GameDayEngine.Step(session, "special-punt")); await RunCpuUntilUserDecision(); };
        extraPoint.Clicked += async (_, _) => { await ShowSnapAsync(GameDayEngine.Step(session, "special-extra-point")); await RunCpuUntilUserDecision(); };
        twoPoint.Clicked += async (_, _) => { await ShowSnapAsync(GameDayEngine.Step(session, "special-two-point")); await RunCpuUntilUserDecision(); };
        kneel.Clicked += async (_, _) => { await ShowSnapAsync(GameDayEngine.Step(session, "special-kneel")); await RunCpuUntilUserDecision(); };
        spike.Clicked += async (_, _) => { await ShowSnapAsync(GameDayEngine.Step(session, "special-spike")); await RunCpuUntilUserDecision(); };
        kick.Clicked += async (_, _) => { await ShowSnapAsync(GameDayEngine.Step(session, "special-field-goal")); await RunCpuUntilUserDecision(); };
        timeout.Clicked += (_, _) =>
        {
            if (!animating && GameDayEngine.TryCallTimeout(session, controlledTeamName))
            { result.Text = $"{controlledTeamName} calls timeout."; Refresh(); }
        };
        skip.Clicked += async (_, _) => { GameDayEngine.SimulateToEnd(session); await FinishIfNeeded(); };
        close.Clicked += async (_, _) => await Navigation.PopModalAsync();
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
                FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#E8C979") });
            var grid = new Grid { ColumnSpacing = 10, RowSpacing = 10,
                ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) } };
            var formationPlays = formation.ToArray();
            for (var index = 0; index < formationPlays.Length; index++)
            {
                if (index % 2 == 0) grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                var play = formationPlays[index];
                var type = play.Concept is OffensiveConcept.InsideRun or OffensiveConcept.OutsideRun ? "RUN" : "PASS";
                var badge = new Label { Text = type, FontSize = 9, FontAttributes = FontAttributes.Bold,
                    TextColor = type == "RUN" ? Color.FromArgb("#B9F6CA") : Color.FromArgb("#B8D8FA") };
                var art = new GraphicsView { Drawable = new PlayArtDrawable(play.Id, play.Name, true, play.Concept.ToString(), play),
                    HeightRequest = 105, BackgroundColor = Color.FromArgb("#102A24") };
                var card = new Border { StrokeThickness = play.Id == selected.Id ? 2 : 0,
                    Stroke = DynastyTheme.Accent, Padding = 9, BackgroundColor = Color.FromArgb("#1B2B37"), Content =
                    new VerticalStackLayout { Spacing = 4, Children =
                    {
                        art, badge, new Label { Text = play.Name, FontAttributes = FontAttributes.Bold, FontSize = 13 },
                        new Label { Text = GamePlanService.Label(play.Concept), FontSize = 10, TextColor = Color.FromArgb("#B8D8FA") },
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
        modal.Disappearing += (_, _) => completion.TrySetResult(null);
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
                FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#E8C979") });
            var grid = new Grid { ColumnSpacing = 10, RowSpacing = 10,
                ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) } };
            var formationPlays = formation.ToArray();
            for (var index = 0; index < formationPlays.Length; index++)
            {
                if (index % 2 == 0) grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                var play = formationPlays[index];
                var badgeText = play.Approach switch { DefensiveApproach.StopRun => "RUN", DefensiveApproach.ProtectPass => "PASS", _ => "BALANCED" };
                var art = new GraphicsView { Drawable = new PlayArtDrawable(play.Id, play.Name, false, play.Approach.ToString(), defensiveDefinition: play),
                    HeightRequest = 105, BackgroundColor = Color.FromArgb("#102A24") };
                var card = new Border { StrokeThickness = play.Id == selected.Id ? 2 : 0,
                    Stroke = DynastyTheme.Accent, Padding = 9, BackgroundColor = Color.FromArgb("#1B2B37"), Content =
                    new VerticalStackLayout { Spacing = 4, Children =
                    {
                        art, new Label { Text = badgeText, FontSize = 9, FontAttributes = FontAttributes.Bold,
                            TextColor = Color.FromArgb("#F4C97A") },
                        new Label { Text = play.Name, FontAttributes = FontAttributes.Bold, FontSize = 13 },
                        new Label { Text = $"{GamePlanService.Label(play.Personnel)} · {GamePlanService.Label(play.Approach)}",
                            FontSize = 10, TextColor = Color.FromArgb("#B8D8FA") },
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
        modal.Disappearing += (_, _) => completion.TrySetResult(null);
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

        public void Draw(ICanvas canvas, RectF rect)
        {
            canvas.FillColor = Color.FromArgb("#245B38"); canvas.FillRectangle(rect);
            canvas.FillColor = HomeColor;
            var leftGoal = (float)GameDayAnimationPlanner.ProjectYardLine(0, 1);
            var rightGoal = (float)GameDayAnimationPlanner.ProjectYardLine(100, 1);
            canvas.FillRectangle(0, 0, rect.Width * leftGoal, rect.Height);
            canvas.FillRectangle(rect.Width * rightGoal, 0, rect.Width * leftGoal, rect.Height);
            canvas.StrokeColor = Colors.White; canvas.StrokeSize = 2;
            canvas.DrawLine(rect.Width * leftGoal, 0, rect.Width * leftGoal, rect.Height);
            canvas.DrawLine(rect.Width * rightGoal, 0, rect.Width * rightGoal, rect.Height);
            canvas.StrokeColor = Color.FromArgb("#FFD447"); canvas.StrokeSize = 4;
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
            canvas.StrokeColor = Color.FromArgb("#A8CBB5"); canvas.StrokeSize = 1;
            for (var yard = 10; yard < 100; yard += 10)
            {
                var x = rect.Width * (float)GameDayAnimationPlanner.ProjectYardLine(yard, 1);
                canvas.DrawLine(x, 0, x, rect.Height);
                canvas.FontColor = Color.FromArgb("#D5E6DB"); canvas.FontSize = 11;
                canvas.DrawString((yard <= 50 ? yard : 100 - yard).ToString(), x - 10, 8, 24, 18,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
                canvas.DrawString((yard <= 50 ? yard : 100 - yard).ToString(), x - 10, rect.Height - 26, 24, 18,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
            }
            canvas.StrokeColor = Color.FromArgb("#D5E6DB"); canvas.StrokeSize = 1;
            for (var yard = 5; yard < 100; yard += 5)
            {
                var x = rect.Width * (float)GameDayAnimationPlanner.ProjectYardLine(yard, 1);
                canvas.DrawLine(x, rect.Height * .25f, x, rect.Height * .29f);
                canvas.DrawLine(x, rect.Height * .71f, x, rect.Height * .75f);
            }
            canvas.FillColor = HomeColor;
            canvas.FillCircle(rect.Center.X, rect.Center.Y, Math.Min(rect.Width, rect.Height) * .105f);
            canvas.FontSize = 42; canvas.FontColor = HomeSecondaryColor;
            canvas.DrawString(HomeInitial, rect.Width * .40f, rect.Height * .36f, rect.Width * .20f, rect.Height * .28f,
                HorizontalAlignment.Center, VerticalAlignment.Center);
            var line = rect.Width * (float)GameDayAnimationPlanner.ProjectYardLine(YardLine, Direction);
            var gain = rect.Width * (float)GameDayAnimationPlanner.ProjectYardLine(Math.Min(100, YardLine + Distance), Direction);
            canvas.StrokeColor = Color.FromArgb("#4FA8FF"); canvas.StrokeSize = 3; canvas.DrawLine(line, 0, line, rect.Height);
            canvas.StrokeColor = Color.FromArgb("#E7C45B"); canvas.StrokeSize = 3; canvas.DrawLine(gain, 0, gain, rect.Height);
        }
    }
}
