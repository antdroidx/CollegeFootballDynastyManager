using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Import;
using DynastyManager.Data.Persistence;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private StartingLibrary _startingLibrary = new();
    private StartingRosterPack? _bundledStartingRoster;
    private StartingLibraryRepository? _startingLibraryRepository;
    private bool _startingLibrarySaving;
    private string? _startingLibraryError;
    private readonly Picker _startingRosterPicker = new() { Title = "Starting roster" };
    private IReadOnlyList<StartingLeagueSetup> _startingSetups = [];
    private IReadOnlyList<StartingRosterPack> _compatibleStartingRosters = [];
    private static readonly Guid ModernSetupId = Guid.Parse("2be66e42-ec81-49e9-8d1c-000000000001");
    private static readonly Guid LegacySetupId = Guid.Parse("2be66e42-ec81-49e9-8d1c-000000000002");

    private IReadOnlyList<StartingLeagueSetup> AvailableStartingSetups() => new[]
    {
        new StartingLeagueSetup { Id = ModernSetupId, Name = "Modern FBS (2026) - 138 teams", Teams = ModernFbsUniverseFactory.Create(_legacyTeams), IsBuiltIn = true, Rules = new LeagueRules { FcsOpponents = true } },
        new StartingLeagueSetup { Id = LegacySetupId, Name = "Legacy FBS - 130 teams", Teams = _legacyTeams, IsBuiltIn = true, Rules = new LeagueRules { FcsOpponents = true } }
    }.Where(s => !_startingLibrary.HiddenBuiltIns.Contains(s.Id)).Concat(_startingLibrary.Setups).ToArray();

    private StartingLeagueSetup? SelectedStartingSetup => _startingSetups.ElementAtOrDefault(_leaguePicker.SelectedIndex);
    private IReadOnlyList<StartingRosterPack> AvailableStartingRosters() =>
        (_bundledStartingRoster is null ? Enumerable.Empty<StartingRosterPack>() : new[] { _bundledStartingRoster })
        .Concat(_startingLibrary.Rosters).ToArray();

    private StartingRosterPack? SelectedStartingRoster => _startingRosterPicker.SelectedIndex > 0
        ? _compatibleStartingRosters.ElementAtOrDefault(_startingRosterPicker.SelectedIndex - 1) : null;

    private void RefreshStartingChoices(Guid? preferredSetup = null, Guid? preferredRoster = null)
    {
        var previousSchool = _teamPicker.SelectedItem as string;
        preferredSetup ??= SelectedStartingSetup?.Id;
        _startingSetups = AvailableStartingSetups();
        _leaguePicker.ItemsSource = _startingSetups.Select(s => s.Name).ToArray();
        _leaguePicker.SelectedIndex = Math.Max(0, Array.FindIndex(_startingSetups.ToArray(), s => s.Id == preferredSetup));
        UpdateNewDynastyTeams();
        if (previousSchool is not null && _newDynastyTeams.Any(t => t.Name == previousSchool)) _teamPicker.SelectedItem = previousSchool;
        if (preferredRoster.HasValue) _startingRosterPicker.SelectedIndex = Array.FindIndex(_compatibleStartingRosters.ToArray(), r => r.Id == preferredRoster) + 1;
    }

    private void UpdateStartingRosterChoices()
    {
        var setup = SelectedStartingSetup;
        _compatibleStartingRosters = setup is null ? [] : AvailableStartingRosters().Where(r => StartingLibraryService.IsCompatible(r, setup.Teams)).ToArray();
        _startingRosterPicker.ItemsSource = new[] { "Legacy / generated roster" }.Concat(_compatibleStartingRosters.Select(r => r.Name)).ToArray();
        var defaultId = setup is not null && _startingLibrary.DefaultRosterBySetup.TryGetValue(setup.Id, out var id) ? id : _bundledStartingRoster?.Id ?? Guid.Empty;
        _startingRosterPicker.SelectedIndex = Array.FindIndex(_compatibleStartingRosters.ToArray(), r => r.Id == defaultId) + 1;
        _newDynastyButton.IsEnabled = setup is not null;
    }

    private Button StartingLibraryButton()
    {
        var button = new Button { Text = "Starting rosters & league setups", BackgroundColor = DynastyTheme.Palette("#315D59") };
        button.Clicked += async (_, _) => await OpenStartingLibraryAsync();
        return button;
    }

    private async Task SaveStartingLibraryAsync(StartingLibrary next)
    {
        if (_startingLibraryRepository is null) throw new InvalidOperationException("The starting library is still loading.");
        if (_startingLibrarySaving) throw new InvalidOperationException("A library save is in progress. Please try again.");
        _startingLibrarySaving = true;
        try { await _startingLibraryRepository.SaveAsync(next); _startingLibrary = next; RefreshStartingChoices(); }
        finally { _startingLibrarySaving = false; }
    }

    private async Task OpenStartingLibraryAsync()
    {
        if (_startingLibraryError is not null) { await GameDialogs.AlertAsync(this, "Starting library", _startingLibraryError, "Close"); return; }
        if (_legacyTeams.Count == 0 || _startingLibraryRepository is null) return;
        var page = new ContentPage { Title = "Starting Library", BackgroundColor = DynastyTheme.Background };
        var rows = new VerticalStackLayout { Spacing = 12 }; var status = new Label { TextColor = DynastyTheme.Muted };
        var close = new Button { Text = "Close" }; close.Clicked += async (_, _) => await Navigation.PopModalAsync();
        var import = new Button { Text = "Import starting roster" };
        import.Clicked += async (_, _) => { await OpenRosterImportAsync(); Render(); };
        var add = new Button { Text = "Add league setup" };
        add.Clicked += async (_, _) => { await EditStartingSetupAsync(null); Render(); };
        var restore = new Button { Text = "Restore built-in league options" };
        restore.Clicked += async (_, _) => { try { await SaveStartingLibraryAsync(_startingLibrary with { HiddenBuiltIns = [] }); Render(); } catch (Exception ex) { status.Text = ex.Message; } };
        void Render()
        {
            rows.Children.Clear(); rows.Children.Add(SectionTitle("League setups", "Each setup stores its teams, conferences and starting rules. Removing a library item never changes an existing dynasty."));
            foreach (var setup in AvailableStartingSetups())
            {
                var edit = new Button { Text = setup.IsBuiltIn ? "Copy & edit" : "Edit" };
                edit.Clicked += async (_, _) => { await EditStartingSetupAsync(setup); Render(); };
                var defaults = new Button { Text = "Default starting roster" };
                defaults.Clicked += async (_, _) =>
                {
                    var compatible = AvailableStartingRosters().Where(r => StartingLibraryService.IsCompatible(r, setup.Teams)).ToArray();
                    var labels = new[] { "Legacy / generated roster" }.Concat(compatible.Select((r, i) => $"{i + 1}. {r.Name}")).ToArray();
                    var choice = await GameDialogs.ActionSheetAsync(this, setup.Name, "Cancel", null, labels); var index = Array.IndexOf(labels, choice);
                    if (index < 0) return;
                    try { var map = _startingLibrary.DefaultRosterBySetup.ToDictionary(); map[setup.Id] = index > 0 ? compatible[index - 1].Id : Guid.Empty;
                        await SaveStartingLibraryAsync(_startingLibrary with { DefaultRosterBySetup = map }); Render(); }
                    catch (Exception ex) { status.Text = ex.Message; }
                };
                var remove = new Button { Text = "Remove" };
                remove.Clicked += async (_, _) => { if (!await GameDialogs.AlertAsync(this, "Remove league option?", $"Remove {setup.Name} from the starting library? Existing dynasties keep their setup.", "Remove", "Cancel")) return;
                    try { await SaveStartingLibraryAsync(StartingLibraryService.RemoveSetup(_startingLibrary, setup)); Render(); } catch (Exception ex) { status.Text = ex.Message; } };
                var defaultId = _startingLibrary.DefaultRosterBySetup.TryGetValue(setup.Id, out var rosterId)
                    ? rosterId : _bundledStartingRoster?.Id ?? Guid.Empty;
                var defaultName = AvailableStartingRosters().FirstOrDefault(r => r.Id == defaultId)?.Name ?? "Legacy / generated";
                rows.Children.Add(new Border { StrokeThickness = 0, Padding = 10, BackgroundColor = DynastyTheme.Palette("#1B2B37"), Content = new VerticalStackLayout { Spacing = 5, Children =
                { new Label { Text = setup.Name, FontAttributes = FontAttributes.Bold }, new Label { Text = $"{setup.Teams.Count} teams · {setup.Rules.GamesPerTeam} games · {setup.Rules.PlayoffTeams}-team playoff · Roster: {defaultName}", FontSize = 11 },
                    new ActionRow { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, Children = { edit, defaults, remove } } } } });
            }
            rows.Children.Add(SectionTitle("Roster packs", "A pack replaces the listed teams' starting rosters. Teams missing from the pack use generated/default players. Packs can be reused across setups; schools outside the chosen setup are ignored."));
            if (_bundledStartingRoster is { } included) rows.Children.Add(new Border { Content = new VerticalStackLayout { Spacing = 5, Children =
            {
                new Label { Text = included.Name, FontAttributes = FontAttributes.Bold },
                new Label { Text = $"Included default · {included.Players.Count:N0} players · 136 schools. The modern 138-school setup generates players for North Dakota State and Sacramento State.", FontSize = 11 },
                new Label { Text = "Choose a different default for each setup above. Imported roster packs remain available.", FontSize = 11, TextColor = DynastyTheme.Muted }
            } } });
            foreach (var roster in _startingLibrary.Rosters)
            {
                var remove = new Button { Text = "Remove roster pack" };
                remove.Clicked += async (_, _) => { if (!await GameDialogs.AlertAsync(this, "Remove starting roster?", $"Remove {roster.Name}? Existing dynasties retain their players.", "Remove", "Cancel")) return;
                    try { await SaveStartingLibraryAsync(StartingLibraryService.RemoveRoster(_startingLibrary, roster.Id)); Render(); } catch (Exception ex) { status.Text = ex.Message; } };
                rows.Children.Add(new VerticalStackLayout { Children = { new Label { Text = $"{roster.Name} · {roster.Players.Count} players · {roster.Players.Select(p => p.TeamName).Distinct().Count()} teams" }, remove } });
            }
            if (_startingLibrary.Rosters.Count == 0) rows.Children.Add(new Label { Text = "No imported starting rosters yet." });
        }
        page.Appearing += (_, _) => Render();
        Render(); page.Content = new ScrollView { Content = new VerticalStackLayout { Padding = 14, Spacing = 10, Children = { close, import, add, restore, status, rows } } };
        await Navigation.PushModalAsync(page);
    }
}
