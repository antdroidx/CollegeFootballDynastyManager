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
        var conceptValues = Enum.GetValues<OffensiveConcept>();
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
                    $"{GamePlanService.Label(play.Personnel)} · {GamePlanService.Label(play.Concept)}", play.Description, favoriteIds, RenderPlayLibrary));
            playLibrary.Children.Add(new Label { Text = $"DEFENSE · {GamePlanService.Label(selectedDefense)}", FontAttributes = FontAttributes.Bold, TextColor = DynastyTheme.Accent,
                Margin = new Thickness(0, 7, 0, 0) });
            foreach (var play in PlaybookCatalog.DefensivePlays.Where(item => item.Formation == selectedDefense))
                playLibrary.Children.Add(PlayLibraryRow(play.Id, play.Name,
                    $"{GamePlanService.Label(play.Personnel)} · {GamePlanService.Label(play.Approach)}", play.Description, favoriteIds, RenderPlayLibrary));
        }
        offensiveFormation.SelectedIndexChanged += (_, _) => RenderPlayLibrary();
        defensiveFormation.SelectedIndexChanged += (_, _) => RenderPlayLibrary();
        RenderPlayLibrary();
        _gamePlanHost.Children.Add(CoachingCard("PLAY LIBRARY",
            new Label { Text = "Browse the initial formation and personnel catalog. Star up to 20 calls for your working playbook; assignment editing follows in the visual play editor.", TextColor = DynastyTheme.Muted },
            playLibrary));
        var script = new VerticalStackLayout { Spacing = 8 };
        var calls = new List<Picker>();
        for (var index = 0; index < 3; index++)
        {
            script.Children.Add(new Label { Text = $"Drive {index + 1} priority", FontAttributes = FontAttributes.Bold });
            var picker = new Picker { Title = $"Drive {index + 1}", ItemsSource = conceptValues.Select(GamePlanService.Label).ToList(),
                SelectedIndex = Array.IndexOf(conceptValues, plan.OpeningScript.ElementAtOrDefault(index)) };
            calls.Add(picker);
            script.Children.Add(picker);
        }
        _gamePlanHost.Children.Add(CoachingCard("OPENING CALL SHEET",
            new Label { Text = "Guide your first three offensive possessions. Each choice sets a drive's emphasis; the playbook takes over afterward. This sheet repeats each game until you change it." },
            script, new Label { Text = "Deep throws offer more yards with more turnover risk. Quick throws trade some explosiveness for control. This first version simulates drives; live snap-by-snap calls are next.", TextColor = DynastyTheme.Muted }));
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
                    OpeningScript = calls.Select(picker => conceptValues[Math.Max(0, picker.SelectedIndex)]).ToArray()
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
                : $"Drive {drive.PossessionNumber} / {GamePlanService.Label(drive.Concept)} / {drive.Outcome}\n{drive.PassAttempts} passes, {drive.RushAttempts} runs / {drive.PassYards + drive.RushYards} yards / +{drive.Points} points" });
        _gamePlanHost.Children.Add(CoachingCard("LAST GAME / DRIVE REPORT", driveRows));
    }

    private static View PlayLibraryRow(string id, string name, string details, string description,
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
        var grid = new Grid { ColumnSpacing = 8,
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) } };
        grid.Add(new VerticalStackLayout { Spacing = 3, Children =
        {
            new Label { Text = name, FontAttributes = FontAttributes.Bold, FontSize = 13 },
            new Label { Text = details, FontSize = 10, TextColor = Color.FromArgb("#B8D8FA") },
            new Label { Text = description, FontSize = 11, TextColor = DynastyTheme.Muted, LineBreakMode = LineBreakMode.WordWrap }
        } }, 0, 0);
        grid.Add(toggle, 1, 0);
        return new Border { StrokeThickness = 0, BackgroundColor = Color.FromArgb("#1B2B37"), Padding = new Thickness(10, 8), Content = grid };
    }
}
