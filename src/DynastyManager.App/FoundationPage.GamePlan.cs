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
        layout.Children.Add(new Label { Text = title, TextColor = DynastyTheme.Muted,
            FontSize = 12, CharacterSpacing = 0, FontAttributes = FontAttributes.Bold });
        foreach (var view in content) layout.Children.Add(view);
        return new Border { Content = layout };
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
        _gamePlanHost.Children.Add(CoachingCard("PROGRAM IDENTITY",
            new Label { Text = teamName, FontSize = 16, FontAttributes = FontAttributes.Bold },
            new Label { Text = $"PASS {profile.PassOffenseRating:F0}  /  RUN {profile.RushOffenseRating:F0}  /  DEF {profile.DefenseRating:F0}",
                TextColor = DynastyTheme.Muted },
            new Label { Text = "Build around your personnel. Playbooks change tendencies; player ratings still decide matchups." }));
        var next = _currentDynasty.Schedule.Where(game => game.SeasonYear == _currentDynasty.SeasonYear &&
            !game.HasPlayed && game.InvolvesTeam(teamName)).OrderBy(game => game.Week).FirstOrDefault();
        if (next is not null)
        {
            var opponent = next.HomeTeamName == teamName ? next.AwayTeamName : next.HomeTeamName;
            var notes = profiles.TryGetValue(opponent, out var rival)
                ? $"Opponent pass defense {rival.PassDefenseRating:F0} / run defense {rival.RushDefenseRating:F0}" : "Prepare your call sheet.";
            _gamePlanHost.Children.Add(CoachingCard("NEXT MATCHUP",
                new Label { Text = $"Week {next.Week} / {teamName} vs {opponent}", FontAttributes = FontAttributes.Bold },
                new Label { Text = notes, TextColor = DynastyTheme.Muted }));
        }
        var offenseValues = Enum.GetValues<OffensivePlaybook>();
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
        offense.SelectedIndexChanged += (_, _) => { if (offense.SelectedIndex >= 0) description.Text = GamePlanService.Describe(offenseValues[offense.SelectedIndex]); };
        _gamePlanHost.Children.Add(CoachingCard("PLAYBOOKS", new Label { Text = "Offense", FontAttributes = FontAttributes.Bold }, offense, description,
            offensiveFormation, new Label { Text = "Defense", FontAttributes = FontAttributes.Bold }, defense, approach, defensiveFormation,
            new Label { Text = "4-3 strengthens the run front; Nickel helps coverage. Run/pass focus gives up strength elsewhere. Pressure creates takeaways but risks bigger gains.", TextColor = DynastyTheme.Muted }));
        var favoriteIds = plan.FavoritePlayIds.ToHashSet(StringComparer.Ordinal);
        var playLibrary = new VerticalStackLayout { Spacing = 8 };
        void RenderPlayLibrary()
        {
            playLibrary.Children.Clear();
            var selectedOffense = offensiveFormationValues[Math.Max(0, offensiveFormation.SelectedIndex)];
            var selectedDefense = defensiveFormationValues[Math.Max(0, defensiveFormation.SelectedIndex)];
            playLibrary.Children.Add(new Label { Text = $"OFFENSE · {GamePlanService.Label(selectedOffense)}", FontAttributes = FontAttributes.Bold, TextColor = DynastyTheme.Accent });
            foreach (var play in PlaybookCatalog.OffensivePlays.Where(item => item.Formation == selectedOffense))
                playLibrary.Children.Add(PlayLibraryRow(play.Id, play.Name,
                    $"{GamePlanService.Label(play.Personnel)} · {GamePlanService.Label(play.Concept)}", play.Description,
                    true, play.Concept.ToString(), favoriteIds, RenderPlayLibrary));
            playLibrary.Children.Add(new Label { Text = $"DEFENSE · {GamePlanService.Label(selectedDefense)}", FontAttributes = FontAttributes.Bold, TextColor = DynastyTheme.Accent,
                Margin = new Thickness(0, 7, 0, 0) });
            foreach (var play in PlaybookCatalog.DefensivePlays.Where(item => item.Formation == selectedDefense))
                playLibrary.Children.Add(PlayLibraryRow(play.Id, play.Name,
                    $"{GamePlanService.Label(play.Personnel)} · {GamePlanService.Label(play.Approach)}", play.Description,
                    false, play.Approach.ToString(), favoriteIds, RenderPlayLibrary));
        }
        offensiveFormation.SelectedIndexChanged += (_, _) => RenderPlayLibrary();
        defensiveFormation.SelectedIndexChanged += (_, _) => RenderPlayLibrary();
        RenderPlayLibrary();
        _gamePlanHost.Children.Add(CoachingCard("PLAY LIBRARY",
            new Label { Text = "Browse the initial formation and personnel catalog. Star up to 20 calls for your working playbook; assignment editing follows in the visual play editor.", TextColor = DynastyTheme.Muted },
            playLibrary));
        var script = new VerticalStackLayout { Spacing = 8 };
        var calls = new List<Picker>();
        var callSheetPlays = PlaybookCatalog.OffensivePlays.ToArray();
        var callSheetLabels = callSheetPlays.Select(play => $"{play.Name} · {GamePlanService.Label(play.Formation)} · {GamePlanService.Label(play.Concept)}").ToList();
        for (var index = 0; index < 3; index++)
        {
            script.Children.Add(new Label { Text = $"Drive {index + 1} call", FontAttributes = FontAttributes.Bold });
            var savedId = plan.OpeningPlayIds.ElementAtOrDefault(index);
            var selected = Array.FindIndex(callSheetPlays, play => play.Id == savedId);
            if (selected < 0 && index < plan.OpeningScript.Count)
                selected = Array.FindIndex(callSheetPlays, play => play.Concept == plan.OpeningScript[index]);
            var picker = new Picker { Title = $"Drive {index + 1}", ItemsSource = callSheetLabels, SelectedIndex = Math.Max(0, selected) };
            calls.Add(picker);
            script.Children.Add(picker);
        }
        _gamePlanHost.Children.Add(CoachingCard("OPENING CALL SHEET",
            new Label { Text = "Call exact plays for your first three possessions. CPU teams select from the same catalog and both paths use the same matchup resolver." },
            script, new Label { Text = "The upcoming live Game Day view will use this same resolver on every snap rather than switching to a separate simulation model.", TextColor = DynastyTheme.Muted }));
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
                    TeamName = teamName, Offense = offenseValues[Math.Max(0, offense.SelectedIndex)],
                    Defense = defenseValues[Math.Max(0, defense.SelectedIndex)], Approach = approachValues[Math.Max(0, approach.SelectedIndex)],
                    OffensiveFormation = offensiveFormationValues[Math.Max(0, offensiveFormation.SelectedIndex)],
                    DefensiveFormation = defensiveFormationValues[Math.Max(0, defensiveFormation.SelectedIndex)],
                    FavoritePlayIds = favoriteIds.ToArray(),
                    OpeningPlayIds = calls.Select(picker => callSheetPlays[Math.Max(0, picker.SelectedIndex)].Id).ToArray()
                });
                _cachedCurrentProfiles = null;
                _cachedNationalRankings = null;
                await SaveCurrentAsync(SaveKind.AutosaveCurrent);
                status.Text = "Game plan saved. It applies to future games, including the postseason.";
            }
            catch (Exception ex) { status.Text = $"Plan applied, but saving failed: {ex.Message}"; }
            finally { save.IsEnabled = true; }
        };
        _gamePlanHost.Children.Add(save);
        _gamePlanHost.Children.Add(status);
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
        HashSet<string> favorites, Action rerender)
    {
        var favorite = favorites.Contains(id);
        var toggle = new Button { Text = favorite ? "★" : "☆", FontSize = 20, Padding = new Thickness(8, 2),
            BackgroundColor = Colors.Transparent, TextColor = favorite ? Color.FromArgb("#E8C979") : DynastyTheme.Muted };
        toggle.Clicked += (_, _) =>
        {
            if (favorites.Contains(id)) favorites.Remove(id);
            else if (favorites.Count < 20) favorites.Add(id);
            rerender();
        };
        var art = new GraphicsView
        {
            Drawable = new PlayArtDrawable(id, name, offense, styleKey),
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

    private sealed class PlayArtDrawable(string id, string playName, bool offense, string styleKey) : IDrawable
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

            var seed = Math.Abs(StringComparer.Ordinal.GetHashCode(id));
            if (offense) DrawOffense(canvas, width, height, seed, styleKey, playName);
            else DrawDefense(canvas, width, height, seed, styleKey, playName);
        }

        private static void DrawOffense(ICanvas canvas, float width, float height, int seed, string concept, string name)
        {
            canvas.FillColor = Color.FromArgb("#F2F5F4");
            for (var player = 0; player < 5; player++)
                canvas.FillCircle(width * (.34f + player * .08f), height * .61f, 3);
            canvas.FillCircle(width * .5f, height * .72f, 3.5f);
            canvas.FillCircle(width * .5f, height * .84f, 3.5f);
            canvas.FillCircle(width * .14f, height * .63f, 3.5f);
            canvas.FillCircle(width * .86f, height * .63f, 3.5f);

            canvas.StrokeColor = Color.FromArgb("#F4CC66");
            canvas.StrokeSize = 2.2f;
            var deep = concept.Contains("Deep", StringComparison.OrdinalIgnoreCase) || concept.Contains("PlayAction", StringComparison.OrdinalIgnoreCase);
            var run = concept.Contains("Run", StringComparison.OrdinalIgnoreCase);
            if (run)
            {
                var outside = Contains(name, "Toss", "Sweep", "Wide", "Stretch", "Speed Option");
                var counter = Contains(name, "Counter", "Trap");
                var targetX = outside ? (seed % 2 == 0 ? .18f : .82f) * width
                    : (seed % 2 == 0 ? .40f : .61f) * width;
                canvas.DrawLine(width * .5f, height * .82f, targetX, height * .48f);
                canvas.DrawLine(targetX, height * .48f,
                    counter ? width * .5f : targetX + (seed % 3 - 1) * 18, height * .34f);
            }
            else
            {
                var top = deep ? .12f : .32f;
                if (Contains(name, "Vertical", "All Go"))
                {
                    DrawRoute(canvas, width * .14f, height * .59f, width * .14f, height * .10f);
                    DrawRoute(canvas, width * .38f, height * .58f, width * .38f, height * .12f);
                    DrawRoute(canvas, width * .62f, height * .58f, width * .62f, height * .12f);
                    DrawRoute(canvas, width * .86f, height * .59f, width * .86f, height * .10f);
                }
                else if (Contains(name, "Mesh", "Cross"))
                {
                    DrawRoute(canvas, width * .14f, height * .59f, width * .72f, height * .42f);
                    DrawRoute(canvas, width * .86f, height * .59f, width * .28f, height * .36f);
                    DrawRoute(canvas, width * .42f, height * .58f, width * .78f, height * .18f);
                }
                else if (Contains(name, "Screen"))
                {
                    DrawRoute(canvas, width * .50f, height * .82f, width * .73f, height * .58f);
                    DrawRoute(canvas, width * .86f, height * .59f, width * .68f, height * .50f);
                }
                else if (Contains(name, "Flood", "Sail", "Boot"))
                {
                    DrawRoute(canvas, width * .14f, height * .59f, width * .76f, height * .18f);
                    DrawRoute(canvas, width * .42f, height * .58f, width * .78f, height * .36f);
                    DrawRoute(canvas, width * .86f, height * .59f, width * .82f, height * .48f);
                }
                else if (Contains(name, "Smash"))
                {
                    DrawRoute(canvas, width * .14f, height * .59f, width * .14f, height * .45f);
                    DrawRoute(canvas, width * .38f, height * .58f, width * .18f, height * .18f);
                    DrawRoute(canvas, width * .86f, height * .59f, width * .86f, height * .45f);
                }
                else
                {
                    DrawRoute(canvas, width * .14f, height * .59f, width * (.18f + (seed % 3) * .08f), height * top);
                    DrawRoute(canvas, width * .86f, height * .59f, width * (.82f - (seed % 4) * .07f), height * (top + .04f));
                    DrawRoute(canvas, width * .42f, height * .58f, width * .58f, height * (deep ? .2f : .39f));
                }
            }
        }

        private static bool Contains(string value, params string[] terms) =>
            terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));

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
