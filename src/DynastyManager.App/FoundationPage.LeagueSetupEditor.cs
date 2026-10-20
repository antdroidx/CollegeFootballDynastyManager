using System.Globalization;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Import;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private async Task EditStartingSetupAsync(StartingLeagueSetup? original)
    {
        var page = new ContentPage { Title = "League Setup", BackgroundColor = DynastyTheme.Background };
        var name = new Entry { Text = original is null ? "My League" : original.Name + (original.IsBuiltIn ? " Custom" : ""), MaxLength = 80 };
        var teams = original?.Teams.ToList() ?? new List<Team>();
        var games = new Picker { Title = "Regular-season games per team", ItemsSource = Enumerable.Range(1, 12).Select(n => n.ToString()).ToArray(), SelectedIndex = (original?.Rules.GamesPerTeam ?? 6) - 1 };
        var playoffs = new Picker { Title = "Playoff size", ItemsSource = new[] { "2", "4", "8", "12" }, SelectedIndex = Array.IndexOf(new[] { 2, 4, 8, 12 }, original?.Rules.PlayoffTeams ?? 4) };
        var championships = new Switch { IsToggled = original?.Rules.ConferenceChampionships ?? true };
        var list = new VerticalStackLayout { Spacing = 5 }; var status = new Label { TextColor = DynastyTheme.Muted };
        var search = new SearchBar { Placeholder = "Find a team" };
        var summary = new Label { TextColor = DynastyTheme.Accent };
        var save = new Button { Text = "Validate schedule & save setup" }; var cancel = new Button { Text = "Cancel" };
        var cancelled = false;
        cancel.Clicked += async (_, _) => { cancelled = true; await Navigation.PopModalAsync(); };
        page.Disappearing += (_, _) => { if (!save.IsEnabled) cancelled = true; };
        void Render()
        {
            summary.Text = $"{teams.Count} teams · {teams.Select(t => t.ConferenceName).Distinct(StringComparer.OrdinalIgnoreCase).Count()} conferences";
            list.Children.Clear();
            foreach (var team in teams.Where(t => string.IsNullOrWhiteSpace(search.Text) || t.Name.Contains(search.Text, StringComparison.OrdinalIgnoreCase)).OrderBy(t => t.Name).Take(40))
            {
                var edit = new Button { Text = "Edit" }; var remove = new Button { Text = "Remove" };
                edit.Clicked += async (_, _) => await EditTeamAsync(team);
                remove.Clicked += (_, _) => { teams.Remove(team); Render(); };
                list.Children.Add(new Border { StrokeThickness = 0, Padding = 8, BackgroundColor = Color.FromArgb("#1B2B37"), Content = new VerticalStackLayout { Spacing = 3, Children =
                { new Label { Text = $"{team.Name} ({team.Abbreviation})", FontAttributes = FontAttributes.Bold },
                    new Label { Text = $"{team.ConferenceName}{(team.DivisionName is null ? "" : " · " + team.DivisionName)} · Prestige {team.Prestige}", FontSize = 11 },
                    new HorizontalStackLayout { Children = { edit, remove } } } } });
            }
        }
        async Task EditTeamAsync(Team? existing)
        {
            var editor = new ContentPage { Title = "Team", BackgroundColor = DynastyTheme.Background };
            var teamName = new Entry { Text = existing?.Name, Placeholder = "School name" };
            var abbreviation = new Entry { Text = existing?.Abbreviation, Placeholder = "Abbreviation", MaxLength = 8 };
            var conference = new Entry { Text = existing?.ConferenceName ?? "Independent", Placeholder = "Conference" };
            var division = new Entry { Text = existing?.DivisionName, Placeholder = "Division (optional)" };
            var prestige = new Entry { Text = (existing?.Prestige ?? 50).ToString(), Keyboard = Keyboard.Numeric };
            var region = new Picker { ItemsSource = RecruitGeography.RegionNames.ToArray(), SelectedIndex = Math.Clamp(existing?.LegacyRegionId ?? 0, 0, 4) };
            var conferenceGames = new Entry { Text = existing?.ConferenceGamesPerSeason?.ToString(), Placeholder = "Automatic", Keyboard = Keyboard.Numeric };
            var message = new Label { TextColor = DynastyTheme.Accent }; var done = new Button { Text = "Save team" }; var back = new Button { Text = "Cancel" };
            back.Clicked += async (_, _) => await Navigation.PopModalAsync();
            done.Clicked += async (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(teamName.Text) || string.IsNullOrWhiteSpace(abbreviation.Text) || string.IsNullOrWhiteSpace(conference.Text) ||
                    !int.TryParse(prestige.Text, out var value) || value is < 0 or > 100)
                { message.Text = "Enter school, abbreviation, conference, prestige 0–100 and a recruiting region."; return; }
                int? target = null;
                if (!string.IsNullOrWhiteSpace(conferenceGames.Text))
                { if (!int.TryParse(conferenceGames.Text, out var n) || n is < 0 or > 12) { message.Text = "Conference games must be automatic or 0–12."; return; } target = n; }
                if (teams.Any(t => t != existing && t.Name.Equals(teamName.Text.Trim(), StringComparison.OrdinalIgnoreCase))) { message.Text = "That school name already exists."; return; }
                var updated = new Team { Id = existing?.Id ?? Guid.NewGuid(), Name = teamName.Text.Trim(), Abbreviation = abbreviation.Text.Trim(), ConferenceName = conference.Text.Trim(),
                    DivisionName = string.IsNullOrWhiteSpace(division.Text) ? null : division.Text.Trim(), Prestige = value, LegacyRegionId = Math.Max(0, region.SelectedIndex), ConferenceGamesPerSeason = target };
                if (existing is not null) teams.Remove(existing); teams.Add(updated);
                await Navigation.PopModalAsync(); Render();
            };
            editor.Content = new ScrollView { Content = new VerticalStackLayout { Padding = 14, Spacing = 8, Children =
            { back, new Label { Text = "School" }, teamName, new Label { Text = "Abbreviation" }, abbreviation, new Label { Text = "Conference" }, conference,
                new Label { Text = "Division" }, division, new Label { Text = "Prestige" }, prestige, new Label { Text = "Recruiting region" }, region,
                new Label { Text = "Conference games per season" }, conferenceGames, done, message } } };
            await Navigation.PushModalAsync(editor);
        }
        search.TextChanged += (_, _) => Render();
        var add = new Button { Text = "Add team" }; add.Clicked += async (_, _) => await EditTeamAsync(null);
        var import = new Button { Text = "Import league universe CSV" };
        import.Clicked += async (_, _) =>
        {
            try
            {
                var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Choose original-game universe CSV" }); if (file is null) return;
                using var stream = await file.OpenReadAsync(); using var reader = new StreamReader(stream);
                var buffer = new char[2_000_001]; var length = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
                if (length > 2_000_000) throw new FormatException("Universe CSV exceeds the 2 MB limit.");
                var imported = LegacyUniverseCsvImporter.Import(new string(buffer, 0, length));
                if (imported.Teams.Count < 2) throw new FormatException("Select an original-game universe CSV containing [START_TEAMS] and [END_TEAMS].");
                teams = imported.Teams.ToList(); Render(); status.Text = "Imported team/conference setup. Validate the schedule before saving.";
            }
            catch (Exception ex) { status.Text = ex.Message; }
        };
        var checking = new ActivityIndicator { IsVisible = false };
        save.Clicked += async (_, _) =>
        {
            save.IsEnabled = false; checking.IsVisible = checking.IsRunning = true;
            list.IsEnabled = name.IsEnabled = games.IsEnabled = playoffs.IsEnabled = championships.IsEnabled = import.IsEnabled = add.IsEnabled = false;
            try
            {
                var setup = new StartingLeagueSetup { Id = original is { IsBuiltIn: false } ? original.Id : Guid.NewGuid(), Name = name.Text?.Trim() ?? "",
                    Teams = teams.ToArray(), Rules = new LeagueRules { GamesPerTeam = games.SelectedIndex + 1, PlayoffTeams = new[] { 2, 4, 8, 12 }[Math.Max(0, playoffs.SelectedIndex)],
                        ConferenceChampionships = championships.IsToggled, AllowAutomaticConferenceFill = true } };
                if (_startingLibrary.Setups.Any(s => s.Id != setup.Id && s.Name.Equals(setup.Name, StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("Choose a unique league setup name.");
                StartingLibraryService.Validate(setup); status.Text = "Checking schedule feasibility...";
                await Task.Run(() => SeasonScheduleBuilder.BuildRegularSeason(setup.Teams, setup.Id, 2026, gamesPerTeam: setup.Rules.GamesPerTeam, allowAutomaticConferenceFill: true));
                if (cancelled) return;
                // A roster default only survives edits while the roster still includes schools in the setup.
                var defaults = _startingLibrary.DefaultRosterBySetup.ToDictionary();
                if (defaults.TryGetValue(setup.Id, out var rosterId) && _startingLibrary.Rosters.FirstOrDefault(r => r.Id == rosterId) is { } roster && !StartingLibraryService.IsCompatible(roster, setup.Teams)) defaults.Remove(setup.Id);
                await SaveStartingLibraryAsync(_startingLibrary with { Setups = _startingLibrary.Setups.Where(s => s.Id != setup.Id).Append(setup).ToArray(), DefaultRosterBySetup = defaults });
                RefreshStartingChoices(setup.Id); if (Navigation.ModalStack.LastOrDefault() == page) await Navigation.PopModalAsync();
            }
            catch (Exception ex) { status.Text = ex.Message; }
            finally { save.IsEnabled = true; checking.IsVisible = checking.IsRunning = false;
                list.IsEnabled = name.IsEnabled = games.IsEnabled = playoffs.IsEnabled = championships.IsEnabled = import.IsEnabled = add.IsEnabled = true; }
        };
        Render(); page.Content = new ScrollView { Content = new VerticalStackLayout { Padding = 14, Spacing = 10, Children =
        { cancel, SectionTitle("League setup", "Add/remove schools and change conferences. Use automatic conference games unless you need exact targets. The current season calendar is 13 weeks; playoffs support 2, 4, 8 or 12 teams."),
            name, new Label { Text = "Games per team" }, games, new Label { Text = "Playoff teams" }, playoffs,
            new HorizontalStackLayout { Children = { championships, new Label { Text = "Conference championship games (conferences with 8+ teams)", VerticalOptions = LayoutOptions.Center } } },
            import, add, save, checking, status, summary, search, list } } };
        await Navigation.PushModalAsync(page);
    }
}
