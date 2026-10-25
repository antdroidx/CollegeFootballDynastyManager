using DynastyManager.App;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Microsoft.Maui.Controls.Shapes;
using ProFootball.Core;
namespace ProFootball.App;
// Thin host over the shared simulation, renderer, animation planner and sprite artwork.
public sealed class ProLivePage : ContentPage
{
    readonly GameDaySession session; readonly ProLeague league; readonly Func<Task> save;
    readonly Label score = new() { FontSize = 21, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center };
    readonly Label situation = new() { HorizontalTextAlignment = TextAlignment.Center }; readonly Label commentary = new() { FontSize = 14 };
    readonly AbsoluteLayout field = new() { HeightRequest = 260, BackgroundColor = Colors.DarkGreen };
    readonly GameFieldDrawable art; readonly GraphicsView turf; readonly Button next = new() { Text = "Next snap" }; readonly Button pause = new() { Text = "Pause watch" };
    bool active, working, pending, paused, saved, second, initialized; double speed = 1, replaySpeed = .5; string? offense, defense; GameSnapEvent? last;
    readonly Label selected = new() { Text = "CPU recommended calls" };
    public ProLivePage(GameDaySession session, ProLeague league, Func<Task> save)
    {
        this.session = session; this.league = league; this.save = save; BackgroundColor = DynastyTheme.Background;
        var home = league.Teams.Single(t => t.Name == session.Game.HomeTeamName);
        art = new() { HomeName = home.Abbreviation, HomeInitial = home.Abbreviation, HomeColor = Color.FromArgb(home.Primary), HomeSecondaryColor = Color.FromArgb(home.Secondary), HashMarkFraction = .30, Weather = session.Weather };
        turf = new() { Drawable = art, InputTransparent = true }; AbsoluteLayout.SetLayoutFlags(turf, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.All); AbsoluteLayout.SetLayoutBounds(turf, new Rect(0, 0, 1, 1)); field.Add(turf);
        var body = new VerticalStackLayout { Padding = 12, Spacing = 10 }; body.Add(new HorizontalStackLayout { HorizontalOptions = LayoutOptions.Center, Spacing = 12, Children = { new TeamBadgeView(league.Teams.Single(t => t.Name == session.Game.AwayTeamName), 32), score, new TeamBadgeView(home, 32) } }); body.Add(situation);
        body.Add(new Border { Padding = 5, Stroke = DynastyTheme.Accent, StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 12 }, BackgroundColor = DynastyTheme.Surface, Content = field });
        body.Add(selected); var controls = new ActionRow(); controls.Add(next); controls.Add(pause);
        Button Button(string text, Func<Task> action)
        {
            var b = new Button { Text = text }; b.Clicked += async (_, _) =>
            {
                if (pending) return; pending = true; var wasPaused = paused; paused = true;
                try { while (working) await Task.Delay(30); working = true; await action(); }
                finally { working = false; pending = false; paused = session.Mode == GameDayMode.Play || wasPaused; Update(); }
            }; controls.Add(b); return b;
        }
        next.Clicked += async (_, _) => await Snap(); pause.Clicked += (_, _) => { paused = !paused; pause.Text = paused ? "Resume watch" : "Pause watch"; };
        Button("Choose offense", async () => { var plays = PlaybookCatalog.OffensivePlays.ToArray(); var pick = await GameDialogs.ActionSheetAsync(this, "Offensive play", "Cancel", null, plays.Select(p => p.Name + " · " + p.Formation).Append("Punt").Append("Field goal").ToArray()); if (pick == "Punt") offense = "special-punt"; else if (pick == "Field goal") offense = "special-field-goal"; else offense = plays.FirstOrDefault(p => p.Name + " · " + p.Formation == pick)?.Id; selected.Text = "Offense: " + (pick ?? "CPU"); });
        Button("Choose defense", async () => { var plays = PlaybookCatalog.DefensivePlays.ToArray(); var pick = await GameDialogs.ActionSheetAsync(this, "Defensive play", "Cancel", null, plays.Select(p => p.Name).ToArray()); defense = plays.FirstOrDefault(p => p.Name == pick)?.Id; selected.Text = "Defense: " + (pick ?? "CPU"); });
        Button("Instant replay", async () => { if (last is not null) await Animate(last, true); });
        Button("Speed", async () => { var kind = await GameDialogs.ActionSheetAsync(this, "Speed", "Cancel", null, "Game speed", "Replay speed"); if (kind is null or "Cancel") return; var pick = await GameDialogs.ActionSheetAsync(this, kind, "Cancel", null, "0.5x", "1x", "2x"); if (pick is null or "Cancel") return; var value = pick == "0.5x" ? .5 : pick == "2x" ? 2 : 1; if (kind == "Game speed") speed = value; else replaySpeed = value; });
        Button("Take over", () => { session.Mode = GameDayMode.Play; paused = true; Update(); selected.Text = "Coach mode · choose plays, then Next snap"; return Task.CompletedTask; });
        Button("Depth chart", async () => { var positions = session.Roster.Where(p => p.TeamName == session.ControlledTeamName).Select(p => p.Position).Distinct().ToArray(); var pos = await GameDialogs.ActionSheetAsync(this, "Position", "Cancel", null, positions.Select(p => p.ToString()).ToArray()); if (!Enum.TryParse<Position>(pos, out var position)) return; var starters = GameDayCoachingService.Starters(session, session.ControlledTeamName!); var pool = session.Roster.Where(p => p.TeamName == session.ControlledTeamName && p.Position == position).OrderBy(p => p.DepthChartOrder).ToArray(); var names = pool.Select(p => $"{p.FullName} · {p.OverallRating}" + (starters.Contains(p.PlayerId) ? " · In game" : "")).ToArray(); var name = await GameDialogs.ActionSheetAsync(this, "Choose player", "Cancel", null, names); var index = Array.IndexOf(names, name); if (index >= 0) { var changed = GameDayCoachingService.Substitute(session, session.ControlledTeamName!, pool[index].PlayerId); selected.Text = changed ? pool[index].FullName + " in for next snap" : "Player unavailable"; } });
        Button("Timeout", () => { GameDayEngine.TryCallTimeout(session, session.ControlledTeamName!); Update(); return Task.CompletedTask; });
        Button("Second team", () => { second = !second; GameDayCoachingService.UseSecondUnit(session, session.ControlledTeamName!, second); selected.Text = second ? "Second team on field" : "Starters on field"; return Task.CompletedTask; });
        Button("Box score", async () => { var box = GameDayEngine.BoxScore(session); await GameDialogs.AlertAsync(this, "Live box score", $"{session.Game.AwayTeamName} {session.AwayScore} · {session.Game.HomeTeamName} {session.HomeScore} · {session.Events.Count} events · Yards {box.AwayStats.TotalYards}/{box.HomeStats.TotalYards} · Turnovers {box.AwayStats.Turnovers}/{box.HomeStats.Turnovers}", "Close"); });
        Button("Finish game", async () => { paused = true; await Task.Run(() => GameDayEngine.SimulateToEnd(session)); await Commit(); Update(); });
        Button("Return to franchise", async () => { if (!session.IsComplete && !await GameDialogs.AlertAsync(this, "Leave game?", "This unfinished game's snaps will be discarded. Your saved franchise remains unchanged.", "Leave", "Cancel")) return; active = false; await Navigation.PopModalAsync(); });
        body.Add(controls); body.Add(commentary); Content = new ScrollView { Content = body };
        SizeChanged += (_, _) => field.HeightRequest = Math.Clamp(Math.Min((Width - 36) / 2.25, Height * .43), 120, 420);
        Loaded += (_, _) => { art.HomeLogo = TeamBadgeView.Mark(home, false); turf.Invalidate(); if (initialized) return; initialized = true; active = true; Update(); if (session.Mode == GameDayMode.Watch) _ = Watch(); };
    }
    protected override bool OnBackButtonPressed() { if (working) return true; _ = Close(); return true; }
    async Task Close() { if (session.IsComplete || await GameDialogs.AlertAsync(this, "Leave game?", "Unfinished snaps will be discarded.", "Leave", "Cancel")) { active = false; await Navigation.PopModalAsync(); } }
    async Task Watch() { while (active && !session.IsComplete) { if (!working && !paused) await Snap(); await Task.Delay(300); } }
    async Task Snap()
    {
        if (working || pending || session.IsComplete) return; working = true; next.IsEnabled = false;
        try { var own = session.PossessionTeamName == session.ControlledTeamName; last = GameDayEngine.Step(session, session.Mode == GameDayMode.Play && own ? offense : null, session.Mode == GameDayMode.Play && !own ? defense : null); await Animate(last, false); Update(); if (session.IsComplete) await Commit(); }
        catch (Exception e) { paused = true; await GameDialogs.AlertAsync(this, "Game paused", e.Message, "OK"); }
        finally { working = false; next.IsEnabled = !session.IsComplete; RenderHuddles(); }
    }
    async Task Commit() { if (saved || !session.IsComplete) return; await save(); saved = true; }
    void Update() { string Short(string team) => league.Teams.Single(t => t.Name == team).Abbreviation; score.Text = $"{session.AwayScore}  –  {session.HomeScore}"; situation.Text = session.IsComplete ? "FINAL · Saved" : $"Q{session.Quarter} {session.ClockSeconds / 60}:{session.ClockSeconds % 60:00} · {session.Down} & {session.Distance} · {GameDayEngine.BallOn(session, Short)}"; if (last is null) { art.Direction = (session.PossessionTeamName == session.Game.HomeTeamName ? 1 : -1) * (session.Quarter % 2 == 0 ? -1 : 1); art.YardLine = session.YardLine; art.Distance = session.Distance; } turf.Invalidate(); pause.IsVisible = session.Mode == GameDayMode.Watch; pause.Text = paused ? "Resume watch" : "Pause watch"; RenderHuddles(); }
    void RenderHuddles()
    {
        if (working || pending || session.IsComplete || session.Mode != GameDayMode.Play) return;
        foreach (var view in field.Children.Where(v => v != turf).ToArray()) field.Remove(view);
        var direction = (session.PossessionTeamName == session.Game.HomeTeamName ? 1 : -1) * (session.Quarter % 2 == 0 ? -1 : 1);
        art.Direction = direction; art.YardLine = session.YardLine; art.Distance = session.Distance; turf.Invalidate();
        foreach (var team in new[] { session.Game.HomeTeamName, session.Game.AwayTeamName })
        {
            var attacking = team == session.PossessionTeamName; var home = team == session.Game.HomeTeamName;
            var profile = home ? session.HomeProfile : session.AwayProfile; var plan = profile.GamePlan ?? new TeamGamePlan { TeamName = team };
            var depth = home ? session.HomeDepthChart : session.AwayDepthChart; var fatigue = home ? session.HomeFatigue : session.AwayFatigue;
            var previewRandom = new Random(unchecked(session.Game.SimulationSeed ^ session.Events.Count * 31 ^ (home ? 17 : 37)));
            var play = PlaybookCatalog.OffensivePlays.FirstOrDefault(p => p.Id == offense) ?? GamePlanService.RecommendOffensivePlay(plan, session.Down, session.Distance, session.YardLine);
            var call = PlaybookCatalog.DefensivePlays.FirstOrDefault(p => p.Id == defense) ?? GamePlanService.RecommendDefensivePlay(plan, session.Down, session.Distance);
            var preview = attacking ? PersonnelRotationService.SelectOffense(depth, team, play.Personnel, fatigue, previewRandom) : PersonnelRotationService.SelectDefense(depth, team, call.Personnel, fatigue, previewRandom);
            var unit = GameDayCoachingService.ApplySubstitutions(session, preview).PlayerIds.Select(id => session.PlayersById.GetValueOrDefault(id)).OfType<DynastyPlayer>().ToArray();
            var brand = league.Teams.Single(t => t.Name == team);
            for (var i = 0; i < unit.Length; i++)
            {
                var p = unit[i]; var point = GameDayLivePresentation.HuddlePoint(session.YardLine, direction, attacking, i);
                var sprite = new FootballSpriteView(Color.FromArgb(brand.Primary), Color.FromArgb(brand.Secondary), !home, p.PlayerId.ToByteArray()[0] % 4, p.JerseyNumber?.ToString() ?? p.Position.ToString());
                sprite.HuddleFacing(.5 - point.X, .5 - point.Y);
                AbsoluteLayout.SetLayoutFlags(sprite, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.PositionProportional);
                AbsoluteLayout.SetLayoutBounds(sprite, new Rect(point.X, point.Y, 28, 28)); field.Add(sprite);
            }
        }
    }
    async Task Animate(GameSnapEvent snap, bool replay)
    {
        foreach (var view in field.Children.Where(v => v != turf).ToArray()) field.Remove(view);
        commentary.Text = (replay ? "REPLAY · " : "") + (snap.Description ?? $"{snap.Outcome} · {snap.Yards} yards");
        var direction = (snap.OffenseTeamName == session.Game.HomeTeamName ? 1 : -1) * (snap.Quarter % 2 == 0 ? -1 : 1); art.Direction = direction;
        art.YardLine = snap.StartYardLine; art.Distance = snap.Distance; turf.Invalidate();
        var movements = new List<(Guid Id, FootballSpriteView View, GameFieldPoint From, GameFieldPoint To, bool Carrier, bool Pursue, bool Primary, GameDayRouteFamily? Route)>();
        var roles = new Dictionary<(bool, Position), int>();
        var defensivePlay = PlaybookCatalog.DefensivePlays.FirstOrDefault(p => p.Id == snap.DefensivePlayId) ?? PlaybookCatalog.DefensivePlays.First();
        var defenders = snap.DefensePersonnel.Where(session.PlayersById.ContainsKey).Select(id => session.PlayersById[id]).ToArray();
        var starts = DefensivePursuitService.Alignments(defenders, defensivePlay);
        var assists = LiveFootballChoreography.AssistIds(snap); var defenseIndex = 0;
        var coverage = new Dictionary<Guid, (Guid Receiver, bool Zone)>();
        var contact = GameDayAnimationPlanner.ContactPoint(snap, direction, snap.CarrierLane ?? .5);
        foreach (var id in snap.OffensePersonnel.Concat(snap.DefensePersonnel).Distinct())
        {
            if (!session.PlayersById.TryGetValue(id, out var p)) continue; var attacking = snap.OffensePersonnel.Contains(id); var key = (attacking, p.Position); var role = roles.GetValueOrDefault(key); roles[key] = role + 1;
            var play = PlaybookCatalog.OffensivePlays.FirstOrDefault(o => o.Id == snap.OffensivePlayId);
            var alignment = play is null ? GameDayAnimationPlanner.Alignment(OffensiveFormation.ShotgunSpread, p.Position, role) : GameDayAnimationPlanner.OffensiveAlignment(play, p.Position, role);
            var lane = attacking ? alignment.Y : starts[id].Y;
            var offset = attacking ? alignment.X : starts[id].X;
            var from = new GameFieldPoint(GameDayAnimationPlanner.ProjectFieldYardLine(snap.StartYardLine + offset, direction), lane);
            var carrier = id == snap.BallCarrierId || id == snap.TargetId;
            var primary = !attacking && id == snap.PrimaryDefenderId && snap.Outcome is not (SnapOutcome.Incompletion or SnapOutcome.Penalty);
            var pursue = primary || !attacking && assists.Contains(id);
            GameDayRouteFamily? route = attacking && play is not null && p.Position is Position.WR or Position.TE or Position.RB && snap.TargetId.HasValue ? GameDayAnimationPlanner.AssignedRoute(play, p.Position, role) : null;
            var to = carrier ? contact : attacking ? new GameFieldPoint(GameDayAnimationPlanner.ProjectFieldYardLine(snap.StartYardLine + (route.HasValue ? Math.Max(8, snap.Yards) : Math.Clamp(snap.Yards * .55, 0, 15)) + offset, direction), lane) : LiveFootballChoreography.DefensiveDestination(from, contact, GameDayAnimationPlanner.ProjectYardLine(snap.StartYardLine, direction), contact.X, defenseIndex++, primary, pursue);
            if (!attacking && snap.TargetId.HasValue && !DefensivePursuitService.IsPassRusher(defensivePlay, p.Position, role))
            {
                var receiver = movements.Where(m => m.Route.HasValue).OrderBy(m => Math.Abs(m.From.Y - lane)).FirstOrDefault();
                if (receiver.View is not null) coverage[id] = (receiver.Id, LiveFootballChoreography.Zone(defensivePlay, defensivePlay.Assignments.FirstOrDefault(a => a.Position == p.Position && a.RoleIndex == role)));
            }
            else if (!attacking && !pursue && p.Position is Position.DE or Position.DT)
            {
                var blocker = movements.Where(m => session.PlayersById[m.Id].Position == Position.OL).OrderBy(m => Math.Abs(m.From.Y - lane)).FirstOrDefault();
                if (blocker.View is not null) to = new(blocker.To.X + direction * .020, blocker.To.Y);
            }
            var team = league.Teams.Single(t => t.Name == p.TeamName); var sprite = new FootballSpriteView(Color.FromArgb(team.Primary), Color.FromArgb(team.Secondary), p.TeamName == session.Game.AwayTeamName, id.ToByteArray()[0] % 4, p.JerseyNumber?.ToString() ?? p.Position.ToString()); sprite.Highlight(carrier);
            field.Add(sprite); movements.Add((id, sprite, from, to, carrier, pursue, primary, route));
        }
        var ball = new Border { WidthRequest = 18, HeightRequest = 10, BackgroundColor = Color.FromArgb("#9B652F"), Stroke = Colors.White, StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 6 }, InputTransparent = true };
        field.Add(ball);
        var frames = 30; for (var frame = 0; frame <= frames; frame++)
        {
            if (!active) break; var progress = frame / (double)frames;
            GameFieldPoint Lerp(GameFieldPoint a, GameFieldPoint b, double t) => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
            GameFieldPoint PositionAt((Guid Id, FootballSpriteView View, GameFieldPoint From, GameFieldPoint To, bool Carrier, bool Pursue, bool Primary, GameDayRouteFamily? Route) m, double at)
            {
                if (m.Route is { } route) return GameDayAnimationPlanner.RoutePoint(m.From, m.To, route, at);
                return m.Carrier ? GameDayAnimationPlanner.CarrierPoint(m.From, m.To, at, 75, snap.BrokeTackle) : Lerp(m.From, m.To, m.Pursue ? GameDayAnimationPlanner.PursuitProgress(at, m.Primary) : at);
            }
            foreach (var m in movements)
            {
                var point = PositionAt(m, progress);
                if (coverage.TryGetValue(m.Id, out var cover))
                {
                    var receiver = movements.First(r => r.Id == cover.Receiver); const double caught = .65;
                    var at = m.Pursue ? Math.Min(progress, caught) : progress;
                    point = LiveFootballChoreography.Coverage(m.From, PositionAt(receiver, at), at, cover.Zone, direction);
                    if (m.Pursue && progress > caught) point = Lerp(point, m.To, (progress - caught) / (1 - caught));
                }
                AbsoluteLayout.SetLayoutBounds(m.View, new Rect(Math.Clamp(point.X * field.Width - 14, 0, Math.Max(0, field.Width - 28)), Math.Clamp(point.Y * field.Height - 14, 0, Math.Max(0, field.Height - 28)), 28, 28)); m.View.Animate(progress, 1, m.To.X - m.From.X, m.To.Y - m.From.Y, false, false);
            }
            var owner = GameDayAnimationPlanner.BallOwner(snap, progress, .35);
            var carrier = movements.FirstOrDefault(m => m.Id == owner);
            var ballPoint = carrier.View is null ? new GameFieldPoint(GameDayAnimationPlanner.ProjectFieldYardLine(snap.StartYardLine + snap.Yards * progress, direction), snap.CarrierLane ?? .5) : GameDayAnimationPlanner.CarrierPoint(carrier.From, carrier.To, progress, 75, false);
            AbsoluteLayout.SetLayoutBounds(ball, new Rect(Math.Clamp(ballPoint.X * field.Width - 9, 0, Math.Max(0, field.Width - 18)), Math.Clamp(ballPoint.Y * field.Height - 5, 0, Math.Max(0, field.Height - 10)), 18, 10));
            await Task.Delay((int)(33 / (replay ? replaySpeed : speed)));
        }
    }
}
