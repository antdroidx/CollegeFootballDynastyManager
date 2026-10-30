using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;
namespace DynastyManager.App;

/// <summary>UI-only inputs for the college and professional hosts. No simulation steps or randomness.</summary>
public sealed class GameCastAnimationContext
{
    public required GameDaySession Session { get; init; }
    public required TeamGamePlan ControlledPlan { get; init; }
    public required AbsoluteLayout Players { get; init; }
    public required GraphicsView Turf { get; init; }
    public required GameFieldDrawable Artwork { get; init; }
    public required Label Commentary { get; init; }
    public required VisualElement Highlight { get; init; }
    public required Dictionary<FootballSpriteView, DynastyPlayer?> SpritePlayers { get; init; }
    public required Dictionary<Border, (DynastyPlayer? Player, Color Primary, Color Secondary, bool Away)> MarkerHosts { get; init; }
    public required HashSet<Guid> ActiveBallOwners { get; init; }
    public required Func<DynastyPlayer?, Color, Color, bool, Border> CreateMarker { get; init; }
    public required Action ClearMarkers { get; init; }
    public required Func<string, Color> TeamPrimary { get; init; }
    public required Func<string, Color> TeamSecondary { get; init; }
    public Func<bool> UseSprites { get; init; } = () => true;
    public Func<bool> ShowCarrierNames { get; init; } = () => true;
    public Func<int> CameraMode { get; init; } = () => 0;
    public Action<double, double> FrameCamera { get; init; } = (_, _) => { };
    public double GameSpeed { get; init; } = 1;
    public double ReplaySpeed { get; init; } = 1;
}

/// <summary>The college GameCast choreography, shared without changing simulation or save models.</summary>
public static class GameCastAnimation
{
    public static async Task PlayAsync(GameCastAnimationContext context, GameSnapEvent snap, bool instantReplay = false)
    {
        var session = context.Session; var game = session.Game;
        var home = session.HomeProfile; var away = session.AwayProfile;
        var controlledPlan = context.ControlledPlan;
        var highlight = context.Highlight; var result = context.Commentary;
        var fieldPlayers = context.Players; var fieldArt = context.Artwork; var fieldLines = context.Turf;
        var spritePlayers = context.SpritePlayers; var markerHosts = context.MarkerHosts;
        var activeBallOwners = context.ActiveBallOwners; var Marker = context.CreateMarker; var TeamColor = context.TeamPrimary;
        var gameSpeed = context.GameSpeed; var replaySpeed = context.ReplaySpeed;

        if (snap.IsOnsideKick) { await Task.Delay(350); return; }
        if (!GameDayEngine.IsScrimmage(snap) && !GameDayAnimationPlanner.IsPlaceKick(snap) && snap.Outcome is not (SnapOutcome.Kickoff or SnapOutcome.Punt)) { await Task.Delay(350); return; }
        highlight.IsVisible = false;
        result.Text = instantReplay ? "REPLAY · " + GameDayCommentaryService.Describe(snap, session.PlayersById)
            : $"LIVE · {snap.OffenseTeamName} · {PlayEditorService.OffensiveLibrary(controlledPlan).FirstOrDefault(p => p.Id == snap.OffensivePlayId)?.Name ?? (snap.Outcome is SnapOutcome.Punt or SnapOutcome.Kickoff ? "Kick and return" : "Play in progress")}";
        fieldPlayers.AbortAnimation("snap-movement");
        fieldPlayers.Children.Clear(); context.ClearMarkers();
        fieldArt.SpottingBall = false;
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
            var player = BackfieldRoles.MarkerRole(offense[index], snap.FullbackId);
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
                case Position.FB:
                    x = line - direction * .055; y = .50; break;
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
                if (player.Position is Position.QB or Position.RB or Position.FB or Position.WR or Position.TE)
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
            var isEligible = playerAssignment?.Task != AssignmentTask.Block && player?.Position is Position.WR or Position.TE or Position.RB or Position.FB;
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
            var view = Marker(player, offenseColor, context.TeamSecondary(snap.OffenseTeamName), offenseIsAway);
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
        var assistIds = LiveFootballChoreography.AssistIds(snap);
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
            var defensiveDestination = LiveFootballChoreography.DefensiveDestination(new(x,y), contact, line, destination, index, primaryDefender, assistingDefender);
            var endX = defensiveDestination.X;
            var endY = defensiveDestination.Y;
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
            var view = Marker(player, defenseColor, context.TeamSecondary(snap.DefenseTeamName), defenseIsAway);
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
            carrierLabel.IsVisible = context.UseSprites() && context.ShowCarrierNames() && labelOwner.HasValue;
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
            if (context.CameraMode() != 0) context.FrameCamera(context.CameraMode() == 2 ? currentBall.X : (centerPoint.X + currentBall.X) / 2, context.CameraMode() == 2 ? currentBall.Y : .5);
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

    static void AddMovement(View view, Rect from, Rect to, List<(View View, Rect From, Rect To)> movements, AbsoluteLayout field)
    {
        AbsoluteLayout.SetLayoutFlags(view, AbsoluteLayoutFlags.PositionProportional);
        AbsoluteLayout.SetLayoutBounds(view, from); field.Add(view); movements.Add((view, from, to));
    }
    static Rect Interpolate(Rect start, Rect end, double progress) => new(
        start.X + (end.X - start.X) * progress, start.Y + (end.Y - start.Y) * progress, start.Width, start.Height);
}
