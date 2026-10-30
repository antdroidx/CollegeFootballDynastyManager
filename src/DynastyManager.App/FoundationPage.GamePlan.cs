using Microsoft.Maui.Layouts;
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
        return new Border { Content = layout, Padding = 14, BackgroundColor = DynastyTheme.Palette("#1B2632"), StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 } };
    }

    private void RenderGamePlanScreen()
    {
        _gamePlanHost.Children.Clear();
        if (_currentDynasty is null)
        {
            _gamePlanHost.Children.Add(new Label { Text = "Create or load a dynasty to build your game plan." });
            return;
        }
        var program = new Button { Text = "Program & staff", BackgroundColor = DynastyTheme.Palette("#493D65") };
        program.Clicked += (_, _) => ShowSection(AppSection.Program);
        var depthChart = new Button { Text = "Manage depth chart", BackgroundColor = DynastyTheme.Palette("#315D59") };
        depthChart.Clicked += (_, _) => { _showDepthChartOrganizer = true; ShowSection(AppSection.Roster); };
        _gamePlanHost.Children.Add(new ActionRow { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, Children = { program, depthChart } });
        var careerTools = new ActionRow { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap };
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
                View Matchup(string heading, double theirs, double ours) => new Border { BackgroundColor = DynastyTheme.Palette("#142C35"), Padding = 10,
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
                scouting.Children.Add(new Label { Text = passEdge >= runEdge ? "Matchup focus: your passing game has the better relative matchup." : "Matchup focus: your running game has the better relative matchup.", TextColor = DynastyTheme.Palette("#8CDBC0") });
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
                row.Add(PlayerLink(player), 0, 0); row.Add(new Label { Text = $"{player.Position.DisplayName()} · {player.OverallRating} OVR", VerticalOptions = LayoutOptions.Center, TextColor = DynastyTheme.Muted }, 1, 0);
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
        var offense = new Picker { AutomationId = "offensive-playbook", Title = "Offensive playbook", ItemsSource = offenseValues.Select(GamePlanService.Label).ToList(), SelectedIndex = Array.IndexOf(offenseValues, plan.Offense) };
        var defense = new Picker { AutomationId = "defensive-playbook", Title = "Defensive playbook", ItemsSource = defenseValues.Select(GamePlanService.Label).ToList(), SelectedIndex = Array.IndexOf(defenseValues, plan.Defense) };
        var approach = new Picker { Title = "Defensive approach", ItemsSource = approachValues.Select(GamePlanService.Label).ToList(), SelectedIndex = Array.IndexOf(approachValues, plan.Approach) };
        var offensiveFormation = new Picker { Title = "Primary offensive formation", ItemsSource = offensiveFormationValues.Select(GamePlanService.Label).ToList(), SelectedIndex = Array.IndexOf(offensiveFormationValues, plan.OffensiveFormation) };
        var defensiveFormation = new Picker { Title = "Primary defensive formation", ItemsSource = defensiveFormationValues.Select(GamePlanService.Label).ToList(), SelectedIndex = Array.IndexOf(defensiveFormationValues, plan.DefensiveFormation) };
        tempo.IsEnabled = offense.IsEnabled = offensiveFormation.IsEnabled = UserCoachControlService.ControlsOffense(_currentDynasty);
        defense.IsEnabled = approach.IsEnabled = defensiveFormation.IsEnabled = UserCoachControlService.ControlsDefense(_currentDynasty);
        if (_currentDynasty.UserCoachRole != StaffRole.HeadCoach)
            settingsHost.Children.Add(new Label { Text = UserCoachControlService.Label(_currentDynasty.UserCoachRole) + " · The staff manages the other side.", TextColor = DynastyTheme.Muted });
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
        var editor = new Button { Text = "Open Playbook Editor", BackgroundColor = DynastyTheme.Palette("#315D59") };
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
        void FinishCoachTabs()
        {
            var planPanel = new VerticalStackLayout { Spacing = 12 };
            var controls = _gamePlanHost.Children.OfType<View>().ToArray();
            _gamePlanHost.Children.Clear();
            foreach (var view in controls) planPanel.Children.Add(view);
            _gamePlanHost.Children.Add(ProfileTabs("coach", ("Game plan", planPanel), ("Development", BuildCoachDevelopmentPanel())));
        }
        if (last is null) { FinishCoachTabs(); return; }
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
        FinishCoachTabs();
    }

    private static View PlayLibraryRow(string id, string name, string details, string description,
        bool offense, string styleKey,
        HashSet<string> favorites, Action rerender, OffensivePlayDefinition? definition = null, DefensivePlayDefinition? defensiveDefinition = null)
    {
        var favorite = favorites.Contains(id);
        var toggle = new Button { Text = favorite ? "★" : "☆", FontSize = 20, Padding = new Thickness(8, 2),
            BackgroundColor = Colors.Transparent, TextColor = favorite ? DynastyTheme.Palette("#E8C979") : DynastyTheme.Muted };
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
            BackgroundColor = DynastyTheme.Palette("#102A24")
        };
        SemanticProperties.SetDescription(art, $"{name} play diagram");
        var grid = new Grid { ColumnSpacing = 10, RowSpacing = 6,
            ColumnDefinitions = new ColumnDefinitionCollection { new(new GridLength(150)), new(GridLength.Star), new(GridLength.Auto) } };
        grid.Add(art, 0, 0);
        grid.Add(new VerticalStackLayout { Spacing = 3, Children =
        {
            new Label { Text = name, FontAttributes = FontAttributes.Bold, FontSize = 13 },
            new Label { Text = details, FontSize = 10, TextColor = DynastyTheme.Palette("#B8D8FA") },
            new Label { Text = description, FontSize = 11, TextColor = DynastyTheme.Muted, LineBreakMode = LineBreakMode.WordWrap }
        } }, 1, 0);
        grid.Add(toggle, 2, 0);
        return new Border { StrokeThickness = 0, BackgroundColor = DynastyTheme.Palette("#1B2B37"), Padding = new Thickness(10, 10), Content = grid };
    }

}
