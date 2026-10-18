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
        field.Add(fieldLines); field.Add(fieldPlayers);

        var controlledProfile = controlledTeamName.Equals(game.HomeTeamName, StringComparison.OrdinalIgnoreCase) ? home : away;
        var controlledPlan = GamePlanService.Normalize(controlledProfile.GamePlan ?? new TeamGamePlan { TeamName = controlledTeamName });
        var availablePlays = GamePlanService.AvailableOffensivePlays(controlledPlan).ToArray();
        var availableDefenses = GamePlanService.AvailableDefensivePlays(controlledPlan).ToArray();
        var selectedPlay = GamePlanService.RecommendOffensivePlay(controlledPlan, session.Down, session.Distance, session.YardLine);
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
            selectedHeading.Text = recommended ? "RECOMMENDED OFFENSIVE CALL" : "SELECTED OFFENSIVE CALL";
            selectedArt.Drawable = new PlayArtDrawable(play.Id, true, play.Concept.ToString());
            selectedName.Text = play.Name;
            selectedDetails.Text = $"{GamePlanService.Label(play.Formation)} · {GamePlanService.Label(play.Personnel)} · {GamePlanService.Label(play.Concept)}";
            selectedDescription.Text = play.Description;
        }
        SelectPlay(selectedPlay, true);

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
            defenseArt.Drawable = new PlayArtDrawable(play.Id, false, play.Approach.ToString());
            defenseName.Text = play.Name;
            defenseDetails.Text = $"{GamePlanService.Label(play.Formation)} · {GamePlanService.Label(play.Personnel)} · {GamePlanService.Label(play.Approach)}";
            defenseDescription.Text = play.Description;
        }
        SelectDefense(selectedDefense, true);

        var call = new Button { Text = "Call play", BackgroundColor = DynastyTheme.Accent, TextColor = DynastyTheme.Background };
        var callDefense = new Button { Text = "Call defense", BackgroundColor = DynastyTheme.Accent, TextColor = DynastyTheme.Background };
        var delegateOffense = new Button { Text = "Offense: You", BackgroundColor = Color.FromArgb("#315D59") };
        var delegateDefense = new Button { Text = "Defense: You", BackgroundColor = Color.FromArgb("#315D59") };
        var watch = new Button { Text = "Watch / Pause" };
        var takeover = new Button { Text = "Take over", IsVisible = false };
        var punt = new Button { Text = "Punt", IsVisible = false };
        var kick = new Button { Text = "Field goal", IsVisible = false };
        var skip = new Button { Text = "Skip to end" };
        var close = new Button { Text = "Close", IsVisible = false };
        var controls = new FlexLayout { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, JustifyContent = FlexJustify.Start };
        foreach (var button in new[] { call, callDefense, punt, kick, delegateOffense, delegateDefense, watch, takeover, skip, close })
        { button.Margin = new Thickness(0, 0, 8, 8); controls.Children.Add(button); }
        page.Content = new ScrollView { Content = new VerticalStackLayout { Padding = 14, Spacing = 10, Children =
        {
            score, situation, possession, field, result, playCard, defenseCard, controls
        } } };

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
            fieldPlayers.AbortAnimation("snap-movement");
            fieldPlayers.Children.Clear();
            fieldArt.YardLine = snap.StartYardLine; fieldArt.Distance = snap.Distance;
            fieldArt.HomeColor = TeamColor(game.HomeTeamName);
            var offenseColor = TeamColor(snap.OffenseTeamName);
            var defenseColor = TeamColor(snap.DefenseTeamName);
            var offenseIsAway = snap.OffenseTeamName.Equals(game.AwayTeamName, StringComparison.OrdinalIgnoreCase);
            var defenseIsAway = snap.DefenseTeamName.Equals(game.AwayTeamName, StringComparison.OrdinalIgnoreCase);
            var direction = (snap.OffenseTeamName == game.HomeTeamName ? 1 : -1) * (snap.Quarter % 2 == 0 ? -1 : 1);
            double Project(double yard) => direction == 1 ? yard / 100d : 1 - yard / 100d;
            var line = Math.Clamp(Project(snap.StartYardLine), .06, .94);
            var ballEndYardLine = snap.BallEndYardLine != 0 || snap.StartYardLine + snap.Yards == 0
                ? snap.BallEndYardLine : Math.Clamp(snap.StartYardLine + snap.Yards, 0, 100);
            var destination = Math.Clamp(Project(ballEndYardLine), .025, .975);
            fieldArt.Direction = direction; fieldLines.Invalidate();
            var movements = new List<(View View, Rect From, Rect To)>();
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
            var receiverSlots = new[] { .10, .23, .77, .90, .84, .16 };
            var lineIndex = 0;
            var receiverIndex = 0;
            var backIndex = 0;
            var terminalCarrierId = snap.BallCarrierId ?? snap.ReturnerId ??
                (snap.Outcome is SnapOutcome.Completion or SnapOutcome.Touchdown ? snap.TargetId : null) ??
                (snap.Outcome == SnapOutcome.Sack ? snap.QuarterbackId : null);
            var isTackle = terminalCarrierId.HasValue &&
                snap.Outcome is SnapOutcome.Run or SnapOutcome.Completion or SnapOutcome.Sack or SnapOutcome.Fumble;
            Rect? qbFrom = null, qbTo = null, runnerFrom = null, runnerTo = null, receiverFrom = null, receiverTo = null,
                terminalCarrierTo = null, kickerFrom = null, kickerTo = null, returnerFrom = null, returnerTo = null;
            View? terminalCarrierView = null, primaryTacklerView = null;
            for (var index = 0; index < offense.Length; index++)
            {
                var player = offense[index];
                double x, y;
                switch (player?.Position)
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
                        y = backIndex++ % 2 == 0 ? .59 : .41;
                        break;
                    case Position.WR:
                        x = line - direction * .018;
                        y = receiverSlots[Math.Min(receiverIndex++, receiverSlots.Length - 1)];
                        break;
                    case Position.TE:
                        x = line - direction * .022;
                        y = receiverIndex++ % 2 == 0 ? .69 : .31;
                        break;
                    default:
                        x = line - direction * (.04 + (index % 3) * .02);
                        y = .20 + (index % 7) * .10;
                        break;
                }
                x = Math.Clamp(x, .025, .96);
                var endY = Math.Clamp(y + RouteLane(player, index, snap) * .35, .07, .93);
                var endX = Math.Clamp(x + (destination - line + direction * .045) * RouteShare(player, snap), .025, .96);
                if (player?.PlayerId == terminalCarrierId) endX = destination;
                var view = Marker(player, offenseColor, offenseIsAway);
                if (player is not null && (snap.BallCarrierId == player.PlayerId ||
                    snap.Outcome is SnapOutcome.Completion or SnapOutcome.Touchdown && snap.TargetId == player.PlayerId))
                { view.Stroke = Colors.Yellow; view.StrokeThickness = 3; }
                var from = new Rect(x, y, 19, 19);
                var to = new Rect(endX, endY, 19, 19);
                if (player?.PlayerId == snap.QuarterbackId) { qbFrom = from; qbTo = to; }
                if (player?.PlayerId == snap.BallCarrierId) { runnerFrom = from; runnerTo = to; }
                if (player?.PlayerId == snap.TargetId) { receiverFrom = from; receiverTo = to; }
                if (player?.PlayerId == terminalCarrierId) { terminalCarrierTo = to; terminalCarrierView = view; }
                if (player?.PlayerId == snap.KickerId) { kickerFrom = from; kickerTo = to; }
                if (player?.PlayerId == snap.ReturnerId) { returnerFrom = from; returnerTo = to; }
                AddMovement(view, from, to, movements, fieldPlayers);
            }
            var contact = GameDayAnimationPlanner.ContactPoint(snap, direction, terminalCarrierTo?.Y ?? .50);
            var assistCount = snap.Outcome switch
            {
                SnapOutcome.Sack => 0,
                SnapOutcome.Run when snap.Yards < 0 => 1,
                SnapOutcome.Run or SnapOutcome.Completion or SnapOutcome.Fumble => snap.Yards >= 8 ? 2 : 1,
                _ => 0
            };
            var assistIds = defense.Where(player => player is not null && player.PlayerId != snap.PrimaryDefenderId)
                .OrderBy(player => player!.Position is Position.MLB or Position.OLB or Position.FS or Position.SS ? 0 : 1)
                .ThenBy(player => player!.PlayerId)
                .Take(assistCount).Select(player => player!.PlayerId).ToHashSet();
            var defensiveLine = 0;
            var linebackers = 0;
            var secondary = 0;
            for (var index = 0; index < defense.Length; index++)
            {
                var player = defense[index];
                double x, y;
                switch (player?.Position)
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
                var primaryDefender = isTackle && player?.PlayerId == snap.PrimaryDefenderId;
                var assistingDefender = isTackle && player is not null && assistIds.Contains(player.PlayerId);
                var hasPursuitTarget = primaryDefender || assistingDefender;
                var pursuitTarget = GameDayAnimationPlanner.PursuitTarget(contact, index, primaryDefender);
                var endX = hasPursuitTarget ? pursuitTarget.X : Math.Clamp(x + (destination - line) * .72, .025, .96);
                var endY = hasPursuitTarget ? pursuitTarget.Y : Math.Clamp(y + ((index % 3) - 1) * .025, .07, .93);
                var view = Marker(player, defenseColor, defenseIsAway);
                if (hasPursuitTarget) pursuingDefenders.Add(view);
                if (primaryDefender)
                {
                    view.Stroke = Colors.Yellow; view.StrokeThickness = 3;
                    primaryTacklerView = view;
                }
                var from = new Rect(x, y, 19, 19);
                var to = new Rect(endX, endY, 19, 19);
                if (player?.PlayerId == snap.KickerId) { kickerFrom = from; kickerTo = to; }
                if (player?.PlayerId == snap.ReturnerId)
                {
                    returnerFrom = from; returnerTo = to;
                    terminalCarrierTo ??= to; terminalCarrierView ??= view;
                }
                AddMovement(view, from, to, movements, fieldPlayers);
            }
            var ball = new Border { WidthRequest = 14, HeightRequest = 9, BackgroundColor = Color.FromArgb("#8B4D24"),
                Stroke = Color.FromArgb("#F5D8B5"), StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 5 } };
            // Ball starts on the center, goes to the QB, then follows the handoff
            // or pass target. Its position is computed from the same player movement.
            var center = movements.Where(m => m.From.Y >= .35 && m.From.Y <= .65 &&
                Math.Abs(m.From.X - (line - direction * .016)) < .01)
                .OrderBy(m => Math.Abs(m.From.Y - .50)).FirstOrDefault();
            var centerPoint = center.View is null ? new Rect(line, .50, 14, 9) : center.From;
            Rect BallAt(Rect player) => new(Math.Clamp(player.X + .006, .02, .97),
                Math.Clamp(player.Y + .006, .02, .97), 14, 9);
            var isPass = snap.TargetId.HasValue && !snap.BallCarrierId.HasValue;
            var isSack = snap.Outcome == SnapOutcome.Sack;
            var caught = snap.Outcome is SnapOutcome.Completion or SnapOutcome.Touchdown;
            var isKick = snap.Outcome is SnapOutcome.Kickoff or SnapOutcome.Punt or SnapOutcome.FieldGoal or SnapOutcome.MissedFieldGoal;
            var ballStart = BallAt(centerPoint);
            AbsoluteLayout.SetLayoutFlags(ball, AbsoluteLayoutFlags.PositionProportional);
            AbsoluteLayout.SetLayoutBounds(ball, ballStart);
            fieldPlayers.Add(ball);
            var completion = new TaskCompletionSource();
            var animation = new Animation(progress =>
            {
                foreach (var movement in movements)
                {
                    var movementProgress = pursuingDefenders.Contains(movement.View)
                        ? GameDayAnimationPlanner.PursuitProgress(progress, ReferenceEquals(movement.View, primaryTacklerView))
                        : progress;
                    AbsoluteLayout.SetLayoutBounds(movement.View, Interpolate(movement.From, movement.To, movementProgress));
                }
                Rect Track(Rect? from, Rect? to, double fraction) =>
                    from is Rect a && to is Rect b ? Interpolate(a, b, fraction) : centerPoint;
                Rect currentBall;
                if (isKick && snap.Outcome is SnapOutcome.Kickoff or SnapOutcome.Punt)
                {
                    var launch = kickerFrom ?? qbFrom ?? centerPoint;
                    if (progress < .65)
                    {
                        var landing = returnerFrom ?? new Rect(destination, .50, 14, 9);
                        currentBall = Interpolate(BallAt(launch), BallAt(landing), progress / .65);
                        currentBall = currentBall with { Y = Math.Clamp(currentBall.Y - Math.Sin(progress / .65 * Math.PI) * .18, .02, .97) };
                    }
                    else if (returnerFrom.HasValue)
                        currentBall = BallAt(Track(returnerFrom, returnerTo, (progress - .65) / .35));
                    else currentBall = new Rect(destination, .50, 14, 9);
                }
                else if (progress < .13)
                    currentBall = Interpolate(ballStart, BallAt(Track(qbFrom, qbTo, 0)), progress / .13);
                else if (isKick)
                    currentBall = Interpolate(BallAt(Track(qbFrom, qbTo, 0)),
                        new Rect(destination, .5, 14, 9), (progress - .13) / .87);
                else if (isSack)
                    currentBall = BallAt(Track(qbFrom, qbTo, progress));
                else if (isPass && progress < .53)
                    currentBall = BallAt(Track(qbFrom, qbTo, (progress - .13) / .87));
                else if (isPass && receiverFrom.HasValue)
                {
                    var receiver = Track(receiverFrom, receiverTo, progress);
                    currentBall = caught
                        ? progress < .67
                            ? Interpolate(BallAt(Track(qbFrom, qbTo, .45)), BallAt(receiver), (progress - .53) / .14)
                            : BallAt(receiver)
                        : Interpolate(BallAt(Track(qbFrom, qbTo, .45)),
                            new Rect(destination, .25, 14, 9), (progress - .53) / .47);
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
            animation.Commit(fieldPlayers, "snap-movement", 16, session.Mode == GameDayMode.Watch ? 2800u : 2200u,
                Easing.CubicInOut, (_, _) => completion.TrySetResult());
            await completion.Task;
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
            downValue.Text = session.KickoffPending ? "—" : Ordinal(session.Down);
            toGoValue.Text = session.KickoffPending ? "KICKOFF" : session.Distance.ToString();
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
                    SelectPlay(GamePlanService.RecommendOffensivePlay(controlledPlan, session.Down, session.Distance, session.YardLine), true);
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
            defenseCard.IsVisible = callDefense.IsVisible;
            delegateOffense.IsVisible = delegateDefense.IsVisible = session.Mode == GameDayMode.Play && !session.IsComplete;
            delegateOffense.Text = coachCallsOffense ? "Offense: Coach" : "Offense: You";
            delegateDefense.Text = coachCallsDefense ? "Defense: Coach" : "Defense: You";
            watch.IsVisible = !session.IsComplete;
            watch.Text = session.Mode == GameDayMode.Watch ? "Pause watch" : "Watch CPU";
            takeover.IsVisible = session.Mode == GameDayMode.Watch && !session.IsComplete;
            punt.IsVisible = kick.IsVisible = awaitingOffense && session.Down == 4;
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
            try { await ShowSnapAsync(GameDayEngine.Step(session, selectedPlay.Id)); await RunCpuUntilUserDecision(); }
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
        kick.Clicked += async (_, _) => { await ShowSnapAsync(GameDayEngine.Step(session, "special-field-goal")); await RunCpuUntilUserDecision(); };
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
                var art = new GraphicsView { Drawable = new PlayArtDrawable(play.Id, true, play.Concept.ToString()),
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
                var art = new GraphicsView { Drawable = new PlayArtDrawable(play.Id, false, play.Approach.ToString()),
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
        public int Direction { get; set; } = 1;
        public Color HomeColor { get; set; } = Colors.DarkBlue;
        public Color HomeSecondaryColor { get; set; } = Colors.White;
        public string HomeInitial { get; set; } = "H";

        public void Draw(ICanvas canvas, RectF rect)
        {
            canvas.FillColor = Color.FromArgb("#245B38"); canvas.FillRectangle(rect);
            canvas.FillColor = HomeColor;
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
                canvas.DrawString((yard <= 50 ? yard : 100 - yard).ToString(), x - 10, rect.Height - 26, 24, 18,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
            }
            canvas.StrokeColor = Color.FromArgb("#D5E6DB"); canvas.StrokeSize = 1;
            for (var yard = 5; yard < 100; yard += 5)
            {
                var x = rect.Width * yard / 100f;
                canvas.DrawLine(x, rect.Height * .25f, x, rect.Height * .29f);
                canvas.DrawLine(x, rect.Height * .71f, x, rect.Height * .75f);
            }
            canvas.FillColor = HomeColor;
            canvas.FillCircle(rect.Center.X, rect.Center.Y, Math.Min(rect.Width, rect.Height) * .105f);
            canvas.FontSize = 42; canvas.FontColor = HomeSecondaryColor;
            canvas.DrawString(HomeInitial, rect.Width * .40f, rect.Height * .36f, rect.Width * .20f, rect.Height * .28f,
                HorizontalAlignment.Center, VerticalAlignment.Center);
            var line = rect.Width * (Direction == 1 ? YardLine : 100 - YardLine) / 100f;
            var gain = rect.Width * (Direction == 1 ? Math.Min(100, YardLine + Distance) :
                100 - Math.Min(100, YardLine + Distance)) / 100f;
            canvas.StrokeColor = Color.FromArgb("#4FA8FF"); canvas.StrokeSize = 3; canvas.DrawLine(line, 0, line, rect.Height);
            canvas.StrokeColor = Color.FromArgb("#E7C45B"); canvas.StrokeSize = 3; canvas.DrawLine(gain, 0, gain, rect.Height);
        }
    }
}
