using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Persistence;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private readonly VerticalStackLayout _gamePlanHost = new() { Spacing = 12 };

    private View BuildGamePlanSection() => _gamePlanHost;

    private static View CoachingCard(string title, params View[] content)
    {
        var layout = new VerticalStackLayout { Spacing = 10 };
        layout.Children.Add(new Label { Text = title, TextColor = DynastyTheme.Accent,
            FontSize = 11, CharacterSpacing = 0, FontAttributes = FontAttributes.Bold });
        foreach (var view in content) layout.Children.Add(view);
        return new Border { Content = layout, Padding = 14, BackgroundColor = Color.FromArgb("#1B2632"), StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 } };
    }

    private void RenderGamePlanScreen()
    {
        _gamePlanHost.Children.Clear();
        if (_currentDynasty is null)
        {
            _gamePlanHost.Children.Add(new Label { Text = "Create or load a dynasty to build your game plan." });
            return;
        }
        var program = new Button { Text = "Program & staff", BackgroundColor = Color.FromArgb("#493D65") };
        program.Clicked += (_, _) => ShowSection(AppSection.Program);
        var depthChart = new Button { Text = "Manage depth chart", BackgroundColor = Color.FromArgb("#315D59") };
        depthChart.Clicked += (_, _) => { _showDepthChartOrganizer = true; ShowSection(AppSection.Roster); };
        _gamePlanHost.Children.Add(new HorizontalStackLayout { Spacing = 8, Children = { program, depthChart } });
        var careerTools = new HorizontalStackLayout { Spacing = 8 };
        if (_currentDynasty.UserHeadCoach is not null)
        {
            var career = CreateCoachCareerButton(_currentDynasty.UserHeadCoach);
            career.BackgroundColor = Microsoft.Maui.Graphics.Colors.Transparent;
            career.TextColor = DynastyTheme.Accent;
            careerTools.Children.Add(career);
        }
        var archive = CreateCoachArchiveShortcut();
        if (archive is Button archiveButton)
        {
            archiveButton.BackgroundColor = Microsoft.Maui.Graphics.Colors.Transparent;
            archiveButton.TextColor = DynastyTheme.Accent;
        }
        careerTools.Children.Add(archive);
        _gamePlanHost.Children.Add(careerTools);
        var dynastyId = _currentDynasty.DynastyId;
        var teamName = _currentDynasty.UserTeamName;
        var profiles = BuildCurrentSimulationProfiles();
        if (!profiles.TryGetValue(teamName, out var profile)) return;
        var plan = GamePlanService.GetPlan(_currentDynasty, profile);
        var identity = new Grid { ColumnSpacing = 12,
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) } };
        identity.Add(new Label { Text = teamName, FontSize = 16, FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center }, 0, 0);
        identity.Add(new Label { Text = $"PASS {profile.PassOffenseRating:F0}  /  RUN {profile.RushOffenseRating:F0}  /  DEF {profile.DefenseRating:F0}",
            FontSize = 12, TextColor = DynastyTheme.Muted, VerticalOptions = LayoutOptions.Center }, 1, 0);
        _gamePlanHost.Children.Add(CoachingCard("YOUR PROGRAM", identity));
        var scoutingHost = new ContentView { VerticalOptions = LayoutOptions.Start };
        var settingsHost = new VerticalStackLayout { Spacing = 12 };
        var workspace = new Grid { ColumnSpacing = 12, RowSpacing = 12,
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(new GridLength(0)) },
            RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Auto) } };
        workspace.Add(scoutingHost, 0, 0); workspace.Add(settingsHost, 0, 1);
        bool? wideLayout = null;
        workspace.SizeChanged += (_, _) =>
        {
            var wide = workspace.Width >= 760 && scoutingHost.Content is not null;
            if (wideLayout == wide) return;
            wideLayout = wide;
            workspace.ColumnDefinitions[1].Width = wide ? GridLength.Star : new GridLength(0);
            Grid.SetColumn(settingsHost, wide ? 1 : 0); Grid.SetRow(settingsHost, wide ? 0 : 1);
        };
        _gamePlanHost.Children.Add(workspace);
        var next = _currentDynasty.Schedule.Where(game => game.SeasonYear == _currentDynasty.SeasonYear &&
            !game.HasPlayed && game.InvolvesTeam(teamName)).OrderBy(game => game.Week).FirstOrDefault();
        if (next is not null)
        {
            var opponent = next.HomeTeamName == teamName ? next.AwayTeamName : next.HomeTeamName;
            var report = OpponentScoutingService.Build(_currentDynasty, opponent);
            var scouting = new VerticalStackLayout { Spacing = 9 };
            var opponentLink = new Button { Text = $"{opponent} · {report.Wins}–{report.Losses}", FontSize = 17, FontAttributes = FontAttributes.Bold,
                BackgroundColor = Colors.Transparent, TextColor = DynastyTheme.Accent, Padding = 0 };
            opponentLink.Clicked += (_, _) => OpenSchool(opponent);
            scouting.Children.Add(opponentLink);
            scouting.Children.Add(new Label { Text = $"Week {next.Week} · {(next.HomeTeamName == teamName ? "Home" : "Away")} · {teamName} vs {opponent}", TextColor = DynastyTheme.Muted });
            if (profiles.TryGetValue(opponent, out var rival))
            {
                var rivalPlan = rival.GamePlan ?? GamePlanService.GetPlan(_currentDynasty, rival);
                var matchup = new Grid { ColumnSpacing = 8, RowSpacing = 8,
                    ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) },
                    RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Auto) } };
                View Matchup(string heading, double theirs, double ours) => new Border { BackgroundColor = Color.FromArgb("#142C35"), Padding = 10,
                    Content = new VerticalStackLayout { Spacing = 3, Children = {
                        new Label { Text = heading, FontSize = 10, TextColor = DynastyTheme.Accent },
                        new Label { Text = $"{theirs:F0}  vs  {ours:F0}", FontSize = 18, FontAttributes = FontAttributes.Bold },
                        new Label { Text = $"Opponent / {teamName}", FontSize = 10, TextColor = DynastyTheme.Muted } } } };
                matchup.Add(Matchup("PASS ATTACK / YOUR COVERAGE", rival.PassOffenseRating, profile.PassDefenseRating), 0, 0);
                matchup.Add(Matchup("RUN ATTACK / YOUR FRONT", rival.RushOffenseRating, profile.RushDefenseRating), 1, 0);
                matchup.Add(Matchup("PASS DEFENSE / YOUR PASSING", rival.PassDefenseRating, profile.PassOffenseRating), 0, 1);
                matchup.Add(Matchup("RUN DEFENSE / YOUR RUSHING", rival.RushDefenseRating, profile.RushOffenseRating), 1, 1);
                scouting.Children.Add(matchup);
                scouting.Children.Add(new Label { Text = $"Scheme: {GamePlanService.Label(rivalPlan.Offense)} / {GamePlanService.Label(rivalPlan.Defense)} · {GamePlanService.Label(rivalPlan.Approach)}", TextColor = DynastyTheme.Muted });
                var passEdge = profile.PassOffenseRating - rival.PassDefenseRating;
                var runEdge = profile.RushOffenseRating - rival.RushDefenseRating;
                scouting.Children.Add(new Label { Text = passEdge >= runEdge ? "Matchup focus: your passing game has the better relative matchup." : "Matchup focus: your running game has the better relative matchup.", TextColor = Color.FromArgb("#8CDBC0") });
            }
            scouting.Children.Add(new Label { Text = report.Games == 0 ? "No games played this season. Tendencies appear after results are recorded."
                : $"{report.PointsFor:F1} points/game · {report.PointsAgainst:F1} allowed · {report.Turnovers} giveaways" +
                    (report.PassShare is double share ? $" · {share:P0} pass attempts (excludes sacks)" : ""), TextColor = DynastyTheme.Muted });
            foreach (var game in report.RecentGames)
            {
                var homeGame = game.HomeTeamName == opponent;
                var opponentPoints = homeGame ? game.HomeScore : game.AwayScore;
                var otherPoints = homeGame ? game.AwayScore : game.HomeScore;
                scouting.Children.Add(new Label { Text = $"Week {game.Week} · {(opponentPoints > otherPoints ? "W" : "L")} {opponentPoints}–{otherPoints} {(homeGame ? "vs" : "at")} {(homeGame ? game.AwayTeamName : game.HomeTeamName)}", FontSize = 12 });
            }
            if (report.KeyPlayers.Count > 0) scouting.Children.Add(new Label { Text = "PLAYERS TO WATCH", FontSize = 10, TextColor = DynastyTheme.Accent, FontAttributes = FontAttributes.Bold });
            foreach (var player in report.KeyPlayers)
            {
                var row = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) } };
                row.Add(PlayerLink(player), 0, 0); row.Add(new Label { Text = $"{player.Position} · {player.OverallRating} OVR", VerticalOptions = LayoutOptions.Center, TextColor = DynastyTheme.Muted }, 1, 0);
                scouting.Children.Add(row);
            }
            scoutingHost.Content = CoachingCard("OPPONENT SCOUTING REPORT", scouting);
        }
        var offenseValues = Enum.GetValues<OffensivePlaybook>();
        var tempoValues = Enum.GetValues<OffensiveTempo>();
        var tempo = new Picker { Title = "Offensive tempo", ItemsSource = new[] { "Automatic — adapt to the game", "Normal", "Up tempo", "Chew clock" }, SelectedIndex = (int)plan.Tempo };
        var defenseValues = Enum.GetValues<DefensivePlaybook>();
        var approachValues = Enum.GetValues<DefensiveApproach>();
        var offensiveFormationValues = Enum.GetValues<OffensiveFormation>();
        var defensiveFormationValues = Enum.GetValues<DefensiveFormation>();
        var offense = new Picker { Title = "Offensive playbook", ItemsSource = offenseValues.Select(GamePlanService.Label).ToList(), SelectedIndex = Array.IndexOf(offenseValues, plan.Offense) };
        var defense = new Picker { Title = "Defensive playbook", ItemsSource = defenseValues.Select(GamePlanService.Label).ToList(), SelectedIndex = Array.IndexOf(defenseValues, plan.Defense) };
        var approach = new Picker { Title = "Defensive approach", ItemsSource = approachValues.Select(GamePlanService.Label).ToList(), SelectedIndex = Array.IndexOf(approachValues, plan.Approach) };
        var offensiveFormation = new Picker { Title = "Primary offensive formation", ItemsSource = offensiveFormationValues.Select(GamePlanService.Label).ToList(), SelectedIndex = Array.IndexOf(offensiveFormationValues, plan.OffensiveFormation) };
        var defensiveFormation = new Picker { Title = "Primary defensive formation", ItemsSource = defensiveFormationValues.Select(GamePlanService.Label).ToList(), SelectedIndex = Array.IndexOf(defensiveFormationValues, plan.DefensiveFormation) };
        var description = new Label { Text = GamePlanService.Describe(plan.Offense), TextColor = DynastyTheme.Muted };
        offense.SelectedIndexChanged += (_, _) =>
        {
            if (offense.SelectedIndex < 0) return;
            var book = offenseValues[offense.SelectedIndex];
            description.Text = GamePlanService.Describe(book);
            var preferred = book switch
            {
                OffensivePlaybook.Flexbone => OffensiveFormation.Flexbone,
                OffensivePlaybook.PowerRun => OffensiveFormation.IForm,
                OffensivePlaybook.Option => OffensiveFormation.Pistol,
                OffensivePlaybook.AirRaid => OffensiveFormation.Empty,
                _ => OffensiveFormation.ShotgunSpread
            };
            if (book != OffensivePlaybook.Custom) offensiveFormation.SelectedIndex = Array.IndexOf(offensiveFormationValues, preferred);
        };
        settingsHost.Children.Add(CoachingCard("SCHEME & PLAYBOOK SELECTION",
            new Label { Text = "Offensive tempo — controls time used before the snap; trailing teams hurry and leading teams protect the clock late.", TextColor = DynastyTheme.Muted }, tempo, new Label { Text = "Offensive playbook — available calls and CPU run/pass tendencies", FontAttributes = FontAttributes.Bold }, offense, description,
            new Label { Text = "Preferred offensive formation — CPU starting point", TextColor = DynastyTheme.Muted }, offensiveFormation, new Label { Text = "Defensive playbook — available fronts and coverages", FontAttributes = FontAttributes.Bold }, defense, new Label { Text = "Defensive tendency — how aggressively the CPU calls defense", TextColor = DynastyTheme.Muted }, approach, new Label { Text = "Preferred defensive formation", TextColor = DynastyTheme.Muted }, defensiveFormation,
            new Label { Text = "4-3 strengthens the run front; Nickel helps coverage. Run/pass focus gives up strength elsewhere. Pressure creates takeaways but risks bigger gains.", TextColor = DynastyTheme.Muted }));
        var editor = new Button { Text = "Open Playbook Editor", BackgroundColor = Color.FromArgb("#315D59") };
        editor.Clicked += async (_, _) => await OpenPlaybookEditorAsync(plan, dynastyId);
        settingsHost.Children.Add(CoachingCard("CUSTOM PLAYBOOK",
            new Label { Text = "Build a Custom offense and defense from the full play catalog. Custom books replace the standard book and use the same 17-offense / 14-defense call limits.", TextColor = DynastyTheme.Muted }, editor));
        var status = new Label { TextColor = DynastyTheme.Muted };
        var save = new Button { Text = "Apply & save game plan", BackgroundColor = DynastyTheme.Accent, TextColor = DynastyTheme.Background };
        save.Clicked += async (_, _) =>
        {
            if (_currentDynasty is null || _isAdvancing || _currentDynasty.DynastyId != dynastyId || _currentDynasty.UserTeamName != teamName) return;
            save.IsEnabled = false;
            try
            {
                _currentDynasty = GamePlanService.SetPlan(_currentDynasty, new TeamGamePlan
                {
                    TeamName = teamName, Tempo = tempoValues[Math.Max(0, tempo.SelectedIndex)], Offense = offenseValues[Math.Max(0, offense.SelectedIndex)],
                    Defense = defenseValues[Math.Max(0, defense.SelectedIndex)], Approach = approachValues[Math.Max(0, approach.SelectedIndex)],
                    OffensiveFormation = offensiveFormationValues[Math.Max(0, offensiveFormation.SelectedIndex)],
                    DefensiveFormation = defensiveFormationValues[Math.Max(0, defensiveFormation.SelectedIndex)],
                    CustomOffensivePlayIds = plan.CustomOffensivePlayIds, CustomDefensivePlayIds = plan.CustomDefensivePlayIds,
                    CreatedOffensivePlays = plan.CreatedOffensivePlays, CreatedDefensivePlays = plan.CreatedDefensivePlays,
                    OpeningPlayIds = [], OpeningScript = []
                });
                _cachedCurrentProfiles = null;
                _cachedNationalRankings = null;
                await SaveCurrentAsync(SaveKind.AutosaveCurrent);
                status.Text = "Game plan saved. It applies to future games, including the postseason.";
            }
            catch (Exception ex) { status.Text = $"Could not save game plan: {ex.Message}"; }
            finally { save.IsEnabled = true; }
        };
        settingsHost.Children.Add(save);
        settingsHost.Children.Add(status);
        var last = _currentDynasty.Schedule.Where(game => game.HasPlayed && game.InvolvesTeam(teamName))
            .OrderByDescending(game => game.SeasonYear).ThenByDescending(game => game.Week).FirstOrDefault();
        if (last is null) return;
        var driveRows = new VerticalStackLayout { Spacing = 10 };
        var ownPlan = last.HomeTeamName == teamName ? last.HomeGamePlan : last.AwayGamePlan;
        driveRows.Children.Add(new Label { Text = $"{last.SeasonYear} Week {last.Week} / {last.AwayTeamName} {last.AwayScore} - {last.HomeScore} {last.HomeTeamName}", FontAttributes = FontAttributes.Bold });
        if (ownPlan is not null) driveRows.Children.Add(new Label { Text = $"Played with {GamePlanService.Label(ownPlan.Offense)} / {GamePlanService.Label(ownPlan.Defense)}", TextColor = DynastyTheme.Muted });
        var drives = last.Drives.Where(drive => drive.TeamName == teamName).OrderBy(drive => drive.PossessionNumber).ToArray();
        if (drives.Length == 0) driveRows.Children.Add(new Label { Text = "Drive reports are available for games played with this version." });
        foreach (var drive in drives)
            driveRows.Children.Add(new Label { Text = drive.IsOvertime ? $"OT / {drive.Outcome} / +{drive.Points} points"
                : $"Drive {drive.PossessionNumber} / {PlaybookCatalog.OffensivePlays.FirstOrDefault(play => play.Id == drive.OffensivePlayId)?.Name ?? GamePlanService.Label(drive.Concept)} / {drive.Outcome}\n{drive.PassAttempts} passes, {drive.RushAttempts} runs / {drive.PassYards + drive.RushYards} yards / +{drive.Points} points" });
        _gamePlanHost.Children.Add(CoachingCard("LAST GAME / DRIVE REPORT", driveRows));
    }

    private static View PlayLibraryRow(string id, string name, string details, string description,
        bool offense, string styleKey,
        HashSet<string> favorites, Action rerender, OffensivePlayDefinition? definition = null, DefensivePlayDefinition? defensiveDefinition = null)
    {
        var favorite = favorites.Contains(id);
        var toggle = new Button { Text = favorite ? "★" : "☆", FontSize = 20, Padding = new Thickness(8, 2),
            BackgroundColor = Colors.Transparent, TextColor = favorite ? Color.FromArgb("#E8C979") : DynastyTheme.Muted };
        toggle.Clicked += (_, _) =>
        {
            if (favorites.Contains(id)) favorites.Remove(id);
            else if (favorites.Count < (offense ? 17 : 14)) favorites.Add(id);
            rerender();
        };
        var art = new GraphicsView
        {
            Drawable = new PlayArtDrawable(id, name, offense, styleKey, definition, defensiveDefinition),
            HeightRequest = 92,
            WidthRequest = 150,
            BackgroundColor = Color.FromArgb("#102A24")
        };
        SemanticProperties.SetDescription(art, $"{name} play diagram");
        var grid = new Grid { ColumnSpacing = 10, RowSpacing = 6,
            ColumnDefinitions = new ColumnDefinitionCollection { new(new GridLength(150)), new(GridLength.Star), new(GridLength.Auto) } };
        grid.Add(art, 0, 0);
        grid.Add(new VerticalStackLayout { Spacing = 3, Children =
        {
            new Label { Text = name, FontAttributes = FontAttributes.Bold, FontSize = 13 },
            new Label { Text = details, FontSize = 10, TextColor = Color.FromArgb("#B8D8FA") },
            new Label { Text = description, FontSize = 11, TextColor = DynastyTheme.Muted, LineBreakMode = LineBreakMode.WordWrap }
        } }, 1, 0);
        grid.Add(toggle, 2, 0);
        return new Border { StrokeThickness = 0, BackgroundColor = Color.FromArgb("#1B2B37"), Padding = new Thickness(10, 10), Content = grid };
    }

    private sealed class PlayArtDrawable(string id, string playName, bool offense, string styleKey, OffensivePlayDefinition? definition = null, DefensivePlayDefinition? defensiveDefinition = null) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var width = dirtyRect.Width;
            var height = dirtyRect.Height;
            canvas.StrokeColor = Color.FromArgb("#3E675B");
            canvas.StrokeSize = 1;
            for (var yard = 1; yard < 5; yard++)
            {
                var y = height * yard / 5f;
                canvas.DrawLine(0, y, width, y);
            }
            canvas.StrokeColor = Color.FromArgb("#D8E5E2");
            canvas.StrokeSize = 1.5f;
            canvas.DrawLine(8, height * .58f, width - 8, height * .58f);

            var seed = id.Aggregate(0, (hash, character) => (hash * 31 + character) & int.MaxValue);
            if (offense) DrawOffense(canvas, width, height, id, definition);
            else if (defensiveDefinition?.Assignments.Count > 0)
            {
                canvas.StrokeColor = Color.FromArgb("#7CC6FF"); canvas.StrokeSize = 2;
                foreach (var a in defensiveDefinition.Assignments)
                {
                    var x = width * (float)a.Lane; var y = height * (.55f - (float)a.Depth * .014f);
                    canvas.DrawCircle(x, y, 4);
                    if (a.Task == AssignmentTask.Rush) DrawRoute(canvas, x, y, x, height * .68f);
                    else if (a.Task == AssignmentTask.Zone) canvas.DrawEllipse(x - 10, y - 15, 20, 12);
                    else canvas.DrawLine(x, y, x + 8, y - 15);
                }
            }
            else DrawDefense(canvas, width, height, seed, styleKey, playName);
        }

        private static void DrawOffense(ICanvas canvas, float width, float height, string playId, OffensivePlayDefinition? definition)
        {
            var play = definition ?? PlaybookCatalog.OffensivePlays.FirstOrDefault(item => item.Id == playId);
            if (play is null) return;
            var family = GameDayAnimationPlanner.RouteFamily(play);
            canvas.FillColor = Color.FromArgb("#F2F5F4");
            for (var player = 0; player < 5; player++)
            {
                var lineman = play.Assignments.FirstOrDefault(a => a.Position == Position.OL && a.RoleIndex == player);
                canvas.FillCircle(width * (float)(lineman?.Lane ?? .34 + player * .08), height * (.61f - (float)(lineman?.Depth ?? 0) * .03f), 3);
            }
            var qb = play.Assignments.FirstOrDefault(a => a.Position == Position.QB && a.RoleIndex == 0);
            var quarterback = qb is null ? GameDayAnimationPlanner.Alignment(play.Formation, Position.QB, 0) : new GameFieldPoint(qb.Depth, qb.Lane);
            canvas.FillCircle(width * (float)quarterback.Y, height * (.61f - (float)quarterback.X * .03f), 3.5f);
            var positions = GameDayAnimationPlanner.EligiblePositions(play.Personnel);
            var roleCounts = new Dictionary<Position, int>();
            for (var i = 0; i < positions.Count; i++)
            {
                var position = positions[i];
                roleCounts.TryGetValue(position, out var roleIndex);
                roleCounts[position] = roleIndex + 1;
                var custom = play.Assignments.FirstOrDefault(a => a.Position == position && a.RoleIndex == roleIndex);
                var alignment = custom is null ? GameDayAnimationPlanner.Alignment(play.Formation, position, roleIndex) : new GameFieldPoint(custom.Depth, custom.Lane);
                var assigned = GameDayAnimationPlanner.AssignedRoute(play, position, roleIndex);
                var start = new GameFieldPoint(0, alignment.Y);
                var depth = GameDayAnimationPlanner.RouteDepthShare(assigned, position, false);
                var end = new GameFieldPoint(depth, GameDayAnimationPlanner.RouteEndLane(assigned, position, i, start.Y, false));
                var startY = .61f - (float)alignment.X * .03f;
                canvas.FillColor = Color.FromArgb("#F2F5F4");
                canvas.FillCircle(width * (float)start.Y, height * startY, 3.5f);
                if (custom?.Task == AssignmentTask.Block) continue;
                if (family == GameDayRouteFamily.Run)
                {
                    if (i == 0 && position == Position.RB)
                    {
                        canvas.StrokeColor = Color.FromArgb("#F4CC66"); canvas.StrokeSize = 2;
                        DrawRoute(canvas, width * (float)start.Y, height * startY,
                            width * (play.Concept == OffensiveConcept.OutsideRun ? .80f : .50f), height * .38f);
                    }
                    continue;
                }
                canvas.StrokeColor = Color.FromArgb("#F4CC66");
                canvas.StrokeSize = 1.8f;
                var previousX = width * (float)start.Y;
                var previousY = height * startY;
                for (var step = 1; step <= 10; step++)
                {
                    var point = GameDayAnimationPlanner.RoutePoint(start, end, assigned, step / 10d);
                    var nextX = width * (float)point.Y;
                    var nextY = height * (startY - (float)point.X * .52f);
                    canvas.DrawLine(previousX, previousY, nextX, nextY);
                    if (step == 10) DrawRoute(canvas, previousX, previousY, nextX, nextY);
                    previousX = nextX; previousY = nextY;
                }
            }
        }

        private static void DrawRoute(ICanvas canvas, float x1, float y1, float x2, float y2)
        {
            canvas.DrawLine(x1, y1, x2, y2);
            canvas.DrawLine(x2, y2, x2 - 4, y2 + 7);
            canvas.DrawLine(x2, y2, x2 + 5, y2 + 5);
        }

        private static void DrawDefense(ICanvas canvas, float width, float height, int seed, string approach, string name)
        {
            canvas.StrokeColor = Color.FromArgb("#7CC6FF");
            canvas.StrokeSize = 2;
            var pressure = approach.Contains("Pressure", StringComparison.OrdinalIgnoreCase);
            var protect = approach.Contains("Protect", StringComparison.OrdinalIgnoreCase);
            for (var player = 0; player < 4; player++)
            {
                var x = width * (.32f + player * .12f);
                canvas.DrawCircle(x, height * .49f, 4);
                if (pressure || player < 2) canvas.DrawLine(x, height * .44f, x + (player % 2 == 0 ? -6 : 6), height * .31f);
            }
            for (var player = 0; player < 3; player++)
                canvas.DrawCircle(width * (.3f + player * .2f), height * .34f, 4);
            canvas.DrawCircle(width * .18f, height * .2f, 4);
            canvas.DrawCircle(width * .82f, height * .2f, 4);
            if (name.Contains("Cover 3", StringComparison.OrdinalIgnoreCase))
            {
                canvas.StrokeColor = Color.FromArgb("#99E0D0");
                for (var zone = 0; zone < 3; zone++)
                    canvas.DrawArc(width * (.06f + zone * .30f), height * .06f, width * .28f, height * .30f, 190, 160, false, false);
            }
            else if (name.Contains("Quarters", StringComparison.OrdinalIgnoreCase))
            {
                canvas.StrokeColor = Color.FromArgb("#99E0D0");
                for (var zone = 0; zone < 4; zone++)
                    canvas.DrawArc(width * (.03f + zone * .24f), height * .07f, width * .22f, height * .27f, 190, 160, false, false);
            }
            else if (protect)
            {
                canvas.StrokeColor = Color.FromArgb("#99E0D0");
                canvas.DrawArc(width * .1f, height * .08f, width * .8f, height * .32f, 190, 160, false, false);
            }
            else if (seed % 2 == 0)
                canvas.DrawLine(width * .3f, height * .3f, width * .48f, height * .6f);
        }
    }
}
